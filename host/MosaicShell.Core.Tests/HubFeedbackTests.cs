using FluentAssertions;
using MosaicShell.Core.HostPlatform;
using MosaicShell.Core.HubShell;
using MosaicShell.Core.Services;

namespace MosaicShell.Core.Tests
{
    /// <summary>
    /// Feedback in three places instead of one status line: a toast for a finished action (with Undo where it applies),
    /// a banner on Home for something that needs attention, and the Settings page laid out the same on every desktop,
    /// in words that never promise a tray GNOME does not have.
    /// </summary>
    public sealed class HubFeedbackTests
    {
        private static readonly DesktopFeatures Windows = DesktopFeatures.Resolve(true, null);
        private static readonly DesktopFeatures Gnome = DesktopFeatures.Resolve(false, "GNOME");

        [Fact]
        public void A_toast_for_a_finished_action_goes_by_itself()
        {
            HubToast toast = HubToast.Done("Saved.");

            _ = toast.Message.Should().Be("Saved.");
            _ = toast.UndoLabel.Should().BeNull();
            _ = toast.StaysMs.Should().Be(HubToast.PlainMs);
        }

        [Fact]
        public void A_toast_with_Undo_stays_long_enough_to_use_it()
        {
            HubToast toast = HubToast.Undoable("Removed Chrono.");

            _ = toast.UndoLabel.Should().Be("Undo");
            _ = toast.StaysMs.Should().BeGreaterThan(HubToast.PlainMs);
        }

        [Fact]
        public void A_module_that_failed_to_start_puts_a_banner_on_Home_pointing_at_its_page()
        {
            _ = HubBanner.ModuleFailed("Tessera").Should().Be("Tessera could not start. Open Tessera for details.");
        }

        [Fact]
        public void An_update_banner_names_the_version()
        {
            _ = HubBanner.UpdateReady("1.4.0").Should().Be("MosaicShell 1.4.0 is ready to install.");
        }

        [Fact]
        public void Settings_sections_are_in_the_order_people_need_them()
        {
            _ = HubSettingsLayout.Sections.Should().Equal("General", "Updates", "Browser media", "Diagnostics");
            _ = HubSettingsLayout.FoldedByDefault("Diagnostics").Should().BeTrue();
            _ = HubSettingsLayout.FoldedByDefault("General").Should().BeFalse();
        }

        [Fact]
        public void Closing_offers_two_real_choices_each_saying_what_happens()
        {
            IReadOnlyList<CloseChoice> choices = HubSettingsLayout.CloseChoices(Windows);

            _ = choices.Select(c => c.KeepsRunning).Should().Equal(true, false);
            _ = choices[0].Label.Should().Be("Keep running in the tray");
            _ = choices[0].Consequence.Should().Be("Closing hides this window. Open it again from the tray icon.");
            _ = choices[1].Label.Should().Be("Quit MosaicShell");
            _ = choices[1].Consequence.Should().Be("Closing this window stops every module.");
        }

        [Fact]
        public void On_GNOME_keeping_running_says_there_is_no_tray_and_how_to_come_back()
        {
            CloseChoice keep = HubSettingsLayout.CloseChoices(Gnome)[0];

            _ = keep.Label.Should().Be("Keep running in the background");
            _ = keep.Consequence.Should().Be(
                "Closing hides this window. A tray icon is not available on GNOME yet, so open MosaicShell again from your apps to bring it back.");
        }

        [Fact]
        public void A_tray_is_a_desktop_feature_GNOME_lacks()
        {
            _ = Gnome.Has(DesktopFeature.Tray).Should().BeFalse();
            _ = Windows.Has(DesktopFeature.Tray).Should().BeTrue();
            _ = DesktopFeatures.Resolve(false, "KDE").Has(DesktopFeature.Tray).Should().BeTrue();
            _ = Gnome.MissingText(DesktopFeature.Tray).Should().Be("A tray icon is not available on GNOME yet.");
        }

        [Theory]
        [InlineData(true, "Check for updates")]
        [InlineData(false, "Check for updates")]
        public void Updates_has_one_action_on_every_OS(bool windows, string label)
        {
            _ = HubSettingsLayout.UpdateActionLabel.Should().Be(label);
            _ = HubSettingsLayout.UpdateHint(windows).Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public void On_Linux_the_update_hint_says_how_updating_works_there()
        {
            _ = HubSettingsLayout.UpdateHint(isWindows: false)
                .Should().Be("Checks GitHub for a newer release. On Linux, install it the way you installed MosaicShell.");
        }

        [Fact]
        public void Browser_media_says_whether_Grout_is_connected()
        {
            _ = HubSettingsLayout.BrowserMediaStatus(1).Should().Be("Grout is connected.");
            _ = HubSettingsLayout.BrowserMediaStatus(2).Should().Be("Grout is connected in 2 browsers.");
            _ = HubSettingsLayout.BrowserMediaStatus(0).Should().Be(
                "Grout is not connected. Install it in your browser, then open YouTube Music, YouTube, Spotify or SoundCloud.");
            _ = HubSettingsLayout.BrowserMediaStatus(null).Should().Be("This build does not read browser media.");
        }

        [Theory]
        [InlineData(2, GroutLight.Connected)]
        [InlineData(1, GroutLight.Connected)]
        [InlineData(0, GroutLight.Disconnected)]
        [InlineData(null, GroutLight.None)]
        public void Grouts_light_is_green_when_connected_and_red_when_not(int? connections, GroutLight light)
        {
            _ = HubSettingsLayout.GroutLightFor(connections).Should().Be(light);
        }

        [Fact]
        public void Grout_has_one_place_to_get_it()
        {
            _ = HubSettingsLayout.GroutStoreUrl.Should().StartWith("https://chromewebstore.google.com/detail/grout/");
            _ = HubSettingsLayout.OffersGroutInstall(0).Should().BeTrue();
            _ = HubSettingsLayout.OffersGroutInstall(1).Should().BeFalse();
            _ = HubSettingsLayout.OffersGroutInstall(null).Should().BeFalse();
        }

        [Fact]
        public void The_media_stack_counts_the_browsers_Grout_is_connected_in()
        {
            using CompositeMediaSessionService media = new(new FakeMediaSessionService(), new CountedSource(2), new CountedSource(1));

            _ = media.BrowserConnections.Should().Be(3);
        }

        private sealed class CountedSource(int connections) : IBrowserMediaSource, IBrowserConnectionStatus
        {
            public int BrowserConnections => connections;

            public BrowserPlayerSnapshot? Active => null;

            public event EventHandler? Changed { add { } remove { } }

            public Task SetLikedAsync(bool liked)
            {
                return Task.CompletedTask;
            }

            public Task SetDislikedAsync(bool disliked)
            {
                return Task.CompletedTask;
            }

            public Task ToggleShuffleAsync()
            {
                return Task.CompletedTask;
            }

            public Task ToggleRepeatAsync()
            {
                return Task.CompletedTask;
            }

            public void Dispose()
            {
            }
        }
    }
}
