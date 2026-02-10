using System;
using System.IO;
using System.Management.Automation;
using System.Text;
using GlyphShell.Engine;
using GlyphShell.Themes;

namespace GlyphShell.Cmdlets;

/// <summary>
/// Standalone directory listing with GlyphShell styling and clean property names.
/// Outputs custom objects where Name, Mode, Size, Date are the colored GlyphShell versions.
/// The format view is registered in FormatFileGenerator at module load.
/// Properties are resolved via direct C# engine calls for performance.
/// </summary>
[Cmdlet(VerbsCommon.Select, "GlyphShell")]
[OutputType(typeof(PSObject))]
public class SelectGlyphShellCmdlet : PSCmdlet
{
    private const string CustomTypeName = "GlyphShell.SelectedItem";

    private static readonly string[] DefaultProperties = ["Icon", "Mode", "Date", "Size", "Name"];

    // Icon resolver — lazy double-check lock init (same pattern as Format cmdlets)
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

    // Git status provider — static instance (same pattern as FormatGlyphShellGitCmdlet)
    private static readonly GitStatusProvider _git = new();

    // Git status indicator colors (mirrored from FormatGlyphShellGitCmdlet)
    private const string ColorModified   = "\x1b[38;2;220;80;80m";
    private const string ColorAdded      = "\x1b[38;2;100;220;100m";
    private const string ColorUntracked  = "\x1b[38;2;150;150;150m";
    private const string ColorRenamed    = "\x1b[38;2;100;150;220m";
    private const string ColorDeleted    = "\x1b[38;2;180;40;40m";
    private const string ColorIgnored    = "\x1b[38;2;80;80;80m";
    private const string ColorConflicted = "\x1b[38;2;220;200;40m";
    private const string ColorStaged     = "\x1b[38;2;100;220;100m";

    [Parameter(Position = 0)]
    public string? Path { get; set; }

    [Parameter]
    [ValidateSet("Icon", "Badge", "Mode", "Name", "Size", "Date", "Git", IgnoreCase = true)]
    public string[]? Property { get; set; }

    [Parameter]
    public SwitchParameter Force { get; set; }

    [Parameter]
    public SwitchParameter Recurse { get; set; }

    [Parameter]
    public int Depth { get; set; } = -1;

    /// <summary>Temporarily preview a theme for this listing only. Specify the base name (e.g. "dracula").</summary>
    [Parameter]
    [ArgumentCompleter(typeof(ThemeBaseNameCompleter))]
    public string? Theme { get; set; }

    private string? _savedIconTheme;
    private string? _savedColorTheme;

    protected override void BeginProcessing()
    {
        if (Theme is not null)
        {
            ThemeManager.Initialize();
            _savedIconTheme = ThemeManager.CurrentIconTheme.Name;
            _savedColorTheme = ThemeManager.CurrentColorTheme.Name;

            var iconName = Theme + "-icons";
            var colorName = Theme + "-colors";

            if (!ThemeManager.SetIconTheme(iconName))
            {
                // Try exact name as fallback
                ThemeManager.SetIconTheme(Theme);
            }
            if (!ThemeManager.SetColorTheme(colorName))
            {
                ThemeManager.SetColorTheme(Theme);
            }
        }
    }

    protected override void EndProcessing()
    {
        try
        {
            var selected = Property ?? DefaultProperties;

            var gciArgs = new StringBuilder("Get-ChildItem");
            if (!string.IsNullOrEmpty(Path))
                gciArgs.Append($" -LiteralPath '{Path.Replace("'", "''")}'");
            if (Force.IsPresent)
                gciArgs.Append(" -Force");
            if (Recurse.IsPresent)
                gciArgs.Append(" -Recurse");
            if (Depth >= 0)
                gciArgs.Append($" -Depth {Depth}");

            try
            {
                var results = InvokeCommand.InvokeScript(gciArgs.ToString());
                foreach (var result in results)
                {
                    var fsInfo = result.BaseObject as FileSystemInfo;
                    if (fsInfo is null) continue;

                    var pso = new PSObject();
                    pso.TypeNames.Insert(0, CustomTypeName);

                    foreach (var prop in selected)
                    {
                        var value = ResolveProperty(prop, fsInfo);
                        pso.Properties.Add(new PSNoteProperty(prop, value));
                    }

                    WriteObject(pso);
                }
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(ex, "SelectGlyphShell", ErrorCategory.InvalidOperation, null));
            }
        }
        finally
        {
            if (_savedIconTheme is not null)
                ThemeManager.SetIconTheme(_savedIconTheme);
            if (_savedColorTheme is not null)
                ThemeManager.SetColorTheme(_savedColorTheme);
        }
    }

