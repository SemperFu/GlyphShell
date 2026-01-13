namespace GlyphShell.Engine;

public static class GlyphShellSettings
{
    public static bool DiagnosticsEnabled { get; set; } = false;
    public static bool DateColorByAge { get; set; } = true;
    public static string DateFlatColor { get; set; } = "\x1b[38;2;86;156;214m";
}
