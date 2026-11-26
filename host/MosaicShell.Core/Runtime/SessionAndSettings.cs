using System.Text.Json;
using MosaicShell.Core.Capabilities;
using MosaicShell.Core.Modules;
using MosaicShell.Core.Styles;

namespace MosaicShell.Core.Runtime
{
    public sealed record TileSessionState(
        string ModuleId,
        int X,
        int Y,
        double Width,
        double Height);

    public sealed class SessionStore
    {
        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

        public static string StorePath => Path.Combine(AppPaths.ConfigDirectory, "sessions.json");

        public static IReadOnlyList<TileSessionState> Load()
        {
            AppPaths.EnsureLayout();
            if (!File.Exists(StorePath))
            {
                return [];
            }

            try
            {
                string json = File.ReadAllText(StorePath);
                return JsonSerializer.Deserialize<List<TileSessionState>>(json, JsonOptions) ?? [];
            }
            catch
            {
                return [];
            }
        }

        public static void Save(IEnumerable<TileSessionState> sessions)
        {
            AppPaths.EnsureLayout();
            File.WriteAllText(StorePath, JsonSerializer.Serialize(sessions.ToList(), JsonOptions));
        }
    }

    public static class ModuleSettingsStore
    {
        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

        public static string PathFor(string moduleId)
        {
            return Path.Combine(AppPaths.ConfigDirectory, "modules", moduleId + ".json");
        }

        public static T Load<T>(string moduleId, Func<T> factory) where T : class
        {
            AppPaths.EnsureLayout();
            _ = Directory.CreateDirectory(Path.Combine(AppPaths.ConfigDirectory, "modules"));
            string path = PathFor(moduleId);
            if (!File.Exists(path))
            {
                T created = factory();
                _ = StyleIds.TryMigratePersistedStyle(created);
                Save(moduleId, created);
                return created;
            }

            try
            {
                T loaded = JsonSerializer.Deserialize<T>(File.ReadAllText(path), JsonOptions) ?? factory();
                if (StyleIds.TryMigratePersistedStyle(loaded))
                {
                    Save(moduleId, loaded);
                }

                return loaded;
            }
            catch
            {
                return factory();
            }
        }

        public static void Save<T>(string moduleId, T settings)
        {
            AppPaths.EnsureLayout();
            _ = Directory.CreateDirectory(Path.Combine(AppPaths.ConfigDirectory, "modules"));
            File.WriteAllText(PathFor(moduleId), JsonSerializer.Serialize(settings, JsonOptions));
        }

        public static void Delete(string moduleId)
        {
            string path = PathFor(moduleId);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    /// <summary>What a removal took away, so the Hub can offer Undo for a bundled module.</summary>
    /// <param name="ModuleId">The removed module.</param>
    /// <param name="IsBundled">True when MosaicShell ships it, so adding it again restores the files.</param>
    /// <param name="WasArmed">True when it was in the armed list, so Undo should arm it again.</param>
    public sealed record ModuleRemoval(string ModuleId, bool IsBundled, bool WasArmed);

    public static class ModuleUninstaller
    {
        /// <inheritdoc cref="Remove"/>
        public static bool Uninstall(string moduleId, ITileRuntime? runtime = null, ICapabilityHost? capabilityHost = null)
        {
            return Remove(moduleId, runtime, capabilityHost) is not null;
        }

        /// <summary>
        /// Stops and disarms an installed module, deletes its folder under <c>Modules</c> and drops it
        /// from saved sessions and the armed list. Its settings file is kept, so adding the module back
        /// restores it as it was. Works for bundled and third-party modules alike.
        /// </summary>
        /// <returns>What was removed, or <see langword="null"/> when nothing was installed under that id.</returns>
        public static ModuleRemoval? Remove(string moduleId, ITileRuntime? runtime = null, ICapabilityHost? capabilityHost = null)
        {
            if (!IsSingleFolderName(moduleId) || !ModuleCatalog.IsInstalled(moduleId))
            {
                return null;
            }

            bool wasArmed = CapabilityStore.Load().Armed
                .Any(id => id.Equals(moduleId, StringComparison.OrdinalIgnoreCase));

            _ = (runtime?.Stop(moduleId));
            if (capabilityHost is not null)
            {
                _ = capabilityHost.DisarmAsync(moduleId).GetAwaiter().GetResult();
            }

            string dir = Path.Combine(AppPaths.ModulesDirectory, moduleId);
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, recursive: true);
            }

            // Drop from persisted sessions / armed list
            IEnumerable<TileSessionState> remaining = SessionStore.Load().Where(s => !s.ModuleId.Equals(moduleId, StringComparison.OrdinalIgnoreCase));
            SessionStore.Save(remaining);

            IEnumerable<string> armed = CapabilityStore.Load().Armed
                .Where(id => !id.Equals(moduleId, StringComparison.OrdinalIgnoreCase));
            CapabilityStore.SaveArmed(armed);
            return new ModuleRemoval(moduleId, ModuleCatalog.IsBundled(moduleId), wasArmed);
        }

