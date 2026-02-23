using System.Management.Automation;
using GlyphShell.Engine;

namespace GlyphShell.Cmdlets;

/// <summary>Disables (unregisters) a GlyphShell plugin by name.</summary>
[Cmdlet("Disable", "GlyphShellPlugin")]
public class DisableGlyphShellPluginCmdlet : PSCmdlet
{
    [Parameter(Mandatory = true, Position = 0)]
    public string Name { get; set; } = string.Empty;

    protected override void ProcessRecord()
    {
        PluginManager.Unregister(Name);
        WriteVerbose($"Disabled plugin '{Name}'.");
    }
}
