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
/// ModernFlyouts-style native OSD suppressor: locate explorer's volume/brightness host
/// (NativeHWNDHost / XamlExplorerHostIslandWindow @ ZBAND AboveLockUX), hook WinEvents,
/// and hide on SHOW. See ModernFlyouts.Core.Interop.NativeFlyoutHandler.
/// </summary>
public sealed class WindowsNativeOsdSuppressor : INativeOsdSuppressor
{
    private const uint WINEVENT_OUTOFCONTEXT = 0;
    private const uint EVENT_OBJECT_CREATE = 0x8000;
    private const uint EVENT_OBJECT_DESTROY = 0x8001;
    private const uint EVENT_OBJECT_SHOW = 0x8002;
    private const uint EVENT_OBJECT_HIDE = 0x8003;
    private const uint EVENT_OBJECT_STATECHANGE = 0x800A;
    private const int ZBandAboveLockUx = 0x12; // ModernFlyouts ZBandID.AboveLockUX
    private const int SW_HIDE = 0;
    private const int SW_MINIMIZE = 6;
    private const int SW_RESTORE = 9;
    private const int SW_FORCEMINIMIZE = 11;

    private readonly WinEventDelegate _winEventProc;
    private IntPtr _hook = IntPtr.Zero;
    private IntPtr _hwndHost = IntPtr.Zero;
    private IntPtr _hwndDui = IntPtr.Zero;
    private uint _shellPid;
    private bool _resolved;
    private System.Threading.Timer? _rehook;
    private System.Threading.Timer? _burst;
    private DateTimeOffset _burstUntil = DateTimeOffset.MinValue;

    public WindowsNativeOsdSuppressor() => _winEventProc = OnWinEvent;

    public bool IsActive { get; private set; }

    public void Start()
    {
        if (IsActive) return;
        IsActive = true;
        _resolved = TryResolveNativeFlyout();
        InstallHook();
        HideNativeFlyout(permanent: true);
    }

    public void Stop()
    {
        IsActive = false;
        _burst?.Dispose();
        _burst = null;
        _rehook?.Dispose();
        _rehook = null;
        RemoveHook();
        // Leave native OSD usable again after Tessera disarms
        try
        {
            if (_hwndDui != IntPtr.Zero)
                ShowWindowAsync(_hwndDui, SW_RESTORE);
        }
        catch { /* soft-fail */ }
        _hwndHost = IntPtr.Zero;
        _hwndDui = IntPtr.Zero;
        _resolved = false;
    }

    public void SuppressBurst(int durationMs = 2500)
    {
        if (!IsActive) return;
        _burstUntil = DateTimeOffset.UtcNow.AddMilliseconds(Math.Clamp(durationMs, 200, 8000));
        // Re-resolve often - explorer can recreate the OSD HWND between volume ticks
        _resolved = TryResolveNativeFlyout();
        HideNativeFlyout(permanent: false);
        _burst?.Dispose();
        _burst = new System.Threading.Timer(_ =>
        {
            if (!IsActive || DateTimeOffset.UtcNow > _burstUntil)
            {
                _burst?.Dispose();
                _burst = null;
                return;
            }
            if (!_resolved || _hwndHost == IntPtr.Zero || !IsWindow(_hwndHost))
                _resolved = TryResolveNativeFlyout();
            HideNativeFlyout(permanent: false);
        }, null, 0, 33);
    }

    public void SuppressOnce() => HideNativeFlyout(permanent: false);

