using Xunit;
using GlyphShell.Engine;
using GlyphShell.Data;

namespace GlyphShell.Tests;

public class ThemeExpanderTests
{
    [Fact]
    public void AllDefaultExtensionsHaveCategory()
    {
        // Every extension in DefaultColorTheme should map to a category
        foreach (var ext in DefaultColorTheme.FileExtensions.Keys)
        {
            var category = ThemeExpander.GetExtensionCategory(ext);
            Assert.NotNull(category);
            Assert.NotEmpty(category);
        }
    }

    [Fact]
    public void GetExtensionCategory_ReturnsCorrectCategories()
    {
        Assert.Equal("Source", ThemeExpander.GetExtensionCategory(".cs"));
        Assert.Equal("Source", ThemeExpander.GetExtensionCategory(".py"));
        Assert.Equal("Image", ThemeExpander.GetExtensionCategory(".png"));
        Assert.Equal("Archive", ThemeExpander.GetExtensionCategory(".zip"));
        Assert.Equal("Document", ThemeExpander.GetExtensionCategory(".md"));
        Assert.Equal("Data", ThemeExpander.GetExtensionCategory(".json"));
        Assert.Equal("Style", ThemeExpander.GetExtensionCategory(".css"));
        Assert.Equal("Markup", ThemeExpander.GetExtensionCategory(".html"));
        Assert.Equal("Video", ThemeExpander.GetExtensionCategory(".mp4"));
        Assert.Equal("Audio", ThemeExpander.GetExtensionCategory(".mp3"));
    }

    [Fact]
    public void GetExtensionCategory_IsCaseInsensitive()
    {
        Assert.Equal(
            ThemeExpander.GetExtensionCategory(".CS"),
            ThemeExpander.GetExtensionCategory(".cs"));
    }

    [Fact]
    public void GetLanguageAccent_ReturnsKnownLanguages()
    {
        Assert.Equal("python", ThemeExpander.GetLanguageAccent(".py"));
        Assert.Equal("rust", ThemeExpander.GetLanguageAccent(".rs"));
        Assert.Equal("go", ThemeExpander.GetLanguageAccent(".go"));
        Assert.Equal("csharp", ThemeExpander.GetLanguageAccent(".cs"));
        Assert.Null(ThemeExpander.GetLanguageAccent(".zip")); // not a language
    }

    [Fact]
    public void GetDirectoryCategory_ReturnsCorrectCategories()
    {
        Assert.Equal("git", ThemeExpander.GetDirectoryCategory(".git"));
        Assert.Equal("build", ThemeExpander.GetDirectoryCategory("bin"));
        Assert.Equal("src", ThemeExpander.GetDirectoryCategory("src"));
        Assert.Equal("test", ThemeExpander.GetDirectoryCategory("tests"));
        Assert.Equal("default", ThemeExpander.GetDirectoryCategory("randomfolder"));
    }

    [Fact]
    public void GenerateColorTheme_ProducesValidYaml()
    {
        var categories = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Source"] = "#FF0000",
            ["Image"] = "#00FF00",
            ["Archive"] = "#0000FF",
        };

        var yaml = ThemeExpander.GenerateColorTheme(
            "test-theme-colors", categories, null, null,
            "#FFFFFF", "#AAAAAA", "#888888");

        Assert.Contains("name: test-theme-colors", yaml);
        Assert.Contains("type: color", yaml);
        Assert.Contains("extensions:", yaml);
        Assert.Contains("directories:", yaml);
        Assert.Contains("wellknown:", yaml);
        Assert.Contains("defaults:", yaml);
        Assert.Contains("file: \"#FFFFFF\"", yaml);
        Assert.Contains("directory: \"#AAAAAA\"", yaml);
        Assert.Contains("symlink: \"#888888\"", yaml);
    }

    [Fact]
    public void GenerateColorTheme_IncludesAllDefaultExtensions()
    {
        var categories = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Source"] = "#FF0000",
        };

        var yaml = ThemeExpander.GenerateColorTheme(
            "count-test-colors", categories, null, null,
            "#FFFFFF", "#AAAAAA", "#888888");

        // Should contain entries for all extensions in DefaultColorTheme
        foreach (var ext in DefaultColorTheme.FileExtensions.Keys)
        {
            Assert.Contains($"  {ext}:", yaml);
        }
    }

    [Fact]
    public void InferCategoriesFromSparse_InfersCorrectly()
    {
        var sparse = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [".cs"] = "#00FF00",
            [".py"] = "#00FF00",
            [".js"] = "#FFFF00",
            [".png"] = "#FF0000",
            [".jpg"] = "#FF0000",
            [".gif"] = "#FF0000",
        };

        var inferred = ThemeExpander.InferCategoriesFromSparse(sparse);

        // Source should be inferred (majority vote between #00FF00 and #FFFF00)
        Assert.True(inferred.ContainsKey("Source"));
        // Image should be #FF0000 (unanimous)
        Assert.Equal("#FF0000", inferred["Image"]);
    }

    [Fact]
    public void ExpandSparseTheme_ProducesFullTheme()
    {
        var sparse = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [".cs"] = "#00FF00",
            [".py"] = "#00FF00",
            [".png"] = "#FF0000",
        };

        var yaml = ThemeExpander.ExpandSparseTheme("sparse-test-colors", sparse);

        Assert.Contains("name: sparse-test-colors", yaml);
        Assert.Contains("type: color", yaml);
        Assert.Contains("extensions:", yaml);
        Assert.Contains("defaults:", yaml);
    }
}
