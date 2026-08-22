using FluentAssertions;
using MosaicShell.Core.Services;

namespace MosaicShell.Core.Tests;

public class ServiceContractTests
{
    [Fact]
    public void Fake_audio_roundtrips_volume_and_mute()
    {
        var audio = new FakeAudioService { MasterVolume = 0.4, IsMuted = false };
        audio.MasterVolume = 0.8;
        audio.IsMuted = true;
        audio.MasterVolume.Should().BeApproximately(0.8, 0.001);
        audio.IsMuted.Should().BeTrue();
    }

    [Fact]
    public void Fake_metrics_returns_machine_and_disks()
    {
        var snap = new FakeSystemMetricsService().Sample();
        snap.MachineName.Should().NotBeNullOrWhiteSpace();
        snap.Disks.Should().NotBeEmpty();
    }

    [Fact]
    public void Fake_media_playpause_toggles_flag()
    {
        var media = new FakeMediaSessionService
        {
            Current = new MediaSessionInfo("Song", "Artist", "app", false)
        };
        media.PlayPauseAsync().GetAwaiter().GetResult();
        media.Current!.IsPlaying.Should().BeTrue();
    }

    [Fact]
    public void Fake_app_audio_set_volume()
    {
        var apps = new FakeAppAudioService();
        apps.Sessions.Add(new AppAudioSession("1", "Browser", 0.5, false));
        apps.SetVolume("1", 0.25);
        apps.GetSessions()[0].Volume.Should().BeApproximately(0.25, 0.001);
    }

    [Fact]
    public void Fake_audio_levels_expose_bands()
    {
        var levels = new FakeAudioLevelService();
        levels.Bands.Should().HaveCount(16);
    }

    [Fact]
    public void Windows_services_factory_constructs()
    {
        using var hub = HostServices.CreateWindowsDefaults();
        hub.Metrics.Sample().MachineName.Should().Be(Environment.MachineName);
    }
}
