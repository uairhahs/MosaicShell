using Avalonia.Controls;
using MosaicShell.Core;
using MosaicShell.Core.Modules;
using MosaicShell.Core.Services;

namespace MosaicShell.Host.Tiles.Surfaces;

/// <summary>Creates Avalonia tile chrome for a module id (built-in or loaded from module.dll).</summary>
public interface ITileViewFactory
{
    string ModuleId { get; }
    Control Create(HostServices services);
}

/// <summary>
/// Built-in + optional <c>Modules/{id}/module.dll</c> (or legacy <c>tile.dll</c>) factories.
/// Mirrors <see cref="MosaicShell.Core.Capabilities.CapabilityRegistry"/> external load.
/// </summary>
public sealed class TileViewRegistry
{
    private readonly Dictionary<string, ITileViewFactory> _factories =
        new(StringComparer.OrdinalIgnoreCase);

    public void Register(ITileViewFactory factory) =>
        _factories[factory.ModuleId] = factory;

    public bool TryGetFactory(string moduleId, out ITileViewFactory? factory) =>
        _factories.TryGetValue(moduleId, out factory);

    public void RegisterBuiltIns()
    {
        Register(new DelegateTileViewFactory("Canvas", s => new CanvasTileView(s.Metrics)));
        Register(new DelegateTileViewFactory("Chrono", _ => new ChronoTileView()));
        Register(new DelegateTileViewFactory("Phono", s => new PhonoTileView(s.Media)));
        Register(new DelegateTileViewFactory("Pulse", s => new PulseTileView(s.AudioLevels)));
        Register(new DelegateTileViewFactory("Tessera", s => new TesseraTileView(s.Audio, s.Brightness, s.Media)));
        Register(new DelegateTileViewFactory("Mixdeck", s => new MixdeckTileView(s.AppAudio, s.Audio)));
        Register(new DelegateTileViewFactory("Inlay", _ => new InlayTileView()));
        Register(new DelegateTileViewFactory("Slate", _ => new SlateTileView()));
        Register(new DelegateTileViewFactory("Chord", _ => new ChordTileView()));
        Register(new DelegateTileViewFactory("Substrate", s => new SubstrateTileView(s.Audio, s.Brightness, s.Media)));
    }

    /// <summary>
    /// Load <c>Modules\{id}\module.dll</c> or <c>tile.dll</c> exporting a parameterless <see cref="ITileViewFactory"/>.
    /// Built-ins win on id collision.
    /// </summary>
    public void TryLoadExternal(string moduleId, string modulesRoot)
    {
        if (_factories.ContainsKey(moduleId))
            return;

        foreach (var name in new[] { "module.dll", "tile.dll" })
        {
            var dll = Path.Combine(modulesRoot, moduleId, name);
            if (!File.Exists(dll))
                continue;
            try
            {
                var asm = System.Reflection.Assembly.LoadFrom(dll);
                var type = asm.GetExportedTypes()
                    .FirstOrDefault(t => typeof(ITileViewFactory).IsAssignableFrom(t)
                                         && !t.IsAbstract
                                         && t.GetConstructor(Type.EmptyTypes) is not null);
                if (type is null)
                    continue;
                if (Activator.CreateInstance(type) is ITileViewFactory factory)
                    Register(factory);
                return;
            }
            catch
            {
                // best-effort plugin load
            }
        }
    }

    public Control Create(ModuleInfo info, HostServices services)
    {
        TryLoadExternal(info.Id, AppPaths.ModulesDirectory);
        if (TryGetFactory(info.Id, out var factory) && factory is not null)
            return factory.Create(services);
        return new GenericTileView(info);
    }

    private sealed class DelegateTileViewFactory(string moduleId, Func<HostServices, Control> create) : ITileViewFactory
    {
        public string ModuleId => moduleId;
        public Control Create(HostServices services) => create(services);
    }
}

public static class TileSurfaceFactory
{
    private static readonly TileViewRegistry Registry = CreateDefault();

    private static TileViewRegistry CreateDefault()
    {
        var r = new TileViewRegistry();
        r.RegisterBuiltIns();
        return r;
    }

    /// <summary>Shared registry used by Host overlays.</summary>
    public static TileViewRegistry RegistryInstance => Registry;

    public static Control Create(ModuleInfo info, HostServices services) =>
        Registry.Create(info, services);
}
