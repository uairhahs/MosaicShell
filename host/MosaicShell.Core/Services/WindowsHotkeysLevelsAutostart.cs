using System.Runtime.InteropServices;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace MosaicShell.Core.Services;

public sealed class WindowsHotkeyService : IHotkeyService
{
    private readonly Dictionary<string, int> _ids = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<int, Action> _callbacks = new();
    private int _nextId = 1;
    private readonly HotkeyWindow _window;

    public WindowsHotkeyService()
    {
        _window = new HotkeyWindow(OnHotkey);
    }

    public bool Register(string id, ModifierKeys modifiers, int virtualKey, Action callback)
    {
        Unregister(id);
        if (_window.Handle == IntPtr.Zero)
            return false;

        var hotkeyId = _nextId++;
        // MOD_NOREPEAT = 0x4000 avoids key-repeat floods
        var fsModifiers = (uint)modifiers | 0x4000u;
        if (!RegisterHotKey(_window.Handle, hotkeyId, fsModifiers, (uint)virtualKey))
            return false;

        _ids[id] = hotkeyId;
        _callbacks[hotkeyId] = callback;
        return true;
    }

    public void Unregister(string id)
    {
        if (!_ids.Remove(id, out var hotkeyId)) return;
        UnregisterHotKey(_window.Handle, hotkeyId);
        _callbacks.Remove(hotkeyId);
    }

    private void OnHotkey(int id)
    {
        if (_callbacks.TryGetValue(id, out var cb))
            cb();
    }

    public void Dispose()
    {
        foreach (var id in _ids.Values.ToList())
            UnregisterHotKey(_window.Handle, id);
        _ids.Clear();
        _callbacks.Clear();
        _window.Dispose();
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private sealed class HotkeyWindow : IDisposable
    {
        private readonly Action<int> _onHotkey;
        private readonly Thread _thread;
        private volatile bool _running = true;
        public IntPtr Handle { get; private set; }

        public HotkeyWindow(Action<int> onHotkey)
        {
            _onHotkey = onHotkey;
            using var ready = new ManualResetEventSlim(false);
            Exception? startError = null;
            _thread = new Thread(() =>
            {
                try
                {
                    Handle = CreateMessageWindow();
                    if (Handle == IntPtr.Zero)
                    {
                        startError = new InvalidOperationException(
                            "CreateWindowEx failed for hotkey HWND_MESSAGE (err=" +
                            Marshal.GetLastWin32Error() + ").");
                    }
                }
                catch (Exception ex)
                {
                    startError = ex;
                }
                finally
                {
                    ready.Set();
                }

                if (Handle == IntPtr.Zero)
                    return;

                while (_running && GetMessage(out var msg, IntPtr.Zero, 0, 0) > 0)
                {
                    if (msg.message == 0x0312) // WM_HOTKEY
                        _onHotkey((int)msg.wParam);
                    TranslateMessage(ref msg);
                    DispatchMessage(ref msg);
                }
            })
            {
                IsBackground = true,
                Name = "MosaicShell.Hotkeys"
            };
            _thread.SetApartmentState(ApartmentState.STA);
            _thread.Start();
            if (!ready.Wait(5000))
                throw new InvalidOperationException("Timed out creating hotkey message window.");
            if (Handle == IntPtr.Zero)
                throw new InvalidOperationException("Failed to create hotkey message window.", startError);
        }

        public void Dispose()
        {
            _running = false;
            if (Handle != IntPtr.Zero)
                PostMessage(Handle, 0x0012, IntPtr.Zero, IntPtr.Zero); // WM_QUIT
        }

        private static IntPtr CreateMessageWindow()
        {
            var className = "MosaicShellHotkeyWnd." + Guid.NewGuid().ToString("N");
            var hInstance = GetModuleHandle(null);
            var wndClass = new WNDCLASS
            {
                style = 0,
                lpfnWndProc = Marshal.GetFunctionPointerForDelegate(s_wndProc),
                cbClsExtra = 0,
                cbWndExtra = 0,
                hInstance = hInstance,
                hIcon = IntPtr.Zero,
                hCursor = IntPtr.Zero,
                hbrBackground = IntPtr.Zero,
                lpszMenuName = null,
                lpszClassName = className
            };

            var atom = RegisterClass(ref wndClass);
            if (atom == 0)
            {
                var err = Marshal.GetLastWin32Error();
                if (err != 1410) // ERROR_CLASS_ALREADY_EXISTS
                    return IntPtr.Zero;
            }

            return CreateWindowEx(
                0,
                className,
                "MosaicShellHotkeys",
                0,
                0, 0, 0, 0,
                HWND_MESSAGE,
                IntPtr.Zero,
                hInstance,
                IntPtr.Zero);
        }

        private static readonly WndProc s_wndProc = (hWnd, msg, w, l) => DefWindowProc(hWnd, msg, w, l);
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

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr GetModuleHandle(string? lpModuleName);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern ushort RegisterClass(ref WNDCLASS lpWndClass);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr CreateWindowEx(
            int dwExStyle,
            string lpClassName,
            string lpWindowName,
            int dwStyle,
            int x, int y, int nWidth, int nHeight,
            IntPtr hWndParent,
            IntPtr hMenu,
            IntPtr hInstance,
            IntPtr lpParam);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr DefWindowProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern int GetMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

        [DllImport("user32.dll")]
        private static extern bool TranslateMessage(ref MSG lpMsg);

        [DllImport("user32.dll")]
        private static extern IntPtr DispatchMessage(ref MSG lpMsg);

        [DllImport("user32.dll")]
        private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
    }
}

public sealed class WindowsAudioLevelService : IAudioLevelService
{
    private WasapiLoopbackCapture? _capture;
    private readonly float[] _bands = new float[16];
    private double _peak;
    private readonly object _gate = new();

