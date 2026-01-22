using System.Management.Automation;
using GlyphShell.Engine;

namespace GlyphShell.Cmdlets;

/// <summary>
/// Lists all user icon/color overrides as PSCustomObject entries.
/// </summary>
[Cmdlet("Get", "GlyphShellOverrides")]
[OutputType(typeof(PSObject))]
public class GetGlyphShellOverridesCmdlet : PSCmdlet
{
    /// <inheritdoc/>
    protected override void ProcessRecord()
    {
        OverrideManager.Initialize();

        foreach (var (type, key, icon, color) in OverrideManager.GetAllOverrides())
        {
            var obj = new PSObject();
            obj.Properties.Add(new PSNoteProperty("Type", type));
            obj.Properties.Add(new PSNoteProperty("Key", key));
            obj.Properties.Add(new PSNoteProperty("Icon", icon));
            obj.Properties.Add(new PSNoteProperty("Color", color));
            WriteObject(obj);
        }
    }
}
