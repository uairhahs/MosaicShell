using System.Runtime.InteropServices;
using System.Text.Json;

namespace MosaicShell.Core.Scale
{
    public static class DpiProbe
    {
        [DllImport("user32.dll")]
        private static extern uint GetDpiForSystem();

        /// <summary>Windows display scale (1.0 = 100%). Falls back to 1.0 if probe fails.</summary>
        public static double GetDpiScale()
        {
            try
            {
                if (!OperatingSystem.IsWindows())
                {
                    return 1.0;
                }

                uint dpi = GetDpiForSystem();
                return dpi == 0 ? 1.0 : Math.Round(dpi / 96.0, 4);
            }
            catch
            {
                return 1.0;
            }
        }
    }

    public sealed class ScaleSettings
    {
        public double DpiScale { get; set; } = 1.0;
        public double UserScale { get; set; } = 1.0;
    }

    public static class ScaleSettingsStore
    {
        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

        public static string DefaultPath =>
            Path.Combine(AppPaths.ConfigDirectory, "scale.json");

        public static ScaleSettings Load(string? path = null)
        {
            path ??= DefaultPath;
            if (!File.Exists(path))
            {
                return new ScaleSettings
                {
                    DpiScale = DpiProbe.GetDpiScale(),
                    UserScale = 1.0
                };
            }

            string json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<ScaleSettings>(json, JsonOptions)
                   ?? new ScaleSettings { DpiScale = DpiProbe.GetDpiScale(), UserScale = 1.0 };
        }

        public static void Save(ScaleSettings settings, string? path = null)
        {
            path ??= DefaultPath;
            _ = Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, JsonSerializer.Serialize(settings, JsonOptions));
        }
    }
}
