using System.IO;
using System.Management.Automation;
using GlyphShell.Engine;
using GlyphShell.Themes;

namespace GlyphShell.Cmdlets;

/// <summary>
/// Scans for Terminal-Icons .psd1 themes and imports them into GlyphShell.
/// Searches the standard Terminal-Icons module directories.
/// </summary>
[Cmdlet("Import", "GlyphShellLegacyThemes")]
[OutputType(typeof(void))]
public class ImportGlyphShellLegacyThemesCmdlet : PSCmdlet
{
    /// <summary>
    /// Optional path to a Terminal-Icons installation directory.
    /// If not specified, searches standard PowerShell module paths.
    /// </summary>
    [Parameter(Position = 0)]
    public string? Path { get; set; }

    /// <inheritdoc/>
    protected override void ProcessRecord()
    {
        ThemeManager.Initialize();

        var dim = "\x1b[38;2;140;140;140m";
        var cyan = "\x1b[38;2;0;200;200m";
        var gold = "\x1b[38;2;255;200;60m";
        var green = "\x1b[38;2;100;220;100m";
        var red = "\x1b[38;2;220;80;80m";
        var reset = ColorEngine.Reset;

        Host.UI.WriteLine("");
        Host.UI.WriteLine($"  {cyan}\uF0EC  Terminal-Icons Migration{reset}");
        Host.UI.WriteLine("");

        var searchPaths = new List<string>();

        if (Path is not null)
        {
            searchPaths.Add(SessionState.Path.GetUnresolvedProviderPathFromPSPath(Path));
        }
        else
        {
            // Search standard PowerShell module paths for Terminal-Icons
            var modulePaths = Environment.GetEnvironmentVariable("PSModulePath")?.Split(
                System.IO.Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries) ?? [];

            foreach (var modulePath in modulePaths)
            {
                var tiDir = System.IO.Path.Combine(modulePath, "Terminal-Icons");
                if (Directory.Exists(tiDir))
                {
                    // Terminal-Icons may have versioned subdirectories
                    foreach (var versionDir in Directory.GetDirectories(tiDir))
                    {
                        searchPaths.Add(versionDir);
                    }
                    searchPaths.Add(tiDir);
                }
            }
        }

        if (searchPaths.Count == 0)
        {
            Host.UI.WriteLine($"  {red}No Terminal-Icons installation found.{reset}");
            Host.UI.WriteLine($"  {dim}Specify a path with: Import-GlyphShellLegacyThemes -Path <dir>{reset}");
            Host.UI.WriteLine("");
            return;
        }

        int imported = 0;
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var searchPath in searchPaths)
        {
            if (!Directory.Exists(searchPath)) continue;

            // Look for .psd1 files in Data/ subdirectory (Terminal-Icons structure)
            var dataDir = System.IO.Path.Combine(searchPath, "Data");
            var themeDirs = new[] { searchPath, dataDir };

            foreach (var dir in themeDirs)
            {
                if (!Directory.Exists(dir)) continue;

                foreach (var psd1 in Directory.EnumerateFiles(dir, "*.psd1", SearchOption.AllDirectories))
                {
                    // Skip the main module manifest
                    if (psd1.EndsWith("Terminal-Icons.psd1", StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (!seen.Add(psd1)) continue;

                    var name = ThemeManager.LoadPsd1Theme(psd1);
                    if (name is not null)
                    {
                        Host.UI.WriteLine($"    {green}\u2713{reset} Imported {gold}{name}{reset} {dim}from {psd1}{reset}");
                        imported++;
                    }
                    else
                    {
                        Host.UI.WriteLine($"    {dim}\u2717 Skipped {psd1} (could not parse){reset}");
                    }
                }
            }
        }

        Host.UI.WriteLine("");
        if (imported > 0)
            Host.UI.WriteLine($"  {green}Imported {imported} theme(s).{reset} Use {cyan}Get-GlyphShellTheme{reset} to see them.");
        else
            Host.UI.WriteLine($"  {dim}No themes found to import.{reset}");
        Host.UI.WriteLine("");
    }
}
