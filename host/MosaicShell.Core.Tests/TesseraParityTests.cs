using FluentAssertions;
using MosaicShell.Core;
using MosaicShell.Core.Capabilities;
using MosaicShell.Core.Capabilities.BuiltIn;
using MosaicShell.Core.Runtime;
using MosaicShell.Core.Services;
using MosaicShell.Core.Settings;
using MosaicShell.Core.Styles;

namespace MosaicShell.Core.Tests;

public class TesseraParityTests : IDisposable
{
    private readonly string _root;
    private readonly List<FlyoutRequest> _shown = [];

    public TesseraParityTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "MosaicTesseraParity_" + Guid.NewGuid().ToString("N"));
        AppPaths.SetRootOverride(_root);
        AppPaths.EnsureLayout();
        Directory.CreateDirectory(Path.Combine(AppPaths.ModulesDirectory, "Tessera"));
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
        var info = new MediaSessionInfo("T", "A", "app", true, ThumbnailPng: [1, 2, 3], PositionSeconds: 10, DurationSeconds: 100);
        info.ThumbnailPng.Should().HaveCount(3);
        info.PositionSeconds.Should().Be(10);
        info.DurationSeconds.Should().Be(100);
    }

    [Fact]
    public void TesseraSettings_defaults_match_jaxcore_placement()
    {
        var s = new TesseraSettings();
        s.Position.Should().Be("TL");
        s.UseLegacyVolumeHooks.Should().BeTrue();
        s.Style.Should().Be("Fluent");
    }

    [Theory]
    [InlineData(null, "TL")]
    [InlineData("", "TL")]
    [InlineData("nope", "TL")]
    [InlineData("br", "BR")]
    public void FlyoutAnchor_normalize(string? input, string expect) =>
        FlyoutAnchor.Normalize(input).Should().Be(expect);

    [Theory]
    [InlineData("TL", 24, 24)]
    [InlineData("BR", 1920 - 320 - 24, 1080 - 120 - 24)]
    [InlineData("CC", (1920 - 320) / 2, (1080 - 120) / 2)]
    public void FlyoutAnchor_nine_point(string pos, int expectX, int expectY)
    {
        var (x, y) = FlyoutAnchor.Compute(0, 0, 1920, 1080, 320, 120, pos, 24, 24);
        x.Should().Be(expectX);
        y.Should().Be(expectY);
    }

    [Fact]
    public void FlyoutAnchor_clamps_inside_work_area_when_oversized()
    {
        // Window wider than work area → pinned to workX
        var (x, y) = FlyoutAnchor.Compute(100, 50, 800, 600, 900, 100, "BR", 24, 24);
        x.Should().Be(100);
        y.Should().BeGreaterThanOrEqualTo(50);
        y.Should().BeLessThanOrEqualTo(50 + 600 - 100);
    }

    [Fact]
    public void FlyoutAnchor_clamps_negative_pad_junk()
    {
        var (x, y) = FlyoutAnchor.Compute(0, 0, 1920, 1080, 320, 120, "TL", -50, 9999);
        x.Should().Be(0); // pad clamped
        y.Should().BeInRange(0, 1080 / 2);
    }

    [Fact]
    public void TesseraSettings_roundtrip_expanded_fields()
    {
        var s = new TesseraSettings
        {
            Style = "Win11",
            Position = "BC",
            MonitorIndex = 2,
            XPad = 12,
            YPad = 18,
            AutoDismissMs = 3000,
            Ani = 1,
            AniDir = "Bottom",
            EnableFlightFlyouts = false,
            ShowMediaStripOnVolume = false
        };
        ModuleSettingsStore.Save("Tessera", s);
        var loaded = ModuleSettingsStore.Load("Tessera", () => new TesseraSettings());
        loaded.Style.Should().Be("Win11");
        loaded.Position.Should().Be("BC");
        loaded.MonitorIndex.Should().Be(2);
        loaded.AniDir.Should().Be("Bottom");
        loaded.EnableFlightFlyouts.Should().BeFalse();
    }

    [Fact]
    public async Task Armed_tessera_emits_locks_and_flight()
    {
        var lockSvc = new RaisingLockKeys();
        var air = new RaisingAirplane();
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

        var ui = new CaptureUi(_shown);
        var registry = new CapabilityRegistry();
        BuiltInCapabilityFactories.RegisterAll(registry);
        var daemon = new CapabilityDaemon(registry, services, ui);
        (await daemon.ArmAsync("Tessera")).Should().BeTrue();

        lockSvc.Raise(new LockKeyState(LockKeyKind.CapsLock, true));
        _shown.Should().Contain(r => r.Kind == "locks");

        air.Raise();
        _shown.Should().Contain(r => r.Kind == "flight");
    }

    [Fact]
    public void StyleCatalog_still_has_eleven_tessera_layouts()
    {
        StyleCatalog.IdsFor("Tessera").Should().HaveCount(11);
    }

    private sealed class RaisingLockKeys : ILockKeysService
    {
        public LockKeyState Caps => new(LockKeyKind.CapsLock, false);
        public LockKeyState Num => new(LockKeyKind.NumLock, false);
        public LockKeyState Scroll => new(LockKeyKind.ScrollLock, false);
        public event EventHandler<LockKeyState>? Changed;
        public void Start() { }
        public void Stop() { }
        public void Dispose() { }
        public void Raise(LockKeyState s) => Changed?.Invoke(this, s);
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

    private sealed class CaptureUi(List<FlyoutRequest> shown) : ICapabilityUiBridge
    {
        public IFlyoutPresenter Flyouts { get; } = new CaptureFlyouts(shown);
    }

    private sealed class CaptureFlyouts(List<FlyoutRequest> shown) : IFlyoutPresenter
    {
        public void Show(FlyoutRequest request) => shown.Add(request);
        public void Update(FlyoutRequest request) => shown.Add(request);
        public void SoftRefresh(FlyoutRequest request) { }
        public void Hide(string moduleId) { }
        public void HideAll() { }
        public bool IsVisible(string moduleId) => shown.Any(r => r.ModuleId.Equals(moduleId, StringComparison.OrdinalIgnoreCase));
    }
}