    private object? ResolveProperty(string name, FileSystemInfo item)
    {
        return name.ToLowerInvariant() switch
        {
            "icon"  => ResolveIcon(item),
            "badge" => ResolveBadge(item),
            "name"  => ResolveName(item),
            "mode"  => ResolveMode(item),
            "size"  => ResolveSize(item),
            "date"  => ResolveDate(item),
            "git"   => ResolveGit(item),
            _       => null
        };
    }

    #region Direct C# property resolvers

    private object? ResolveIcon(FileSystemInfo item)
    {
        try
        {
            var resolved = GetResolver().Resolve(item);
            return resolved.Glyph is not null
                ? $"{resolved.ColorSequence}{resolved.Glyph}{ColorEngine.Reset}"
                : "";
        }
        catch
        {
            return SafeInvoke("Format-GlyphShellIcon", item) ?? "";
        }
    }

    private object? ResolveBadge(FileSystemInfo item)
    {
        try
        {
            var resolved = GetResolver().Resolve(item);
            return resolved.Badge is not null
                ? $"{resolved.ColorSequence}{resolved.Badge}{ColorEngine.Reset}"
                : "";
        }
        catch
        {
            return SafeInvoke("Format-GlyphShellBadge", item) ?? "";
        }
    }

    private object? ResolveName(FileSystemInfo item)
    {
        try
        {
            var resolved = GetResolver().Resolve(item);

            string suffix = resolved.Suffix is not null
                ? $" \x1b[38;2;100;100;100m{resolved.Suffix}\x1b[0m"
                : "";

            return $"{resolved.ColorSequence}{item.Name}{resolved.Target}{ColorEngine.Reset}{suffix}";
        }
        catch
        {
            return SafeInvoke("Format-GlyphShell", item) ?? item.Name;
        }
    }

    private object? ResolveMode(FileSystemInfo item)
    {
        var mode = GetModeString(item);
        if (string.IsNullOrEmpty(mode)) return mode ?? "";

        try
        {
            var sb = new StringBuilder(mode.Length * 20);
            foreach (var c in mode)
            {
                var color = char.ToLower(c) switch
                {
                    'd' => "\x1b[38;2;86;156;214m",
                    'l' => "\x1b[38;2;78;201;176m",
                    'a' => "\x1b[38;2;100;200;100m",
                    'r' => "\x1b[38;2;220;220;100m",
                    'h' => "\x1b[38;2;128;128;128m",
                    's' => "\x1b[38;2;220;80;80m",
                    '-' => "\x1b[38;2;60;60;60m",
                    _   => "\x1b[38;2;180;180;180m",
                };
                sb.Append(color).Append(c);
            }
            sb.Append(ColorEngine.Reset);
            return sb.ToString();
        }
        catch
        {
            return SafeInvoke("Format-GlyphShellMode", mode) ?? mode;
        }
    }

    private object? ResolveSize(FileSystemInfo item)
    {
        if (item is not FileInfo fi) return "";

        try
        {
            long bytes = fi.Length;

            var (value, unit) = bytes switch
            {
                < 1024L => ((double)bytes, "B"),
                < 1024L * 1024 => (bytes / 1024.0, "K"),
                < 1024L * 1024 * 1024 => (bytes / (1024.0 * 1024), "M"),
                < 1024L * 1024 * 1024 * 1024 => (bytes / (1024.0 * 1024 * 1024), "G"),
                _ => (bytes / (1024.0 * 1024 * 1024 * 1024), "T"),
            };

            var color = bytes switch
            {
                < 1024L => "\x1b[38;2;100;200;100m",
                < 100 * 1024L => "\x1b[38;2;140;200;140m",
                < 1024L * 1024 => "\x1b[38;2;200;200;100m",
                < 10 * 1024L * 1024 => "\x1b[38;2;220;180;80m",
                < 100 * 1024L * 1024 => "\x1b[38;2;220;140;60m",
                < 1024L * 1024 * 1024 => "\x1b[38;2;220;100;60m",
                _ => "\x1b[38;2;220;60;60m",
            };

            if (unit == "B")
                return $"{color}{bytes,6} B{ColorEngine.Reset}";
            else if (value >= 100)
                return $"{color}{value,5:F0} {unit}{ColorEngine.Reset}";
            else if (value >= 10)
                return $"{color}{value,5:F1} {unit}{ColorEngine.Reset}";
            else
                return $"{color}{value,5:F2} {unit}{ColorEngine.Reset}";
        }
        catch
        {
            return SafeInvoke("Format-GlyphShellSize", fi.Length) ?? "";
        }
    }

