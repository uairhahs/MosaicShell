namespace MosaicShell.Core.Services.Kde
{
    /// <summary>
    /// Capability hotkeys on KDE: each one is an action of the <c>mosaicshell</c> component in KGlobalAccel, bound by Qt
    /// key code with <c>setShortcut</c>. Under Wayland an X11 key grab only sees keys while an X11 window has focus, so
    /// KGlobalAccel is the route that works everywhere on Plasma. Kept pure so it is tested on every OS.
    /// </summary>
    public static class KGlobalAccelHotkeyPolicy
    {
        public const string Path = "/kglobalaccel";
        public const string Interface = "org.kde.KGlobalAccel";
        public const string ComponentUnique = "mosaicshell";
        public const string ComponentFriendly = "MosaicShell";
        public const string ComponentPath = "/component/" + ComponentUnique;

        /// <summary>SetPresent | NoAutoloading: the key the Host asks for is the active one, not a saved one.</summary>
        public const uint SetFlags = 6;

        private const int QtShift = 0x02000000;
        private const int QtControl = 0x04000000;
        private const int QtAlt = 0x08000000;
        private const int QtMeta = 0x10000000;

        /// <summary>componentUnique, actionUnique, componentFriendly, actionFriendly.</summary>
        public static string[] ActionId(string id)
        {
            string friendly = id.StartsWith("cap:", StringComparison.Ordinal) ? "Open " + id[4..] : id;
            return [ComponentUnique, id, ComponentFriendly, friendly];
        }

        /// <summary>The Qt key code for a gesture from <c>HotkeyGestureParser</c>; false for any other key or no modifier.</summary>
        public static bool TryQtKey(ModifierKeys modifiers, int virtualKey, out int qtKey)
        {
            qtKey = 0;
            int key = virtualKey switch
            {
                (>= 'A' and <= 'Z') or (>= '0' and <= '9') or 0x20 => virtualKey,
                0x1B => 0x01000000,
                0x09 => 0x01000001,
                0x0D => 0x01000004,
                >= 0x70 and <= 0x7B => 0x01000030 + (virtualKey - 0x70),
                _ => 0,
            };
            if (key == 0 || modifiers == ModifierKeys.None)
            {
                return false;
            }

            qtKey = key
                | (modifiers.HasFlag(ModifierKeys.Shift) ? QtShift : 0)
                | (modifiers.HasFlag(ModifierKeys.Control) ? QtControl : 0)
                | (modifiers.HasFlag(ModifierKeys.Alt) ? QtAlt : 0)
                | (modifiers.HasFlag(ModifierKeys.Win) ? QtMeta : 0);
            return true;
        }

        /// <summary>Whether <c>setShortcut</c> bound the key: one another component holds comes back as 0.</summary>
        public static bool Took(int requested, IReadOnlyList<int> assigned)
        {
            return assigned.Contains(requested);
        }
    }
}
