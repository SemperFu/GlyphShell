using System.Management.Automation;
using GlyphShell.Engine;

namespace GlyphShell.Cmdlets;

/// <summary>Unregisters a GlyphShell plugin by name.</summary>
[Cmdlet("Unregister", "GlyphShellPlugin")]
public class UnregisterGlyphShellPluginCmdlet : PSCmdlet
{
    [Parameter(Mandatory = true, Position = 0)]
    public string Name { get; set; } = string.Empty;

    protected override void ProcessRecord()
    {
        PluginManager.Unregister(Name);
        WriteVerbose($"Unregistered plugin '{Name}'.");
    }
}
