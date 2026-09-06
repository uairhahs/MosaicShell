using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using NAudio.CoreAudioApi;

namespace MosaicShell.Core.Services
{
    /// <summary>
    /// Caps/Num/Scroll via GetKeyState polling while armed. Does not depend on WH_KEYBOARD_LL
    /// or the Host UI message pump (Avalonia does not reliably deliver LL hook callbacks).
    /// </summary>
    public sealed class WindowsLockKeysService : ILockKeysService
    {
        private readonly Lock _gate = new();
        private Timer? _poll;
        private int _startRef;
        private bool _caps, _num, _scroll;
        private EventHandler<LockKeyState>? _changed;

        public bool IsActive { get; private set; }
        public LockKeyState Caps => new(LockKeyKind.CapsLock, _caps);
        public LockKeyState Num => new(LockKeyKind.NumLock, _num);
        public LockKeyState Scroll => new(LockKeyKind.ScrollLock, _scroll);

        public event EventHandler<LockKeyState>? Changed
        {
            add
            {
                if (value is null)
                {
                    return;
                }

                lock (_gate)
                {
                    _changed += value;
                    EnsurePollLocked();
                }
            }
            remove
            {
                if (value is null)
                {
                    return;
                }

                lock (_gate)
                {
                    _changed -= value;
                    TryReleasePollLocked();
                }
            }
        }

        public void Start()
        {
            lock (_gate)
            {
                if (++_startRef == 1)
                {
                    EnsurePollLocked();
                }
            }
        }

        public void Stop()
        {
            lock (_gate)
            {
                if (_startRef <= 0)
                {
                    return;
                }

                --_startRef;
                TryReleasePollLocked();
            }
        }

        private void SyncFromKeyboard()
        {
            _caps = LockKeyInputPolicy.IsToggleBitOn(GetKeyState(LockKeyInputPolicy.VkCapital));
            _num = LockKeyInputPolicy.IsToggleBitOn(GetKeyState(LockKeyInputPolicy.VkNumlock));
            _scroll = LockKeyInputPolicy.IsToggleBitOn(GetKeyState(LockKeyInputPolicy.VkScroll));
        }

        private void EnsurePollLocked()
        {
            if (IsActive)
            {
                return;
            }

            SyncFromKeyboard();
            int ms = LockKeyPollPolicy.PollIntervalMs;
            _poll = new Timer(PollCallback, null, ms, ms);
            IsActive = true;
            TryLog($"poll ok thread={Environment.CurrentManagedThreadId}");
        }

        private void ReleasePollLocked()
        {
            if (!IsActive)
            {
                return;
            }

            _poll?.Dispose();
            _poll = null;
            IsActive = false;
        }

        private void TryReleasePollLocked()
        {
            if (_startRef > 0 || _changed is not null)
            {
                return;
            }

            ReleasePollLocked();
        }

        private void PollCallback(object? _)
        {
            lock (_gate)
            {
                if (!IsActive)
                {
                    return;
                }

                try
                {
                    SampleToggleLocked(LockKeyKind.CapsLock, LockKeyInputPolicy.VkCapital, ref _caps);
                    SampleToggleLocked(LockKeyKind.NumLock, LockKeyInputPolicy.VkNumlock, ref _num);
                    SampleToggleLocked(LockKeyKind.ScrollLock, LockKeyInputPolicy.VkScroll, ref _scroll);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[LockKeys] poll {ex.Message}");
                }
            }
        }

        private void SampleToggleLocked(LockKeyKind kind, int vk, ref bool field)
        {
            bool on = LockKeyInputPolicy.IsToggleBitOn(GetKeyState(vk));
            if (on == field)
            {
                return;
            }

            field = on;
            LockKeyState state = new(kind, on);
            TryLog($"edge {state.Key} on={state.IsOn} handlers={_changed?.GetInvocationList().Length ?? 0}");
            try { _changed?.Invoke(this, state); }
            catch (Exception ex) { Debug.WriteLine($"[LockKeys] {ex.Message}"); }
        }

        private static void TryLog(string line)
        {
            try
            {
                AppPaths.EnsureLayout();
                string path = Path.Combine(AppPaths.CacheDirectory, "lockkeys.log");
                File.AppendAllText(path, $"{DateTime.Now:HH:mm:ss.fff} {line}{Environment.NewLine}");
            }
            catch { /* soft-fail */ }
        }

        public void Dispose()
        {
            Stop();
        }

        [DllImport("user32.dll")]
        private static extern short GetKeyState(int nVirtKey);
    }

    public sealed class WindowsAirplaneModeService : IAirplaneModeService
    {
        private Timer? _timer;

        public bool IsSupported { get; private set; } = true;
        public bool IsEnabled { get; private set; }
        public event EventHandler? Changed;

        public void Start()
        {
            Sample(raise: false);
            _timer = new Timer(_ => Sample(raise: true), null, 500, 500);
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
                bool on = ReadAirplaneRegistry();
                IsSupported = true;
                if (on != IsEnabled)
                {
                    IsEnabled = on;
                    if (raise)
                    {
                        Changed?.Invoke(this, EventArgs.Empty);
                    }
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
                using RegistryKey? key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                    @"SYSTEM\CurrentControlSet\Control\RadioManagement\SystemRadioState");
                if (key?.GetValue(null) is int v)
                {
                    return v == 0;
                }
            }
            catch { /* ignore */ }
            return false;
        }

        public void Dispose()
        {
            Stop();
        }
    }

    public sealed class WindowsAudioDeviceService : IAudioDeviceService
    {
        private readonly MMDeviceEnumerator _enum = new();

        public IReadOnlyList<AudioOutputDevice> GetOutputDevices()
        {
            List<AudioOutputDevice> list = [];
            try
            {
                MMDevice def = _enum.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
                foreach (MMDevice? d in _enum.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active))
                {
                    list.Add(new AudioOutputDevice(d.ID, d.FriendlyName, d.ID == def.ID));
                }
            }
            catch { /* ignore */ }
            return list;
        }

        public void SetDefaultOutput(string deviceId)
        {
            // Default endpoint switching requires undocumented PolicyConfig COM; list-only for MVP polish.
        }

        public void Dispose()
        {
            _enum.Dispose();
        }
    }

    public sealed class NullLockKeysService : ILockKeysService
    {
        public bool IsActive { get; private set; }
        public LockKeyState Caps => new(LockKeyKind.CapsLock, false);
        public LockKeyState Num => new(LockKeyKind.NumLock, false);
        public LockKeyState Scroll => new(LockKeyKind.ScrollLock, false);
        public event EventHandler<LockKeyState>? Changed { add { } remove { } }
        public void Start()
        {
            IsActive = true;
        }

        public void Stop()
        {
            IsActive = false;
        }

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
        public IReadOnlyList<AudioOutputDevice> GetOutputDevices()
        {
            return [];
        }

        public void SetDefaultOutput(string deviceId) { }
        public void Dispose() { }
    }
}
