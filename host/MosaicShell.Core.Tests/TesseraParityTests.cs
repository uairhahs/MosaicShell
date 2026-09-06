using FluentAssertions;
using MosaicShell.Core.Capabilities;
using MosaicShell.Core.Capabilities.BuiltIn;
using MosaicShell.Core.Runtime;
using MosaicShell.Core.Services;
using MosaicShell.Core.Settings;
using MosaicShell.Core.Styles;

namespace MosaicShell.Core.Tests
{
    public class TesseraParityTests : IDisposable
    {
        private readonly string _root;
        private readonly List<FlyoutRequest> _shown = [];

        public TesseraParityTests()
        {
            _root = Path.Combine(Path.GetTempPath(), "MosaicTesseraParity_" + Guid.NewGuid().ToString("N"));
            AppPaths.SetRootOverride(_root);
            AppPaths.EnsureLayout();
            _ = Directory.CreateDirectory(Path.Combine(AppPaths.ModulesDirectory, "Tessera"));
            ModuleManifest.WriteDefault("Tessera");
        }

        public void Dispose()
        {
            AppPaths.ClearRootOverride();
            try { Directory.Delete(_root, true); } catch { /* ignore */ }
        }

        [Fact]
        public void MediaSessionInfo_accepts_thumbnail_and_timeline()
        {
            MediaSessionInfo info = new("T", "A", "app", true, ThumbnailPng: [1, 2, 3], PositionSeconds: 10, DurationSeconds: 100);
            _ = info.ThumbnailPng.Should().HaveCount(3);
            _ = info.PositionSeconds.Should().Be(10);
            _ = info.DurationSeconds.Should().Be(100);
        }

        [Fact]
        public void TesseraSettings_defaults_match_jaxcore_placement()
        {
            TesseraSettings s = new();
            _ = s.Position.Should().Be("TL");
            _ = s.UseLegacyVolumeHooks.Should().BeTrue();
            _ = s.Style.Should().Be("Fluent");
        }

        [Fact]
        public void TesseraSettings_host_polish_defaults()
        {
            TesseraSettings s = new();
            _ = s.FlyoutScalePercent.Should().Be(100);
            _ = s.UseBackdropBlur.Should().BeTrue();
            _ = s.UseBakedFrost.Should().BeTrue();
            _ = s.UseAcrylicBackdrop.Should().BeTrue();
            _ = s.UseOsAcrylic.Should().BeFalse();
            _ = s.UseFocusDim.Should().BeTrue();
            _ = Math.Clamp(s.FlyoutScalePercent, 50, 150).Should().Be(100);
        }

        [Theory]
        [InlineData(null, "TL")]
        [InlineData("", "TL")]
        [InlineData("nope", "TL")]
        [InlineData("br", "BR")]
        public void FlyoutAnchor_normalize(string? input, string expect)
        {
            _ = FlyoutAnchor.Normalize(input).Should().Be(expect);
        }

        [Theory]
        [InlineData("TL", 24, 24)]
        [InlineData("BR", 1920 - 320 - 24, 1080 - 120 - 24)]
        [InlineData("CC", (1920 - 320) / 2, (1080 - 120) / 2)]
        public void FlyoutAnchor_nine_point(string pos, int expectX, int expectY)
        {
            (int x, int y) = FlyoutAnchor.Compute(0, 0, 1920, 1080, 320, 120, pos, 24, 24);
            _ = x.Should().Be(expectX);
            _ = y.Should().Be(expectY);
        }

        [Fact]
        public void FlyoutAnchor_clamps_inside_work_area_when_oversized()
        {
            // Window wider than work area → pinned to workX
            (int x, int y) = FlyoutAnchor.Compute(100, 50, 800, 600, 900, 100, "BR", 24, 24);
            _ = x.Should().Be(100);
            _ = y.Should().BeGreaterThanOrEqualTo(50);
            _ = y.Should().BeLessThanOrEqualTo(50 + 600 - 100);
        }

        [Fact]
        public void FlyoutAnchor_clamps_negative_pad_junk()
        {
            (int x, int y) = FlyoutAnchor.Compute(0, 0, 1920, 1080, 320, 120, "TL", -50, 9999);
            _ = x.Should().Be(0); // pad clamped
            _ = y.Should().BeInRange(0, 1080 / 2);
        }

        [Fact]
        public void TesseraSettings_roundtrip_expanded_fields()
        {
            TesseraSettings s = new()
            {
                Style = "Windows11",
                Position = "BC",
                MonitorIndex = 2,
                XPad = 12,
                YPad = 18,
                AutoDismissMs = 3000,
                Ani = 1,
                AniDir = "Bottom",
                EnableFlightFlyouts = false,
                ShowMediaStripOnVolume = false,
                UseOsAcrylic = true,
                AccentColor = "#D8E2F8"
            };
            ModuleSettingsStore.Save("Tessera", s);
            TesseraSettings loaded = ModuleSettingsStore.Load("Tessera", () => new TesseraSettings());
            _ = loaded.Style.Should().Be("Windows11");
            _ = loaded.AccentColor.Should().Be("#D8E2F8");
            _ = loaded.Position.Should().Be("BC");
            _ = loaded.MonitorIndex.Should().Be(2);
            _ = loaded.AniDir.Should().Be("Bottom");
            _ = loaded.EnableFlightFlyouts.Should().BeFalse();
            _ = loaded.UseOsAcrylic.Should().BeTrue();
        }

