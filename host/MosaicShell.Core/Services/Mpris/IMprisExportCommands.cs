namespace MosaicShell.Core.Services.Mpris
{
    /// <summary>What the published MPRIS player does when a client presses play, skips or seeks.</summary>
    public interface IMprisExportCommands
    {
        Task PlayPauseAsync();

        Task PlayAsync();

        Task PauseAsync();

        Task NextAsync();

        Task PreviousAsync();

        /// <summary>Moves to <paramref name="positionSeconds"/> from the start of the track.</summary>
        Task SeekToAsync(double positionSeconds);
    }
}
