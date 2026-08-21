using System.Runtime.InteropServices;
using System.Diagnostics;

namespace MosaicShell.Core.Services;

/// <summary>Polls WMI brightness and raises Changed when the value moves.</summary>
public sealed class WindowsBrightnessChangeSource : IBrightnessChangeSource
{
    private readonly IBrightnessService _brightness;
    private readonly System.Threading.Timer _timer;
    private double _last = double.NaN;
    private bool _running;

    public WindowsBrightnessChangeSource(IBrightnessService brightness)
    {
        _brightness = brightness;
        _timer = new System.Threading.Timer(_ => Tick(), null, Timeout.Infinite, Timeout.Infinite);
    }

    public event EventHandler? Changed;

    public void Start()
    {
        _running = true;
        _last = _brightness.IsSupported ? _brightness.Brightness : double.NaN;
        _timer.Change(200, 200);
    }

    public void Stop()
    {
        _running = false;
        _timer.Change(Timeout.Infinite, Timeout.Infinite);
    }

    private void Tick()
    {
        if (!_running || !_brightness.IsSupported) return;
        var v = _brightness.Brightness;
        if (double.IsNaN(_last) || Math.Abs(v - _last) > 0.005)
        {
            _last = v;
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    public void Dispose() => _timer.Dispose();
}

/// <summary>
/// Best-effort: hide common Windows OSD windows (ShellExperienceHost / volume HUD).
/// Soft-fails if windows are not found.
/// </summary>
public sealed class WindowsNativeOsdSuppressor : INativeOsdSuppressor
{
    private System.Threading.Timer? _timer;
    public bool IsActive { get; private set; }

    public void Start()
    {
        if (IsActive) return;
        IsActive = true;
        SuppressOnce();
        _timer = new System.Threading.Timer(_ => SuppressOnce(), null, 100, 250);
    }

    public void Stop()
    {
        IsActive = false;
        _timer?.Dispose();
        _timer = null;
    }

    public void SuppressOnce()
    {
        try
        {
            foreach (var p in Process.GetProcessesByName("ShellExperienceHost"))
            {
                foreach (var h in EnumerateTopLevel(p.Id))
                    TryHide(h);
            }

            // Win11 volume HUD class names vary; also scan foreground-adjacent windows by title heuristics.
            EnumWindows((h, _) =>
            {
                var title = GetWindowTitle(h);
                var cls = GetClassName(h);
                if (cls.Contains("XamlExplorerHostIslandWindow", StringComparison.OrdinalIgnoreCase)
                    || title.Contains("Volume", StringComparison.OrdinalIgnoreCase)
                    || cls.Contains("Windows.UI.Core.CoreWindow", StringComparison.OrdinalIgnoreCase))
                {
                    // Only hide very small HUD-like windows
                    if (GetWindowRect(h, out var r))
                    {
                        var w = r.Right - r.Left;
                        var ht = r.Bottom - r.Top;
                        if (w > 40 && w < 900 && ht > 20 && ht < 500)
                            TryHide(h);
                    }
                }
                return true;
            }, IntPtr.Zero);
        }
        catch
        {
            // Soft-fail
        }
    }

    private static IEnumerable<IntPtr> EnumerateTopLevel(int pid)
    {
        var list = new List<IntPtr>();
        EnumWindows((h, _) =>
        {
            GetWindowThreadProcessId(h, out var windowPid);
            if (windowPid == (uint)pid && IsWindowVisible(h))
                list.Add(h);
            return true;
        }, IntPtr.Zero);
        return list;
    }

    private static void TryHide(IntPtr h)
    {
        if (h == IntPtr.Zero) return;
        ShowWindow(h, SW_HIDE);
    }

    private static string GetWindowTitle(IntPtr h)
    {
        var len = GetWindowTextLength(h);
        if (len <= 0) return "";
        var sb = new System.Text.StringBuilder(len + 1);
        GetWindowText(h, sb, sb.Capacity);
        return sb.ToString();
    }

    private static string GetClassName(IntPtr h)
    {
        var sb = new System.Text.StringBuilder(256);
        GetClassName(h, sb, sb.Capacity);
        return sb.ToString();
    }

    public void Dispose() => Stop();

    private const int SW_HIDE = 0;

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowTextLength(IntPtr hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetClassName(IntPtr hWnd, System.Text.StringBuilder lpClassName, int nMaxCount);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left, Top, Right, Bottom;
    }
}

/// <summary>Low-level WH_KEYBOARD_LL hook for volume keys when LegacyVol is enabled.</summary>
public sealed class WindowsLegacyMediaKeyHook : ILegacyMediaKeyHook
{
    private IntPtr _hook;
    private LowLevelKeyboardProc? _proc;
    public bool IsActive { get; private set; }
    public event EventHandler<LegacyVolumeKey>? Pressed;

    public void Start()
    {
        if (IsActive) return;
        _proc = HookCallback;
        using var cur = Process.GetCurrentProcess();
        using var mod = cur.MainModule!;
        _hook = SetWindowsHookEx(WH_KEYBOARD_LL, _proc, GetModuleHandle(mod.ModuleName), 0);
        IsActive = _hook != IntPtr.Zero;
    }

    public void Stop()
    {
        if (_hook != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_hook);
            _hook = IntPtr.Zero;
        }
        IsActive = false;
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN))
        {
            var vk = Marshal.ReadInt32(lParam);
            LegacyVolumeKey? key = vk switch
            {
                VK_VOLUME_UP => LegacyVolumeKey.Up,
                VK_VOLUME_DOWN => LegacyVolumeKey.Down,
                VK_VOLUME_MUTE => LegacyVolumeKey.Mute,
                _ => null
            };
            if (key is not null)
            {
                Pressed?.Invoke(this, key.Value);
                return (IntPtr)1; // swallow
            }
        }
        return CallNextHookEx(_hook, nCode, wParam, lParam);
    }

    public void Dispose() => Stop();

    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_SYSKEYDOWN = 0x0104;
    private const int VK_VOLUME_MUTE = 0xAD;
    private const int VK_VOLUME_DOWN = 0xAE;
    private const int VK_VOLUME_UP = 0xAF;

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);
}

