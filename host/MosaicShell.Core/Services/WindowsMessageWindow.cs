using System.Runtime.InteropServices;

namespace MosaicShell.Core.Services
{
    /// <summary>
    /// A message-only window on a thread of its own, with its own message loop. Win32 APIs that tie their work to a
    /// window (RegisterHotKey, RegisterRawInputDevices, WTSRegisterSessionNotification) must be called on the window's
    /// thread; <see cref="Run{T}"/> does that from any thread. Every message, posted or sent, reaches the handler given
    /// to the constructor on the window's thread before DefWindowProc; the handler returns true to mark it handled.
    /// </summary>
    internal sealed class WindowsMessageWindow : IDisposable
    {
        /// <summary>A window message on the window's thread; return true when handled (DefWindowProc is then skipped).</summary>
        public delegate bool MessageHandler(uint message, nint wParam, nint lParam);

        private readonly MessageHandler _onMessage;
        private readonly WndProc _wndProc;
        private readonly Thread _thread;
        private volatile bool _running = true;
        private readonly System.Collections.Concurrent.ConcurrentQueue<Action> _queued = new();

        private const uint WmQuit = 0x0012;
        private const uint WmRunQueued = 0x8000 + 1; // WM_APP + 1
        private const int RunTimeoutMs = 5000;
        private static readonly IntPtr HwndMessage = new(-3);

        public IntPtr Handle { get; private set; }

        /// <param name="name">Thread and window name, for debuggers.</param>
        /// <param name="onMessage">Called on the window's thread for each message.</param>
        public WindowsMessageWindow(string name, MessageHandler onMessage)
        {
            _onMessage = onMessage;
            _wndProc = WindowProc;
            using ManualResetEventSlim ready = new(false);
            Exception? startError = null;
            _thread = new Thread(() =>
            {
                try
                {
                    Handle = CreateMessageWindow(name);
                    if (Handle == IntPtr.Zero)
                    {
                        startError = new InvalidOperationException(
                            $"CreateWindowEx failed for {name} HWND_MESSAGE (err={Marshal.GetLastWin32Error()}).");
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
                {
                    return;
                }

                while (_running && GetMessage(out MSG msg, IntPtr.Zero, 0, 0) > 0)
                {
                    if (msg.message == WmRunQueued)
                    {
                        DrainQueued();
                        continue;
                    }

                    _ = TranslateMessage(ref msg);
                    _ = DispatchMessage(ref msg);
                }
            })
            {
                IsBackground = true,
                Name = name
            };
            _thread.SetApartmentState(ApartmentState.STA);
            _thread.Start();
            if (!ready.Wait(5000))
            {
                throw new InvalidOperationException($"Timed out creating the {name} message window.");
            }

            if (Handle == IntPtr.Zero)
            {
                throw new InvalidOperationException($"Failed to create the {name} message window.", startError);
            }
        }

        /// <summary>
        /// Runs <paramref name="work"/> on the window's thread and waits for it: inline when already there, else
        /// queued behind a posted message. <paramref name="fallback"/> when the loop is gone or does not answer.
        /// </summary>
        public T Run<T>(Func<T> work, T fallback)
        {
            if (Handle == IntPtr.Zero || !_running)
            {
                return fallback;
            }

            if (Environment.CurrentManagedThreadId == _thread.ManagedThreadId)
            {
                return work();
            }

            TaskCompletionSource<T> done = new(TaskCreationOptions.RunContinuationsAsynchronously);
            _queued.Enqueue(() =>
            {
                try
                {
                    done.SetResult(work());
                }
                catch (Exception ex)
                {
                    done.SetException(ex);
                }
            });
            return PostMessage(Handle, WmRunQueued, IntPtr.Zero, IntPtr.Zero) && done.Task.Wait(RunTimeoutMs)
                ? done.Task.Result
                : fallback;
        }

        private void DrainQueued()
        {
            while (_queued.TryDequeue(out Action? next))
            {
                next();
            }
        }

        private IntPtr WindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            try
            {
                if (_onMessage(msg, wParam, lParam))
                {
                    return IntPtr.Zero;
                }
            }
            catch (Exception ex)
            {
                // An exception must never unwind through a native window procedure.
                System.Diagnostics.Debug.WriteLine($"[{_thread.Name}] {ex.Message}");
            }

            return DefWindowProc(hWnd, msg, wParam, lParam);
        }

        public void Dispose()
        {
            _running = false;
            if (Handle != IntPtr.Zero)
            {
                _ = PostMessage(Handle, WmQuit, IntPtr.Zero, IntPtr.Zero);
            }
        }

        private IntPtr CreateMessageWindow(string name)
        {
            string className = "MosaicShell." + name + "." + Guid.NewGuid().ToString("N");
            nint hInstance = GetModuleHandle(null);
            WNDCLASS wndClass = new()
            {
                style = 0,
                lpfnWndProc = Marshal.GetFunctionPointerForDelegate(_wndProc),
                cbClsExtra = 0,
                cbWndExtra = 0,
                hInstance = hInstance,
                hIcon = IntPtr.Zero,
                hCursor = IntPtr.Zero,
                hbrBackground = IntPtr.Zero,
                lpszMenuName = null,
                lpszClassName = className
            };

            ushort atom = RegisterClass(ref wndClass);
            if (atom == 0)
            {
                int err = Marshal.GetLastWin32Error();
                if (err != 1410) // ERROR_CLASS_ALREADY_EXISTS
                {
                    return IntPtr.Zero;
                }
            }

            return CreateWindowEx(0, className, name, 0, 0, 0, 0, 0, HwndMessage, IntPtr.Zero, hInstance, IntPtr.Zero);
        }

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
