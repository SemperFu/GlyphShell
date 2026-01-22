using System.IO;
using System.Management.Automation;
using System.Text.RegularExpressions;
using GlyphShell.Engine;
using GlyphShell.Themes;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace GlyphShell.Cmdlets;

/// <summary>
/// Exports the current GlyphShell theme(s) to a YAML file.
/// </summary>
[Cmdlet("Export", "GlyphShellTheme")]
[OutputType(typeof(FileInfo))]
public class ExportGlyphShellThemeCmdlet : PSCmdlet
{
    /// <summary>Output file path. Defaults to ./GlyphShell-theme.yaml</summary>
    [Parameter(Position = 0)]
    public string? Path { get; set; }

    /// <summary>Export only the icon theme.</summary>
    [Parameter]
    public SwitchParameter IconOnly { get; set; }

    /// <summary>Export only the color theme.</summary>
    [Parameter]
    public SwitchParameter ColorOnly { get; set; }

    private static readonly Regex AnsiTrueColorRegex = new(
        @"^\x1b\[38;2;(\d+);(\d+);(\d+)m$", RegexOptions.Compiled);

    /// <inheritdoc/>
    protected override void ProcessRecord()
    {
        ThemeManager.Initialize();

        var gold = "\x1b[38;2;255;200;60m";
        var green = "\x1b[38;2;100;220;100m";
        var reset = ColorEngine.Reset;

        var serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
            .Build();

        if (IconOnly.IsPresent && ColorOnly.IsPresent)
        {
            WriteError(new ErrorRecord(
                new ArgumentException("Cannot specify both -IconOnly and -ColorOnly."),
                "ConflictingSwitches", ErrorCategory.InvalidArgument, null));
            return;
        }

        if (!ColorOnly.IsPresent)
        {
            var iconPath = ResolvePath("icon");
            var iconYaml = BuildIconYaml(ThemeManager.CurrentIconTheme);
            var yaml = serializer.Serialize(iconYaml);
            File.WriteAllText(iconPath, yaml);
            Host.UI.WriteLine($"  {green}\u2713{reset} Icon theme exported to {gold}{iconPath}{reset}");
            WriteObject(new FileInfo(iconPath));
        }

        if (!IconOnly.IsPresent)
        {
            var colorPath = ResolvePath("color");
            var colorYaml = BuildColorYaml(ThemeManager.CurrentColorTheme);
            var yaml = serializer.Serialize(colorYaml);
            File.WriteAllText(colorPath, yaml);
            Host.UI.WriteLine($"  {green}\u2713{reset} Color theme exported to {gold}{colorPath}{reset}");
            WriteObject(new FileInfo(colorPath));
        }
    }

    private string ResolvePath(string type)
    {
        if (Path is not null)
            return SessionState.Path.GetUnresolvedProviderPathFromPSPath(Path);

        var dir = SessionState.Path.CurrentFileSystemLocation.Path;
        var suffix = (IconOnly.IsPresent || ColorOnly.IsPresent) ? "" : $"-{type}";
        return System.IO.Path.Combine(dir, $"GlyphShell-theme{suffix}.yaml");
    }

    private static Dictionary<string, object> BuildIconYaml(IconTheme theme)
    {
        var result = new Dictionary<string, object>
        {
            ["name"] = theme.Name + "-export",
            ["type"] = "icon",
        };

        if (theme.FileExtensions.Count > 0)
            result["extensions"] = new SortedDictionary<string, string>(theme.FileExtensions, StringComparer.OrdinalIgnoreCase);
        if (theme.DirectoryNames.Count > 0)
            result["directories"] = new SortedDictionary<string, string>(theme.DirectoryNames, StringComparer.OrdinalIgnoreCase);
        if (theme.WellKnownFiles.Count > 0)
            result["wellknown"] = new SortedDictionary<string, string>(theme.WellKnownFiles, StringComparer.OrdinalIgnoreCase);

        result["defaults"] = new Dictionary<string, string>
        {
            ["file"] = theme.DefaultFileIcon,
            ["directory"] = theme.DefaultDirectoryIcon,
        };

        return result;
    }

    private static Dictionary<string, object> BuildColorYaml(ColorTheme theme)
    {
        var result = new Dictionary<string, object>
        {
            ["name"] = theme.Name + "-export",
            ["type"] = "color",
        };

        if (theme.FileExtensions.Count > 0)
            result["extensions"] = ToHexDict(theme.FileExtensions);
        if (theme.DirectoryNames.Count > 0)
            result["directories"] = ToHexDict(theme.DirectoryNames);
        if (theme.WellKnownFiles.Count > 0)
            result["wellknown"] = ToHexDict(theme.WellKnownFiles);

        result["defaults"] = new Dictionary<string, string>
        {
            ["file"] = AnsiToHex(theme.DefaultFileColor),
            ["directory"] = AnsiToHex(theme.DefaultDirectoryColor),
        };

        return result;
    }

    private static SortedDictionary<string, string> ToHexDict(Dictionary<string, string> source)
    {
        var sorted = new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in source)
            sorted[key] = AnsiToHex(value);
        return sorted;
    }

    /// <summary>
    /// Converts an ANSI 24-bit true-color sequence back to a hex color string.
    /// Falls back to the raw value if it cannot be parsed.
    /// </summary>
    internal static string AnsiToHex(string ansi)
    {
        if (string.IsNullOrEmpty(ansi) || ansi == "\x1b[0m")
            return "#D4D4D4"; // sensible default for reset

        var match = AnsiTrueColorRegex.Match(ansi);
        if (match.Success)
        {
            int r = int.Parse(match.Groups[1].Value);
            int g = int.Parse(match.Groups[2].Value);
            int b = int.Parse(match.Groups[3].Value);
            return $"#{r:X2}{g:X2}{b:X2}";
        }

        // Already hex or unknown — return as-is
        if (ansi.StartsWith('#')) return ansi;
        return ansi;
    }
}
