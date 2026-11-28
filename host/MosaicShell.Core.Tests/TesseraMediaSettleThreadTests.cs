using FluentAssertions;
using MosaicShell.Core.Capabilities;
using MosaicShell.Core.Capabilities.BuiltIn;
using MosaicShell.Core.Capabilities.Platform;
using MosaicShell.Core.Runtime;
using MosaicShell.Core.Services;
using MosaicShell.Core.Settings;

namespace MosaicShell.Core.Tests
{
    /// <summary>
    /// After a media flyout presents, a settle timer refreshes it once title and art have caught up. The timer fires on a
    /// pool thread, but the presenter's windows belong to the UI thread: reading them there threw "The calling thread
    /// cannot access this object" (GNOME journal, 2026-11-27). The settle work must run on the host thread like every
    /// other Tessera handler.
    /// </summary>
    public sealed class TesseraMediaSettleThreadTests : IDisposable
    {
        private readonly string _root;

        public TesseraMediaSettleThreadTests()
        {
            _root = Path.Combine(Path.GetTempPath(), "MosaicSettleThread_" + Guid.NewGuid().ToString("N"));
            AppPaths.SetRootOverride(_root);
            AppPaths.EnsureLayout();
            _ = Directory.CreateDirectory(Path.Combine(AppPaths.ModulesDirectory, "Tessera"));
            ModuleSettingsStore.Save("Tessera", new TesseraSettings { EnableMediaFlyouts = true });
        }

        public void Dispose()
        {
            AppPaths.ClearRootOverride();
            try { Directory.Delete(_root, true); } catch { /* ignore */ }
        }

        [Fact]
        public async Task The_media_settle_refresh_touches_the_presenter_only_on_the_host_thread()
        {
            ThreadCheckedFlyouts flyouts = new();
            BridgeUi ui = new(flyouts);
            flyouts.Ui = ui;
            FakeMediaSessionService media = new();
            HostServices services = HostServicesFakes.Create();
            HostServices withMedia = new()
            {
                Audio = services.Audio,
                AppAudio = services.AppAudio,
                Brightness = services.Brightness,
                Media = media,
                Hotkeys = services.Hotkeys,
                Metrics = services.Metrics,
                AudioLevels = services.AudioLevels,
                Autostart = services.Autostart,
                BrightnessChanges = services.BrightnessChanges,
                OsdSuppressor = services.OsdSuppressor,
                LegacyVolumeKeys = services.LegacyVolumeKeys,
                Idle = services.Idle,
                Fullscreen = services.Fullscreen,
                LockKeys = services.LockKeys,
                Airplane = services.Airplane,
                AudioDevices = services.AudioDevices,
                ShellFlyoutTriggers = services.ShellFlyoutTriggers,
            };
            TesseraCapability cap = new(TestCapabilityContext.Create(withMedia, ui));
            await cap.ArmAsync();

            media.Current = new MediaSessionInfo("Track A", "Artist", "app", true, null, 0, 100);
            // Only what the settle timer does from here on: arming and the first present are not on its thread.
            flyouts.Reset();
            await Task.Delay(MediaPresentPolicy.PresentSettleMs + 400);

            _ = flyouts.Calls.Should().BePositive("the settle refresh ran");
            _ = flyouts.OffThreadCalls.Should().Be(0);
            await cap.DisarmAsync();
        }

        /// <summary>Counts every presenter call, and those made outside <see cref="BridgeUi.RunOnHostThread"/>.</summary>
        private sealed class ThreadCheckedFlyouts : IFlyoutPresenter
        {
            private readonly List<FlyoutRequest> _shown = [];
            private int _calls;
            private int _offThread;

            public BridgeUi? Ui { get; set; }

            public int Calls => Volatile.Read(ref _calls);

            public int OffThreadCalls => Volatile.Read(ref _offThread);

            public void Reset()
            {
                Volatile.Write(ref _calls, 0);
                Volatile.Write(ref _offThread, 0);
            }

            public event Action<string>? TransientDismissed { add { } remove { } }

            public void Show(FlyoutRequest request)
            {
                Touch();
                _shown.Add(request);
            }

            public void Update(FlyoutRequest request)
            {
                Touch();
                _shown.Add(request);
            }

            public void SoftRefresh(FlyoutRequest request)
            {
                Touch();
            }

            public void Hide(string moduleId)
            {
                Touch();
            }

            public void HideAll()
            {
                Touch();
            }

            public bool IsVisible(string moduleId)
            {
                Touch();
                return _shown.Count > 0;
            }

            private void Touch()
            {
                _ = Interlocked.Increment(ref _calls);
                if (Ui is { InHostThread: false })
                {
                    _ = Interlocked.Increment(ref _offThread);
                }
            }
        }
    }
}
