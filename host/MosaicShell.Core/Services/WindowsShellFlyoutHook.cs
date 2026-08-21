using System.Runtime.InteropServices;
using System.Threading;

namespace MosaicShell.Core.Services;

/// <summary>
/// Message-only HWND + RegisterShellHookWindow (ModernFlyouts pattern).
/// Soft-fails if Win32 registration fails.
/// </summary>
public sealed class WindowsShellFlyoutHook : IShellFlyoutTriggerSource
{
    private Thread? _thread;
    private volatile bool _running;
    private IntPtr _hwnd;
    private uint _shellHookMsg;
    private WndProc? _wndProc;

    public bool IsActive { get; private set; }
    public event EventHandler<ShellFlyoutKind>? Triggered;

    public void Start()
    {
        if (IsActive) return;
        _running = true;
        _thread = new Thread(MessageLoop)
        {
            IsBackground = true,
            Name = "MosaicShell.ShellFlyoutHook"
        };
        _thread.SetApartmentState(ApartmentState.STA);
        _thread.Start();
        // Brief wait for HWND; soft-fail if never created
        for (var i = 0; i < 50 && _hwnd == IntPtr.Zero && _running; i++)
            Thread.Sleep(10);
        IsActive = _hwnd != IntPtr.Zero;
        if (!IsActive)
            System.Diagnostics.Debug.WriteLine("[ShellFlyoutHook] HWND registration soft-failed");
    }

    public void Stop()
    {
        _running = false;
        IsActive = false;
        if (_hwnd != IntPtr.Zero)
        {
            try { PostMessage(_hwnd, WM_QUIT, IntPtr.Zero, IntPtr.Zero); } catch { /* ignore */ }
        }
        _thread?.Join(1000);
        _thread = null;
        _hwnd = IntPtr.Zero;
    }

    private void MessageLoop()
    {
        try
        {
            _wndProc = WndProcImpl;
            var className = "MosaicShell.ShellFlyoutHook." + Guid.NewGuid().ToString("N");
            var wc = new WNDCLASS
            {
                lpfnWndProc = Marshal.GetFunctionPointerForDelegate(_wndProc),
                hInstance = GetModuleHandle(null),
                lpszClassName = className
            };
            if (RegisterClass(ref wc) == 0 && Marshal.GetLastWin32Error() != 1410) // already exists
            {
                System.Diagnostics.Debug.WriteLine("[ShellFlyoutHook] RegisterClass failed");
                return;
            }

            _hwnd = CreateWindowEx(
                0, className, "MosaicShell ShellFlyoutHook",
                0, 0, 0, 0, 0,
                HWND_MESSAGE, IntPtr.Zero, wc.hInstance, IntPtr.Zero);
            if (_hwnd == IntPtr.Zero)
            {
                System.Diagnostics.Debug.WriteLine("[ShellFlyoutHook] CreateWindowEx failed");
                return;
            }

            _shellHookMsg = RegisterWindowMessage("SHELLHOOK");
            if (!RegisterShellHookWindow(_hwnd))
            {
                System.Diagnostics.Debug.WriteLine("[ShellFlyoutHook] RegisterShellHookWindow failed");
                DestroyWindow(_hwnd);
                _hwnd = IntPtr.Zero;
                return;
            }

            while (_running)
            {
                var gm = GetMessage(out var msg, IntPtr.Zero, 0, 0);
                if (gm <= 0) break;
                TranslateMessage(ref msg);
                DispatchMessage(ref msg);
            }

            try { DeregisterShellHookWindow(_hwnd); } catch { /* ignore */ }
            try { DestroyWindow(_hwnd); } catch { /* ignore */ }
            _hwnd = IntPtr.Zero;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ShellFlyoutHook] {ex.Message}");
            _hwnd = IntPtr.Zero;
        }
    }

    private IntPtr WndProcImpl(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        if (msg == _shellHookMsg && ShellFlyoutTriggerDecoder.TryDecode(wParam, lParam, out var kind))
        {
            try { Triggered?.Invoke(this, kind); } catch { /* soft-fail */ }
            return IntPtr.Zero;
        }
        return DefWindowProc(hWnd, msg, wParam, lParam);
    }

    public void Dispose() => Stop();

    private const uint WM_QUIT = 0x0012;
    private static readonly IntPtr HWND_MESSAGE = new(-3);

    private delegate IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WNDCLASS
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
    private struct MSG
    {
        public IntPtr hwnd;
        public uint message;
        public IntPtr wParam;
        public IntPtr lParam;
        public uint time;
        public int pt_x;
        public int pt_y;
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern ushort RegisterClass(ref WNDCLASS lpWndClass);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr CreateWindowEx(
        int dwExStyle, string lpClassName, string lpWindowName, int dwStyle,
        int x, int y, int nWidth, int nHeight,
        IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);

    [DllImport("user32.dll")]
    private static extern bool DestroyWindow(IntPtr hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern uint RegisterWindowMessage(string lpString);

    [DllImport("user32.dll")]
    private static extern bool RegisterShellHookWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool DeregisterShellHookWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern int GetMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

    [DllImport("user32.dll")]
    private static extern bool TranslateMessage(ref MSG lpMsg);

    [DllImport("user32.dll")]
    private static extern IntPtr DispatchMessage(ref MSG lpMsg);

    [DllImport("user32.dll")]
    private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern IntPtr DefWindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);
}

public sealed class NullShellFlyoutTriggerSource : IShellFlyoutTriggerSource
{
    public bool IsActive => false;
    public event EventHandler<ShellFlyoutKind>? Triggered;
    public void Start() { }
    public void Stop() { }
    public void Dispose() { }
}

/// <summary>Test double — raise Triggered manually.</summary>
public sealed class FakeShellFlyoutTriggerSource : IShellFlyoutTriggerSource
{
    public bool IsActive { get; private set; }
    public event EventHandler<ShellFlyoutKind>? Triggered;
    public void Start() => IsActive = true;
    public void Stop() => IsActive = false;
    public void Dispose() => Stop();
    public void Raise(ShellFlyoutKind kind) => Triggered?.Invoke(this, kind);
}
