using Xunit;
using GlyphShell.Engine;
using GlyphShell.Data;

namespace GlyphShell.Tests;

public class NewGlyphShellThemeTests
{
    [Fact]
    public void GeneratedTheme_HasCorrectExtensionCount()
    {
        var categories = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Source"] = "#FF0000",
            ["Markup"] = "#00FF00",
            ["Style"] = "#0000FF",
            ["Data"] = "#FFFF00",
            ["Image"] = "#FF00FF",
            ["Archive"] = "#00FFFF",
        };

        var yaml = ThemeExpander.GenerateColorTheme(
            "gen-test-colors", categories, null, null,
            "#D4D4D4", "#569CD6", "#8BE9FD");

        // Count extension entries (lines starting with "  .")
        var extCount = yaml.Split('\n')
            .Count(line => line.TrimStart().StartsWith(".") && line.Contains(":"));

        // Should have at least as many as DefaultColorTheme
        Assert.True(extCount >= DefaultColorTheme.FileExtensions.Count,
            $"Expected at least {DefaultColorTheme.FileExtensions.Count} extensions but got {extCount}");
    }

    [Fact]
    public void GeneratedTheme_AppliesLanguageAccents()
    {
        var categories = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Source"] = "#FFFFFF",
        };
        var langAccents = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["python"] = "#3776AB",
            ["rust"] = "#CE422B",
        };

        var yaml = ThemeExpander.GenerateColorTheme(
            "accent-test-colors", categories, langAccents, null,
            "#D4D4D4", "#569CD6", "#8BE9FD");

        // .py should get the Python accent, not the default Source color
        Assert.Contains(".py: \"#3776AB\"", yaml);
        // .rs should get the Rust accent
        Assert.Contains(".rs: \"#CE422B\"", yaml);
    }
}
