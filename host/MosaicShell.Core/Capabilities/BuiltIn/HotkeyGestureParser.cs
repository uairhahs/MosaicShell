using MosaicShell.Core.Services;

namespace MosaicShell.Core.Capabilities.BuiltIn;

/// <summary>Parses and formats Win+S style gestures for capability hotkeys.</summary>
public static class HotkeyGestureParser
{
    /// <summary>Gestures Windows usually owns; RegisterHotKey fails or steals shell UX.</summary>
    private static readonly HashSet<string> OsReserved = new(StringComparer.OrdinalIgnoreCase)
    {
        "Win+A", "Win+S", "Win+Q", "Win+E", "Win+I", "Win+L", "Win+D", "Win+R", "Win+X",
        "Win+Tab", "Win+Space", "Win+V", "Win+N", "Win+W", "Win+H", "Win+K", "Win+P",
        "Win+U", "Win+G", "Win+Z", "Ctrl+Esc"
    };

    public static bool IsLikelyOsReserved(string gesture) =>
        !string.IsNullOrWhiteSpace(gesture) && OsReserved.Contains(Normalize(gesture));

    public static string Normalize(string gesture)
    {
        if (!TryParse(gesture, out var mods, out var vk))
            return gesture.Trim();
        return Format(mods, vk);
    }

    /// <summary>Replace OS-reserved gestures with a safe Ctrl+Alt default for the module.</summary>
    public static string EnsureRegisterable(string moduleId, string gesture)
    {
        if (!string.IsNullOrWhiteSpace(gesture) && TryParse(gesture, out _, out _) && !IsLikelyOsReserved(gesture))
            return Normalize(gesture);
        return SafeDefault(moduleId);
    }

    public static string SafeDefault(string moduleId) => moduleId.ToLowerInvariant() switch
    {
        "inlay" => "Ctrl+Alt+I",
        "chord" => "Ctrl+Alt+K",
        "substrate" => "Ctrl+Alt+Q",
        "mixdeck" => "Ctrl+Alt+M",
        _ => "Ctrl+Alt+O"
    };

    public static bool TryParse(string gesture, out ModifierKeys mods, out int vk)
    {
        mods = ModifierKeys.None;
        vk = 0;
        if (string.IsNullOrWhiteSpace(gesture)) return false;
        var parts = gesture.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0) return false;
        foreach (var p in parts[..^1])
        {
            mods |= p.ToLowerInvariant() switch
            {
                "ctrl" or "control" => ModifierKeys.Control,
                "alt" => ModifierKeys.Alt,
                "shift" => ModifierKeys.Shift,
                "win" or "windows" or "meta" or "cmd" => ModifierKeys.Win,
                _ => ModifierKeys.None
            };
        }

        var key = parts[^1];
        if (key.Length == 1)
        {
            var c = char.ToUpperInvariant(key[0]);
            if (c is >= 'A' and <= 'Z' or >= '0' and <= '9')
            {
                vk = c;
                return mods != ModifierKeys.None; // require at least one modifier for global hotkeys
            }
            return false;
        }

        vk = key.ToLowerInvariant() switch
        {
            "space" => 0x20,
            "escape" or "esc" => 0x1B,
            "tab" => 0x09,
            "enter" or "return" => 0x0D,
            "f1" => 0x70,
            "f2" => 0x71,
            "f3" => 0x72,
            "f4" => 0x73,
            "f5" => 0x74,
            "f6" => 0x75,
            "f7" => 0x76,
            "f8" => 0x77,
            "f9" => 0x78,
            "f10" => 0x79,
            "f11" => 0x7A,
            "f12" => 0x7B,
            _ => 0
        };
        return vk != 0 && mods != ModifierKeys.None;
    }

    public static string Format(ModifierKeys mods, int vk)
    {
        var parts = new List<string>();
        if (mods.HasFlag(ModifierKeys.Control)) parts.Add("Ctrl");
        if (mods.HasFlag(ModifierKeys.Alt)) parts.Add("Alt");
        if (mods.HasFlag(ModifierKeys.Shift)) parts.Add("Shift");
        if (mods.HasFlag(ModifierKeys.Win)) parts.Add("Win");
        parts.Add(FormatVk(vk));
        return string.Join("+", parts);
    }

    public static string FormatVk(int vk) => vk switch
    {
        0x20 => "Space",
        0x1B => "Esc",
        0x09 => "Tab",
        0x0D => "Enter",
        >= 0x70 and <= 0x7B => "F" + (vk - 0x6F),
        >= 'A' and <= 'Z' => ((char)vk).ToString(),
        >= '0' and <= '9' => ((char)vk).ToString(),
        _ => "0x" + vk.ToString("X2")
    };
}
