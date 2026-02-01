using System.Collections.Frozen;
using System.Text;
using GlyphShell.Data;

namespace GlyphShell.Engine;

/// <summary>
/// Static utility for generating and expanding color themes from category-level definitions.
/// Maps file extensions, directory names, and well-known filenames to categories,
/// then generates complete YAML theme files from category colors.
/// </summary>
public static class ThemeExpander
{
    private static readonly FrozenDictionary<string, string> _extensionToCategory;
    private static readonly FrozenDictionary<string, string> _extensionToLang;
    private static readonly FrozenDictionary<string, string> _dirToCategory;
    private static readonly FrozenDictionary<string, string> _wellKnownToCategory;

    static ThemeExpander()
    {
        // ── Extension → Category ────────────────────────────────────────────

        var ext = new Dictionary<string, string>(600, StringComparer.OrdinalIgnoreCase);

        Add(ext, "Source",
            ".cs", ".js", ".ts", ".py", ".java", ".go", ".rs", ".c", ".cpp", ".h",
            ".rb", ".php", ".swift", ".kt", ".scala", ".lua", ".m", ".f90",
            ".hs", ".ml", ".ex", ".clj", ".jl", ".v", ".vhd", ".asm",
            ".cc", ".cxx", ".hxx", ".hpp", ".hh", ".ipp", ".cuh",
            ".fs", ".fsi", ".fsx", ".vb", ".vbs",
            ".pl", ".pm", ".tcl", ".awk", ".d", ".nim", ".zig", ".s",
            ".erl", ".exs", ".eex", ".leex", ".dart", ".gd", ".sc", ".kts", ".groovy",
            ".smali", ".qml", ".qmlc", ".elm", ".cljs", ".cljc",
            ".pyx", ".pyi", ".pyc", ".pyo", ".pyd",
            ".cjs", ".mjs", ".mts", ".cts", ".csx",
            ".ps1", ".psm1", ".psd1", ".ps1xml", ".pssc", ".psc1",
            ".sh", ".bat", ".cmd", ".erb", ".class", ".jar",
            ".sv", ".vhdl", ".f", ".R", ".Rmd", ".Rproj",
            ".applescript", ".luac", ".iLogicVb");

        Add(ext, "Markup",
            ".html", ".htm", ".xml", ".svg", ".xaml", ".jsx", ".tsx", ".vue", ".svelte", ".astro",
            ".xhtml", ".xsl", ".xslt", ".xsd", ".dtd", ".xquery",
            ".mdx", ".hbs", ".cshtml", ".aspx", ".ascx", ".asp");

        Add(ext, "Style",
            ".css", ".scss", ".sass", ".less", ".styl", ".uss", ".uxml");

        Add(ext, "Data",
            ".json", ".jsonc", ".yaml", ".yml", ".toml", ".ini", ".cfg", ".conf", ".env",
            ".properties", ".csv", ".tsv", ".jsonl", ".plist", ".reg", ".proto",
            ".graphql", ".gql", ".pkl", ".msgpack", ".safetensors", ".onnx", ".pb",
            ".data", ".dat", ".npz", ".settings", ".option", ".resjson", ".resource", ".resx");

        Add(ext, "Config",
            ".gitignore", ".editorconfig", ".eslintrc", ".prettierrc", ".dockerignore",
            ".gitattributes", ".gitconfig", ".gitmodules", ".gitkeep",
            ".bowerrc", ".npmrc", ".nvmrc", ".yarnrc", ".stylelintrc",
            ".jscsrc", ".jshintrc", ".jshintignore", ".jsbeautifyrc",
            ".esformatter", ".esmrc", ".firebaserc", ".mrconfig", ".nmpignore",
            ".htaccess", ".buildignore", ".vscodeignore", ".vsixmanifest", ".globalconfig",
            ".claude", ".copilot", ".cursor", ".gemini", ".codex");

        Add(ext, "Document",
            ".md", ".txt", ".rst", ".tex", ".doc", ".docx", ".pdf", ".rtf", ".odt",
            ".markdown", ".adoc", ".latex", ".bib", ".stex", ".pgf",
            ".ppt", ".pptx", ".pptm", ".potx", ".potm", ".pps", ".ppsm", ".ppsx",
            ".xls", ".xlsx", ".xlsm", ".xlt", ".xltx", ".xltm", ".xlm", ".xla", ".xlam", ".xll",
            ".dot", ".dotx", ".docm", ".msg", ".eml", ".man", ".info", ".chm",
            ".wbk", ".wpd", ".wks", ".wp5",
            ".sldm", ".sldx",
            ".vsd", ".vsdx", ".vsdm", ".vss", ".vssm", ".vst", ".vstm", ".vstx",
            ".xps", ".ppa", ".ppam",
            ".accdb", ".accde", ".accdr", ".accdt");

        Add(ext, "Image",
            ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".ico", ".webp", ".tiff", ".psd", ".ai",
            ".tif", ".raw", ".dng", ".exr", ".hdr", ".tga", ".pbm", ".rgb",
            ".fpx", ".gifv", ".bpg", ".jng", ".jxr", ".jb2", ".jbig2",
            ".eps", ".afphoto", ".sketch", ".fig", ".xd", ".dds", ".bmap",
            ".gbr", ".cur", ".psb", ".pxlm", ".wmf");

        Add(ext, "Video",
            ".mp4", ".avi", ".mkv", ".mov", ".wmv", ".flv", ".webm",
            ".mpeg", ".mpg", ".mpe", ".mpv", ".m2v", ".ogv", ".vob",
            ".rmvb", ".rm", ".swf", ".yuv", ".ass", ".srt", ".lrc");

        Add(ext, "Audio",
            ".mp3", ".wav", ".flac", ".ogg", ".aac", ".wma", ".m4a",
            ".mid", ".midi", ".opus", ".aif", ".aiff", ".aifc", ".cda",
            ".mp2", ".adt", ".adts", ".sf2", ".wem", ".bnk", ".fsb");

        Add(ext, "Archive",
            ".zip", ".tar", ".gz", ".bz2", ".xz", ".7z", ".rar", ".zst",
            ".gzip", ".bz", ".brotli", ".br", ".tgz",
            ".cab", ".deb", ".rpm", ".iso", ".img", ".dmg",
            ".vhd", ".vhdx", ".vmdk",
            ".msi", ".msix", ".msixbundle", ".msp", ".mst",
            ".appx", ".AppxBundle",
            ".nupkg", ".snupkg", ".whl", ".pack", ".pak", ".pck", ".apk", ".aar");

        Add(ext, "Binary",
            ".exe", ".dll", ".so", ".dylib", ".obj", ".o", ".lib", ".a", ".wasm",
            ".sys", ".drv", ".ocx", ".cpl", ".scr",
            ".pdb", ".dmp", ".etl", ".evtx", ".winmd", ".mui", ".mum", ".pnf",
            ".nbt", ".ROM", ".node", ".out", ".lnk");

        Add(ext, "Temp",
            ".tmp", ".bak", ".swp", ".swo", ".log", ".cache", ".old", ".DS_Store",
            ".stamp", ".lock", ".ldb", ".ldf");

        Add(ext, "Build",
            ".sln", ".csproj", ".fsproj", ".vbproj", ".proj", ".targets", ".props",
            ".cmake", ".make", ".ninja", ".slnx", ".slnf", ".dbproj", ".sqlproj",
            ".wixproj", ".vcxproj", ".vcxitems", ".nuspec", ".gradle",
            ".mk", ".in", ".ruleset", ".manifest", ".snippet");

        Add(ext, "Crypto",
            ".pem", ".key", ".crt", ".cer", ".p12", ".pfx", ".pub", ".asc", ".gpg",
            ".sig", ".crypt", ".der", ".cert", ".jks", ".p7s", ".ovpn", ".ssh");

        Add(ext, "Font",
            ".ttf", ".otf", ".woff", ".woff2", ".eot", ".ttc", ".fnt", ".fon", ".font", ".odttf");

        Add(ext, "Database",
            ".sql", ".db", ".sqlite", ".mdb", ".mdf", ".ndf", ".psql", ".pgsql", ".pkb", ".pks");

        Add(ext, "3D",
            ".blend", ".fbx", ".glb", ".stl", ".3ds", ".3mf", ".dae", ".iges", ".igs",
            ".step", ".stp", ".dwg", ".dxf", ".mtl", ".model");

        Add(ext, "GameDev",
            ".unity", ".prefab", ".asset", ".assets", ".anim", ".mat", ".meta",
            ".compute", ".shader", ".shadergraph", ".shadersubgraph", ".cginc",
            ".hlsl", ".glsl", ".fx", ".tres", ".tscn", ".xnb");

        Add(ext, "Web",
            ".http", ".url", ".map", ".tsbuildinfo", ".bundle", ".bytes");

        _extensionToCategory = ext.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

        // ── Extension → Language accent key ─────────────────────────────────

        var lang = new Dictionary<string, string>(30, StringComparer.OrdinalIgnoreCase);
        Add(lang, "python", ".py", ".pyc", ".pyo", ".pyd", ".pyi", ".pyx");
        Add(lang, "rust", ".rs");
        Add(lang, "go", ".go");
        Add(lang, "c_cpp", ".c", ".cc", ".cpp", ".cxx", ".h", ".hh", ".hpp", ".hxx", ".ipp");
        Add(lang, "csharp", ".cs", ".csx");
        Add(lang, "java", ".java", ".kt", ".kts", ".jar", ".class");
        Add(lang, "ruby", ".rb", ".erb");
        Add(lang, "powershell", ".ps1", ".psm1", ".psd1");
        _extensionToLang = lang.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

        // ── Directory name → Category ───────────────────────────────────────

        var dir = new Dictionary<string, string>(120, StringComparer.OrdinalIgnoreCase);

        Add(dir, "git", ".git", ".github", ".gitlab");
        Add(dir, "tool", ".vscode", ".vscode-insiders", ".idea", ".idx", ".cursor");
        Add(dir, "lang",
            ".cargo", ".rustup", ".npm", ".nvm", ".pnpm", ".yarn", ".pip", ".nuget",
            ".m2", ".gradle", "node_modules", "bower_components",
            "__pycache__", ".mypy_cache", ".pytest_cache", "vendor");
        Add(dir, "build",
            "bin", "obj", "out", "output", "dist", "target", "build", "artifacts",
            "coverage", "packages");
        Add(dir, "src", "src", "source", "lib", "scripts", "development");
        Add(dir, "doc", "docs", "documents");
        Add(dir, "test", "tests", "test", "samples", "benchmark");
        Add(dir, "infra",
            "deploy", "infra", "k8s", "kubernetes", "helm", "charts",
            "terraform", ".terraform", "puppet", "vagrant", ".vagrant",
            "ansible", ".ansible", ".docker");
        Add(dir, "media",
            "images", "media", "photos", "pictures", "movies", "videos", "music",
            "songs", "fonts", "my games", "saved games", "games");
        Add(dir, "config",
            ".config", ".local", ".ssh", ".aws", ".azure", ".kube", ".helm",
            ".claude", ".copilot", ".codex", ".gemini", ".antigravity",
            ".matplotlib", ".templateengine", ".android", ".dotnet", ".mg");
        Add(dir, "user",
            "desktop", "downloads", "favorites", "links", "searches", "shortcuts",
            "onedrive", "dropbox", "contacts", "applications", "apps", "users", "projects");

        _dirToCategory = dir.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

        // ── Well-known filename → Category ──────────────────────────────────

        var wk = new Dictionary<string, string>(160, StringComparer.OrdinalIgnoreCase);

        Add(wk, "Build",
            ".azure-pipelines.yml", ".gitlab-ci.yml", ".jenkinsfile", ".travis.yml",
            "bitbucket-pipelines.yaml", "bitbucket-pipelines.yml",
            "build.gradle", "build.gradle.kts", "build.sbt", "build.zig", "build.zig.zon",
            "Chart.yaml", "CMakeLists.txt",
            "docker-compose.ci.yml", "docker-compose.dev.yml", "docker-compose.local.yml",
            "docker-compose.override.yml", "docker-compose.prod.yml", "docker-compose.production.yml",
            "docker-compose.staging.yml", "docker-compose.test.yml",
            "docker-compose.yaml", "docker-compose.yml",
            "Dockerfile", "gradlew",
            "gruntfile.js", "gulpfile.babel.js", "gulpfile.js", "gulpfile.ts",
            "helmfile.yaml", "justfile", "Makefile", "Procfile",
            "rollup.config.js", "rollup.config.ts",
            "settings.gradle", "settings.gradle.kts",
            "skaffold.yaml", "Taskfile.yaml", "Taskfile.yml",
            "Tiltfile", "Vagrantfile", "values.yaml", "webpack.config.js");

        Add(wk, "Temp",
            ".terraform.lock.hcl", "Cargo.lock", "cdp.pid", "composer.lock",
            "Gemfile.lock", "mix.lock", "package-lock.json",
            "Pipfile.lock", "poetry.lock", "pubspec.lock");

        Add(wk, "Data",
            "babel.config.js", "bower.json", "Brewfile", "cabal.project",
            "Cargo.toml", "composer.json", "firebase.json", "Gemfile",
            "global.json", "go.mod", "go.sum",
            "manifest.mf", "MANIFEST.in", "Manifest.toml",
            "mix.exs", "nuget.config", "package.json", "Package.swift",
            "Pipfile", "pom.xml", "Project.toml", "pubspec.yaml",
            "pyproject.toml", "renovate.json", "requirements.txt",
            "setup.cfg", "setup.py", "stack.yaml", "tox.ini",
            "tsconfig.json", "tslint.json");

        Add(wk, "Config",
            ".bowerrc", ".buildignore", ".clang-format", ".clang-tidy",
            ".claude.json.backup", ".dockerignore", ".DS_Store",
            ".editorconfig", ".esformatter", ".eslintrc", ".eslintrc.js", ".eslintrc.json",
            ".esmrc", ".firebaserc",
            ".git-for-windows-updater", ".gitattributes", ".gitconfig", ".gitignore",
            ".gitkeep", ".gitmodules",
            ".htaccess", ".jsbeautifyrc", ".jscsrc", ".jshintignore", ".jshintrc",
            ".mrconfig", ".nmpignore", ".node-version", ".npmrc", ".nvmrc",
            ".prettierrc", ".prettierrc.json",
            ".python-version", ".python_history", ".ruby-version",
            ".stylelintrc", ".tool-versions", ".tsbuildinfo", ".yardopts", ".yarnrc",
            "eslint.config.js", "favicon.ico",
            "jest.config.js", "jest.config.ts",
            "tailwind.config.js", "tailwind.config.ts",
            "vite.config.js", "vite.config.ts", "vitest.config.ts",
            "vue.config.js", "vue.config.ts");

        Add(wk, "Document",
            "authors", "authors.md", "authors.txt",
            "CHANGELOG", "CHANGELOG.md", "CHANGELOG.txt",
            "code_of_conduct.md", "code_of_conduct.txt", "CODEOWNERS",
            "CONTRIBUTING", "CONTRIBUTING.md",
            "git-history",
            "LICENSE", "LICENSE.md", "LICENSE.txt",
            "README", "README.md", "README.txt",
            "SECURITY", "SECURITY.md");

        _wellKnownToCategory = wk.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
    }

