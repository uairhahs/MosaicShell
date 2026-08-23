using MosaicShell.Core.Capabilities;
using MosaicShell.Core.Modules.Tessera;
using MosaicShell.Core.Runtime;
using MosaicShell.Core.Services;
using MosaicShell.Core.Settings;

namespace MosaicShell.Core.Capabilities.BuiltIn;

public sealed class TesseraCapability : IModuleCapability
{
    private readonly HostServices _services;
    private readonly ICapabilityUiBridge _ui;
    private readonly TesseraFlyoutRequestBuilder _requests = new();
    private TesseraSettings _settings = new();
    private DateTime _settingsMtimeUtc = DateTime.MinValue;
    private readonly object _gate = new();
    private DateTimeOffset _lastShowUtc = DateTimeOffset.MinValue;
    private string _lastKind = "";
    private LockKeyState? _lastLock;
    private Timer? _mediaPoll;
    private Timer? _mediaPresentSettle;
    private bool _presentingMedia;
    private MediaSessionInfo? _lastMediaIdentity;

    public TesseraCapability(HostServices services, ICapabilityUiBridge ui)
    {
        _services = services;
        _ui = ui;
    }

    public string ModuleId => "Tessera";
    public bool IsArmed { get; private set; }

    public Task ArmAsync(CancellationToken cancellationToken = default)
    {
        if (IsArmed) return Task.CompletedTask;
        ReloadSettings();
        _services.Audio.Changed += OnVolume;
        _services.Media.Changed += OnMedia;
        _services.Media.ProgressChanged += OnMediaProgress;
        _services.BrightnessChanges.Changed += OnBrightness;
        _services.BrightnessChanges.Start();
        _services.OsdSuppressor.Start();
        _services.ShellFlyoutTriggers.Triggered += OnShellTrigger;
        _services.ShellFlyoutTriggers.Start();
        if (_settings.UseLegacyVolumeHooks)
        {
            _services.LegacyVolumeKeys.Pressed += OnLegacyKey;
            StartLegacyVolumeHook();
        }
        if (_settings.EnableLockFlyouts)
        {
            _services.LockKeys.Changed += OnLock;
            StartLockKeysHook();
        }
        if (_settings.EnableFlightFlyouts)
        {
            _services.Airplane.Changed += OnFlight;
            _services.Airplane.Start();
        }

        if (TesseraMediaFlyoutPolicy.MustPollTimelineWhileArmed
            && (_settings.EnableMediaFlyouts || _settings.ShowMediaStripOnVolume))
        {
            var ms = TesseraMediaFlyoutPolicy.ArmedTimelinePollMs;
            _mediaPoll = new Timer(_ =>
            {
                try { _services.Media.PumpTimeline(); }
                catch { /* soft-fail */ }
            }, null, ms, ms);
        }

        IsArmed = true;
        return Task.CompletedTask;
    }

    public Task DisarmAsync(CancellationToken cancellationToken = default)
    {
        if (!IsArmed) return Task.CompletedTask;
        _mediaPresentSettle?.Dispose();
        _mediaPresentSettle = null;
        _mediaPoll?.Dispose();
        _mediaPoll = null;
        _services.Audio.Changed -= OnVolume;
        _services.Media.Changed -= OnMedia;
        _services.Media.ProgressChanged -= OnMediaProgress;
        _services.BrightnessChanges.Changed -= OnBrightness;
        _services.BrightnessChanges.Stop();
        _services.OsdSuppressor.Stop();
        _services.ShellFlyoutTriggers.Triggered -= OnShellTrigger;
        _services.ShellFlyoutTriggers.Stop();
        _services.LegacyVolumeKeys.Pressed -= OnLegacyKey;
        StopLegacyVolumeHook();
        _services.LockKeys.Changed -= OnLock;
        StopLockKeysHook();
        _services.Airplane.Changed -= OnFlight;
        _services.Airplane.Stop();
        _ui.Flyouts.Hide(ModuleId);
        _lastMediaIdentity = null;
        IsArmed = false;
        return Task.CompletedTask;
    }

