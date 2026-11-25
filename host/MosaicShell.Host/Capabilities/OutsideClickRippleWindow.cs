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
        private readonly Rectangle _aurora;
        private readonly Panel _auroraLayer;
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
            X11WindowState.DeclareOverlay(this);
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

            // The aurora: the ring's own brush over the whole stage (so its colours line up with the ring's), clipped to
            // a wavy band outside the ring each frame and blurred as a layer, so the clip's edge dissolves.
            _aurora = new Rectangle { Width = _side, Height = _side, Fill = sheen, IsHitTestVisible = false };
            _auroraLayer = new Panel
            {
                Width = _side,
                Height = _side,
                Effect = new BlurEffect { Radius = TesseraOutsideClickRippleSpec.AuroraBlurDip },
                IsHitTestVisible = false,
            };
            _auroraLayer.Children.Add(_aurora);

            Canvas stage = new() { Width = _side, Height = _side, IsHitTestVisible = false };
            stage.Children.Add(_glow);
            stage.Children.Add(_auroraLayer);
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

        /// <summary>
        /// Plays one ripple centered on <paramref name="click"/> (screen pixels), where the route allows it. With
        /// <paramref name="endMs"/> (the flyout's exit length, from now) it finishes when the flyout does; without, it runs
        /// its own <see cref="TesseraOutsideClickRippleSpec.DurationMs"/>.
        /// </summary>
        public static OutsideClickRippleWindow? Play(PixelPoint click, FocusDimRoute route, Screens? screens, double? endMs = null)
        {
            if (!TesseraOutsideClickRippleSpec.ShouldPlay(route))
            {
                return null;
            }

            OutsideClickRippleWindow ripple = new(click, ScalingAt(click, screens), route);
            ripple.ApplyClickThrough();
            ripple._endMs = endMs;
            ripple.Show();
            ripple.Start(closeWhenDone: true);
            return ripple;
        }

        /// <summary>
        /// For reference images (<c>--capture-glint</c>): the same window, shown at <paramref name="click"/> with no clock,
        /// so each frame is drawn on demand with <see cref="DrawAt"/>.
        /// </summary>
        internal static OutsideClickRippleWindow ShowFrozen(PixelPoint click, FocusDimRoute route, double scaling)
        {
            OutsideClickRippleWindow ripple = new(click, scaling, route);
            ripple.ApplyClickThrough();
            ripple.Show();
            return ripple;
        }

        /// <summary>Draws the glint as it is at <paramref name="progress"/> (0 to 1), as the clock would.</summary>
        internal void DrawAt(double progress)
        {
            ApplyFrame(progress);
        }

        /// <summary>What the window draws, for rendering it off-screen.</summary>
        internal Control Stage => (Control)Content!;

        private static double ScalingAt(PixelPoint point, Screens? screens)
        {
            double scaling = screens?.ScreenFromPoint(point)?.Scaling ?? 1.0;
            return scaling > 0.1 ? scaling : 1.0;
        }

        private bool _closeWhenDone;
        private double? _firstFrameMs;
        private double? _endMs;

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
            double progress = _endMs is double end
                ? TesseraOutsideClickRippleSpec.ProgressToward(_firstFrameMs, now, end)
                : TesseraOutsideClickRippleSpec.ProgressSinceFirstFrame(_firstFrameMs, now);
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
            _aurora.Clip = AuroraBand(center, radius, progress);
            _auroraLayer.Opacity = _ring.Opacity * TesseraOutsideClickRippleSpec.AuroraShare;
            for (int i = 0; i < _ringStops.Length; i++)
            {
                // A conic gradient starts at the top and runs clockwise; the spec measures clockwise from the right.
                double angle = 270 + (360 * _ringStops[i].Offset);
                (byte r, byte g, byte b) = TesseraOutsideClickRippleSpec.RingColorAt(angle, progress);
                _ringStops[i].Color = Color.FromRgb(r, g, b);
            }
        }

        /// <summary>The band from the ring out to the aurora's wavy edge (even-odd: outer outline minus the ring's disc).</summary>
        private static StreamGeometry AuroraBand(double center, double radius, double progress)
        {
            int n = TesseraOutsideClickRippleSpec.AuroraSamples;
            StreamGeometry band = new();
            using StreamGeometryContext ctx = band.Open();
            ctx.SetFillRule(FillRule.EvenOdd);
            for (int edge = 0; edge < 2; edge++)
            {
                for (int i = 0; i < n; i++)
                {
                    double angle = 360.0 * i / n;
                    double r = edge == 0 ? radius + TesseraOutsideClickRippleSpec.AuroraReachAt(angle, progress) : radius;
                    double t = angle * Math.PI / 180;
                    Point point = new(center + (r * Math.Cos(t)), center + (r * Math.Sin(t)));
                    if (i == 0)
                    {
                        ctx.BeginFigure(point, isFilled: true);
                    }
                    else
                    {
                        ctx.LineTo(point);
                    }
                }

                ctx.EndFigure(isClosed: true);
            }

            return band;
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
