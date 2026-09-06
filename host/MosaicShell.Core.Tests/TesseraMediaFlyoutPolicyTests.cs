using FluentAssertions;
using MosaicShell.Core.Modules.Tessera;
using MosaicShell.Core.Services;

namespace MosaicShell.Core.Tests
{
    public class TesseraMediaFlyoutPolicyTests
    {
        [Theory]
        [InlineData(true, false, "", TesseraMediaChangeAction.PresentMediaFlyout)]
        [InlineData(true, false, "vol", TesseraMediaChangeAction.PresentMediaFlyout)]
        [InlineData(true, false, "media", TesseraMediaChangeAction.PresentMediaFlyout)]
        [InlineData(false, false, "", TesseraMediaChangeAction.Ignore)]
        [InlineData(true, true, "vol", TesseraMediaChangeAction.SoftRefreshVisible)]
        [InlineData(true, true, "bright", TesseraMediaChangeAction.SoftRefreshVisible)]
        [InlineData(true, true, "media", TesseraMediaChangeAction.PresentMediaFlyout)]
        // A visible status chip owns the session: presenting media paints its shell over the chip.
        [InlineData(true, true, "locks", TesseraMediaChangeAction.Ignore)]
        [InlineData(true, true, "flight", TesseraMediaChangeAction.Ignore)]
        [InlineData(false, true, "locks", TesseraMediaChangeAction.Ignore)]
        [InlineData(false, true, "vol", TesseraMediaChangeAction.SoftRefreshVisible)]
        public void Resolve_track_change_actions(
            bool enableMedia,
            bool visible,
            string lastKind,
            TesseraMediaChangeAction expected)
        {
            _ = TesseraMediaFlyoutPolicy.Resolve(enableMedia, visible, lastKind).Should().Be(expected);
        }

        /// <summary>
        /// Regression: caps-lock chips were permanently masked because a media track change
        /// presented the media shell ~250ms after the chip appeared, covering it. A visible
        /// status flyout must suppress the media present entirely.
        /// </summary>
        [Theory]
        [InlineData("locks")]
        [InlineData("flight")]
        public void Visible_status_chip_is_never_masked_by_a_media_present(string statusKind)
        {
            _ = TesseraMediaFlyoutPolicy.Resolve(
                    enableMediaFlyouts: true, flyoutVisible: true, lastKind: statusKind)
                .Should().Be(TesseraMediaChangeAction.Ignore);

            _ = TesseraMediaFlyoutPolicy.IsStripKind(statusKind).Should().BeFalse();
            _ = TesseraStatusFlyoutPolicy.IsStatusKind(statusKind).Should().BeTrue();
        }

        [Fact]
        public void Timeline_poll_owned_by_host_services_not_tessera_module()
        {
            _ = TesseraMediaFlyoutPolicy.MustPollTimelineWhileArmed.Should().BeFalse(
                "CapabilityDaemon MediaSessionPlatform + IMediaSessionService own polling");
            _ = MediaSessionChangePolicy.MustPollTimelineIndependentlyOfFlyout.Should().BeTrue();
            _ = MediaSessionChangePolicy.TimelinePollMs.Should().BePositive();
        }
    }
}
