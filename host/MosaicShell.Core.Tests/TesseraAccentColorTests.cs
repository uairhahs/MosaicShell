using MosaicShell.Core.Modules.Tessera;
using FluentAssertions;
using MosaicShell.Core.Settings;

namespace MosaicShell.Core.Tests;

public class TesseraAccentColorTests
{
    [Theory]
    [InlineData(null, false, 0, 0, 0)]
    [InlineData("", false, 0, 0, 0)]
    [InlineData("  ", false, 0, 0, 0)]
    [InlineData("#0273CD", true, 0x02, 0x73, 0xCD)]
    [InlineData("0273CD", true, 0x02, 0x73, 0xCD)]
    [InlineData("D8E2F8", true, 0xD8, 0xE2, 0xF8)]
    [InlineData("#GGGGGG", false, 0, 0, 0)]
    public void TryParse_accepts_hex(string? input, bool ok, int r, int g, int b)
    {
        TesseraAccentColor.TryParse(input, out var rb, out var gb, out var bb).Should().Be(ok);
        if (ok)
        {
            rb.Should().Be((byte)r);
            gb.Should().Be((byte)g);
            bb.Should().Be((byte)b);
        }
    }

    [Fact]
    public void NormalizeOrEmpty_uppercases_valid_hex()
    {
        TesseraAccentColor.NormalizeOrEmpty("d8e2f8").Should().Be("#D8E2F8");
        TesseraAccentColor.NormalizeOrEmpty("").Should().BeEmpty();
        TesseraAccentColor.NormalizeOrEmpty("nope").Should().BeEmpty();
    }

    [Fact]
    public void Presets_start_with_system_then_curated_circles()
    {
        TesseraAccentColor.Presets.Should().HaveCountGreaterThanOrEqualTo(10);
        TesseraAccentColor.Presets[0].Name.Should().Be("System");
        TesseraAccentColor.Presets[0].Hex.Should().BeEmpty();
        TesseraAccentColor.Presets[0].IsSystem.Should().BeTrue();

        var colors = TesseraAccentColor.Presets.Skip(1).ToList();
        foreach (var p in colors)
        {
            var ok = TesseraAccentColor.TryParse(p.Hex, out _, out _, out _);
            ok.Should().BeTrue($"preset {p.Name} hex {p.Hex}");
        }
        colors.Select(p => TesseraAccentColor.NormalizeOrEmpty(p.Hex)).Should().OnlyHaveUniqueItems();
        colors.Select(p => TesseraAccentColor.NormalizeOrEmpty(p.Hex)).Should().Contain("#0273CD");
    }

    [Fact]
    public void MatchesPreset_normalizes_hex_and_treats_blank_as_system()
    {
        var blue = TesseraAccentColor.Presets.First(p => p.Hex.Equals("#0273CD", StringComparison.OrdinalIgnoreCase)
                                                         || TesseraAccentColor.NormalizeOrEmpty(p.Hex) == "#0273CD");
        TesseraAccentColor.MatchesPreset("0273cd", blue).Should().BeTrue();
        TesseraAccentColor.MatchesPreset("#0273CD", TesseraAccentColor.Presets[0]).Should().BeFalse();
        TesseraAccentColor.MatchesPreset("", TesseraAccentColor.Presets[0]).Should().BeTrue();
        TesseraAccentColor.MatchesPreset("  ", TesseraAccentColor.Presets[0]).Should().BeTrue();
    }
}
