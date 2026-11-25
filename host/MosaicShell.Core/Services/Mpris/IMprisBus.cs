namespace MosaicShell.Core.Services.Mpris
{
    /// <summary>A player's properties changed; <paramref name="Seeked"/> when it was only a Seeked signal.</summary>
    public sealed record MprisPlayerSignal(string BusName, bool Seeked);

    /// <summary>
    /// The session bus as <see cref="MprisMediaSessionService"/> needs it: list and read players, send them commands,
    /// hear when they change. Implemented over D-Bus by <c>DBusMprisBus</c> and by a fake in tests.
    /// Every call may throw when a player vanishes mid-call; callers treat that as "player gone".
    /// </summary>
    public interface IMprisBus : IDisposable
    {
        /// <summary>A player changed one of its properties, or seeked.</summary>
        event EventHandler<MprisPlayerSignal>? PlayerChanged;

        /// <summary>A player appeared on or left the bus.</summary>
        event EventHandler? PlayersChanged;

        Task ConnectAsync();
        Task<IReadOnlyList<string>> ListPlayersAsync();

        /// <summary>Everything about one player, or null when it is gone.</summary>
        Task<MprisPlayerState?> ReadPlayerAsync(string busName);

        /// <summary>
        /// Position in microseconds. MPRIS does not signal position as it advances, so this is polled; null when the
        /// player is gone or does not report it.
        /// </summary>
        Task<long?> ReadPositionAsync(string busName);

        Task PlayPauseAsync(string busName);
        Task NextAsync(string busName);
        Task PreviousAsync(string busName);
        Task SetPositionAsync(string busName, string trackId, long positionMicros);
        Task SeekByAsync(string busName, long offsetMicros);
        Task SetShuffleAsync(string busName, bool shuffle);
        Task SetLoopStatusAsync(string busName, string loopStatus);
    }

    /// <summary>Album art by <c>mpris:artUrl</c>.</summary>
    public interface IMprisArtSource : IDisposable
    {
        /// <summary>Raised when art that was not ready on an earlier <see cref="TryGet"/> has arrived.</summary>
        event EventHandler? Fetched;

        /// <summary>The image now, or null (then a fetch may have started; <see cref="Fetched"/> follows). Never blocks on the network.</summary>
        byte[]? TryGet(string url);
    }
}
