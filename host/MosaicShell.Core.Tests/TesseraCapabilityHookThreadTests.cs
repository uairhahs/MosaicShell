using FluentAssertions;
using MosaicShell.Core;
using MosaicShell.Core.Capabilities;
using MosaicShell.Core.Capabilities.BuiltIn;
using MosaicShell.Core.Modules.Tessera;
using MosaicShell.Core.Runtime;
using MosaicShell.Core.Services;
using MosaicShell.Core.Settings;

namespace MosaicShell.Core.Tests;

public class TesseraCapabilityHookThreadTests : IDisposable
{
    private readonly string _root;

    public TesseraCapabilityHookThreadTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "MosaicHookThread_" + Guid.NewGuid().ToString("N"));
        AppPaths.SetRootOverride(_root);
        AppPaths.EnsureLayout();
        Directory.CreateDirectory(Path.Combine(AppPaths.ModulesDirectory, "Tessera"));
    }

    public void Dispose()
    {
        AppPaths.ClearRootOverride();
        try { Directory.Delete(_root, true); } catch { /* ignore */ }
    }

    [Fact]
    public async Task Arm_starts_legacy_hook_on_host_thread()
    {
        ModuleSettingsStore.Save("Tessera", new TesseraSettings
        {
            EnableLockFlyouts = true,
            UseLegacyVolumeHooks = true
        });

        var bridge = new BridgeUi(new NullFlyoutPresenter());
        var lockKeys = new TrackingLockKeysService();
        var legacy = new TrackingLegacyHook(() => bridge.InHostThread);
        var services = CreateServices(lockKeys, legacy);
        var cap = new TesseraCapability(TestCapabilityContext.Create(services, bridge));

        await cap.ArmAsync();

        lockKeys.StartCalled.Should().BeTrue();
        legacy.StartOnHostThread.Should().BeTrue();
        await cap.DisarmAsync();
        legacy.StopOnHostThread.Should().BeTrue();
        lockKeys.StopCalled.Should().BeTrue();
    }

    private static HostServices CreateServices(TrackingLockKeysService lockKeys, TrackingLegacyHook legacy) =>
        new()
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
            LegacyVolumeKeys = legacy,
            Idle = new NullIdleService(),
            Fullscreen = new NullFullscreenProbe(),
            LockKeys = lockKeys,
            Airplane = new NullAirplaneModeService(),
            AudioDevices = new NullAudioDeviceService(),
            ShellFlyoutTriggers = new NullShellFlyoutTriggerSource(),
        };

    internal sealed class TrackingLockKeysService : ILockKeysService
    {
        public bool IsActive { get; private set; }
        public bool StartCalled { get; private set; }
        public bool StopCalled { get; private set; }
        public LockKeyState Caps => new(LockKeyKind.CapsLock, false);
        public LockKeyState Num => new(LockKeyKind.NumLock, false);
        public LockKeyState Scroll => new(LockKeyKind.ScrollLock, false);
        public event EventHandler<LockKeyState>? Changed { add { } remove { } }

        public void Start()
        {
            StartCalled = true;
            IsActive = true;
        }

        public void Stop()
        {
            StopCalled = true;
            IsActive = false;
        }

        public void Dispose() { }
    }

    internal sealed class TrackingLegacyHook : ILegacyMediaKeyHook
    {
        private readonly Func<bool> _isHostThread;

        public TrackingLegacyHook(Func<bool>? isHostThread = null)
        {
            _isHostThread = isHostThread ?? (() => false);
        }

        public bool IsActive { get; private set; }
        public bool StartOnHostThread { get; private set; }
        public bool StopOnHostThread { get; private set; }
        public event EventHandler<LegacyVolumeKey>? Pressed { add { } remove { } }

        public void Start()
        {
            StartOnHostThread = _isHostThread();
            IsActive = true;
        }

        public void Stop()
        {
            StopOnHostThread = _isHostThread();
            IsActive = false;
        }

        public void Dispose() { }
    }
}