    // ── Public category lookups ─────────────────────────────────────────────

    /// <summary>
    /// Returns the category for a file extension (e.g. "Source", "Archive", "3D"),
    /// or "Misc" if the extension is not in any explicit category.
    /// </summary>
    public static string GetExtensionCategory(string extension)
    {
        if (string.IsNullOrEmpty(extension)) return "Misc";
        return _extensionToCategory.GetValueOrDefault(extension) ?? "Misc";
    }

    /// <summary>
    /// Returns the language accent key for a file extension (e.g. "python", "csharp"),
    /// or null if no language accent is defined.
    /// </summary>
    public static string? GetLanguageAccent(string extension)
    {
        if (string.IsNullOrEmpty(extension)) return null;
        return _extensionToLang.GetValueOrDefault(extension);
    }

    /// <summary>
    /// Returns the category for a directory name (e.g. "git", "build", "media"),
    /// or "default" if the directory is not in any explicit category.
    /// </summary>
    public static string GetDirectoryCategory(string name)
    {
        if (string.IsNullOrEmpty(name)) return "default";
        return _dirToCategory.GetValueOrDefault(name) ?? "default";
    }

    /// <summary>
    /// Returns the category for a well-known filename (e.g. "Build", "Temp", "Data",
    /// "Config", or "Document"), or "Config" for unrecognized well-known files.
    /// </summary>
    public static string GetWellKnownCategory(string name)
    {
        if (string.IsNullOrEmpty(name)) return "Config";
        return _wellKnownToCategory.GetValueOrDefault(name) ?? "Config";
    }

