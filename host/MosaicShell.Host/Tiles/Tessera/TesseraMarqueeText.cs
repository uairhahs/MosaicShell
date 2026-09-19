using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;

namespace MosaicShell.Host.Tiles.Tessera
{
    /// <summary>
    /// Wraps a title <see cref="TextBlock"/> in a clipped viewport capped at <c>maxWidth</c>, the
    /// same soft upper bound the plain <see cref="TextBlock.MaxWidth"/> it replaces used - not a
    /// hard <c>Width</c>. A caller's maxWidth is frequently just an approximation of what its
    /// surrounding layout actually grants (CoreUI's media column, for one, only has ~150dip after
    /// its own padding chain, not the 220 every other style happens to use); the real available
    /// space always wins, read back from the viewport's own arranged <see cref="Visual.Bounds"/>
    /// once layout has run, exactly like the plain TextBlock it replaces would have shrunk to fit.
    /// <para>
    /// When the text fits, it just shows normally. When it overflows, it rests on a static
    /// ellipsis-trimmed view ("Title…"), then periodically scrolls the full, untrimmed text back
    /// and forth so the whole title becomes readable over time, pausing at each end before
    /// reversing.
    /// </para>
    /// <para>
    /// Both the scroll speed and the dwell time before it starts scale with <c>autoDismissMs</c>,
    /// bounded so neither extreme becomes jarring. Speed is expressed in characters-per-second
    /// (converted to pixels using this title's own font size) and interpolates between an anchor
    /// matching Android's TextView marquee default (30dp/second, which backs out to roughly 4
    /// CPS for typical UI text - the pace for a long-lived or persistent flyout with no time
    /// pressure) and a brisker end used only for the shortest windows the settings allow, capped
    /// inside the "comfortable" 12-20 CPS band established by subtitle/caption accessibility
    /// standards rather than drifting toward "fast." A short-lived flyout also spends less of its
    /// visible time sitting still on the trimmed "Title…" view before the reveal starts; a
    /// long-lived one can afford to let the reader register the start before it moves. All four
    /// bounds (min/max speed, min/max dwell) are floored and capped so neither extreme disappears
    /// or drags on absurdly.
    /// </para>
    /// <para>
    /// A third factor, independent of the dismiss window, is how far this particular title
    /// actually overflows: the urgency-derived pace is a per-character rate, so a title with much
    /// more distance to cover takes proportionally longer to fully reveal even though each
    /// character still passes at the same speed - a long enough title could otherwise take an
    /// unreasonably long one-way reveal regardless of how patient the dismiss window is. If the
    /// urgency pace would blow past <see cref="MaxScrollDurationMs"/> for this title's specific
    /// overflow distance, speed increases just enough to fit that budget, capped at
    /// <see cref="LengthDrivenMaxCps"/> so even the longest titles stay inside comfortable
    /// reading speed rather than crossing into "fast"/"unreadable" - a title long enough to need
    /// more than that simply takes longer than the budget rather than becoming unreadable.
    /// </para>
    /// <para>
    /// Callers keep using the wrapped <see cref="TextBlock"/> itself for live text updates
    /// (registering it with <see cref="TesseraLiveAmbient"/> exactly as before) and insert the
    /// <see cref="Control"/> this returns into the visual tree in its place. A live update to
    /// <c>Text</c> - however it is set - restarts the marquee against the new title.
    /// </para>
    /// </summary>
    internal static class TesseraMarqueeText
    {
        private const int TickMs = 30;

        // Speed is expressed in characters-per-second, not a flat pixel rate, and scaled against
        // how long the flyout actually stays open - the window the user asked for:
        //
        // - MinCps anchors to Android's TextView marquee default (30dp/second - the OS-level
        //   convention for exactly this: a scrolling label in constrained UI chrome), which backs
        //   out to roughly 4 characters/second for typical UI text sizes. That's the pace for a
        //   long-lived or persistent flyout with no time pressure.
        // - MaxCps is the fast end, used only for the shortest windows the settings allow (0.5s).
        //   It stays inside the "comfortable" 12-20 CPS band established by subtitle/caption
        //   accessibility standards (BBC/Netflix/FCC-aligned: comfortable 12-20 CPS, fast 20-25,
        //   unreadable 25+) rather than drifting toward "fast" just because the window is short -
        //   a short window should trim the dwell first, not blow past legible reading speed.
        private const double MinCps = 4;
        private const double MaxCps = 14;

