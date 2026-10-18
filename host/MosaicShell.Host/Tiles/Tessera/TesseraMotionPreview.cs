using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using MosaicShell.Core.Modules.Tessera;

namespace MosaicShell.Host.Tiles.Tessera
{
    /// <summary>
    /// A small looping stage that plays a motion look the way the flyout plays it: the same stepped curve, the same
    /// duration, distance and direction, the slide with the fade, and for the Fancy look the media strip growing in
    /// afterwards. It is what makes the looks comparable at a glance in the Hub, so it samples the same policy functions
    /// the Host's motion controller does and draws nothing the engine would not.
    /// </summary>
    public sealed class TesseraMotionPreview : UserControl
    {
        public static readonly StyledProperty<int> AniProperty =
            AvaloniaProperty.Register<TesseraMotionPreview, int>(nameof(Ani), TesseraFlyoutAnimationPolicy.DefaultAni);

        public static readonly StyledProperty<int> AniStepsProperty =
            AvaloniaProperty.Register<TesseraMotionPreview, int>(nameof(AniSteps), TesseraFlyoutAnimationPolicy.DefaultAniSteps);

        public static readonly StyledProperty<string?> AniEaseProperty =
            AvaloniaProperty.Register<TesseraMotionPreview, string?>(nameof(AniEase), TesseraFlyoutAnimationPolicy.DefaultEase);

        public static readonly StyledProperty<string?> AniDirProperty =
            AvaloniaProperty.Register<TesseraMotionPreview, string?>(nameof(AniDir), "Left");

        public static readonly StyledProperty<int> DisplacementPxProperty =
            AvaloniaProperty.Register<TesseraMotionPreview, int>(
                nameof(DisplacementPx), TesseraFlyoutAnimationPolicy.DefaultDisplacementPx);

        private const double CardWidth = 104;
        private const double CardHeight = 26;
        private const double MediaHeight = 22;
        private const double HoldMs = 900;
        private const double RestMs = 700;

        /// <summary>Preview pixels per screen pixel of travel: 200 px of real travel is 80 px here.</summary>
        private const double TravelScale = 0.4;

        private readonly Border _stage;
        private readonly StackPanel _cluster;
        private readonly Border _media;
        private readonly TranslateTransform _move = new();
        private readonly DispatcherTimer _timer;
        private readonly Stopwatch _clock = new();

        static TesseraMotionPreview()
        {
            _ = AniProperty.Changed.AddClassHandler<TesseraMotionPreview>((p, _) => p.Restart());
            _ = AniStepsProperty.Changed.AddClassHandler<TesseraMotionPreview>((p, _) => p.Restart());
            _ = AniEaseProperty.Changed.AddClassHandler<TesseraMotionPreview>((p, _) => p.Restart());
            _ = AniDirProperty.Changed.AddClassHandler<TesseraMotionPreview>((p, _) => p.Restart());
            _ = DisplacementPxProperty.Changed.AddClassHandler<TesseraMotionPreview>((p, _) => p.Restart());
        }

        public TesseraMotionPreview()
        {
            Border volume = new()
            {
                Width = CardWidth,
                Height = CardHeight,
                CornerRadius = new CornerRadius(8),
                Background = new SolidColorBrush(Color.Parse("#585b70")),
                Child = new Grid
                {
                    ColumnDefinitions = new ColumnDefinitions("Auto,*"),
                    Margin = new Thickness(9, 0),
                    Children =
                    {
                        new Border
                        {
                            Width = 10,
                            Height = 10,
                            CornerRadius = new CornerRadius(5),
                            Background = new SolidColorBrush(Color.Parse("#cdd6f4")),
                            VerticalAlignment = VerticalAlignment.Center,
                        },
                        new Border
                        {
                            [Grid.ColumnProperty] = 1,
                            Height = 4,
                            Margin = new Thickness(8, 0, 0, 0),
                            CornerRadius = new CornerRadius(2),
                            Background = new SolidColorBrush(Color.Parse("#89b4fa")),
                            VerticalAlignment = VerticalAlignment.Center,
                            HorizontalAlignment = HorizontalAlignment.Stretch,
                            Opacity = 0.9,
                        },
                    },
                },
            };

            _media = new Border
            {
                Width = CardWidth,
                Height = 0,
                CornerRadius = new CornerRadius(8),
                Background = new SolidColorBrush(Color.Parse("#45475a")),
                ClipToBounds = true,
                Margin = new Thickness(0, 4, 0, 0),
                Opacity = 0,
                Child = new Grid
                {
                    ColumnDefinitions = new ColumnDefinitions("Auto,*"),
                    Margin = new Thickness(7, 0),
                    Children =
                    {
                        new Border
                        {
                            Width = 14,
                            Height = 14,
                            CornerRadius = new CornerRadius(3),
                            Background = new SolidColorBrush(Color.Parse("#f9e2af")),
                            VerticalAlignment = VerticalAlignment.Center,
                        },
                        new StackPanel
                        {
                            [Grid.ColumnProperty] = 1,
                            Margin = new Thickness(7, 0, 0, 0),
                            VerticalAlignment = VerticalAlignment.Center,
                            Spacing = 3,
                            Children =
                            {
                                new Border { Height = 3, Width = 50, CornerRadius = new CornerRadius(1.5), Background = new SolidColorBrush(Color.Parse("#cdd6f4")), HorizontalAlignment = HorizontalAlignment.Left },
                                new Border { Height = 3, Width = 34, CornerRadius = new CornerRadius(1.5), Background = new SolidColorBrush(Color.Parse("#7f849c")), HorizontalAlignment = HorizontalAlignment.Left },
                            },
                        },
                    },
                },
            };

            _cluster = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                RenderTransform = _move,
                Children = { volume, _media },
            };

