using System.Diagnostics;
using MosaicShell.Core.Services.Leds;

namespace MosaicShell.Core.Services.Linux
{
    /// <summary>The keyboard LEDs and their brightness; a seam so tests never need a keyboard.</summary>
    public interface ILedReader
    {
        IReadOnlyList<(string Name, int Brightness)> Read();
    }

    /// <summary>Reads <c>/sys/class/leds/*/brightness</c>. Keyboards come and go, so the folder is listed on every read.</summary>
    public sealed class SysClassLedReader : ILedReader
    {
        public IReadOnlyList<(string Name, int Brightness)> Read()
        {
            List<(string, int)> leds = [];
            try
            {
                foreach (string dir in Directory.EnumerateDirectories(LedLockKeysPolicy.Directory))
                {
                    string name = Path.GetFileName(dir);
                    if (LedLockKeysPolicy.KeyFor(name) is null)
                    {
                        continue;
                    }

                    try
                    {
                        if (int.TryParse(File.ReadAllText(Path.Combine(dir, "brightness")).Trim(), out int brightness))
                        {
                            leds.Add((name, brightness));
                        }
                    }
                    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                    {
                        // A keyboard unplugged between the listing and the read.
                    }
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                Debug.WriteLine($"[LedLockKeys] {ex.Message}");
            }

            return leds;
        }
    }

    /// <summary>
    /// Caps, Num and Scroll Lock from the keyboard LEDs (<see cref="LedLockKeysPolicy"/>), for desktops without KWin's
    /// keystate protocol (GNOME). The kernel pushes no change for an LED, so they are read every
    /// <see cref="LedLockKeysPolicy.PollInterval"/> while the service runs. Like the other lock key services it runs while
    /// started or subscribed, takes the first state of each key without raising Changed, and raises Changed once per
    /// edge, on a timer thread (Tessera marshals it to the UI).
    /// </summary>
    /// <remarks>Without any keyboard lock LED it stays inactive and every key reads off.</remarks>
    /// <param name="reader">The LEDs; null reads <c>/sys/class/leds</c>. Tests pass a stand-in.</param>
    /// <param name="pollInterval">Null for <see cref="LedLockKeysPolicy.PollInterval"/>; tests pass an infinite one and call Poll.</param>
    public sealed class LedLockKeysService(ILedReader? reader = null, TimeSpan? pollInterval = null) : ILockKeysService
    {
        private readonly ILedReader _reader = reader ?? new SysClassLedReader();
        private readonly TimeSpan _interval = pollInterval ?? LedLockKeysPolicy.PollInterval;
        private readonly Lock _gate = new();
        private EventHandler<LockKeyState>? _changed;
        private Timer? _timer;
        private int _startRef;
#pragma warning disable IDE0032 // Read and written under the lock; an auto property would not be.
        private bool _caps, _num, _scroll, _active;
#pragma warning restore IDE0032

        public bool IsActive
        {
            get
            {
                lock (_gate)
                {
                    return _timer is not null && _active;
                }
            }
        }

        /// <summary>Whether the LEDs are being read (started or subscribed).</summary>
        public bool IsRunning
        {
            get
            {
                lock (_gate)
                {
                    return _timer is not null;
                }
            }
        }

        public LockKeyState Caps => Read(LockKeyKind.CapsLock);
        public LockKeyState Num => Read(LockKeyKind.NumLock);
        public LockKeyState Scroll => Read(LockKeyKind.ScrollLock);

        public event EventHandler<LockKeyState>? Changed
        {
            add
            {
                lock (_gate)
                {
                    _changed += value;
                    EnsureRunningLocked();
                }
            }
            remove
            {
                Timer? stopping;
                lock (_gate)
                {
                    _changed -= value;
                    stopping = ReleaseLocked();
                }

                stopping?.Dispose();
            }
        }

        public void Start()
        {
            lock (_gate)
            {
                _startRef++;
                EnsureRunningLocked();
            }
        }

        public void Stop()
        {
            Timer? stopping;
            lock (_gate)
            {
                if (_startRef <= 0)
                {
                    return;
                }

                _startRef--;
                stopping = ReleaseLocked();
            }

            stopping?.Dispose();
        }

        public void Dispose()
        {
            Timer? stopping;
            lock (_gate)
            {
                _startRef = 0;
                _changed = null;
                stopping = ReleaseLocked();
            }

            stopping?.Dispose();
        }

        /// <summary>Reads the LEDs once and raises Changed for each key that flipped.</summary>
        internal void Poll()
        {
            (bool caps, bool num, bool scroll, bool any) = LedLockKeysPolicy.Resolve(_reader.Read());
            List<LockKeyState> edges = [];
            EventHandler<LockKeyState>? changed;
            lock (_gate)
            {
                if (_timer is null)
                {
                    return;
                }

                _active = any;
                if (caps != _caps)
                {
                    edges.Add(new LockKeyState(LockKeyKind.CapsLock, caps));
                }

                if (num != _num)
                {
                    edges.Add(new LockKeyState(LockKeyKind.NumLock, num));
                }

                if (scroll != _scroll)
                {
                    edges.Add(new LockKeyState(LockKeyKind.ScrollLock, scroll));
                }

                (_caps, _num, _scroll) = (caps, num, scroll);
                changed = _changed;
            }

            foreach (LockKeyState edge in edges)
            {
                try
                {
                    changed?.Invoke(this, edge);
                }
                catch (Exception ex)
                {
                    // A subscriber's failure must not stop the reads.
                    Debug.WriteLine($"[LedLockKeys] handler: {ex.Message}");
                }
            }
        }

        private void EnsureRunningLocked()
        {
            if (_timer is not null)
            {
                return;
            }

            // The first state is taken as is, without raising.
            (_caps, _num, _scroll, _active) = LedLockKeysPolicy.Resolve(_reader.Read());
            _timer = new Timer(_ => Poll(), null, _interval, _interval);
        }

        private Timer? ReleaseLocked()
        {
            if (_startRef > 0 || _changed is not null || _timer is null)
            {
                return null;
            }

            Timer stopping = _timer;
            _timer = null;
            return stopping;
        }

        private LockKeyState Read(LockKeyKind kind)
        {
            lock (_gate)
            {
                bool on = _timer is not null && kind switch
                {
                    LockKeyKind.CapsLock => _caps,
                    LockKeyKind.NumLock => _num,
                    _ => _scroll,
                };
                return new LockKeyState(kind, on);
            }
        }
    }
}
