using System.Diagnostics;
using System.Runtime.InteropServices;
using MosaicShell.Core.Services.Rfkill;

namespace MosaicShell.Core.Services.Linux
{
    /// <summary>The kernel's rfkill event queue; a seam so tests never need radios.</summary>
    public interface IRfkillReader : IDisposable
    {
        /// <summary>Takes the next queued event without waiting; false when none is queued.</summary>
        bool TryRead(out RfkillEvent ev);

        /// <summary>Waits until an event is queued (true) or <see cref="Wake"/> is called (false).</summary>
        bool WaitReadable();

        /// <summary>Ends a <see cref="WaitReadable"/>, from any thread.</summary>
        void Wake();
    }

    /// <summary>
    /// Airplane mode on Linux from <c>/dev/rfkill</c> (<see cref="RfkillPolicy"/>). Start takes every radio's state at
    /// once without raising, as the Windows service samples once; after that the kernel pushes each change, so nothing
    /// is sampled, and Changed is raised on the reading thread when airplane mode flips. The last state stays readable
    /// after Stop.
    /// </summary>
    /// <remarks>
    /// Without radios (most desktops) or without <c>/dev/rfkill</c> it is unsupported, so Tessera shows no flight flyout,
    /// and nothing throws.
    /// </remarks>
    /// <param name="open">Opens the event queue; null opens <c>/dev/rfkill</c>. Tests pass a stand-in.</param>
    public sealed class RfkillAirplaneModeService(Func<IRfkillReader>? open = null) : IAirplaneModeService
    {
        private readonly Func<IRfkillReader> _open = open ?? (() => new DevRfkillReader());
        private readonly Lock _gate = new();
        private RfkillRadios _radios = new();
        private IRfkillReader? _reader;
#pragma warning disable IDE0032 // Read and written under the lock; an auto property would not be.
        private bool _supported;
#pragma warning restore IDE0032
        private bool _enabled;

        public event EventHandler? Changed;

        public bool IsSupported
        {
            get
            {
                lock (_gate)
                {
                    return _supported;
                }
            }
        }

        public bool IsEnabled
        {
            get
            {
                lock (_gate)
                {
                    return _enabled;
                }
            }
        }

        public void Start()
        {
            lock (_gate)
            {
                if (_reader is not null)
                {
                    return;
                }

                IRfkillReader reader;
                try
                {
                    reader = _open();
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or DllNotFoundException or EntryPointNotFoundException)
                {
                    Debug.WriteLine($"[Rfkill] unavailable: {ex.Message}");
                    _supported = false;
                    return;
                }

                // The kernel queues an add for every radio on open: that is the current state.
                _radios = new RfkillRadios();
                Drain(reader);
                _supported = _radios.IsSupported;
                _enabled = _radios.IsAirplane;
                _reader = reader;
                new Thread(() => Run(reader)) { IsBackground = true, Name = "rfkill" }.Start();
            }
        }

        public void Stop()
        {
            IRfkillReader? reader;
            lock (_gate)
            {
                reader = _reader;
                _reader = null;
            }

            // The reading thread closes it once woken.
            reader?.Wake();
        }

        public void Dispose()
        {
            Stop();
        }

        private void Run(IRfkillReader reader)
        {
            try
            {
                while (reader.WaitReadable())
                {
                    EventHandler? changed = null;
                    lock (_gate)
                    {
                        if (_reader != reader)
                        {
                            return;
                        }

                        Drain(reader);
                        _supported = _radios.IsSupported;
                        if (_radios.IsAirplane != _enabled)
                        {
                            _enabled = _radios.IsAirplane;
                            changed = Changed;
                        }
                    }

                    try
                    {
                        changed?.Invoke(this, EventArgs.Empty);
                    }
                    catch (Exception ex)
                    {
                        // A subscriber's failure must not end the watch.
                        Debug.WriteLine($"[Rfkill] handler: {ex.Message}");
                    }
                }
            }
            finally
            {
                reader.Dispose();
            }
        }

        private void Drain(IRfkillReader reader)
        {
            while (reader.TryRead(out RfkillEvent ev))
            {
                _radios.Apply(ev);
            }
        }
    }

    /// <summary>
    /// <c>/dev/rfkill</c> opened non-blocking, waited on with poll() beside an eventfd so <see cref="Wake"/> can end the wait:
    /// a blocking read of a character device cannot be cancelled from .NET.
    /// </summary>
    internal sealed unsafe class DevRfkillReader : IRfkillReader
    {
        private const string DevicePath = "/dev/rfkill";
        private const int ORdOnly = 0;
        private const int ONonBlock = 0x800;
        private const int OCloExec = 0x80000;
        private const short PollIn = 0x1;
        private const short PollErr = 0x8;
        private const short PollHup = 0x10;
        private const short PollNval = 0x20;
        private const int EIntr = 4;

        private readonly int _fd;
        private readonly int _wake;
        private int _disposed;

        public DevRfkillReader()
        {
            _fd = open(DevicePath, ORdOnly | ONonBlock | OCloExec);
            if (_fd < 0)
            {
                throw new IOException($"{DevicePath}: errno {Marshal.GetLastPInvokeError()}");
            }

            _wake = eventfd(0, ONonBlock | OCloExec);
            if (_wake < 0)
            {
                int errno = Marshal.GetLastPInvokeError();
                _ = close(_fd);
                throw new IOException($"eventfd: errno {errno}");
            }
        }

        public bool TryRead(out RfkillEvent ev)
        {
            byte* buffer = stackalloc byte[RfkillPolicy.EventSize];
            nint got = read(_fd, buffer, RfkillPolicy.EventSize);
            ev = default;
            return got == RfkillPolicy.EventSize && RfkillPolicy.TryParse(new ReadOnlySpan<byte>(buffer, RfkillPolicy.EventSize), out ev);
        }

        public bool WaitReadable()
        {
            PollFd* fds = stackalloc PollFd[2];
            while (true)
            {
                fds[0] = new PollFd { Fd = _fd, Events = PollIn };
                fds[1] = new PollFd { Fd = _wake, Events = PollIn };
                if (poll(fds, 2, -1) < 0)
                {
                    if (Marshal.GetLastPInvokeError() == EIntr)
                    {
                        continue;
                    }

                    return false;
                }

                if ((fds[1].Revents & PollIn) != 0 || (fds[0].Revents & (PollErr | PollHup | PollNval)) != 0)
                {
                    return false;
                }

                if ((fds[0].Revents & PollIn) != 0)
                {
                    return true;
                }
            }
        }

        public void Wake()
        {
            ulong one = 1;
            _ = write(_wake, &one, sizeof(ulong));
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
            {
                _ = close(_fd);
                _ = close(_wake);
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PollFd
        {
            public int Fd;
            public short Events;
            public short Revents;
        }

        [DllImport("libc", SetLastError = true)]
        private static extern int open([MarshalAs(UnmanagedType.LPUTF8Str)] string path, int flags);

        [DllImport("libc", EntryPoint = "read", SetLastError = true)]
        private static extern nint read(int fd, byte* buffer, nint count);

        [DllImport("libc", SetLastError = true)]
        private static extern nint write(int fd, void* buffer, nint count);

        [DllImport("libc", SetLastError = true)]
        private static extern int poll(PollFd* fds, nuint count, int timeout);

        [DllImport("libc", SetLastError = true)]
        private static extern int eventfd(uint initial, int flags);

        [DllImport("libc", SetLastError = true)]
        private static extern int close(int fd);
    }
}