    public double Peak
    {
        get { lock (_gate) return _peak; }
    }

    public IReadOnlyList<double> Bands
    {
        get
        {
            lock (_gate)
                return _bands.Select(b => (double)b).ToArray();
        }
    }

    public void Start()
    {
        if (_capture is not null) return;
        try
        {
            _capture = new WasapiLoopbackCapture();
            _capture.DataAvailable += OnData;
            _capture.StartRecording();
        }
        catch
        {
            _capture = null;
        }
    }

    public void Stop()
    {
        if (_capture is null) return;
        try
        {
            _capture.DataAvailable -= OnData;
            _capture.StopRecording();
            _capture.Dispose();
        }
        catch { /* ignore */ }
        _capture = null;
    }

    private void OnData(object? sender, WaveInEventArgs e)
    {
        if (e.BytesRecorded < 4) return;
        var samples = e.BytesRecorded / 4;
        double sum = 0;
        var bandAcc = new double[16];
        var bandCount = new int[16];
        for (var i = 0; i < samples; i++)
        {
            var sample = BitConverter.ToSingle(e.Buffer, i * 4);
            var a = Math.Abs(sample);
            sum += a;
            var band = Math.Clamp(i * 16 / Math.Max(1, samples), 0, 15);
            bandAcc[band] += a;
            bandCount[band]++;
        }

        lock (_gate)
        {
            _peak = Math.Clamp(sum / samples * 4, 0, 1);
            for (var b = 0; b < 16; b++)
                _bands[b] = (float)Math.Clamp(bandCount[b] == 0 ? 0 : bandAcc[b] / bandCount[b] * 6, 0, 1);
        }
    }

    public void Dispose() => Stop();
}

public sealed class WindowsAutostartService : IAutostartService
{
    private static string ShortcutPath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Startup),
            "MosaicShell.Host.url");

    public bool IsEnabled => File.Exists(ShortcutPath);

    public void SetEnabled(bool enabled)
    {
        if (!enabled)
        {
            if (File.Exists(ShortcutPath)) File.Delete(ShortcutPath);
            return;
        }

        var exe = Environment.ProcessPath ?? Path.Combine(AppContext.BaseDirectory, "MosaicShell.Host.exe");
        var contents = $"[InternetShortcut]\r\nURL=file:///{exe.Replace('\\', '/')}\r\nIconIndex=0\r\n";
        File.WriteAllText(ShortcutPath, contents);
    }
}
