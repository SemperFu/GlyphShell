using GlyphShell.Data;
using GlyphShell.Engine;

namespace GlyphShell.Themes;

public static class ThemeManager
{
    private static readonly object _lock = new();
    private static readonly Dictionary<string, IconTheme> _iconThemes = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, ColorTheme> _colorThemes = new(StringComparer.OrdinalIgnoreCase);
    private static bool _initialized;

    public static IconTheme CurrentIconTheme { get; private set; } = null!;
    public static ColorTheme CurrentColorTheme { get; private set; } = null!;

    public static void Initialize()
    {
        lock (_lock)
        {
            if (_initialized) return;
            LoadBuiltInThemes();
            _initialized = true;
        }
    }

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

    public static bool SetIconTheme(string name)
    {
        lock (_lock)
        {
            if (_iconThemes.TryGetValue(name, out var theme)) { CurrentIconTheme = theme; return true; }
            return false;
        }
    }

    public static bool SetColorTheme(string name)
    {
        lock (_lock)
        {
            if (_colorThemes.TryGetValue(name, out var theme)) { CurrentColorTheme = theme; return true; }
            return false;
        }
    }

    public static IReadOnlyList<string> GetIconThemeNames()
    {
        lock (_lock) { return [.. _iconThemes.Keys]; }
    }

    public static IReadOnlyList<string> GetColorThemeNames()
    {
        lock (_lock) { return [.. _colorThemes.Keys]; }
    }

    public static IconTheme? GetIconTheme(string name)
    {
        lock (_lock) { return _iconThemes.GetValueOrDefault(name); }
    }

    public static ColorTheme? GetColorTheme(string name)
    {
        lock (_lock) { return _colorThemes.GetValueOrDefault(name); }
    }

    public static bool AddIconTheme(IconTheme theme)
    {
        lock (_lock)
        {
            if (_iconThemes.TryGetValue(theme.Name, out var existing) && existing.IsBuiltIn) return false;
            _iconThemes[theme.Name] = theme;
            return true;
        }
    }

    public static bool AddColorTheme(ColorTheme theme)
    {
        lock (_lock)
        {
            if (_colorThemes.TryGetValue(theme.Name, out var existing) && existing.IsBuiltIn) return false;
            _colorThemes[theme.Name] = theme;
            return true;
        }
    }

    public static bool RemoveIconTheme(string name)
    {
        lock (_lock)
        {
            if (!_iconThemes.TryGetValue(name, out var theme) || theme.IsBuiltIn) return false;
            if (CurrentIconTheme.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                CurrentIconTheme = _iconThemes["default"];
            return _iconThemes.Remove(name);
        }
    }

    public static bool RemoveColorTheme(string name)
    {
        lock (_lock)
        {
            if (!_colorThemes.TryGetValue(name, out var theme) || theme.IsBuiltIn) return false;
            if (CurrentColorTheme.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                CurrentColorTheme = _colorThemes["default"];
            return _colorThemes.Remove(name);
        }
    }
}
