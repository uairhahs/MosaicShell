using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Media;
using MosaicShell.Core.Modules.Tessera;
using MosaicShell.Core.Styles;
using MosaicShell.Host.Capabilities;

namespace MosaicShell.Host.Tiles.Tessera
{
    /// <summary>
    /// Host binding for Fancy phase-2 (TweenNode1) layout reveal per style.
    /// Applies YourFlyouts Animated meter formulas to individual controls.
    /// </summary>
    internal sealed class TesseraRevealHost : ContentControl
    {
        public static readonly StyledProperty<double> RevealProgressProperty =
            AvaloniaProperty.Register<TesseraRevealHost, double>(nameof(RevealProgress), 0);

        public static readonly StyledProperty<string?> StyleIdProperty =
            AvaloniaProperty.Register<TesseraRevealHost, string?>(nameof(StyleId));

        public static readonly StyledProperty<bool> MusicVisibleProperty =
            AvaloniaProperty.Register<TesseraRevealHost, bool>(nameof(MusicVisible), true);

        public static readonly StyledProperty<double> FullMediaWidthProperty =
            AvaloniaProperty.Register<TesseraRevealHost, double>(nameof(FullMediaWidth), double.NaN);

        public static readonly StyledProperty<double> FullMediaHeightProperty =
            AvaloniaProperty.Register<TesseraRevealHost, double>(nameof(FullMediaHeight), double.NaN);

        public static readonly StyledProperty<double> FullDividerHeightProperty =
            AvaloniaProperty.Register<TesseraRevealHost, double>(nameof(FullDividerHeight), double.NaN);

        public static readonly StyledProperty<double> FullVolumeHeightProperty =
            AvaloniaProperty.Register<TesseraRevealHost, double>(nameof(FullVolumeHeight), double.NaN);

        public static readonly StyledProperty<double> FullVolumeWidthProperty =
            AvaloniaProperty.Register<TesseraRevealHost, double>(nameof(FullVolumeWidth), double.NaN);

        public static readonly StyledProperty<double> FullShellWidthProperty =
            AvaloniaProperty.Register<TesseraRevealHost, double>(nameof(FullShellWidth), double.NaN);

        public static readonly StyledProperty<double> ShellCornerRadiusProperty =
            AvaloniaProperty.Register<TesseraRevealHost, double>(nameof(ShellCornerRadius), double.NaN);

        public static readonly StyledProperty<double> BaseTrackThicknessProperty =
            AvaloniaProperty.Register<TesseraRevealHost, double>(nameof(BaseTrackThickness), double.NaN);

        public static readonly StyledProperty<Control?> DividerProperty =
            AvaloniaProperty.Register<TesseraRevealHost, Control?>(nameof(Divider));

        public static readonly StyledProperty<Control?> VolumeRowProperty =
            AvaloniaProperty.Register<TesseraRevealHost, Control?>(nameof(VolumeRow));

        public static readonly StyledProperty<TesseraTrack?> VolumeTrackProperty =
            AvaloniaProperty.Register<TesseraRevealHost, TesseraTrack?>(nameof(VolumeTrack));

        public static readonly StyledProperty<TesseraRingVolume?> RingVolumeProperty =
            AvaloniaProperty.Register<TesseraRevealHost, TesseraRingVolume?>(nameof(RingVolume));

        public static readonly StyledProperty<bool> Phase2EngagedProperty =
            AvaloniaProperty.Register<TesseraRevealHost, bool>(nameof(Phase2Engaged));

        public static readonly StyledProperty<bool> GnomeVolumeFillOnlyProperty =
            AvaloniaProperty.Register<TesseraRevealHost, bool>(nameof(GnomeVolumeFillOnly));

        private Border? _mediaClipHost;
        private Border? _mediaOverlay;
        private Control? _mediaLeaf;

        public double RevealProgress
        {
            get => GetValue(RevealProgressProperty);
            set => SetValue(RevealProgressProperty, value);
        }

