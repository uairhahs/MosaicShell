using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform;
using MosaicShell.Core.Capabilities;
using MosaicShell.Core.Services;

namespace MosaicShell.Host.Tiles.Tessera;

/// <summary>Scaled live Tessera flyout template for the module config panel.</summary>
public sealed class TesseraStylePreview : Border
{
    public static readonly StyledProperty<string?> StyleIdProperty =
        AvaloniaProperty.Register<TesseraStylePreview, string?>(nameof(StyleId), "Fluent");

    public static readonly StyledProperty<bool> ShowMediaStripProperty =
        AvaloniaProperty.Register<TesseraStylePreview, bool>(nameof(ShowMediaStrip), true);

    private static readonly Lazy<byte[]?> LogoPng = new(LoadLogoPng);

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

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == StyleIdProperty || change.Property == ShowMediaStripProperty)
            Rebuild();
    }

    private void Rebuild()
    {
        try
        {
            var services = HostServicesFakes.Create();
            services.Audio.MasterVolume = 0.62;
            if (services.Media is FakeMediaSessionService media)
            {
                media.Current = new MediaSessionInfo(
                    Title: "Sample track",
                    Artist: "Artist",
                    AppId: "preview",
                    IsPlaying: true,
                    ThumbnailPng: LogoPng.Value,
                    PositionSeconds: 42,
                    DurationSeconds: 180);
            }

            var payload = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["volume"] = "0.62",
                ["muted"] = "0",
                ["showMediaStrip"] = ShowMediaStrip ? "1" : "0",
                ["mediaTitle"] = "Sample track",
                ["mediaArtist"] = "Artist",
                ["mediaPlaying"] = "1"
            };

            var style = string.IsNullOrWhiteSpace(StyleId) ? "Fluent" : StyleId!;
            var request = new FlyoutRequest("Tessera", "vol", style, Payload: payload);
            var vm = TesseraFlyoutViewModel.FromRequest(services, request);
            var flyout = TesseraStyleFactory.Create(style, vm);
            flyout.IsHitTestVisible = false;
            _host.Content = flyout;
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

    private static byte[]? LoadLogoPng()
    {
        try
        {
            // Source of truth: .github/res/MosaicShell.png (linked into Assets via csproj)
            var uri = new Uri("avares://MosaicShell.Host/Assets/MosaicShell.png");
            using var stream = AssetLoader.Open(uri);
            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            return ms.ToArray();
        }
        catch
        {
            return null;
        }
    }
}
