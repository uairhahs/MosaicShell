using FluentAssertions;
using MosaicShell.Core.HostPlatform;

namespace MosaicShell.Core.Tests
{
    /// <summary>
    /// What the Host's main window does when asked to close. A logout on Plasma reaches the Host as a close of
    /// every window, hidden ones included; cancelling it there makes ksmserver abort the whole logout
    /// ("Logout canceled by 'MosaicShell'"), so session and application shutdown must always be let through.
    /// </summary>
    public class MainWindowClosePolicyTests
    {
        [Fact]
        public void A_user_close_hides_to_tray_when_the_setting_is_on()
        {
            _ = MainWindowClosePolicy.Decide(MainWindowCloseTrigger.User, minimizeToTray: true)
                .Should().Be(MainWindowCloseAction.HideToTray);
        }

        [Fact]
        public void A_user_close_exits_the_app_when_the_setting_is_off()
        {
            _ = MainWindowClosePolicy.Decide(MainWindowCloseTrigger.User, minimizeToTray: false)
                .Should().Be(MainWindowCloseAction.ExitApplication);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void A_session_logout_is_never_vetoed(bool minimizeToTray)
        {
            _ = MainWindowClosePolicy.Decide(MainWindowCloseTrigger.SessionEnding, minimizeToTray)
                .Should().Be(MainWindowCloseAction.AllowClose);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void An_application_shutdown_is_never_vetoed(bool minimizeToTray)
        {
            _ = MainWindowClosePolicy.Decide(MainWindowCloseTrigger.ApplicationShutdown, minimizeToTray)
                .Should().Be(MainWindowCloseAction.AllowClose);
        }
    }
}
