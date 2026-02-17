using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.IO;
using System.Linq;

namespace GlyphShell.Engine;

/// <summary>
/// Detects project types by scanning a directory's top-level contents for marker files.
/// Also detects content-type directories (music, photos, video, etc.) via well-known
/// directory names and file-extension sniffing.
/// Returns an icon name and color for the detected type, or null.
/// </summary>
public static class ProjectTypeDetector
{
    /// <summary>Result of project type detection.</summary>
    public readonly record struct ProjectTypeResult(string IconName, string ColorSequence);

    // Maps marker filename (case-insensitive) → project type key
    private static readonly FrozenDictionary<string, string> _markerToType;

    // Maps project type key → display info (icon + color)
    private static readonly FrozenDictionary<string, ProjectTypeResult> _typeToDisplay;

    // Priority: higher number wins when multiple markers match in same directory
    private static readonly FrozenDictionary<string, int> _typePriority;

    // Extensions to match via pattern (e.g. *.csproj)
    private static readonly FrozenSet<string> _dotnetProjectExtensions;

    // Maps a directory's own name → content type (case-insensitive)
    private static readonly FrozenDictionary<string, string> _dirNameToType;

    // Maps file extension → content type for sniffing (case-insensitive)
    private static readonly FrozenDictionary<string, string> _extensionToContentType;

    /// <summary>Minimum proportion of files that must belong to one category for sniffing to match.</summary>
    private const double ContentSniffThreshold = 0.6;

    /// <summary>Minimum number of files required before content sniffing applies.</summary>
    private const int ContentSniffMinFiles = 3;

    /// <summary>Maximum files to inspect during content sniffing.</summary>
    private const int ContentSniffMaxScan = 100;

    /// <summary>Highest marker priority — break immediately when reached (unity/unreal).</summary>
    private const int MaxMarkerPriority = 7;

    /// <summary>How long cached results remain valid.</summary>
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(15);

    /// <summary>Max filesystem entries to inspect per directory to avoid hanging on huge dirs.</summary>
    private const int MaxEnumerateEntries = 500;

    private static readonly ConcurrentDictionary<string, (ProjectTypeResult? Result, DateTime CachedAt)> _cache = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Clears the detection cache. Useful for testing or after filesystem changes.</summary>
    public static void ClearCache() => _cache.Clear();

