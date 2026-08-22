using Avalonia.Input;
using MosaicShell.Core.Capabilities.BuiltIn;
using MosaicShell.Core.Services;

namespace MosaicShell.Host.Input;

/// <summary>Maps Avalonia key events to RegisterHotKey-compatible gesture strings.</summary>
public static class HotkeyCapture
{
    public static bool TryFormat(KeyEventArgs e, out string gesture)
    {
        gesture = "";
        var key = e.Key;
        if (IsModifierOnly(key))
            return false;

        if (!TryMapVk(key, out var vk))
            return false;

        var mods = ModifierKeys.None;
        var km = e.KeyModifiers;
        if (km.HasFlag(KeyModifiers.Control)) mods |= ModifierKeys.Control;
        if (km.HasFlag(KeyModifiers.Alt)) mods |= ModifierKeys.Alt;
        if (km.HasFlag(KeyModifiers.Shift)) mods |= ModifierKeys.Shift;
        if (km.HasFlag(KeyModifiers.Meta)) mods |= ModifierKeys.Win;

        if (mods == ModifierKeys.None)
            return false;

        gesture = HotkeyGestureParser.Format(mods, vk);
        return true;
    }

    private static bool IsModifierOnly(Key key) =>
        key is Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt
            or Key.LeftShift or Key.RightShift or Key.LWin or Key.RWin
            or Key.System;

    private static bool TryMapVk(Key key, out int vk)
    {
        vk = 0;
        if (key is >= Key.A and <= Key.Z)
        {
            vk = 'A' + (key - Key.A);
            return true;
        }
        if (key is >= Key.D0 and <= Key.D9)
        {
            vk = '0' + (key - Key.D0);
            return true;
        }
        if (key is >= Key.F1 and <= Key.F12)
        {
            vk = 0x70 + (key - Key.F1);
            return true;
        }

        vk = key switch
        {
            Key.Space => 0x20,
            Key.Escape => 0x1B,
            Key.Tab => 0x09,
            Key.Enter or Key.Return => 0x0D,
            _ => 0
        };
        return vk != 0;
    }
}
