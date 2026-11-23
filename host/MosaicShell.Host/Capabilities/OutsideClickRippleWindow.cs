using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.Threading;
using MosaicShell.Core.Modules.Tessera;

namespace MosaicShell.Host.Capabilities
{
    /// <summary>
    /// A small click-through window that plays <see cref="TesseraOutsideClickRippleSpec"/> at an outside click (a soft
    /// holographic ring catching a passing sheen, over a pale glow) and then closes itself. It is a window of its own
    /// because the layer behind the flyout closes as the dismiss begins.
    /// </summary>
    internal sealed class OutsideClickRippleWindow : Window
    {
        private readonly FocusDimRoute _route;
        private readonly double _side;
        private readonly Ellipse _ring;
        private readonly GradientStop[] _ringStops;
        private readonly Ellipse _glow;
        private readonly Stopwatch _clock = new();
        private readonly DispatcherTimer _frames;

        private OutsideClickRippleWindow(PixelPoint click, double scaling, FocusDimRoute route)
        {
            _route = route;
            Title = "MosaicShell - Click ripple";
            WindowDecorations = WindowDecorations.None;
            CanResize = false;
            Topmost = true;
            ShowInTaskbar = false;
            X11WindowState.ReassertOnShow(this);
            ShowActivated = false;
            Focusable = false;
            IsHitTestVisible = false;
            TransparencyLevelHint = [WindowTransparencyLevel.Transparent];
            Background = Brushes.Transparent;

            (int x, int y) = TesseraOutsideClickRippleSpec.WindowOrigin(click.X, click.Y, scaling);
            Position = new PixelPoint(x, y);
            _side = TesseraOutsideClickRippleSpec.WindowSizePx(scaling) / scaling;
            Width = _side;
            Height = _side;

            (byte gr, byte gg, byte gb) = TesseraOutsideClickRippleSpec.GlowColor;
            _glow = new Ellipse
            {
                Fill = new RadialGradientBrush
                {
                    GradientStops =
                    [
                        new GradientStop(Color.FromArgb(255, gr, gg, gb), 0),
                        new GradientStop(Color.FromArgb(0, gr, gg, gb), 1),
                    ],
                },
                IsHitTestVisible = false,
            };

            // One stop per sample plus a closing stop at offset 1, so the gradient meets itself with no seam.
            int stops = TesseraOutsideClickRippleSpec.GradientStops;
            _ringStops = new GradientStop[stops + 1];
            ConicGradientBrush sheen = new() { Center = RelativePoint.Center };
            for (int i = 0; i <= stops; i++)
            {
                _ringStops[i] = new GradientStop(Colors.Transparent, (double)i / stops);
                sheen.GradientStops.Add(_ringStops[i]);
            }

            _ring = new Ellipse
            {
                Stroke = sheen,
                Effect = new BlurEffect { Radius = TesseraOutsideClickRippleSpec.BlurRadiusDip },
                IsHitTestVisible = false,
            };

            Canvas stage = new() { Width = _side, Height = _side, IsHitTestVisible = false };
            stage.Children.Add(_glow);
            stage.Children.Add(_ring);
            Content = stage;
            ApplyFrame(0);

            _frames = new DispatcherTimer(DispatcherPriority.Render)
            {
                Interval = TimeSpan.FromMilliseconds(TesseraOutsideClickRippleSpec.FrameIntervalMs),
            };
            _frames.Tick += (_, _) => OnFrame();
            Opened += (_, _) => ApplyClickThrough();
            Closed += (_, _) => _frames.Stop();
        }

        /// <summary>Plays one ripple centered on <paramref name="click"/> (screen pixels), where the route allows it.</summary>
        public static OutsideClickRippleWindow? Play(PixelPoint click, FocusDimRoute route, Screens? screens)
        {
            if (!TesseraOutsideClickRippleSpec.ShouldPlay(route))
            {
                return null;
            }

            double scaling = screens?.ScreenFromPoint(click)?.Scaling ?? 1.0;
            OutsideClickRippleWindow ripple = new(click, scaling > 0.1 ? scaling : 1.0, route);
            ripple.ApplyClickThrough();
            ripple.Show();
            ripple._clock.Start();
            ripple._frames.Start();
            return ripple;
        }

        private void OnFrame()
        {
            double elapsed = _clock.Elapsed.TotalMilliseconds;
            ApplyFrame(TesseraOutsideClickRippleSpec.ProgressAt(elapsed));
            if (elapsed >= TesseraOutsideClickRippleSpec.DurationMs)
            {
                _frames.Stop();
                try { Close(); } catch { /* ignore */ }
            }
        }

        private void ApplyFrame(double progress)
        {
            double center = _side / 2;
            double diameter = TesseraOutsideClickRippleSpec.DiameterAt(progress);
            double radius = diameter / 2;

            _glow.Width = diameter;
            _glow.Height = diameter;
            Canvas.SetLeft(_glow, center - radius);
            Canvas.SetTop(_glow, center - radius);
            _glow.Opacity = TesseraOutsideClickRippleSpec.GlowOpacityAt(progress);

            _ring.Width = diameter;
            _ring.Height = diameter;
            Canvas.SetLeft(_ring, center - radius);
            Canvas.SetTop(_ring, center - radius);
            _ring.StrokeThickness = TesseraOutsideClickRippleSpec.StrokeAt(progress);
            _ring.Opacity = TesseraOutsideClickRippleSpec.OpacityAt(progress);
            for (int i = 0; i < _ringStops.Length; i++)
            {
                // A conic gradient starts at the top and runs clockwise; the spec measures clockwise from the right.
                double angle = 270 + (360 * _ringStops[i].Offset);
                (byte r, byte g, byte b) = TesseraOutsideClickRippleSpec.RingColorAt(angle, progress);
                _ringStops[i].Color = Color.FromRgb(r, g, b);
            }
        }

        private void ApplyClickThrough()
        {
            if (_route == FocusDimRoute.X11ArgbInputShape)
            {
                _ = X11FocusDim.Apply(this, clickThrough: true);
                return;
            }

            // The flyouts' recipe for a Transparent HWND that stays visible, plus click-through.
            Win32WindowChrome.ApplyClickThroughNow(this);
            _ = Win32WindowChrome.ForceOpaqueLayer(this);
        }
    }
}
