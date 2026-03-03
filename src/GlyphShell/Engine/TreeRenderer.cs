using System.IO;
using System.Security;

namespace GlyphShell.Engine;

/// <summary>Result of rendering a directory tree.</summary>
public record struct TreeResult(IReadOnlyList<string> Lines, int DirectoryCount, int FileCount, long TotalBytes);

/// <summary>
/// Renders a directory tree with Nerd Font icons, ANSI colors, and Unicode box-drawing lines.
/// </summary>
public class TreeRenderer
{
    private const string Branch = "├── ";
    private const string LastBranch = "└── ";
    private const string Pipe = "│   ";
    private const string Space = "    ";

    private static readonly string DimColor = "\x1b[38;2;140;140;140m";
    private static readonly string SizeColor = "\x1b[38;2;100;160;200m";
    private static readonly string CountDirColor = "\x1b[38;2;100;200;100m";
    private static readonly string CountFileColor = "\x1b[38;2;100;160;220m";
    private static readonly string TotalSizeColor = "\x1b[38;2;220;180;80m";

    private readonly IconResolver _resolver;
    private readonly HashSet<string> _visitedPaths = new(StringComparer.OrdinalIgnoreCase);

    public TreeRenderer(IconResolver resolver)
    {
        _resolver = resolver;
    }

    /// <summary>Renders a full directory tree from <paramref name="rootPath"/>.</summary>
    /// <param name="writeLine">When provided, lines stream immediately to the caller instead of buffering.</param>
    public TreeResult RenderTree(
        string rootPath,
        int maxDepth = int.MaxValue,
        bool showHidden = false,
        bool directoriesFirst = true,
        bool showSize = false,
        Action<string>? writeLine = null)
    {
        _visitedPaths.Clear();

        int dirCount = 0;
        int fileCount = 0;

        var rootDir = new DirectoryInfo(rootPath);
        if (!rootDir.Exists)
            return new TreeResult(Array.Empty<string>(), 0, 0, 0);

        TrackVisited(rootDir);

        // Emit root line immediately (no size — total shown in footer)
        var rootIcon = _resolver.Resolve(rootDir);
        string rootLine = FormatEntry(rootIcon, rootDir.Name, rootDir.LinkTarget);

        List<string>? lines = writeLine is null ? new List<string>() : null;
        Action<string> emit = writeLine ?? (line => lines!.Add(line));

        emit(rootLine);

        long totalBytes = RenderChildren(rootDir, "", maxDepth, 0, showHidden, directoriesFirst, showSize, emit, ref dirCount, ref fileCount);

        return new TreeResult(
            (IReadOnlyList<string>?)lines?.AsReadOnly() ?? Array.Empty<string>(),
            dirCount, fileCount, totalBytes);
    }

    /// <summary>Returns total bytes of all entries rendered under this directory.</summary>
    private long RenderChildren(
        DirectoryInfo parent,
        string indent,
        int maxDepth,
        int currentDepth,
        bool showHidden,
        bool directoriesFirst,
        bool showSize,
        Action<string> emit,
        ref int dirCount,
        ref int fileCount)
    {
        if (currentDepth >= maxDepth)
            return 0;

        FileSystemInfo[] entries;
        try
        {
            entries = GetSortedEntries(parent, showHidden, directoriesFirst);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or SecurityException or IOException)
        {
            return 0;
        }

        long subtotal = 0;

        for (int i = 0; i < entries.Length; i++)
        {
            bool isLast = i == entries.Length - 1;
            string connector = isLast ? LastBranch : Branch;
            string childIndent = indent + (isLast ? Space : Pipe);
            var entry = entries[i];

            var resolved = _resolver.Resolve(entry);
            string line = indent + connector + FormatEntry(resolved, entry.Name, entry.LinkTarget);

            if (entry is DirectoryInfo dir)
            {
                dirCount++;

                if (showSize)
                {
                    long dirSize = ComputeDirectorySize(dir, showHidden);
                    line += FormatSize(dirSize);
                    subtotal += dirSize;
                }

                emit(line);

                if (dir.LinkTarget is not null)
                {
                    if (!TrackVisited(dir))
                        continue;
                }
                else
                {
                    TrackVisited(dir);
                }

                // Recurse (sizes already counted via ComputeDirectorySize, don't double-count)
                RenderChildren(dir, childIndent, maxDepth, currentDepth + 1, showHidden, directoriesFirst, showSize, emit, ref dirCount, ref fileCount);
            }
            else
            {
                fileCount++;

                if (showSize && entry is FileInfo fi)
                {
                    try
                    {
                        long fileSize = fi.Length;
                        line += FormatSize(fileSize);
                        subtotal += fileSize;
                    }
                    catch { /* inaccessible file */ }
                }

                emit(line);
            }
        }

        return subtotal;
    }

