using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GlyphShell.Engine;

namespace GlyphShell.Plugins;

/// <summary>
/// Built-in plugin that appends file metadata (line count, type category) after filenames.
/// Opt-in via <c>Enable-GlyphShellPlugin -Name FilePreview</c>.
/// </summary>
public static class FilePreviewPlugin
{
    private const long MaxSizeBytes = 10 * 1024 * 1024; // 10 MB

    private static readonly HashSet<string> TextExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".cs", ".js", ".ts", ".jsx", ".tsx", ".py", ".md", ".txt", ".json", ".yaml", ".yml",
        ".xml", ".html", ".htm", ".css", ".scss", ".less", ".sh", ".bash", ".ps1", ".psm1",
        ".psd1", ".bat", ".cmd", ".c", ".cpp", ".h", ".hpp", ".java", ".go", ".rs", ".rb",
        ".php", ".swift", ".kt", ".lua", ".r", ".sql", ".toml", ".ini", ".cfg", ".conf",
        ".env", ".csv", ".log", ".gitignore", ".editorconfig", ".dockerfile", ".razor",
        ".csproj", ".sln", ".fsproj", ".vbproj", ".props", ".targets"
    };

    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".webp", ".ico", ".svg", ".tiff", ".tif"
    };

    private static readonly HashSet<string> ArchiveExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".zip", ".tar", ".gz", ".tgz", ".7z", ".rar", ".bz2", ".xz", ".cab", ".nupkg", ".whl"
    };

    /// <summary>Resolves file preview metadata for the given file system entry.</summary>
    public static PluginResult? Resolve(FileSystemInfo item)
    {
        if (item is DirectoryInfo)
            return null;

        if (item is not FileInfo fi)
            return null;

        var ext = fi.Extension;

        if (ArchiveExtensions.Contains(ext))
            return new PluginResult(null, null, "(archive)");

        if (ImageExtensions.Contains(ext))
            return new PluginResult(null, null, "(img)");

        if (TextExtensions.Contains(ext))
        {
            try
            {
                if (fi.Length > MaxSizeBytes)
                    return null;

                int count = File.ReadLines(fi.FullName).Count();
                string label = count == 1 ? "1 line" : $"{count} lines";
                return new PluginResult(null, null, $"({label})");
            }
            catch
            {
                return null;
            }
        }

        return null;
    }
}
