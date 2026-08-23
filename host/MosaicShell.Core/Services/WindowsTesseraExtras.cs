using System.Diagnostics;
using System.Runtime.InteropServices;
using NAudio.CoreAudioApi;

namespace MosaicShell.Core.Services;

/// <summary>
/// Caps/Num/Scroll via WH_KEYBOARD_LL on a dedicated STA GetMessage pump
/// (same pattern as <see cref="WindowsShellFlyoutHook"/>).
/// Toggle truth is edge-flipped on key-down, do not poll GetKeyState afterward on this
/// thread (message-only queues keep stale toggle bits and would undo real Caps presses).
/// </summary>
public sealed class WindowsLockKeysService : ILockKeysService
{
    private readonly object _sync = new();
    private Thread? _thread;
    private volatile bool _running;
    private IntPtr _hwnd;
    private IntPtr _hook;
    private LowLevelKeyboardProc? _proc;
    private WndProc? _wndProc;
    private bool _caps, _num, _scroll;

    public LockKeyState Caps => new(LockKeyKind.CapsLock, _caps);
    public LockKeyState Num => new(LockKeyKind.NumLock, _num);
    public LockKeyState Scroll => new(LockKeyKind.ScrollLock, _scroll);
    public event EventHandler<LockKeyState>? Changed;

    public void Start()
    {
        lock (_sync)
        {
            if (_running) return;
            // Snapshot once before the pump; after that only LL edge-toggles mutate state.
            _caps = LockKeyInputPolicy.IsToggleBitOn(GetKeyState(LockKeyInputPolicy.VkCapital));
            _num = LockKeyInputPolicy.IsToggleBitOn(GetKeyState(LockKeyInputPolicy.VkNumlock));
            _scroll = LockKeyInputPolicy.IsToggleBitOn(GetKeyState(LockKeyInputPolicy.VkScroll));
            _running = true;
            _thread = new Thread(MessageLoop)
            {
                IsBackground = true,
                Name = "MosaicShell.LockKeys"
            };
            _thread.SetApartmentState(ApartmentState.STA);
            _thread.Start();
        }

        for (var i = 0; i < 50 && _hwnd == IntPtr.Zero && _running; i++)
            Thread.Sleep(10);

        if (_hwnd == IntPtr.Zero)
            Debug.WriteLine("[LockKeys] message HWND soft-failed");
        else if (_hook == IntPtr.Zero)
            Debug.WriteLine("[LockKeys] keyboard hook soft-failed");
    }

    public void Stop()
    {
        Thread? thread;
        lock (_sync)
        {
            if (!_running) return;
            _running = false;
            thread = _thread;
            _thread = null;
        }

        var hwnd = _hwnd;
        if (hwnd != IntPtr.Zero)
        {
            try { PostMessage(hwnd, WmQuit, IntPtr.Zero, IntPtr.Zero); } catch { /* ignore */ }
        }

        thread?.Join(1500);
        _hwnd = IntPtr.Zero;
        _hook = IntPtr.Zero;
    }

    private void MessageLoop()
    {
        try
        {
            _wndProc = WndProcImpl;
            var className = "MosaicShell.LockKeys." + Guid.NewGuid().ToString("N");
            var wc = new WndClass
            {
                lpfnWndProc = Marshal.GetFunctionPointerForDelegate(_wndProc),
                hInstance = GetModuleHandle(null),
                lpszClassName = className
            };
            if (RegisterClass(ref wc) == 0 && Marshal.GetLastWin32Error() != 1410)
            {
                Debug.WriteLine("[LockKeys] RegisterClass failed");
                return;
            }

            _hwnd = CreateWindowEx(
                0, className, "MosaicShell LockKeys",
                0, 0, 0, 0, 0,
                HwndMessage, IntPtr.Zero, wc.hInstance, IntPtr.Zero);
            if (_hwnd == IntPtr.Zero)
            {
                Debug.WriteLine("[LockKeys] CreateWindowEx failed");
                return;
            }

            // Install on THIS pump thread so WH_KEYBOARD_LL callbacks are delivered here.
            InstallHook();

            while (_running)
            {
                var gm = GetMessage(out var msg, IntPtr.Zero, 0, 0);
                if (gm <= 0) break;
                TranslateMessage(ref msg);
                DispatchMessage(ref msg);
            }

            RemoveHook();
            try { DestroyWindow(_hwnd); } catch { /* ignore */ }
            _hwnd = IntPtr.Zero;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[LockKeys] {ex.Message}");
            _hwnd = IntPtr.Zero;
        }
    }

    private IntPtr WndProcImpl(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam) =>
        DefWindowProc(hWnd, msg, wParam, lParam);

    private void InstallHook()
    {
        if (_hook != IntPtr.Zero) return;
        _proc = HookCallback;
        _hook = SetWindowsHookEx(WhKeyboardLl, _proc, GetModuleHandle(null), 0);
        if (_hook == IntPtr.Zero)
            _hook = SetWindowsHookEx(WhKeyboardLl, _proc, GetModuleHandle("user32.dll"), 0);
        if (_hook == IntPtr.Zero)
            Debug.WriteLine($"[LockKeys] SetWindowsHookEx failed err={Marshal.GetLastWin32Error()}");
    }

    private void RemoveHook()
    {
        if (_hook == IntPtr.Zero) return;
        UnhookWindowsHookEx(_hook);
        _hook = IntPtr.Zero;
        _proc = null;
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        var msg = (int)wParam;
        var vk = Marshal.ReadInt32(lParam);
        if (LockKeyInputPolicy.ShouldSampleFromHook(nCode, msg, vk)
            && LockKeyInputPolicy.IsToggleKeyDown(msg))
        {
            ApplyToggleEdge(vk);
        }

        return CallNextHookEx(_hook, nCode, wParam, lParam);
    }

