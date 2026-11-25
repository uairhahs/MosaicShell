using FluentAssertions;
using MosaicShell.Core.Services.Xdg;

namespace MosaicShell.Core.Tests
{
    /// <summary>
    /// Which desktop this is, from XDG_CURRENT_DESKTOP (a colon-separated list, most specific first), for the routes
    /// that differ between Plasma and GNOME: lock keys, idle, airplane mode, the pop-up suppressor and the overlay
    /// window type.
    /// </summary>
    public sealed class XdgCurrentDesktopTests
    {
        [Theory]
        [InlineData("KDE", true, false)]
        [InlineData("GNOME", false, true)]
        [InlineData("ubuntu:GNOME", false, true)]
        [InlineData("GNOME-Classic:GNOME", false, true)]
        [InlineData("kde", true, false)]
        [InlineData("X-Cinnamon", false, false)]
        [InlineData("", false, false)]
        [InlineData(null, false, false)]
        public void A_desktop_is_named_by_any_entry_of_the_list(string? value, bool plasma, bool gnome)
        {
            _ = XdgCurrentDesktop.IsPlasma(value).Should().Be(plasma);
            _ = XdgCurrentDesktop.IsGnome(value).Should().Be(gnome);
        }

        [Fact]
        public void The_variable_is_the_one_the_desktops_set()
        {
            _ = XdgCurrentDesktop.Variable.Should().Be("XDG_CURRENT_DESKTOP");
        }
    }
}