    static ProjectTypeDetector()
    {
        _typeToDisplay = new Dictionary<string, ProjectTypeResult>(StringComparer.OrdinalIgnoreCase)
        {
            // Programming project types — BMP glyphs only (U+0000–U+FFFF).
            // Supplementary plane glyphs (nf-md-*) cause PS column misalignment
            // due to surrogate pair width bug (PS#23861). Use nf-dev-*/nf-seti-*/nf-fa-*.
            ["dotnet"]     = new("nf-seti-c_sharp",      ColorEngine.FromHex("#9B6BDF")),
            ["nodejs"]     = new("nf-dev-nodejs_small",   ColorEngine.FromHex("#8CC84B")),
            ["typescript"] = new("nf-seti-typescript",    ColorEngine.FromHex("#3178C6")),
            ["python"]     = new("nf-dev-python",         ColorEngine.FromHex("#FFD43B")),
            ["go"]         = new("nf-dev-go",             ColorEngine.FromHex("#00ADD8")),
            ["rust"]       = new("nf-dev-rust",           ColorEngine.FromHex("#DEA584")),
            ["ruby"]       = new("nf-dev-ruby",           ColorEngine.FromHex("#CC342D")),
            ["java"]       = new("nf-dev-java",           ColorEngine.FromHex("#F89820")),
            ["php"]        = new("nf-dev-php",            ColorEngine.FromHex("#777BB4")),
            ["dart"]       = new("nf-dev-dart",           ColorEngine.FromHex("#02569B")),
            ["swift"]      = new("nf-dev-swift",          ColorEngine.FromHex("#F05138")),
            ["cpp"]        = new("nf-seti-cpp",           ColorEngine.FromHex("#00599C")),
            ["docker"]     = new("nf-dev-docker",         ColorEngine.FromHex("#2496ED")),
            ["unity"]      = new("nf-fa-gamepad",         ColorEngine.FromHex("#555555")),
            ["unreal"]     = new("nf-fa-gamepad",         ColorEngine.FromHex("#555555")),

            // Content-type directories — BMP glyphs (nf-fa-*) for same reason.
            ["music"]      = new("nf-fa-music",           ColorEngine.FromHex("#1DB954")),
            ["photos"]     = new("nf-fa-camera",          ColorEngine.FromHex("#FF6F61")),
            ["video"]      = new("nf-fa-film",            ColorEngine.FromHex("#E040FB")),
            ["documents"]  = new("nf-fa-file_text",       ColorEngine.FromHex("#4FC3F7")),
            ["fonts"]      = new("nf-fa-font",            ColorEngine.FromHex("#E0E0E0")),
            ["3d"]         = new("nf-fa-cube",            ColorEngine.FromHex("#FF7043")),
            ["ebooks"]     = new("nf-fa-book",            ColorEngine.FromHex("#8D6E63")),
            ["games"]      = new("nf-fa-gamepad",         ColorEngine.FromHex("#76FF03")),
            ["design"]     = new("nf-fa-paint_brush",     ColorEngine.FromHex("#FF80AB")),
            ["data"]       = new("nf-fa-database",        ColorEngine.FromHex("#26C6DA")),
        }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

        // Higher priority wins when multiple markers found.
        // Content types are priority 0 — any programming marker overrides them.
        _typePriority = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["docker"]     = 1,
            ["nodejs"]     = 2,
            ["php"]        = 2,
            ["ruby"]       = 2,
            ["dart"]       = 3,
            ["python"]     = 3,
            ["java"]       = 3,
            ["go"]         = 4,
            ["rust"]       = 4,
            ["swift"]      = 4,
            ["cpp"]        = 4,
            ["dotnet"]     = 5,
            ["typescript"] = 6,  // tsconfig.json beats package.json
            ["unity"]      = 7,
            ["unreal"]     = 7,

            // Content types — all priority 0 (lowest)
            ["music"]      = 0,
            ["photos"]     = 0,
            ["video"]      = 0,
            ["documents"]  = 0,
            ["fonts"]      = 0,
            ["3d"]         = 0,
            ["ebooks"]     = 0,
            ["games"]      = 0,
            ["design"]     = 0,
            ["data"]       = 0,
        }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

