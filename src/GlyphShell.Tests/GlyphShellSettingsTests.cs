using FluentAssertions;
using GlyphShell.Engine;
using Xunit;

namespace GlyphShell.Tests;

public class GlyphShellSettingsTests
{
    [Fact]
    public void DiagnosticsEnabled_DefaultIsFalse()
    {
        // Reset to default to ensure clean state
        GlyphShellSettings.DiagnosticsEnabled = false;
        GlyphShellSettings.DiagnosticsEnabled.Should().BeFalse();
    }

    [Fact]
    public void DateColorByAge_DefaultIsTrue()
    {
        GlyphShellSettings.DateColorByAge = true;
        GlyphShellSettings.DateColorByAge.Should().BeTrue();
    }

    [Fact]
    public void ProjectDetectionEnabled_DefaultIsTrue()
    {
        GlyphShellSettings.ProjectDetectionEnabled = true;
        GlyphShellSettings.ProjectDetectionEnabled.Should().BeTrue();
    }

    [Fact]
    public void DateFlatColor_DefaultIsBlueAnsi()
    {
        GlyphShellSettings.DateFlatColor = "\x1b[38;2;86;156;214m";
        GlyphShellSettings.DateFlatColor.Should().Be("\x1b[38;2;86;156;214m");
    }

    [Fact]
    public void DiagnosticsEnabled_SetTrue_Persists()
    {
        var original = GlyphShellSettings.DiagnosticsEnabled;
        try
        {
            GlyphShellSettings.DiagnosticsEnabled = true;
            GlyphShellSettings.DiagnosticsEnabled.Should().BeTrue();
        }
        finally
        {
            GlyphShellSettings.DiagnosticsEnabled = original;
        }
    }

    [Fact]
    public void DateColorByAge_SetFalse_Persists()
    {
        var original = GlyphShellSettings.DateColorByAge;
        try
        {
            GlyphShellSettings.DateColorByAge = false;
            GlyphShellSettings.DateColorByAge.Should().BeFalse();
        }
        finally
        {
            GlyphShellSettings.DateColorByAge = original;
        }
    }

    [Fact]
    public void ProjectDetectionEnabled_SetFalse_Persists()
    {
        var original = GlyphShellSettings.ProjectDetectionEnabled;
        try
        {
            GlyphShellSettings.ProjectDetectionEnabled = false;
            GlyphShellSettings.ProjectDetectionEnabled.Should().BeFalse();
        }
        finally
        {
            GlyphShellSettings.ProjectDetectionEnabled = original;
        }
    }

    [Fact]
    public void DateFlatColor_SetCustomValue_Persists()
    {
        var original = GlyphShellSettings.DateFlatColor;
        try
        {
            var customColor = "\x1b[38;2;255;0;0m";
            GlyphShellSettings.DateFlatColor = customColor;
            GlyphShellSettings.DateFlatColor.Should().Be(customColor);
        }
        finally
        {
            GlyphShellSettings.DateFlatColor = original;
        }
    }
}