    // ── Public theme generation ─────────────────────────────────────────────

    /// <summary>
    /// Generates a complete color theme YAML string from category colors.
    /// Iterates all extensions, directories, and well-known files from
    /// <see cref="DefaultColorTheme"/> and assigns colors based on category membership.
    /// </summary>
    /// <param name="name">Theme name (e.g. "mytheme-colors").</param>
    /// <param name="categoryColors">Map of category name → hex color (e.g. "Source" → "#8BE9FD").</param>
    /// <param name="langAccents">Optional per-language accent colors (e.g. "python" → "#50FA7B").</param>
    /// <param name="dirCategoryColors">Optional map of directory category → hex color.</param>
    /// <param name="defaultFile">Default file color hex.</param>
    /// <param name="defaultDir">Default directory color hex.</param>
    /// <param name="symlink">Symlink color hex.</param>
    /// <returns>Complete YAML string ready to write to file.</returns>
    public static string GenerateColorTheme(
        string name,
        Dictionary<string, string> categoryColors,
        Dictionary<string, string>? langAccents,
        Dictionary<string, string>? dirCategoryColors,
        string defaultFile,
        string defaultDir,
        string symlink)
    {
        var sb = new StringBuilder(16384);

        sb.AppendLine($"name: {name}");
        sb.AppendLine("type: color");

        // ── Extensions ──
        sb.AppendLine("extensions:");
        foreach (var ext in DefaultColorTheme.FileExtensions.Keys
            .OrderBy(k => k, StringComparer.OrdinalIgnoreCase))
        {
            var color = ResolveExtensionColor(ext, categoryColors, langAccents, defaultFile);
            sb.AppendLine($"  {ext}: \"{color}\"");
        }

        // ── Directories ──
        sb.AppendLine("directories:");
        foreach (var dir in DefaultColorTheme.DirectoryNames.Keys
            .OrderBy(k => k, StringComparer.OrdinalIgnoreCase))
        {
            var category = GetDirectoryCategory(dir);
            var color = (dirCategoryColors != null && dirCategoryColors.TryGetValue(category, out var c))
                ? c : defaultDir;
            sb.AppendLine($"  {dir}: \"{color}\"");
        }

        // ── Well-known files ──
        sb.AppendLine("wellknown:");
        foreach (var wk in DefaultColorTheme.WellKnownFiles.Keys
            .OrderBy(k => k, StringComparer.OrdinalIgnoreCase))
        {
            var category = GetWellKnownCategory(wk);
            var color = categoryColors.TryGetValue(category, out var c) ? c : defaultFile;
            sb.AppendLine($"  {wk}: \"{color}\"");
        }

        // ── Defaults ──
        sb.AppendLine("defaults:");
        sb.AppendLine($"  file: \"{defaultFile}\"");
        sb.AppendLine($"  directory: \"{defaultDir}\"");
        sb.Append($"  symlink: \"{symlink}\"");

        return sb.ToString();
    }

