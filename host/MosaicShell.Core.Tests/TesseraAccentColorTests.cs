using FluentAssertions;
using MosaicShell.Core.Modules.Tessera;

namespace MosaicShell.Core.Tests
{
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
            _ = TesseraAccentColor.TryParse(input, out byte rb, out byte gb, out byte bb).Should().Be(ok);
            if (ok)
            {
                _ = rb.Should().Be((byte)r);
                _ = gb.Should().Be((byte)g);
                _ = bb.Should().Be((byte)b);
            }
        }

        [Fact]
        public void NormalizeOrEmpty_uppercases_valid_hex()
        {
            _ = TesseraAccentColor.NormalizeOrEmpty("d8e2f8").Should().Be("#D8E2F8");
            _ = TesseraAccentColor.NormalizeOrEmpty("").Should().BeEmpty();
            _ = TesseraAccentColor.NormalizeOrEmpty("nope").Should().BeEmpty();
        }

        [Fact]
        public void Presets_start_with_system_then_curated_circles()
        {
            _ = TesseraAccentColor.Presets.Should().HaveCountGreaterThanOrEqualTo(10);
            _ = TesseraAccentColor.Presets[0].Name.Should().Be("System");
            _ = TesseraAccentColor.Presets[0].Hex.Should().BeEmpty();
            _ = TesseraAccentColor.Presets[0].IsSystem.Should().BeTrue();

            List<TesseraAccentPreset> colors = [.. TesseraAccentColor.Presets.Skip(1)];
            foreach (TesseraAccentPreset p in colors)
            {
                bool ok = TesseraAccentColor.TryParse(p.Hex, out _, out _, out _);
                _ = ok.Should().BeTrue($"preset {p.Name} hex {p.Hex}");
            }
            _ = colors.Select(p => TesseraAccentColor.NormalizeOrEmpty(p.Hex)).Should().OnlyHaveUniqueItems();
            _ = colors.Select(p => TesseraAccentColor.NormalizeOrEmpty(p.Hex)).Should().Contain("#0273CD");
        }

        [Fact]
        public void MatchesPreset_normalizes_hex_and_treats_blank_as_system()
        {
            TesseraAccentPreset blue = TesseraAccentColor.Presets.First(p => p.Hex.Equals("#0273CD", StringComparison.OrdinalIgnoreCase)
                                                             || TesseraAccentColor.NormalizeOrEmpty(p.Hex) == "#0273CD");
            _ = TesseraAccentColor.MatchesPreset("0273cd", blue).Should().BeTrue();
            _ = TesseraAccentColor.MatchesPreset("#0273CD", TesseraAccentColor.Presets[0]).Should().BeFalse();
            _ = TesseraAccentColor.MatchesPreset("", TesseraAccentColor.Presets[0]).Should().BeTrue();
            _ = TesseraAccentColor.MatchesPreset("  ", TesseraAccentColor.Presets[0]).Should().BeTrue();
        }
    }
}
