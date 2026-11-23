namespace MosaicShell.Core.Services.Mpris
{
    /// <summary>Where the Host's own MPRIS player is published (the session bus on Linux).</summary>
    public interface IMprisExport : IDisposable
    {
        /// <summary>Publishes <paramref name="state"/>, or withdraws the player when it is null.</summary>
        Task PublishAsync(MprisExportedState? state);
    }
}
