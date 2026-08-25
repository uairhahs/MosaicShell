using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Media;
using MosaicShell.Core.Modules.Tessera;
using MosaicShell.Core.Styles;
using MosaicShell.Host.Capabilities;

namespace MosaicShell.Host.Tiles.Tessera;

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
        RevealProgressProperty.Changed.AddClassHandler<TesseraRevealHost>((h, _) => h.ApplyReveal());
        StyleIdProperty.Changed.AddClassHandler<TesseraRevealHost>((h, _) => h.ApplyReveal());
        MusicVisibleProperty.Changed.AddClassHandler<TesseraRevealHost>((h, _) => h.ApplyReveal());
        DividerProperty.Changed.AddClassHandler<TesseraRevealHost>((h, _) => h.ApplyReveal());
        VolumeRowProperty.Changed.AddClassHandler<TesseraRevealHost>((h, _) => h.ApplyReveal());
        VolumeTrackProperty.Changed.AddClassHandler<TesseraRevealHost>((h, _) => h.ApplyReveal());
        Phase2EngagedProperty.Changed.AddClassHandler<TesseraRevealHost>((h, _) => h.ApplyReveal());
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        ApplyReveal();
    }

    private void ApplyReveal()
    {
        var style = (StyleId ?? string.Empty).ToLowerInvariant();
        var p = RevealProgress;
        var music = MusicVisible;

        switch (style)
        {
            case TesseraFlyoutRevealSpec.StyleFluent:
                ApplyFluentReveal(p, music);
                return;
            case TesseraFlyoutRevealSpec.StyleWindows11:
                ApplyWin11Reveal(p, music);
                return;
            case TesseraFlyoutRevealSpec.StyleGnome:
                ApplyGnomeReveal(p, music);
                return;
            case TesseraFlyoutRevealSpec.StylePlainText:
                ApplyPlainTextReveal(p, music);
                return;
            case TesseraFlyoutRevealSpec.StyleCoreUi:
                ApplyCoreUiReveal(p, music);
                return;
            case TesseraFlyoutRevealSpec.StyleSquare:
                ApplySquareReveal(p);
                return;
            default:
                ApplyLegacyReveal(style, p, music);
                break;
        }
    }

    private void ApplyFluentReveal(double p, bool music)
    {
        if (Divider is Line line
            && !double.IsNaN(FullDividerHeight)
            && FullDividerHeight > 0)
        {
            var endY = TesseraFlyoutAnimatedTargetSpec.ResolveFluentDividerHeightDip(
                FullDividerHeight, p, music);
            line.StartPoint = new Point(0, 0);
            line.EndPoint = new Point(0, endY);
            line.Opacity = music ? 0.55 : 0;
            line.IsVisible = music || p > 0.001;
        }
        else if (Divider is Control divider
                 && !double.IsNaN(FullDividerHeight)
                 && FullDividerHeight > 0)
        {
            divider.Height = TesseraFlyoutAnimatedTargetSpec.ResolveFluentDividerHeightDip(
                FullDividerHeight, p, music);
            divider.Opacity = music ? 1 : 0;
            divider.IsVisible = music || p > 0.001;
        }

        if (_mediaClipHost is not null
            && !double.IsNaN(FullMediaWidth)
            && FullMediaWidth > 0)
        {
            var layoutW = TesseraFlyoutAnimatedTargetSpec.ResolveFluentMediaLayoutWidthDip(
                FullMediaWidth, music);
            _mediaClipHost.Width = layoutW > 0 ? layoutW : 0;
            _mediaClipHost.MaxHeight = double.PositiveInfinity;
            _mediaClipHost.ClipToBounds = true;
            var clipW = TesseraFlyoutAnimatedTargetSpec.ResolveFluentMediaClipWidthDip(
                FullMediaWidth, p, music);
            ApplyHorizontalClip(_mediaClipHost, clipW, layoutW);
            _mediaClipHost.IsVisible = music || p > 0.001;
        }

        if (_mediaOverlay is not null)
        {
            _mediaOverlay.Opacity = TesseraFlyoutAnimatedTargetSpec.ResolveFluentMediaOverlayAlpha(p, music);
            _mediaOverlay.IsVisible = false;
        }

        if (_mediaLeaf is not null)
        {
            _mediaLeaf.Opacity = 1;
            _mediaLeaf.RenderTransform = null;
            _mediaLeaf.MaxWidth = double.IsNaN(FullMediaWidth) ? double.PositiveInfinity : FullMediaWidth;
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
            var fullH = TesseraFlyoutAnimatedTargetSpec.ResolveWin11LayoutHeightDip(
                FullVolumeHeight, FullMediaHeight, music);
            Height = fullH;
            MinHeight = fullH;
            MaxHeight = fullH;
            ClipToBounds = true;
            VerticalAlignment = VerticalAlignment.Top;

            var shellH = Phase2Engaged
                ? TesseraFlyoutAnimatedTargetSpec.ResolveWin11BorderHeightDip(
                    FullVolumeHeight, FullMediaHeight, p, music)
                : FullVolumeHeight;

            var shellW = double.IsNaN(FullShellWidth) ? double.PositiveInfinity : FullShellWidth;
            var radius = double.IsNaN(ShellCornerRadius) ? 0 : ShellCornerRadius;
            if (radius > 0 && !double.IsInfinity(shellW) && shellW > 0 && shellH > 0)
                Clip = new RectangleGeometry(new Rect(0, 0, shellW, shellH), radius, radius);
            else
                Clip = null;

            volumeRow.Opacity = TesseraFlyoutAnimatedTargetSpec.Win11VolumeControlsStayOpaque
                ? 1
                : TesseraFlyoutAnimatedTargetSpec.ResolveWin11VolumeFillOpacityFactor(p, music);
        }

        if (TesseraFlyoutHwndRegionSpec.StyleNeedsStrokeBRegion(StyleId, music)
            && VisualRoot is FlyoutWindow flyout)
            flyout.SyncStrokeBRegion();

        if (Divider is Control divider)
        {
            divider.Opacity = music ? 1 : 0;
            divider.IsVisible = music && p > 0.001;
        }

        if (_mediaClipHost is not null
            && !double.IsNaN(FullMediaHeight)
            && FullMediaHeight > 0)
        {
            _mediaClipHost.MaxHeight = FullMediaHeight;
            _mediaClipHost.Width = double.NaN;
            _mediaClipHost.IsVisible = music || p > 0.001;
        }

        if (_mediaOverlay is not null)
        {
            _mediaOverlay.Opacity = TesseraFlyoutAnimatedTargetSpec.ResolveWin11MediaOverlayAlpha(p, music);
            _mediaOverlay.IsVisible = false;
        }

        if (_mediaLeaf is not null)
        {
            _mediaLeaf.Opacity = 1;
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
            var scale = TesseraFlyoutAnimatedTargetSpec.ResolveCoreUiVolumeBarScaleFactor(p, music);
            track.RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);
            track.RenderTransform = Math.Abs(scale - 1) > 0.001
                ? new ScaleTransform(1, Math.Max(0.001, scale))
                : null;
        }

        if (_mediaClipHost is not null
            && !double.IsNaN(FullMediaWidth)
            && FullMediaWidth > 0)
        {
            var layoutW = TesseraFlyoutAnimatedTargetSpec.ResolveCoreUiMediaLayoutWidthDip(
                FullMediaWidth, music);
            _mediaClipHost.Width = layoutW;
            _mediaClipHost.ClipToBounds = true;
            var clipW = TesseraFlyoutAnimatedTargetSpec.ResolveCoreUiMediaClipWidthDip(
                FullMediaWidth, p, music);
            ApplyHorizontalClip(_mediaClipHost, clipW, layoutW);
            _mediaClipHost.IsVisible = music || p > 0.001;
        }

        if (_mediaOverlay is not null)
        {
            _mediaOverlay.Opacity = TesseraFlyoutAnimatedTargetSpec.ResolveCoreUiMediaOverlayAlpha(p, music);
            _mediaOverlay.IsVisible = false;
        }

        if (_mediaLeaf is not null && !TesseraFlyoutAnimatedTargetSpec.CoreUiMediaMustClipOnly)
        {
            var offset = TesseraFlyoutAnimatedTargetSpec.ResolveCoreUiMediaSlideOffsetDip(
                FullMediaWidth, p, music);
            _mediaLeaf.RenderTransform = Math.Abs(offset) > 0.01
                ? new TranslateTransform(offset, 0)
                : null;
            _mediaLeaf.Opacity = TesseraFlyoutAnimatedTargetSpec.ResolveCoreUiMediaContentOpacityFactor(p, music);
        }
        else if (_mediaLeaf is not null)
        {
            _mediaLeaf.RenderTransform = null;
            _mediaLeaf.Opacity = 1;
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
            if (_mediaOverlay is not null)
                _mediaOverlay.IsVisible = false;
            return;
        }

        var scale = TesseraFlyoutAnimatedTargetSpec.ResolveGnomeContentScale(p, music);
        var alpha = TesseraFlyoutAnimatedTargetSpec.ResolveGnomeOverlayAlpha(p, music);

        if (_mediaOverlay is not null)
        {
            _mediaOverlay.Opacity = alpha;
            _mediaOverlay.IsVisible = false;
        }

        if (_mediaLeaf is not null)
        {
            _mediaLeaf.Opacity = 1;
            _mediaLeaf.RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);
            _mediaLeaf.RenderTransform = Math.Abs(scale - 1) > 0.001
                ? new ScaleTransform(scale, scale)
                : null;
        }
    }

    private void ApplyPlainTextReveal(double p, bool music)
    {
        if (_mediaLeaf is null)
            return;

        var panelW = double.IsNaN(FullMediaWidth)
            ? TesseraStackedPlacementSpec.PlainTextWidthDip
            : FullMediaWidth;
        var offset = TesseraFlyoutAnimatedTargetSpec.ResolvePlainTextSlideOffsetDip(panelW, 1, p, music);
        _mediaLeaf.RenderTransform = Math.Abs(offset) > 0.01
            ? new TranslateTransform(offset, 0)
            : null;
        _mediaLeaf.Opacity = TesseraFlyoutAnimatedTargetSpec.ResolvePlainTextFillOpacityFactor(p, music);
    }

    private void ApplySquareReveal(double p)
    {
        if (_mediaLeaf is null)
            return;

        var scale = TesseraFlyoutAnimatedTargetSpec.ResolveSquareLabelScale(p);
        _mediaLeaf.Opacity = 1;
        _mediaLeaf.RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);
        _mediaLeaf.RenderTransform = Math.Abs(scale - 1) > 0.001
            ? new ScaleTransform(scale, scale)
            : null;
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

        var clipH = host.Bounds.Height;
        if (clipH < 1)
            clipH = 4096;
        host.Clip = new RectangleGeometry(new Rect(0, 0, clipWidthDip, clipH));
    }

    private void ApplyLegacyReveal(string style, double p, bool music)
    {
        if (_mediaLeaf is not null)
            _mediaLeaf.Opacity = TesseraFlyoutRevealSpec.ResolveMediaOpacity(style, p, music);

        if (Divider is Control divider)
        {
            var scale = TesseraFlyoutRevealSpec.ResolveDividerScale(style, p, music);
            divider.Opacity = music ? scale : 0;
        }
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
        var (clipHost, overlay, clipGrid) = BuildMediaClip(media);

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
        var (clipHost, overlay, clipGrid) = BuildMediaClip(media);
        var revealStack = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 0,
            Children = { divider, clipGrid }
        };
        var body = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 0,
            Children = { volumeRow, revealStack }
        };
        var glass = TesseraChrome.GlassTinted(body, cornerRadius, shellBrush, w: shellWidth);

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
        var (clipHost, overlay, clipGrid) = BuildMediaClip(media);
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

    internal static TesseraRevealHost WrapGnomeVolumeFill(TesseraFlyoutViewModel vm, Control volume)
    {
        var (clipHost, overlay, clipGrid) = BuildMediaClip(volume);
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
        var clipHost = new Border
        {
            ClipToBounds = true,
            Background = Brushes.Transparent,
        };
        var overlay = new Border
        {
            Background = Brushes.Transparent,
            IsHitTestVisible = false,
            IsVisible = false,
        };
        var clipGrid = new Grid();
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
        Control mediaLeaf,
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
        bool gnomeVolumeFillOnly = false)
    {
        var styleId = (styleOverride ?? StyleIds.Normalize(vm.StyleId)).ToLowerInvariant();
        var isPreview = TesseraRevealBuildContext.IsPreview;
        var ani = TesseraRevealBuildContext.IsActive ? TesseraRevealBuildContext.Ani : vm.Ani;
        var willRunPhase2 = TesseraFlyoutAnimationPolicy.Phase2RequiresAnimatedLayout(
            ani, styleId, vm.ShowMediaStrip);
        return new TesseraRevealHost
        {
            StyleId = styleId,
            MusicVisible = vm.ShowMediaStrip,
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
            Content = content,
            _mediaClipHost = clipHost,
            _mediaOverlay = overlay,
            _mediaLeaf = mediaLeaf,
        };
    }
}
