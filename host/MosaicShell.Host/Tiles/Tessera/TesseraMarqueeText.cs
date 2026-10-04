using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using MosaicShell.Core.Modules.Tessera;
using MosaicShell.Core.Services;
using MosaicShell.Host.Capabilities;

namespace MosaicShell.Host.Tiles.Tessera
{
    /// <summary>
    /// Wraps a title <see cref="TextBlock"/> in a clipped viewport capped at <c>maxWidth</c>, the
    /// same soft upper bound the plain <see cref="TextBlock.MaxWidth"/> it replaces used, not a
    /// hard <c>Width</c>. A caller's maxWidth is often only an approximation of what its layout
    /// grants (CoreUI's media column has about 150 dip after its padding, not 220), so the width
    /// the parent actually offers always wins, capped at maxWidth.
    /// <para>
    /// When the text fits, it shows normally. When it overflows, it rests on an ellipsis-trimmed
    /// view ("Title…"), then periodically scrolls the full text back and forth so the whole title
    /// becomes readable, pausing at each end. Distance, speed and dwell come from
    /// <see cref="TesseraMarqueeSpec"/>.
    /// </para>
    /// <para>
    /// The available width is the constraint the parent passes to the viewport's measure, which
    /// does not depend on the viewport's own content. It is never the viewport's arranged
    /// <c>Bounds</c>: those follow the previous title, and after a skip through an empty title
    /// they made a new title scroll almost entirely out of view (audit F01).
    /// </para>
    /// <para>
    /// Callers keep using the wrapped <see cref="TextBlock"/> for live text updates (registering it
    /// with <see cref="TesseraLiveAmbient"/> as before) and insert the returned
    /// <see cref="Control"/> in its place. A live change to <c>Text</c> restarts the marquee for
    /// the new title once layout has run for it.
    /// </para>
    /// </summary>
    internal static class TesseraMarqueeText
    {
        public static Control Wrap(
            TextBlock textBlock,
            double maxWidth,
            int autoDismissMs = 0,
            bool centerWhenStatic = false)
        {
            textBlock.ClearValue(TextBlock.MaxWidthProperty);
            textBlock.TextTrimming = TextTrimming.None;
            textBlock.TextWrapping = TextWrapping.NoWrap;
            textBlock.HorizontalAlignment = HorizontalAlignment.Left;
            TranslateTransform transform = new();
            textBlock.RenderTransform = transform;

            TextBlock trimmed = new()
            {
                Text = textBlock.Text,
                FontSize = textBlock.FontSize,
                FontWeight = textBlock.FontWeight,
                FontFamily = textBlock.FontFamily,
                Foreground = textBlock.Foreground,
                MaxWidth = maxWidth,
                TextTrimming = TextTrimming.CharacterEllipsis,
                TextWrapping = TextWrapping.NoWrap,
                HorizontalAlignment = centerWhenStatic ? HorizontalAlignment.Center : HorizontalAlignment.Left,
                TextAlignment = centerWhenStatic ? TextAlignment.Center : TextAlignment.Left,
            };

            MarqueeViewport viewport = new()
            {
                // MaxWidth, not Width: it caps the viewport without forcing a size the surrounding
                // layout may not have (see the type doc).
                MaxWidth = maxWidth,
                HorizontalAlignment = centerWhenStatic ? HorizontalAlignment.Center : HorizontalAlignment.Left,
                ClipToBounds = true,
                Children = { textBlock, trimmed }
            };

            DispatcherTimer? timer = null;
            double offset = 0;
            int direction = -1;
            int dwellTicksRemaining = 0;
            TesseraMarqueeSpec.Timing timing = default;
            string? lastRestartedText = null;
            double lastAvailable = double.NaN;
            bool attached = false;
            bool restartPending = false;
            int restartCount = 0;

            void ShowTrimmed(bool show)
            {
                trimmed.IsVisible = show;
                textBlock.IsVisible = !show;
            }

            void StopTimer()
            {
                timer?.Stop();
                timer = null;
            }

            void Restart(string reason)
            {
                StopTimer();
                lastRestartedText = textBlock.Text;
                trimmed.Text = textBlock.Text;
                transform.X = 0;
                offset = 0;
                direction = -1;

                double available = TesseraMarqueeSpec.AvailableWidth(maxWidth, viewport.GrantedWidth);
                lastAvailable = available;
                textBlock.Width = double.NaN;
                textBlock.Measure(Size.Infinity);
                // Measuring only computes DesiredSize; the next layout pass would still arrange
                // textBlock at whatever narrower width the Grid offers and clip it there. An explicit
                // Width keeps its arranged size at the full text, so the viewport's clip is what
                // crops it while the RenderTransform slides it into view.
                double textWidth = textBlock.DesiredSize.Width;
                textBlock.Width = textWidth;
                double distance = TesseraMarqueeSpec.ScrollDistance(textWidth, available);

                restartCount++;
                if (TesseraFlyoutDiagnostics.IsEnabled(DiagnosticLogLevel.Debug))
                {
                    // Length only: the title itself is already in the media trace.
                    TesseraFlyoutDiagnostics.Log(
                        DiagnosticLogLevel.Debug,
                        $"marquee restart n={restartCount} reason={reason} len={textBlock.Text?.Length ?? 0} " +
                        $"granted={viewport.GrantedWidth:0.#} max={maxWidth:0.#} available={available:0.#} " +
                        $"text={textWidth:0.#} distance={distance:0.#}");
                }

                if (distance <= 0)
                {
                    ShowTrimmed(false);
                    return;
                }

                timing = TesseraMarqueeSpec.ResolveTiming(autoDismissMs, textBlock.FontSize, distance);
                dwellTicksRemaining = timing.StartDwellTicks;
                ShowTrimmed(true);
                DispatcherTimer t = new() { Interval = TimeSpan.FromMilliseconds(TesseraMarqueeSpec.TickMs) };
                t.Tick += (_, _) =>
                {
                    if (dwellTicksRemaining > 0)
                    {
                        dwellTicksRemaining--;
                        if (dwellTicksRemaining == 0 && direction == -1)
                        {
                            ShowTrimmed(false);
                        }

                        return;
                    }

                    offset += direction * timing.PixelsPerTick;
                    if (offset <= -distance)
                    {
                        offset = -distance;
                        direction = 1;
                        dwellTicksRemaining = timing.EndDwellTicks;
                    }
                    else if (offset >= 0)
                    {
                        offset = 0;
                        direction = -1;
                        dwellTicksRemaining = timing.StartDwellTicks;
                        ShowTrimmed(true);
                    }

                    transform.X = offset;
                };
                timer = t;
                t.Start();
            }

            // Restart runs at Loaded priority, after the pending layout pass for the new text, and
            // several triggers in one burst (an empty title, then the real one) coalesce into one
            // restart against whatever the text is by then. Restart writes textBlock.Width, which
            // invalidates layout, so it must never be driven by a layout signal itself
            // (LayoutUpdated once looped that way). The grant trigger below cannot loop: the
            // constraint a parent offers does not depend on the viewport's content.
            void ScheduleRestart(string reason)
            {
                if (restartPending)
                {
                    return;
                }

                restartPending = true;
                Dispatcher.UIThread.Post(
                    () =>
                    {
                        restartPending = false;
                        if (attached)
                        {
                            Restart(reason);
                        }
                    },
                    DispatcherPriority.Loaded);
            }

            textBlock.PropertyChanged += (_, e) =>
            {
                // Live updates set Text on every poll tick whether or not the title changed; only a
                // real change restarts, so the tween engine is not competing with needless work.
                if (e.Property == TextBlock.TextProperty && !Equals(textBlock.Text, lastRestartedText))
                {
                    ScheduleRestart("text");
                }
            };

            viewport.GrantChanged = () =>
            {
                if (attached && lastRestartedText is not null
                    && TesseraMarqueeSpec.NeedsRederive(lastAvailable, TesseraMarqueeSpec.AvailableWidth(maxWidth, viewport.GrantedWidth)))
                {
                    ScheduleRestart("width");
                }
            };

            viewport.AttachedToVisualTree += (_, _) =>
            {
                attached = true;
                ScheduleRestart("attach");
            };
            viewport.DetachedFromVisualTree += (_, _) =>
            {
                attached = false;
                StopTimer();
            };

            return viewport;
        }

        /// <summary>
        /// A <see cref="Grid"/> that records the width its parent offers in measure. That constraint
        /// already has <c>MaxWidth</c> applied and does not depend on the children, unlike the
        /// arranged bounds, so it is the width a new title can use.
        /// </summary>
        private sealed class MarqueeViewport : Grid
        {
            public double GrantedWidth { get; private set; } = double.NaN;

            public Action? GrantChanged { get; set; }

            protected override Type StyleKeyOverride => typeof(Grid);

            protected override Size MeasureOverride(Size availableSize)
            {
                double previous = GrantedWidth;
                GrantedWidth = availableSize.Width;
                if (!previous.Equals(GrantedWidth))
                {
                    GrantChanged?.Invoke();
                }

                return base.MeasureOverride(availableSize);
            }
        }
    }
}
