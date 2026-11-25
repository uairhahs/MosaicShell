namespace MosaicShell.Core.Services
{
    public interface IAudioService : IDisposable
    {
        double MasterVolume { get; set; }
        bool IsMuted { get; set; }
        event EventHandler? Changed;
    }

    public sealed record AppAudioSession(string Id, string DisplayName, double Volume, bool IsMuted);

    public interface IAppAudioService : IDisposable
    {
        IReadOnlyList<AppAudioSession> GetSessions();
        void SetVolume(string sessionId, double volume);
        void SetMuted(string sessionId, bool muted);
        event EventHandler? SessionsChanged;
    }

    public interface IBrightnessService
    {
        bool IsSupported { get; }
        double Brightness { get; set; }
    }

    public sealed record MediaSessionInfo(
        string? Title,
        string? Artist,
        string? AppId,
        bool IsPlaying,
        byte[]? ThumbnailPng = null,
        double PositionSeconds = 0,
        double DurationSeconds = 0,
        /// <summary>Browser like rating when known (0 unrated, 1 disliked, 5 liked); null for native SMTC-only.</summary>
        int? LikeRating = null,
        /// <summary>What the browser player behind this session can be asked to do; none for native SMTC-only.</summary>
        BrowserMediaCapabilities Capabilities = BrowserMediaCapabilities.None);

    public interface IMediaSessionService : IDisposable
    {
        MediaSessionInfo? Current { get; }
        /// <summary>Title / artist / play-state / thumbnail identity changes.</summary>
        event EventHandler? Changed;
        /// <summary>Timeline position ticks while playing - refresh visible UI only; do not open flyouts.</summary>
        event EventHandler? ProgressChanged;
        /// <summary>
        /// Poll SMTC timeline (and retry thumbnail if missing). Safe to call from a UI timer -
        /// many players (YouTube Music) do not raise TimelinePropertiesChanged.
        /// </summary>
        void PumpTimeline();
        Task PlayPauseAsync();
        Task NextAsync();
        Task PreviousAsync();
        Task SeekAsync(double positionSeconds);
        Task ToggleShuffleAsync();
        Task ToggleRepeatAsync();
        /// <summary>Like or remove like (never thumbs-down). <paramref name="wantLiked"/> comes from UI heart state.</summary>
        Task ToggleLikeAsync(bool wantLiked);
        /// <summary>Dislike or clear dislike (YouTube Music / like-dislike players only).</summary>
        Task ToggleDislikeAsync(bool wantDisliked);
    }

    /// <summary>
    /// Implemented by a media service that merges several sources, so a diagnostic can say which one
    /// supplied each field instead of showing only the merged result.
    /// </summary>
    public interface IMediaSourceDiagnostics
    {
        string DescribeSources();
    }

    public sealed record HotkeyBinding(string Id, string Gesture);

    public interface IHotkeyService : IDisposable
    {
        bool Register(string id, ModifierKeys modifiers, int virtualKey, Action callback);
        void Unregister(string id);
    }

    [Flags]
    public enum ModifierKeys
    {
        None = 0,
        Alt = 1,
        Control = 2,
        Shift = 4,
        Win = 8,
    }

    public sealed record DiskMetric(string Name, double FreeGb, double TotalGb);
    public sealed record SystemMetricsSnapshot(
        double CpuPercent,
        double RamUsedPercent,
        double RamUsedGb,
        double RamTotalGb,
        IReadOnlyList<DiskMetric> Disks,
        string MachineName);

    public interface ISystemMetricsService : IDisposable
    {
        SystemMetricsSnapshot Sample();
    }

    public interface IAudioLevelService : IDisposable
    {
        /// <summary>0..1 peak level.</summary>
        double Peak { get; }
        /// <summary>Optional FFT/band levels 0..1.</summary>
        IReadOnlyList<double> Bands { get; }
        void Start();
        void Stop();
    }

    public interface IAutostartService
    {
        bool IsEnabled { get; }
        void SetEnabled(bool enabled);
    }

    /// <summary>Composition root for OS services used by tiles and hub.</summary>
    public sealed class HostServices : IDisposable
    {
        public required IAudioService Audio { get; init; }
        public required IAppAudioService AppAudio { get; init; }
        public required IBrightnessService Brightness { get; init; }
        public required IMediaSessionService Media { get; init; }
        public required IHotkeyService Hotkeys { get; init; }
        public required ISystemMetricsService Metrics { get; init; }
        public required IAudioLevelService AudioLevels { get; init; }
        public required IAutostartService Autostart { get; init; }
        public required IBrightnessChangeSource BrightnessChanges { get; init; }
        public required INativeOsdSuppressor OsdSuppressor { get; init; }
        public required ILegacyMediaKeyHook LegacyVolumeKeys { get; init; }
        public required IIdleService Idle { get; init; }
        public required IFullscreenProbe Fullscreen { get; init; }
        public required ILockKeysService LockKeys { get; init; }
        public required IAirplaneModeService Airplane { get; init; }
        public required IAudioDeviceService AudioDevices { get; init; }
        public required IShellFlyoutTriggerSource ShellFlyoutTriggers { get; init; }

        /// <summary>Platform parts that serve no interface of their own but live as long as the services (Linux: the MPRIS export).</summary>
        public IReadOnlyList<IDisposable> Companions { get; init; } = [];

        public void Dispose()
        {
            // Companions read the services, so they go first.
            foreach (IDisposable companion in Companions)
            {
                companion.Dispose();
            }

            Audio.Dispose();
            AppAudio.Dispose();
            Media.Dispose();
            Hotkeys.Dispose();
            Metrics.Dispose();
            AudioLevels.Dispose();
            BrightnessChanges.Dispose();
            OsdSuppressor.Dispose();
            LegacyVolumeKeys.Dispose();
            Idle.Dispose();
            (Fullscreen as IDisposable)?.Dispose();
            LockKeys.Dispose();
            Airplane.Dispose();
            AudioDevices.Dispose();
            ShellFlyoutTriggers.Dispose();
        }

        /// <summary>
        /// The services for the OS this build runs on: the Windows set on Windows, the Linux set on Linux, otherwise
        /// <see cref="CreateUnsupportedPlatformDefaults"/> until that platform has its own.
        /// </summary>
        /// <param name="browserSources">See <c>CreateWindowsDefaults</c>; ignored where there is no browser bridge.</param>
        public static HostServices CreatePlatformDefaults(bool browserSources = false)
        {
#if WINDOWS
            return CreateWindowsDefaults(browserSources);
#else
            return OperatingSystem.IsLinux() ? CreateLinuxDefaults(browserSources) : CreateUnsupportedPlatformDefaults();
#endif
        }

        /// <summary>
        /// Finds the Native Translucency backend and sets <see cref="Capabilities.Platform.NativeTranslucencyPlatform"/>.
        /// Windows already knows it from the OS version. Linux asks KWin whether its blur effect is loaded, which takes a
        /// bus round trip, so the Host starts this at launch without waiting: flyouts shown before it finishes keep the
        /// frost, as they do wherever there is no backend.
        /// </summary>
        public static async Task DetectNativeTranslucencyAsync()
        {
#if WINDOWS
            await Task.CompletedTask.ConfigureAwait(false);
#else
            if (!OperatingSystem.IsLinux())
            {
                return;
            }

            bool hasX11Display = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DISPLAY"));
            bool kwinBlur = hasX11Display && await Linux.KWinBlurProbe.IsBlurLoadedAsync().ConfigureAwait(false);
            Capabilities.Platform.NativeTranslucencyPlatform.Set(Capabilities.Platform.NativeTranslucencyPlatform.Resolve(
                isWindows: false,
                windowsSupportsWinUiAcrylic: false,
                isLinux: true,
                hasX11Display,
                kwinBlur));
#endif
        }

        /// <summary>
        /// Every service present and inert: features report themselves unavailable rather than showing invented data.
        /// For a platform with no implementations yet, so the Host can start and draw.
        /// </summary>
        public static HostServices CreateUnsupportedPlatformDefaults()
        {
            return CreateInert(new NullMediaSessionService());
        }

#if !WINDOWS
        /// <summary>
        /// Linux: media from MPRIS players, shell flyout triggers and capability hotkeys from KDE's KGlobalAccel, Plasma
        /// pop-up suppression and screen brightness from PowerDevil (all on the session bus), lock keys from KWin's
        /// keystate protocol, master and per-app volume and the outputs through pactl, and output levels through parec; every
        /// other service inert until it has a Linux implementation.
        /// With browser sources, Grout's report is also published as the Host's own MPRIS player
        /// (<see cref="Mpris.GroutMprisBridge"/>), so Plasma's media controls get the page's cover too.
        /// </summary>
        /// <param name="browserSources">The Grout extension source beside MPRIS (the Host passes true; tests leave it off).</param>
        public static HostServices CreateLinuxDefaults(bool browserSources = false)
        {
            string? desktop = Xdg.XdgCurrentDesktop.Current;
            // One PowerDevil watcher serves both the value and its changes, as Windows' pair shares one service.
            Linux.KdeScreenBrightness brightness = new();

            // One pactl subscription serves per-app audio and the output devices; disposing it twice is harmless.
            Linux.PulseAppAudioService streams = new();
            Mpris.MprisMediaSessionService mpris = new(new Linux.DBusMprisBus(), new Mpris.MprisArtSource());
            IMediaSessionService media;
            List<IDisposable> companions = [];
            if (browserSources)
            {
                IBrowserMediaSource[] browser = BrowserSourceStack.CreateLinux(() => mpris.Current);
                CompositeMediaSessionService composite = new(mpris, browser);
                companions.Add(new Mpris.GroutMprisBridge(
                    browser[0],
                    composite,
                    (busName, commands) => new Linux.DBusMprisExport(address: null, commands, busName: busName),
                    Mpris.MprisExportPolicy.DefaultArtCacheDirectory(),
                    () => mpris.ActiveProcessId,
                    LinuxDesktopEntryResolver()));
                media = composite;
            }
            else
            {
                media = new CompositeMediaSessionService(mpris);
            }

            return CreateInert(
                media,
                new Linux.KGlobalAccelShellFlyoutTriggers(),
                CreateLinuxOsdSuppressor(desktop),
                new Linux.PulseAudioService(),
                brightness,
                brightness,
                companions,
                new Linux.KGlobalAccelHotkeyService(),
                CreateLinuxLockKeys(desktop),
                streams,
                new Linux.ParecAudioLevelService(),
                streams,
                new Linux.ProcSystemMetricsService(),
                Linux.XdgAutostartService.ForCurrentProcess(),
                CreateLinuxIdle(desktop),
                new Linux.KWinFullscreenProbe(),
                CreateLinuxAirplane(desktop));
        }

        /// <summary>
        /// On GNOME, MosaicShell's Shell extension (GNOME has no API to hide its pop-ups); elsewhere Plasma's osdService
        /// and KWin route.
        /// </summary>
        public static INativeOsdSuppressor CreateLinuxOsdSuppressor(string? xdgCurrentDesktop)
        {
            return Xdg.XdgCurrentDesktop.IsGnome(xdgCurrentDesktop)
                ? new Linux.GnomeOsdSuppressor()
                : new Linux.PlasmaOsdSuppressor();
        }

        /// <summary>
        /// On GNOME, Mutter's idle monitor (it has no ext_idle_notifier_v1); elsewhere the Wayland idle protocol.
        /// </summary>
        public static IIdleService CreateLinuxIdle(string? xdgCurrentDesktop)
        {
            return Xdg.XdgCurrentDesktop.IsGnome(xdgCurrentDesktop)
                ? new Linux.MutterIdleService()
                : new Linux.WaylandIdleService();
        }

        /// <summary>
        /// On Plasma, KWin pushes each lock key change (keystate protocol); elsewhere (GNOME offers none) the keyboard LEDs
        /// are read.
        /// </summary>
        public static ILockKeysService CreateLinuxLockKeys(string? xdgCurrentDesktop)
        {
            return Xdg.XdgCurrentDesktop.IsPlasma(xdgCurrentDesktop)
                ? new Linux.KWinKeystateLockKeys()
                : new Linux.LedLockKeysService();
        }

        /// <summary>
        /// On Plasma, its own airplane setting with the radios saying whether there is one to switch off; elsewhere the
        /// radios alone (every radio blocked), until a desktop's own setting is wired the same way.
        /// </summary>
        public static IAirplaneModeService CreateLinuxAirplane(string? xdgCurrentDesktop)
        {
            return Xdg.XdgCurrentDesktop.IsPlasma(xdgCurrentDesktop)
                ? new Linux.PlasmaAirplaneModeService(new Linux.RfkillAirplaneModeService())
                : new Linux.RfkillAirplaneModeService();
        }

        /// <summary>
        /// MPRIS in the same composite that Windows wraps SMTC in, so both platforms merge and present by one path.
        /// </summary>
        /// <summary>
        /// The browser's installed desktop entry for an app id and process (<see cref="Mpris.MprisExportPolicy.DesktopEntryFor"/>),
        /// remembered per pair: the bridge asks on every progress tick, and an install does not change under a running process.
        /// </summary>
        private static Func<string, int?, string> LinuxDesktopEntryResolver()
        {
            Linux.LinuxDesktopEntries entries = Linux.LinuxDesktopEntries.ForCurrentUser();
            System.Collections.Concurrent.ConcurrentDictionary<(string, int?), string> known = new();
            return (appId, pid) => known.GetOrAdd((appId, pid), key => Mpris.MprisExportPolicy.DesktopEntryFor(
                key.Item1,
                key.Item2 is int p ? Linux.LinuxDesktopEntries.ExecutablePath(p) : null,
                key.Item2 is int q ? Linux.LinuxDesktopEntries.FlatpakAppId(q) : null,
                entries.Exists));
        }

        /// <param name="busAddress">Null for the user's session bus; tests pass a private bus.</param>
        public static IMediaSessionService CreateLinuxMediaStack(string? busAddress = null)
        {
            return new CompositeMediaSessionService(
                new Mpris.MprisMediaSessionService(new Linux.DBusMprisBus(busAddress), new Mpris.MprisArtSource()));
        }
#endif

        private static HostServices CreateInert(
            IMediaSessionService media,
            IShellFlyoutTriggerSource? shellFlyoutTriggers = null,
            INativeOsdSuppressor? osdSuppressor = null,
            IAudioService? audio = null,
            IBrightnessService? brightness = null,
            IBrightnessChangeSource? brightnessChanges = null,
            IReadOnlyList<IDisposable>? companions = null,
            IHotkeyService? hotkeys = null,
            ILockKeysService? lockKeys = null,
            IAppAudioService? appAudio = null,
            IAudioLevelService? audioLevels = null,
            IAudioDeviceService? audioDevices = null,
            ISystemMetricsService? metrics = null,
            IAutostartService? autostart = null,
            IIdleService? idle = null,
            IFullscreenProbe? fullscreen = null,
            IAirplaneModeService? airplane = null)
        {
            return new HostServices
            {
                Audio = audio ?? new NullAudioService(),
                AppAudio = appAudio ?? new NullAppAudioService(),
                Brightness = brightness ?? new NullBrightnessService(),
                Media = media,
                Hotkeys = hotkeys ?? new NullHotkeyService(),
                Metrics = metrics ?? new NullSystemMetricsService(),
                AudioLevels = audioLevels ?? new NullAudioLevelService(),
                Autostart = autostart ?? new NullAutostartService(),
                BrightnessChanges = brightnessChanges ?? new NullBrightnessChangeSource(),
                OsdSuppressor = osdSuppressor ?? new NullNativeOsdSuppressor(),
                LegacyVolumeKeys = new NullLegacyMediaKeyHook(),
                Idle = idle ?? new NullIdleService(),
                Fullscreen = fullscreen ?? new NullFullscreenProbe(),
                LockKeys = lockKeys ?? new NullLockKeysService(),
                Airplane = airplane ?? new NullAirplaneModeService(),
                AudioDevices = audioDevices ?? new NullAudioDeviceService(),
                ShellFlyoutTriggers = shellFlyoutTriggers ?? new NullShellFlyoutTriggerSource(),
                Companions = companions ?? [],
            };
        }

#if WINDOWS
        /// <param name="browserSources">
        /// True only in the Host: it listens for the browser extension and falls back to reading the browser's like buttons
        /// through the accessibility tree. The Worker and tests, which build their own services, must not each open the
        /// extension's pipe or poll the browser.
        /// </param>
        public static HostServices CreateWindowsDefaults(bool browserSources = false)
        {
            WindowsBrightnessService brightness = new();
            return new HostServices
            {
                Audio = new WindowsAudioService(),
                AppAudio = new WindowsAppAudioService(),
                Brightness = brightness,
                Media = CreateMediaStack(browserSources),
                Hotkeys = new WindowsHotkeyService(),
                Metrics = new WindowsSystemMetricsService(),
                AudioLevels = new WindowsAudioLevelService(),
                Autostart = new WindowsAutostartService(),
                BrightnessChanges = new WindowsBrightnessChangeSource(brightness),
                OsdSuppressor = new WindowsNativeOsdSuppressor(),
                LegacyVolumeKeys = new WindowsLegacyMediaKeyHook(),
                Idle = new WindowsIdleService(),
                Fullscreen = new WindowsFullscreenProbe(),
                LockKeys = new WindowsLockKeysService(),
                Airplane = new WindowsAirplaneModeService(),
                AudioDevices = new WindowsAudioDeviceService(),
                ShellFlyoutTriggers = new WindowsShellFlyoutHook(),
            };
        }

        /// <summary>
        /// Windows SMTC, which carries title, artist and cover, plus (in the Host) the browser sources, which carry the like
        /// and dislike state SMTC does not have: see <see cref="BrowserSourceStack"/>.
        /// </summary>
        public static IMediaSessionService CreateMediaStack(bool browserSources = false)
        {
            WindowsMediaSessionService smtc = new();
            return browserSources
                ? new CompositeMediaSessionService(smtc, BrowserSourceStack.Create(() => smtc.Current))
                : new CompositeMediaSessionService(smtc);
        }
#endif
    }
}
