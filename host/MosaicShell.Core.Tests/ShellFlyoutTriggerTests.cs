using FluentAssertions;
using MosaicShell.Core;
using MosaicShell.Core.Capabilities;
using MosaicShell.Core.Capabilities.BuiltIn;
using MosaicShell.Core.Services;

namespace MosaicShell.Core.Tests;

public class ShellFlyoutTriggerTests : IDisposable
{
    private readonly string _root;

    public ShellFlyoutTriggerTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "MosaicShellHook_" + Guid.NewGuid().ToString("N"));
        AppPaths.SetRootOverride(_root);
        AppPaths.EnsureLayout();
        Directory.CreateDirectory(Path.Combine(AppPaths.ModulesDirectory, "Tessera"));
    }

    public void Dispose()
    {
        AppPaths.ClearRootOverride();
        try { Directory.Delete(_root, true); } catch { /* ignore */ }
    }

    [Theory]
    [InlineData(ShellFlyoutTriggerDecoder.HsHellBrightness, 0, ShellFlyoutKind.Brightness)]
    [InlineData(ShellFlyoutTriggerDecoder.HsHellAppCommand, ShellFlyoutTriggerDecoder.MediaVolPlus, ShellFlyoutKind.Volume)]
    [InlineData(ShellFlyoutTriggerDecoder.HsHellAppCommand, ShellFlyoutTriggerDecoder.MediaVolMinus, ShellFlyoutKind.Volume)]
    [InlineData(ShellFlyoutTriggerDecoder.HsHellAppCommand, ShellFlyoutTriggerDecoder.MediaVolMute, ShellFlyoutKind.Volume)]
    [InlineData(ShellFlyoutTriggerDecoder.HsHellAppCommand, ShellFlyoutTriggerDecoder.MediaPlayPause, ShellFlyoutKind.Media)]
    public void Decoder_matches_modernflyouts_constants(long wParam, long lParam, ShellFlyoutKind expected)
    {
        ShellFlyoutTriggerDecoder.TryDecode((nint)wParam, (nint)lParam, out var kind).Should().BeTrue();
        kind.Should().Be(expected);
    }

    [Fact]
    public void Decoder_ignores_unknown_appcommands()
    {
        ShellFlyoutTriggerDecoder.TryDecode(
            ShellFlyoutTriggerDecoder.HsHellAppCommand, 12345, out _).Should().BeFalse();
    }

    [Fact]
    public async Task Tessera_shows_vol_on_shell_volume_trigger()
    {
        var hook = new FakeShellFlyoutTriggerSource();
        var shown = new List<FlyoutRequest>();
        var services = new HostServices
        {
            Audio = new FakeAudioService { MasterVolume = 0.4 },
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
            LockKeys = new NullLockKeysService(),
            Airplane = new NullAirplaneModeService(),
            AudioDevices = new NullAudioDeviceService(),
            ShellFlyoutTriggers = hook,
        };

        var ui = new CaptureUi(shown);
        var cap = new TesseraCapability(services, ui);
        await cap.ArmAsync();
        hook.Raise(ShellFlyoutKind.Volume);
        shown.Should().Contain(r => r.Kind == "vol");
        await cap.DisarmAsync();
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
        public bool IsVisible(string moduleId) => shown.Count > 0;
    }
}
