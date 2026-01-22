using System.IO;
using System.Management.Automation;
using GlyphShell.Engine;
using GlyphShell.Themes;

namespace GlyphShell.Cmdlets;

/// <summary>
/// Registers a new color theme from a YAML file.
/// </summary>
[Cmdlet("Add", "GlyphShellColorTheme")]
[OutputType(typeof(void))]
public class AddGlyphShellColorThemeCmdlet : PSCmdlet
{
    /// <summary>Path to a YAML theme file.</summary>
    [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true)]
    [Alias("LiteralPath")]
    public string Path { get; set; } = "";

    /// <inheritdoc/>
    protected override void ProcessRecord()
    {
        ThemeManager.Initialize();

        var resolvedPath = SessionState.Path.GetUnresolvedProviderPathFromPSPath(Path);
        if (!File.Exists(resolvedPath))
        {
            WriteError(new ErrorRecord(
                new FileNotFoundException($"Theme file not found: {resolvedPath}"),
                "ThemeFileNotFound", ErrorCategory.ObjectNotFound, resolvedPath));
            return;
        }

        var gold = "\x1b[38;2;255;200;60m";
        var green = "\x1b[38;2;100;220;100m";
        var reset = ColorEngine.Reset;

        var name = ThemeManager.LoadYamlTheme(resolvedPath);
        if (name is not null)
            Host.UI.WriteLine($"  {green}\u2713{reset} Color theme {gold}{name}{reset} loaded from {resolvedPath}");
        else
            WriteError(new ErrorRecord(
                new InvalidDataException($"Failed to parse theme file: {resolvedPath}"),
                "ThemeParseError", ErrorCategory.ParserError, resolvedPath));
    }
}
