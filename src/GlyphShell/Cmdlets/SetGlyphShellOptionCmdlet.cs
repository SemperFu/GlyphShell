using System.Management.Automation;
using GlyphShell.Engine;

namespace GlyphShell.Cmdlets;

/// <summary>
/// Configures runtime options for GlyphShell.
/// </summary>
[Cmdlet("Set", "GlyphShellOption", SupportsShouldProcess = true)]
public class SetGlyphShellOptionCmdlet : PSCmdlet
{
    /// <summary>Enable or disable diagnostic warnings for resolver errors.</summary>
    [Parameter]
    public SwitchParameter Diagnostics { get; set; }

    /// <summary>Enable or disable age-based date coloring (green→yellow→gray gradient).</summary>
    [Parameter]
    public SwitchParameter DateAge { get; set; }

    /// <summary>Set a flat date color as an RGB hex string (e.g. "#569CD6" for blue). Implicitly disables DateAge.</summary>
    [Parameter]
    [ValidatePattern(@"^#[0-9A-Fa-f]{6}$")]
    public string? DateColor { get; set; }

    /// <summary>Enable or disable project-type detection for directories.</summary>
    [Parameter]
    public SwitchParameter ProjectDetection { get; set; }

    /// <summary>Enable or disable git status indicators in the Git column.</summary>
    [Parameter]
    public SwitchParameter GitStatus { get; set; }

    /// <summary>Enable or disable the plugin system. Requires confirmation when enabling.</summary>
    [Parameter]
    public SwitchParameter Plugins { get; set; }

    /// <summary>Enable or disable badge merge — when enabled, project badges replace the folder icon instead of a separate column.</summary>
    [Parameter]
    public SwitchParameter BadgeMerge { get; set; }

    protected override void ProcessRecord()
    {
        bool refreshFormat = false;

        if (MyInvocation.BoundParameters.ContainsKey("Diagnostics"))
        {
            GlyphShellSettings.DiagnosticsEnabled = Diagnostics.IsPresent;
            WriteVerbose($"GlyphShell diagnostics: {(GlyphShellSettings.DiagnosticsEnabled ? "enabled" : "disabled")}");
        }

        if (MyInvocation.BoundParameters.ContainsKey("DateAge"))
        {
            GlyphShellSettings.DateColorByAge = DateAge.IsPresent;
            WriteVerbose($"GlyphShell date age coloring: {(GlyphShellSettings.DateColorByAge ? "enabled" : "disabled")}");
        }

        if (MyInvocation.BoundParameters.ContainsKey("ProjectDetection"))
        {
            GlyphShellSettings.ProjectDetectionEnabled = ProjectDetection.IsPresent;
            WriteVerbose($"GlyphShell project detection: {(GlyphShellSettings.ProjectDetectionEnabled ? "enabled" : "disabled")}");
            refreshFormat = true;
        }

        if (MyInvocation.BoundParameters.ContainsKey("GitStatus"))
        {
            GlyphShellSettings.GitStatusEnabled = GitStatus.IsPresent;
            WriteVerbose($"GlyphShell git status: {(GlyphShellSettings.GitStatusEnabled ? "enabled" : "disabled")}");
            refreshFormat = true;
        }

        if (DateColor is not null)
        {
            GlyphShellSettings.DateFlatColor = ColorEngine.FromHex(DateColor);
            GlyphShellSettings.DateColorByAge = false;
            WriteVerbose($"GlyphShell date color set to {DateColor}, age coloring disabled");
        }

        if (MyInvocation.BoundParameters.ContainsKey("BadgeMerge"))
        {
            GlyphShellSettings.BadgeMerge = BadgeMerge.IsPresent;
            WriteVerbose($"GlyphShell badge merge: {(GlyphShellSettings.BadgeMerge ? "enabled" : "disabled")}");
            refreshFormat = true;
        }

        if (MyInvocation.BoundParameters.ContainsKey("Plugins"))
        {
            if (Plugins.IsPresent)
            {
                // Enabling requires confirmation
                var warning = "\u26a0\ufe0f  Plugin Security Warning\n\n"
                    + "Plugins execute code on EVERY file listing (Get-ChildItem).\n"
                    + "A malicious plugin could read file contents or slow down your shell.\n\n"
                    + "Custom plugins run in a sandboxed environment that blocks:\n"
                    + "  \u2022 File writes (Remove-Item, Set-Content, New-Item, etc.)\n"
                    + "  \u2022 Network access (Invoke-WebRequest, Invoke-RestMethod)\n"
                    + "  \u2022 Process execution (Start-Process, Invoke-Expression)\n\n"
                    + "Only enable if you review and trust every plugin you register.";

                if (ShouldContinue(warning, "Enable GlyphShell Plugin System?"))
                {
                    GlyphShellSettings.PluginsEnabled = true;
                    Host.UI.WriteLine(ConsoleColor.Green, Host.UI.RawUI.BackgroundColor, "Plugin system enabled.");
                }
                else
                {
                    Host.UI.WriteLine(ConsoleColor.Yellow, Host.UI.RawUI.BackgroundColor, "Plugin system remains disabled.");
                }
            }
            else
            {
                GlyphShellSettings.PluginsEnabled = false;
                // Unregister all custom plugins when disabling
                foreach (var name in PluginManager.GetAll())
                {
                    PluginManager.Unregister(name);
                }
                Host.UI.WriteLine("Plugin system disabled. All custom plugins unregistered.");
            }
        }

        if (refreshFormat)
        {
            var formatPath = FormatFileGenerator.Generate();
            using var ps = PowerShell.Create(RunspaceMode.CurrentRunspace);
            ps.AddCommand("Update-FormatData").AddParameter("PrependPath", formatPath);
            ps.Invoke();
        }
    }
}
