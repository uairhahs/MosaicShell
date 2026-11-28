using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform;
using Avalonia.VisualTree;
using MosaicShell.Core.HubShell;

namespace MosaicShell.Host.Views
{
    /// <summary>
    /// The tray icon's quick panel (U24): a switch per installed module, then Open and Quit. Windows and Plasma close a
    /// native tray menu on every click, so turning several modules on or off meant reopening it each time; this panel
    /// stays open until a click elsewhere or Esc. It opens by the pointer (<see cref="TrayPanelPlacement"/>).
    /// </summary>
    public partial class TrayPanel : Window
    {
        public TrayPanel()
        {
            InitializeComponent();
            Deactivated += (_, _) => Hide();
            OpenButton.Content = TrayMenu.OpenLabel;
            QuitButton.Content = TrayMenu.QuitLabel;
        }

        public event EventHandler? OpenRequested;

        public event EventHandler? QuitRequested;

        /// <summary>Opens by the pointer that clicked the tray icon, or at the work area's bottom right without one.</summary>
        public void ShowByPointer()
        {
            (int? px, int? py) = PointerPosition();
            Screen? screen = (px is int x && py is int y ? Screens.ScreenFromPoint(new PixelPoint(x, y)) : null) ?? Screens.Primary;
            if (screen is not null)
            {
                double scale = screen.Scaling > 0.1 ? screen.Scaling : 1;
                Measure(new Size(Width, double.PositiveInfinity));
                int widthPx = (int)Math.Ceiling(Width * scale);
                int heightPx = (int)Math.Ceiling(DesiredSize.Height * scale);
                PixelRect work = screen.WorkingArea;
                (int left, int top) = TrayPanelPlacement.TopLeft(
                    px,
                    py,
                    new PixelBox(work.X, work.Y, work.Right, work.Bottom),
                    widthPx,
                    heightPx);
                Position = new PixelPoint(left, top);
            }

            Show();
            Activate();
            _ = this.GetVisualDescendants().OfType<ToggleSwitch>().FirstOrDefault()?.Focus() ?? OpenButton.Focus();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Hide();
                e.Handled = true;
                return;
            }

            base.OnKeyDown(e);
        }

        private void OnOpenClicked(object? sender, RoutedEventArgs e)
        {
            Hide();
            OpenRequested?.Invoke(this, EventArgs.Empty);
        }

        private void OnQuitClicked(object? sender, RoutedEventArgs e)
        {
            Hide();
            QuitRequested?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>The pointer in physical pixels on Windows; none elsewhere (Wayland does not give it out).</summary>
        private static (int? X, int? Y) PointerPosition()
        {
            return OperatingSystem.IsWindows() && GetCursorPos(out NativePoint p) ? (p.X, p.Y) : (null, null);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct NativePoint
        {
            public int X;
            public int Y;
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetCursorPos(out NativePoint point);
    }
}
