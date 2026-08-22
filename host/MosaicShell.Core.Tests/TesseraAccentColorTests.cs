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
}
