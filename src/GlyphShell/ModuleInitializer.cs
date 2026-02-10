using System.IO;
using System.Management.Automation;
using System.Reflection;
using System.Runtime.InteropServices;
using GlyphShell.Engine;
using GlyphShell.Themes;

namespace GlyphShell;

/// <summary>
/// Initializes the module when imported, prepending the format data
/// so our view takes priority over the default FileSystem formatter.
/// </summary>
public class ModuleInitializer : IModuleAssemblyInitializer
{
    private static bool _nativeResolverRegistered;

    /// <inheritdoc/>
    public void OnImport()
    {
        PreloadNativeGit2();

        var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;

        // Initialize theme system with built-in defaults, then load bundled + user themes
        ThemeManager.Initialize();

        // Load bundled YAML themes from module/themes/ directory.
        // Try beside the assembly first (published layout), then the dev layout.
        var bundledThemesDir = Path.Combine(assemblyDir, "themes");
        if (!Directory.Exists(bundledThemesDir))
        {
            // Dev layout: DLL is at src/GlyphShell/bin/Release/net9.0/, themes at module/themes/
            var candidate = Path.GetFullPath(Path.Combine(assemblyDir, "..", "..", "..", "..", "..", "module", "themes"));
            if (Directory.Exists(candidate))
                bundledThemesDir = candidate;
        }
        if (Directory.Exists(bundledThemesDir))
            ThemeManager.LoadThemesFromDirectory(bundledThemesDir);

        ThemeManager.LoadThemesFromDirectory(ThemeManager.UserThemesDirectory);

        // Generate format file based on current settings (git/badge columns)
        var formatFile = FormatFileGenerator.Generate();

        using var ps = PowerShell.Create(RunspaceMode.CurrentRunspace);

        // Warn if Terminal-Icons is also loaded — both hook the same format types
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

        ps.Commands.Clear();
        ps.AddCommand("Update-FormatData").AddParameter("PrependPath", formatFile);
        ps.Invoke();
    }

    /// <summary>
    /// Preloads the LibGit2Sharp native library from the runtimes/ directory.
    /// PowerShell binary modules don't use .NET's standard RID-based native lib resolution,
    /// so we load it explicitly before LibGit2Sharp's static constructor runs.
    /// </summary>
    private static void PreloadNativeGit2()
    {
        if (_nativeResolverRegistered) return;
        _nativeResolverRegistered = true;

        try
        {
            var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
            var rids = GetFallbackRids();

            foreach (var rid in rids)
            {
                var nativeDir = Path.Combine(assemblyDir, "runtimes", rid, "native");
                if (!Directory.Exists(nativeDir)) continue;

                foreach (var candidate in Directory.GetFiles(nativeDir, "git2*"))
                {
                    if (NativeLibrary.TryLoad(candidate, out _))
                        return; // loaded successfully
                }
            }
        }
        catch
        {
            // Git status will silently degrade — never crash module import
        }
    }

    private static string[] GetFallbackRids()
    {
        var arch = RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.X64 => "x64",
            Architecture.Arm64 => "arm64",
            Architecture.X86 => "x86",
            _ => "x64",
        };

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return [$"win-{arch}"];
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return [$"linux-{arch}"];
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return [$"osx-{arch}"];

        return [];
    }
}
