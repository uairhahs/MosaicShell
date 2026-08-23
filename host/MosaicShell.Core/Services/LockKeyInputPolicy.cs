namespace MosaicShell.Core.Services;

/// <summary>
/// Pure helpers for Caps/Num/Scroll detection.
/// </summary>
public static class LockKeyInputPolicy
{
    public const int VkCapital = 0x14;
    public const int VkNumlock = 0x90;
    public const int VkScroll = 0x91;

    public const int WmKeydown = 0x0100;
    public const int WmKeyup = 0x0101;
    public const int WmSyskeydown = 0x0104;
    public const int WmSyskeyup = 0x0105;

    /// <summary>
    /// Legacy volume WH_KEYBOARD_LL must install on the arming thread (same as before).
    /// Lock keys use <see cref="LockKeyPollPolicy"/> polling instead of LL hooks.
    /// </summary>
    public const bool MustInstallOnCallingThreadLikeLegacyVolumeHook = true;

    /// <summary>
    /// After arm, mutate Caps/Num/Scroll only via key-down edge toggles on the LL callback.
    /// </summary>
    public const bool MustEdgeToggleOnKeyDown = true;

    public static bool IsToggleVirtualKey(int vk) =>
        vk is VkCapital or VkNumlock or VkScroll;

    public static bool IsKeyboardMessage(int msg) =>
        msg is WmKeydown or WmKeyup or WmSyskeydown or WmSyskeyup;

    /// <summary>WH_KEYBOARD_LL should handle this event for lock keys.</summary>
    public static bool ShouldSampleFromHook(int nCode, int msg, int vk) =>
        nCode >= 0 && IsKeyboardMessage(msg) && IsToggleVirtualKey(vk);

    /// <summary>Key-down edge for toggle keys (Caps does not auto-repeat on typical boards).</summary>
    public static bool IsToggleKeyDown(int msg) =>
        msg is WmKeydown or WmSyskeydown;

    /// <summary>Low bit of GetKeyState / keyboard state for Caps/Num/Scroll = toggled on.</summary>
    public static bool IsToggleBitOn(short keyState) => (keyState & 1) != 0;

    public static LockKeyKind? KindFromVirtualKey(int vk) => vk switch
    {
        VkCapital => LockKeyKind.CapsLock,
        VkNumlock => LockKeyKind.NumLock,
        VkScroll => LockKeyKind.ScrollLock,
        _ => null
    };
}