    /// <summary>
    /// Infers category-level colors from a sparse extension→color map.
    /// Groups known extensions by category and picks the most common color per category.
    /// </summary>
    /// <param name="sparseExtensionColors">Sparse map of file extension → hex color.</param>
    /// <returns>Dictionary mapping category names to the most-voted hex color.</returns>
    public static Dictionary<string, string> InferCategoriesFromSparse(
        Dictionary<string, string> sparseExtensionColors)
    {
        var categoryVotes = new Dictionary<string, Dictionary<string, int>>(StringComparer.OrdinalIgnoreCase);

        foreach (var (ext, color) in sparseExtensionColors)
        {
            var category = GetExtensionCategory(ext);

            if (!categoryVotes.TryGetValue(category, out var votes))
            {
                votes = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                categoryVotes[category] = votes;
            }

            votes.TryGetValue(color, out var count);
            votes[color] = count + 1;
        }

        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (category, votes) in categoryVotes)
        {
            var winner = votes.MaxBy(kv => kv.Value);
            result[category] = winner.Key;
        }

        return result;
    }

    /// <summary>
    /// Expands a sparse theme (e.g. from Terminal-Icons import) into a full theme.
    /// Combines <see cref="InferCategoriesFromSparse"/> with <see cref="GenerateColorTheme"/>.
    /// </summary>
    /// <param name="name">Theme name for the generated YAML.</param>
    /// <param name="sparseExtensionColors">Sparse map of file extension → hex color.</param>
    /// <param name="defaultFile">Default file color hex, or null for "#F8F8F2".</param>
    /// <param name="defaultDir">Default directory color hex, or null for "#569CD6".</param>
    /// <returns>Complete YAML theme string.</returns>
    public static string ExpandSparseTheme(
        string name,
        Dictionary<string, string> sparseExtensionColors,
        string? defaultFile = null,
        string? defaultDir = null)
    {
        var categoryColors = InferCategoriesFromSparse(sparseExtensionColors);

        return GenerateColorTheme(
            name,
            categoryColors,
            langAccents: null,
            dirCategoryColors: null,
            defaultFile ?? "#F8F8F2",
            defaultDir ?? "#569CD6",
            symlink: "#7373FF");
    }

    // ── Private helpers ─────────────────────────────────────────────────────

    private static string ResolveExtensionColor(
        string ext,
        Dictionary<string, string> categoryColors,
        Dictionary<string, string>? langAccents,
        string defaultFile)
    {
        if (langAccents != null
            && _extensionToLang.TryGetValue(ext, out var lang)
            && langAccents.TryGetValue(lang, out var accentColor))
        {
            return accentColor;
        }

        var category = _extensionToCategory.GetValueOrDefault(ext) ?? "Misc";
        if (categoryColors.TryGetValue(category, out var catColor))
        {
            return catColor;
        }

        return defaultFile;
    }

    private static void Add(Dictionary<string, string> map, string value, params string[] keys)
    {
        foreach (var key in keys)
            map[key] = value;
    }
}
