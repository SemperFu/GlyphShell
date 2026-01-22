using System.IO;
using System.Management.Automation;
using System.Reflection;
using GlyphShell.Engine;
using GlyphShell.Themes;

namespace GlyphShell;

public class ModuleInitializer : IModuleAssemblyInitializer
{
    public void OnImport()
    {
        var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;

        ThemeManager.Initialize();

        var bundledThemesDir = Path.Combine(assemblyDir, "themes");
        if (!Directory.Exists(bundledThemesDir))
        {
            var candidate = Path.GetFullPath(Path.Combine(assemblyDir, "..", "..", "..", "..", "..", "module", "themes"));
            if (Directory.Exists(candidate))
                bundledThemesDir = candidate;
        }
        if (Directory.Exists(bundledThemesDir))
            ThemeManager.LoadThemesFromDirectory(bundledThemesDir);

        ThemeManager.LoadThemesFromDirectory(ThemeManager.UserThemesDirectory);

        var formatFile = Path.Combine(assemblyDir, "..", "..", "..", "..", "..", "module", "GlyphShell.format.ps1xml");
        if (!File.Exists(formatFile))
            formatFile = Path.Combine(assemblyDir, "GlyphShell.format.ps1xml");

        using var ps = PowerShell.Create(RunspaceMode.CurrentRunspace);

        ps.AddCommand("Get-Module").AddParameter("Name", "Terminal-Icons");
        var result = ps.Invoke();
        if (result.Count > 0)
        {
            ps.Commands.Clear();
            ps.AddCommand("Write-Warning")
              .AddParameter("Message",
                "Terminal-Icons is also loaded. Both modules format FileInfo/DirectoryInfo " +
                "and will conflict. Consider removing 'Import-Module Terminal-Icons' from your $PROFILE.");
            ps.Invoke();
        }

        if (File.Exists(formatFile))
        {
            ps.Commands.Clear();
            ps.AddCommand("Update-FormatData").AddParameter("PrependPath", Path.GetFullPath(formatFile));
            ps.Invoke();
        }
    }
}
