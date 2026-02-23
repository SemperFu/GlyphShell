using System.Management.Automation;
using GlyphShell.Engine;

namespace GlyphShell.Cmdlets;

/// <summary>Lists all registered GlyphShell plugins.</summary>
[Cmdlet("Get", "GlyphShellPlugins")]
[OutputType(typeof(string))]
public class GetGlyphShellPluginsCmdlet : PSCmdlet
{
    protected override void ProcessRecord()
    {
        foreach (var name in PluginManager.GetAll())
        {
            WriteObject(name);
        }
    }
}
