using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace MosaicShell.Host.Tiles.Tessera;

/// <summary>Scaled live Tessera flyout template for the module config panel.</summary>
public sealed class TesseraStylePreview : Border
{
    public static readonly StyledProperty<string?> StyleIdProperty =
        AvaloniaProperty.Register<TesseraStylePreview, string?>(nameof(StyleId), "Fluent");

    public static readonly StyledProperty<bool> ShowMediaStripProperty =
        AvaloniaProperty.Register<TesseraStylePreview, bool>(nameof(ShowMediaStrip), true);

    public static readonly StyledProperty<string?> AccentColorProperty =
        AvaloniaProperty.Register<TesseraStylePreview, string?>(nameof(AccentColor));

    private readonly ContentControl _host = new()
    {
        HorizontalAlignment = HorizontalAlignment.Center,
        VerticalAlignment = VerticalAlignment.Center,
        IsHitTestVisible = false
    };

    public TesseraStylePreview()
    {
        Background = new SolidColorBrush(Color.Parse("#181825"));
        BorderBrush = new SolidColorBrush(Color.Parse("#313244"));
        BorderThickness = new Thickness(1);
        CornerRadius = new CornerRadius(12);
        Padding = new Thickness(16, 14);
        MinHeight = 148;
        ClipToBounds = true;
        Child = new Viewbox
        {
            Stretch = Stretch.Uniform,
            StretchDirection = StretchDirection.DownOnly,
            MaxHeight = 100,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Child = _host
        };
        Rebuild();
    }

    public string? StyleId
    {
        get => GetValue(StyleIdProperty);
        set => SetValue(StyleIdProperty, value);
    }

    public bool ShowMediaStrip
    {
        get => GetValue(ShowMediaStripProperty);
        set => SetValue(ShowMediaStripProperty, value);
    }

    public string? AccentColor
    {
        get => GetValue(AccentColorProperty);
        set => SetValue(AccentColorProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == StyleIdProperty || change.Property == ShowMediaStripProperty
            || change.Property == AccentColorProperty)
            Rebuild();
    }

    private void Rebuild()
    {
        try
        {
            var style = string.IsNullOrWhiteSpace(StyleId) ? "Fluent" : StyleId!;
            _host.Content = TesseraPreviewExporter.BuildFlyout(style, ShowMediaStrip, AccentColor);
        }
        catch
        {
            _host.Content = new TextBlock
            {
                Text = "Preview unavailable",
                Foreground = new SolidColorBrush(Color.Parse("#6c7086")),
                FontSize = 12,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
        }
    }
}
