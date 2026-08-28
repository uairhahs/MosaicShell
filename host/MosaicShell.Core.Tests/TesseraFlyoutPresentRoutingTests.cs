using FluentAssertions;
using MosaicShell.Core.Capabilities;
using MosaicShell.Core.Capabilities.BuiltIn;
using MosaicShell.Core.Modules.Tessera;
using MosaicShell.Core.Runtime;
using MosaicShell.Core.Services;
using MosaicShell.Core.Settings;

namespace MosaicShell.Core.Tests
{
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
            _ = Directory.CreateDirectory(Path.Combine(AppPaths.ModulesDirectory, "Tessera"));
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
            RaisingLockKeys lockSvc = new();
            HostServices services = TesseraServices(lockSvc);
            TesseraCapability cap = new(TestCapabilityContext.Create(services, _ui));
            await cap.ArmAsync();

            services.Audio.MasterVolume = 0.5;
            _ = _flyouts.Visible.Should().BeTrue();

            _flyouts.ResetCounts();

            lockSvc.Raise(new LockKeyState(LockKeyKind.CapsLock, true));

            _ = _flyouts.ShowCount.Should().Be(1);
            _ = _flyouts.UpdateCount.Should().Be(0);
            await cap.DisarmAsync();
        }

        [Fact]
        public async Task Repeat_lock_toggle_while_visible_patches()
        {
            RaisingLockKeys lockSvc = new();
            HostServices services = TesseraServices(lockSvc);
            TesseraCapability cap = new(TestCapabilityContext.Create(services, _ui));
            await cap.ArmAsync();

            lockSvc.Raise(new LockKeyState(LockKeyKind.CapsLock, true));
            _ = _flyouts.ShowCount.Should().Be(1);

            _flyouts.ResetCounts();

            lockSvc.Raise(new LockKeyState(LockKeyKind.CapsLock, false));

            _ = _flyouts.UpdateCount.Should().Be(1);
            _ = _flyouts.ShowCount.Should().Be(0);
            await cap.DisarmAsync();
        }

        [Fact]
        public async Task Position_restart_with_stale_title_soft_refreshes_while_visible()
        {
            FakeMediaSessionService media = new();
            HostServices services = TesseraServices(media: media);
            TesseraCapability cap = new(TestCapabilityContext.Create(services, _ui));
            await cap.ArmAsync();

            media.Current = new MediaSessionInfo("Track A", "Artist", "app", true, null, 40, 180);
            _ = _flyouts.ShowCount.Should().Be(1);

            _flyouts.ResetCounts();

            media.Current = new MediaSessionInfo("Track A", "Artist", "app", true, null, 0.5, 180);

            _ = _flyouts.UpdateCount.Should().Be(1, "track boundary resets auto-dismiss via Patch");
            _ = _flyouts.SoftRefreshCount.Should().Be(0);
            await cap.DisarmAsync();
        }

        [Fact]
        public async Task Late_art_while_media_visible_soft_refreshes()
        {
            FakeMediaSessionService media = new();
            HostServices services = TesseraServices(media: media);
            TesseraCapability cap = new(TestCapabilityContext.Create(services, _ui));
            await cap.ArmAsync();

            media.Current = new MediaSessionInfo("Track A", "Artist", "app", true, null, 0, 100);
            _ = _flyouts.ShowCount.Should().Be(1);

            _flyouts.ResetCounts();

            media.Current = new MediaSessionInfo("Track A", "Artist", "app", true, [1, 2, 3], 0, 100);

            _ = _flyouts.SoftRefreshCount.Should().Be(1);
            _ = _flyouts.UpdateCount.Should().Be(0);
            _ = _flyouts.ShowCount.Should().Be(0);
            await cap.DisarmAsync();
        }

        [Fact]
        public async Task Track_change_while_media_visible_patches()
        {
            FakeMediaSessionService media = new();
            HostServices services = TesseraServices(media: media);
            TesseraCapability cap = new(TestCapabilityContext.Create(services, _ui));
            await cap.ArmAsync();

            media.Current = new MediaSessionInfo("Track A", "Artist", "app", true, null, 0, 100);
            _ = _flyouts.ShowCount.Should().Be(1);
            _ = _flyouts.UpdateCount.Should().Be(0);

            _flyouts.ResetCounts();

            media.Current = new MediaSessionInfo("Track B", "Artist", "app", true, null, 0, 100);

            _ = _flyouts.UpdateCount.Should().Be(1);
            _ = _flyouts.ShowCount.Should().Be(0);
            await cap.DisarmAsync();
        }

        [Fact]
        public async Task Thread_pool_deferred_lock_edges_all_reach_flyouts()
        {
            ThreadPoolDeferringLockKeys lockSvc = new();
            CountingFlyouts flyouts = new();
            BridgeUi ui = new(flyouts);
            TesseraCapability cap = new(TestCapabilityContext.Create(TesseraServices(lockSvc), ui));
            await cap.ArmAsync();

            lockSvc.RaiseDeferred(new LockKeyState(LockKeyKind.CapsLock, true));
            lockSvc.RaiseDeferred(new LockKeyState(LockKeyKind.CapsLock, false));
            await Task.Delay(300);

            _ = (flyouts.ShowCount + flyouts.UpdateCount).Should().BeGreaterThanOrEqualTo(2);
            await cap.DisarmAsync();
        }

        [Fact]
        public async Task Track_change_after_transient_dismiss_presents()
        {
            FakeMediaSessionService media = new();
            HostServices services = TesseraServices(media: media);
            TesseraCapability cap = new(TestCapabilityContext.Create(services, _ui));
            await cap.ArmAsync();

            media.Current = new MediaSessionInfo("Track A", "Artist", "app", true, null, 0, 100);
            _ = _flyouts.ShowCount.Should().Be(1);

            _flyouts.RaiseTransientDismiss();
            _ = _flyouts.Visible.Should().BeFalse();

            _flyouts.ResetCounts();

            media.Current = new MediaSessionInfo("Track B", "Artist", "app", true, null, 0, 100);

            _ = _flyouts.ShowCount.Should().Be(1);
            _ = _flyouts.UpdateCount.Should().Be(0);
            await cap.DisarmAsync();
        }

        [Fact]
        public async Task Position_restart_after_transient_dismiss_presents()
        {
            FakeMediaSessionService media = new();
            HostServices services = TesseraServices(media: media);
            TesseraCapability cap = new(TestCapabilityContext.Create(services, _ui));
            await cap.ArmAsync();

            media.Current = new MediaSessionInfo("Track A", "Artist", "app", true, null, 40, 180);
            _ = _flyouts.ShowCount.Should().Be(1);

            _flyouts.RaiseTransientDismiss();

            _flyouts.ResetCounts();

            media.Current = new MediaSessionInfo("Track A", "Artist", "app", true, null, 0.5, 180);

            _ = _flyouts.ShowCount.Should().Be(1);
            await cap.DisarmAsync();
        }

        [Fact]
        public async Task Late_art_after_transient_dismiss_does_not_present()
        {
            FakeMediaSessionService media = new();
            HostServices services = TesseraServices(media: media);
            TesseraCapability cap = new(TestCapabilityContext.Create(services, _ui));
            await cap.ArmAsync();

            media.Current = new MediaSessionInfo("Track A", "Artist", "app", true, null, 10, 180);
            _flyouts.RaiseTransientDismiss();

            _flyouts.ResetCounts();
            media.Current = new MediaSessionInfo("Track A", "Artist", "app", true, [1, 2, 3], 10, 180);

            _ = _flyouts.ShowCount.Should().Be(0);
            await cap.DisarmAsync();
        }

        [Fact]
        public async Task Volume_after_transient_dismiss_clears_suppress_and_shows()
        {
            FakeMediaSessionService media = new();
            HostServices services = TesseraServices(media: media);
            TesseraCapability cap = new(TestCapabilityContext.Create(services, _ui));
            await cap.ArmAsync();

            media.Current = new MediaSessionInfo("Track A", "Artist", "app", true, null, 0, 100);
            _flyouts.RaiseTransientDismiss();

            _flyouts.ResetCounts();
            services.Audio.MasterVolume = 0.5;

            _ = _flyouts.ShowCount.Should().Be(1);
            await cap.DisarmAsync();
        }

        [Fact]
        public async Task Track_change_after_hide_without_transient_dismiss_still_presents()
        {
            FakeMediaSessionService media = new();
            HostServices services = TesseraServices(media: media);
            TesseraCapability cap = new(TestCapabilityContext.Create(services, _ui));
            await cap.ArmAsync();

            media.Current = new MediaSessionInfo("Track A", "Artist", "app", true, null, 0, 100);
            _flyouts.Hide("Tessera");
            _ = _flyouts.Visible.Should().BeFalse();

            _flyouts.ResetCounts();

            media.Current = new MediaSessionInfo("Track B", "Artist", "app", true, null, 0, 100);

            _ = _flyouts.ShowCount.Should().Be(1);
            _ = _flyouts.UpdateCount.Should().Be(0);
            await cap.DisarmAsync();
        }

        [Fact]
        public async Task Same_kind_volume_tick_uses_Update()
        {
            HostServices services = TesseraServices();
            TesseraCapability cap = new(TestCapabilityContext.Create(services, _ui));
            await cap.ArmAsync();

            services.Audio.MasterVolume = 0.4;
            _ = _flyouts.ShowCount.Should().Be(1);
            _ = _flyouts.UpdateCount.Should().Be(0);

            _flyouts.ResetCounts();
            services.Audio.MasterVolume = 0.5;

            _ = _flyouts.UpdateCount.Should().Be(1);
            _ = _flyouts.ShowCount.Should().Be(0);
            await cap.DisarmAsync();
        }

        [Fact]
        public void Status_kind_payload_does_not_include_media_fields()
        {
            FakeMediaSessionService media = new()
            {
                Current = new MediaSessionInfo("Active Track", "Artist", "app", true, null, 0, 100)
            };
            HostServices services = TesseraServices(media: media);
            TesseraSettings settings = new()
            {
                ShowMediaStripOnVolume = true,
                Style = "Gnome"
            };
            TesseraFlyoutRequestBuilder builder = new();

            Dictionary<string, string> locksPayload = builder.BuildPayload(services, settings, "locks");
            Dictionary<string, string> flightPayload = builder.BuildPayload(services, settings, "flight");
            Dictionary<string, string> volPayload = builder.BuildPayload(services, settings, "vol");

            // Status kinds should have empty media fields
            _ = locksPayload["mediaTitle"].Should().BeEmpty();
            _ = locksPayload["mediaArtist"].Should().BeEmpty();
            _ = locksPayload["mediaPlaying"].Should().Be("0");
            _ = locksPayload["showMediaStrip"].Should().Be("0");

            _ = flightPayload["mediaTitle"].Should().BeEmpty();
            _ = flightPayload["mediaArtist"].Should().BeEmpty();
            _ = flightPayload["mediaPlaying"].Should().Be("0");
            _ = flightPayload["showMediaStrip"].Should().Be("0");

            // Volume kind should have media fields populated
            _ = volPayload["mediaTitle"].Should().Be("Active Track");
            _ = volPayload["mediaArtist"].Should().Be("Artist");
            _ = volPayload["mediaPlaying"].Should().Be("1");
            _ = volPayload["showMediaStrip"].Should().Be("1");
        }

        private static HostServices TesseraServices(
            ILockKeysService? lockKeys = null,
            IMediaSessionService? media = null)
        {
            return new()
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

        private sealed class ThreadPoolDeferringLockKeys : ILockKeysService
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

            public void RaiseDeferred(LockKeyState s)
            {
                _ = ThreadPool.QueueUserWorkItem(_ => Changed?.Invoke(this, s));
            }
        }

        private sealed class CountingFlyouts : IFlyoutPresenter
        {
            private int _showCount;
            private int _updateCount;
            private int _softRefreshCount;

            public int ShowCount => _showCount;
            public int UpdateCount => _updateCount;
            public int SoftRefreshCount => _softRefreshCount;
            public bool Visible { get; private set; }

            public event Action<string>? TransientDismissed;

            public void RaiseTransientDismiss(string moduleId = "Tessera")
            {
                Visible = false;
                TransientDismissed?.Invoke(moduleId);
            }

            public void Show(FlyoutRequest request)
            {
                _ = Interlocked.Increment(ref _showCount);
                Visible = true;
            }

            public void Update(FlyoutRequest request)
            {
                _ = Interlocked.Increment(ref _updateCount);
            }

            public void SoftRefresh(FlyoutRequest request)
            {
                _ = Interlocked.Increment(ref _softRefreshCount);
            }

            public void Hide(string moduleId)
            {
                Visible = false;
            }

            public void HideAll()
            {
                Visible = false;
            }

            public bool IsVisible(string moduleId)
            {
                return Visible;
            }

            public void ResetCounts()
            {
                _ = Interlocked.Exchange(ref _showCount, 0);
                _ = Interlocked.Exchange(ref _updateCount, 0);
                _ = Interlocked.Exchange(ref _softRefreshCount, 0);
            }
        }
    }
}
