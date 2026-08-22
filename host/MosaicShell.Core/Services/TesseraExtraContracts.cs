namespace MosaicShell.Core.Services;

public enum LockKeyKind
{
    CapsLock,
    NumLock,
    ScrollLock
}

public sealed record LockKeyState(LockKeyKind Key, bool IsOn);

public interface ILockKeysService : IDisposable
{
    LockKeyState Caps { get; }
    LockKeyState Num { get; }
    LockKeyState Scroll { get; }
    event EventHandler<LockKeyState>? Changed;
    void Start();
    void Stop();
}

public interface IAirplaneModeService : IDisposable
{
    bool IsSupported { get; }
    bool IsEnabled { get; }
    event EventHandler? Changed;
    void Start();
    void Stop();
}

public sealed record AudioOutputDevice(string Id, string Name, bool IsDefault);

public interface IAudioDeviceService : IDisposable
{
    IReadOnlyList<AudioOutputDevice> GetOutputDevices();
    void SetDefaultOutput(string deviceId);
}
