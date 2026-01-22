using System.Management.Automation;
using GlyphShell.Engine;
using GlyphShell.Themes;

namespace GlyphShell.Cmdlets;

/// <summary>
/// Shows the currently active icon and color theme names and lists available themes.
/// </summary>
[Cmdlet("Get", "GlyphShellTheme")]
[OutputType(typeof(PSObject))]
public class GetGlyphShellThemeCmdlet : PSCmdlet
{
    /// <inheritdoc/>
    protected override void ProcessRecord()
    {
        ThemeManager.Initialize();

        var dim = "\x1b[38;2;140;140;140m";
        var cyan = "\x1b[38;2;0;200;200m";
        var gold = "\x1b[38;2;255;200;60m";
        var green = "\x1b[38;2;100;220;100m";
        var reset = ColorEngine.Reset;

        Host.UI.WriteLine("");
        Host.UI.WriteLine($"  {cyan}\uDB80\uDD2B  GlyphShell Themes{reset}");
        Host.UI.WriteLine("");
        Host.UI.WriteLine($"  {dim}Active icon theme:{reset}   {gold}{ThemeManager.CurrentIconTheme.Name}{reset}");
        Host.UI.WriteLine($"  {dim}Active color theme:{reset}  {gold}{ThemeManager.CurrentColorTheme.Name}{reset}");
        Host.UI.WriteLine("");

        Host.UI.WriteLine($"  {dim}Available icon themes:{reset}");
        foreach (var name in ThemeManager.GetIconThemeNames())
        {
            var marker = name.Equals(ThemeManager.CurrentIconTheme.Name, StringComparison.OrdinalIgnoreCase)
                ? $"{green}\u25CF{reset}" : $"{dim}\u25CB{reset}";
            var theme = ThemeManager.GetIconTheme(name);
            var builtIn = theme?.IsBuiltIn == true ? $" {dim}(built-in){reset}" : "";
            Host.UI.WriteLine($"    {marker} {name}{builtIn}");
        }
        Host.UI.WriteLine("");

        Host.UI.WriteLine($"  {dim}Available color themes:{reset}");
        foreach (var name in ThemeManager.GetColorThemeNames())
        {
            var marker = name.Equals(ThemeManager.CurrentColorTheme.Name, StringComparison.OrdinalIgnoreCase)
                ? $"{green}\u25CF{reset}" : $"{dim}\u25CB{reset}";
            var theme = ThemeManager.GetColorTheme(name);
            var builtIn = theme?.IsBuiltIn == true ? $" {dim}(built-in){reset}" : "";
            Host.UI.WriteLine($"    {marker} {name}{builtIn}");
        }
        Host.UI.WriteLine("");
    }
}