        /// <summary>Target ceiling for how long a title's one-way reveal (start to fully scrolled)
        /// should take. Only kicks in once the urgency-derived pace would exceed it - most titles
        /// never reach this; it exists for the ones long enough to need it.</summary>
        private const double MaxScrollDurationMs = 6000;

        /// <summary>Absolute fastest the length-driven speed-up is allowed to go, regardless of
        /// how long the title is. Matches the top of the "comfortable" 12-20 CPS subtitle band
        /// (Netflix's own adult-content ceiling) rather than drifting into "fast"/"unreadable" -
        /// an extremely long title takes longer than MaxScrollDurationMs instead.</summary>
        private const double LengthDrivenMaxCps = 20;

        // Average glyph width as a fraction of font size (em) - a standard typographic estimate
        // for proportional Latin text, used to turn a target CPS into an actual pixel speed for
        // whatever font size this particular title uses.
        private const double AvgCharWidthEm = 0.55;

        private const double MinStartDwellMs = 300;
        private const double MaxStartDwellMs = 1200;
        private const double MinEndDwellMs = 250;
        private const double MaxEndDwellMs = 900;

        /// <summary>Settings-enforced floor for AutoDismissMs (see TesseraAutoDismissSeconds) -
        /// the shortest real window speed and dwell ever need to compress for.</summary>
        private const double ShortWindowMs = 500;

        /// <summary>Window length at/above which speed and dwell are already at their relaxed end;
        /// also the assumed budget when the caller has no real auto-dismiss window (0/negative -
        /// persistent flyout).</summary>
        private const double RelaxedWindowMs = 4000;

        public static Control Wrap(
            TextBlock textBlock,
            double maxWidth,
            int autoDismissMs = 0,
            bool centerWhenStatic = false)
        {
            double budgetMs = autoDismissMs > 0 ? autoDismissMs : RelaxedWindowMs;
            double urgency = Math.Clamp((RelaxedWindowMs - budgetMs) / (RelaxedWindowMs - ShortWindowMs), 0, 1);
            int startDwellTicks = (int)Math.Round(Math.Clamp(budgetMs * 0.3, MinStartDwellMs, MaxStartDwellMs) / TickMs);
            int endDwellTicks = (int)Math.Round(Math.Clamp(budgetMs * 0.2, MinEndDwellMs, MaxEndDwellMs) / TickMs);
            double targetCps = MinCps + ((MaxCps - MinCps) * urgency);
            double pxPerCharAtFontSize = textBlock.FontSize * AvgCharWidthEm;

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

            Grid viewport = new()
            {
                MaxWidth = maxWidth,
                // A hard Width forces that exact size regardless of what the surrounding layout
                // actually offers, unlike MaxWidth, which only caps it - see the type doc for why
                // that distinction is the whole point here.
                HorizontalAlignment = centerWhenStatic ? HorizontalAlignment.Center : HorizontalAlignment.Left,
                ClipToBounds = true,
                Children = { textBlock, trimmed }
            };

            DispatcherTimer? timer = null;
            double offset = 0;
            int direction = -1;
            int dwellTicksRemaining = startDwellTicks;
            double pixelsPerTick = 0;
            string? lastRestartedText = null;

            void ShowTrimmed(bool show)
            {
                trimmed.IsVisible = show;
                textBlock.IsVisible = !show;
            }

            void Restart()
            {
                timer?.Stop();
                timer = null;
                lastRestartedText = textBlock.Text;
                trimmed.Text = textBlock.Text;
                transform.X = 0;
                offset = 0;
                direction = -1;
                dwellTicksRemaining = startDwellTicks;

                double actualWidth = viewport.Bounds.Width > 0 ? viewport.Bounds.Width : maxWidth;
                textBlock.Width = double.NaN;
                textBlock.Measure(Size.Infinity);
                // Measuring alone only computes DesiredSize for the distance below - the
                // framework's own next layout pass would otherwise still arrange textBlock at
                // whatever (narrower) width the Grid offers it, and it renders clipped to that
                // arranged size regardless of this manual measurement. An explicit Width forces
                // its real arranged size to the full text, so the parent's clip - not the child's
                // own bounds - is what crops it while the RenderTransform slides it into view.
                textBlock.Width = textBlock.DesiredSize.Width;
                double distance = textBlock.DesiredSize.Width - actualWidth;
                if (distance <= 0)
                {
                    ShowTrimmed(false);
                    return;
                }

                // Longer overflow needs proportionally longer at a fixed per-character rate, so
                // speed up just enough to keep this title's own one-way reveal within
                // MaxScrollDurationMs, capped at LengthDrivenMaxCps.
                double urgencyPxPerSecond = targetCps * pxPerCharAtFontSize;
                double urgencyDurationMs = distance / urgencyPxPerSecond * 1000.0;
                double effectivePxPerSecond = urgencyDurationMs > MaxScrollDurationMs
                    ? Math.Min(distance / (MaxScrollDurationMs / 1000.0), LengthDrivenMaxCps * pxPerCharAtFontSize)
                    : urgencyPxPerSecond;
                pixelsPerTick = effectivePxPerSecond * TickMs / 1000.0;

                ShowTrimmed(true);
                DispatcherTimer t = new() { Interval = TimeSpan.FromMilliseconds(TickMs) };
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

                    offset += direction * pixelsPerTick;
                    if (offset <= -distance)
                    {
                        offset = -distance;
                        direction = 1;
                        dwellTicksRemaining = endDwellTicks;
                    }
                    else if (offset >= 0)
                    {
                        offset = 0;
                        direction = -1;
                        dwellTicksRemaining = startDwellTicks;
                        ShowTrimmed(true);
                    }

                    transform.X = offset;
                };
                timer = t;
                t.Start();
            }