        // Exact filename markers
        _markerToType = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["package.json"]        = "nodejs",
            ["tsconfig.json"]       = "typescript",
            ["pyproject.toml"]      = "python",
            ["requirements.txt"]    = "python",
            ["setup.py"]            = "python",
            ["Pipfile"]             = "python",
            ["go.mod"]              = "go",
            ["Cargo.toml"]          = "rust",
            ["Gemfile"]             = "ruby",
            ["pom.xml"]             = "java",
            ["build.gradle"]        = "java",
            ["build.gradle.kts"]    = "java",
            ["composer.json"]       = "php",
            ["pubspec.yaml"]        = "dart",
            ["Package.swift"]       = "swift",
            ["CMakeLists.txt"]      = "cpp",
            ["Makefile"]            = "cpp",
            ["Dockerfile"]          = "docker",
            ["docker-compose.yml"]  = "docker",
            ["docker-compose.yaml"] = "docker",
        }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

        _dotnetProjectExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".csproj", ".fsproj", ".vbproj", ".sln", ".slnx"
        }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

        // Well-known directory names → content type
        _dirNameToType = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // Music
            ["Music"]       = "music",
            ["Songs"]       = "music",
            ["Audio"]       = "music",
            ["Podcasts"]    = "music",
            ["Playlists"]   = "music",
            ["Soundtracks"] = "music",

            // Photos
            ["Photos"]      = "photos",
            ["Pictures"]    = "photos",
            ["Images"]      = "photos",
            ["Screenshots"] = "photos",
            ["Wallpapers"]  = "photos",
            ["Camera"]      = "photos",
            ["DCIM"]        = "photos",
            ["Camera Roll"] = "photos",

            // Video
            ["Videos"]      = "video",
            ["Movies"]      = "video",
            ["Films"]       = "video",
            ["Clips"]       = "video",
            ["Recordings"]  = "video",
            ["Screencasts"] = "video",

            // Documents
            ["Documents"]   = "documents",
            ["Docs"]        = "documents",
            ["Papers"]      = "documents",
            ["PDFs"]        = "documents",
            ["Reports"]     = "documents",
            ["Invoices"]    = "documents",
            ["Contracts"]   = "documents",
            ["Receipts"]    = "documents",

            // Fonts
            ["Fonts"]       = "fonts",
            ["Typography"]  = "fonts",

            // 3D
            ["3D"]          = "3d",
            ["Models"]      = "3d",
            ["Blender"]     = "3d",
            ["CAD"]         = "3d",

            // Ebooks
            ["Books"]       = "ebooks",
            ["Ebooks"]      = "ebooks",
            ["Library"]     = "ebooks",
            ["Kindle"]      = "ebooks",

            // Games
            ["Games"]       = "games",
            ["ROMs"]        = "games",
            ["ISOs"]        = "games",
            ["Emulators"]   = "games",

            // Design
            ["Design"]      = "design",
            ["Creative"]    = "design",
            ["Artwork"]     = "design",
            ["Art"]         = "design",
            ["Graphics"]    = "design",
            ["Mockups"]     = "design",
            ["Logos"]       = "design",

            // Data
            ["Datasets"]    = "data",
            ["Data"]        = "data",
            ["Databases"]   = "data",
        }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

        // File extension → content type for sniffing
        _extensionToContentType = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // Music / Audio
            [".mp3"]  = "music", [".flac"] = "music", [".wav"]  = "music", [".ogg"]  = "music",
            [".aac"]  = "music", [".wma"]  = "music", [".m4a"]  = "music", [".opus"] = "music",
            [".aiff"] = "music", [".alac"] = "music", [".mid"]  = "music", [".midi"] = "music",

            // Photos / Images
            [".jpg"]  = "photos", [".jpeg"] = "photos", [".png"]  = "photos", [".bmp"]  = "photos",
            [".raw"]  = "photos", [".cr2"]  = "photos", [".nef"]  = "photos", [".arw"]  = "photos",
            [".dng"]  = "photos", [".tiff"] = "photos", [".tif"]  = "photos", [".webp"] = "photos",
            [".gif"]  = "photos", [".heic"] = "photos", [".heif"] = "photos", [".svg"]  = "photos",
            [".ico"]  = "photos", [".psd"]  = "photos", [".xcf"]  = "photos",

            // Video (note: .ts omitted — conflicts with TypeScript)
            [".mp4"]  = "video", [".avi"]  = "video", [".mkv"]  = "video", [".mov"]  = "video",
            [".wmv"]  = "video", [".flv"]  = "video", [".webm"] = "video", [".m4v"]  = "video",
            [".mpg"]  = "video", [".mpeg"] = "video", [".vob"]  = "video",
            [".3gp"]  = "video", [".ogv"]  = "video",

            // Documents
            [".pdf"]  = "documents", [".doc"]  = "documents", [".docx"] = "documents",
            [".xls"]  = "documents", [".xlsx"] = "documents", [".ppt"]  = "documents",
            [".pptx"] = "documents", [".odt"]  = "documents", [".ods"]  = "documents",
            [".odp"]  = "documents", [".rtf"]  = "documents", [".txt"]  = "documents",

            // Fonts
            [".ttf"]  = "fonts", [".otf"]  = "fonts", [".woff"] = "fonts",
            [".woff2"] = "fonts", [".eot"]  = "fonts",

            // 3D Models
            [".obj"]  = "3d", [".fbx"]  = "3d", [".stl"]  = "3d", [".blend"] = "3d",
            [".3ds"]  = "3d", [".dae"]  = "3d", [".gltf"] = "3d", [".glb"]   = "3d",
            [".usdz"] = "3d", [".ply"]  = "3d", [".step"] = "3d", [".stp"]   = "3d",

            // Ebooks
            [".epub"] = "ebooks", [".mobi"] = "ebooks", [".azw"]  = "ebooks",
            [".azw3"] = "ebooks", [".cbr"]  = "ebooks", [".cbz"]  = "ebooks", [".djvu"] = "ebooks",

            // Design
            [".ai"]       = "design", [".sketch"] = "design", [".fig"]      = "design",
            [".xd"]       = "design", [".afdesign"] = "design", [".afphoto"] = "design",
            [".indd"]     = "design",

            // Data
            [".csv"]     = "data", [".tsv"]     = "data", [".sqlite"]  = "data",
            [".db"]      = "data", [".mdb"]     = "data", [".accdb"]   = "data",
            [".parquet"] = "data", [".json"]    = "data", [".ndjson"]  = "data",
            [".avro"]    = "data",
        }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Scans the directory's top-level entries for project marker files, checks well-known
    /// directory names, and performs content-type sniffing as a final fallback.
    /// Returns the best-matching type's icon and color, or null.
    /// </summary>
    public static ProjectTypeResult? Detect(DirectoryInfo directory)
    {
        if (!GlyphShellSettings.ProjectDetectionEnabled)
            return null;

        var key = directory.FullName;

        // Return cached result if still within TTL
        if (_cache.TryGetValue(key, out var cached)
            && (DateTime.UtcNow - cached.CachedAt) < CacheTtl)
        {
            return cached.Result;
        }

        var result = DetectCore(directory);
        _cache[key] = (result, DateTime.UtcNow);
        return result;
    }

    private static ProjectTypeResult? DetectCore(DirectoryInfo directory)
    {

        // Check directory's own name against well-known content directories (used as fallback)
        _dirNameToType.TryGetValue(directory.Name, out var dirNameType);

        string? bestType = null;
        int bestPriority = -1;

        try
        {
            // Content sniffing accumulators
            Dictionary<string, int>? contentCounts = null;
            int totalFiles = 0;

            foreach (var entry in directory.EnumerateFileSystemInfos().Take(MaxEnumerateEntries))
            {
                string? matchedType = null;

                // Check exact filename markers
                if (_markerToType.TryGetValue(entry.Name, out var type))
                {
                    matchedType = type;
                }
                // Check .NET project file extensions
                else if (entry is FileInfo && _dotnetProjectExtensions.Contains(entry.Extension))
                {
                    matchedType = "dotnet";
                }
                // Check for Unity ProjectSettings directory
                else if (entry is DirectoryInfo && entry.Name.Equals("ProjectSettings", StringComparison.OrdinalIgnoreCase))
                {
                    matchedType = "unity";
                }
                // Check for Unreal .uproject files
                else if (entry is FileInfo && entry.Extension.Equals(".uproject", StringComparison.OrdinalIgnoreCase))
                {
                    matchedType = "unreal";
                }

                if (matchedType is not null)
                {
                    var priority = _typePriority.GetValueOrDefault(matchedType, 0);
                    if (priority > bestPriority)
                    {
                        bestType = matchedType;
                        bestPriority = priority;
                    }

                    // Max priority reached — nothing can beat it; skip remaining entries
                    if (bestPriority >= MaxMarkerPriority)
                        break;
                }

                // Content sniffing: accumulate file extension counts (only while no programming marker matched)
                if (bestType is null && entry is FileInfo fi && totalFiles < ContentSniffMaxScan)
                {
                    totalFiles++;
                    if (_extensionToContentType.TryGetValue(fi.Extension, out var contentType))
                    {
                        contentCounts ??= new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                        contentCounts[contentType] = contentCounts.GetValueOrDefault(contentType) + 1;
                    }
                }
            }

            // Programming marker matched — return it immediately
            if (bestType is not null && _typeToDisplay.TryGetValue(bestType, out var progResult))
                return progResult;

            // Directory name match (lower priority than programming markers)
            if (dirNameType is not null && _typeToDisplay.TryGetValue(dirNameType, out var nameResult))
                return nameResult;

            // Content sniffing fallback: ≥60% of files in one category, minimum 3 files
            if (contentCounts is not null && totalFiles >= ContentSniffMinFiles)
            {
                foreach (var kvp in contentCounts)
                {
                    if ((double)kvp.Value / totalFiles >= ContentSniffThreshold
                        && _typeToDisplay.TryGetValue(kvp.Key, out var contentResult))
                    {
                        return contentResult;
                    }
                }
            }
        }
        catch
        {
            // Access denied, etc. — silently skip detection
            return null;
        }

        return null;
    }
}
