using FluentAssertions;
using MosaicShell.Core.Services;

namespace MosaicShell.Core.Tests
{
    /// <summary>
    /// The service set a platform without its own implementations gets (not Windows or Linux). Every member must exist,
    /// do nothing harmful, and say plainly that the feature is unavailable instead of inventing data.
    /// </summary>
    public class UnsupportedPlatformServicesTests
    {
        [Fact]
        public void Every_service_is_present_and_disposes()
        {
            HostServices hub = HostServices.CreateUnsupportedPlatformDefaults();

            _ = hub.Audio.Should().NotBeNull();
            _ = hub.ShellFlyoutTriggers.Should().NotBeNull();
            hub.Dispose();
        }

        [Fact]
        public void Features_report_unavailable_rather_than_fake_data()
        {
            using HostServices hub = HostServices.CreateUnsupportedPlatformDefaults();

            _ = hub.Brightness.IsSupported.Should().BeFalse();
            _ = hub.Airplane.IsSupported.Should().BeFalse();
            _ = hub.Media.Current.Should().BeNull();
            _ = hub.AppAudio.GetSessions().Should().BeEmpty();
            _ = hub.AudioDevices.GetOutputDevices().Should().BeEmpty();
            _ = hub.Autostart.IsEnabled.Should().BeFalse();
            _ = hub.Hotkeys.Register("test", ModifierKeys.Control, 0x41, () => { }).Should().BeFalse();
        }

        [Fact]
        public void Audio_keeps_what_it_is_told_so_the_ui_stays_consistent()
        {
            using HostServices hub = HostServices.CreateUnsupportedPlatformDefaults();

            hub.Audio.MasterVolume = 0.4;
            hub.Audio.IsMuted = true;

            _ = hub.Audio.MasterVolume.Should().Be(0.4);
            _ = hub.Audio.IsMuted.Should().BeTrue();
        }

        [Fact]
        public void Audio_levels_are_silent_with_the_meter_band_count()
        {
            using HostServices hub = HostServices.CreateUnsupportedPlatformDefaults();

            _ = hub.AudioLevels.Peak.Should().Be(0);
            _ = hub.AudioLevels.Bands.Should().HaveCount(AudioLevelMeter.BandCount).And.OnlyContain(b => b == 0);
        }

        [Fact]
        public void Metrics_name_the_machine()
        {
            using HostServices hub = HostServices.CreateUnsupportedPlatformDefaults();

            _ = hub.Metrics.Sample().MachineName.Should().Be(Environment.MachineName);
        }

        [Fact]
        public async Task Media_commands_complete_without_a_session()
        {
            using HostServices hub = HostServices.CreateUnsupportedPlatformDefaults();

            hub.Media.PumpTimeline();
            await hub.Media.PlayPauseAsync();
            await hub.Media.SeekAsync(10);
        }

        [Fact]
        public void Platform_defaults_use_the_unsupported_set_where_there_is_no_platform_set()
        {
            if (OperatingSystem.IsWindows() || OperatingSystem.IsLinux())
            {
                return;
            }

            using HostServices hub = HostServices.CreatePlatformDefaults();
            _ = hub.Brightness.IsSupported.Should().BeFalse();
        }
    }
}
