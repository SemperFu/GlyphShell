using System;
using System.IO;
using System.Management.Automation;
using System.Text;

namespace GlyphShell.Engine;

/// <summary>
/// Static methods for CodeProperty-backed format columns.
/// Called directly by PowerShell's Extended Type System via PropertyName in format.ps1xml,
/// bypassing ScriptBlock compilation, cmdlet resolution, and parameter binding overhead.
/// Each method must be: public static T MethodName(PSObject instance)
/// </summary>
public static class FormatHelpers
{
    private static IconResolver? _resolver;
    private static readonly object _resolverLock = new();

    // Thread-local single-entry cache: Format-Table evaluates columns left-to-right per row,
    // so Icon→Badge→Name calls for the same file hit this cache every time.
    [ThreadStatic] private static FileSystemInfo? _lastFsi;
    [ThreadStatic] private static ResolvedIcon _lastResolved;

    internal static IconResolver GetResolver()
    {
        if (_resolver is not null) return _resolver;
        lock (_resolverLock)
        {
            _resolver ??= new IconResolver();
            return _resolver;
        }
    }

    private static ResolvedIcon CachedResolve(FileSystemInfo fsi)
    {
        if (ReferenceEquals(fsi, _lastFsi)) return _lastResolved;
        _lastResolved = GetResolver().Resolve(fsi);
        _lastFsi = fsi;
        return _lastResolved;
    }

    /// <summary>Colored icon glyph for the Icon column.</summary>
    public static string GetColoredIcon(PSObject instance)
    {
        try
        {
            if (instance?.BaseObject is not FileSystemInfo fsi) return "";
            var resolved = CachedResolve(fsi);
            return resolved.Glyph is not null
                ? $"{resolved.ColorSequence}{resolved.Glyph}{ColorEngine.Reset}"
                : "";
        }
        catch { return ""; }
    }

    /// <summary>Colored filename for the Name column. Includes symlink target and suffix.</summary>
    public static string GetColoredName(PSObject instance)
    {
        try
        {
            if (instance?.BaseObject is not FileSystemInfo fsi) return "";
            var resolved = CachedResolve(fsi);
            string suffix = resolved.Suffix is not null
                ? $" \x1b[38;2;100;100;100m{resolved.Suffix}\x1b[0m"
                : "";
            return $"{resolved.ColorSequence}{fsi.Name}{resolved.Target}{ColorEngine.Reset}{suffix}";
        }
        catch
        {
            return instance?.BaseObject is FileSystemInfo f ? f.Name : "";
        }
    }

    /// <summary>Project/content-type badge glyph for the Badge column.</summary>
    public static string GetColoredBadge(PSObject instance)
    {
        try
        {
            if (instance?.BaseObject is not FileSystemInfo fsi) return "";
            var resolved = CachedResolve(fsi);
            return resolved.Badge is not null
                ? $"{resolved.ColorSequence}{resolved.Badge}{ColorEngine.Reset}"
                : "";
        }
        catch { return ""; }
    }

    /// <summary>Per-character colored mode flags (d, a, r, h, s, l).</summary>
    public static string GetColoredMode(PSObject instance)
    {
        try
        {
            if (instance?.BaseObject is not FileSystemInfo fsi) return "";

            // Compute mode from FileAttributes directly (avoids ETS property lookup overhead)
            var attr = fsi.Attributes;
            ReadOnlySpan<char> flags =
            [
                (attr & FileAttributes.Directory) != 0 ? 'd' : '-',
                (attr & FileAttributes.Archive) != 0 ? 'a' : '-',
                (attr & FileAttributes.ReadOnly) != 0 ? 'r' : '-',
                (attr & FileAttributes.Hidden) != 0 ? 'h' : '-',
                (attr & FileAttributes.System) != 0 ? 's' : '-',
                (attr & FileAttributes.ReparsePoint) != 0 ? 'l' : '-',
            ];

            var sb = new StringBuilder(128);
            foreach (var c in flags)
            {
                var color = c switch
                {
                    'd' => "\x1b[38;2;86;156;214m",
                    'l' => "\x1b[38;2;78;201;176m",
                    'a' => "\x1b[38;2;100;200;100m",
                    'r' => "\x1b[38;2;220;220;100m",
                    'h' => "\x1b[38;2;128;128;128m",
                    's' => "\x1b[38;2;220;80;80m",
                    '-' => "\x1b[38;2;60;60;60m",
                    _ => "\x1b[38;2;180;180;180m",
                };
                sb.Append(color).Append(c);
            }
            sb.Append(ColorEngine.Reset);
            return sb.ToString();
        }
        catch { return ""; }
    }

