using NAudio.CoreAudioApi;

namespace MosaicShell.Core.Services;

public sealed class WindowsLockKeysService : ILockKeysService
{
    private System.Threading.Timer? _timer;
    private bool _caps, _num, _scroll;

    public LockKeyState Caps => new(LockKeyKind.CapsLock, _caps);
    public LockKeyState Num => new(LockKeyKind.NumLock, _num);
    public LockKeyState Scroll => new(LockKeyKind.ScrollLock, _scroll);
    public event EventHandler<LockKeyState>? Changed;

    public void Start()
    {
        Sample(raise: false);
        _timer = new System.Threading.Timer(_ => Sample(raise: true), null, 80, 80);
    }

    public void Stop()
    {
        _timer?.Dispose();
        _timer = null;
    }

    private void Sample(bool raise)
    {
        var caps = (GetKeyState(0x14) & 1) != 0;
        var num = (GetKeyState(0x90) & 1) != 0;
        var scroll = (GetKeyState(0x91) & 1) != 0;
        if (caps != _caps) { _caps = caps; if (raise) Changed?.Invoke(this, Caps); }
        if (num != _num) { _num = num; if (raise) Changed?.Invoke(this, Num); }
        if (scroll != _scroll) { _scroll = scroll; if (raise) Changed?.Invoke(this, Scroll); }
    }

    public void Dispose() => Stop();

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern short GetKeyState(int nVirtKey);
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
    public event EventHandler<LockKeyState>? Changed;
    public void Start() { }
    public void Stop() { }
    public void Dispose() { }
}

public sealed class NullAirplaneModeService : IAirplaneModeService
{
    public bool IsSupported => false;
    public bool IsEnabled => false;
    public event EventHandler? Changed;
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
