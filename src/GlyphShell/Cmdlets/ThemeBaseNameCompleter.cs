using System.Management.Automation;
using System.Management.Automation.Language;
using GlyphShell.Themes;

namespace GlyphShell.Cmdlets;

/// <summary>Tab-completion for base theme names (strips -icons/-colors suffixes).</summary>
public class ThemeBaseNameCompleter : IArgumentCompleter
{
    /// <inheritdoc/>
    public IEnumerable<CompletionResult> CompleteArgument(
        string commandName, string parameterName, string wordToComplete,
        CommandAst commandAst,
        System.Collections.IDictionary fakeBoundParameters)
    {
        ThemeManager.Initialize();

        var baseNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var name in ThemeManager.GetIconThemeNames())
        {
            var baseName = StripSuffix(name);
            if (baseName != "default")
                baseNames.Add(baseName);
        }
        foreach (var name in ThemeManager.GetColorThemeNames())
        {
            var baseName = StripSuffix(name);
            if (baseName != "default")
                baseNames.Add(baseName);
        }

        return baseNames
            .Where(n => n.StartsWith(wordToComplete, StringComparison.OrdinalIgnoreCase))
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
            .Select(n => new CompletionResult(n, n, CompletionResultType.ParameterValue, $"Preview {n} theme"));
    }

    private static string StripSuffix(string name)
    {
        if (name.EndsWith("-icons", StringComparison.OrdinalIgnoreCase))
            return name[..^6];
        if (name.EndsWith("-colors", StringComparison.OrdinalIgnoreCase))
            return name[..^7];
        return name;
    }
}
