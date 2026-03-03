using System.Management.Automation;
using GlyphShell.Engine;

namespace GlyphShell.Cmdlets;

/// <summary>
/// Displays a directory tree with Nerd Font icons, ANSI colors, and box-drawing lines.
/// </summary>
[Cmdlet("Show", "GlyphShellTree")]
[Alias("gstree")]
[OutputType(typeof(void))]
public class ShowGlyphShellTreeCmdlet : PSCmdlet
{
    private static readonly IconResolver _resolver = new();

    [Parameter(Position = 0)]
    public string Path { get; set; } = ".";

    [Parameter]
    public int Depth { get; set; } = 3;

    [Parameter]
    public SwitchParameter ShowHidden { get; set; }

    [Parameter]
    public SwitchParameter FilesFirst { get; set; }

    [Parameter]
    [Alias("s")]
    public SwitchParameter Size { get; set; }

    protected override void ProcessRecord()
    {
        string resolvedPath;
        try
        {
            resolvedPath = SessionState.Path.GetUnresolvedProviderPathFromPSPath(Path);
        }
        catch (Exception ex)
        {
            WriteError(new ErrorRecord(ex, "InvalidPath", ErrorCategory.InvalidArgument, Path));
            return;
        }

        if (!System.IO.Directory.Exists(resolvedPath))
        {
            WriteError(new ErrorRecord(
                new DirectoryNotFoundException($"Directory not found: {resolvedPath}"),
                "DirectoryNotFound",
                ErrorCategory.ObjectNotFound,
                resolvedPath));
            return;
        }

        var renderer = new TreeRenderer(_resolver);
        var result = renderer.RenderTree(
            resolvedPath,
            maxDepth: Depth,
            showHidden: ShowHidden.IsPresent,
            directoriesFirst: !FilesFirst.IsPresent,
            showSize: Size.IsPresent,
            writeLine: line => Host.UI.WriteLine(line));

        Host.UI.WriteLine("");
        Host.UI.WriteLine(TreeRenderer.FormatSummary(result.DirectoryCount, result.FileCount, result.TotalBytes));
    }
}