            textBlock.PropertyChanged += (_, e) =>
            {
                // Live updates (TesseraLiveHost) set Text on every poll tick regardless of whether
                // the title actually changed - several times a second while media plays, per
                // flyout.log's SoftRefresh cadence. A full Restart() re-measures the whole string,
                // tears down and recreates the DispatcherTimer, and snaps the animation back to
                // its start; doing that on every poll instead of only on a real title change was
                // constant unnecessary work competing with the tween engine on the same UI thread
                // for no visible benefit, since the text never changed.
                if (e.Property == TextBlock.TextProperty && !Equals(textBlock.Text, lastRestartedText))
                {
                    Restart();
                }
            };

            // Restart() mutates textBlock.Width, which invalidates layout - so it must never be
            // driven by a signal that itself fires on layout changes (LayoutUpdated did exactly
            // that: fires on any layout pass anywhere in the tree, and Restart()'s own write could
            // trigger another one, indefinitely - a self-sustaining loop with no way to terminate,
            // visible as rapid flashing and, given enough queued dispatcher work, an unresponsive
            // UI thread). viewport.Bounds isn't valid yet at AttachedToVisualTree (no layout pass
            // has run), so instead poll for it a bounded number of times via low-priority posts,
            // which run after pending layout/render work rather than racing it.
            //
            // A single positive read isn't enough: nested Grid-in-StackPanel layouts like this one
            // can take more than one pass to converge, so an early read can be real but not yet
            // final. Trusting it anyway (as this used to) meant a title whose content gets rebuilt
            // mid-convergence - which is what happens on a track skip, unlike steady-state position
            // polling that only patches an already-settled instance's text - could lock in a
            // too-small width for its entire lifetime, rendering the trimmed overlay clipped to
            // almost nothing: a blank-looking title, only on skip, only sometimes, exactly the
            // reported symptom. Requiring the same width on two consecutive reads is a bounded,
            // finite way to wait for real convergence without reintroducing the LayoutUpdated risk.
            void WaitForFirstLayout(int attemptsLeft, double lastSeenWidth)
            {
                Dispatcher.UIThread.Post(
                    () =>
                    {
                        double currentWidth = viewport.Bounds.Width;
                        bool stable = currentWidth > 0 && Math.Abs(currentWidth - lastSeenWidth) < 0.5;
                        if (stable || attemptsLeft <= 0)
                        {
                            Restart();
                            return;
                        }

                        WaitForFirstLayout(attemptsLeft - 1, currentWidth);
                    },
                    DispatcherPriority.Loaded);
            }

            viewport.AttachedToVisualTree += (_, _) => WaitForFirstLayout(8, -1);
            viewport.DetachedFromVisualTree += (_, _) =>
            {
                timer?.Stop();
                timer = null;
            };

            return viewport;
        }
    }
}
