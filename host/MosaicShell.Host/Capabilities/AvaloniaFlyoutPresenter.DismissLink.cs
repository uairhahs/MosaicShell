using Avalonia;
using Avalonia.Threading;
using MosaicShell.Core.Modules;
using MosaicShell.Core.Modules.Tessera;

namespace MosaicShell.Host.Capabilities
{
    /// <summary>
    /// The "Dismiss" link under the Tessera cluster (<see cref="TesseraFlyoutDismissSpec"/>): shown while the pointer is
    /// over the flyout or the link, placed under the cluster's bottom-right edge, and the whole session leaves when it is
    /// clicked. The rules and numbers are the spec's; this file only places the window and routes the events.
    /// </summary>
    public sealed partial class AvaloniaFlyoutPresenter
    {
        private FlyoutDismissLink? _dismissLink;
        private IDisposable? _dismissLinkGrace;

        private FlyoutDismissLink EnsureDismissLink()
        {
            if (_dismissLink is null)
            {
                _dismissLink = new FlyoutDismissLink();
                _dismissLink.DismissClicked += OnDismissLinkActivated;
                _dismissLink.HoverChanged += OnDismissLinkHoverChanged;
            }

            return _dismissLink;
        }

        /// <summary>The Tessera windows of the session that is up: the stacked slots, or the single window.</summary>
        private List<FlyoutWindow> TesseraSessionWindows()
        {
            if (_stackedSession is not null)
            {
                return [.. GetStackedWindowsFromSession()];
            }

            lock (_gate)
            {
                return _windows.TryGetValue(ModuleIds.Tessera, out FlyoutWindow? single) ? [single] : [];
            }
        }

        /// <summary>
        /// Shows, places or hides the link for the session's hover and phase. A hide caused only by the pointer leaving
        /// waits <see cref="TesseraFlyoutDismissSpec.HideGraceMs"/> and is dropped if the pointer comes back, which also
        /// carries the pointer across the gap from the cluster to the link; any other hide is immediate.
        /// </summary>
        private void SyncDismissLink()
        {
            List<FlyoutWindow> windows = TesseraSessionWindows();
            FlyoutWindow? lead = windows.FirstOrDefault();
            bool linkHovered = _dismissLink?.IsHovered ?? false;
            bool hovered = linkHovered || windows.Any(static w => w.IsFlyoutHovered);
            bool eligible = lead is not null && TesseraFlyoutDismissSpec.ShowsOn(lead.Kind);
            TesseraFlyoutPhase phase = lead?.Phase ?? TesseraFlyoutPhase.Hidden;
            bool shows = eligible && TesseraFlyoutDismissSpec.IsVisible(hovered, phase);
            bool onlyHoverLost = eligible && !hovered && TesseraFlyoutDismissSpec.IsVisible(hovered: true, phase);

            _dismissLinkGrace?.Dispose();
            _dismissLinkGrace = null;
            if (shows && lead is not null)
            {
                ShowDismissLink(lead, windows);
                return;
            }

            if (onlyHoverLost && _dismissLink is { IsVisible: true })
            {
                _dismissLinkGrace = DispatcherTimer.RunOnce(
                    () =>
                    {
                        _dismissLinkGrace = null;
                        if (!(_dismissLink?.IsHovered ?? false) && !TesseraSessionWindows().Any(static w => w.IsFlyoutHovered))
                        {
                            HideDismissLink();
                        }
                    },
                    TimeSpan.FromMilliseconds(TesseraFlyoutDismissSpec.HideGraceMs));
                return;
            }

            HideDismissLink();
        }

        private void ShowDismissLink(FlyoutWindow lead, IReadOnlyList<FlyoutWindow> windows)
        {
            // Where the panels rest, not where they are now: the entrance slide and the media reveal move and resize the
            // windows, and the link jumped to wherever the motion had got to each time the pointer came back.
            List<TesseraFlyoutDismissSpec.PanelBounds> panels = [];
            foreach (FlyoutWindow window in windows.Where(static w => w.IsVisible))
            {
                if (window.RestBoundsPx is { } rest)
                {
                    panels.Add(new(rest.X, rest.Y, rest.Width, rest.Height));
                }
            }

            if (TesseraFlyoutDismissSpec.ResolveClusterBounds(panels) is not { } cluster)
            {
                HideDismissLink();
                return;
            }

            FlyoutDismissLink link = EnsureDismissLink();
            double flyoutScale = TesseraFlyoutRequestBuilder.FlyoutScaleFromPayload(lead.FlyoutRequest.Payload);
            double monitorScale = lead.RenderScaling > 0.1 ? lead.RenderScaling : 1.0;
            link.ApplyScale(flyoutScale);
            Size pill = link.MeasurePill();
            int widthPx = (int)Math.Ceiling(pill.Width * monitorScale);
            int heightPx = (int)Math.Ceiling(pill.Height * monitorScale);
            PixelRect work = lead.Screens?.ScreenFromWindow(lead)?.WorkingArea ?? new PixelRect(0, 0, int.MaxValue / 2, int.MaxValue / 2);
            (int x, int y) = TesseraFlyoutDismissSpec.ResolvePlacement(
                cluster,
                widthPx,
                heightPx,
                (int)Math.Round(TesseraFlyoutDismissSpec.GapDip * flyoutScale * monitorScale),
                work.Y,
                work.Bottom);
            PixelPoint at = new(x, y);
            if (link.Position != at)
            {
                link.Position = at;
            }

            if (!link.IsVisible)
            {
                link.Show();
                Log($"dismiss link shown at={x},{y} size={widthPx}x{heightPx} cluster={cluster.X},{cluster.Y} {cluster.Width}x{cluster.Height}");
            }

            link.Opacity = 1;
        }

        private void HideDismissLink()
        {
            _dismissLinkGrace?.Dispose();
            _dismissLinkGrace = null;
            if (_dismissLink is { IsVisible: true } link)
            {
                link.Opacity = 0;
                link.Hide();
            }

            foreach (FlyoutWindow window in TesseraSessionWindows())
            {
                window.IsDismissLinkHovered = false;
            }
        }

        private void OnDismissLinkHoverChanged()
        {
            bool hovered = _dismissLink?.IsHovered ?? false;
            List<FlyoutWindow> windows = TesseraSessionWindows();
            foreach (FlyoutWindow window in windows)
            {
                window.IsDismissLinkHovered = hovered;
            }

            if (!hovered && windows.Any(static w => w.IsFlyoutSessionShowing) && !windows.Any(static w => w.IsFlyoutHovered))
            {
                // Leaving the link is leaving the flyout: it goes soon after, as from the cluster itself.
                if (_stackedSession is not null)
                {
                    StartStackedAutoDismiss(TesseraFlyoutDismissPolicy.ResolvePointerLeaveDismissMs(_stackedDismissRequest?.AutoDismissMs ?? 0));
                }
                else
                {
                    foreach (FlyoutWindow window in windows)
                    {
                        window.RestartPointerLeaveDismiss();
                    }
                }
            }

            SyncDismissLink();
        }

        private void OnDismissLinkActivated()
        {
            Log("dismiss link clicked");
            HideDismissLink();
            if (_stackedSession is not null)
            {
                DismissStackedTesseraImmediate();
                return;
            }

            DismissTesseraSingleImmediate();
        }

        private void OnTesseraHoverChanged()
        {
            SyncDismissLink();
        }
    }
}
