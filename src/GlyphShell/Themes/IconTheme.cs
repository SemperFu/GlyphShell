namespace GlyphShell.Themes;

/// <summary>
/// Represents an icon theme mapping file types to Nerd Font glyphs.
/// </summary>
public sealed class IconTheme
{
    /// <summary>Theme display name (e.g. "default", "dracula").</summary>
    public required string Name { get; init; }

    /// <summary>Whether this theme was loaded from built-in compiled data.</summary>
    public bool IsBuiltIn { get; init; }

    /// <summary>Maps file extensions (e.g. ".cs") to Nerd Font glyph names.</summary>
    public Dictionary<string, string> FileExtensions { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Maps well-known directory names to Nerd Font glyph names.</summary>
    public Dictionary<string, string> DirectoryNames { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Maps exact well-known filenames to Nerd Font glyph names.</summary>
    public Dictionary<string, string> WellKnownFiles { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Default icon for files with no specific mapping.</summary>
    public string DefaultFileIcon { get; init; } = "nf-fa-file";

    /// <summary>Default icon for directories with no specific mapping.</summary>
    public string DefaultDirectoryIcon { get; init; } = "nf-oct-file_directory";

    /// <summary>Icon for symlinked files.</summary>
    public string FileSymlinkIcon { get; init; } = "nf-oct-file_symlink_file";

    /// <summary>Icon for symlinked directories.</summary>
    public string DirSymlinkIcon { get; init; } = "nf-cod-file_symlink_directory";

    /// <summary>Icon for file junctions.</summary>
    public string FileJunctionIcon { get; init; } = "nf-fa-external_link";

    /// <summary>Icon for directory junctions.</summary>
    public string DirJunctionIcon { get; init; } = "nf-fa-external_link";
}
