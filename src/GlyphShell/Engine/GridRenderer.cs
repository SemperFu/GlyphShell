using System.IO;
using System.Management.Automation.Host;

namespace GlyphShell.Engine;

/// <summary>
/// Renders file system entries in a grid layout, respecting terminal width.
/// Each cell shows: [icon] [name] with appropriate padding.
/// </summary>
public class GridRenderer
{
    private readonly IconResolver _resolver;

    public GridRenderer(IconResolver resolver)
    {
        _resolver = resolver;
    }

    /// <summary>
    /// Renders items in a grid, returning lines of ANSI-colored text.
    /// </summary>
    public IEnumerable<string> Render(IReadOnlyList<FileSystemInfo> items, int terminalWidth)
    {
        if (items.Count == 0) yield break;

        // Calculate the visible width of each entry (icon + space + name)
        var entries = new List<(string display, int visibleWidth, FileSystemInfo item)>();
        foreach (var item in items)
        {
            var resolved = _resolver.Resolve(item);
            string display;
            int visibleWidth;

            if (resolved.Glyph is not null)
            {
                display = $"{resolved.ColorSequence}{resolved.Glyph}  {item.Name}{ColorEngine.Reset}";
                visibleWidth = 3 + item.Name.Length; // glyph(1-2) + spaces(2) + name
            }
            else
            {
                display = $"{resolved.ColorSequence}{item.Name}{ColorEngine.Reset}";
                visibleWidth = item.Name.Length;
            }
            entries.Add((display, visibleWidth, item));
        }

        // Find optimal column count
        int maxNameWidth = entries.Max(e => e.visibleWidth);
        int columnWidth = maxNameWidth + 2; // 2 chars padding between columns
        int columns = Math.Max(1, terminalWidth / columnWidth);

        // Render rows
        int rows = (entries.Count + columns - 1) / columns;
        for (int row = 0; row < rows; row++)
        {
            var sb = new System.Text.StringBuilder();
            for (int col = 0; col < columns; col++)
            {
                int idx = row + col * rows; // column-major order (like ls)
                if (idx >= entries.Count) break;

                var entry = entries[idx];
                sb.Append(entry.display);

                // Pad to column width (only if not last column)
                if (col < columns - 1)
                {
                    int padding = columnWidth - entry.visibleWidth;
                    if (padding > 0) sb.Append(' ', padding);
                }
            }
            yield return sb.ToString();
        }
    }
}
