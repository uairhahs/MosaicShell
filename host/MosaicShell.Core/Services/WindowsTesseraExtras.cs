using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using NAudio.CoreAudioApi;

namespace MosaicShell.Core.Services
{
    /// <summary>
    /// Caps, Num and Scroll Lock from keyboard Raw Input (<see cref="LockKeyEventPolicy"/>): a message-only window on its
    /// own thread sinks every keyboard event and resamples the toggles with GetKeyState, so nothing runs between key
    /// presses. A return to the session (unlock, connect) resamples too, since the secure desktop sends no raw input.
    /// Runs while started or subscribed; the first states are taken quietly; Changed is raised on the listener thread.
    /// </summary>
    public sealed class WindowsLockKeysService : ILockKeysService
    {
        private const uint WmInput = 0x00FF;
        private const uint WmWtsSessionChange = 0x02B1;
        private const uint RidHeader = 0x10000005;
        private const uint RidevRemove = 0x00000001;
        private const int NotifyForThisSession = 0;

        private readonly Lock _gate = new();
        private readonly Lock _stateGate = new();
        private WindowsMessageWindow? _listener;
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
                    EnsureListeningLocked();
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
                    TryReleaseLocked();
                }
            }
        }

        public void Start()
        {
            lock (_gate)
            {
                if (++_startRef == 1)
                {
                    EnsureListeningLocked();
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
                TryReleaseLocked();
            }
        }

        private static (bool Caps, bool Num, bool Scroll) ReadToggles()
        {
            return (
                LockKeyInputPolicy.IsToggleBitOn(GetKeyState(LockKeyInputPolicy.VkCapital)),
                LockKeyInputPolicy.IsToggleBitOn(GetKeyState(LockKeyInputPolicy.VkNumlock)),
                LockKeyInputPolicy.IsToggleBitOn(GetKeyState(LockKeyInputPolicy.VkScroll)));
        }

        // _gate guards the lifecycle only; the listener thread never takes it, so waiting on Run under it is safe.
        private void EnsureListeningLocked()
        {
            if (IsActive)
            {
                return;
            }

            lock (_stateGate)
            {
                (_caps, _num, _scroll) = ReadToggles();
            }

            WindowsMessageWindow listener = new("MosaicShell.LockKeys", OnMessage);
            // The error is read on the listener thread, where the call failed.
            int rawError = listener.Run(
                () => RegisterKeyboard(listener.Handle, LockKeyEventPolicy.RidevInputSink) ? 0 : Marshal.GetLastPInvokeError(),
                fallback: -1);
            bool session = listener.Run(
                () => WTSRegisterSessionNotification(listener.Handle, NotifyForThisSession),
                fallback: false);
            _listener = listener;
            IsActive = true;
            TryLog($"listening raw={rawError == 0} session={session} thread={Environment.CurrentManagedThreadId}"
                + (rawError == 0 ? "" : $" err={rawError}"));
        }

        private void ReleaseLocked()
        {
            if (!IsActive)
            {
                return;
            }

            WindowsMessageWindow? listener = _listener;
            _listener = null;
            IsActive = false;
            if (listener is null)
            {
                return;
            }

            _ = listener.Run(() =>
            {
                _ = RegisterKeyboard(IntPtr.Zero, RidevRemove);
                _ = WTSUnRegisterSessionNotification(listener.Handle);
                return true;
            }, fallback: false);
            listener.Dispose();
        }

        private void TryReleaseLocked()
        {
            if (_startRef > 0 || _changed is not null)
            {
                return;
            }

            ReleaseLocked();
        }

        /// <summary>On the listener thread: resample on keyboard input and on a return to the session.</summary>
        private bool OnMessage(uint message, nint wParam, nint lParam)
        {
            if (message == WmInput)
            {
                if (LockKeyEventPolicy.ShouldResampleOnRawInput(RawInputType(lParam)))
                {
                    Resample();
                }

                // DefWindowProc still runs for WM_INPUT, which frees the input's buffer.
                return false;
            }

            if (message == WmWtsSessionChange && LockKeyEventPolicy.ShouldResampleOnSessionChange((int)wParam))
            {
                Resample();
            }

            return false;
        }

        private void Resample()
        {
            IReadOnlyList<LockKeyState> edges;
            lock (_stateGate)
            {
                (bool Caps, bool Num, bool Scroll) now = ReadToggles();
                edges = LockKeyEventPolicy.Edges((_caps, _num, _scroll), now);
                (_caps, _num, _scroll) = now;
            }

            foreach (LockKeyState state in edges)
            {
                EventHandler<LockKeyState>? handlers = _changed;
                TryLog($"edge {state.Key} on={state.IsOn} handlers={handlers?.GetInvocationList().Length ?? 0}");
                try { handlers?.Invoke(this, state); }
                catch (Exception ex) { Debug.WriteLine($"[LockKeys] {ex.Message}"); }
            }
        }

        private static uint RawInputType(nint rawInput)
        {
            uint size = (uint)Marshal.SizeOf<RawInputHeader>();
            return GetRawInputData(rawInput, RidHeader, out RawInputHeader header, ref size, size) == size
                ? header.Type
                : uint.MaxValue;
        }

        private static bool RegisterKeyboard(nint target, uint flags)
        {
            RawInputDevice[] devices =
            [
                new RawInputDevice
                {
                    UsagePage = LockKeyEventPolicy.RawUsagePage,
                    Usage = LockKeyEventPolicy.RawUsageKeyboard,
                    Flags = flags,
                    Target = target,
                },
            ];
            return RegisterRawInputDevices(devices, (uint)devices.Length, (uint)Marshal.SizeOf<RawInputDevice>());
        }

        private static void TryLog(string line)
        {
            DiagnosticLog.Append("lockkeys.log", line);
        }

        public void Dispose()
        {
            Stop();
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RawInputDevice
        {
            public ushort UsagePage;
            public ushort Usage;
            public uint Flags;
            public nint Target;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RawInputHeader
        {
            public uint Type;
            public uint Size;
            public nint Device;
            public nint WParam;
        }

        [DllImport("user32.dll")]
        private static extern short GetKeyState(int nVirtKey);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterRawInputDevices(RawInputDevice[] devices, uint count, uint size);

        [DllImport("user32.dll")]
        private static extern uint GetRawInputData(nint rawInput, uint command, out RawInputHeader data, ref uint size, uint headerSize);

        [DllImport("wtsapi32.dll", SetLastError = true)]
        private static extern bool WTSRegisterSessionNotification(nint hWnd, int flags);

        [DllImport("wtsapi32.dll")]
        private static extern bool WTSUnRegisterSessionNotification(nint hWnd);
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
}
