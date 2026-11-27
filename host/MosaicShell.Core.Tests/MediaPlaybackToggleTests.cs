using FluentAssertions;
using MosaicShell.Core.Capabilities.Platform;
using MosaicShell.Core.HostPlatform;
using MosaicShell.Core.Services;

namespace MosaicShell.Core.Tests
{
    /// <summary>
    /// GNOME gives MosaicShell no media keys, so a play/pause reaches it only as the player changing state. On such a
    /// desktop that change stands in for the key press, or the media flyout never opens again once one has closed.
    /// </summary>
    public class MediaPlaybackToggleTests
    {
        [Fact]
        public void Pausing_and_resuming_the_same_track_are_playback_toggles_not_track_changes()
        {
            (FakeMediaSessionService media, List<MediaSessionSignal> signals, MediaSessionPlatform platform) = Start(
                new MediaSessionInfo("Fresh Bloom", "Yaahn Hunter Jr.", "edge", true));
            using (platform)
            {
                media.Current = new MediaSessionInfo("Fresh Bloom", "Yaahn Hunter Jr.", "edge", false);
                media.Current = new MediaSessionInfo("Fresh Bloom", "Yaahn Hunter Jr.", "edge", true);

                _ = signals.Select(s => (s.IsPlaybackToggle, s.IsTrackBoundary)).Should().Equal((true, false), (true, false));
            }
        }

        [Fact]
        public void A_change_that_keeps_the_playing_state_is_not_a_toggle()
        {
            (FakeMediaSessionService media, List<MediaSessionSignal> signals, MediaSessionPlatform platform) = Start(
                new MediaSessionInfo("Fresh Bloom", "Yaahn Hunter Jr.", "edge", true));
            using (platform)
            {
                media.Current = new MediaSessionInfo("Fresh Bloom", "Yaahn Hunter Jr. & Magie Hue", "edge", true);

                _ = signals.Single().IsPlaybackToggle.Should().BeFalse();
            }
        }

        [Fact]
        public void A_player_appearing_is_not_a_toggle()
        {
            (FakeMediaSessionService media, List<MediaSessionSignal> signals, MediaSessionPlatform platform) = Start(null);
            using (platform)
            {
                media.Current = new MediaSessionInfo("Fresh Bloom", "Yaahn Hunter Jr.", "edge", true);

                _ = signals.Single().IsPlaybackToggle.Should().BeFalse();
            }
        }

        [Fact]
        public void Without_a_media_key_route_a_toggle_stands_in_for_the_key()
        {
            MediaSessionSignal toggle = new(null, MediaSessionSignalKind.SessionChanged, false, IsPlaybackToggle: true);

            _ = MediaKeyStandInPolicy.Trigger(toggle, hasMediaKeyRoute: false).Should().Be(FlyoutSyncTrigger.ShellMedia);
        }

        [Fact]
        public void With_a_media_key_route_the_key_itself_opens_the_flyout()
        {
            MediaSessionSignal toggle = new(null, MediaSessionSignalKind.SessionChanged, false, IsPlaybackToggle: true);

            _ = MediaKeyStandInPolicy.Trigger(toggle, hasMediaKeyRoute: true).Should().Be(FlyoutSyncTrigger.MediaSession);
        }

        [Fact]
        public void Any_other_change_stays_a_media_session_change()
        {
            MediaSessionSignal change = new(null, MediaSessionSignalKind.SessionChanged, false);

            _ = MediaKeyStandInPolicy.Trigger(change, hasMediaKeyRoute: false).Should().Be(FlyoutSyncTrigger.MediaSession);
        }

        [Fact]
        public void A_stood_in_key_reopens_the_flyout_after_a_dismiss()
        {
            _ = FlyoutAutoPresentPolicy.ShouldColdPresent(suppressAfterUserDismiss: true, FlyoutSyncTrigger.ShellMedia).Should().BeTrue();
            _ = FlyoutAutoPresentPolicy.ShouldColdPresent(suppressAfterUserDismiss: true, FlyoutSyncTrigger.MediaSession).Should().BeFalse();
        }

        [Theory]
        [InlineData(true, null, true)]
        [InlineData(false, "KDE", true)]
        [InlineData(false, "GNOME", false)]
        [InlineData(false, "ubuntu:GNOME", false)]
        [InlineData(false, "Hyprland", false)]
        [InlineData(false, null, false)]
        public void Media_keys_reach_MosaicShell_on_Windows_and_Plasma_only(bool windows, string? desktop, bool expected)
        {
            _ = DesktopFeatures.Resolve(windows, desktop).Has(DesktopFeature.MediaKeys).Should().Be(expected);
        }

        [Theory]
        [InlineData(true, null)]
        [InlineData(false, "KDE")]
        public void Windows_and_Plasma_have_every_feature(bool windows, string? desktop)
        {
            DesktopFeatures features = DesktopFeatures.Resolve(windows, desktop);

            _ = Enum.GetValues<DesktopFeature>().Should().OnlyContain(f => features.Has(f));
        }

        [Fact]
        public void GNOME_has_no_global_hotkeys_media_keys_or_full_screen_probe_yet()
        {
            DesktopFeatures gnome = DesktopFeatures.Resolve(false, "GNOME");

            // Its one feature is the tray: MosaicShell's own Shell extension puts an icon in the top bar.
            _ = Enum.GetValues<DesktopFeature>().Where(f => f != DesktopFeature.Tray).Should().OnlyContain(f => !gnome.Has(f));
            _ = gnome.Has(DesktopFeature.Tray).Should().BeTrue();
        }

        private static (FakeMediaSessionService, List<MediaSessionSignal>, MediaSessionPlatform) Start(MediaSessionInfo? initial)
        {
            FakeMediaSessionService media = new() { Current = initial };
            MediaSessionPlatform platform = new(media);
            List<MediaSessionSignal> signals = [];
            platform.Signal += signals.Add;
            platform.Acquire();
            return (media, signals, platform);
        }
    }
}
