using FluentAssertions;
using MosaicShell.Core.HostPlatform;
using MosaicShell.Core.Services;

namespace MosaicShell.Core.Tests
{
    public class ServiceContractTests
    {
        [Fact]
        public void Fake_audio_roundtrips_volume_and_mute()
        {
            FakeAudioService audio = new() { MasterVolume = 0.4, IsMuted = false };
            audio.MasterVolume = 0.8;
            audio.IsMuted = true;
            _ = audio.MasterVolume.Should().BeApproximately(0.8, 0.001);
            _ = audio.IsMuted.Should().BeTrue();
        }

        [Fact]
        public void Fake_metrics_returns_machine_and_disks()
        {
            SystemMetricsSnapshot snap = new FakeSystemMetricsService().Sample();
            _ = snap.MachineName.Should().NotBeNullOrWhiteSpace();
            _ = snap.Disks.Should().NotBeEmpty();
        }

        [Fact]
        public async Task Fake_media_playpause_toggles_flag()
        {
            FakeMediaSessionService media = new()
            {
                Current = new MediaSessionInfo("Song", "Artist", "app", false)
            };
            await media.PlayPauseAsync();
            _ = media.Current.IsPlaying.Should().BeTrue();
        }

        [Fact]
        public void Fake_app_audio_set_volume()
        {
            FakeAppAudioService apps = new();
            apps.Sessions.Add(new AppAudioSession("1", "Browser", 0.5, false));
            apps.SetVolume("1", 0.25);
            _ = apps.GetSessions()[0].Volume.Should().BeApproximately(0.25, 0.001);
        }

        [Fact]
        public void Fake_audio_levels_expose_bands()
        {
            FakeAudioLevelService levels = new();
            _ = levels.Bands.Should().HaveCount(16);
        }

        [Fact]
        public void Windows_services_factory_constructs()
        {
            if (!WindowsAudioEndpointPolicy.HasDefaultRenderEndpoint)
            {
                // GitHub Actions and other headless runners often lack a default render endpoint (HRESULT 0x80070490).
                using WindowsSystemMetricsService metrics = new();
                _ = metrics.Sample().MachineName.Should().Be(Environment.MachineName);
                return;
            }

            using HostServices hub = HostServices.CreateWindowsDefaults();
            _ = hub.Metrics.Sample().MachineName.Should().Be(Environment.MachineName);
        }
    }
}
