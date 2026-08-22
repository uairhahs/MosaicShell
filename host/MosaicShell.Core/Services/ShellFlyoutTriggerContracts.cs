namespace MosaicShell.Core.Services;

/// <summary>ModernFlyouts-compatible shell flyout trigger (volume / media / brightness).</summary>
public enum ShellFlyoutKind
{
    Volume,
    Media,
    Brightness
}

/// <summary>
/// ShellHook-sourced flyout triggers (RegisterShellHookWindow + SHELLHOOK),
/// matching ModernFlyouts ShellMessageHookHandler.
/// </summary>
public interface IShellFlyoutTriggerSource : IDisposable
{
    bool IsActive { get; }
    event EventHandler<ShellFlyoutKind>? Triggered;
    void Start();
    void Stop();
}

/// <summary>Pure decode of SHELLHOOK wParam/lParam (unit-testable).</summary>
public static class ShellFlyoutTriggerDecoder
{
    public const int HsHellAppCommand = 12;
    public const int HsHellBrightness = 55;

    // ModernFlyouts HookMessageEnum values
    public const long MediaVolMute = 524288;
    public const long MediaVolMinus = 589824;
    public const long MediaVolPlus = 655360;
    public const long MediaStop = 851968;
    public const long MediaPlayPause = 917504;
    public const long MediaPrevious = 786432;
    public const long MediaNext = 720896;

    public static bool TryDecode(nint wParam, nint lParam, out ShellFlyoutKind kind)
    {
        kind = default;
        var wp = wParam.ToInt64();
        if (wp == HsHellBrightness)
        {
            kind = ShellFlyoutKind.Brightness;
            return true;
        }

        if (wp != HsHellAppCommand)
            return false;

        var lp = lParam.ToInt64();
        switch (lp)
        {
            case MediaVolMute:
            case MediaVolMinus:
            case MediaVolPlus:
                kind = ShellFlyoutKind.Volume;
                return true;
            case MediaStop:
            case MediaPlayPause:
            case MediaPrevious:
            case MediaNext:
                kind = ShellFlyoutKind.Media;
                return true;
            default:
                return false;
        }
    }
}
