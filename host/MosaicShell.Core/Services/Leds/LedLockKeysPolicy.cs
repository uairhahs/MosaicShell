namespace MosaicShell.Core.Services.Leds
{
    /// <summary>
    /// Caps, Num and Scroll Lock from the keyboard LEDs the kernel lists in <c>/sys/class/leds</c>
    /// (<c>input&lt;n&gt;::capslock</c> and so on, readable by every user), for desktops whose compositor offers no lock
    /// state to clients (GNOME). The compositor lights every keyboard's LED for a lock that is on, so a key is on when
    /// any keyboard lights it. Kept pure so it is tested on every OS.
    /// </summary>
    public static class LedLockKeysPolicy
    {
        public const string Directory = "/sys/class/leds";

        /// <summary>How often the LEDs are read: well under a keystroke apart, and a dozen tiny reads each time.</summary>
        public static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(50);

        /// <summary>The lock key a keyboard LED (<c>input17::capslock</c>) shows; null for any other LED.</summary>
        public static LockKeyKind? KeyFor(string ledName)
        {
            int split = ledName.IndexOf("::", StringComparison.Ordinal);
            return split <= 0 || !ledName.StartsWith("input", StringComparison.Ordinal)
                ? null
                : ledName[(split + 2)..] switch
                {
                    "capslock" => LockKeyKind.CapsLock,
                    "numlock" => LockKeyKind.NumLock,
                    "scrolllock" => LockKeyKind.ScrollLock,
                    _ => null,
                };
        }

        /// <summary>Each key's state over every keyboard, and whether any lock LED was there to read.</summary>
        public static (bool Caps, bool Num, bool Scroll, bool Any) Resolve(IEnumerable<(string Name, int Brightness)> leds)
        {
            bool caps = false, num = false, scroll = false, any = false;
            foreach ((string name, int brightness) in leds)
            {
                if (KeyFor(name) is not LockKeyKind key)
                {
                    continue;
                }

                any = true;
                bool on = brightness > 0;
                switch (key)
                {
                    case LockKeyKind.CapsLock:
                        caps |= on;
                        break;
                    case LockKeyKind.NumLock:
                        num |= on;
                        break;
                    case LockKeyKind.ScrollLock:
                        scroll |= on;
                        break;
                }
            }

            return (caps, num, scroll, any);
        }
    }
}
