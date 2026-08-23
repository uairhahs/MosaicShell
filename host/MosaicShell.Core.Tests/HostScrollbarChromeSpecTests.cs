using FluentAssertions;
using MosaicShell.Core.Modules;
using MosaicShell.Core.Modules.Tessera;

namespace MosaicShell.Core.Tests;

public class HostScrollbarChromeSpecTests
{
    [Fact]
    public void Thumb_is_slim_mocha_overlay_not_fluent_chunk()
    {
        HostScrollbarChromeSpec.Thickness.Should().BeInRange(6, 10);
        HostScrollbarChromeSpec.AutoHide.Should().BeTrue();
        HostScrollbarChromeSpec.HideLineButtons.Should().BeTrue();
        HostScrollbarChromeSpec.ThumbHex.Should().Be("#585B70");
        HostScrollbarChromeSpec.ThumbHoverHex.Should().Be("#6C7086");
        HostScrollbarChromeSpec.ThumbPressedHex.Should().Be("#7F849C");
        HostScrollbarChromeSpec.TrackHex.Should().Be("#00000000");
    }

    [Fact]
    public void Thumb_hex_values_parse()
    {
        var thumb = TesseraAccentColor.TryParse(HostScrollbarChromeSpec.ThumbHex, out _, out _, out _);
        var hover = TesseraAccentColor.TryParse(HostScrollbarChromeSpec.ThumbHoverHex, out _, out _, out _);
        var pressed = TesseraAccentColor.TryParse(HostScrollbarChromeSpec.ThumbPressedHex, out _, out _, out _);
        thumb.Should().BeTrue();
        hover.Should().BeTrue();
        pressed.Should().BeTrue();
    }

    [Fact]
    public void Chrome_argb_is_what_host_must_bind_not_hex_strings()
    {
        HostScrollbarChromeSpec.Thumb.Should().Be(new HostChromeArgb(255, 0x58, 0x5B, 0x70));
        HostScrollbarChromeSpec.ThumbHover.Should().Be(new HostChromeArgb(255, 0x6C, 0x70, 0x86));
        HostScrollbarChromeSpec.ThumbPressed.Should().Be(new HostChromeArgb(255, 0x7F, 0x84, 0x9C));
        HostScrollbarChromeSpec.Track.Should().Be(new HostChromeArgb(0, 0, 0, 0));
    }

    [Theory]
    [InlineData("#585B70", true, 255, 0x58, 0x5B, 0x70)]
    [InlineData("#00000000", true, 0, 0, 0, 0)]
    [InlineData("#80FFFFFF", true, 0x80, 0xFF, 0xFF, 0xFF)]
    [InlineData("not-hex", false, 0, 0, 0, 0)]
    public void TryParseArgb_accepts_rgb_and_argb(string hex, bool ok, int a, int r, int g, int b)
    {
        var parsed = HostScrollbarChromeSpec.TryParseArgb(hex, out var aa, out var rr, out var gg, out var bb);
        parsed.Should().Be(ok);
        if (!ok)
            return;
        aa.Should().Be((byte)a);
        rr.Should().Be((byte)r);
        gg.Should().Be((byte)g);
        bb.Should().Be((byte)b);
    }
}
