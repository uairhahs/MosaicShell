using MosaicShell.Core.Capabilities;
using MosaicShell.Core.Runtime;
using MosaicShell.Core.Services;
using MosaicShell.Core.Settings;

namespace MosaicShell.Core.Capabilities.BuiltIn;

public sealed class TesseraCapability : IModuleCapability
{
    private readonly HostServices _services;
    private readonly ICapabilityUiBridge _ui;
    private TesseraSettings _settings = new();
    private readonly object _gate = new();
    private DateTimeOffset _lastShowUtc = DateTimeOffset.MinValue;
    private string _lastKind = "";
    private LockKeyState? _lastLock;

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
        _settings = ModuleSettingsStore.Load("Tessera", () => new TesseraSettings());
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
            _services.LegacyVolumeKeys.Start();
        }
        if (_settings.EnableLockFlyouts)
        {
            _services.LockKeys.Changed += OnLock;
            _services.LockKeys.Start();
        }
        if (_settings.EnableFlightFlyouts)
        {
            _services.Airplane.Changed += OnFlight;
            _services.Airplane.Start();
        }

        IsArmed = true;
        return Task.CompletedTask;
    }

    public Task DisarmAsync(CancellationToken cancellationToken = default)
    {
        if (!IsArmed) return Task.CompletedTask;
        _services.Audio.Changed -= OnVolume;
        _services.Media.Changed -= OnMedia;
        _services.Media.ProgressChanged -= OnMediaProgress;
        _services.BrightnessChanges.Changed -= OnBrightness;
        _services.BrightnessChanges.Stop();
        _services.OsdSuppressor.Stop();
        _services.ShellFlyoutTriggers.Triggered -= OnShellTrigger;
        _services.ShellFlyoutTriggers.Stop();
        _services.LegacyVolumeKeys.Pressed -= OnLegacyKey;
        _services.LegacyVolumeKeys.Stop();
        _services.LockKeys.Changed -= OnLock;
        _services.LockKeys.Stop();
        _services.Airplane.Changed -= OnFlight;
        _services.Airplane.Stop();
        _ui.Flyouts.Hide(ModuleId);
        IsArmed = false;
        return Task.CompletedTask;
    }

    private void OnShellTrigger(object? s, ShellFlyoutKind kind)
    {
        switch (kind)
        {
            case ShellFlyoutKind.Volume:
                ShowOrUpdate("vol");
                break;
            case ShellFlyoutKind.Brightness:
                ShowOrUpdate("bright");
                break;
            case ShellFlyoutKind.Media:
                if (_settings.EnableMediaFlyouts) ShowOrUpdate("media");
                break;
        }
    }

    private void OnVolume(object? s, EventArgs e) => ShowOrUpdate("vol");
    private void OnBrightness(object? s, EventArgs e) => ShowOrUpdate("bright");
    private void OnMedia(object? s, EventArgs e)
    {
        // Prefer refreshing an already-visible volume/brightness strip (art/title) in place.
        // Only open a dedicated media flyout when nothing is showing (or media flyouts enabled).
        string? refreshKind = null;
        lock (_gate)
        {
            if (_ui.Flyouts.IsVisible(ModuleId)
                && (_lastKind.Equals("vol", StringComparison.OrdinalIgnoreCase)
                    || _lastKind.Equals("bright", StringComparison.OrdinalIgnoreCase)
                    || _lastKind.Equals("media", StringComparison.OrdinalIgnoreCase)))
                refreshKind = _lastKind;
        }

        if (refreshKind is not null)
        {
            SoftUpdateVisible(refreshKind);
            return;
        }

        if (_settings.EnableMediaFlyouts) ShowOrUpdate("media");
    }

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
            // Progress / art refresh — must not reset auto-dismiss
            _ui.Flyouts.SoftRefresh(BuildRequest(kind, null));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[TesseraCapability soft] {ex}");
        }
    }

    private void OnLock(object? s, LockKeyState state)
    {
        if (!_settings.EnableLockFlyouts) return;
        _lastLock = state;
        ShowOrUpdate("locks", new Dictionary<string, string>
        {
            ["lock"] = state.Key.ToString(),
            ["on"] = state.IsOn ? "1" : "0"
        });
    }

    private void OnFlight(object? s, EventArgs e)
    {
        if (!_settings.EnableFlightFlyouts || !_services.Airplane.IsSupported) return;
        ShowOrUpdate("flight", new Dictionary<string, string>
        {
            ["on"] = _services.Airplane.IsEnabled ? "1" : "0"
        });
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
        ShowOrUpdate("vol");
    }

    private static double StepVolume(double current, int deltaPercent) =>
        VolumePercent.Step(current, deltaPercent);

    private void ShowOrUpdate(string kind, IReadOnlyDictionary<string, string>? payload = null)
    {
        try
        {
            lock (_gate)
            {
                _settings = ModuleSettingsStore.Load("Tessera", () => new TesseraSettings());
                var now = DateTimeOffset.UtcNow;
                if (kind == _lastKind && (now - _lastShowUtc).TotalMilliseconds < 40 && _ui.Flyouts.IsVisible(ModuleId))
                {
                    _ui.Flyouts.Update(BuildRequest(kind, payload));
                    return;
                }
                _lastKind = kind;
                _lastShowUtc = now;
            }

            try { _services.OsdSuppressor.SuppressBurst(3000); } catch { /* soft-fail */ }
            var request = BuildRequest(kind, payload);
            if (_ui.Flyouts.IsVisible(ModuleId))
                _ui.Flyouts.Update(request);
            else
                _ui.Flyouts.Show(request);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[TesseraCapability] {ex}");
        }
    }

    private FlyoutRequest BuildRequest(string kind, IReadOnlyDictionary<string, string>? payload)
    {
        var p = payload is null ? new Dictionary<string, string>() : new Dictionary<string, string>(payload);
        p["volume"] = _services.Audio.MasterVolume.ToString("0.###");
        p["muted"] = _services.Audio.IsMuted ? "1" : "0";
        p["brightness"] = _services.Brightness.IsSupported ? _services.Brightness.Brightness.ToString("0.###") : "0.5";
        p["mediaTitle"] = _services.Media.Current?.Title ?? "";
        p["mediaArtist"] = _services.Media.Current?.Artist ?? "";
        p["mediaPlaying"] = _services.Media.Current?.IsPlaying == true ? "1" : "0";
        p["showMediaStrip"] = _settings.ShowMediaStripOnVolume ? "1" : "0";
        if (_lastLock is not null && kind == "locks")
        {
            p["lock"] = _lastLock.Key.ToString();
            p["on"] = _lastLock.IsOn ? "1" : "0";
        }
        if (kind == "flight")
            p["on"] = _services.Airplane.IsEnabled ? "1" : "0";

        return new FlyoutRequest(
            ModuleId,
            kind,
            _settings.Style,
            _settings.Position,
            _settings.AutoDismissMs,
            p,
            _settings.MonitorIndex,
            _settings.XPad,
            _settings.YPad,
            _settings.Ani,
            _settings.AniDir);
    }

    public void Dispose() => DisarmAsync().GetAwaiter().GetResult();
}
