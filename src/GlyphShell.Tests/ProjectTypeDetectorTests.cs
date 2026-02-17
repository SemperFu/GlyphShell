using FluentAssertions;
using GlyphShell.Engine;
using Xunit;

namespace GlyphShell.Tests;

public class ProjectTypeDetectorTests : IDisposable
{
    private readonly List<string> _tempDirs = [];

    private DirectoryInfo CreateTempDir(params string[] markerFiles)
    {
        var dir = Directory.CreateTempSubdirectory("GlyphShellTest_");
        _tempDirs.Add(dir.FullName);

        foreach (var file in markerFiles)
        {
            var path = Path.Combine(dir.FullName, file);
            // If marker is a directory name (e.g. "ProjectSettings"), create as directory
            if (file == "ProjectSettings")
                Directory.CreateDirectory(path);
            else
                File.WriteAllText(path, "");
        }

        return dir;
    }

    [Fact]
    public void Detect_CsprojFile_ReturnsDotnet()
    {
        var dir = CreateTempDir("MyApp.csproj");
        var result = ProjectTypeDetector.Detect(dir);

        result.Should().NotBeNull();
        result!.Value.IconName.Should().Be("nf-seti-c_sharp");
    }

    [Fact]
    public void Detect_SlnFile_ReturnsDotnet()
    {
        var dir = CreateTempDir("MyApp.sln");
        var result = ProjectTypeDetector.Detect(dir);

        result.Should().NotBeNull();
        result!.Value.IconName.Should().Be("nf-seti-c_sharp");
    }

    [Fact]
    public void Detect_PackageJson_ReturnsNodejs()
    {
        var dir = CreateTempDir("package.json");
        var result = ProjectTypeDetector.Detect(dir);

        result.Should().NotBeNull();
        result!.Value.IconName.Should().Be("nf-dev-nodejs_small");
    }

    [Fact]
    public void Detect_TsconfigAndPackageJson_ReturnsTypescript()
    {
        // TypeScript has higher priority than Node.js
        var dir = CreateTempDir("package.json", "tsconfig.json");
        var result = ProjectTypeDetector.Detect(dir);

        result.Should().NotBeNull();
        result!.Value.IconName.Should().Be("nf-seti-typescript");
    }

    [Fact]
    public void Detect_CargoToml_ReturnsRust()
    {
        var dir = CreateTempDir("Cargo.toml");
        var result = ProjectTypeDetector.Detect(dir);

        result.Should().NotBeNull();
        result!.Value.IconName.Should().Be("nf-dev-rust");
    }

    [Fact]
    public void Detect_GoMod_ReturnsGo()
    {
        var dir = CreateTempDir("go.mod");
        var result = ProjectTypeDetector.Detect(dir);

        result.Should().NotBeNull();
        result!.Value.IconName.Should().Be("nf-dev-go");
    }

    [Fact]
    public void Detect_PyprojectToml_ReturnsPython()
    {
        var dir = CreateTempDir("pyproject.toml");
        var result = ProjectTypeDetector.Detect(dir);

        result.Should().NotBeNull();
        result!.Value.IconName.Should().Be("nf-dev-python");
    }

    [Fact]
    public void Detect_Gemfile_ReturnsRuby()
    {
        var dir = CreateTempDir("Gemfile");
        var result = ProjectTypeDetector.Detect(dir);

        result.Should().NotBeNull();
        result!.Value.IconName.Should().Be("nf-dev-ruby");
    }

    [Fact]
    public void Detect_Dockerfile_ReturnsDocker()
    {
        var dir = CreateTempDir("Dockerfile");
        var result = ProjectTypeDetector.Detect(dir);

        result.Should().NotBeNull();
        result!.Value.IconName.Should().Be("nf-dev-docker");
    }

    [Fact]
    public void Detect_EmptyDirectory_ReturnsNull()
    {
        var dir = CreateTempDir();
        var result = ProjectTypeDetector.Detect(dir);

        result.Should().BeNull();
    }

    [Fact]
    public void Detect_NoMarkerFiles_ReturnsNull()
    {
        var dir = CreateTempDir("readme.md", "notes.txt", "data.csv");
        var result = ProjectTypeDetector.Detect(dir);

        result.Should().BeNull();
    }

    [Fact]
    public void Detect_ResultHasNonEmptyColorSequence()
    {
        var dir = CreateTempDir("go.mod");
        var result = ProjectTypeDetector.Detect(dir);

        result.Should().NotBeNull();
        result!.Value.ColorSequence.Should().StartWith("\x1b[");
        result!.Value.ColorSequence.Should().EndWith("m");
    }

    [Fact]
    public void Detect_HigherPriorityWins_DotnetOverNodejs()
    {
        // dotnet priority=5, nodejs priority=2
        var dir = CreateTempDir("package.json", "MyApp.csproj");
        var result = ProjectTypeDetector.Detect(dir);

        result.Should().NotBeNull();
        result!.Value.IconName.Should().Be("nf-seti-c_sharp");
    }

    [Fact]
    public void Detect_WhenProjectDetectionDisabled_ReturnsNull()
    {
        var original = GlyphShellSettings.ProjectDetectionEnabled;
        try
        {
            GlyphShellSettings.ProjectDetectionEnabled = false;
            var dir = CreateTempDir("package.json");
            var result = ProjectTypeDetector.Detect(dir);

            result.Should().BeNull();
        }
        finally
        {
            GlyphShellSettings.ProjectDetectionEnabled = original;
        }
    }

    public void Dispose()
    {
        foreach (var dir in _tempDirs)
        {
            try { Directory.Delete(dir, recursive: true); }
            catch { /* best effort cleanup */ }
        }
    }
}
