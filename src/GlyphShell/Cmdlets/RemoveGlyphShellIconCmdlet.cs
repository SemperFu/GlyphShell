using System.Management.Automation;
using GlyphShell.Engine;

namespace GlyphShell.Cmdlets;

/// <summary>
/// Removes a user icon/color override for a file extension, directory name, or well-known filename.
/// </summary>
[Cmdlet("Remove", "GlyphShellIcon", DefaultParameterSetName = "Extension")]
[OutputType(typeof(void))]
public class RemoveGlyphShellIconCmdlet : PSCmdlet
{
    /// <summary>File extension to remove override for (e.g. ".xyz").</summary>
    [Parameter(Mandatory = true, Position = 0, ParameterSetName = "Extension")]
    public string? Extension { get; set; }

    /// <summary>Directory name to remove override for.</summary>
    [Parameter(Mandatory = true, Position = 0, ParameterSetName = "Directory")]
    public string? Directory { get; set; }

    /// <summary>Well-known filename to remove override for.</summary>
    [Parameter(Mandatory = true, Position = 0, ParameterSetName = "WellKnown")]
    public string? WellKnown { get; set; }

    /// <inheritdoc/>
    protected override void ProcessRecord()
    {
        OverrideManager.Initialize();

        string type;
        string key;

        switch (ParameterSetName)
        {
            case "Extension":
                type = "extension";
                key = Extension!;
                break;
            case "Directory":
                type = "directory";
                key = Directory!;
                break;
            case "WellKnown":
                type = "wellknown";
                key = WellKnown!;
                break;
            default:
                return;
        }

        OverrideManager.RemoveOverride(type, key);

        var green = "\x1b[38;2;100;220;100m";
        var gold = "\x1b[38;2;255;200;60m";
        var reset = ColorEngine.Reset;
        Host.UI.WriteLine($"  {green}\u2713{reset} Override removed: {gold}{type}{reset} {gold}{key}{reset}");
    }
}
