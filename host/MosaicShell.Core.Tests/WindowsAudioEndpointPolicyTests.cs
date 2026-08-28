using MosaicShell.Core.HostPlatform;

namespace MosaicShell.Core.Tests
{
    public class WindowsAudioEndpointPolicyTests
    {
        [Fact]
        public void HasDefaultRenderEndpoint_does_not_throw()
        {
            bool _ = WindowsAudioEndpointPolicy.HasDefaultRenderEndpoint;
        }
    }
}
