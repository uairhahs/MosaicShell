namespace MosaicShell.Core.Services;

/// <summary>
/// Pure helpers for Caps/Num/Scroll detection. Win32 <c>GetKeyState</c> toggle bits are only
/// reliable on a thread that pumps input (UI or a dedicated hook loop), not on thread-pool timers.
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

    /// <summary>Poll interval on the lock-keys message thread (ms).</summary>
    public const int PollIntervalMs = 50;

    /// <summary>Delay after a toggle key event before re-sampling (ms).</summary>
    public const int PostToggleSampleDelayMs = 40;

    /// <summary>
    /// WH_KEYBOARD_LL must be installed on a dedicated STA thread that runs GetMessage,
    /// not on a thread-pool timer and not solely on the Avalonia UI arm path
    /// (ConfigureAwait can leave the hook on a thread with no pump).
    /// </summary>
    public const bool MustUseDedicatedMessagePump = true;

    /// <summary>
    /// After arm, mutate Caps/Num/Scroll only via key-down edge toggles.
    /// Polling GetKeyState on a message-only pump thread reads stale bits and undoes real presses.
    /// </summary>
    public const bool MustNotPollGetKeyStateOnPumpThread = true;

    public static bool IsToggleVirtualKey(int vk) =>
        vk is VkCapital or VkNumlock or VkScroll;

    public static bool IsKeyboardMessage(int msg) =>
        msg is WmKeydown or WmKeyup or WmSyskeydown or WmSyskeyup;

    /// <summary>WH_KEYBOARD_LL should schedule a toggle sample for this event.</summary>
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
