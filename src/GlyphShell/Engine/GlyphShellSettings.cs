namespace GlyphShell.Engine;

/// <summary>
/// Runtime settings for GlyphShell. Toggle via Set-GlyphShellOption.
/// </summary>
public static class GlyphShellSettings
{
    /// <summary>When true, resolver errors are emitted as PowerShell warnings.</summary>
    public static bool DiagnosticsEnabled { get; set; } = false;

    /// <summary>When true, dates are colored by age (green→yellow→gray). When false, uses a flat color.</summary>
    public static bool DateColorByAge { get; set; } = true;

    /// <summary>Flat date color when DateColorByAge is false. ANSI escape sequence.</summary>
    public static string DateFlatColor { get; set; } = "\x1b[38;2;86;156;214m"; // blue (eza-style)

    /// <summary>When true, directories are scanned for marker files to detect project type. Default: true.</summary>
    public static bool ProjectDetectionEnabled { get; set; } = true;

    /// <summary>When true, git status indicators are shown in the Git column. Default: false (performance).</summary>
    public static bool GitStatusEnabled { get; set; } = false;

    /// <summary>When true, custom ScriptBlock plugins can be registered. Default: false (security).</summary>
    public static bool PluginsEnabled { get; set; } = false;

    /// <summary>When true, project badges replace the folder icon instead of showing in a separate column. Default: true.</summary>
    public static bool BadgeMerge { get; set; } = true;
}
