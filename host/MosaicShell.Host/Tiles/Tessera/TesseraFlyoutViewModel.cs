using MosaicShell.Core.Capabilities;
using MosaicShell.Core.Runtime;
using MosaicShell.Core.Services;
using MosaicShell.Core.Settings;

namespace MosaicShell.Host.Tiles.Tessera;

public sealed class TesseraFlyoutViewModel
{
    private TesseraFlyoutViewModel(HostServices services, string kind, string styleId, IReadOnlyDictionary<string, string> payload)
    {
        Services = services;
        Kind = kind;
        StyleId = styleId;
        Payload = payload;
        Volume = Parse(payload, "volume", services.Audio.MasterVolume);
        IsMuted = payload.GetValueOrDefault("muted") == "1" || services.Audio.IsMuted;
        Brightness = Parse(payload, "brightness", services.Brightness.IsSupported ? services.Brightness.Brightness : 0.5);
        var media = services.Media.Current;
        MediaTitle = payload.GetValueOrDefault("mediaTitle") ?? media?.Title ?? "No media";
        MediaArtist = payload.GetValueOrDefault("mediaArtist") ?? media?.Artist ?? "";
        IsPlaying = payload.GetValueOrDefault("mediaPlaying") == "1" || media?.IsPlaying == true;
        ThumbnailPng = media?.ThumbnailPng;
        MediaPositionSeconds = media?.PositionSeconds ?? 0;
        MediaDurationSeconds = media?.DurationSeconds ?? 0;
        ShowMediaStrip = payload.GetValueOrDefault("showMediaStrip") == "1"
                         && !string.IsNullOrWhiteSpace(MediaTitle)
                         && MediaTitle != "No media";
        LockName = payload.GetValueOrDefault("lock") ?? "CapsLock";
        LockOn = payload.GetValueOrDefault("on") == "1";
        FlightOn = payload.GetValueOrDefault("on") == "1";
        Settings = ModuleSettingsStore.Load("Tessera", () => new TesseraSettings());
    }

    public static TesseraFlyoutViewModel FromRequest(HostServices services, FlyoutRequest request) =>
        new(services, request.Kind, request.StyleId ?? "Fluent",
            request.Payload ?? new Dictionary<string, string>());

    public HostServices Services { get; }
    public TesseraSettings Settings { get; }
    public string Kind { get; }
    public string StyleId { get; }
    public IReadOnlyDictionary<string, string> Payload { get; }
    public double Volume { get; set; }
    public bool IsMuted { get; set; }
    public double Brightness { get; set; }
    public string MediaTitle { get; }
    public string MediaArtist { get; }
    public bool IsPlaying { get; }
    public byte[]? ThumbnailPng { get; }
    public double MediaPositionSeconds { get; }
    public double MediaDurationSeconds { get; }
    public double MediaProgress =>
        MediaDurationSeconds > 0.5 ? Math.Clamp(MediaPositionSeconds / MediaDurationSeconds, 0, 1) : 0;
    public bool ShowMediaStrip { get; }
    public string LockName { get; }
    public bool LockOn { get; }
    public bool FlightOn { get; }

    public double PrimaryValue => Kind.Equals("bright", StringComparison.OrdinalIgnoreCase) ? Brightness : Volume;
    public string PrimaryPercent => Kind.Equals("bright", StringComparison.OrdinalIgnoreCase)
        ? $"{VolumePercent.ToPercent(Brightness)}"
        : IsMuted ? "Mute" : $"{VolumePercent.ToPercent(Volume)}";

    public string KindLabel => Kind.ToLowerInvariant() switch
    {
        "bright" => "Brightness",
        "media" => "Now playing",
        "locks" => LockOn ? $"{LockName} On" : $"{LockName} Off",
        "flight" => FlightOn ? "Airplane mode On" : "Airplane mode Off",
        _ => IsMuted ? "Muted" : "Volume"
    };

    public void ApplyPrimary(double v)
    {
        v = VolumePercent.Quantize(v);
        if (Kind.Equals("bright", StringComparison.OrdinalIgnoreCase))
        {
            if (Math.Abs(Brightness - v) < 0.004) return;
            Brightness = v;
            if (Services.Brightness.IsSupported) Services.Brightness.Brightness = v;
        }
        else
        {
            if (VolumePercent.ToPercent(Volume) == VolumePercent.ToPercent(v) && !IsMuted)
                return;
            Volume = v;
            if (VolumePercent.ToPercent(Services.Audio.MasterVolume) != VolumePercent.ToPercent(v))
                Services.Audio.MasterVolume = v;
            if (v > 0.001 && IsMuted)
            {
                IsMuted = false;
                if (Services.Audio.IsMuted)
                    Services.Audio.IsMuted = false;
            }
        }
    }

    public void Nudge(double delta)
    {
        if (Kind is "locks" or "flight" or "media") return;
        // Interpret small deltas as percent steps (legacy BindWheel used 0.02)
        var step = Math.Abs(delta) < 0.015 ? 1 : VolumePercent.ToPercent(Math.Abs(delta));
        if (step < 1) step = 1;
        ApplyPrimary(VolumePercent.Step(PrimaryValue, delta >= 0 ? step : -step));
    }

    public void ToggleMute()
    {
        IsMuted = !IsMuted;
        Services.Audio.IsMuted = IsMuted;
    }

    public Task PlayPauseAsync() => Services.Media.PlayPauseAsync();
    public Task NextAsync() => Services.Media.NextAsync();
    public Task PreviousAsync() => Services.Media.PreviousAsync();
    public Task SeekAsync(double seconds) => Services.Media.SeekAsync(seconds);

    public async Task ToggleShuffleAsync(Material.Icons.Avalonia.MaterialIcon? icon = null)
    {
        await Services.Media.ToggleShuffleAsync();
        if (icon is not null)
            icon.Foreground = new Avalonia.Media.SolidColorBrush(
                Avalonia.Media.Color.FromArgb(255, 255, 255, 255));
    }

    public async Task ToggleRepeatAsync(Material.Icons.Avalonia.MaterialIcon? icon = null)
    {
        await Services.Media.ToggleRepeatAsync();
        if (icon is not null)
            icon.Foreground = new Avalonia.Media.SolidColorBrush(
                Avalonia.Media.Color.FromArgb(255, 255, 255, 255));
    }

    public async Task ToggleLikeAsync(Material.Icons.Avalonia.MaterialIcon? icon = null)
    {
        await Services.Media.ToggleLikeAsync();
        if (icon is not null)
        {
            var on = icon.Kind != Material.Icons.MaterialIconKind.Heart;
            icon.Kind = on ? Material.Icons.MaterialIconKind.Heart : Material.Icons.MaterialIconKind.HeartOutline;
            icon.Foreground = on
                ? new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromRgb(255, 80, 100))
                : TesseraPalette.FontBrush;
        }
    }

    public IReadOnlyList<AudioOutputDevice> Devices => Services.AudioDevices.GetOutputDevices();

    private static double Parse(IReadOnlyDictionary<string, string> p, string key, double fallback) =>
        p.TryGetValue(key, out var s) && double.TryParse(s, out var v) ? v : fallback;
}