            _stage = new Border
            {
                Background = new SolidColorBrush(Color.Parse("#181825")),
                BorderBrush = new SolidColorBrush(Color.Parse("#313244")),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                ClipToBounds = true,
                MinHeight = 78,
                Child = _cluster,
            };

            Content = _stage;
            _timer = new DispatcherTimer(DispatcherPriority.Render) { Interval = TimeSpan.FromMilliseconds(16) };
            _timer.Tick += (_, _) => Tick();
            AttachedToVisualTree += (_, _) => Restart();
            DetachedFromVisualTree += (_, _) => _timer.Stop();
        }

        public int Ani
        {
            get => GetValue(AniProperty);
            set => SetValue(AniProperty, value);
        }

        public int AniSteps
        {
            get => GetValue(AniStepsProperty);
            set => SetValue(AniStepsProperty, value);
        }

        public string? AniEase
        {
            get => GetValue(AniEaseProperty);
            set => SetValue(AniEaseProperty, value);
        }

        public string? AniDir
        {
            get => GetValue(AniDirProperty);
            set => SetValue(AniDirProperty, value);
        }

        public int DisplacementPx
        {
            get => GetValue(DisplacementPxProperty);
            set => SetValue(DisplacementPxProperty, value);
        }

        private void Restart()
        {
            if (VisualRoot is null)
            {
                return;
            }

            _clock.Restart();
            if (!_timer.IsEnabled)
            {
                _timer.Start();
            }

            Tick();
        }

        private void Tick()
        {
            if (!IsEffectivelyVisible)
            {
                return;
            }

            Frame frame = Sample(_clock.Elapsed.TotalMilliseconds);
            _move.X = frame.Dx;
            _move.Y = frame.Dy;
            _cluster.Opacity = frame.Opacity;
            _media.IsVisible = Ani >= 2;
            _media.Height = MediaHeight * frame.Reveal;
            _media.Margin = new Thickness(0, 4 * frame.Reveal, 0, 0);
            _media.Opacity = frame.Reveal;
        }

        private readonly record struct Frame(double Dx, double Dy, double Opacity, double Reveal);

        /// <summary>The look at <paramref name="elapsedMs"/> into a loop: enter, hold, leave, rest.</summary>
        private Frame Sample(double elapsedMs)
        {
            int ani = Math.Clamp(Ani, 0, 2);
            int steps = TesseraFlyoutAnimationPolicy.NormalizeAniSteps(AniSteps);
            string ease = TesseraFlyoutAnimationPolicy.NormalizeEase(AniEase);
            double durationMs = ani > 0 ? TesseraFlyoutAnimationPolicy.ResolvePhaseDurationMs(steps) : TesseraFlyoutAnimationPolicy.FadeDurationMs;
            int moveSteps = ani > 0 ? steps : TesseraFlyoutAnimationPolicy.ResolveFadeSampleCount();
            double lead = ani >= 2 ? TesseraFlyoutAnimationPolicy.ResolveShowPhase2LeadDelayMs(ease, steps) : 0;
            double enterTotal = Math.Max(durationMs, lead + durationMs);
            double leaveTotal = ani >= 2 ? durationMs + TesseraFlyoutAnimationPolicy.FancyPauseMs + durationMs : durationMs;
            double cycle = enterTotal + HoldMs + leaveTotal + RestMs;
            double t = elapsedMs % cycle;

            double travel = ani > 0
                ? TesseraFlyoutAnimationPolicy.NormalizeDisplacementPx(DisplacementPx) * TravelScale
                : 0;
            (double sx, double sy) = (AniDir ?? "Left").ToLowerInvariant() switch
            {
                "right" => (1d, 0d),
                "top" => (0d, -1d),
                "bottom" => (0d, 1d),
                _ => (-1d, 0d),
            };

            double slide;
            double reveal = 0;
            if (t < enterTotal)
            {
                slide = TesseraFlyoutAnimationPolicy.SampleSteppedAt(0, 1, t / durationMs, moveSteps, ease, entrance: true);
                if (ani >= 2)
                {
                    reveal = Math.Clamp(
                        TesseraFlyoutAnimationPolicy.ResolvePhase2RevealProgressAt(true, (t - lead) / durationMs, steps, ease), 0, 1);
                }
            }
            else if (t < enterTotal + HoldMs)
            {
                slide = 1;
                reveal = ani >= 2 ? 1 : 0;
            }
            else if (t < enterTotal + HoldMs + leaveTotal)
            {
                double u = t - enterTotal - HoldMs;
                if (ani >= 2 && u < durationMs)
                {
                    slide = 1;
                    reveal = Math.Clamp(
                        TesseraFlyoutAnimationPolicy.ResolvePhase2RevealProgressAt(false, u / durationMs, steps, ease), 0, 1);
                }
                else
                {
                    double v = ani >= 2 ? u - durationMs - TesseraFlyoutAnimationPolicy.FancyPauseMs : u;
                    slide = v < 0 ? 1 : TesseraFlyoutAnimationPolicy.SampleSteppedAt(1, 0, v / durationMs, moveSteps, ease, entrance: false);
                }
            }
            else
            {
                slide = 0;
            }

            double away = travel * (1 - slide);
            return new Frame(sx * away, sy * away, Math.Clamp(slide, 0, 1), reveal);
        }
    }
}