    private object? ResolveDate(FileSystemInfo item)
    {
        try
        {
            var date = item.LastWriteTime;
            var formatted = $"{date.ToString("d"),10}  {date.ToString("t"),8}";

            if (!GlyphShellSettings.DateColorByAge)
                return $"{GlyphShellSettings.DateFlatColor}{formatted}{ColorEngine.Reset}";

            var age = DateTime.Now - date;
            var color = age.TotalHours switch
            {
                < 1    => "\x1b[38;2;80;250;123m",
                < 24   => "\x1b[38;2;100;220;100m",
                < 72   => "\x1b[38;2;130;200;100m",
                < 168  => "\x1b[38;2;180;200;80m",
                < 720  => "\x1b[38;2;200;180;60m",
                < 4380 => "\x1b[38;2;160;140;80m",
                < 8760 => "\x1b[38;2;120;120;100m",
                _      => "\x1b[38;2;90;90;90m",
            };

            return $"{color}{formatted}{ColorEngine.Reset}";
        }
        catch
        {
            return SafeInvoke("Format-GlyphShellDate", item.LastWriteTime) ?? item.LastWriteTime.ToString();
        }
    }

    private object? ResolveGit(FileSystemInfo item)
    {
        try
        {
            if (!GlyphShellSettings.GitStatusEnabled || !_git.IsInGitRepo(item.FullName))
                return " ";

            if (item is DirectoryInfo)
            {
                var summary = _git.GetDirectorySummary(item.FullName);
                if (summary.Modified == 0 && summary.Added == 0
                    && summary.Untracked == 0 && summary.Deleted == 0 && summary.Conflicted == 0)
                    return " ";

                var dirStatus = _git.GetFileStatus(item.FullName);
                return FormatGitIndicator(dirStatus);
            }

            var status = _git.GetFileStatus(item.FullName);
            return FormatGitIndicator(status);
        }
        catch
        {
            return SafeInvoke("Format-GlyphShellGit", item) ?? " ";
        }
    }

    private static string FormatGitIndicator(GitFileStatus status) => status switch
    {
        GitFileStatus.Modified   => $"{ColorModified}M{ColorEngine.Reset}",
        GitFileStatus.Added      => $"{ColorAdded}A{ColorEngine.Reset}",
        GitFileStatus.Untracked  => $"{ColorUntracked}?{ColorEngine.Reset}",
        GitFileStatus.Renamed    => $"{ColorRenamed}R{ColorEngine.Reset}",
        GitFileStatus.Deleted    => $"{ColorDeleted}D{ColorEngine.Reset}",
        GitFileStatus.Ignored    => $"{ColorIgnored}I{ColorEngine.Reset}",
        GitFileStatus.Conflicted => $"{ColorConflicted}C{ColorEngine.Reset}",
        GitFileStatus.Staged     => $"{ColorStaged}S{ColorEngine.Reset}",
        _                        => " ",
    };

    #endregion

    private static string GetModeString(FileSystemInfo item)
    {
        try
        {
            var pso = PSObject.AsPSObject(item);
            return pso.Properties["Mode"]?.Value?.ToString() ?? "";
        }
        catch { return ""; }
    }

    /// <summary>
    /// Fallback: invokes the Format cmdlet via PowerShell if the direct C# call fails.
    /// </summary>
    private object? SafeInvoke(string cmdletName, object? argument)
    {
        try
        {
            var results = InvokeCommand.InvokeScript(
                $"GlyphShell\\{cmdletName} $args[0]",
                false,
                System.Management.Automation.Runspaces.PipelineResultTypes.None,
                null,
                argument);
            return results.Count > 0 ? results[0]?.BaseObject : null;
        }
        catch { return null; }
    }
}
