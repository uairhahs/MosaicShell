using System.Text.Json;
using MosaicShell.Core.Modules;

namespace MosaicShell.Core.Capabilities;

public sealed class CapabilityArmedState
{
    public List<string> Armed { get; set; } = [];
}

public static class CapabilityStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static string StorePath => Path.Combine(AppPaths.ConfigDirectory, "capabilities.json");

    public static CapabilityArmedState Load()
    {
        AppPaths.EnsureLayout();
        if (!File.Exists(StorePath)) return new CapabilityArmedState();
        try
        {
            return JsonSerializer.Deserialize<CapabilityArmedState>(File.ReadAllText(StorePath), JsonOptions)
                   ?? new CapabilityArmedState();
        }
        catch
        {
            return new CapabilityArmedState();
        }
    }

    public static void Save(CapabilityArmedState state)
    {
        AppPaths.EnsureLayout();
        File.WriteAllText(StorePath, JsonSerializer.Serialize(state, JsonOptions));
    }

    public static void SaveArmed(IEnumerable<string> armed) =>
        Save(new CapabilityArmedState { Armed = armed.Distinct(StringComparer.OrdinalIgnoreCase).ToList() });
}
