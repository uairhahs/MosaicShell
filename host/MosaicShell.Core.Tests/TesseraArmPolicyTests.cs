using FluentAssertions;
using MosaicShell.Core.Modules.Tessera;

namespace MosaicShell.Core.Tests
{
    public class TesseraArmPolicyTests
    {

        [Theory]
        [InlineData("LegacyVolumeKeys", true)]
        [InlineData("LockKeys", false)]
        [InlineData("ShellFlyoutTriggers", false)]
        [InlineData("Audio", false)]
        public void RequiresHostThreadForStart_matches_hook_services(string id, bool expected)
        {
            _ = TesseraArmPolicy.RequiresHostThreadForStart(id).Should().Be(expected);
        }
    }
}
