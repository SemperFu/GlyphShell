namespace GlyphShell.Themes;

/// <summary>
/// Represents a color theme mapping file types to ANSI escape sequences.
/// </summary>
public sealed class ColorTheme
{
    /// <summary>Theme display name (e.g. "default", "dracula").</summary>
    public required string Name { get; init; }

    /// <summary>Whether this theme was loaded from built-in compiled data.</summary>
    public bool IsBuiltIn { get; init; }

    /// <summary>Maps file extensions (e.g. ".cs") to ANSI foreground color sequences.</summary>
    public Dictionary<string, string> FileExtensions { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Maps well-known directory names to ANSI foreground color sequences.</summary>
    public Dictionary<string, string> DirectoryNames { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Maps exact well-known filenames to ANSI foreground color sequences.</summary>
    public Dictionary<string, string> WellKnownFiles { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Default color for files with no specific mapping.</summary>
    public string DefaultFileColor { get; init; } = "\x1b[0m";

    /// <summary>Default color for directories with no specific mapping.</summary>
    public string DefaultDirectoryColor { get; init; } = "\x1b[38;2;86;156;214m";

    /// <summary>Color for symlink targets.</summary>
    public string SymlinkColor { get; init; } = "\x1b[38;2;115;115;255m";
}