        [Fact]
        public async Task TesseraCapability_picks_up_settings_changes_without_rearm()
        {
            ModuleSettingsStore.Save("Tessera", new TesseraSettings { Style = "Fluent" });
            HostServices services = HostServicesFakes.Create();
            BridgeUi ui = new(new CaptureFlyouts(_shown));
            CapabilityRegistry registry = new();
            BuiltInCapabilityFactories.RegisterAll(registry);
            CapabilityDaemon daemon = new(registry, services, ui);
            _ = (await daemon.ArmAsync("Tessera")).Should().BeTrue();

            services.Audio.MasterVolume = 0.5;
            _ = _shown.Should().ContainSingle();
            _ = _shown[0].StyleId.Should().Be("Fluent");

            ModuleSettingsStore.Save("Tessera", new TesseraSettings { Style = "Windows11", FlyoutScalePercent = 120 });
            _shown.Clear();
            services.Audio.MasterVolume = 0.6;
            _ = _shown.Should().ContainSingle();
            _ = _shown[0].StyleId.Should().Be("Windows11");
            _ = _shown[0].Payload!["flyoutScale"].Should().Be("120");
        }

        [Fact]
        public async Task Armed_tessera_emits_locks_and_flight()
        {
            RaisingLockKeys lockSvc = new();
            RaisingAirplane air = new();
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
                Idle = new NullIdleService(),
                Fullscreen = new NullFullscreenProbe(),
                LockKeys = lockSvc,
                Airplane = air,
                AudioDevices = new NullAudioDeviceService(),
                ShellFlyoutTriggers = new NullShellFlyoutTriggerSource(),
            };

            ModuleSettingsStore.Save("Tessera", new TesseraSettings
            {
                EnableLockFlyouts = true,
                EnableFlightFlyouts = true
            });

            BridgeUi ui = new(new CaptureFlyouts(_shown));
            CapabilityRegistry registry = new();
            BuiltInCapabilityFactories.RegisterAll(registry);
            CapabilityDaemon daemon = new(registry, services, ui);
            _ = (await daemon.ArmAsync("Tessera")).Should().BeTrue();

            lockSvc.Raise(new LockKeyState(LockKeyKind.CapsLock, true));
            _ = _shown.Should().Contain(r => r.Kind == "locks" && r.Payload!["on"] == "1");

            lockSvc.Raise(new LockKeyState(LockKeyKind.CapsLock, false));
            _ = _shown.Should().Contain(r => r.Kind == "locks" && r.Payload!["on"] == "0");

            air.Raise();
            _ = _shown.Should().Contain(r => r.Kind == "flight");
        }

        [Fact]
        public async Task Armed_tessera_shows_media_flyout_on_track_change()
        {
            ModuleSettingsStore.Save("Tessera", new TesseraSettings { EnableMediaFlyouts = true });
            HostServices services = HostServicesFakes.Create();
            FakeMediaSessionService media = (FakeMediaSessionService)services.Media;
            media.Current = new MediaSessionInfo("Track A", "Artist", "app", true, null, 0, 100);

            BridgeUi ui = new(new CaptureFlyouts(_shown));
            CapabilityRegistry registry = new();
            BuiltInCapabilityFactories.RegisterAll(registry);
            CapabilityDaemon daemon = new(registry, services, ui);
            _ = (await daemon.ArmAsync("Tessera")).Should().BeTrue();
            _shown.Clear();

            media.Current = new MediaSessionInfo("Track B", "Artist", "app", true, null, 0, 100);

            _ = _shown.Should().Contain(r => r.Kind == "media" && r.ModuleId == "Tessera");
        }

        [Fact]
        public void StyleCatalog_still_has_eleven_tessera_layouts()
        {
            _ = StyleCatalog.IdsFor("Tessera").Should().HaveCount(11);
        }

        private sealed class RaisingLockKeys : ILockKeysService
        {
            public bool IsActive { get; private set; }
            public LockKeyState Caps => new(LockKeyKind.CapsLock, false);
            public LockKeyState Num => new(LockKeyKind.NumLock, false);
            public LockKeyState Scroll => new(LockKeyKind.ScrollLock, false);
            public event EventHandler<LockKeyState>? Changed;
            public void Start()
            {
                IsActive = true;
            }

            public void Stop()
            {
                IsActive = false;
            }

            public void Dispose() { }
            public void Raise(LockKeyState s)
            {
                Changed?.Invoke(this, s);
            }
        }

        private sealed class RaisingAirplane : IAirplaneModeService
        {
            public bool IsSupported => true;
            public bool IsEnabled { get; private set; }
            public event EventHandler? Changed;
            public void Start() { }
            public void Stop() { }
            public void Dispose() { }
            public void Raise()
            {
                IsEnabled = !IsEnabled;
                Changed?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}
