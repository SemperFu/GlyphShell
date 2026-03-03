using System.IO;
using System.Management.Automation;
using GlyphShell.Engine;

namespace GlyphShell.Cmdlets;

/// <summary>
/// Displays directory contents in a responsive grid layout with icons and colors.
/// Similar to 'eza --grid'.
/// </summary>
[Cmdlet("Show", "GlyphShellGrid")]
[Alias("gsgrid")]
[OutputType(typeof(string))]
public class ShowGlyphShellGridCmdlet : PSCmdlet
{
    private static readonly IconResolver _resolver = new();

    /// <summary>Path to the directory to display. Defaults to current directory.</summary>
    [Parameter(Position = 0, ValueFromPipeline = true)]
    public string Path { get; set; } = ".";

    /// <summary>Include hidden files and directories.</summary>
    [Parameter]
    public SwitchParameter ShowHidden { get; set; }

    /// <summary>Show directories before files.</summary>
    [Parameter]
    public SwitchParameter DirectoriesFirst { get; set; }

    protected override void ProcessRecord()
    {
        string fullPath;
        try
        {
            fullPath = SessionState.Path.GetUnresolvedProviderPathFromPSPath(Path);
        }
        catch
        {
            WriteError(new ErrorRecord(
                new DirectoryNotFoundException($"Cannot find path: {Path}"),
                "PathNotFound", ErrorCategory.ObjectNotFound, Path));
            return;
        }

        if (!Directory.Exists(fullPath))
        {
            WriteError(new ErrorRecord(
                new DirectoryNotFoundException($"Not a directory: {fullPath}"),
                "NotDirectory", ErrorCategory.ObjectNotFound, fullPath));
            return;
        }

        var dirInfo = new DirectoryInfo(fullPath);
        var items = new List<FileSystemInfo>();

        try
        {
            foreach (var entry in dirInfo.EnumerateFileSystemInfos())
            {
                if (!ShowHidden && (entry.Attributes & FileAttributes.Hidden) != 0)
                    continue;
                items.Add(entry);
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            WriteWarning($"Access denied: {ex.Message}");
            return;
        }

        // Sort: directories first if requested, then by name
        if (DirectoriesFirst)
        {
            items.Sort((a, b) =>
            {
                bool aDir = a is DirectoryInfo;
                bool bDir = b is DirectoryInfo;
                if (aDir != bDir) return bDir.CompareTo(aDir); // dirs first
                return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
            });
        }
        else
        {
            items.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
        }

        // Get terminal width
        int termWidth;
        try { termWidth = Host.UI.RawUI.WindowSize.Width; }
        catch { termWidth = 120; } // fallback

        var renderer = new GridRenderer(_resolver);
        foreach (var line in renderer.Render(items, termWidth))
        {
            WriteObject(line);
        }
    }
}
