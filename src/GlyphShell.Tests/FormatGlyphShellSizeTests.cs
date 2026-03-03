using FluentAssertions;
using GlyphShell.Engine;
using Xunit;

namespace GlyphShell.Tests;

/// <summary>
/// Tests the size formatting logic used by FormatGlyphShellSizeCmdlet.
/// Since the formatting is inline in the cmdlet's ProcessRecord, we replicate
/// the pure computation here to validate the unit conversion and format patterns.
/// </summary>
public class FormatGlyphShellSizeTests
{
    // Replicates the cmdlet's unit conversion logic for testability
    private static (double Value, string Unit) ConvertToUnit(long bytes) => bytes switch
    {
        < 1024L => (bytes, "B"),
        < 1024L * 1024 => (bytes / 1024.0, "K"),
        < 1024L * 1024 * 1024 => (bytes / (1024.0 * 1024), "M"),
        < 1024L * 1024 * 1024 * 1024 => (bytes / (1024.0 * 1024 * 1024), "G"),
        _ => (bytes / (1024.0 * 1024 * 1024 * 1024), "T"),
    };

    private static string FormatSize(long bytes)
    {
        var (value, unit) = ConvertToUnit(bytes);

        // Color selection matches cmdlet logic
        var color = bytes switch
        {
            < 1024L => "\x1b[38;2;100;200;100m",
            < 100 * 1024L => "\x1b[38;2;140;200;140m",
            < 1024L * 1024 => "\x1b[38;2;200;200;100m",
            < 10 * 1024L * 1024 => "\x1b[38;2;220;180;80m",
            < 100 * 1024L * 1024 => "\x1b[38;2;220;140;60m",
            < 1024L * 1024 * 1024 => "\x1b[38;2;220;100;60m",
            _ => "\x1b[38;2;220;60;60m",
        };

        if (unit == "B")
            return $"{color}{bytes,6} B{ColorEngine.Reset}";
        else if (value >= 100)
            return $"{color}{value,5:F0} {unit}{ColorEngine.Reset}";
        else if (value >= 10)
            return $"{color}{value,5:F1} {unit}{ColorEngine.Reset}";
        else
            return $"{color}{value,5:F2} {unit}{ColorEngine.Reset}";
    }

    [Theory]
    [InlineData(0, "B")]
    [InlineData(100, "B")]
    [InlineData(1023, "B")]
    public void UnitConversion_ByteRange_ReturnsB(long bytes, string expectedUnit)
    {
        var (_, unit) = ConvertToUnit(bytes);
        unit.Should().Be(expectedUnit);
    }

    [Theory]
    [InlineData(1024, "K")]
    [InlineData(500_000, "K")]
    public void UnitConversion_KilobyteRange_ReturnsK(long bytes, string expectedUnit)
    {
        var (_, unit) = ConvertToUnit(bytes);
        unit.Should().Be(expectedUnit);
    }

    [Theory]
    [InlineData(1_048_576, "M")]       // 1 MB
    [InlineData(500_000_000, "M")]     // ~477 MB
    public void UnitConversion_MegabyteRange_ReturnsM(long bytes, string expectedUnit)
    {
        var (_, unit) = ConvertToUnit(bytes);
        unit.Should().Be(expectedUnit);
    }

    [Fact]
    public void UnitConversion_GigabyteRange_ReturnsG()
    {
        var (_, unit) = ConvertToUnit(1_073_741_824); // 1 GB
        unit.Should().Be("G");
    }

    [Fact]
    public void UnitConversion_TerabyteRange_ReturnsT()
    {
        var (_, unit) = ConvertToUnit(1_099_511_627_776); // 1 TB
        unit.Should().Be("T");
    }

    [Fact]
    public void UnitConversion_ExactKilobyte_ReturnsValueOf1()
    {
        var (value, unit) = ConvertToUnit(1024);
        unit.Should().Be("K");
        value.Should().Be(1.0);
    }

    [Fact]
    public void UnitConversion_ExactMegabyte_ReturnsValueOf1()
    {
        var (value, unit) = ConvertToUnit(1024 * 1024);
        unit.Should().Be("M");
        value.Should().Be(1.0);
    }

    [Fact]
    public void FormatSize_Bytes_ContainsAnsiColorAndReset()
    {
        var result = FormatSize(512);
        result.Should().StartWith("\x1b[");
        result.Should().EndWith(ColorEngine.Reset);
        result.Should().Contain("B");
    }

    [Fact]
    public void FormatSize_Kilobytes_ContainsKUnit()
    {
        var result = FormatSize(2048);
        result.Should().Contain("K");
    }

    [Fact]
    public void FormatSize_Megabytes_ContainsMUnit()
    {
        var result = FormatSize(5 * 1024 * 1024);
        result.Should().Contain("M");
    }

    [Fact]
    public void FormatSize_SmallBytes_UsesGreenColor()
    {
        var result = FormatSize(100);
        result.Should().Contain("\x1b[38;2;100;200;100m");
    }

    [Fact]
    public void FormatSize_LargeFile_UsesRedColor()
    {
        var result = FormatSize(2L * 1024 * 1024 * 1024);
        result.Should().Contain("\x1b[38;2;220;60;60m");
    }

    [Fact]
    public void FormatSize_Zero_FormatsAsBytes()
    {
        var result = FormatSize(0);
        result.Should().Contain("B");
    }
}
