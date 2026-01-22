using System.Management.Automation;
using GlyphShell.Data;
using GlyphShell.Engine;
using GlyphShell.Themes;

namespace GlyphShell.Cmdlets;

/// <summary>
/// Previews the current theme by showing a sample directory listing with icons and colors.
/// </summary>
[Cmdlet("Show", "GlyphShellTheme")]
[OutputType(typeof(void))]
public class ShowGlyphShellThemeCmdlet : PSCmdlet
{
    /// <inheritdoc/>
    protected override void ProcessRecord()
    {
        ThemeManager.Initialize();

        var iconTheme = ThemeManager.CurrentIconTheme;
        var colorTheme = ThemeManager.CurrentColorTheme;
        var glyphs = BuiltInGlyphs.All;

        var dim = "\x1b[38;2;140;140;140m";
        var cyan = "\x1b[38;2;0;200;200m";
        var gold = "\x1b[38;2;255;200;60m";
        var reset = ColorEngine.Reset;

        Host.UI.WriteLine("");
        Host.UI.WriteLine($"  {cyan}\uDB80\uDD2B  Theme Preview{reset}");
        Host.UI.WriteLine($"  {dim}Icon theme:{reset}  {gold}{iconTheme.Name}{reset}    {dim}Color theme:{reset}  {gold}{colorTheme.Name}{reset}");
        Host.UI.WriteLine("");

        // Sample file extensions to preview
        var sampleExtensions = new[]
        {
            (".cs", "Program.cs"), (".js", "index.js"), (".py", "main.py"),
            (".ts", "app.ts"), (".json", "config.json"), (".md", "README.md"),
            (".html", "index.html"), (".css", "styles.css"), (".go", "main.go"),
            (".rs", "lib.rs"), (".java", "App.java"), (".yml", "docker-compose.yml"),
            (".zip", "archive.zip"), (".png", "logo.png"), (".dockerfile", "Dockerfile"),
        };

        Host.UI.WriteLine($"  {dim}── Files ──{reset}");
        foreach (var (ext, filename) in sampleExtensions)
        {
            iconTheme.FileExtensions.TryGetValue(ext, out var iconName);
            iconName ??= iconTheme.DefaultFileIcon;

            colorTheme.FileExtensions.TryGetValue(ext, out var color);
            color ??= colorTheme.DefaultFileColor;

            var glyph = glyphs.GetValueOrDefault(iconName) ?? "?";
            Host.UI.WriteLine($"    {color}{glyph}  {filename}{reset}");
        }
        Host.UI.WriteLine("");

        // Sample directories
        var sampleDirs = new[] { ".git", "src", "node_modules", "docs", "tests", ".github", "bin" };

        Host.UI.WriteLine($"  {dim}── Directories ──{reset}");
        foreach (var dir in sampleDirs)
        {
            iconTheme.DirectoryNames.TryGetValue(dir, out var iconName);
            iconName ??= iconTheme.DefaultDirectoryIcon;

            colorTheme.DirectoryNames.TryGetValue(dir, out var color);
            color ??= colorTheme.DefaultDirectoryColor;

            var glyph = glyphs.GetValueOrDefault(iconName) ?? "?";
            Host.UI.WriteLine($"    {color}{glyph}  {dir}{reset}");
        }
        Host.UI.WriteLine("");

        // Stats
        Host.UI.WriteLine($"  {dim}Extension mappings:{reset}  {iconTheme.FileExtensions.Count} icons, {colorTheme.FileExtensions.Count} colors");
        Host.UI.WriteLine($"  {dim}Directory mappings:{reset}  {iconTheme.DirectoryNames.Count} icons, {colorTheme.DirectoryNames.Count} colors");
        Host.UI.WriteLine($"  {dim}Well-known files:{reset}    {iconTheme.WellKnownFiles.Count} icons, {colorTheme.WellKnownFiles.Count} colors");
        Host.UI.WriteLine("");
    }
}
