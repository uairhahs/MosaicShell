using Avalonia.Controls;
using Avalonia.Platform;
using MosaicShell.Core.Capabilities;
using MosaicShell.Core.Services;
using MosaicShell.Core.Styles;

namespace MosaicShell.Host.Tiles.Tessera;

/// <summary>Builds sample Tessera flyouts for the module config panel preview.</summary>
public static class TesseraPreviewExporter
{
    private static readonly Lazy<byte[]?> LogoPng = new(LoadLogoPng);

    public static Control BuildFlyout(string styleId, bool showMediaStrip = true, string? accentColor = null)
    {
        TesseraGlass.PreviewMode = true;
        try
        {
            return BuildFlyoutCore(styleId, showMediaStrip, accentColor);
        }
        finally
        {
            TesseraGlass.PreviewMode = false;
        }
    }

    private static Control BuildFlyoutCore(string styleId, bool showMediaStrip = true, string? accentColor = null)
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

        var style = string.IsNullOrWhiteSpace(styleId) ? "Fluent" : styleId;
        var strip = showMediaStrip && TesseraLayoutCoverage.UsesStackedMediaStrip(style);
        var payload = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["volume"] = "0.62",
            ["muted"] = "0",
            ["showMediaStrip"] = strip ? "1" : "0",
            ["mediaTitle"] = "Sample track",
            ["mediaArtist"] = "Artist",
            ["mediaPlaying"] = "1"
        };

        var request = new FlyoutRequest("Tessera", "vol", style, Payload: payload);
        var vm = TesseraFlyoutViewModel.FromRequest(services, request);
        var flyout = TesseraStyleFactory.Create(style, vm, accentColor);
        flyout.IsHitTestVisible = false;
        return flyout;
    }

    private static byte[]? LoadLogoPng()
    {
        try
        {
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
