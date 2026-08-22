using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Styling;

namespace MosaicShell.Host.Capabilities;

internal sealed class FocusDimWindow : Window
{
    private int _monitorIndex;

    private const int GwlExStyle = -20;

    public FocusDimWindow(int monitorIndexOneBased)
    {
        _monitorIndex = monitorIndexOneBased;
        Title = "MosaicShell - Focus dim";
        SystemDecorations = SystemDecorations.None;
        CanResize = false;
        Topmost = true;
        ShowInTaskbar = false;
        ShowActivated = false;
        Focusable = false;
        IsHitTestVisible = false;
        TransparencyLevelHint = new[] { WindowTransparencyLevel.Transparent };
        Background = Brushes.Transparent;
        Opacity = 0;
        Content = new Border
        {
            IsHitTestVisible = false,
            Background = new SolidColorBrush(Color.FromArgb(68, 17, 17, 27))
        };
        Opened += (_, _) =>
        {
            PlaceOnMonitor(_monitorIndex);
            ApplyWin32ClickThrough();
        };
    }

    public void PlaceOnMonitor(int monitorIndexOneBased)
    {
        _monitorIndex = monitorIndexOneBased;
        try
        {
            var screens = Screens?.All?.ToList() ?? [];
            var screen = ResolveScreen(screens, _monitorIndex) ?? Screens?.Primary;
            if (screen is null) return;

            var bounds = screen.Bounds;
            var scale = screen.Scaling > 0.1 ? screen.Scaling : 1.0;
            Position = new PixelPoint(bounds.X, bounds.Y);
            Width = Math.Max(1, bounds.Width / scale);
            Height = Math.Max(1, bounds.Height / scale);
            ApplyWin32ClickThrough();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[FocusDim] {ex.Message}");
        }
    }

    public void FadeIn()
    {
        Opacity = 0;
        AnimateOpacity(0, 1, 180);
    }

    public void InstantClose()
    {
        try
        {
            Opacity = 0;
            Close();
        }
        catch { /* ignore */ }
    }

    private void ApplyWin32ClickThrough()
    {
        try
        {
            var handle = TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
            if (handle == IntPtr.Zero) return;

            var current = GetWindowLongPtr(handle, GwlExStyle);
            var next = current | 0x80800A0; // WS_EX_LAYERED | WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE
            if (next != current)
                SetWindowLongPtr(handle, GwlExStyle, next);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[FocusDim click-through] {ex.Message}");
        }
    }

    private void AnimateOpacity(double from, double to, int ms)
    {
        var animation = new Animation
        {
            Duration = TimeSpan.FromMilliseconds(ms),
            FillMode = FillMode.Forward,
            Children =
            {
                new KeyFrame { Cue = new Cue(0.0), Setters = { new Setter(OpacityProperty, from) } },
                new KeyFrame { Cue = new Cue(1.0), Setters = { new Setter(OpacityProperty, to) } }
            }
        };
        _ = animation.RunAsync(this);
    }

    private static Screen? ResolveScreen(IReadOnlyList<Screen> screens, int monitorIndexOneBased)
    {
        if (screens.Count == 0) return null;
        if (monitorIndexOneBased <= 1)
            return screens.FirstOrDefault(s => s.IsPrimary) ?? screens[0];

        var idx = Math.Clamp(monitorIndexOneBased - 1, 0, screens.Count - 1);
        return screens[idx];
    }

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW", SetLastError = true)]
    private static extern nint GetWindowLongPtr(nint hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
    private static extern nint SetWindowLongPtr(nint hWnd, int nIndex, nint dwNewLong);
}
