using System.Management.Automation;
using GlyphShell.Engine;
using GlyphShell.Themes;

namespace GlyphShell.Cmdlets;

/// <summary>
/// Unregisters a user theme by name. Built-in themes cannot be removed.
/// Removes from both icon and color theme registries if found.
/// </summary>
[Cmdlet("Remove", "GlyphShellTheme", SupportsShouldProcess = true)]
[OutputType(typeof(void))]
public class RemoveGlyphShellThemeCmdlet : PSCmdlet
{
    /// <summary>Name of the theme to remove.</summary>
    [Parameter(Mandatory = true, Position = 0)]
    public string Name { get; set; } = "";

    /// <inheritdoc/>
    protected override void ProcessRecord()
    {
        ThemeManager.Initialize();

        var gold = "\x1b[38;2;255;200;60m";
        var green = "\x1b[38;2;100;220;100m";
        var red = "\x1b[38;2;220;80;80m";
        var dim = "\x1b[38;2;140;140;140m";
        var reset = ColorEngine.Reset;

        bool removedAny = false;

        if (ShouldProcess(Name, "Remove theme"))
        {
            if (ThemeManager.RemoveIconTheme(Name))
            {
                Host.UI.WriteLine($"  {green}\u2713{reset} Removed icon theme {gold}{Name}{reset}");
                removedAny = true;
            }

            if (ThemeManager.RemoveColorTheme(Name))
            {
                Host.UI.WriteLine($"  {green}\u2713{reset} Removed color theme {gold}{Name}{reset}");
                removedAny = true;
            }

            if (!removedAny)
            {
                // Check if it exists but is built-in
                var iconExists = ThemeManager.GetIconTheme(Name);
                var colorExists = ThemeManager.GetColorTheme(Name);

                if (iconExists?.IsBuiltIn == true || colorExists?.IsBuiltIn == true)
                    WriteError(new ErrorRecord(
                        new InvalidOperationException($"Cannot remove built-in theme '{Name}'."),
                        "BuiltInTheme", ErrorCategory.PermissionDenied, Name));
                else
                    WriteError(new ErrorRecord(
                        new ItemNotFoundException($"Theme '{Name}' not found."),
                        "ThemeNotFound", ErrorCategory.ObjectNotFound, Name));
            }
        }
    }
}
