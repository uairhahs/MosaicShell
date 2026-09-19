using Avalonia.Controls;
using Avalonia.Platform;
using MosaicShell.Core.Capabilities;
using MosaicShell.Core.Modules.Tessera;
using MosaicShell.Core.Services;
using MosaicShell.Core.Modules;

namespace MosaicShell.Host.Tiles.Tessera
{
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
            HostServices services = HostServicesFakes.Create();
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

            string style = string.IsNullOrWhiteSpace(styleId) ? "Fluent" : styleId;
            bool strip = showMediaStrip && TesseraLayoutCoverage.UsesStackedMediaStrip(style);
            Dictionary<string, string> payload = new(StringComparer.OrdinalIgnoreCase)
            {
                ["volume"] = "0.62",
                ["muted"] = "0",
                ["showMediaStrip"] = strip ? "1" : "0",
                ["mediaTitle"] = "Sample track",
                ["mediaArtist"] = "Artist",
                ["mediaPlaying"] = "1"
            };

            FlyoutRequest request = new(ModuleIds.Tessera, "vol", style, Payload: payload);
            TesseraFlyoutViewModel vm = TesseraFlyoutViewModel.FromRequest(services, request);
            Control flyout = TesseraStyleFactory.Create(style, vm, accentColor, embeddedPreview: true);
            flyout.IsHitTestVisible = false;
            return flyout;
        }

        private static byte[]? LoadLogoPng()
        {
            try
            {
                Uri uri = new("avares://MosaicShell.Host/Assets/MosaicShell.png");
                using Stream stream = AssetLoader.Open(uri);
                using MemoryStream ms = new();
                stream.CopyTo(ms);
                return ms.ToArray();
            }
            catch
            {
                return null;
            }
        }
    }
}
