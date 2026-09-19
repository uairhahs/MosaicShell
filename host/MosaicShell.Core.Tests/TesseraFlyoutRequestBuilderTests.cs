using FluentAssertions;
using MosaicShell.Core.Capabilities;
using MosaicShell.Core.Modules.Tessera;
using MosaicShell.Core.Services;
using MosaicShell.Core.Settings;
using MosaicShell.Core.Styles;

namespace MosaicShell.Core.Tests
{
    public class TesseraFlyoutRequestBuilderTests
    {
        [Fact]
        public void BuildPayload_matches_capability_shape_for_volume()
        {
            HostServices services = HostServicesFakes.Create();
            services.Audio.MasterVolume = 0.62;
            TesseraSettings settings = new() { Style = "Fluent", ShowMediaStripOnVolume = true };
            TesseraFlyoutRequestBuilder builder = new();

            FlyoutRequest request = builder.Build(services, settings, "vol");
            _ = request.ModuleId.Should().Be("Tessera");
            _ = request.Kind.Should().Be("vol");
            _ = request.StyleId.Should().Be("Fluent");
            _ = request.Payload!["volume"].Should().Be("0.62");
            _ = request.Payload["showMediaStrip"].Should().Be(
                TesseraLayoutCoverage.UsesStackedMediaStrip("Fluent") ? "1" : "0");
        }

        [Fact]
        public void MaterialYou_payload_requests_in_layout_media_chrome()
        {
            HostServices services = HostServicesFakes.Create();
            TesseraSettings settings = new() { Style = StyleIds.MaterialYou, ShowMediaStripOnVolume = true };
            Dictionary<string, string> payload = new TesseraFlyoutRequestBuilder().BuildPayload(services, settings, "vol");
            _ = TesseraFlyoutTweenTargetCatalog.StyleRequestsVolumeMediaChrome(StyleIds.MaterialYou).Should().BeTrue();
            _ = TesseraLayoutCoverage.UsesStackedMediaStrip(StyleIds.MaterialYou).Should().BeFalse();
            _ = payload["showMediaStrip"].Should().Be("1");
        }

        /// <summary>
        /// ShowMediaStripOnVolume is a "vol"-kind preference (does the volume flyout also
        /// show media); it must not gate a standalone "media" flyout's own phase-2 reveal.
        /// A media card is always itself a media presentation - if showMediaStrip reads
        /// false here, FlyoutMotionSession.Filter finds nothing phase-2-eligible and the
        /// card's internal reveal is skipped entirely while phase 1 (window slide/fade)
        /// still runs, which looks like a partial, broken entrance/exit.
        /// </summary>
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Media_kind_payload_always_requests_media_chrome_regardless_of_volume_setting(
            bool showMediaStripOnVolume)
        {
            HostServices services = HostServicesFakes.Create();
            TesseraSettings settings = new()
            {
                Style = "Fluent",
                ShowMediaStripOnVolume = showMediaStripOnVolume,
            };
            Dictionary<string, string> payload =
                new TesseraFlyoutRequestBuilder().BuildPayload(services, settings, "media");
            _ = payload["showMediaStrip"].Should().Be("1");
        }

        [Fact]
        public void BuildLivePayload_honors_show_media_strip_override()
        {
            HostServices services = HostServicesFakes.Create();
            TesseraSettings settings = new() { Style = "MaterialYou", ShowMediaStripOnVolume = true };
            TesseraFlyoutRequestBuilder builder = new();

            _ = builder.BuildLivePayload(services, settings, showMediaStripOverride: false)["showMediaStrip"]
                .Should().Be("0");
        }

        [Fact]
        public void BuildPayload_does_not_overwrite_explicit_lock_state()
        {
            HostServices services = HostServicesFakes.Create();
            TesseraFlyoutRequestBuilder builder = new();
            LockKeyState stale = new(LockKeyKind.CapsLock, true);

            Dictionary<string, string> payload = builder.BuildPayload(
                services,
                new TesseraSettings(),
                "locks",
                new Dictionary<string, string> { ["lock"] = "CapsLock", ["on"] = "0" },
                stale);

            _ = payload["on"].Should().Be("0");
        }

        [Fact]
        public void RefreshStatusPayload_reads_live_lock_state()
        {
            HostServices services = new()
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

            Dictionary<string, string> payload = TesseraFlyoutRequestBuilder.RefreshStatusPayload(
                services,
                "locks",
                new Dictionary<string, string> { ["lock"] = "CapsLock" });

            _ = payload["on"].Should().Be("1");
        }

        [Theory]
        [InlineData("1", true)]
        [InlineData("0", false)]
        [InlineData("off", false)]
        public void BackdropBlurFromPayload_reads_backdropBlur_key(string raw, bool expected)
        {
            _ = TesseraFlyoutRequestBuilder.BackdropBlurFromPayload(
                new Dictionary<string, string> { ["backdropBlur"] = raw }).Should().Be(expected);
        }

        [Theory]
        [InlineData("1", true)]
        [InlineData("0", false)]
        public void BackdropBlurFromPayload_falls_back_to_legacy_bakedFrost(string raw, bool expected)
        {
            _ = TesseraFlyoutRequestBuilder.BackdropBlurFromPayload(
                new Dictionary<string, string> { ["bakedFrost"] = raw }).Should().Be(expected);
        }

        [Fact]
        public void BuildPayload_emits_backdropBlur_and_legacy_bakedFrost()
        {
            HostServices services = HostServicesFakes.Create();
            TesseraSettings settings = new() { UseBackdropBlur = false };
            FlyoutRequest request = new TesseraFlyoutRequestBuilder().Build(services, settings, "vol");
            _ = request.Payload!["backdropBlur"].Should().Be("0");
            _ = request.Payload["bakedFrost"].Should().Be("0");
        }

        [Fact]
        public void BuildPayload_emits_normalized_accent_for_live_flyouts()
        {
            HostServices services = HostServicesFakes.Create();
            TesseraSettings settings = new() { AccentColor = "d8e2f8" };
            FlyoutRequest request = new TesseraFlyoutRequestBuilder().Build(services, settings, "vol");
            _ = request.Payload!["accent"].Should().Be("#D8E2F8");
        }

        [Fact]
        public void BuildPayload_emits_empty_accent_for_system()
        {
            HostServices services = HostServicesFakes.Create();
            TesseraSettings settings = new() { AccentColor = "" };
            FlyoutRequest request = new TesseraFlyoutRequestBuilder().Build(services, settings, "vol");
            _ = request.Payload!["accent"].Should().BeEmpty();
        }

        [Theory]
        [InlineData("#CBA6F7", "#CBA6F7")]
        [InlineData("", null)]
        [InlineData(null, null)]
        public void AccentFromPayload_returns_normalized_or_null_for_system(string? raw, string? expected)
        {
            IReadOnlyDictionary<string, string>? payload = raw is null
                ? null
                : new Dictionary<string, string> { ["accent"] = raw };
            _ = TesseraFlyoutRequestBuilder.AccentFromPayload(payload).Should().Be(expected);
        }

        private sealed class FakeLockKeysService(bool capsOn) : ILockKeysService
        {
            public bool IsActive => false;
            public LockKeyState Caps => new(LockKeyKind.CapsLock, capsOn);
            public LockKeyState Num => new(LockKeyKind.NumLock, false);
            public LockKeyState Scroll => new(LockKeyKind.ScrollLock, false);
            public event EventHandler<LockKeyState>? Changed { add { } remove { } }
            public void Start() { }
            public void Stop() { }
            public void Dispose() { }
        }
    }
}
