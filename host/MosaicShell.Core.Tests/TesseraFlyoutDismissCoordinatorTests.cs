using FluentAssertions;
using MosaicShell.Core.Modules.Tessera;

namespace MosaicShell.Core.Tests
{
    public class TesseraFlyoutDismissCoordinatorTests
    {
        [Fact]
        public void Session_owns_auto_dismiss_and_ipc_must_echo_snapshot()
        {
            _ = TesseraFlyoutDismissCoordinator.SessionOwnsAutoDismissClock.Should().BeTrue();
            _ = TesseraFlyoutDismissCoordinator.IpcMustEchoSessionSnapshot.Should().BeTrue();
            _ = TesseraFlyoutDismissCoordinator.WindowMustSuppressAutoDismiss(true).Should().BeTrue();
            _ = TesseraFlyoutDismissCoordinator.WindowMustSuppressAutoDismiss(false).Should().BeFalse();
            _ = TesseraFlyoutDismissCoordinator.ShouldArmSessionAutoDismiss(2500, sessionOpen: true).Should().BeTrue();
            _ = TesseraFlyoutDismissCoordinator.ShouldArmSessionAutoDismiss(0, sessionOpen: true).Should().BeFalse();
            _ = TesseraFlyoutDismissCoordinator.ShouldArmSessionAutoDismiss(2500, sessionOpen: false).Should().BeFalse();
        }
    }
}
