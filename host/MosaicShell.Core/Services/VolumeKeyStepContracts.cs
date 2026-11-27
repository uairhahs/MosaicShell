using MosaicShell.Core.HostPlatform;

namespace MosaicShell.Core.Services
{
    /// <summary>The step one volume key press makes, where the desktop owns it (<see cref="VolumeKeyStepPolicy"/>).</summary>
    public interface IVolumeKeyStep
    {
        VolumeKeyStepSource Source { get; }

        /// <summary>The step in percent points, or null when it cannot be read (or Tessera's own settings hold it).</summary>
        int? Read();

        /// <summary>Stores the step, clamped to the owner's range; false when it could not be stored.</summary>
        bool Write(int percent);
    }

    /// <summary>No step MosaicShell can reach.</summary>
    public sealed class NullVolumeKeyStep : IVolumeKeyStep
    {
        public VolumeKeyStepSource Source => VolumeKeyStepSource.None;

        public int? Read()
        {
            return null;
        }

        public bool Write(int percent)
        {
            return false;
        }
    }

    /// <summary>Windows: Tessera's key hook steps the volume, and the step lives in Tessera's settings.</summary>
    public sealed class OwnHookVolumeKeyStep : IVolumeKeyStep
    {
        public VolumeKeyStepSource Source => VolumeKeyStepSource.OwnHook;

        public int? Read()
        {
            return null;
        }

        public bool Write(int percent)
        {
            return false;
        }
    }
}