        public string? StyleId
        {
            get => GetValue(StyleIdProperty);
            set => SetValue(StyleIdProperty, value);
        }

        public bool MusicVisible
        {
            get => GetValue(MusicVisibleProperty);
            set => SetValue(MusicVisibleProperty, value);
        }

        public double FullMediaWidth
        {
            get => GetValue(FullMediaWidthProperty);
            set => SetValue(FullMediaWidthProperty, value);
        }

        public double FullMediaHeight
        {
            get => GetValue(FullMediaHeightProperty);
            set => SetValue(FullMediaHeightProperty, value);
        }

        public double FullDividerHeight
        {
            get => GetValue(FullDividerHeightProperty);
            set => SetValue(FullDividerHeightProperty, value);
        }

        public double FullVolumeHeight
        {
            get => GetValue(FullVolumeHeightProperty);
            set => SetValue(FullVolumeHeightProperty, value);
        }

        public double FullVolumeWidth
        {
            get => GetValue(FullVolumeWidthProperty);
            set => SetValue(FullVolumeWidthProperty, value);
        }

        public double FullShellWidth
        {
            get => GetValue(FullShellWidthProperty);
            set => SetValue(FullShellWidthProperty, value);
        }

        public double ShellCornerRadius
        {
            get => GetValue(ShellCornerRadiusProperty);
            set => SetValue(ShellCornerRadiusProperty, value);
        }

        public double BaseTrackThickness
        {
            get => GetValue(BaseTrackThicknessProperty);
            set => SetValue(BaseTrackThicknessProperty, value);
        }

        public Control? Divider
        {
            get => GetValue(DividerProperty);
            set => SetValue(DividerProperty, value);
        }

        public Control? VolumeRow
        {
            get => GetValue(VolumeRowProperty);
            set => SetValue(VolumeRowProperty, value);
        }

        public TesseraTrack? VolumeTrack
        {
            get => GetValue(VolumeTrackProperty);
            set => SetValue(VolumeTrackProperty, value);
        }

        public TesseraRingVolume? RingVolume
        {
            get => GetValue(RingVolumeProperty);
            set => SetValue(RingVolumeProperty, value);
        }

        public bool Phase2Engaged
        {
            get => GetValue(Phase2EngagedProperty);
            set => SetValue(Phase2EngagedProperty, value);
        }

        public bool GnomeVolumeFillOnly
        {
            get => GetValue(GnomeVolumeFillOnlyProperty);
            set => SetValue(GnomeVolumeFillOnlyProperty, value);
        }

        static TesseraRevealHost()
        {
            _ = RevealProgressProperty.Changed.AddClassHandler<TesseraRevealHost>((h, _) => h.ApplyReveal());
            _ = StyleIdProperty.Changed.AddClassHandler<TesseraRevealHost>((h, _) => h.ApplyReveal());
            _ = MusicVisibleProperty.Changed.AddClassHandler<TesseraRevealHost>((h, _) => h.ApplyReveal());
            _ = DividerProperty.Changed.AddClassHandler<TesseraRevealHost>((h, _) => h.ApplyReveal());
            _ = VolumeRowProperty.Changed.AddClassHandler<TesseraRevealHost>((h, _) => h.ApplyReveal());
            _ = VolumeTrackProperty.Changed.AddClassHandler<TesseraRevealHost>((h, _) => h.ApplyReveal());
            _ = Phase2EngagedProperty.Changed.AddClassHandler<TesseraRevealHost>((h, _) => h.ApplyReveal());
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            ApplyReveal();
        }

