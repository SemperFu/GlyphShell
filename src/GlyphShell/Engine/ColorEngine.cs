using System.Globalization;

namespace GlyphShell.Engine;

/// <summary>
/// Generates ANSI escape sequences for coloring glyphs and filenames.
/// </summary>
public static class ColorEngine
{
    /// <summary>ANSI reset sequence.</summary>
    public const string Reset = "\x1b[0m";

    /// <summary>Produces a 24-bit true-color foreground escape sequence.</summary>
    public static string Foreground(byte r, byte g, byte b) => $"\x1b[38;2;{r};{g};{b}m";

    /// <summary>Produces a 256-color foreground escape sequence.</summary>
    public static string Foreground256(byte color) => $"\x1b[38;5;{color}m";

    /// <summary>
    /// Converts a hex RGB string (e.g. "FF5733") to an ANSI 24-bit foreground escape sequence.
    /// </summary>
    public static string FromHex(string hex)
    {
        ReadOnlySpan<char> span = hex.AsSpan();
        if (span.Length > 0 && span[0] == '#')
            span = span[1..];

        byte r = byte.Parse(span[..2], NumberStyles.HexNumber);
        byte g = byte.Parse(span[2..4], NumberStyles.HexNumber);
        byte b = byte.Parse(span[4..6], NumberStyles.HexNumber);
        return Foreground(r, g, b);
    }
}
