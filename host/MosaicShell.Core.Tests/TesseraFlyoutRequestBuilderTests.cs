using FluentAssertions;
using MosaicShell.Core.Capabilities;
using MosaicShell.Core.Runtime;
using MosaicShell.Core.Services;
using MosaicShell.Core.Settings;
using MosaicShell.Core.Styles;

namespace MosaicShell.Core.Tests;

public class TesseraFlyoutRequestBuilderTests
{
    [Fact]
    public void BuildPayload_matches_capability_shape_for_volume()
    {
        var services = HostServicesFakes.Create();
        services.Audio.MasterVolume = 0.62;
        var settings = new TesseraSettings { Style = "Fluent", ShowMediaStripOnVolume = true };
        var builder = new TesseraFlyoutRequestBuilder();

        var request = builder.Build(services, settings, "vol");
        request.ModuleId.Should().Be("Tessera");
        request.Kind.Should().Be("vol");
        request.StyleId.Should().Be("Fluent");
        request.Payload!["volume"].Should().Be("0.62");
        request.Payload["showMediaStrip"].Should().Be(
            TesseraLayoutCoverage.UsesStackedMediaStrip("Fluent") ? "1" : "0");
    }

    [Fact]
    public void BuildLivePayload_honors_show_media_strip_override()
    {
        var services = HostServicesFakes.Create();
        var settings = new TesseraSettings { Style = "Pixel", ShowMediaStripOnVolume = true };
        var builder = new TesseraFlyoutRequestBuilder();

        builder.BuildLivePayload(services, settings, showMediaStripOverride: false)["showMediaStrip"]
            .Should().Be("0");
    }

    [Fact]
    public void BuildPayload_does_not_overwrite_explicit_lock_state()
    {
        var services = HostServicesFakes.Create();
        var builder = new TesseraFlyoutRequestBuilder();
        var stale = new LockKeyState(LockKeyKind.CapsLock, true);

        var payload = builder.BuildPayload(
            services,
            new TesseraSettings(),
            "locks",
            new Dictionary<string, string> { ["lock"] = "CapsLock", ["on"] = "0" },
            stale);

        payload["on"].Should().Be("0");
    }

    [Fact]
    public void RefreshStatusPayload_reads_live_lock_state()
    {
        var services = new HostServices
        {
            Audio = new FakeAudioService(),
            AppAudio = new FakeAppAudioService(),
            Brightness = new FakeBrightnessService(),
            Media = new FakeMediaSessionService(),
            Hotkeys = new FakeHotkeyService(),
            Metrics = new FakeSystemMetricsService(),
            AudioLevels = new FakeAudioLevelService(),
            Autostart = new FakeAutostartService(),
            BrightnessChanges = new NullBrightnessChangeSource(),
            OsdSuppressor = new NullNativeOsdSuppressor(),
            LegacyVolumeKeys = new NullLegacyMediaKeyHook(),
            Idle = new FakeIdleService(),
            Fullscreen = new FakeFullscreenProbe(),
            LockKeys = new FakeLockKeysService(capsOn: true),
            Airplane = new NullAirplaneModeService(),
            AudioDevices = new NullAudioDeviceService(),
            ShellFlyoutTriggers = new NullShellFlyoutTriggerSource(),
        };

        var payload = TesseraFlyoutRequestBuilder.RefreshStatusPayload(
            services,
            "locks",
            new Dictionary<string, string> { ["lock"] = "CapsLock" });

        payload["on"].Should().Be("1");
    }

    [Theory]
    [InlineData("1", true)]
    [InlineData("0", false)]
    [InlineData("off", false)]
    public void BackdropBlurFromPayload_reads_backdropBlur_key(string raw, bool expected)
    {
        TesseraFlyoutRequestBuilder.BackdropBlurFromPayload(
            new Dictionary<string, string> { ["backdropBlur"] = raw }).Should().Be(expected);
    }

    [Theory]
    [InlineData("1", true)]
    [InlineData("0", false)]
    public void BackdropBlurFromPayload_falls_back_to_legacy_bakedFrost(string raw, bool expected)
    {
        TesseraFlyoutRequestBuilder.BackdropBlurFromPayload(
            new Dictionary<string, string> { ["bakedFrost"] = raw }).Should().Be(expected);
    }

    [Fact]
    public void BuildPayload_emits_backdropBlur_and_legacy_bakedFrost()
    {
        var services = HostServicesFakes.Create();
        var settings = new TesseraSettings { UseBackdropBlur = false };
        var request = new TesseraFlyoutRequestBuilder().Build(services, settings, "vol");
        request.Payload!["backdropBlur"].Should().Be("0");
        request.Payload["bakedFrost"].Should().Be("0");
    }

    private sealed class FakeLockKeysService(bool capsOn) : ILockKeysService
    {
        public LockKeyState Caps => new(LockKeyKind.CapsLock, capsOn);
        public LockKeyState Num => new(LockKeyKind.NumLock, false);
        public LockKeyState Scroll => new(LockKeyKind.ScrollLock, false);
        public event EventHandler<LockKeyState>? Changed { add { } remove { } }
        public void Start() { }
        public void Stop() { }
        public void Dispose() { }
    }
}
