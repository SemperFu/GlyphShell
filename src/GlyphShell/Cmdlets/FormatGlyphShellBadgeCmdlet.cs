using System.IO;
using System.Management.Automation;
using GlyphShell.Engine;

namespace GlyphShell.Cmdlets;

/// <summary>
/// Returns the project/content-type badge glyph (with color) for a directory.
/// Used by the format.ps1xml Badge column.
/// </summary>
[Cmdlet("Format", "GlyphShellBadge")]
[OutputType(typeof(string))]
public class FormatGlyphShellBadgeCmdlet : PSCmdlet
{
    private static IconResolver? _resolver;
    private static readonly object _resolverLock = new();

    private static IconResolver GetResolver()
    {
        if (_resolver is not null) return _resolver;
        lock (_resolverLock)
        {
            _resolver ??= new IconResolver();
            return _resolver;
        }
    }

    [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true)]
    public FileSystemInfo? FileInfo { get; set; }

    protected override void ProcessRecord()
    {
        if (FileInfo is null) return;

        ResolvedIcon resolved;
        try
        {
            resolved = GetResolver().Resolve(FileInfo);
        }
        catch (Exception ex)
        {
            if (GlyphShellSettings.DiagnosticsEnabled)
                WriteWarning($"GlyphShell resolver error: {ex.GetType().Name}: {ex.Message}");
            WriteObject("");
            return;
        }

        if (resolved.Badge is not null)
        {
            WriteObject($"{resolved.ColorSequence}{resolved.Badge}{ColorEngine.Reset}");
        }
        else
        {
            WriteObject("");
        }
    }
}
