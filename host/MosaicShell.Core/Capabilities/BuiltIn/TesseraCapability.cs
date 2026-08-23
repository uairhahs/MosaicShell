using MosaicShell.Core.Capabilities;
using MosaicShell.Core.Capabilities.Platform;
using MosaicShell.Core.Modules.Tessera;
using MosaicShell.Core.Runtime;
using MosaicShell.Core.Services;
using MosaicShell.Core.Settings;

namespace MosaicShell.Core.Capabilities.BuiltIn;

/// <summary>
/// Tessera OSD replacement. Platform (<see cref="CapabilityFlyoutSession"/>,
/// <see cref="MediaSessionPlatform"/>) owns flyout routing and media signals;
/// this module builds requests and wires Tessera-specific hooks/settings.
/// </summary>
public sealed class TesseraCapability : IModuleCapability
{
    private readonly ICapabilityContext _ctx;
    private readonly TesseraFlyoutRequestBuilder _requests = new();
    private TesseraSettings _settings = new();
    private DateTime _settingsMtimeUtc = DateTime.MinValue;
    private LockKeyState? _lastLock;
    private bool _presentingMedia;
    private bool _mediaAcquired;

    public TesseraCapability(ICapabilityContext context) => _ctx = context;

    public string ModuleId => "Tessera";
    public bool IsArmed { get; private set; }

    private HostServices Services => _ctx.Services;
    private CapabilityFlyoutSession Flyouts => _ctx.Flyouts;

    public Task ArmAsync(CancellationToken cancellationToken = default)
    {
        if (IsArmed) return Task.CompletedTask;
        ReloadSettings();
        Services.Audio.Changed += OnVolume;
        Services.BrightnessChanges.Changed += OnBrightness;
        Services.BrightnessChanges.Start();
        Services.OsdSuppressor.Start();
        Services.ShellFlyoutTriggers.Triggered += OnShellTrigger;
        Services.ShellFlyoutTriggers.Start();
        if (_settings.UseLegacyVolumeHooks)
        {
            Services.LegacyVolumeKeys.Pressed += OnLegacyKey;
            StartLegacyVolumeHook();
        }
        if (_settings.EnableLockFlyouts)
        {
            Services.LockKeys.Changed += OnLock;
            StartLockKeysHook();
        }
        if (_settings.EnableFlightFlyouts)
        {
            Services.Airplane.Changed += OnFlight;
            Services.Airplane.Start();
        }

        if (_settings.EnableMediaFlyouts || _settings.ShowMediaStripOnVolume)
        {
            _ctx.Media.Acquire();
            _ctx.Media.Signal += OnMediaSignal;
            _mediaAcquired = true;
        }

        Flyouts.SetPresentSettleHandler(OnMediaPresentSettle);
        IsArmed = true;
        return Task.CompletedTask;
    }

    public Task DisarmAsync(CancellationToken cancellationToken = default)
    {
        if (!IsArmed) return Task.CompletedTask;
        Flyouts.SetPresentSettleHandler(null);
        if (_mediaAcquired)
        {
            _ctx.Media.Signal -= OnMediaSignal;
            _ctx.Media.Release();
            _mediaAcquired = false;
        }
        Services.Audio.Changed -= OnVolume;
        Services.BrightnessChanges.Changed -= OnBrightness;
        Services.BrightnessChanges.Stop();
        Services.OsdSuppressor.Stop();
        Services.ShellFlyoutTriggers.Triggered -= OnShellTrigger;
        Services.ShellFlyoutTriggers.Stop();
        Services.LegacyVolumeKeys.Pressed -= OnLegacyKey;
        StopLegacyVolumeHook();
        Services.LockKeys.Changed -= OnLock;
        StopLockKeysHook();
        Services.Airplane.Changed -= OnFlight;
        Services.Airplane.Stop();
        Flyouts.Hide();
        Flyouts.ClearMediaIdentity();
        IsArmed = false;
        return Task.CompletedTask;
    }

