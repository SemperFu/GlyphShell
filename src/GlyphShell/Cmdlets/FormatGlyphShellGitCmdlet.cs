using System.IO;
using System.Management.Automation;
using GlyphShell.Engine;

namespace GlyphShell.Cmdlets;

/// <summary>
/// Formats a git status indicator for a file or directory.
/// Returns a single colored character (M, A, ?, R, D, I, C) or a compact directory summary.
/// Called by .format.ps1xml for the Git column.
/// </summary>
[Cmdlet("Format", "GlyphShellGit")]
[OutputType(typeof(string))]
public class FormatGlyphShellGitCmdlet : PSCmdlet
{
    // Shared provider — lazy init on first use, disposed when module unloads.
    private static readonly GitStatusProvider _git = new();

    // ── Status indicator colors (24-bit ANSI) ──────────────────────────
    private const string ColorModified   = "\x1b[38;2;220;80;80m";     // red
    private const string ColorAdded      = "\x1b[38;2;100;220;100m";   // green
    private const string ColorUntracked  = "\x1b[38;2;150;150;150m";   // gray
    private const string ColorRenamed    = "\x1b[38;2;100;150;220m";   // blue
    private const string ColorDeleted    = "\x1b[38;2;180;40;40m";     // dark red
    private const string ColorIgnored    = "\x1b[38;2;80;80;80m";      // dark gray
    private const string ColorConflicted = "\x1b[38;2;220;200;40m";    // yellow
    private const string ColorStaged     = "\x1b[38;2;100;220;100m";   // green (same as Added)

    [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true)]
    public FileSystemInfo? FileInfo { get; set; }

    protected override void ProcessRecord()
    {
        if (FileInfo is null) { WriteObject(" "); return; }

        try
        {
            if (!GlyphShellSettings.GitStatusEnabled || !_git.IsInGitRepo(FileInfo.FullName))
            {
                WriteObject(" ");
                return;
            }

            if (FileInfo is DirectoryInfo)
            {
                WriteObject(FormatDirectorySummary(FileInfo.FullName));
                return;
            }

            var status = _git.GetFileStatus(FileInfo.FullName);
            WriteObject(FormatIndicator(status));
        }
        catch
        {
            WriteObject(" ");
        }
    }

    private static string FormatIndicator(GitFileStatus status) => status switch
    {
        GitFileStatus.Modified   => $"{ColorModified}M{ColorEngine.Reset}",
        GitFileStatus.Added      => $"{ColorAdded}A{ColorEngine.Reset}",
        GitFileStatus.Untracked  => $"{ColorUntracked}?{ColorEngine.Reset}",
        GitFileStatus.Renamed    => $"{ColorRenamed}R{ColorEngine.Reset}",
        GitFileStatus.Deleted    => $"{ColorDeleted}D{ColorEngine.Reset}",
        GitFileStatus.Ignored    => $"{ColorIgnored}I{ColorEngine.Reset}",
        GitFileStatus.Conflicted => $"{ColorConflicted}C{ColorEngine.Reset}",
        GitFileStatus.Staged     => $"{ColorStaged}S{ColorEngine.Reset}",
        _                        => " ",
    };

    /// <summary>
    /// For directories, show compact aggregate like "3M 1?" (3 modified, 1 untracked).
    /// Falls back to the worst single-file indicator if the summary is empty.
    /// </summary>
    private string FormatDirectorySummary(string dirPath)
    {
        var summary = _git.GetDirectorySummary(dirPath);
        if (summary.Modified == 0 && summary.Added == 0
            && summary.Untracked == 0 && summary.Deleted == 0 && summary.Conflicted == 0)
        {
            // No changes inside this directory
            return " ";
        }

        // Use the single worst-status indicator to keep it in the 3-char column
        var status = _git.GetFileStatus(dirPath);
        return FormatIndicator(status);
    }
}