    private void OnShellTrigger(object? s, ShellFlyoutKind kind) =>
        _ui.RunOnHostThread(() =>
        {
            EnsureSettingsFresh();
            switch (kind)
            {
                case ShellFlyoutKind.Volume:
                    ShowOrUpdate("vol", trigger: TesseraFlyoutRefreshTrigger.VolumeTick);
                    break;
                case ShellFlyoutKind.Brightness:
                    ShowOrUpdate("bright", trigger: TesseraFlyoutRefreshTrigger.BrightnessTick);
                    break;
                case ShellFlyoutKind.Media:
                    if (_settings.EnableMediaFlyouts)
                        PresentMediaFlyout(pumpFirst: true, TesseraFlyoutRefreshTrigger.ShellMediaHook);
                    break;
            }
        });

    private void OnVolume(object? s, EventArgs e) =>
        _ui.RunOnHostThread(() => ShowOrUpdate("vol", trigger: TesseraFlyoutRefreshTrigger.VolumeTick));

    private void OnBrightness(object? s, EventArgs e) =>
        _ui.RunOnHostThread(() => ShowOrUpdate("bright", trigger: TesseraFlyoutRefreshTrigger.BrightnessTick));

    private void OnMedia(object? s, EventArgs e) =>
        _ui.RunOnHostThread(() =>
        {
            EnsureSettingsFresh();
            var current = _services.Media.Current;
            string lastKind;
            lock (_gate) lastKind = _lastKind;
            var visible = _ui.Flyouts.IsVisible(ModuleId);
            var action = TesseraMediaFlyoutPolicy.Resolve(
                _settings.EnableMediaFlyouts,
                visible,
                lastKind);

            if (action == TesseraMediaChangeAction.PresentMediaFlyout
                && visible
                && lastKind.Equals("media", StringComparison.OrdinalIgnoreCase)
                && !TesseraMediaFlyoutPolicy.IsTrackBoundary(_lastMediaIdentity, current))
            {
                SoftUpdateVisible("media");
                return;
            }

            switch (action)
            {
                case TesseraMediaChangeAction.SoftRefreshVisible:
                    SoftUpdateVisible(lastKind);
                    break;
                case TesseraMediaChangeAction.PresentMediaFlyout:
                    PresentMediaFlyout(pumpFirst: false, TesseraFlyoutRefreshTrigger.MediaSessionChanged);
                    break;
            }
        });

    /// <summary>Timeline ticks: update scrubber/time on an already-open flyout only.</summary>
    private void OnMediaProgress(object? s, EventArgs e)
    {
        if (!_ui.Flyouts.IsVisible(ModuleId)) return;
        string kind;
        lock (_gate) kind = _lastKind;
        if (kind is not ("vol" or "bright" or "media")) return;
        SoftUpdateVisible(kind);
    }

