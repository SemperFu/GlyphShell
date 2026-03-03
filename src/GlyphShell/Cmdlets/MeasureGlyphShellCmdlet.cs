using System.Diagnostics;
using System.Management.Automation;
using GlyphShell.Engine;

namespace GlyphShell.Cmdlets;

/// <summary>
/// Benchmarks GlyphShell performance and optionally compares against Terminal-Icons.
/// </summary>
[Cmdlet("Measure", "GlyphShell")]
[OutputType(typeof(void))]
public class MeasureGlyphShellCmdlet : PSCmdlet
{
    [Parameter(Position = 0)]
    public string Path { get; set; } = ".";

    [Parameter]
    public SwitchParameter Compare { get; set; }

    protected override void ProcessRecord()
    {
        var cyan = "\x1b[38;2;0;200;200m";
        var gold = "\x1b[38;2;255;200;60m";
        var green = "\x1b[38;2;100;220;100m";
        var red = "\x1b[38;2;220;80;80m";
        var dim = "\x1b[38;2;140;140;140m";
        var reset = ColorEngine.Reset;

        Host.UI.WriteLine($"\n  {cyan}\uF489  GlyphShell Benchmark{reset}\n");

        var fullPath = SessionState.Path.GetUnresolvedProviderPathFromPSPath(Path);
        if (!System.IO.Directory.Exists(fullPath))
        {
            WriteError(new ErrorRecord(
                new System.IO.DirectoryNotFoundException($"Directory not found: {fullPath}"),
                "DirNotFound", ErrorCategory.ObjectNotFound, fullPath));
            return;
        }

        var entries = System.IO.Directory.GetFileSystemEntries(fullPath);
        Host.UI.WriteLine($"  {dim}Directory:{reset}  {fullPath}");
        Host.UI.WriteLine($"  {dim}Entries:{reset}    {entries.Length}\n");

        // Benchmark GlyphShell resolve
        var resolver = new IconResolver();
        var items = new System.IO.FileSystemInfo[entries.Length];
        for (int i = 0; i < entries.Length; i++)
        {
            items[i] = System.IO.File.Exists(entries[i])
                ? new System.IO.FileInfo(entries[i])
                : new System.IO.DirectoryInfo(entries[i]);
        }

        // Warmup
        foreach (var item in items) resolver.Resolve(item);

        // Timed run
        var sw = Stopwatch.StartNew();
        const int iterations = 100;
        for (int iter = 0; iter < iterations; iter++)
            foreach (var item in items) resolver.Resolve(item);
        sw.Stop();

        var totalResolves = entries.Length * iterations;
        var avgMicroseconds = sw.Elapsed.TotalMicroseconds / totalResolves;
        var perDirMs = sw.Elapsed.TotalMilliseconds / iterations;

        Host.UI.WriteLine($"  {gold}GlyphShell (C# FrozenDictionary){reset}");
        Host.UI.WriteLine($"  {dim}Per-file resolve:{reset}  {green}{avgMicroseconds:F2} \u00b5s{reset}");
        Host.UI.WriteLine($"  {dim}Full directory:{reset}    {green}{perDirMs:F2} ms{reset} {dim}({entries.Length} files x {iterations} iterations){reset}");
        Host.UI.WriteLine($"  {dim}Total resolves:{reset}    {totalResolves:N0} in {sw.ElapsedMilliseconds} ms\n");

        if (Compare.IsPresent)
        {
            BenchmarkTerminalIcons(entries, fullPath, dim, gold, red, green, reset, avgMicroseconds);
        }
        else
        {
            Host.UI.WriteLine($"  {dim}Tip: Use {cyan}Measure-GlyphShell -Compare{dim} to benchmark against Terminal-Icons{reset}\n");
        }
    }

    private void BenchmarkTerminalIcons(string[] entries, string fullPath,
        string dim, string gold, string red, string green, string reset, double glyphShellUs)
    {
        // Check if Terminal-Icons is available
        using var checkPs = PowerShell.Create(RunspaceMode.CurrentRunspace);
        checkPs.AddCommand("Get-Module").AddParameter("Name", "Terminal-Icons").AddParameter("ListAvailable");
        var modules = checkPs.Invoke();

        if (modules.Count == 0)
        {
            Host.UI.WriteLine($"  {red}Terminal-Icons not installed. Skipping comparison.{reset}\n");
            return;
        }

        Host.UI.WriteLine($"  {gold}Terminal-Icons (PowerShell hashtable){reset}");
        Host.UI.WriteLine($"  {dim}Running in isolated subprocess to avoid conflicts...{reset}");

        // Spawn a separate pwsh process so Terminal-Icons doesn't conflict with GlyphShell
        var script = $@"
$sw = [System.Diagnostics.Stopwatch]::StartNew()
Import-Module Terminal-Icons -Force
$sw.Stop()
$importMs = $sw.ElapsedMilliseconds

$items = Get-ChildItem -Path '{fullPath.Replace("'", "''")}' -Force
$items | Format-Table | Out-Null

$sw2 = [System.Diagnostics.Stopwatch]::StartNew()
for ($i = 0; $i -lt 10; $i++) {{ $items | Format-Table | Out-Null }}
$sw2.Stop()

Write-Output ""$($importMs)|$($sw2.ElapsedMilliseconds / 10)|$($items.Count)""
";

        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "pwsh",
                Arguments = $"-NoProfile -NonInteractive -Command \"{script.Replace("\"", "\\\"")}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using var proc = System.Diagnostics.Process.Start(psi);
            if (proc is null)
            {
                Host.UI.WriteLine($"  {red}Could not start pwsh subprocess.{reset}\n");
                return;
            }

            var output = proc.StandardOutput.ReadToEnd().Trim();
            proc.WaitForExit(30_000);

            var parts = output.Split('\n')[^1].Trim().Split('|');
            if (parts.Length < 3)
            {
                Host.UI.WriteLine($"  {red}Unexpected output from Terminal-Icons benchmark.{reset}\n");
                return;
            }

            var tiImportMs = double.Parse(parts[0]);
            var tiFormatMs = double.Parse(parts[1]);
            var fileCount = int.Parse(parts[2]);

            var tiPerFileUs = fileCount > 0 ? (tiFormatMs * 1000.0) / fileCount : 0;

            Host.UI.WriteLine($"  {dim}Module import:{reset}    {red}{tiImportMs:F0} ms{reset}");
            Host.UI.WriteLine($"  {dim}Per-file format:{reset}  {red}{tiPerFileUs:F2} \u00b5s{reset}");
            Host.UI.WriteLine($"  {dim}Full directory:{reset}   {red}{tiFormatMs:F2} ms{reset} {dim}({fileCount} files){reset}\n");

            var speedup = tiPerFileUs > 0 ? tiPerFileUs / glyphShellUs : 0;
            Host.UI.WriteLine($"  {green}\u26a1 GlyphShell is ~{speedup:F0}x faster per-file resolve{reset}\n");
        }
        catch (Exception ex)
        {
            Host.UI.WriteLine($"  {red}Benchmark failed: {ex.Message}{reset}\n");
        }
    }
}