    private void OnShellTrigger(object? s, ShellFlyoutKind kind) =>
        _ctx.Ui.RunOnHostThread(() =>
        {
            EnsureSettingsFresh();
            switch (kind)
            {
                case ShellFlyoutKind.Volume:
                    RouteFlyout("vol", FlyoutSyncTrigger.Volume);
                    break;
                case ShellFlyoutKind.Brightness:
                    RouteFlyout("bright", FlyoutSyncTrigger.Brightness);
                    break;
                case ShellFlyoutKind.Media:
                    if (_settings.EnableMediaFlyouts)
                        PresentMediaFlyout(pumpFirst: true, FlyoutSyncTrigger.ShellMedia);
                    break;
            }
        });

    private void OnVolume(object? s, EventArgs e) =>
        _ctx.Ui.RunOnHostThread(() => RouteFlyout("vol", FlyoutSyncTrigger.Volume));

    private void OnBrightness(object? s, EventArgs e) =>
        _ctx.Ui.RunOnHostThread(() => RouteFlyout("bright", FlyoutSyncTrigger.Brightness));

    private void OnMediaSignal(MediaSessionSignal signal) =>
        _ctx.Ui.RunOnHostThread(() =>
        {
            EnsureSettingsFresh();
            if (signal.Kind == MediaSessionSignalKind.Progress)
            {
                if (!Flyouts.IsVisible) return;
                if (Flyouts.OpenKind is not ("vol" or "bright" or "media")) return;
                Flyouts.SoftRefresh(BuildRequest(Flyouts.OpenKind, null));
                return;
            }

            var current = signal.Current;
            var visible = Flyouts.IsVisible;
            var action = MediaFlyoutRouter.Resolve(
                _settings.EnableMediaFlyouts,
                visible,
                Flyouts.OpenKind);

            if (action == MediaFlyoutAction.PresentMediaFlyout
                && visible
                && Flyouts.OpenKind.Equals("media", StringComparison.OrdinalIgnoreCase)
                && !signal.IsTrackBoundary)
            {
                Flyouts.SoftRefresh(BuildRequest("media", null));
                return;
            }

            switch (action)
            {
                case MediaFlyoutAction.SoftRefreshVisible:
                    Flyouts.SoftRefresh(BuildRequest(Flyouts.OpenKind, null));
                    break;
                case MediaFlyoutAction.PresentMediaFlyout:
                    if (!Flyouts.ShouldColdPresentMedia(FlyoutSyncTrigger.MediaSession))
                        break;
                    PresentMediaFlyout(pumpFirst: false, FlyoutSyncTrigger.MediaSession);
                    break;
            }
        });

    private void OnLock(object? s, LockKeyState state)
    {
        EnsureSettingsFresh();
        if (!_settings.EnableLockFlyouts) return;
        _lastLock = state;
        var payload = new Dictionary<string, string>
        {
            ["lock"] = state.Key.ToString(),
            ["on"] = state.IsOn ? "1" : "0"
        };
        _ctx.Ui.RunOnHostThread(() => RouteFlyout("locks", FlyoutSyncTrigger.StatusToggle, payload));
    }

    private void OnFlight(object? s, EventArgs e)
    {
        EnsureSettingsFresh();
        if (!_settings.EnableFlightFlyouts || !Services.Airplane.IsSupported) return;
        var payload = new Dictionary<string, string>
        {
            ["on"] = Services.Airplane.IsEnabled ? "1" : "0"
        };
        _ctx.Ui.RunOnHostThread(() => RouteFlyout("flight", FlyoutSyncTrigger.StatusToggle, payload));
    }