    private void SoftUpdateVisible(string kind)
    {
        try
        {
            // Timeline ticks must SoftRefresh only — ShowOrUpdate would reset auto-dismiss
            // every ArmedTimelinePollMs while a media flyout is open.
            _ui.Flyouts.SoftRefresh(BuildRequest(kind, null));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[TesseraCapability soft] {ex}");
        }
    }

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
        _ui.RunOnHostThread(() => ShowOrUpdate("locks", payload, TesseraFlyoutRefreshTrigger.StatusToggle));
    }

    private void OnFlight(object? s, EventArgs e)
    {
        EnsureSettingsFresh();
        if (!_settings.EnableFlightFlyouts || !_services.Airplane.IsSupported) return;
        var payload = new Dictionary<string, string>
        {
            ["on"] = _services.Airplane.IsEnabled ? "1" : "0"
        };
        _ui.RunOnHostThread(() => ShowOrUpdate("flight", payload, TesseraFlyoutRefreshTrigger.StatusToggle));
    }

    private void OnLegacyKey(object? s, LegacyVolumeKey key)
    {
        switch (key)
        {
            case LegacyVolumeKey.Up:
                _services.Audio.MasterVolume = StepVolume(
                    _services.Audio.MasterVolume,
                    Math.Max(1, (int)Math.Round(_settings.LegacyVolumeStep * 100)));
                break;
            case LegacyVolumeKey.Down:
                _services.Audio.MasterVolume = StepVolume(
                    _services.Audio.MasterVolume,
                    -Math.Max(1, (int)Math.Round(_settings.LegacyVolumeStep * 100)));
                break;
            case LegacyVolumeKey.Mute:
                _services.Audio.IsMuted = !_services.Audio.IsMuted;
                break;
        }
        ShowOrUpdate("vol", trigger: TesseraFlyoutRefreshTrigger.VolumeTick);
    }

    private static double StepVolume(double current, int deltaPercent) =>
        VolumePercent.Step(current, deltaPercent);

    private void PresentMediaFlyout(bool pumpFirst, TesseraFlyoutRefreshTrigger trigger)
    {
        if (_presentingMedia)
        {
            ShowOrUpdate("media", trigger: trigger);
            return;
        }

        _presentingMedia = true;
        try
        {
            if (pumpFirst && TesseraMediaPresentPolicy.ShouldPumpBeforeShellMediaPresent)
            {
                try { _services.Media.PumpTimeline(); }
                catch { /* soft-fail */ }
            }
            ShowOrUpdate("media", trigger: trigger);
        }
        finally
        {
            _presentingMedia = false;
        }
    }

    private void ShowOrUpdate(
        string kind,
        IReadOnlyDictionary<string, string>? payload = null,
        TesseraFlyoutRefreshTrigger trigger = TesseraFlyoutRefreshTrigger.VolumeTick)
    {
        try
        {
            string openKind;
            string style;
            lock (_gate)
            {
                EnsureSettingsFresh();
                openKind = _lastKind;
                style = _settings.Style;
                _lastKind = kind;
                _lastShowUtc = DateTimeOffset.UtcNow;
            }

            try { _services.OsdSuppressor.SuppressBurst(3500); } catch { /* soft-fail */ }

            var visible = _ui.Flyouts.IsVisible(ModuleId);
            var action = TesseraFlyoutRefreshPolicy.ResolvePresentation(
                trigger,
                visible,
                openKind,
                kind,
                style,
                style,
                _settings.EnableMediaFlyouts);

            var request = BuildRequest(kind, payload);
            var currentMedia = _services.Media.Current;

            // Present when cold or structural; Patch when visible same kind/style.
            // Host coalesces Patch and must not Present/restack on that path.
            if (action == TesseraFlyoutSyncAction.Present)
                _ui.Flyouts.Show(request);
            else if (TesseraFlyoutDismissPolicy.ShouldResetAutoDismiss(trigger, _lastMediaIdentity, currentMedia))
            {
                _ui.Flyouts.Update(request);
                if (trigger == TesseraFlyoutRefreshTrigger.MediaSessionChanged
                    && kind.Equals("media", StringComparison.OrdinalIgnoreCase))
                    _lastMediaIdentity = currentMedia;
            }
            else
                _ui.Flyouts.SoftRefresh(request);

            if (TesseraMediaPresentPolicy.ShouldSchedulePresentSettle(kind, action))
                ScheduleMediaPresentSettle();

            if (action == TesseraFlyoutSyncAction.Present
                && kind.Equals("media", StringComparison.OrdinalIgnoreCase))
                _lastMediaIdentity = currentMedia;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[TesseraCapability] {ex}");
        }
    }

    private FlyoutRequest BuildRequest(string kind, IReadOnlyDictionary<string, string>? payload)
    {
        EnsureSettingsFresh();
        return _requests.Build(_services, _settings, kind, payload, _lastLock);
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

    private void ScheduleMediaPresentSettle()
    {
        _mediaPresentSettle?.Dispose();
        var ms = TesseraMediaPresentPolicy.PresentSettleMs;
        _mediaPresentSettle = new Timer(_ =>
        {
            try
            {
                if (!IsArmed) return;
                _ui.RunOnHostThread(() =>
                {
                    try
                    {
                        _services.Media.PumpTimeline();
                        string kind;
                        lock (_gate) kind = _lastKind;
                        if (!kind.Equals("media", StringComparison.OrdinalIgnoreCase)) return;
                        if (!_ui.Flyouts.IsVisible(ModuleId))
                        {
                            PresentMediaFlyout(
                                pumpFirst: false,
                                TesseraFlyoutRefreshTrigger.MediaSessionChanged);
                            return;
                        }

                        SoftUpdateVisible("media");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[TesseraCapability media settle] {ex}");
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TesseraCapability media settle timer] {ex}");
            }
        }, null, ms, Timeout.Infinite);
    }

    private void StartLockKeysHook() => _services.LockKeys.Start();

    private void StopLockKeysHook() => _services.LockKeys.Stop();

    private void StartLegacyVolumeHook() =>
        _ui.RunOnHostThread(() => _services.LegacyVolumeKeys.Start());

    private void StopLegacyVolumeHook() =>
        _ui.RunOnHostThread(() => _services.LegacyVolumeKeys.Stop());

    public void Dispose() => DisarmAsync().GetAwaiter().GetResult();
}
