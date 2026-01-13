using System.Collections.Frozen;

namespace GlyphShell.Engine;

/// <summary>
/// Classifies file extensions into eza-style categories, each with a default ANSI color.
/// Used as a color fallback when no theme color is defined for an extension.
/// </summary>
public static class FileTypeClassifier
{
    private static readonly FrozenDictionary<string, string> _extensionToCategory;
    private static readonly FrozenDictionary<string, string> _categoryToColor;

    static FileTypeClassifier()
    {
        _categoryToColor = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Source"]   = ColorEngine.FromHex("#4EC9B0"),
            ["Markup"]   = ColorEngine.FromHex("#CE9178"),
            ["Style"]    = ColorEngine.FromHex("#C586C0"),
            ["Data"]     = ColorEngine.FromHex("#DCDCAA"),
            ["Config"]   = ColorEngine.FromHex("#808080"),
            ["Document"] = ColorEngine.FromHex("#D4D4D4"),
            ["Image"]    = ColorEngine.FromHex("#D16D9E"),
            ["Video"]    = ColorEngine.FromHex("#B267E6"),
            ["Audio"]    = ColorEngine.FromHex("#CCA8E8"),
            ["Archive"]  = ColorEngine.FromHex("#F44747"),
            ["Binary"]   = ColorEngine.FromHex("#CD3131"),
            ["Temp"]     = ColorEngine.FromHex("#555555"),
            ["Build"]    = ColorEngine.FromHex("#FFD700"),
            ["Crypto"]   = ColorEngine.FromHex("#FF6347"),
            ["Font"]     = ColorEngine.FromHex("#4DC9B0"),
            ["Database"] = ColorEngine.FromHex("#569CD6"),
        }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        AddCategory(map, "Source",
            ".cs", ".js", ".ts", ".py", ".java", ".go", ".rs", ".c", ".cpp", ".h",
            ".rb", ".php", ".swift", ".kt", ".scala", ".lua", ".r", ".m", ".f90",
            ".hs", ".ml", ".ex", ".clj", ".jl", ".v", ".vhd", ".asm");

        AddCategory(map, "Markup",
            ".html", ".htm", ".xml", ".svg", ".xaml", ".jsx", ".tsx", ".vue", ".svelte", ".astro");

        AddCategory(map, "Style",
            ".css", ".scss", ".sass", ".less", ".styl");

        AddCategory(map, "Data",
            ".json", ".jsonc", ".yaml", ".yml", ".toml", ".ini", ".cfg", ".conf",
            ".env", ".properties");

        AddCategory(map, "Config",
            ".gitignore", ".editorconfig", ".eslintrc", ".prettierrc", ".dockerignore");

        AddCategory(map, "Document",
            ".md", ".txt", ".rst", ".tex", ".doc", ".docx", ".pdf", ".rtf", ".odt");

        AddCategory(map, "Image",
            ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".ico", ".webp", ".tiff", ".psd", ".ai");

        AddCategory(map, "Video",
            ".mp4", ".avi", ".mkv", ".mov", ".wmv", ".flv", ".webm");

        AddCategory(map, "Audio",
            ".mp3", ".wav", ".flac", ".ogg", ".aac", ".wma", ".m4a");

        AddCategory(map, "Archive",
            ".zip", ".tar", ".gz", ".bz2", ".xz", ".7z", ".rar", ".zst");

        AddCategory(map, "Binary",
            ".exe", ".dll", ".so", ".dylib", ".obj", ".o", ".lib", ".a", ".wasm");

        AddCategory(map, "Temp",
            ".tmp", ".bak", ".swp", ".swo", ".log", ".cache");

        AddCategory(map, "Build",
            ".sln", ".csproj", ".fsproj", ".vbproj", ".proj", ".targets", ".props",
            ".cmake", ".make", ".ninja");

        AddCategory(map, "Crypto",
            ".pem", ".key", ".crt", ".cer", ".p12", ".pfx", ".pub");

        AddCategory(map, "Font",
            ".ttf", ".otf", ".woff", ".woff2", ".eot");

        AddCategory(map, "Database",
            ".sql", ".db", ".sqlite", ".mdb");

        _extensionToCategory = map.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Returns the eza-style category name for the given extension, or null.</summary>
    public static string? GetCategory(string extension)
    {
        if (string.IsNullOrEmpty(extension)) return null;
        return _extensionToCategory.GetValueOrDefault(extension);
    }

    /// <summary>Returns the ANSI color sequence for the given extension's category, or null.</summary>
    public static string? GetCategoryColor(string extension)
    {
        var category = GetCategory(extension);
        if (category is null) return null;
        return _categoryToColor.GetValueOrDefault(category);
    }

    private static void AddCategory(Dictionary<string, string> map, string category, params string[] extensions)
    {
        foreach (var ext in extensions)
            map[ext] = category;
    }
}
