using System.Diagnostics;
using System.Runtime.InteropServices;
using NAudio.CoreAudioApi;

namespace MosaicShell.Core.Services;

public sealed class WindowsLockKeysService : ILockKeysService
{
    private System.Threading.Timer? _timer;
    private IntPtr _hook;
    private LowLevelKeyboardProc? _proc;
    private readonly object _sync = new();
    private bool _caps, _num, _scroll;

    public LockKeyState Caps => new(LockKeyKind.CapsLock, _caps);
    public LockKeyState Num => new(LockKeyKind.NumLock, _num);
    public LockKeyState Scroll => new(LockKeyKind.ScrollLock, _scroll);
    public event EventHandler<LockKeyState>? Changed;

    public void Start()
    {
        lock (_sync)
        {
            Sample(raise: false);
            InstallHook();
            _timer ??= new System.Threading.Timer(_ => Sample(raise: true), null, 500, 500);
        }
    }

    public void Stop()
    {
        lock (_sync)
        {
            _timer?.Dispose();
            _timer = null;
            RemoveHook();
        }
    }

    private void InstallHook()
    {
        if (_hook != IntPtr.Zero) return;
        _proc = HookCallback;
        using var cur = Process.GetCurrentProcess();
        using var mod = cur.MainModule!;
        _hook = SetWindowsHookEx(WhKeyboardLl, _proc, GetModuleHandle(mod.ModuleName!), 0);
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
        if (nCode >= 0)
        {
            var msg = (int)wParam;
            if (msg is WmKeyup or WmSyskeyup)
            {
                var vk = Marshal.ReadInt32(lParam);
                if (vk is VkCapital or VkNumlock or VkScroll)
                    Sample(raise: true);
            }
        }
        return CallNextHookEx(_hook, nCode, wParam, lParam);
    }

    private void Sample(bool raise)
    {
        lock (_sync)
        {
            var caps = (GetKeyState(VkCapital) & 1) != 0;
            var num = (GetKeyState(VkNumlock) & 1) != 0;
            var scroll = (GetKeyState(VkScroll) & 1) != 0;
            if (caps != _caps) { _caps = caps; if (raise) Changed?.Invoke(this, Caps); }
            if (num != _num) { _num = num; if (raise) Changed?.Invoke(this, Num); }
            if (scroll != _scroll) { _scroll = scroll; if (raise) Changed?.Invoke(this, Scroll); }
        }
    }

    public void Dispose() => Stop();

    private const int WhKeyboardLl = 13;
    private const int WmKeyup = 0x0101;
    private const int WmSyskeyup = 0x0105;
    private const int VkCapital = 0x14;
    private const int VkNumlock = 0x90;
    private const int VkScroll = 0x91;

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern short GetKeyState(int nVirtKey);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);
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
    public IReadOnlyList<AudioOutputDevice> GetOutputDevices() => [];
    public void SetDefaultOutput(string deviceId) { }
    public void Dispose() { }
}
