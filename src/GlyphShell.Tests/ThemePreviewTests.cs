using Xunit;
using GlyphShell.Cmdlets;
using GlyphShell.Themes;

namespace GlyphShell.Tests;

public class ThemePreviewTests
{
    [Fact]
    public void ThemeBaseNameCompleter_ReturnsBaseNames()
    {
        ThemeManager.Initialize();

        var completer = new ThemeBaseNameCompleter();
        var results = completer.CompleteArgument("Select-GlyphShell", "Theme", "",
            null!, new System.Collections.Hashtable());

        var names = results.Select(r => r.CompletionText).ToList();

        // Should have base names (not -icons/-colors suffixed)
        Assert.All(names, n =>
        {
            Assert.DoesNotContain("-icons", n);
            Assert.DoesNotContain("-colors", n);
        });

        // Should not include "default"
        Assert.DoesNotContain("default", names, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void ThemeBaseNameCompleter_FiltersOnPrefix()
    {
        ThemeManager.Initialize();

        // First get all base names to see if themes are available
        var completer = new ThemeBaseNameCompleter();
        var allResults = completer.CompleteArgument("Select-GlyphShell", "Theme", "",
            null!, new System.Collections.Hashtable());
        var allNames = allResults.Select(r => r.CompletionText).ToList();

        // Skip filter assertions if no themes loaded (test environment without module dir)
        if (allNames.Count == 0)
            return;

        var results = completer.CompleteArgument("Select-GlyphShell", "Theme", "dra",
            null!, new System.Collections.Hashtable());

        var names = results.Select(r => r.CompletionText).ToList();

        // Should include dracula (starts with "dra")
        Assert.Contains("dracula", names, StringComparer.OrdinalIgnoreCase);

        // Should not include names that don't start with "dra"
        Assert.All(names, n => Assert.StartsWith("dra", n, StringComparison.OrdinalIgnoreCase));
    }
}
