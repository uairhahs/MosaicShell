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
                         && IsPlaying
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
        ? $"{(int)(Brightness * 100)}"
        : IsMuted ? "Mute" : $"{(int)(Volume * 100)}";

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
        v = Math.Clamp(v, 0, 1);
        if (Kind.Equals("bright", StringComparison.OrdinalIgnoreCase))
        {
            if (Math.Abs(Brightness - v) < 0.0005) return;
            Brightness = v;
            if (Services.Brightness.IsSupported) Services.Brightness.Brightness = v;
        }
        else
        {
            if (Math.Abs(Volume - v) < 0.0005 && !IsMuted) return;
            Volume = v;
            if (Math.Abs(Services.Audio.MasterVolume - v) >= 0.0005)
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
        ApplyPrimary(Math.Clamp(PrimaryValue + delta, 0, 1));
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

    public IReadOnlyList<AudioOutputDevice> Devices => Services.AudioDevices.GetOutputDevices();

    private static double Parse(IReadOnlyDictionary<string, string> p, string key, double fallback) =>
        p.TryGetValue(key, out var s) && double.TryParse(s, out var v) ? v : fallback;
}