        private void ApplyReveal()
        {
            double p = RevealProgress;
            bool music = MusicVisible;
            TesseraFlyoutRevealKind kind = TesseraFlyoutRevealSpec.ResolveHostKind(StyleId);

            switch (kind)
            {
                case TesseraFlyoutRevealKind.Fluent:
                    ApplyFluentReveal(p, music);
                    break;
                case TesseraFlyoutRevealKind.Windows11:
                    ApplyWin11Reveal(p, music);
                    break;
                case TesseraFlyoutRevealKind.Gnome:
                    ApplyGnomeReveal(p, music);
                    break;
                case TesseraFlyoutRevealKind.PlainText:
                    ApplyPlainTextReveal(p, music);
                    break;
                case TesseraFlyoutRevealKind.CoreUi:
                    ApplyCoreUiReveal(p, music);
                    break;
                case TesseraFlyoutRevealKind.Square:
                    ApplySquareReveal(p);
                    break;
                case TesseraFlyoutRevealKind.Meter:
                    ApplyMeterReveal(p, music);
                    break;
                case TesseraFlyoutRevealKind.Compact:
                    ApplyCompactReveal(p, music);
                    break;
                case TesseraFlyoutRevealKind.ModernFlyouts:
                    ApplyModernReveal(p, music);
                    break;
                case TesseraFlyoutRevealKind.MaterialYou:
                    ApplyMaterialYouReveal(p, music);
                    break;
                case TesseraFlyoutRevealKind.Radial:
                    ApplyRadialReveal(p, music);
                    break;
                default:
                    ApplyLegacyReveal((StyleId ?? string.Empty).ToLowerInvariant(), p, music);
                    break;
            }

            if (VisualRoot is FlyoutWindow flyout)
            {
                flyout.SyncRevealRegion();
            }
        }

        private void ApplyFluentReveal(double p, bool music)
        {
            if (Divider is Line line
                && !double.IsNaN(FullDividerHeight)
                && FullDividerHeight > 0)
            {
                double endY = TesseraFlyoutAnimatedTargetSpec.ResolveFluentDividerHeightDip(
                    FullDividerHeight, p, music);
                line.StartPoint = new Point(0, 0);
                line.EndPoint = new Point(0, endY);
                line.Height = Math.Max(0, endY);
                line.Opacity = TesseraFlyoutAnimatedTargetSpec.ResolveFluentDividerOpacity(p, music);
                line.IsVisible = music && endY > 0.5;
            }
            else if (Divider is Control divider
                     && !double.IsNaN(FullDividerHeight)
                     && FullDividerHeight > 0)
            {
                divider.Height = TesseraFlyoutAnimatedTargetSpec.ResolveFluentDividerHeightDip(
                    FullDividerHeight, p, music);
                divider.Opacity = TesseraFlyoutAnimatedTargetSpec.ResolveFluentDividerOpacity(p, music);
                divider.IsVisible = music || p > 0.001;
            }

            if (_mediaClipHost is not null
                && !double.IsNaN(FullMediaWidth)
                && FullMediaWidth > 0)
            {
                double layoutW = TesseraFlyoutAnimatedTargetSpec.ResolveFluentMediaLayoutWidthDip(
                    FullMediaWidth, music);
                _mediaClipHost.Width = layoutW > 0 ? layoutW : 0;
                _mediaClipHost.MaxHeight = double.PositiveInfinity;
                _mediaClipHost.ClipToBounds = true;
                double clipW = TesseraFlyoutAnimatedTargetSpec.ResolveFluentMediaClipWidthDip(
                    FullMediaWidth, p, music);
                ApplyHorizontalClip(_mediaClipHost, clipW, layoutW);
                _mediaClipHost.IsVisible = music || p > 0.001;
            }

            if (_mediaOverlay is not null)
            {
                _mediaOverlay.Opacity = TesseraFlyoutAnimatedTargetSpec.ResolveFluentMediaOverlayAlpha(p, music);
                _mediaOverlay.IsVisible = false;
            }

            if (_mediaLeaf is not null
                && !double.IsNaN(FullMediaWidth)
                && FullMediaWidth > 0)
            {
                _mediaLeaf.Opacity = TesseraFlyoutAnimatedTargetSpec.ResolveFluentMediaContentOpacity(p, music);
                _mediaLeaf.RenderTransform = null;
                _mediaLeaf.MaxWidth = FullMediaWidth;
                _mediaLeaf.MaxHeight = double.PositiveInfinity;
            }
        }

