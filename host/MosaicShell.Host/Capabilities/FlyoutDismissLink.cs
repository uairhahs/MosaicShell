using Avalonia;
using Avalonia.Animation;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using MosaicShell.Core.Modules.Tessera;

namespace MosaicShell.Host.Capabilities
{
    /// <summary>
    /// The "Dismiss" link under a Tessera flyout cluster (<see cref="TesseraFlyoutDismissSpec"/>). Its own small window,
    /// because each flyout window is clipped to its card and the link sits outside the card. It acts on release inside
    /// itself, so sliding off cancels a press, and a lost pointer capture cancels too.
    /// </summary>
    internal sealed class FlyoutDismissLink : Window
    {
        private readonly Border _pill;
        private readonly TextBlock _text;
        private bool _pressed;
        private double _scale = -1;

        /// <summary>Released inside the link.</summary>
        public event Action? DismissClicked;

        /// <summary>The pointer arrived on the link or left it.</summary>
        public event Action? HoverChanged;

        public bool IsHovered { get; private set; }

        public FlyoutDismissLink()
        {
            Title = "MosaicShell - Dismiss";
            WindowDecorations = WindowDecorations.None;
            CanResize = false;
            Topmost = true;
            ShowInTaskbar = false;
            ShowActivated = false;
            X11WindowState.ReassertOnShow(this);
            X11WindowState.DeclareOverlay(this);
            SizeToContent = SizeToContent.WidthAndHeight;
            TransparencyLevelHint = [WindowTransparencyLevel.Transparent];
            Background = Brushes.Transparent;

            _text = new TextBlock
            {
                Text = TesseraFlyoutDismissSpec.Label,
                Foreground = Brushes.White,
                FontFamily = new FontFamily("Segoe UI Variable, Segoe UI, Cantarell, Noto Sans, sans-serif"),
                FontWeight = FontWeight.SemiBold,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                IsHitTestVisible = false,
            };
            _pill = new Border
            {
                Child = _text,
                Cursor = new Cursor(StandardCursorType.Hand),
            };
            Content = _pill;
            Opacity = 0;
            Transitions =
            [
                new DoubleTransition
                {
                    Property = OpacityProperty,
                    Duration = TimeSpan.FromMilliseconds(TesseraFlyoutDismissSpec.FadeMs),
                },
            ];
            AutomationProperties.SetName(_pill, TesseraFlyoutDismissSpec.Label);
            SyncFill();

            _pill.PointerEntered += (_, _) => SetHovered(true);
            _pill.PointerExited += (_, _) => SetHovered(false);
            _pill.PointerPressed += OnPressed;
            _pill.PointerReleased += OnReleased;
            _pill.PointerCaptureLost += (_, _) => { _pressed = false; SyncFill(); };
        }

        /// <summary>Sizes the pill and its text for the flyout scale; a no-op when the scale is unchanged.</summary>
        public void ApplyScale(double scale)
        {
            if (Math.Abs(scale - _scale) < 0.001)
            {
                return;
            }

            _scale = scale;
            double height = TesseraFlyoutDismissSpec.HeightDip * scale;
            _pill.Height = height;
            _pill.Padding = new Thickness(TesseraFlyoutDismissSpec.PaddingXDip * scale, 0);
            _pill.CornerRadius = new CornerRadius(height / 2);
            _text.FontSize = TesseraFlyoutDismissSpec.FontSizeDip * scale;
        }

        /// <summary>The pill's size in DIP, measured on the control itself: a window that has never been shown measures 0x0.</summary>
        public Size MeasurePill()
        {
            _pill.Measure(Size.Infinity);
            return _pill.DesiredSize;
        }

        private void SetHovered(bool hovered)
        {
            if (IsHovered == hovered)
            {
                return;
            }

            IsHovered = hovered;
            SyncFill();
            HoverChanged?.Invoke();
        }

        private void OnPressed(object? sender, PointerPressedEventArgs e)
        {
            if (!e.GetCurrentPoint(_pill).Properties.IsLeftButtonPressed)
            {
                return;
            }

            e.Handled = true;
            if (!TesseraFlyoutDismissSpec.ActsOnRelease)
            {
                DismissClicked?.Invoke();
                return;
            }

            _pressed = true;
            e.Pointer.Capture(_pill);
            SyncFill();
        }

        private void OnReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (!_pressed)
            {
                return;
            }

            _pressed = false;
            e.Pointer.Capture(null);
            e.Handled = true;
            SyncFill();
            Point at = e.GetPosition(_pill);
            if (at.X >= 0 && at.Y >= 0 && at.X <= _pill.Bounds.Width && at.Y <= _pill.Bounds.Height)
            {
                DismissClicked?.Invoke();
            }
        }

        private void SyncFill()
        {
            byte alpha = _pressed
                ? TesseraFlyoutDismissSpec.PillPressedAlpha
                : IsHovered ? TesseraFlyoutDismissSpec.PillHoverAlpha : TesseraFlyoutDismissSpec.PillAlpha;
            _pill.Background = new SolidColorBrush(Color.FromArgb(alpha, 0x11, 0x11, 0x1B));
        }
    }
}
