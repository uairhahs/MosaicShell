using System.IO.Compression;
using System.Text.Json;
using MosaicShell.Core.Runtime;
using MosaicShell.Core.Settings;

namespace MosaicShell.Core.Shp
{
    public sealed record ShpImportResult(
        bool Success,
        string Message,
        IReadOnlyList<string> ImportedModules);

    /// <summary>
    /// Imports MosaicShell module settings + wallpaper from an .shp ZIP.
    /// Skips Rainmeter.ini activation, plugins, and third-party app skins.
    /// </summary>
    public static class ShpImporter
    {
        public static ShpImportResult Import(string shpPath)
        {
            if (!File.Exists(shpPath))
            {
                return new ShpImportResult(false, $"File not found: {shpPath}", []);
            }

            AppPaths.EnsureLayout();
            string work = Path.Combine(AppPaths.CacheDirectory, "shp-" + Guid.NewGuid().ToString("N"));
            _ = Directory.CreateDirectory(work);

            try
            {
                ZipFile.ExtractToDirectory(shpPath, work);
                List<string> imported = [];

                // Wallpaper
                string wallpaperDir = Path.Combine(work, "Wallpaper");
                if (Directory.Exists(wallpaperDir))
                {
                    string destWall = Path.Combine(AppPaths.ConfigDirectory, "Wallpaper");
                    _ = Directory.CreateDirectory(destWall);
                    foreach (string file in Directory.GetFiles(wallpaperDir))
                    {
                        string target = Path.Combine(destWall, Path.GetFileName(file));
                        File.Copy(file, target, overwrite: true);
                    }
                }

                // Native module settings from Rainmeter/MosaicShell/*.json or Config/modules
                foreach (string? candidate in new[]
                         {
                             Path.Combine(work, "Rainmeter", "MosaicShell"),
                             Path.Combine(work, "MosaicShell", "Config", "modules"),
                             Path.Combine(work, "Config", "modules"),
                         })
                {
                    if (!Directory.Exists(candidate))
                    {
                        continue;
                    }

                    foreach (string json in Directory.GetFiles(candidate, "*.json"))
                    {
                        string id = Path.GetFileNameWithoutExtension(json);
                        _ = Directory.CreateDirectory(Path.Combine(AppPaths.ConfigDirectory, "modules"));
                        File.Copy(json, ModuleSettingsStore.PathFor(id), overwrite: true);
                        imported.Add(id);
                    }
                }

                // Manifest CoreModules hint → ensure empty settings exist
                string manifestPath = Path.Combine(work, "SHP-data.json");
                if (File.Exists(manifestPath))
                {
                    try
                    {
                        using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(manifestPath));
                        if (doc.RootElement.TryGetProperty("Data", out JsonElement data)
                            && data.TryGetProperty("CoreModules", out JsonElement mods))
                        {
                            foreach (string id in mods.GetString()?.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? [])
                            {
                                if (!imported.Contains(id, StringComparer.OrdinalIgnoreCase))
                                {
                                    EnsureDefaultSettings(id);
                                    imported.Add(id);
                                }
                            }
                        }
                    }
                    catch { /* ignore malformed */ }
                }

                return new ShpImportResult(true, $"Imported {imported.Count} module setting(s).", [.. imported.Distinct(StringComparer.OrdinalIgnoreCase)]);
            }
            catch (Exception ex)
            {
                return new ShpImportResult(false, ex.Message, []);
            }
            finally
            {
                try { Directory.Delete(work, recursive: true); } catch { /* ignore */ }
            }
        }

        private static void EnsureDefaultSettings(string id)
        {
            switch (id)
            {
                case "Chrono": _ = ModuleSettingsStore.Load(id, () => new ChronoSettings()); break;
                case "Canvas": _ = ModuleSettingsStore.Load(id, () => new CanvasSettings()); break;
                case "Phono": _ = ModuleSettingsStore.Load(id, () => new PhonoSettings()); break;
                case "Pulse": _ = ModuleSettingsStore.Load(id, () => new PulseSettings()); break;
                case "Tessera": _ = ModuleSettingsStore.Load(id, () => new TesseraSettings()); break;
                case "Mixdeck": _ = ModuleSettingsStore.Load(id, () => new MixdeckSettings()); break;
                case "Inlay": _ = ModuleSettingsStore.Load(id, () => new InlaySettings()); break;
                case "Chord": _ = ModuleSettingsStore.Load(id, () => new ChordSettings()); break;
                case "Substrate": _ = ModuleSettingsStore.Load(id, () => new SubstrateSettings()); break;
                case "Slate": _ = ModuleSettingsStore.Load(id, () => new SlateSettings()); break;
                default: ModuleSettingsStore.Save(id, new { Id = id }); break;
            }
        }
    }
}
