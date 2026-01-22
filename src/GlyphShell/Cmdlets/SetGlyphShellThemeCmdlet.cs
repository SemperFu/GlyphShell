using System.Management.Automation;
using System.Management.Automation.Language;
using GlyphShell.Engine;
using GlyphShell.Themes;

namespace GlyphShell.Cmdlets;

/// <summary>
/// Switches the active icon and/or color theme by name.
/// </summary>
[Cmdlet("Set", "GlyphShellTheme")]
[OutputType(typeof(void))]
public class SetGlyphShellThemeCmdlet : PSCmdlet
{
    /// <summary>Name of the icon theme to activate.</summary>
    [Parameter(Position = 0)]
    [ArgumentCompleter(typeof(IconThemeCompleter))]
    public string? IconTheme { get; set; }

    /// <summary>Name of the color theme to activate.</summary>
    [Parameter(Position = 1)]
    [ArgumentCompleter(typeof(ColorThemeCompleter))]
    public string? ColorTheme { get; set; }

    /// <inheritdoc/>
    protected override void ProcessRecord()
    {
        ThemeManager.Initialize();

        var gold = "\x1b[38;2;255;200;60m";
        var green = "\x1b[38;2;100;220;100m";
        var red = "\x1b[38;2;220;80;80m";
        var reset = Engine.ColorEngine.Reset;

        if (IconTheme is not null)
        {
            if (ThemeManager.SetIconTheme(IconTheme))
                Host.UI.WriteLine($"  {green}\u2713{reset} Icon theme set to {gold}{IconTheme}{reset}");
            else
                WriteError(new ErrorRecord(
                    new ItemNotFoundException($"Icon theme '{IconTheme}' not found. Use Get-GlyphShellTheme to list available themes."),
                    "IconThemeNotFound", ErrorCategory.ObjectNotFound, IconTheme));
        }

        if (ColorTheme is not null)
        {
            if (ThemeManager.SetColorTheme(ColorTheme))
                Host.UI.WriteLine($"  {green}\u2713{reset} Color theme set to {gold}{ColorTheme}{reset}");
            else
                WriteError(new ErrorRecord(
                    new ItemNotFoundException($"Color theme '{ColorTheme}' not found. Use Get-GlyphShellTheme to list available themes."),
                    "ColorThemeNotFound", ErrorCategory.ObjectNotFound, ColorTheme));
        }

        if (IconTheme is null && ColorTheme is null)
        {
            WriteWarning("Specify -IconTheme and/or -ColorTheme. Use Get-GlyphShellTheme to see available themes.");
        }
    }
}

/// <summary>Tab-completion for icon theme names.</summary>
public class IconThemeCompleter : IArgumentCompleter
{
    /// <inheritdoc/>
    public IEnumerable<CompletionResult> CompleteArgument(
        string commandName, string parameterName, string wordToComplete,
        CommandAst commandAst,
        System.Collections.IDictionary fakeBoundParameters)
    {
        ThemeManager.Initialize();
        return ThemeManager.GetIconThemeNames()
            .Where(n => n.StartsWith(wordToComplete, StringComparison.OrdinalIgnoreCase))
            .Select(n => new CompletionResult(n, n, CompletionResultType.ParameterValue, n));
    }
}

/// <summary>Tab-completion for color theme names.</summary>
public class ColorThemeCompleter : IArgumentCompleter
{
    /// <inheritdoc/>
    public IEnumerable<CompletionResult> CompleteArgument(
        string commandName, string parameterName, string wordToComplete,
        CommandAst commandAst,
        System.Collections.IDictionary fakeBoundParameters)
    {
        ThemeManager.Initialize();
        return ThemeManager.GetColorThemeNames()
            .Where(n => n.StartsWith(wordToComplete, StringComparison.OrdinalIgnoreCase))
            .Select(n => new CompletionResult(n, n, CompletionResultType.ParameterValue, n));
    }
}
