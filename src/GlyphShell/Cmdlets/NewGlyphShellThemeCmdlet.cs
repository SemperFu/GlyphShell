using System.IO;
using System.Management.Automation;
using GlyphShell.Engine;
using GlyphShell.Themes;

namespace GlyphShell.Cmdlets;

/// <summary>
/// Generates a comprehensive color theme from category-level color definitions.
/// Instead of specifying 900+ individual colors, define ~20 category colors and
/// the engine fills in every extension, directory, and well-known file.
/// </summary>
[Cmdlet("New", "GlyphShellTheme", SupportsShouldProcess = true)]
[OutputType(typeof(FileInfo))]
public class NewGlyphShellThemeCmdlet : PSCmdlet
{
    // ── Core parameters ──

    /// <summary>Theme name (used in YAML and for registration).</summary>
    [Parameter(Mandatory = true, Position = 0)]
    [ValidateNotNullOrEmpty]
    public string Name { get; set; } = null!;

    // ── Category color parameters (optional, defaults from current theme's DefaultFileColor) ──

    /// <summary>Color for source code files (.cs, .py, .js, etc.).</summary>
    [Parameter] public string? Source { get; set; }
    /// <summary>Color for markup files (.html, .xml, .jsx, etc.).</summary>
    [Parameter] public string? Markup { get; set; }
    /// <summary>Color for stylesheets (.css, .scss, .sass, etc.).</summary>
    [Parameter] public string? Style { get; set; }
    /// <summary>Color for data/config files (.json, .yaml, .toml, etc.).</summary>
    [Parameter] public string? Data { get; set; }
    /// <summary>Color for configuration files (.gitignore, .editorconfig, etc.).</summary>
    [Parameter] public string? Config { get; set; }
    /// <summary>Color for document files (.md, .txt, .pdf, etc.).</summary>
    [Parameter] public string? Document { get; set; }
    /// <summary>Color for image files (.png, .jpg, .svg, etc.).</summary>
    [Parameter] public string? Image { get; set; }
    /// <summary>Color for video files (.mp4, .mkv, .avi, etc.).</summary>
    [Parameter] public string? Video { get; set; }
    /// <summary>Color for audio files (.mp3, .wav, .flac, etc.).</summary>
    [Parameter] public string? Audio { get; set; }
    /// <summary>Color for archive files (.zip, .tar, .gz, etc.).</summary>
    [Parameter] public string? Archive { get; set; }
    /// <summary>Color for binary files (.exe, .dll, .wasm, etc.).</summary>
    [Parameter] public string? Binary { get; set; }
    /// <summary>Color for build system files (.sln, .csproj, .cmake, etc.).</summary>
    [Parameter] public string? Build { get; set; }
    /// <summary>Color for crypto/security files (.pem, .key, .crt, etc.).</summary>
    [Parameter] public string? Crypto { get; set; }
    /// <summary>Color for font files (.ttf, .otf, .woff, etc.).</summary>
    [Parameter] public string? Font { get; set; }
    /// <summary>Color for database files (.sql, .db, .sqlite, etc.).</summary>
    [Parameter] public string? Database { get; set; }
    /// <summary>Color for temporary/cache files (.tmp, .bak, .log, etc.).</summary>
    [Parameter] public string? Temp { get; set; }
    /// <summary>Color for 3D model files (.blend, .fbx, .stl, etc.).</summary>
    [Parameter] public string? ThreeD { get; set; }
    /// <summary>Color for game development files (.unity, .shader, etc.).</summary>
    [Parameter] public string? GameDev { get; set; }
    /// <summary>Color for web-related files (.http, .map, etc.).</summary>
    [Parameter] public string? Web { get; set; }

    // ── Default colors ──

    /// <summary>Default color for files not matching any category.</summary>
    [Parameter] public string? DefaultFile { get; set; }
    /// <summary>Default color for directories.</summary>
    [Parameter] public string? DefaultDirectory { get; set; }
    /// <summary>Color for symlinks.</summary>
    [Parameter] public string? Symlink { get; set; }

    // ── Output options ──

    /// <summary>Output YAML file path. Defaults to ~/.config/GlyphShell/themes/{Name}-colors.yaml.</summary>
    [Parameter]
    public string? Path { get; set; }

    /// <summary>Auto-register the theme after generation.</summary>
    [Parameter]
    public SwitchParameter Register { get; set; }

