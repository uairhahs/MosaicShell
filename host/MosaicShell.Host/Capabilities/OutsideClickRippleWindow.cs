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
            // As the flyouts: a Transparent window whose composition has not settled shows this, not a theme fill.
            TransparencyBackgroundFallback = Brushes.Transparent;

            _frames = new DispatcherTimer(DispatcherPriority.Render)
            {
                Interval = TimeSpan.FromMilliseconds(TesseraOutsideClickRippleSpec.FrameIntervalMs),
            };
            _frames.Tick += (_, _) => OnFrame();
            Opened += (_, _) => ApplyClickThrough();
            Closed += (_, _) => _frames.Stop();
        }

        /// <summary>Show to first rendered frame of the last play, in ms; null until that frame (for the log).</summary>
        public double? FirstFrameLatencyMs { get; private set; }

        /// <summary>Plays one ripple centered on <paramref name="click"/> (screen pixels), where the route allows it.</summary>
        public static OutsideClickRippleWindow? Play(PixelPoint click, FocusDimRoute route, Screens? screens)
        {
            if (!TesseraOutsideClickRippleSpec.ShouldPlay(route))
            {
                return null;
            }

            OutsideClickRippleWindow ripple = new(click, ScalingAt(click, screens), route);
            ripple.ApplyClickThrough();
            ripple.Show();
            ripple.Start(closeWhenDone: true);
            return ripple;
        }

        /// <summary>
        /// A clear, click-through glint window shown now at <paramref name="near"/>, so a later <see cref="Replay"/> needs
        /// no cold Show (<see cref="TesseraOutsideClickRippleSpec.WarmBeforeTheClick"/>). Null where it is not warmed.
        /// </summary>
        public static OutsideClickRippleWindow? Warm(PixelPoint near, FocusDimRoute route, Screens? screens)
        {
            if (!TesseraOutsideClickRippleSpec.ShouldPlay(route) || !TesseraOutsideClickRippleSpec.WarmBeforeTheClick(route))
            {
                return null;
            }

            OutsideClickRippleWindow ripple = new(near, ScalingAt(near, screens), route);
            ripple.ApplyIdle();
            ripple.ApplyClickThrough();
            ripple.Show();
            return ripple;
        }

        /// <summary>Moves a warmed window to <paramref name="click"/> and plays it; it closes when done.</summary>
        public void Replay(PixelPoint click)
        {
            double scaling = ScalingAt(click, Screens);
            (int x, int y) = TesseraOutsideClickRippleSpec.WindowOrigin(click.X, click.Y, scaling);
            Position = new PixelPoint(x, y);
            Start(closeWhenDone: true);
        }

        private static double ScalingAt(PixelPoint point, Screens? screens)
        {
            double scaling = screens?.ScreenFromPoint(point)?.Scaling ?? 1.0;
            return scaling > 0.1 ? scaling : 1.0;
        }

        private bool _closeWhenDone;
        private double? _firstFrameMs;

        private void Start(bool closeWhenDone)
        {
            _closeWhenDone = closeWhenDone;
            _firstFrameMs = null;
            FirstFrameLatencyMs = null;
            ApplyFrame(0);
            _clock.Restart();
            RequestAnimationFrame(_ =>
            {
                _firstFrameMs = _clock.Elapsed.TotalMilliseconds;
                FirstFrameLatencyMs = _firstFrameMs;
            });
            _frames.Start();
        }

        private void OnFrame()
        {
            double now = _clock.Elapsed.TotalMilliseconds;
            double progress = TesseraOutsideClickRippleSpec.ProgressSinceFirstFrame(_firstFrameMs, now);
            ApplyFrame(progress);
            if (progress >= 1)
            {
                _frames.Stop();
                if (_closeWhenDone)
                {
                    try { Close(); } catch { /* ignore */ }
                }
            }
        }

        /// <summary>Nothing drawn: the warmed window waits clear for the click.</summary>
        private void ApplyIdle()
        {
            _glow.Opacity = 0;
            _ring.Opacity = 0;
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
