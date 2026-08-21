using Avalonia.Controls;
using MosaicShell.Core.Modules;
using MosaicShell.Core.Services;

namespace MosaicShell.Host.Tiles.Surfaces;

public static class TileSurfaceFactory
{
    public static Control Create(ModuleInfo info, HostServices services) => info.Id switch
    {
        "Canvas" => new CanvasTileView(services.Metrics),
        "Chrono" => new ChronoTileView(),
        "Phono" => new PhonoTileView(services.Media),
        "Pulse" => new PulseTileView(services.AudioLevels),
        "Tessera" => new TesseraTileView(services.Audio, services.Brightness, services.Media),
        "Mixdeck" => new MixdeckTileView(services.AppAudio),
        "Inlay" => new InlayTileView(),
        "Slate" => new SlateTileView(),
        "Chord" => new ChordTileView(),
        "Substrate" => new SubstrateTileView(services.Audio, services.Brightness),
        _ => new GenericTileView(info)
    };
}