    /// <summary>Overwrite existing file without prompting.</summary>
    [Parameter]
    public SwitchParameter Force { get; set; }

    /// <inheritdoc/>
    protected override void ProcessRecord()
    {
        ThemeManager.Initialize();

        var gold = "\x1b[38;2;255;200;60m";
        var green = "\x1b[38;2;100;220;100m";
        var cyan = "\x1b[38;2;0;200;200m";
        var dim = "\x1b[38;2;140;140;140m";
        var reset = ColorEngine.Reset;

        // Build category color map with defaults for unspecified categories
        var fallbackColor = DefaultFile ?? "#D4D4D4";
        var categoryColors = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Source"] = Source ?? fallbackColor,
            ["Markup"] = Markup ?? fallbackColor,
            ["Style"] = Style ?? fallbackColor,
            ["Data"] = Data ?? fallbackColor,
            ["Config"] = Config ?? fallbackColor,
            ["Document"] = Document ?? fallbackColor,
            ["Image"] = Image ?? fallbackColor,
            ["Video"] = Video ?? fallbackColor,
            ["Audio"] = Audio ?? fallbackColor,
            ["Archive"] = Archive ?? fallbackColor,
            ["Binary"] = Binary ?? fallbackColor,
            ["Build"] = Build ?? fallbackColor,
            ["Crypto"] = Crypto ?? fallbackColor,
            ["Font"] = Font ?? fallbackColor,
            ["Database"] = Database ?? fallbackColor,
            ["Temp"] = Temp ?? fallbackColor,
            ["3D"] = ThreeD ?? fallbackColor,
            ["GameDev"] = GameDev ?? fallbackColor,
            ["Web"] = Web ?? fallbackColor,
            ["Misc"] = fallbackColor,
        };

        var defaultFile = DefaultFile ?? "#D4D4D4";
        var defaultDir = DefaultDirectory ?? "#569CD6";
        var symlink = Symlink ?? "#8BE9FD";

        // Generate the theme YAML
        var themeName = Name.EndsWith("-colors", StringComparison.OrdinalIgnoreCase) ? Name : Name + "-colors";
        var yaml = ThemeExpander.GenerateColorTheme(themeName, categoryColors, null, null, defaultFile, defaultDir, symlink);

        // Resolve output path
        string outputPath;
        if (Path is not null)
        {
            outputPath = SessionState.Path.GetUnresolvedProviderPathFromPSPath(Path);
        }
        else
        {
            var configDir = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".config", "GlyphShell", "themes");
            Directory.CreateDirectory(configDir);
            outputPath = System.IO.Path.Combine(configDir, $"{themeName}.yaml");
        }

        // Check for existing file
        if (File.Exists(outputPath) && !Force.IsPresent)
        {
            if (!ShouldContinue($"File '{outputPath}' already exists. Overwrite?", "Confirm Overwrite"))
                return;
        }

        if (ShouldProcess(outputPath, "Generate theme"))
        {
            File.WriteAllText(outputPath, yaml);

            Host.UI.WriteLine("");
            Host.UI.WriteLine($"  {green}\u2713{reset} Generated {gold}{themeName}{reset}");
            Host.UI.WriteLine($"    {dim}{outputPath}{reset}");

            // Count categories that were explicitly specified
            int specifiedCount = new[] { Source, Markup, Style, Data, Config, Document, Image, Video, Audio, Archive, Binary, Build, Crypto, Font, Database, Temp, ThreeD, GameDev, Web }
                .Count(c => c is not null);
            Host.UI.WriteLine($"    {dim}{specifiedCount} category colors specified, rest use default{reset}");

            if (Register.IsPresent)
            {
                var loaded = ThemeManager.LoadYamlTheme(outputPath);
                if (loaded is not null)
                    Host.UI.WriteLine($"  {green}\u2713{reset} Registered as {cyan}{loaded}{reset}");
                else
                    WriteWarning("Theme was saved but could not be registered.");
            }
            else
            {
                Host.UI.WriteLine($"    {dim}Use -Register to auto-register, or:{reset}");
                Host.UI.WriteLine($"    {dim}Add-GlyphShellColorTheme -Path \"{outputPath}\"{reset}");
            }
            Host.UI.WriteLine("");

            WriteObject(new FileInfo(outputPath));
        }
    }
}
