using System.Reflection;
using Avalonia.Controls;
using MosaicShell.Core;
using MosaicShell.Core.Install;
using MosaicShell.Core.Modules;
using MosaicShell.Core.Services;

namespace MosaicShell.Host.Tiles.Surfaces
{
    /// <summary>Creates Avalonia tile chrome for a module id (built-in or loaded from module.dll).</summary>
    public interface ITileViewFactory
    {
        string ModuleId { get; }
        Control Create(HostServices services);
    }

    /// <summary>
    /// Built-in + optional <c>Modules/{id}/module.dll</c> (or legacy <c>tile.dll</c>) factories.
    /// Mirrors <see cref="Core.Capabilities.CapabilityRegistry"/> external load.
    /// </summary>
    public sealed class TileViewRegistry
    {
        private readonly Dictionary<string, ITileViewFactory> _factories =
            new(StringComparer.OrdinalIgnoreCase);

        public void Register(ITileViewFactory factory)
        {
            _factories[factory.ModuleId] = factory;
        }

        public bool TryGetFactory(string moduleId, out ITileViewFactory? factory)
        {
            return _factories.TryGetValue(moduleId, out factory);
        }

        public void RegisterBuiltIns()
        {
            Register(new DelegateTileViewFactory("Canvas", s => new CanvasTileView(s.Metrics)));
            Register(new DelegateTileViewFactory("Chrono", _ => new ChronoTileView()));
            Register(new DelegateTileViewFactory("Phono", s => new PhonoTileView(s.Media)));
            Register(new DelegateTileViewFactory("Pulse", s => new PulseTileView(s.AudioLevels)));
            Register(new DelegateTileViewFactory(ModuleIds.Tessera, s => new TesseraTileView(s.Audio, s.Brightness, s.Media)));
            Register(new DelegateTileViewFactory("Mixdeck", s => new MixdeckTileView(s.AppAudio, s.Audio)));
            Register(new DelegateTileViewFactory("Inlay", _ => new InlayTileView()));
            Register(new DelegateTileViewFactory("Slate", _ => new SlateTileView()));
            Register(new DelegateTileViewFactory("Chord", _ => new ChordTileView()));
            Register(new DelegateTileViewFactory("Substrate", s => new SubstrateTileView(s.Audio, s.Brightness, s.Media)));
        }

        /// <summary>
        /// Load <c>Modules\{id}\module.dll</c> or <c>tile.dll</c> exporting a parameterless <see cref="ITileViewFactory"/>.
        /// Built-ins win on id collision. Loading is full trust: a third-party tile runs arbitrary
        /// code in the Host process.
        /// </summary>
        public void TryLoadExternal(string moduleId, string modulesRoot)
        {
            if (_factories.ContainsKey(moduleId) || !ModulePackagePolicy.IsValidModuleId(moduleId))
            {
                return;
            }

            foreach (string? name in new[] { "module.dll", "tile.dll" })
            {
                string dll = Path.Combine(modulesRoot, moduleId, name);
                if (!File.Exists(dll))
                {
                    continue;
                }

                try
                {
                    Assembly asm = Assembly.LoadFrom(dll);
                    Type? type = asm.GetExportedTypes()
                        .FirstOrDefault(t => typeof(ITileViewFactory).IsAssignableFrom(t)
                                             && !t.IsAbstract
                                             && t.GetConstructor(Type.EmptyTypes) is not null);
                    if (type is null)
                    {
                        continue;
                    }

                    if (Activator.CreateInstance(type) is ITileViewFactory factory)
                    {
                        Register(factory);
                    }

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
            return TryGetFactory(info.Id, out ITileViewFactory? factory) && factory is not null ? factory.Create(services) : new GenericTileView(info);
        }

        private sealed class DelegateTileViewFactory(string moduleId, Func<HostServices, Control> create) : ITileViewFactory
        {
            public string ModuleId => moduleId;
            public Control Create(HostServices services)
            {
                return create(services);
            }
        }
    }

    public static class TileSurfaceFactory
    {
        private static TileViewRegistry CreateDefault()
        {
            TileViewRegistry r = new();
            r.RegisterBuiltIns();
            return r;
        }

        /// <summary>Shared registry used by Host overlays.</summary>
        public static TileViewRegistry RegistryInstance { get; } = CreateDefault();

        public static Control Create(ModuleInfo info, HostServices services)
        {
            return RegistryInstance.Create(info, services);
        }
    }
}