        private void ApplyWin11Reveal(double p, bool music)
        {
            if (VolumeRow is Control volumeRow
                && !double.IsNaN(FullVolumeHeight)
                && FullVolumeHeight > 0
                && !double.IsNaN(FullMediaHeight)
                && FullMediaHeight > 0)
            {
                double fullH = TesseraFlyoutAnimatedTargetSpec.ResolveWin11LayoutHeightDip(
                    FullVolumeHeight, FullMediaHeight, music);
                Height = fullH;
                MinHeight = fullH;
                MaxHeight = fullH;
                ClipToBounds = true;
                VerticalAlignment = VerticalAlignment.Top;

                double shellH = Phase2Engaged
                    ? TesseraFlyoutAnimatedTargetSpec.ResolveWin11BorderHeightDip(
                        FullVolumeHeight, FullMediaHeight, p, music)
                    : FullVolumeHeight;

                double shellW = double.IsNaN(FullShellWidth) ? double.PositiveInfinity : FullShellWidth;
                double radius = double.IsNaN(ShellCornerRadius) ? 0 : ShellCornerRadius;
                Clip = radius > 0 && !double.IsInfinity(shellW) && shellW > 0 && shellH > 0
                    ? new RectangleGeometry(new Rect(0, 0, shellW, shellH), radius, radius)
                    : (Geometry?)null;

                volumeRow.Opacity = TesseraFlyoutAnimatedTargetSpec.Win11VolumeControlsStayOpaque
                    ? 1
                    : TesseraFlyoutAnimatedTargetSpec.ResolveWin11VolumeFillOpacityFactor(p, music);
            }

            if (TesseraFlyoutHwndRegionSpec.StyleNeedsStrokeBRegion(StyleId, music)
                && VisualRoot is FlyoutWindow flyout)
            {
                flyout.SyncStrokeBRegion();
            }

            if (Divider is Control divider)
            {
                divider.Opacity = TesseraFlyoutAnimatedTargetSpec.ResolveMediaMaskOpacity(p, music);
                divider.IsVisible = music && p > 0.001;
            }

            if (_mediaClipHost is not null
                && !double.IsNaN(FullMediaHeight)
                && FullMediaHeight > 0)
            {
                double clipH = TesseraFlyoutAnimatedTargetSpec.ResolveWin11MediaClipHeightDip(
                    FullMediaHeight, p, music);
                _mediaClipHost.MaxHeight = clipH;
                _mediaClipHost.Width = double.NaN;
                _mediaClipHost.ClipToBounds = true;
                ApplyVerticalClip(_mediaClipHost, clipH, FullMediaHeight);
                _mediaClipHost.IsVisible = music || p > 0.001;
            }

            if (_mediaOverlay is not null)
            {
                _mediaOverlay.Opacity = TesseraFlyoutAnimatedTargetSpec.ResolveWin11MediaOverlayAlpha(p, music);
                _mediaOverlay.IsVisible = false;
            }

            if (_mediaLeaf is not null)
            {
                _mediaLeaf.Opacity = TesseraFlyoutAnimatedTargetSpec.ResolveClippedMediaLeafOpacity(p, music);
                _mediaLeaf.RenderTransform = null;
                _mediaLeaf.MaxHeight = double.IsNaN(FullMediaHeight) ? double.PositiveInfinity : FullMediaHeight;
            }
        }

        private void ApplyCoreUiReveal(double p, bool music)
        {
            if (VolumeTrack is TesseraTrack track && !double.IsNaN(BaseTrackThickness) && BaseTrackThickness > 0)
            {
                track.TrackThickness = TesseraFlyoutAnimatedTargetSpec.ResolveCoreUiLayoutTrackThicknessDip(
                    BaseTrackThickness);
                double scale = TesseraFlyoutAnimatedTargetSpec.ResolveCoreUiVolumeBarScaleFactor(p, music);
                track.RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);
                track.RenderTransform = Math.Abs(scale - 1) > 0.001
                    ? new ScaleTransform(1, Math.Max(0.001, scale))
                    : null;
            }