    private static FileSystemInfo[] GetSortedEntries(DirectoryInfo dir, bool showHidden, bool directoriesFirst)
    {
        var enumOptions = new EnumerationOptions
        {
            IgnoreInaccessible = true,
            AttributesToSkip = showHidden ? 0 : FileAttributes.Hidden
        };

        var entries = dir.EnumerateFileSystemInfos("*", enumOptions).ToList();

        if (directoriesFirst)
        {
            entries.Sort((a, b) =>
            {
                bool aDir = a is DirectoryInfo;
                bool bDir = b is DirectoryInfo;
                if (aDir != bDir) return aDir ? -1 : 1;
                return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
            });
        }
        else
        {
            entries.Sort((a, b) =>
            {
                bool aDir = a is DirectoryInfo;
                bool bDir = b is DirectoryInfo;
                if (aDir != bDir) return aDir ? 1 : -1;
                return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
            });
        }

        return entries.ToArray();
    }

    private static string FormatEntry(ResolvedIcon resolved, string name, string? linkTarget)
    {
        string icon = resolved.Glyph is not null
            ? $"{resolved.ColorSequence}{resolved.Glyph} "
            : "";

        string target = linkTarget is not null
            ? $"{DimColor} → {linkTarget}"
            : "";

        return $"{icon}{resolved.ColorSequence}{name}{target}{ColorEngine.Reset}";
    }

    /// <summary>Formats the summary footer line with colored segments.</summary>
    public static string FormatSummary(int directories, int files, long totalBytes = 0)
    {
        string dirLabel = directories == 1 ? "directory" : "directories";
        string fileLabel = files == 1 ? "file" : "files";

        string summary = $"{CountDirColor}{directories}{DimColor} {dirLabel}, " +
                          $"{CountFileColor}{files}{DimColor} {fileLabel}";

        if (totalBytes > 0)
            summary += $"{DimColor}, {TotalSizeColor}{FormatHumanSize(totalBytes)}{DimColor} total";

        return summary + ColorEngine.Reset;
    }

    private static string FormatSize(long bytes)
    {
        return $"  {SizeColor}({FormatHumanSize(bytes)}){ColorEngine.Reset}";
    }

    private static string FormatHumanSize(long bytes)
    {
        return bytes switch
        {
            < 1024L => $"{bytes} B",
            < 1024L * 1024 => $"{bytes / 1024.0:F1} K",
            < 1024L * 1024 * 1024 => $"{bytes / (1024.0 * 1024):F1} M",
            < 1024L * 1024 * 1024 * 1024 => $"{bytes / (1024.0 * 1024 * 1024):F2} G",
            _ => $"{bytes / (1024.0 * 1024 * 1024 * 1024):F2} T",
        };
    }

    private static long ComputeDirectorySize(DirectoryInfo dir, bool includeHidden)
    {
        try
        {
            var options = new EnumerationOptions
            {
                IgnoreInaccessible = true,
                RecurseSubdirectories = true,
                AttributesToSkip = includeHidden ? 0 : FileAttributes.Hidden
            };
            long total = 0;
            foreach (var fi in dir.EnumerateFiles("*", options))
            {
                try { total += fi.Length; }
                catch { /* skip inaccessible */ }
            }
            return total;
        }
        catch { return 0; }
    }

    /// <summary>
    /// Tracks a directory's real path to detect circular symlinks.
    /// Returns false if the path was already visited.
    /// </summary>
    private bool TrackVisited(DirectoryInfo dir)
    {
        try
        {
            string realPath = dir.LinkTarget is not null
                ? Path.GetFullPath(dir.LinkTarget, dir.Parent?.FullName ?? dir.FullName)
                : dir.FullName;
            return _visitedPaths.Add(realPath);
        }
        catch
        {
            return _visitedPaths.Add(dir.FullName);
        }
    }
}
