using System.Management.Automation;
using GlyphShell.Engine;

namespace GlyphShell.Cmdlets;

[Cmdlet("Set", "GlyphShellOption", SupportsShouldProcess = true)]
public class SetGlyphShellOptionCmdlet : PSCmdlet
{
    [Parameter] public SwitchParameter Diagnostics { get; set; }
    [Parameter] public SwitchParameter DateAge { get; set; }
    [Parameter][ValidatePattern(@"^#[0-9A-Fa-f]{6}$")] public string? DateColor { get; set; }
    [Parameter] public SwitchParameter GitStatus { get; set; }

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

        if (DateColor is not null)
        {
            GlyphShellSettings.DateFlatColor = ColorEngine.FromHex(DateColor);
            GlyphShellSettings.DateColorByAge = false;
            WriteVerbose($"GlyphShell date color set to {DateColor}, age coloring disabled");
        }

        if (MyInvocation.BoundParameters.ContainsKey("GitStatus"))
        {
            GlyphShellSettings.GitStatusEnabled = GitStatus.IsPresent;
            WriteVerbose($"GlyphShell git status: {(GlyphShellSettings.GitStatusEnabled ? "enabled" : "disabled")}");
            refreshFormat = true;
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
