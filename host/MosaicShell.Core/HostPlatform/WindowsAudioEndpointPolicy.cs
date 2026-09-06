using NAudio.CoreAudioApi;

namespace MosaicShell.Core.HostPlatform
{
    /// <summary>
    /// Whether Core Audio default endpoints exist (headless CI often has none).
    /// </summary>
    public static class WindowsAudioEndpointPolicy
    {
        /// <summary>True when a default multimedia render endpoint is available.</summary>
        public static bool HasDefaultRenderEndpoint
        {
            get
            {
                try
                {
                    using MMDeviceEnumerator enumerator = new();
                    using MMDevice? device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
                    return device is not null;
                }
                catch (CoreAudioException)
                {
                    return false;
                }
            }
        }
    }
}
