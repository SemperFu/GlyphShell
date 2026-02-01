using Xunit;
using GlyphShell.Engine;

namespace GlyphShell.Tests;

public class ImporterExpansionTests
{
    [Fact]
    public void ExpandSparseTheme_WithTypicalTerminalIconsCount_ProducesFullTheme()
    {
        // Simulate a typical Terminal-Icons color theme (sparse)
        var sparse = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // Add a few typical extensions per category
        sparse[".cs"] = "#569CD6"; // Source
        sparse[".js"] = "#F1E05A"; // Source
        sparse[".py"] = "#3776AB"; // Source
        sparse[".html"] = "#E44D26"; // Markup
        sparse[".css"] = "#563D7C"; // Style
        sparse[".json"] = "#F5C518"; // Data
        sparse[".md"] = "#083FA1"; // Document
        sparse[".png"] = "#A074C4"; // Image
        sparse[".jpg"] = "#A074C4"; // Image
        sparse[".zip"] = "#DAA520"; // Archive
        sparse[".mp4"] = "#FD971F"; // Video
        sparse[".mp3"] = "#66D9EF"; // Audio

        var yaml = ThemeExpander.ExpandSparseTheme("legacy-test-colors", sparse);

        Assert.Contains("name: legacy-test-colors", yaml);
        Assert.Contains("type: color", yaml);

        // Should have significantly more entries than the sparse input
        var extLineCount = yaml.Split('\n')
            .Count(l => l.TrimStart().StartsWith(".") && l.Contains(":"));
        Assert.True(extLineCount > 100,
            $"Expanded theme should have >100 extension entries but got {extLineCount}");
    }

    [Fact]
    public void InferCategories_EmptyInput_ReturnsEmptyDictionary()
    {
        var result = ThemeExpander.InferCategoriesFromSparse(
            new Dictionary<string, string>());
        Assert.Empty(result);
    }

    [Fact]
    public void InferCategories_SingleExtension_CreatesOneCategory()
    {
        var sparse = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [".py"] = "#3776AB",
        };

        var result = ThemeExpander.InferCategoriesFromSparse(sparse);
        Assert.True(result.ContainsKey("Source"));
        Assert.Equal("#3776AB", result["Source"]);
    }
}
