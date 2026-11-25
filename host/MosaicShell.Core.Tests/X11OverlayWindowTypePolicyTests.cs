using FluentAssertions;
using MosaicShell.Core.HostPlatform;

namespace MosaicShell.Core.Tests
{
    /// <summary>
    /// The EWMH window type the overlays (flyout, focus dim layer, click glint) declare. GNOME Shell plays its own open
    /// and close animations on NORMAL, DIALOG and MODAL_DIALOG windows (windowManager.js, GNOME 51), so on GNOME the
    /// layer shrank and faded on top of MosaicShell's own fade.
    /// </summary>
    public sealed class X11OverlayWindowTypePolicyTests
    {
        [Theory]
        [InlineData("GNOME")]
        [InlineData("ubuntu:GNOME")]
        [InlineData("GNOME-Classic:GNOME")]
        public void On_GNOME_the_overlays_are_utility_windows_which_it_does_not_animate(string desktop)
        {
            _ = X11OverlayWindowTypePolicy.WindowTypeFor(desktop).Should().Be("_NET_WM_WINDOW_TYPE_UTILITY");
        }

        [Theory]
        [InlineData("KDE")]
        [InlineData("")]
        [InlineData(null)]
        public void Elsewhere_the_overlays_keep_the_default_type(string? desktop)
        {
            // KWin hides utility windows while their application is not active, which would hide every flyout.
            _ = X11OverlayWindowTypePolicy.WindowTypeFor(desktop).Should().BeNull();
        }
    }
}