    private void OnLegacyKey(object? s, LegacyVolumeKey key)
    {
        switch (key)
        {
            case LegacyVolumeKey.Up:
                Services.Audio.MasterVolume = StepVolume(
                    Services.Audio.MasterVolume,
                    Math.Max(1, (int)Math.Round(_settings.LegacyVolumeStep * 100)));
                break;
            case LegacyVolumeKey.Down:
                Services.Audio.MasterVolume = StepVolume(
                    Services.Audio.MasterVolume,
                    -Math.Max(1, (int)Math.Round(_settings.LegacyVolumeStep * 100)));
                break;
            case LegacyVolumeKey.Mute:
                Services.Audio.IsMuted = !Services.Audio.IsMuted;
                break;
        }
        RouteFlyout("vol", FlyoutSyncTrigger.Volume);
    }

    private static double StepVolume(double current, int deltaPercent) =>
        VolumePercent.Step(current, deltaPercent);

    private void PresentMediaFlyout(bool pumpFirst, FlyoutSyncTrigger trigger)
    {
        if (!Flyouts.ShouldColdPresentMedia(trigger))
            return;

        if (_presentingMedia)
        {
            RouteFlyout("media", trigger);
            return;
        }

        _presentingMedia = true;
        try
        {
            if (pumpFirst && MediaPresentPolicy.ShouldPumpBeforeShellMediaPresent)
                _ctx.Media.PumpTimeline();
            RouteFlyout("media", trigger);
        }
        finally
        {
            _presentingMedia = false;
        }
    }

    private void RouteFlyout(
        string kind,
        FlyoutSyncTrigger trigger,
        IReadOnlyDictionary<string, string>? payload = null)
    {
        try
        {
            EnsureSettingsFresh();
            try { Services.OsdSuppressor.SuppressBurst(3500); } catch { /* soft-fail */ }

            var request = BuildRequest(kind, payload);
            Flyouts.Route(
                request,
                trigger,
                _settings.EnableMediaFlyouts,
                _settings.Style,
                Services.Media.Current);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[TesseraCapability] {ex}");
        }
    }

    private void OnMediaPresentSettle()
    {
        if (!IsArmed) return;
        try
        {
            _ctx.Media.PumpTimeline();
            if (!Flyouts.OpenKind.Equals("media", StringComparison.OrdinalIgnoreCase)) return;
            if (!Flyouts.IsVisible)
            {
                if (!Flyouts.ShouldColdPresentMedia(FlyoutSyncTrigger.MediaSession))
                    return;
                PresentMediaFlyout(pumpFirst: false, FlyoutSyncTrigger.MediaSession);
                return;
            }

            Flyouts.SoftRefresh(BuildRequest("media", null));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[TesseraCapability media settle] {ex}");
        }
    }

    private FlyoutRequest BuildRequest(string kind, IReadOnlyDictionary<string, string>? payload)
    {
        EnsureSettingsFresh();
        return _requests.Build(Services, _settings, kind, payload, _lastLock);
    }

    private void ReloadSettings()
    {
        _settings = ModuleSettingsStore.Load("Tessera", () => new TesseraSettings());
        var path = ModuleSettingsStore.PathFor("Tessera");
        _settingsMtimeUtc = File.Exists(path) ? File.GetLastWriteTimeUtc(path) : DateTime.MinValue;
    }

    private void EnsureSettingsFresh()
    {
        var path = ModuleSettingsStore.PathFor("Tessera");
        var mtime = File.Exists(path) ? File.GetLastWriteTimeUtc(path) : DateTime.MinValue;
        if (mtime != _settingsMtimeUtc)
            ReloadSettings();
    }

    private void StartLockKeysHook() => Services.LockKeys.Start();
    private void StopLockKeysHook() => Services.LockKeys.Stop();
    private void StartLegacyVolumeHook() =>
        _ctx.Ui.RunOnHostThread(() => Services.LegacyVolumeKeys.Start());
    private void StopLegacyVolumeHook() =>
        _ctx.Ui.RunOnHostThread(() => Services.LegacyVolumeKeys.Stop());

    public void Dispose() => DisarmAsync().GetAwaiter().GetResult();
}