public sealed class WindowsIdleService : IIdleService
{
    private System.Threading.Timer? _timer;
    private bool _fired;

    public TimeSpan IdleTime
    {
        get
        {
            var info = new LASTINPUTINFO { cbSize = (uint)Marshal.SizeOf<LASTINPUTINFO>() };
            if (!GetLastInputInfo(ref info)) return TimeSpan.Zero;
            var idleMs = unchecked(Environment.TickCount - (int)info.dwTime);
            return TimeSpan.FromMilliseconds(Math.Max(0, idleMs));
        }
    }

    public TimeSpan Threshold { get; set; } = TimeSpan.FromMinutes(5);
    public event EventHandler? IdleThresholdReached;

    public void Start()
    {
        _fired = false;
        _timer = new System.Threading.Timer(_ => Tick(), null, 1000, 1000);
    }

    public void Stop()
    {
        _timer?.Dispose();
        _timer = null;
        _fired = false;
    }

    private void Tick()
    {
        if (IdleTime >= Threshold)
        {
            if (!_fired)
            {
                _fired = true;
                IdleThresholdReached?.Invoke(this, EventArgs.Empty);
            }
        }
        else
        {
            _fired = false;
        }
    }

    public void Dispose() => Stop();

    [StructLayout(LayoutKind.Sequential)]
    private struct LASTINPUTINFO
    {
        public uint cbSize;
        public uint dwTime;
    }

    [DllImport("user32.dll")]
    private static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);
}

public sealed class NullNativeOsdSuppressor : INativeOsdSuppressor
{
    public bool IsActive { get; private set; }
    public void Start() => IsActive = true;
    public void Stop() => IsActive = false;
    public void SuppressOnce() { }
    public void Dispose() => Stop();
}

public sealed class NullLegacyMediaKeyHook : ILegacyMediaKeyHook
{
    public bool IsActive => false;
    public event EventHandler<LegacyVolumeKey>? Pressed;
    public void Start() { }
    public void Stop() { }
    public void Dispose() { }
}

public sealed class NullBrightnessChangeSource : IBrightnessChangeSource
{
    public event EventHandler? Changed;
    public void Start() { }
    public void Stop() { }
    public void Dispose() { }
}

public sealed class NullIdleService : IIdleService
{
    public TimeSpan IdleTime => TimeSpan.Zero;
    public TimeSpan Threshold { get; set; } = TimeSpan.FromMinutes(5);
    public event EventHandler? IdleThresholdReached;
    public void Start() { }
    public void Stop() { }
    public void Dispose() { }
}

public sealed class NullFlyoutPresenter : MosaicShell.Core.Capabilities.IFlyoutPresenter
{
    public void Show(MosaicShell.Core.Capabilities.FlyoutRequest request) { }
    public void Hide(string moduleId) { }
    public void HideAll() { }
}
