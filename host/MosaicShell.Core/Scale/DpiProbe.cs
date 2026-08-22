using System.Text.Json;

namespace MosaicShell.Core.Scale;

/// <summary>
/// Diagnostic helpers only. Product layout must not use system DPI —
/// Avalonia Screen.Scaling / RenderScaling already handle per-monitor DPI.
/// </summary>
public static class DpiProbe
{
    /// <summary>
    /// Obsolete product path. Always returns 1.0 so callers that still read
    /// a "dpi scale" factor do not double-apply OS DPI on top of Avalonia DIPs.
    /// </summary>
    public static double GetDpiScale() => 1.0;
}

public sealed class ScaleSettings
{
    /// <summary>Ignored on load; retained for backward-compatible JSON.</summary>
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
            return new ScaleSettings { UserScale = 1.0 };

        var json = File.ReadAllText(path);
        var settings = JsonSerializer.Deserialize<ScaleSettings>(json, JsonOptions)
                       ?? new ScaleSettings { UserScale = 1.0 };
        // Avalonia owns OS DPI; never restore a compound factor into the product path.
        settings.DpiScale = 1.0;
        if (settings.UserScale <= 0)
            settings.UserScale = 1.0;
        return settings;
    }

    public static void Save(ScaleSettings settings, string? path = null)
    {
        path ??= DefaultPath;
        settings.DpiScale = 1.0;
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(settings, JsonOptions));
    }
}
