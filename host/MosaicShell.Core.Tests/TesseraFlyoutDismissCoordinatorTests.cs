using FluentAssertions;
using MosaicShell.Core.Modules.Tessera;

namespace MosaicShell.Core.Tests;

public class TesseraFlyoutDismissCoordinatorTests
{
    [Fact]
    public void Session_owns_auto_dismiss_and_ipc_must_echo_snapshot()
    {
        TesseraFlyoutDismissCoordinator.SessionOwnsAutoDismissClock.Should().BeTrue();
        TesseraFlyoutDismissCoordinator.IpcMustEchoSessionSnapshot.Should().BeTrue();
        TesseraFlyoutDismissCoordinator.WindowMustSuppressAutoDismiss(true).Should().BeTrue();
        TesseraFlyoutDismissCoordinator.WindowMustSuppressAutoDismiss(false).Should().BeFalse();
        TesseraFlyoutDismissCoordinator.ShouldArmSessionAutoDismiss(2500, sessionOpen: true).Should().BeTrue();
        TesseraFlyoutDismissCoordinator.ShouldArmSessionAutoDismiss(0, sessionOpen: true).Should().BeFalse();
        TesseraFlyoutDismissCoordinator.ShouldArmSessionAutoDismiss(2500, sessionOpen: false).Should().BeFalse();
    }
}
