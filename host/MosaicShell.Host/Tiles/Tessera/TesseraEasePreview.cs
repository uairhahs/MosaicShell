using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using MosaicShell.Core.Modules.Tessera;
using MosaicShell.Host.Capabilities;

namespace MosaicShell.Host.Tiles.Tessera;

/// <summary>Hub Motion marker that replays the live stepped ease when family or steps change.</summary>
public sealed class TesseraEasePreview : UserControl
{
    public static readonly StyledProperty<string?> AniEaseProperty =
        AvaloniaProperty.Register<TesseraEasePreview, string?>(nameof(AniEase));

    public static readonly StyledProperty<int> AniStepsProperty =
        AvaloniaProperty.Register<TesseraEasePreview, int>(
            nameof(AniSteps), TesseraFlyoutAnimationPolicy.DefaultAniSteps);

    private readonly Border _marker;
    private TranslateTransform _transform = new();
    private CancellationTokenSource? _run;

    static TesseraEasePreview()
    {
        AniEaseProperty.Changed.AddClassHandler<TesseraEasePreview>((p, _) => p.Replay());
        AniStepsProperty.Changed.AddClassHandler<TesseraEasePreview>((p, _) => p.Replay());
    }

    public TesseraEasePreview()
    {
        MinWidth = TesseraEasePreviewSpec.TrackWidthDip;
        Height = TesseraEasePreviewSpec.TrackHeightDip;
        ClipToBounds = false;

        _marker = new Border
        {
            Width = TesseraEasePreviewSpec.MarkerSizeDip,
            Height = TesseraEasePreviewSpec.MarkerSizeDip,
            CornerRadius = new CornerRadius(TesseraEasePreviewSpec.MarkerSizeDip / 2),
            Background = new SolidColorBrush(Color.Parse("#89dceb")),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center,
            RenderTransform = _transform,
        };

        Content = new Border
        {
            Background = new SolidColorBrush(Color.Parse("#313244")),
            CornerRadius = new CornerRadius(8),
            ClipToBounds = false,
            Child = _marker,
        };

        AttachedToVisualTree += (_, _) => Replay();
        DetachedFromVisualTree += (_, _) =>
        {
            _run?.Cancel();
            _run?.Dispose();
            _run = null;
        };
    }

    public string? AniEase
    {
        get => GetValue(AniEaseProperty);
        set => SetValue(AniEaseProperty, value);
    }

    public int AniSteps
    {
        get => GetValue(AniStepsProperty);
        set => SetValue(AniStepsProperty, value);
    }

    private void Replay()
    {
        if (VisualRoot is null)
            return;

        _run?.Cancel();
        _run?.Dispose();
        _run = new CancellationTokenSource();
        var token = _run.Token;
        _ = ReplayAsync(token);
    }

    private async Task ReplayAsync(CancellationToken token)
    {
        var from = TesseraEasePreviewSpec.OvershootPadDip;
        var to = from + TesseraEasePreviewSpec.TravelDip;
        _transform = new TranslateTransform(from, 0);
        _marker.RenderTransform = _transform;

        try
        {
            await FlyoutMotionController.AnimateSteppedAsync(
                    _transform,
                    TranslateTransform.XProperty,
                    from,
                    to,
                    TesseraEasePreviewSpec.ResolveDurationMs(AniSteps),
                    TesseraFlyoutAnimationPolicy.NormalizeEase(AniEase),
                    TesseraFlyoutAnimationPolicy.NormalizeAniSteps(AniSteps),
                    entrance: true,
                    token)
                .ConfigureAwait(true);
        }
        catch (OperationCanceledException)
        {
            /* superseded */
        }
    }
}