        private static bool IsSingleFolderName(string moduleId)
        {
            return !string.IsNullOrWhiteSpace(moduleId)
                && moduleId is not ("." or "..")
                && moduleId.IndexOfAny(['/', '\\']) < 0
                && moduleId.IndexOfAny(Path.GetInvalidFileNameChars()) < 0;
        }
    }

    public sealed class ModuleManifest
    {
        public string Id { get; set; } = "";
        public string Version { get; set; } = "0.0.0";
        public string? DisplayName { get; set; }
        /// <summary>Hub / Library blurb for discovered modules.</summary>
        public string? Description { get; set; }
        /// <summary>Optional how-to text for module config (third-party modules).</summary>
        public string? UsageSummary { get; set; }
        public string? HowToTrigger { get; set; }
        public Dictionary<string, string>? DefaultSettings { get; set; }

        /// <summary>Widget | Capability | Hybrid</summary>
        public string Kind { get; set; } = "Capability";
        public string? CapabilityId { get; set; }
        public bool DefaultArmed { get; set; }
        public List<string> Styles { get; set; } = [];
        public string? DefaultStyle { get; set; }

        public static string PathInModule(string moduleId)
        {
            return Path.Combine(AppPaths.ModulesDirectory, moduleId, "module.manifest.json");
        }

        public static ModuleManifest? TryLoad(string moduleId)
        {
            string path = PathInModule(moduleId);
            if (!File.Exists(path))
            {
                return null;
            }

            try
            {
                return JsonSerializer.Deserialize<ModuleManifest>(File.ReadAllText(path));
            }
            catch
            {
                return null;
            }
        }

        public static ModuleManifest CreateDefault(string moduleId, string? displayName = null)
        {
            bool isWidget = ModuleCatalog.TryGet(moduleId, out ModuleInfo? info)
                           && info is not null
                           && info.Kind == ModuleKind.Widget;
            List<string> styles = [.. StyleCatalog.IdsFor(moduleId)];
            return new ModuleManifest
            {
                Id = moduleId,
                Version = "1.0.0",
                DisplayName = displayName ?? moduleId,
                Kind = isWidget ? "Widget" : "Capability",
                CapabilityId = isWidget ? null : moduleId,
                DefaultArmed = !isWidget && moduleId.Equals("Tessera", StringComparison.OrdinalIgnoreCase),
                Styles = styles,
                DefaultStyle = styles.FirstOrDefault()
            };
        }

        public static void WriteDefault(string moduleId, string? displayName = null)
        {
            string path = PathInModule(moduleId);
            _ = Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            ModuleManifest manifest = CreateDefault(moduleId, displayName);
            File.WriteAllText(path, JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }));
        }
    }
}