    private void ApplyToggleEdge(int vk)
    {
        LockKeyState state;
        switch (LockKeyInputPolicy.KindFromVirtualKey(vk))
        {
            case LockKeyKind.CapsLock:
                _caps = !_caps;
                state = new LockKeyState(LockKeyKind.CapsLock, _caps);
                break;
            case LockKeyKind.NumLock:
                _num = !_num;
                state = new LockKeyState(LockKeyKind.NumLock, _num);
                break;
            case LockKeyKind.ScrollLock:
                _scroll = !_scroll;
                state = new LockKeyState(LockKeyKind.ScrollLock, _scroll);
                break;
            default:
                return;
        }

        // Off the pump thread: Host Show uses UIThread.Invoke; Stop() Joins this thread.
        ThreadPool.QueueUserWorkItem(_ =>
        {
            try { Changed?.Invoke(this, state); }
            catch (Exception ex) { Debug.WriteLine($"[LockKeys] {ex.Message}"); }
        });
    }

    public void Dispose() => Stop();

    private const int WhKeyboardLl = 13;
    private const uint WmQuit = 0x0012;
    private const IntPtr HwndMessage = -3;

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
    private delegate IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WndClass
    {
        public uint style;
        public IntPtr lpfnWndProc;
        public int cbClsExtra;
        public int cbWndExtra;
        public IntPtr hInstance;
        public IntPtr hIcon;
        public IntPtr hCursor;
        public IntPtr hbrBackground;
        public string? lpszMenuName;
        public string lpszClassName;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Msg
    {
        public IntPtr hwnd;
        public uint message;
        public IntPtr wParam;
        public IntPtr lParam;
        public uint time;
        public int ptX;
        public int ptY;
    }

    [DllImport("user32.dll")]
    private static extern short GetKeyState(int nVirtKey);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern ushort RegisterClass(ref WndClass lpWndClass);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr CreateWindowEx(
        int dwExStyle, string lpClassName, string lpWindowName, int dwStyle,
        int x, int y, int nWidth, int nHeight,
        IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);

    [DllImport("user32.dll")]
    private static extern bool DestroyWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern IntPtr DefWindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool TranslateMessage(ref Msg lpMsg);

    [DllImport("user32.dll")]
    private static extern IntPtr DispatchMessage(ref Msg lpMsg);

    [DllImport("user32.dll")]
    private static extern int GetMessage(out Msg lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

    [DllImport("user32.dll")]
    private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
}

public sealed class WindowsAirplaneModeService : IAirplaneModeService
{
    private System.Threading.Timer? _timer;
    private bool _enabled;

    public bool IsSupported { get; private set; } = true;
    public bool IsEnabled => _enabled;
    public event EventHandler? Changed;

    public void Start()
    {
        Sample(raise: false);
        _timer = new System.Threading.Timer(_ => Sample(raise: true), null, 500, 500);
    }

    public void Stop()
    {
        _timer?.Dispose();
        _timer = null;
    }

    private void Sample(bool raise)
    {
        try
        {
            var on = ReadAirplaneRegistry();
            IsSupported = true;
            if (on != _enabled)
            {
                _enabled = on;
                if (raise) Changed?.Invoke(this, EventArgs.Empty);
            }
        }
        catch
        {
            IsSupported = false;
        }
    }

    private static bool ReadAirplaneRegistry()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                @"SYSTEM\CurrentControlSet\Control\RadioManagement\SystemRadioState");
            if (key?.GetValue(null) is int v)
                return v == 0;
        }
        catch { /* ignore */ }
        return false;
    }

    public void Dispose() => Stop();
}

public sealed class WindowsAudioDeviceService : IAudioDeviceService
{
    private readonly MMDeviceEnumerator _enum = new();

    public IReadOnlyList<AudioOutputDevice> GetOutputDevices()
    {
        var list = new List<AudioOutputDevice>();
        try
        {
            var def = _enum.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            foreach (var d in _enum.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active))
                list.Add(new AudioOutputDevice(d.ID, d.FriendlyName, d.ID == def.ID));
        }
        catch { /* ignore */ }
        return list;
    }

    public void SetDefaultOutput(string deviceId)
    {
        // Default endpoint switching requires undocumented PolicyConfig COM; list-only for MVP polish.
    }

    public void Dispose() => _enum.Dispose();
}

public sealed class NullLockKeysService : ILockKeysService
{
    public LockKeyState Caps => new(LockKeyKind.CapsLock, false);
    public LockKeyState Num => new(LockKeyKind.NumLock, false);
    public LockKeyState Scroll => new(LockKeyKind.ScrollLock, false);
    public event EventHandler<LockKeyState>? Changed { add { } remove { } }
    public void Start() { }
    public void Stop() { }
    public void Dispose() { }
}

public sealed class NullAirplaneModeService : IAirplaneModeService
{
    public bool IsSupported => false;
    public bool IsEnabled => false;
    public event EventHandler? Changed { add { } remove { } }
    public void Start() { }
    public void Stop() { }
    public void Dispose() { }
}

public sealed class NullAudioDeviceService : IAudioDeviceService
{
    public IReadOnlyList<AudioOutputDevice> GetOutputDevices() => Array.Empty<AudioOutputDevice>();
    public void SetDefaultOutput(string deviceId) { }
    public void Dispose() { }
}