    /// <summary>Age-based date coloring gradient.</summary>
    public static string GetColoredDate(PSObject instance)
    {
        try
        {
            if (instance?.BaseObject is not FileSystemInfo fsi) return "";

            var date = fsi.LastWriteTime;
            var formatted = $"{date:d}  {date:t}";

            if (!GlyphShellSettings.DateColorByAge)
                return $"{GlyphShellSettings.DateFlatColor}{formatted}{ColorEngine.Reset}";

            var age = DateTime.Now - date;
            var color = age.TotalHours switch
            {
                < 1    => "\x1b[38;2;80;250;123m",
                < 24   => "\x1b[38;2;100;220;100m",
                < 72   => "\x1b[38;2;130;200;100m",
                < 168  => "\x1b[38;2;180;200;80m",
                < 720  => "\x1b[38;2;200;180;60m",
                < 4380 => "\x1b[38;2;160;140;80m",
                < 8760 => "\x1b[38;2;120;120;100m",
                _      => "\x1b[38;2;90;90;90m",
            };

            return $"{color}{formatted}{ColorEngine.Reset}";
        }
        catch { return ""; }
    }

    /// <summary>Gradient size coloring (green → yellow → orange → red). Empty for directories.</summary>
    public static string GetColoredSize(PSObject instance)
    {
        try
        {
            if (instance?.BaseObject is not FileInfo fi) return "";

            long bytes = fi.Length;

            var (value, unit) = bytes switch
            {
                < 1024L => ((double)bytes, "B"),
                < 1024L * 1024 => (bytes / 1024.0, "K"),
                < 1024L * 1024 * 1024 => (bytes / (1024.0 * 1024), "M"),
                < 1024L * 1024 * 1024 * 1024 => (bytes / (1024.0 * 1024 * 1024), "G"),
                _ => (bytes / (1024.0 * 1024 * 1024 * 1024), "T"),
            };

            var color = bytes switch
            {
                < 1024L => "\x1b[38;2;100;200;100m",
                < 100 * 1024L => "\x1b[38;2;140;200;140m",
                < 1024L * 1024 => "\x1b[38;2;200;200;100m",
                < 10 * 1024L * 1024 => "\x1b[38;2;220;180;80m",
                < 100 * 1024L * 1024 => "\x1b[38;2;220;140;60m",
                < 1024L * 1024 * 1024 => "\x1b[38;2;220;100;60m",
                _ => "\x1b[38;2;220;60;60m",
            };

            if (unit == "B")
                return $"{color}{bytes} B{ColorEngine.Reset}";
            if (value >= 100)
                return $"{color}{value:F0} {unit}{ColorEngine.Reset}";
            if (value >= 10)
                return $"{color}{value:F1} {unit}{ColorEngine.Reset}";
            return $"{color}{value:F2} {unit}{ColorEngine.Reset}";
        }
        catch { return ""; }
    }

    // ── Git status colors (matching FormatGlyphShellGitCmdlet) ──────────
    private const string GitModified   = "\x1b[38;2;220;80;80m";
    private const string GitAdded      = "\x1b[38;2;100;220;100m";
    private const string GitUntracked  = "\x1b[38;2;150;150;150m";
    private const string GitRenamed    = "\x1b[38;2;100;150;220m";
    private const string GitDeleted    = "\x1b[38;2;180;40;40m";
    private const string GitIgnored    = "\x1b[38;2;80;80;80m";
    private const string GitConflicted = "\x1b[38;2;220;200;40m";
    private const string GitStaged     = "\x1b[38;2;100;220;100m";

    // Shared git provider (matches the cmdlet's static instance)
    private static readonly GitStatusProvider _git = new();

    /// <summary>Git status indicator for the Git column.</summary>
    public static string GetColoredGit(PSObject instance)
    {
        try
        {
            if (instance?.BaseObject is not FileSystemInfo fsi) return " ";
            if (!GlyphShellSettings.GitStatusEnabled || !_git.IsInGitRepo(fsi.FullName))
                return " ";

            if (fsi is DirectoryInfo)
            {
                var summary = _git.GetDirectorySummary(fsi.FullName);
                if (summary.Modified == 0 && summary.Added == 0
                    && summary.Untracked == 0 && summary.Deleted == 0 && summary.Conflicted == 0)
                    return " ";
                // Use worst-status indicator for the 3-char column
                return FormatGitIndicator(_git.GetFileStatus(fsi.FullName));
            }

            return FormatGitIndicator(_git.GetFileStatus(fsi.FullName));
        }
        catch { return " "; }
    }

    private static string FormatGitIndicator(GitFileStatus status) => status switch
    {
        GitFileStatus.Modified   => $"{GitModified}M{ColorEngine.Reset}",
        GitFileStatus.Added      => $"{GitAdded}A{ColorEngine.Reset}",
        GitFileStatus.Untracked  => $"{GitUntracked}?{ColorEngine.Reset}",
        GitFileStatus.Renamed    => $"{GitRenamed}R{ColorEngine.Reset}",
        GitFileStatus.Deleted    => $"{GitDeleted}D{ColorEngine.Reset}",
        GitFileStatus.Ignored    => $"{GitIgnored}I{ColorEngine.Reset}",
        GitFileStatus.Conflicted => $"{GitConflicted}C{ColorEngine.Reset}",
        GitFileStatus.Staged     => $"{GitStaged}S{ColorEngine.Reset}",
        _                        => " ",
    };
}
