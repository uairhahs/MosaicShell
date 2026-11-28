using System.Runtime.InteropServices;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace MosaicShell.Core.Services
{
    /// <summary>
    /// Capability hotkeys through RegisterHotKey on a message-only window. RegisterHotKey and UnregisterHotKey refuse a
    /// window owned by another thread (ERROR_WINDOW_OF_OTHER_THREAD), so every call runs on the window's own thread
    /// (<see cref="WindowsMessageWindow.Run{T}"/>); the tables are touched only there, where WM_HOTKEY also reads them.
    /// </summary>
    public sealed class WindowsHotkeyService : IHotkeyService
    {
        private const uint WmHotkey = 0x0312;
        private readonly Dictionary<string, int> _ids = [with(StringComparer.OrdinalIgnoreCase)];
        private readonly Dictionary<int, Action> _callbacks = [];
        private int _nextId = 1;
        private readonly WindowsMessageWindow _window;

        public WindowsHotkeyService()
        {
            _window = new WindowsMessageWindow("MosaicShell.Hotkeys", (message, wParam, _) =>
            {
                if (message != WmHotkey)
                {
                    return false;
                }

                OnHotkey((int)wParam);
                return true;
            });
        }

        public bool Register(string id, ModifierKeys modifiers, int virtualKey, Action callback)
        {
            return _window.Run(() =>
            {
                UnregisterOnWindowThread(id);
                int hotkeyId = _nextId++;
                // MOD_NOREPEAT = 0x4000 avoids key-repeat floods
                uint fsModifiers = (uint)modifiers | 0x4000u;
                if (!RegisterHotKey(_window.Handle, hotkeyId, fsModifiers, (uint)virtualKey))
                {
                    return false;
                }

                _ids[id] = hotkeyId;
                _callbacks[hotkeyId] = callback;
                return true;
            }, fallback: false);
        }

        public void Unregister(string id)
        {
            _ = _window.Run(() =>
            {
                UnregisterOnWindowThread(id);
                return true;
            }, fallback: false);
        }

        private void UnregisterOnWindowThread(string id)
        {
            if (!_ids.Remove(id, out int hotkeyId))
            {
                return;
            }

            _ = UnregisterHotKey(_window.Handle, hotkeyId);
            _ = _callbacks.Remove(hotkeyId);
        }

        private void OnHotkey(int id)
        {
            if (_callbacks.TryGetValue(id, out Action? cb))
            {
                cb();
            }
        }

        public void Dispose()
        {
            _ = _window.Run(() =>
            {
                foreach (int id in _ids.Values)
                {
                    _ = UnregisterHotKey(_window.Handle, id);
                }

                _ids.Clear();
                _callbacks.Clear();
                return true;
            }, fallback: false);
            _window.Dispose();
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    }

    public sealed class WindowsAudioLevelService : IAudioLevelService
    {
        private WasapiRecorder? _capture;
        private readonly float[] _bands = new float[AudioLevelMeter.BandCount];
        private readonly Lock _gate = new();

        public double Peak
        {
            get
            {
                lock (_gate)
                {
                    return field;
                }
            }

            private set;
        }

        public IReadOnlyList<double> Bands
        {
            get
            {
                lock (_gate)
                {
                    return [.. _bands.Select(b => (double)b)];
                }
            }
        }

        public void Start()
        {
            if (_capture is not null)
            {
                return;
            }

            try
            {
                _capture = new WasapiRecorderBuilder()
                    .WithLoopbackCapture()
                    .Build();
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
            if (_capture is null)
            {
                return;
            }

            try
            {
                _capture.DataAvailable -= OnData;
                _capture.StopRecording();
                _capture.Dispose();
            }
            catch { /* ignore */ }
            _capture = null;
        }

        private void OnData(ReadOnlySpan<byte> buffer, AudioClientBufferFlags flags, long devicePosition, long qpcPosition)
        {
            _ = flags;
            _ = devicePosition;
            _ = qpcPosition;
            if (buffer.Length < 4)
            {
                return;
            }

            Span<float> bands = stackalloc float[AudioLevelMeter.BandCount];
            AudioLevelMeter.ProcessIeeeFloat(buffer, bands, out double peak);

            lock (_gate)
            {
                Peak = peak;
                bands.CopyTo(_bands);
            }
        }

        public void Dispose()
        {
            Stop();
        }
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
                if (File.Exists(ShortcutPath))
                {
                    File.Delete(ShortcutPath);
                }

                return;
            }

            string exe = Environment.ProcessPath ?? Path.Combine(AppContext.BaseDirectory, "MosaicShell.Host.exe");
            Install.StartupShortcutPolicy.WriteHostShortcut(ShortcutPath, exe, trayOnly: false);
        }
    }
}
