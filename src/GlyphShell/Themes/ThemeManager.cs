using System.IO;
using System.Text.RegularExpressions;
using GlyphShell.Data;
using GlyphShell.Engine;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace GlyphShell.Themes;

/// <summary>
/// Manages loading, caching, and switching between icon and color themes.
/// Thread-safe singleton — call <see cref="Initialize"/> once at module load.
/// </summary>
public static class ThemeManager
{
    private static readonly object _lock = new();
    private static readonly Dictionary<string, IconTheme> _iconThemes = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, ColorTheme> _colorThemes = new(StringComparer.OrdinalIgnoreCase);
    private static bool _initialized;

    /// <summary>Currently active icon theme.</summary>
    public static IconTheme CurrentIconTheme { get; private set; } = null!;

    /// <summary>Currently active color theme.</summary>
    public static ColorTheme CurrentColorTheme { get; private set; } = null!;

    /// <summary>
    /// User themes directory (~/.config/GlyphShell/themes/).
    /// </summary>
    public static string UserThemesDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".config", "GlyphShell", "themes");

    /// <summary>
    /// Initializes the theme system with built-in defaults.
    /// Safe to call multiple times — subsequent calls are no-ops.
    /// </summary>
    public static void Initialize()
    {
        lock (_lock)
        {
            if (_initialized) return;
            LoadBuiltInThemes();
            _initialized = true;
        }
    }

    /// <summary>Creates icon and color themes from the compiled DefaultIconTheme/DefaultColorTheme data.</summary>
    private static void LoadBuiltInThemes()
    {
        var builtInIconTheme = new IconTheme
        {
            Name = "default",
            IsBuiltIn = true,
            FileExtensions = new Dictionary<string, string>(DefaultIconTheme.FileExtensions, StringComparer.OrdinalIgnoreCase),
            DirectoryNames = new Dictionary<string, string>(DefaultIconTheme.DirectoryNames, StringComparer.OrdinalIgnoreCase),
            WellKnownFiles = new Dictionary<string, string>(DefaultIconTheme.WellKnownFiles, StringComparer.OrdinalIgnoreCase),
            DefaultFileIcon = DefaultIconTheme.DefaultFileIcon,
            DefaultDirectoryIcon = DefaultIconTheme.DefaultDirectoryIcon,
            FileSymlinkIcon = DefaultIconTheme.FileSymlinkIcon,
            DirSymlinkIcon = DefaultIconTheme.DirSymlinkIcon,
            FileJunctionIcon = DefaultIconTheme.FileJunctionIcon,
            DirJunctionIcon = DefaultIconTheme.DirJunctionIcon,
        };

        var builtInColorTheme = new ColorTheme
        {
            Name = "default",
            IsBuiltIn = true,
            FileExtensions = new Dictionary<string, string>(DefaultColorTheme.FileExtensions, StringComparer.OrdinalIgnoreCase),
            DirectoryNames = new Dictionary<string, string>(DefaultColorTheme.DirectoryNames, StringComparer.OrdinalIgnoreCase),
            WellKnownFiles = new Dictionary<string, string>(DefaultColorTheme.WellKnownFiles, StringComparer.OrdinalIgnoreCase),
            DefaultFileColor = DefaultColorTheme.DefaultFileColor,
            DefaultDirectoryColor = DefaultColorTheme.DefaultDirectoryColor,
            SymlinkColor = DefaultColorTheme.SymlinkColor,
        };

        _iconThemes["default"] = builtInIconTheme;
        _colorThemes["default"] = builtInColorTheme;
        CurrentIconTheme = builtInIconTheme;
        CurrentColorTheme = builtInColorTheme;
    }

    /// <summary>Sets the active icon theme by name. Returns true on success.</summary>
    public static bool SetIconTheme(string name)
    {
        lock (_lock)
        {
            if (_iconThemes.TryGetValue(name, out var theme))
            {
                CurrentIconTheme = theme;
                return true;
            }
            return false;
        }
    }

    /// <summary>Sets the active color theme by name. Returns true on success.</summary>
    public static bool SetColorTheme(string name)
    {
        lock (_lock)
        {
            if (_colorThemes.TryGetValue(name, out var theme))
            {
                CurrentColorTheme = theme;
                return true;
            }
            return false;
        }
    }

    /// <summary>Returns all registered icon theme names.</summary>
    public static IReadOnlyList<string> GetIconThemeNames()
    {
        lock (_lock) { return [.. _iconThemes.Keys]; }
    }

    /// <summary>Returns all registered color theme names.</summary>
    public static IReadOnlyList<string> GetColorThemeNames()
    {
        lock (_lock) { return [.. _colorThemes.Keys]; }
    }

    /// <summary>Returns a registered icon theme by name, or null.</summary>
    public static IconTheme? GetIconTheme(string name)
    {
        lock (_lock) { return _iconThemes.GetValueOrDefault(name); }
    }

    /// <summary>Returns a registered color theme by name, or null.</summary>
    public static ColorTheme? GetColorTheme(string name)
    {
        lock (_lock) { return _colorThemes.GetValueOrDefault(name); }
    }

    /// <summary>
    /// Registers an icon theme. Overwrites if a theme with the same name exists (unless built-in).
    /// Returns true on success, false if trying to overwrite a built-in theme.
    /// </summary>
    public static bool AddIconTheme(IconTheme theme)
    {
        lock (_lock)
        {
            if (_iconThemes.TryGetValue(theme.Name, out var existing) && existing.IsBuiltIn)
                return false;
            _iconThemes[theme.Name] = theme;
            return true;
        }
    }

    /// <summary>
    /// Registers a color theme. Overwrites if a theme with the same name exists (unless built-in).
    /// Returns true on success, false if trying to overwrite a built-in theme.
    /// </summary>
    public static bool AddColorTheme(ColorTheme theme)
    {
        lock (_lock)
        {
            if (_colorThemes.TryGetValue(theme.Name, out var existing) && existing.IsBuiltIn)
                return false;
            _colorThemes[theme.Name] = theme;
            return true;
        }
    }

    /// <summary>Removes a user theme by name. Built-in themes cannot be removed.</summary>
    public static bool RemoveIconTheme(string name)
    {
        lock (_lock)
        {
            if (!_iconThemes.TryGetValue(name, out var theme) || theme.IsBuiltIn)
                return false;
            if (CurrentIconTheme.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                CurrentIconTheme = _iconThemes["default"];
            return _iconThemes.Remove(name);
        }
    }

    /// <summary>Removes a user color theme by name. Built-in themes cannot be removed.</summary>
    public static bool RemoveColorTheme(string name)
    {
        lock (_lock)
        {
            if (!_colorThemes.TryGetValue(name, out var theme) || theme.IsBuiltIn)
                return false;
            if (CurrentColorTheme.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                CurrentColorTheme = _colorThemes["default"];
            return _colorThemes.Remove(name);
        }
    }

    // ───────────────────────── YAML theme loading ─────────────────────────

    /// <summary>YAML deserialization model for theme files.</summary>
    private sealed class YamlThemeFile
    {
        public string Name { get; set; } = "";
        public string Type { get; set; } = ""; // "icon" or "color"
        public Dictionary<string, string>? Extensions { get; set; }
        public Dictionary<string, string>? Directories { get; set; }
        public Dictionary<string, string>? Wellknown { get; set; }
        public YamlDefaults? Defaults { get; set; }
    }

    private sealed class YamlDefaults
    {
        public string? File { get; set; }
        public string? Directory { get; set; }
        public string? Symlink { get; set; }
    }

    /// <summary>
    /// Loads a YAML theme file and registers it. Returns the theme name, or null on failure.
    /// Supports both "icon" and "color" type themes.
    /// </summary>
    public static string? LoadYamlTheme(string path)
    {
        if (!File.Exists(path)) return null;

        var yaml = File.ReadAllText(path);
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        YamlThemeFile themeFile;
        try { themeFile = deserializer.Deserialize<YamlThemeFile>(yaml); }
        catch { return null; }

        if (string.IsNullOrWhiteSpace(themeFile.Name)) return null;

        if (themeFile.Type.Equals("color", StringComparison.OrdinalIgnoreCase))
        {
            var colorTheme = new ColorTheme
            {
                Name = themeFile.Name,
                FileExtensions = BuildCaseInsensitiveColorDict(themeFile.Extensions),
                DirectoryNames = BuildCaseInsensitiveColorDict(themeFile.Directories),
                WellKnownFiles = BuildCaseInsensitiveColorDict(themeFile.Wellknown),
                DefaultFileColor = themeFile.Defaults?.File is string f ? ResolveColor(f) : "\x1b[0m",
                DefaultDirectoryColor = themeFile.Defaults?.Directory is string d ? ResolveColor(d) : "\x1b[38;2;86;156;214m",
                SymlinkColor = themeFile.Defaults?.Symlink is string s ? ResolveColor(s) : "\x1b[38;2;115;115;255m",
            };
            AddColorTheme(colorTheme);
            return themeFile.Name;
        }
        else // default: icon
        {
            var iconTheme = new IconTheme
            {
                Name = themeFile.Name,
                FileExtensions = BuildCaseInsensitiveDict(themeFile.Extensions),
                DirectoryNames = BuildCaseInsensitiveDict(themeFile.Directories),
                WellKnownFiles = BuildCaseInsensitiveDict(themeFile.Wellknown),
                DefaultFileIcon = themeFile.Defaults?.File ?? "nf-fa-file",
                DefaultDirectoryIcon = themeFile.Defaults?.Directory ?? "nf-oct-file_directory",
            };
            AddIconTheme(iconTheme);
            return themeFile.Name;
        }
    }

    // ───────────────────────── Terminal-Icons .psd1 migration ─────────────────────────

    /// <summary>
    /// Standard Terminal-Icons theme directory.
    /// </summary>
    public static string TerminalIconsThemeDir { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        "Documents", "PowerShell", "Modules", "Terminal-Icons");

    /// <summary>
    /// Attempts to parse a Terminal-Icons .psd1 theme file.
    /// Returns the theme name on success, or null.
    /// This performs a best-effort regex-based parse of PowerShell data files.
    /// </summary>
    public static string? LoadPsd1Theme(string path)
    {
        if (!File.Exists(path)) return null;

        var content = File.ReadAllText(path);
        var themeName = Path.GetFileNameWithoutExtension(path);

        // Determine if this is an icon or color theme based on content/path heuristics
        bool isColorTheme = path.Contains("Color", StringComparison.OrdinalIgnoreCase)
                         || content.Contains("38;2;", StringComparison.Ordinal)
                         || content.Contains("38;5;", StringComparison.Ordinal);

        var mappings = ParsePsd1Hashtable(content);
        if (mappings.Count == 0) return null;

        // .psd1 themes often have nested sections like Types.Known, Types.Extensions
        var extensions = ExtractSection(mappings, "Extensions");
        var directories = ExtractSection(mappings, "Directories");
        var wellKnown = ExtractSection(mappings, "WellKnown");

        // If no sections found, treat flat mappings as extensions
        if (extensions.Count == 0 && directories.Count == 0 && wellKnown.Count == 0)
            extensions = mappings;

        if (isColorTheme)
        {
            var colorTheme = new ColorTheme
            {
                Name = themeName,
                FileExtensions = extensions,
                DirectoryNames = directories,
                WellKnownFiles = wellKnown,
            };
            AddColorTheme(colorTheme);
        }
        else
        {
            var iconTheme = new IconTheme
            {
                Name = themeName,
                FileExtensions = extensions,
                DirectoryNames = directories,
                WellKnownFiles = wellKnown,
            };
            AddIconTheme(iconTheme);
        }

        return themeName;
    }

    /// <summary>Best-effort parse of PowerShell hashtable key-value pairs from .psd1 content.</summary>
    private static Dictionary<string, string> ParsePsd1Hashtable(string content)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        // Match patterns like: '.cs' = 'nf-md-language_csharp' or ".cs" = "value"
        var regex = new Regex(
            @"['""](?<key>[^'""]+)['""][\s]*=[\s]*['""](?<val>[^'""]+)['""]",
            RegexOptions.Compiled);

        foreach (Match m in regex.Matches(content))
        {
            result[m.Groups["key"].Value] = m.Groups["val"].Value;
        }
        return result;
    }

    /// <summary>Extracts a named section from parsed .psd1 mappings (simple heuristic).</summary>
    private static Dictionary<string, string> ExtractSection(
        Dictionary<string, string> all, string sectionName)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var prefix = sectionName + ".";
        foreach (var (key, value) in all)
        {
            if (key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                result[key[prefix.Length..]] = value;
        }
        return result;
    }

    /// <summary>
    /// Resolves a color string to an ANSI escape sequence.
    /// Accepts hex (#FF5733), existing ANSI sequences, or named formats.
    /// </summary>
    private static string ResolveColor(string color)
    {
        if (string.IsNullOrWhiteSpace(color)) return "\x1b[0m";
        if (color.StartsWith("\x1b[", StringComparison.Ordinal)) return color;
        if (color.StartsWith('#') || (color.Length == 6 && color.All(c => char.IsAsciiHexDigit(c))))
            return ColorEngine.FromHex(color);
        return color;
    }

    private static Dictionary<string, string> BuildCaseInsensitiveDict(Dictionary<string, string>? source)
    {
        if (source is null || source.Count == 0)
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var result = new Dictionary<string, string>(source.Count, StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in source)
            result.TryAdd(key, value); // silently skip case-insensitive duplicates
        return result;
    }

    private static Dictionary<string, string> BuildCaseInsensitiveColorDict(Dictionary<string, string>? source)
    {
        if (source is null || source.Count == 0)
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var result = new Dictionary<string, string>(source.Count, StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in source)
            result.TryAdd(key, ResolveColor(value));
        return result;
    }

    /// <summary>
    /// Scans a directory for .yaml/.yml theme files and loads them all.
    /// Returns the number of themes loaded.
    /// </summary>
    public static int LoadThemesFromDirectory(string directory)
    {
        if (!Directory.Exists(directory)) return 0;

        int count = 0;
        foreach (var file in Directory.EnumerateFiles(directory, "*.yaml")
            .Concat(Directory.EnumerateFiles(directory, "*.yml")))
        {
            if (LoadYamlTheme(file) is not null) count++;
        }
        return count;
    }
}
