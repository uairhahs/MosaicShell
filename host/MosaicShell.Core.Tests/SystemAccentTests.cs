using FluentAssertions;
using MosaicShell.Core.Services;

namespace MosaicShell.Core.Tests
{
    /// <summary>
    /// Tessera's "System" accent: Windows' DWM colorization where it reads, otherwise the desktop's portal accent
    /// (Linux), otherwise the Windows blue. Before, Linux had only the DWM call, which cannot load there, so every
    /// Linux desktop got the fallback.
    /// </summary>
    public sealed class SystemAccentTests
    {
        private static readonly (byte A, byte R, byte G, byte B) Fallback = (255, 2, 115, 205);

        [Fact]
        public void A_readable_colorization_is_the_accent_with_an_empty_alpha_made_opaque()
        {
            _ = SystemAccent.Resolve(0xC40078D7, null).Should().Be((0xC4, 0x00, 0x78, 0xD7));
            _ = SystemAccent.Resolve(0x000078D7, null).Should().Be((255, 0x00, 0x78, 0xD7));
        }

        [Theory]
        [InlineData(0xFF000000u)]
        [InlineData(0xFFFFFFFFu)]
        public void A_near_black_or_near_white_colorization_is_not_used(uint dwm)
        {
            _ = SystemAccent.Resolve(dwm, null).Should().Be(Fallback);
        }

        [Fact]
        public void On_Linux_the_desktops_accent_is_used()
        {
            _ = SystemAccent.Resolve(null, (0x35, 0x84, 0xE4)).Should().Be((255, 0x35, 0x84, 0xE4));
        }

        [Fact]
        public void Without_either_the_Windows_blue_stands()
        {
            _ = SystemAccent.Resolve(null, null).Should().Be(Fallback);
        }

        [Fact]
        public void Without_a_desktop_watch_there_is_no_desktop_accent()
        {
            _ = SystemAccent.DesktopAccent.Should().BeNull("nothing started the watch in this process");
        }
    }
}