    private void HideNativeFlyout(bool permanent)
    {
        try
        {
            if (!_resolved || _hwndHost == IntPtr.Zero || !IsWindow(_hwndHost))
                _resolved = TryResolveNativeFlyout();
            if (!_resolved) return;

            // ModernFlyouts: minimize inner content bridge, then hide/force-minimize host
            if (_hwndDui != IntPtr.Zero)
                ShowWindowAsync(_hwndDui, SW_MINIMIZE);
            ShowWindowAsync(_hwndHost, SW_HIDE);
            if (permanent)
            {
                ShowWindowAsync(_hwndHost, SW_MINIMIZE);
                ShowWindowAsync(_hwndHost, SW_FORCEMINIMIZE);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[OsdSuppressor] hide soft-fail: {ex.Message}");
        }
    }

    private bool TryResolveNativeFlyout()
    {
        _hwndHost = IntPtr.Zero;
        _hwndDui = IntPtr.Zero;

        ResolveClassNames(out var outerClass, out var outerName, out var innerClass, out var innerName);

        for (var host = IntPtr.Zero;
             (host = FindWindowEx(IntPtr.Zero, host, outerClass, outerName)) != IntPtr.Zero;)
        {
            var dui = FindWindowEx(host, IntPtr.Zero, innerClass, string.IsNullOrEmpty(innerName) ? null : innerName);
            if (dui == IntPtr.Zero) continue;

            GetWindowThreadProcessId(host, out var pid);
            if (pid == 0) continue;
            try
            {
                using var proc = Process.GetProcessById((int)pid);
                if (!proc.ProcessName.Equals("explorer", StringComparison.OrdinalIgnoreCase))
                    continue;
            }
            catch
            {
                continue;
            }

            if (!GetWindowBand(host, out var band))
                continue;
            if (band != (UIntPtr)ZBandAboveLockUx)
                continue;

            _hwndHost = host;
            _hwndDui = dui;
            _shellPid = pid;
            return true;
        }

        // Fallback: shell pid only (hook can still catch CREATE)
        _shellPid = (uint)GetShellProcessId();
        return false;
    }

    private static void ResolveClassNames(out string outerClass, out string? outerName, out string innerClass, out string? innerName)
    {
        // Win11 22H2+ (build >= 22620): XAML island OSD - ModernFlyouts NativeFlyoutHandler
        var build = GetOsBuild();
        if (build >= 22620)
        {
            outerClass = "XamlExplorerHostIslandWindow";
            outerName = null;
            innerClass = "Windows.UI.Composition.DesktopWindowContentBridge";
            innerName = "DesktopWindowXamlSource";
        }
        else
        {
            outerClass = "NativeHWNDHost";
            outerName = null;
            innerClass = "DirectUIHWND";
            innerName = null;
        }
    }

    private static int GetOsBuild()
    {
        try
        {
            var desc = RuntimeInformation.OSDescription; // e.g. Microsoft Windows 10.0.26200
            var dot = desc.LastIndexOf('.');
            if (dot >= 0 && int.TryParse(desc[(dot + 1)..], out var build))
                return build;
        }
        catch { /* ignore */ }
        return 0;
    }

    private static int GetShellProcessId()
    {
        try
        {
            var shell = GetShellWindow();
            if (shell == IntPtr.Zero) return 0;
            GetWindowThreadProcessId(shell, out var pid);
            return (int)pid;
        }
        catch
        {
            return 0;
        }
    }

    private void InstallHook()
    {
        RemoveHook();
        if (_shellPid == 0)
            _shellPid = (uint)GetShellProcessId();
        if (_shellPid == 0) return;

        _hook = SetWinEventHook(
            EVENT_OBJECT_CREATE,
            EVENT_OBJECT_STATECHANGE,
            IntPtr.Zero,
            _winEventProc,
            _shellPid,
            0,
            WINEVENT_OUTOFCONTEXT);

        if (_hook == IntPtr.Zero)
            System.Diagnostics.Debug.WriteLine("[OsdSuppressor] SetWinEventHook failed");
    }

    private void RemoveHook()
    {
        if (_hook == IntPtr.Zero) return;
        UnhookWinEvent(_hook);
        _hook = IntPtr.Zero;
    }

    private void OnWinEvent(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
    {
        if (!IsActive || idObject != 0 || idChild != 0 || hwnd == IntPtr.Zero)
            return;

        try
        {
            var inBurst = DateTimeOffset.UtcNow <= _burstUntil;
            if (!_resolved && (eventType == EVENT_OBJECT_CREATE || eventType == EVENT_OBJECT_SHOW))
            {
                var cls = GetClassName(hwnd);
                if (cls is "NativeHWNDHost" or "XamlExplorerHostIslandWindow")
                {
                    _resolved = TryResolveNativeFlyout();
                    if (_resolved)
                        InstallHook();
                    if (inBurst)
                        HideNativeFlyout(permanent: false);
                }
            }

            if (_hwndHost != IntPtr.Zero && hwnd == _hwndHost)
            {
                switch (eventType)
                {
                    case EVENT_OBJECT_CREATE:
                    case EVENT_OBJECT_SHOW:
                    case EVENT_OBJECT_STATECHANGE:
                        HideNativeFlyout(permanent: false);
                        break;
                    case EVENT_OBJECT_DESTROY:
                        _hwndHost = IntPtr.Zero;
                        _hwndDui = IntPtr.Zero;
                        _shellPid = 0;
                        _resolved = false;
                        _rehook?.Dispose();
                        _rehook = new System.Threading.Timer(_ => TryRehook(), null, 1500, 3000);
                        break;
                }
            }
            else if (inBurst && (eventType == EVENT_OBJECT_SHOW || eventType == EVENT_OBJECT_CREATE))
            {
                // Catch a newly minted OSD host during a burst even if band resolve lagged
                var cls = GetClassName(hwnd);
                if (cls is "NativeHWNDHost" or "XamlExplorerHostIslandWindow")
                {
                    _resolved = TryResolveNativeFlyout();
                    HideNativeFlyout(permanent: false);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[OsdSuppressor] WinEvent soft-fail: {ex.Message}");
        }
    }

    private void TryRehook()
    {
        if (!IsActive) return;
        _shellPid = (uint)GetShellProcessId();
        if (_shellPid == 0) return;
        _rehook?.Dispose();
        _rehook = null;
        _resolved = TryResolveNativeFlyout();
        InstallHook();
        HideNativeFlyout(permanent: true);
    }

    private static string GetClassName(IntPtr h)
    {
        var sb = new System.Text.StringBuilder(256);
        GetClassName(h, sb, sb.Capacity);
        return sb.ToString();
    }

    public void Dispose() => Stop();

    private delegate void WinEventDelegate(
        IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

    [DllImport("user32.dll")]
    private static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc,
        WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

    [DllImport("user32.dll")]
    private static extern bool UnhookWinEvent(IntPtr hWinEventHook);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string? lpszClass, string? lpszWindow);

    [DllImport("user32.dll")]
    private static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool IsWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern IntPtr GetShellWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetClassName(IntPtr hWnd, System.Text.StringBuilder lpClassName, int nMaxCount);

    [DllImport("user32.dll")]
    private static extern bool GetWindowBand(IntPtr hWnd, out UIntPtr pdwBand);
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
        if (nCode >= 0)
        {
            var msg = (int)wParam;
            if (msg is WM_KEYDOWN or WM_SYSKEYDOWN or WM_KEYUP or WM_SYSKEYUP)
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
                    // Only act on key-down; always swallow up+down so Windows HUD never sees the key
                    if (msg is WM_KEYDOWN or WM_SYSKEYDOWN)
                        Pressed?.Invoke(this, key.Value);
                    return (IntPtr)1;
                }
            }
        }
        return CallNextHookEx(_hook, nCode, wParam, lParam);
    }

    public void Dispose() => Stop();

    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_KEYUP = 0x0101;
    private const int WM_SYSKEYDOWN = 0x0104;
    private const int WM_SYSKEYUP = 0x0105;
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
    public void SuppressBurst(int durationMs = 1500) { }
    public void Dispose() => Stop();
}

public sealed class NullLegacyMediaKeyHook : ILegacyMediaKeyHook
{
    public bool IsActive => false;
    public event EventHandler<LegacyVolumeKey>? Pressed { add { } remove { } }
    public void Start() { }
    public void Stop() { }
    public void Dispose() { }
}

public sealed class NullBrightnessChangeSource : IBrightnessChangeSource
{
    public event EventHandler? Changed { add { } remove { } }
    public void Start() { }
    public void Stop() { }
    public void Dispose() { }
}

public sealed class NullIdleService : IIdleService
{
    public TimeSpan IdleTime => TimeSpan.Zero;
    public TimeSpan Threshold { get; set; } = TimeSpan.FromMinutes(5);
    public event EventHandler? IdleThresholdReached { add { } remove { } }
    public void Start() { }
    public void Stop() { }
    public void Dispose() { }
}

/// <summary>Win32 probe: foreground HWND covers its monitor work area (approx fullscreen).</summary>
public sealed class WindowsFullscreenProbe : IFullscreenProbe
{
    public bool IsForegroundFullscreen
    {
        get
        {
            try
            {
                var hwnd = GetForegroundWindow();
                if (hwnd == IntPtr.Zero) return false;
                if (!GetWindowRect(hwnd, out var wr)) return false;
                var mon = MonitorFromWindow(hwnd, 2 /* MONITOR_DEFAULTTONEAREST */);
                var info = new MONITORINFO { cbSize = (uint)Marshal.SizeOf<MONITORINFO>() };
                if (!GetMonitorInfo(mon, ref info)) return false;
                var mr = info.rcMonitor;
                // Near-cover of monitor bounds (±2px tolerance for borders)
                return wr.Left <= mr.Left + 2
                       && wr.Top <= mr.Top + 2
                       && wr.Right >= mr.Right - 2
                       && wr.Bottom >= mr.Bottom - 2;
            }
            catch
            {
                return false;
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left, Top, Right, Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MONITORINFO
    {
        public uint cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);
}

public sealed class NullFullscreenProbe : IFullscreenProbe
{
    public bool IsForegroundFullscreen => false;
}
