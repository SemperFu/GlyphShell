using System.Management.Automation;
using GlyphShell.Data;
using GlyphShell.Engine;

namespace GlyphShell.Cmdlets;

/// <summary>
/// Adds a user icon/color override for a file extension, directory name, or well-known filename.
/// Overrides are persisted to ~/.config/GlyphShell/overrides.yaml.
/// </summary>
[Cmdlet("Add", "GlyphShellIcon", DefaultParameterSetName = "Extension")]
[OutputType(typeof(void))]
public class AddGlyphShellIconCmdlet : PSCmdlet
{
    /// <summary>File extension to override (e.g. ".xyz").</summary>
    [Parameter(Mandatory = true, Position = 0, ParameterSetName = "Extension")]
    public string? Extension { get; set; }

    /// <summary>Directory name to override.</summary>
    [Parameter(Mandatory = true, Position = 0, ParameterSetName = "Directory")]
    public string? Directory { get; set; }

    /// <summary>Well-known filename to override (e.g. "Dockerfile").</summary>
    [Parameter(Mandatory = true, Position = 0, ParameterSetName = "WellKnown")]
    public string? WellKnown { get; set; }

    /// <summary>Nerd Font glyph name (e.g. "nf-md-file_code").</summary>
    [Parameter(Mandatory = true, Position = 1)]
    [Alias("Glyph")]
    public string Icon { get; set; } = "";

    /// <summary>Optional hex color (e.g. "#FF6600").</summary>
    [Parameter(Position = 2)]
    public string? Color { get; set; }

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

        // Warn if the chosen glyph is in the supplementary plane (surrogate pair).
        // PowerShell miscounts these as width 2 causing column misalignment (PS#23861).
        if (BuiltInGlyphs.All.TryGetValue(Icon, out var glyphValue)
            && glyphValue.Length > 0
            && char.ConvertToUtf32(glyphValue, 0) > 0xFFFF)
        {
            var yellow = "\x1b[38;2;255;200;60m";
            var reset0 = ColorEngine.Reset;
            WriteWarning(
                $"'{Icon}' is a supplementary plane glyph (surrogate pair). " +
                "PowerShell miscounts these as 2 cells wide, causing column misalignment. " +
                $"Consider using a BMP glyph instead (nf-dev-*, nf-seti-*, nf-fa-*). " +
                "See: https://github.com/PowerShell/PowerShell/issues/23861");
        }

        OverrideManager.AddOverride(type, key, Icon, Color);

        var green = "\x1b[38;2;100;220;100m";
        var gold = "\x1b[38;2;255;200;60m";
        var reset = ColorEngine.Reset;
        Host.UI.WriteLine($"  {green}\u2713{reset} Override added: {gold}{type}{reset} {gold}{key}{reset} → {Icon}");
    }
}
