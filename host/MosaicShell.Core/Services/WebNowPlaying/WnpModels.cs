namespace MosaicShell.Core.Services.WebNowPlaying;

/// <summary>Active browser media from WebNowPlaying extension (WNPLIB revision 3).</summary>
public sealed class WnpPlayerSnapshot
{
    public long PortId { get; init; }
    public string Name { get; init; } = "";
    public string Title { get; init; } = "";
    public string Artist { get; init; } = "";
    public string Album { get; init; } = "";
    public string CoverSrc { get; init; } = "";
    public WnpState State { get; init; }
    public int PositionSeconds { get; init; }
    public int DurationSeconds { get; init; }
    public int Volume { get; init; } = 100;
    public ulong ActiveAt { get; init; }
    public byte[]? CoverPng { get; init; }
    public int Rating { get; init; }
    public int Repeat { get; init; } = 1;
    public bool Shuffle { get; init; }

    public bool IsPlaying => State == WnpState.Playing;
}

public enum WnpState
{
    Playing = 0,
    Paused = 1,
    Stopped = 2,
}

public interface IWebNowPlayingService : IDisposable
{
    /// <summary>Preferred active player (playing + volume, else most recently active).</summary>
    WnpPlayerSnapshot? Active { get; }
    int ConnectedClients { get; }
    int ListenPort { get; }
    event EventHandler? Changed;
}