            if (_mediaClipHost is not null
                && !double.IsNaN(FullMediaWidth)
                && FullMediaWidth > 0)
            {
                double layoutW = TesseraFlyoutAnimatedTargetSpec.ResolveCoreUiMediaLayoutWidthDip(
                    FullMediaWidth, music);
                _mediaClipHost.Width = layoutW;
                _mediaClipHost.ClipToBounds = true;
                double clipW = TesseraFlyoutAnimatedTargetSpec.ResolveCoreUiMediaClipWidthDip(
                    FullMediaWidth, p, music);
                ApplyHorizontalClip(_mediaClipHost, clipW, layoutW);
                _mediaClipHost.IsVisible = music || p > 0.001;
            }

            if (_mediaOverlay is not null)
            {
                _mediaOverlay.Opacity = TesseraFlyoutAnimatedTargetSpec.ResolveCoreUiMediaOverlayAlpha(p, music);
                _mediaOverlay.IsVisible = false;
            }

            if (_mediaLeaf is not null)
            {
                _mediaLeaf.RenderTransform = null;
                _mediaLeaf.Opacity = TesseraFlyoutAnimatedTargetSpec.ResolveCoreUiMediaContentOpacityFactor(p, music);
            }
        }

        private void ApplyGnomeReveal(double p, bool music)
        {
            if (GnomeVolumeFillOnly)
            {
                if (_mediaLeaf is not null)
                {
                    _mediaLeaf.Opacity = TesseraFlyoutAnimatedTargetSpec.ResolveGnomeVolumeFillOpacity(p, music);
                    _mediaLeaf.RenderTransform = null;
                }
                _mediaOverlay?.IsVisible = false;

                return;
            }

            double scale = TesseraFlyoutAnimatedTargetSpec.ResolveGnomeContentScale(p, music);
            double alpha = TesseraFlyoutAnimatedTargetSpec.ResolveGnomeOverlayAlpha(p, music);

            if (_mediaOverlay is not null)
            {
                _mediaOverlay.Opacity = alpha;
                _mediaOverlay.IsVisible = false;
            }

            if (_mediaLeaf is not null)
            {
                _mediaLeaf.Opacity = TesseraFlyoutAnimatedTargetSpec.ResolveMediaMaskOpacity(p, music);
                _mediaLeaf.RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);
                _mediaLeaf.RenderTransform = Math.Abs(scale - 1) > 0.001
                    ? new ScaleTransform(scale, scale)
                    : null;
            }
        }

        private void ApplyPlainTextReveal(double p, bool music)
        {
            if (_mediaLeaf is null)
            {
                return;
            }

            _mediaLeaf.RenderTransform = null;
            _mediaLeaf.Opacity = TesseraFlyoutAnimatedTargetSpec.ResolvePlainTextFillOpacityFactor(p, music);
        }

        private void ApplySquareReveal(double p)
        {
            if (_mediaLeaf is null)
            {
                return;
            }

            double scale = TesseraFlyoutAnimatedTargetSpec.ResolveSquareLabelScale(p);
            _mediaLeaf.Opacity = 1;
            _mediaLeaf.RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);
            _mediaLeaf.RenderTransform = Math.Abs(scale - 1) > 0.001
                ? new ScaleTransform(scale, scale)
                : null;
        }

        private void ApplyMeterReveal(double p, bool music)
        {
            ApplyMediaMask(p, music);
            _mediaLeaf?.RenderTransform = null;
        }

        private void ApplyCompactReveal(double p, bool music)
        {
            ApplyMediaMask(p, music);
            _mediaLeaf?.RenderTransform = null;
        }

        private void ApplyModernReveal(double p, bool music)
        {
            ApplyMediaMask(p, music);
            _mediaLeaf?.RenderTransform = null;

            if (_mediaClipHost is not null
                && !double.IsNaN(FullMediaHeight)
                && FullMediaHeight > 0)
            {
                double clipH = TesseraFlyoutAnimatedTargetSpec.ResolveModernMediaClipHeightDip(
                    FullMediaHeight, p, music);
                _mediaClipHost.MaxHeight = clipH;
                _mediaClipHost.ClipToBounds = true;
                ApplyVerticalClip(_mediaClipHost, clipH, FullMediaHeight);
                _mediaClipHost.IsVisible = music || p > 0.001;
            }
        }

        private void ApplyMaterialYouReveal(double p, bool music)
        {
            ApplyMediaMask(p, music);
            _mediaLeaf?.RenderTransform = null;
        }

        /// <summary>
        /// Radial's hero ring sweeps its fill arc up to the live level on entry (see
        /// <see cref="RingVolume"/>). The side media panel fades/clips only, like every other
        /// style: it does not add a second, independently-directioned slide on top of the
        /// window's own phase-1 motion (ADR-0001: that was the source of the entrance appearing
        /// to reverse direction, since the two motions did not agree). One reveal kind serves both
        /// wraps; whichever target this host was built for is set.
        /// </summary>
        private void ApplyRadialReveal(double p, bool music)
        {
            if (RingVolume is { } ring)
            {
                ring.SweepFactor = TesseraFlyoutAnimatedTargetSpec.ResolveRadialRingSweepFactor(p);
                return;
            }

            ApplyMediaMask(p, music);
            _mediaLeaf?.RenderTransform = null;
        }

        private void ApplyMediaMask(double p, bool music)
        {
            _mediaLeaf?.Opacity = TesseraFlyoutAnimatedTargetSpec.ResolveMediaMaskOpacity(p, music);

            _mediaOverlay?.IsVisible = false;
        }

        private static void ApplyHorizontalClip(Border host, double clipWidthDip, double layoutWidthDip)
        {
            if (clipWidthDip <= 0)
            {
                host.Clip = new RectangleGeometry(new Rect(0, 0, 0, 0));
                return;
            }

            if (clipWidthDip >= layoutWidthDip - 0.01)
            {
                host.Clip = null;
                return;
            }

            double clipH = host.Bounds.Height;
            if (clipH < 1)
            {
                clipH = 4096;
            }

            host.Clip = new RectangleGeometry(new Rect(0, 0, clipWidthDip, clipH));
        }

        private static void ApplyVerticalClip(Border host, double clipHeightDip, double layoutHeightDip)
        {
            if (clipHeightDip <= 0)
            {
                host.Clip = new RectangleGeometry(new Rect(0, 0, 0, 0));
                return;
            }

            if (clipHeightDip >= layoutHeightDip - 0.01)
            {
                host.Clip = null;
                return;
            }

            double clipW = host.Bounds.Width;
            if (clipW < 1)
            {
                clipW = 4096;
            }

            host.Clip = new RectangleGeometry(new Rect(0, 0, clipW, clipHeightDip));
        }

        private void ApplyLegacyReveal(string style, double p, bool music)
        {
            _mediaLeaf?.Opacity = TesseraFlyoutRevealSpec.ResolveMediaOpacity(style, p, music);

            if (Divider is Control divider)
            {
                double scale = TesseraFlyoutRevealSpec.ResolveDividerScale(style, p, music);
                divider.Opacity = music ? scale : 0;
            }
        }

        internal static TesseraRevealHost WrapFluentVolumeDivider(TesseraFlyoutViewModel vm, Control volume)
        {
            double colW = TesseraFluentLayoutSpec.DividerColumnWidthDip;
            const double h = TesseraFluentLayoutSpec.HeightDip;
            const double pad = TesseraFluentLayoutSpec.PadDip;
            double innerH = h - (pad * 2);
            Line line = new()
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(0, 0),
                Stroke = TesseraPalette.StrokeBrush,
                StrokeThickness = 1,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, pad, 0, pad),
                Opacity = 0,
                IsHitTestVisible = false
            };
            Border column = new()
            {
                Width = colW,
                Height = h,
                Background = Brushes.Transparent,
                Child = line
            };
            StackPanel row = new()
            {
                Orientation = Orientation.Horizontal,
                Spacing = 0,
                Children = { volume, column }
            };
            Border unusedClip = new() { IsVisible = false };
            Border unusedOverlay = new() { IsVisible = false, IsHitTestVisible = false };
            return CreateHost(
                vm,
                row,
                unusedClip,
                unusedOverlay,
                mediaLeaf: null,
                divider: line,
                fullDividerHeight: innerH);
        }

        internal static TesseraRevealHost WrapMedia(
            TesseraFlyoutViewModel vm,
            Control media,
            Control? divider = null,
            double fullMediaWidth = double.NaN,
            double fullMediaHeight = double.NaN,
            double fullDividerHeight = double.NaN,
            bool vertical = false)
        {
            (Border? clipHost, Border? overlay, Grid? clipGrid) = BuildMediaClip(media);

            Control revealBody = clipGrid;
            if (divider is not null)
            {
                revealBody = new StackPanel
                {
                    Orientation = vertical
                        ? Orientation.Vertical
                        : Orientation.Horizontal,
                    Spacing = vertical ? 0 : 2,
                    Children = { divider, clipGrid }
                };
            }

            return CreateHost(
                vm,
                revealBody,
                clipHost,
                overlay,
                media,
                divider,
                fullMediaWidth,
                fullMediaHeight,
                fullDividerHeight);
        }

        internal static TesseraRevealHost WrapWin11Fancy(
            TesseraFlyoutViewModel vm,
            Control volumeRow,
            Control media,
            Control divider,
            double volumeHeight,
            double mediaHeight,
            double shellWidth,
            double cornerRadius,
            IBrush shellBrush)
        {
            (Border? clipHost, Border? overlay, Grid? clipGrid) = BuildMediaClip(media);
            StackPanel revealStack = new()
            {
                Orientation = Orientation.Vertical,
                Spacing = 0,
                Children = { divider, clipGrid }
            };
            StackPanel body = new()
            {
                Orientation = Orientation.Vertical,
                Spacing = 0,
                Children = { volumeRow, revealStack }
            };
            Control glass = TesseraChrome.GlassTinted(body, cornerRadius, shellBrush, w: shellWidth);

            return CreateHost(
                vm,
                glass,
                clipHost,
                overlay,
                media,
                divider,
                fullMediaHeight: mediaHeight,
                volumeRow: volumeRow,
                fullVolumeHeight: volumeHeight,
                fullShellWidth: shellWidth,
                shellCornerRadius: cornerRadius,
                styleOverride: TesseraFlyoutRevealSpec.StyleWindows11);
        }

        internal static TesseraRevealHost WrapCoreUi(
            TesseraFlyoutViewModel vm,
            Control media,
            TesseraTrack volumeTrack,
            double fullMediaWidth,
            double baseTrackThickness)
        {
            (Border? clipHost, Border? overlay, Grid? clipGrid) = BuildMediaClip(media);
            return CreateHost(
                vm,
                clipGrid,
                clipHost,
                overlay,
                media,
                fullMediaWidth: fullMediaWidth,
                volumeTrack: volumeTrack,
                baseTrackThickness: baseTrackThickness,
                styleOverride: TesseraFlyoutRevealSpec.StyleCoreUi);
        }

        /// <summary>
        /// Wraps whatever control presents the ring (a fixed-size Border in practice) so entrance
        /// motion can reach the ring's <see cref="TesseraRingVolume.SweepFactor"/> without changing
        /// its layout. <paramref name="content"/> and <paramref name="ring"/> are usually the same
        /// control; content is a container when the ring needs a fixed-size host around it.
        /// </summary>
        internal static TesseraRevealHost WrapRadialRingSweep(
            TesseraFlyoutViewModel vm, Control content, TesseraRingVolume ring)
        {
            Border unusedClip = new() { IsVisible = false };
            Border unusedOverlay = new() { IsVisible = false, IsHitTestVisible = false };
            return CreateHost(
                vm,
                content,
                unusedClip,
                unusedOverlay,
                mediaLeaf: null,
                styleOverride: TesseraFlyoutRevealSpec.StyleRadial,
                ringVolume: ring);
        }

        internal static TesseraRevealHost WrapGnomeVolumeFill(TesseraFlyoutViewModel vm, Control volume)
        {
            (Border? clipHost, Border? overlay, Grid? clipGrid) = BuildMediaClip(volume);
            return CreateHost(
                vm,
                clipGrid,
                clipHost,
                overlay,
                volume,
                styleOverride: TesseraFlyoutRevealSpec.StyleGnome,
                gnomeVolumeFillOnly: true);
        }

        private static (Border ClipHost, Border Overlay, Grid ClipGrid) BuildMediaClip(Control media)
        {
            Border clipHost = new()
            {
                ClipToBounds = true,
                Background = Brushes.Transparent,
            };
            Border overlay = new()
            {
                Background = Brushes.Transparent,
                IsHitTestVisible = false,
                IsVisible = false,
            };
            Grid clipGrid = new();
            clipGrid.Children.Add(clipHost);
            clipGrid.Children.Add(overlay);
            clipHost.Child = media;
            return (clipHost, overlay, clipGrid);
        }

        private static TesseraRevealHost CreateHost(
            TesseraFlyoutViewModel vm,
            Control content,
            Border clipHost,
            Border overlay,
            Control? mediaLeaf,
            Control? divider = null,
            double fullMediaWidth = double.NaN,
            double fullMediaHeight = double.NaN,
            double fullDividerHeight = double.NaN,
            Control? volumeRow = null,
            double fullVolumeHeight = double.NaN,
            double fullShellWidth = double.NaN,
            double shellCornerRadius = double.NaN,
            TesseraTrack? volumeTrack = null,
            double baseTrackThickness = double.NaN,
            string? styleOverride = null,
            bool gnomeVolumeFillOnly = false,
            TesseraRingVolume? ringVolume = null)
        {
            string styleId = (styleOverride ?? StyleIds.Normalize(vm.StyleId)).ToLowerInvariant();
            bool isPreview = TesseraRevealBuildContext.IsPreview;
            int ani = TesseraRevealBuildContext.IsActive ? TesseraRevealBuildContext.Ani : vm.Ani;
            bool music = vm.ShowMediaStrip || vm.HasMediaSession;
            bool willRunPhase2 = TesseraFlyoutAnimationPolicy.Phase2RequiresAnimatedLayout(
                ani, styleId, music);
            return new TesseraRevealHost
            {
                StyleId = styleId,
                MusicVisible = music,
                RevealProgress = TesseraFlyoutRevealSpec.ResolveInitialRevealProgress(
                    isPreview, willRunPhase2, TesseraRevealBuildContext.SessionAlreadyShowing),
                Phase2Engaged = TesseraFlyoutRevealSpec.ResolveInitialPhase2Engaged(
                    isPreview, willRunPhase2, TesseraRevealBuildContext.SessionAlreadyShowing),
                GnomeVolumeFillOnly = gnomeVolumeFillOnly,
                FullMediaWidth = fullMediaWidth,
                FullMediaHeight = fullMediaHeight,
                FullDividerHeight = fullDividerHeight,
                FullVolumeHeight = fullVolumeHeight,
                FullShellWidth = fullShellWidth,
                ShellCornerRadius = shellCornerRadius,
                BaseTrackThickness = baseTrackThickness,
                Divider = divider,
                VolumeRow = volumeRow,
                VolumeTrack = volumeTrack,
                RingVolume = ringVolume,
                Content = content,
                _mediaClipHost = clipHost,
                _mediaOverlay = overlay,
                _mediaLeaf = mediaLeaf,
            };
        }
    }
}
