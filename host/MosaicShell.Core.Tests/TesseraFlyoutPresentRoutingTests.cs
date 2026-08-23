using FluentAssertions;
using MosaicShell.Core;
using MosaicShell.Core.Capabilities;
using MosaicShell.Core.Capabilities.BuiltIn;
using MosaicShell.Core.Runtime;
using MosaicShell.Core.Services;
using MosaicShell.Core.Settings;

namespace MosaicShell.Core.Tests;

public class TesseraFlyoutPresentRoutingTests : IDisposable
{
    private readonly string _root;
    private readonly CountingFlyouts _flyouts = new();
    private readonly BridgeUi _ui;

    public TesseraFlyoutPresentRoutingTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "MosaicPresentRoute_" + Guid.NewGuid().ToString("N"));
        AppPaths.SetRootOverride(_root);
        AppPaths.EnsureLayout();
        Directory.CreateDirectory(Path.Combine(AppPaths.ModulesDirectory, "Tessera"));
        ModuleSettingsStore.Save("Tessera", new TesseraSettings
        {
            EnableLockFlyouts = true,
            EnableMediaFlyouts = true
        });
        _ui = new BridgeUi(_flyouts);
    }

    public void Dispose()
    {
        AppPaths.ClearRootOverride();
        try { Directory.Delete(_root, true); } catch { /* ignore */ }
    }

    [Fact]
    public async Task Kind_change_while_visible_uses_Show_not_Update()
    {
        var lockSvc = new RaisingLockKeys();
        var services = TesseraServices(lockSvc);
        var cap = new TesseraCapability(TestCapabilityContext.Create(services, _ui));
        await cap.ArmAsync();

        services.Audio.MasterVolume = 0.5;
        _flyouts.Visible.Should().BeTrue();

        _flyouts.ShowCount = 0;
        _flyouts.UpdateCount = 0;

        lockSvc.Raise(new LockKeyState(LockKeyKind.CapsLock, true));

        _flyouts.ShowCount.Should().Be(1);
        _flyouts.UpdateCount.Should().Be(0);
        await cap.DisarmAsync();
    }

    [Fact]
    public async Task Repeat_lock_toggle_while_visible_patches()
    {
        var lockSvc = new RaisingLockKeys();
        var services = TesseraServices(lockSvc);
        var cap = new TesseraCapability(TestCapabilityContext.Create(services, _ui));
        await cap.ArmAsync();

        lockSvc.Raise(new LockKeyState(LockKeyKind.CapsLock, true));
        _flyouts.ShowCount.Should().Be(1);

        _flyouts.ShowCount = 0;
        _flyouts.UpdateCount = 0;

        lockSvc.Raise(new LockKeyState(LockKeyKind.CapsLock, false));

        _flyouts.UpdateCount.Should().Be(1);
        _flyouts.ShowCount.Should().Be(0);
        await cap.DisarmAsync();
    }

    [Fact]
    public async Task Position_restart_with_stale_title_soft_refreshes_while_visible()
    {
        var media = new FakeMediaSessionService();
        var services = TesseraServices(media: media);
        var cap = new TesseraCapability(TestCapabilityContext.Create(services, _ui));
        await cap.ArmAsync();

        media.Current = new MediaSessionInfo("Track A", "Artist", "app", true, null, 40, 180);
        _flyouts.ShowCount.Should().Be(1);

        _flyouts.ShowCount = 0;
        _flyouts.UpdateCount = 0;
        _flyouts.SoftRefreshCount = 0;

        media.Current = new MediaSessionInfo("Track A", "Artist", "app", true, null, 0.5, 180);

        _flyouts.UpdateCount.Should().Be(1, "track boundary resets auto-dismiss via Patch");
        _flyouts.SoftRefreshCount.Should().Be(0);
        await cap.DisarmAsync();
    }

    [Fact]
    public async Task Late_art_while_media_visible_soft_refreshes()
    {
        var media = new FakeMediaSessionService();
        var services = TesseraServices(media: media);
        var cap = new TesseraCapability(TestCapabilityContext.Create(services, _ui));
        await cap.ArmAsync();

        media.Current = new MediaSessionInfo("Track A", "Artist", "app", true, null, 0, 100);
        _flyouts.ShowCount.Should().Be(1);

        _flyouts.ShowCount = 0;
        _flyouts.UpdateCount = 0;
        _flyouts.SoftRefreshCount = 0;

        media.Current = new MediaSessionInfo("Track A", "Artist", "app", true, [1, 2, 3], 0, 100);

        _flyouts.SoftRefreshCount.Should().Be(1);
        _flyouts.UpdateCount.Should().Be(0);
        _flyouts.ShowCount.Should().Be(0);
        await cap.DisarmAsync();
    }

    [Fact]
    public async Task Track_change_while_media_visible_patches()
    {
        var media = new FakeMediaSessionService();
        var services = TesseraServices(media: media);
        var cap = new TesseraCapability(TestCapabilityContext.Create(services, _ui));
        await cap.ArmAsync();

        media.Current = new MediaSessionInfo("Track A", "Artist", "app", true, null, 0, 100);
        _flyouts.ShowCount.Should().Be(1);
        _flyouts.UpdateCount.Should().Be(0);

        _flyouts.ShowCount = 0;
        _flyouts.UpdateCount = 0;

        media.Current = new MediaSessionInfo("Track B", "Artist", "app", true, null, 0, 100);

        _flyouts.UpdateCount.Should().Be(1);
        _flyouts.ShowCount.Should().Be(0);
        await cap.DisarmAsync();
    }

    [Fact]
    public async Task Thread_pool_deferred_lock_edges_all_reach_flyouts()
    {
        var lockSvc = new ThreadPoolDeferringLockKeys();
        var flyouts = new CountingFlyouts();
        var ui = new BridgeUi(flyouts);
        var cap = new TesseraCapability(TestCapabilityContext.Create(TesseraServices(lockSvc), ui));
        await cap.ArmAsync();

        lockSvc.RaiseDeferred(new LockKeyState(LockKeyKind.CapsLock, true));
        lockSvc.RaiseDeferred(new LockKeyState(LockKeyKind.CapsLock, false));
        await Task.Delay(300);

        (flyouts.ShowCount + flyouts.UpdateCount).Should().BeGreaterThanOrEqualTo(2);
        await cap.DisarmAsync();
    }

    [Fact]
    public async Task Track_change_after_transient_dismiss_presents()
    {
        var media = new FakeMediaSessionService();
        var services = TesseraServices(media: media);
        var cap = new TesseraCapability(TestCapabilityContext.Create(services, _ui));
        await cap.ArmAsync();

        media.Current = new MediaSessionInfo("Track A", "Artist", "app", true, null, 0, 100);
        _flyouts.ShowCount.Should().Be(1);

        _flyouts.RaiseTransientDismiss();
        _flyouts.Visible.Should().BeFalse();

        _flyouts.ShowCount = 0;
        _flyouts.UpdateCount = 0;

        media.Current = new MediaSessionInfo("Track B", "Artist", "app", true, null, 0, 100);

        _flyouts.ShowCount.Should().Be(1);
        _flyouts.UpdateCount.Should().Be(0);
        await cap.DisarmAsync();
    }

    [Fact]
    public async Task Position_restart_after_transient_dismiss_presents()
    {
        var media = new FakeMediaSessionService();
        var services = TesseraServices(media: media);
        var cap = new TesseraCapability(TestCapabilityContext.Create(services, _ui));
        await cap.ArmAsync();

        media.Current = new MediaSessionInfo("Track A", "Artist", "app", true, null, 40, 180);
        _flyouts.ShowCount.Should().Be(1);

        _flyouts.RaiseTransientDismiss();

        _flyouts.ShowCount = 0;
        _flyouts.UpdateCount = 0;

        media.Current = new MediaSessionInfo("Track A", "Artist", "app", true, null, 0.5, 180);

        _flyouts.ShowCount.Should().Be(1);
        await cap.DisarmAsync();
    }

    [Fact]
    public async Task Late_art_after_transient_dismiss_does_not_present()
    {
        var media = new FakeMediaSessionService();
        var services = TesseraServices(media: media);
        var cap = new TesseraCapability(TestCapabilityContext.Create(services, _ui));
        await cap.ArmAsync();

        media.Current = new MediaSessionInfo("Track A", "Artist", "app", true, null, 10, 180);
        _flyouts.RaiseTransientDismiss();

        _flyouts.ShowCount = 0;
        media.Current = new MediaSessionInfo("Track A", "Artist", "app", true, [1, 2, 3], 10, 180);

        _flyouts.ShowCount.Should().Be(0);
        await cap.DisarmAsync();
    }

    [Fact]
    public async Task Volume_after_transient_dismiss_clears_suppress_and_shows()
    {
        var media = new FakeMediaSessionService();
        var services = TesseraServices(media: media);
        var cap = new TesseraCapability(TestCapabilityContext.Create(services, _ui));
        await cap.ArmAsync();

        media.Current = new MediaSessionInfo("Track A", "Artist", "app", true, null, 0, 100);
        _flyouts.RaiseTransientDismiss();

        _flyouts.ShowCount = 0;
        services.Audio.MasterVolume = 0.5;

        _flyouts.ShowCount.Should().Be(1);
        await cap.DisarmAsync();
    }

    [Fact]
    public async Task Track_change_after_hide_without_transient_dismiss_still_presents()
    {
        var media = new FakeMediaSessionService();
        var services = TesseraServices(media: media);
        var cap = new TesseraCapability(TestCapabilityContext.Create(services, _ui));
        await cap.ArmAsync();

        media.Current = new MediaSessionInfo("Track A", "Artist", "app", true, null, 0, 100);
        _flyouts.Hide("Tessera");
        _flyouts.Visible.Should().BeFalse();

        _flyouts.ShowCount = 0;
        _flyouts.UpdateCount = 0;

        media.Current = new MediaSessionInfo("Track B", "Artist", "app", true, null, 0, 100);

        _flyouts.ShowCount.Should().Be(1);
        _flyouts.UpdateCount.Should().Be(0);
        await cap.DisarmAsync();
    }

    [Fact]
    public async Task Same_kind_volume_tick_uses_Update()
    {
        var services = TesseraServices();
        var cap = new TesseraCapability(TestCapabilityContext.Create(services, _ui));
        await cap.ArmAsync();

        services.Audio.MasterVolume = 0.4;
        _flyouts.ShowCount.Should().Be(1);
        _flyouts.UpdateCount.Should().Be(0);

        _flyouts.ShowCount = 0;
        services.Audio.MasterVolume = 0.5;

        _flyouts.UpdateCount.Should().Be(1);
        _flyouts.ShowCount.Should().Be(0);
        await cap.DisarmAsync();
    }

    private static HostServices TesseraServices(
        ILockKeysService? lockKeys = null,
        IMediaSessionService? media = null) =>
        new()
        {
            Audio = new FakeAudioService(),
            AppAudio = new FakeAppAudioService(),
            Brightness = new FakeBrightnessService(),
            Media = media ?? new FakeMediaSessionService(),
            Hotkeys = new FakeHotkeyService(),
            Metrics = new FakeSystemMetricsService(),
            AudioLevels = new FakeAudioLevelService(),
            Autostart = new FakeAutostartService(),
            BrightnessChanges = new NullBrightnessChangeSource(),
            OsdSuppressor = new NullNativeOsdSuppressor(),
            LegacyVolumeKeys = new NullLegacyMediaKeyHook(),
            Idle = new NullIdleService(),
            Fullscreen = new NullFullscreenProbe(),
            LockKeys = lockKeys ?? new NullLockKeysService(),
            Airplane = new NullAirplaneModeService(),
            AudioDevices = new NullAudioDeviceService(),
            ShellFlyoutTriggers = new NullShellFlyoutTriggerSource(),
        };

    private sealed class RaisingLockKeys : ILockKeysService
    {
        public bool IsActive { get; private set; }
        public LockKeyState Caps => new(LockKeyKind.CapsLock, false);
        public LockKeyState Num => new(LockKeyKind.NumLock, false);
        public LockKeyState Scroll => new(LockKeyKind.ScrollLock, false);
        public event EventHandler<LockKeyState>? Changed;
        public void Start() => IsActive = true;
        public void Stop() => IsActive = false;
        public void Dispose() { }
        public void Raise(LockKeyState s) => Changed?.Invoke(this, s);
    }

    private sealed class ThreadPoolDeferringLockKeys : ILockKeysService
    {
        public bool IsActive { get; private set; }
        public LockKeyState Caps => new(LockKeyKind.CapsLock, false);
        public LockKeyState Num => new(LockKeyKind.NumLock, false);
        public LockKeyState Scroll => new(LockKeyKind.ScrollLock, false);
        public event EventHandler<LockKeyState>? Changed;
        public void Start() => IsActive = true;
        public void Stop() => IsActive = false;
        public void Dispose() { }

        public void RaiseDeferred(LockKeyState s) =>
            ThreadPool.QueueUserWorkItem(_ => Changed?.Invoke(this, s));
    }

    private sealed class CountingFlyouts : IFlyoutPresenter
    {
        public int ShowCount { get; set; }
        public int UpdateCount { get; set; }
        public int SoftRefreshCount { get; set; }
        public bool Visible { get; private set; }

        public event Action<string>? TransientDismissed;

        public void RaiseTransientDismiss(string moduleId = "Tessera")
        {
            Visible = false;
            TransientDismissed?.Invoke(moduleId);
        }

        public void Show(FlyoutRequest request)
        {
            ShowCount++;
            Visible = true;
        }

        public void Update(FlyoutRequest request) => UpdateCount++;

        public void SoftRefresh(FlyoutRequest request) => SoftRefreshCount++;

        public void Hide(string moduleId) => Visible = false;

        public void HideAll() => Visible = false;

        public bool IsVisible(string moduleId) => Visible;
    }
}
