using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using FluentAssertions;
using MosaicShell.Host.Capabilities;

namespace MosaicShell.Host.Tests
{
    /// <summary>
    /// Avalonia's X11 backend sends _NET_WM_STATE (ABOVE, SKIP_TASKBAR) only when Topmost or ShowInTaskbar change,
    /// and the window manager clears that state when a window is hidden. A window shown again must therefore have
    /// both properties sent again, or KWin stacks the flyout under the active window and lists it in the taskbar.
    /// </summary>
    public sealed class X11WindowStateTests : HeadlessHostTest
    {
        private static (Window Window, List<string> Sent) Track()
        {
            Window window = new() { Topmost = true, ShowInTaskbar = false, ShowActivated = false };
            X11WindowState.ReassertOnShow(window);
            List<string> sent = [];
            window.PropertyChanged += (_, e) =>
            {
                if (e.Property == Window.TopmostProperty || e.Property == Window.ShowInTaskbarProperty)
                {
                    sent.Add($"{e.Property.Name}={e.NewValue}");
                }
            };
            return (window, sent);
        }

        [AvaloniaFact]
        public void Showing_a_hidden_window_again_resends_topmost_and_skip_taskbar()
        {
            if (OperatingSystem.IsWindows())
            {
                return;
            }

            (Window window, List<string> sent) = Track();
            window.Show();
            window.Hide();
            sent.Clear();

            window.Show();

            _ = sent.Should().Equal("Topmost=False", "Topmost=True", "ShowInTaskbar=True", "ShowInTaskbar=False");
            _ = window.Topmost.Should().BeTrue();
            _ = window.ShowInTaskbar.Should().BeFalse();
            window.Close();
        }

        [AvaloniaFact]
        public void A_window_that_is_not_topmost_and_shows_in_the_taskbar_is_left_alone()
        {
            Window window = new() { Topmost = false, ShowInTaskbar = true };
            X11WindowState.ReassertOnShow(window);
            int sent = 0;
            window.PropertyChanged += (_, e) =>
            {
                if (e.Property == Window.TopmostProperty || e.Property == Window.ShowInTaskbarProperty)
                {
                    sent++;
                }
            };

            window.Show();

            _ = sent.Should().Be(0);
            window.Close();
        }
    }
}
