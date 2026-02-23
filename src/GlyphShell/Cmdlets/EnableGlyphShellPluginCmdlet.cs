using System.Management.Automation;
using GlyphShell.Engine;
using GlyphShell.Plugins;

namespace GlyphShell.Cmdlets;

/// <summary>
/// Enables a built-in GlyphShell plugin by name.
/// Currently supported: FilePreview.
/// </summary>
[Cmdlet("Enable", "GlyphShellPlugin")]
public class EnableGlyphShellPluginCmdlet : PSCmdlet
{
    [Parameter(Mandatory = true, Position = 0)]
    public string Name { get; set; } = string.Empty;

    protected override void ProcessRecord()
    {
        switch (Name.ToLowerInvariant())
        {
            case "filepreview":
                // Built-in compiled plugin — always allowed (trusted code, no ScriptBlock)
                PluginManager.Register("FilePreview", FilePreviewPlugin.Resolve);
                WriteVerbose("Enabled built-in plugin 'FilePreview'.");
                break;
            default:
                if (!GlyphShellSettings.PluginsEnabled)
                {
                    WriteError(new ErrorRecord(
                        new InvalidOperationException("The plugin system is disabled. Run 'Set-GlyphShellOption -Plugins' to enable it first."),
                        "PluginsDisabled", ErrorCategory.PermissionDenied, null));
                    return;
                }
                WriteWarning($"Unknown built-in plugin '{Name}'. Use Register-GlyphShellPlugin for custom plugins.");
                break;
        }
    }
}
