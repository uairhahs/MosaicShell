using FluentAssertions;
using MosaicShell.Core.Capabilities;
using MosaicShell.Core.Capabilities.BuiltIn;
using MosaicShell.Core.Runtime;
using MosaicShell.Core.Services;
using MosaicShell.Core.Settings;

namespace MosaicShell.Core.Tests
{
    public class ShellFlyoutTriggerTests : IDisposable
    {
        private readonly string _root;

        public ShellFlyoutTriggerTests()
        {
            _root = Path.Combine(Path.GetTempPath(), "MosaicShellHook_" + Guid.NewGuid().ToString("N"));
            AppPaths.SetRootOverride(_root);
            AppPaths.EnsureLayout();
            _ = Directory.CreateDirectory(Path.Combine(AppPaths.ModulesDirectory, "Tessera"));
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
        [InlineData(ShellFlyoutTriggerDecoder.HsHellAppCommand, ShellFlyoutTriggerDecoder.MediaNext, ShellFlyoutKind.Media)]
        [InlineData(ShellFlyoutTriggerDecoder.HsHellAppCommand, ShellFlyoutTriggerDecoder.MediaPrevious, ShellFlyoutKind.Media)]
        public void Decoder_matches_modernflyouts_constants(long wParam, long lParam, ShellFlyoutKind expected)
        {
            _ = ShellFlyoutTriggerDecoder.TryDecode((nint)wParam, (nint)lParam, out ShellFlyoutKind kind).Should().BeTrue();
            _ = kind.Should().Be(expected);
        }

        [Fact]
        public void Decoder_ignores_unknown_appcommands()
        {
            _ = ShellFlyoutTriggerDecoder.TryDecode(
                ShellFlyoutTriggerDecoder.HsHellAppCommand, 12345, out _).Should().BeFalse();
        }

        [Fact]
        public async Task Tessera_shows_vol_on_shell_volume_trigger()
        {
            FakeShellFlyoutTriggerSource hook = new();
            List<FlyoutRequest> shown = [];
            HostServices services = new()
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

            BridgeUi ui = new(new CaptureFlyouts(shown));
            TesseraCapability cap = new(TestCapabilityContext.Create(services, ui));
            await cap.ArmAsync();
            hook.Raise(ShellFlyoutKind.Volume);
            _ = shown.Should().Contain(r => r.Kind == "vol");
            await cap.DisarmAsync();
        }

        [Fact]
        public async Task Tessera_shows_media_on_shell_media_next_trigger()
        {
            ModuleSettingsStore.Save("Tessera", new TesseraSettings { EnableMediaFlyouts = true });
            FakeShellFlyoutTriggerSource hook = new();
            List<FlyoutRequest> shown = [];
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
                LockKeys = new NullLockKeysService(),
                Airplane = new NullAirplaneModeService(),
                AudioDevices = new NullAudioDeviceService(),
                ShellFlyoutTriggers = hook,
            };

            BridgeUi ui = new(new CaptureFlyouts(shown));
            TesseraCapability cap = new(TestCapabilityContext.Create(services, ui));
            await cap.ArmAsync();
            hook.Raise(ShellFlyoutKind.Media);
            _ = shown.Should().Contain(r => r.Kind == "media" && r.ModuleId == "Tessera");
            await cap.DisarmAsync();
        }
    }
}
