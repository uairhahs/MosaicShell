namespace MosaicShell.Core.Capabilities
{
    /// <summary>
    /// Arm/disarm and query armed capabilities. Implemented by in-process
    /// <see cref="CapabilityDaemon"/> or <see cref="Ipc.RemoteCapabilityHost"/> when Worker owns the daemon.
    /// </summary>
    public interface ICapabilityHost : IDisposable
    {
        IReadOnlyList<string> ArmedModuleIds { get; }
        bool IsArmed(string moduleId);
        Task<bool> ArmAsync(string moduleId, bool persist = true, CancellationToken cancellationToken = default);
        Task<bool> DisarmAsync(string moduleId, bool persist = true, CancellationToken cancellationToken = default);
        Task<bool> ReArmAsync(string moduleId, CancellationToken cancellationToken = default);
        string? GetHotkeyError(string moduleId);
        Task RestoreAsync(CancellationToken cancellationToken = default);
        /// <summary>
        /// Stops every capability. With <paramref name="persist"/> false the saved armed list is left as it was, which is
        /// what a shutdown wants: the same modules arm again on the next start.
        /// </summary>
        Task DisarmAllAsync(bool persist = true, CancellationToken cancellationToken = default);
        void Persist();
    }
}
