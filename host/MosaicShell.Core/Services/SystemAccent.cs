namespace MosaicShell.Core.Services
{
    /// <summary>
    /// The system accent Tessera's "System" choice uses. Windows answers with its DWM colorization; Linux with the
    /// desktop's portal accent, kept current by one portal watcher (Linux builds only) the Host starts with
    /// <see cref="StartDesktopWatch"/>. Without either, the Windows blue.
    /// </summary>
    public static class SystemAccent
    {
        public static readonly (byte A, byte R, byte G, byte B) Fallback = (255, 2, 115, 205);

#if !WINDOWS
        private static readonly Lock Gate = new();
        private static Linux.PortalAccentWatcher? s_desktop;
#endif

        /// <summary>The desktop's accent, or null before the watch has one or where none was started.</summary>
#if WINDOWS
        public static (byte R, byte G, byte B)? DesktopAccent => null;
#else
        public static (byte R, byte G, byte B)? DesktopAccent
        {
            get
            {
                lock (Gate)
                {
                    return s_desktop?.Current;
                }
            }
        }
#endif

        /// <summary>Starts following the desktop's accent on Linux; does nothing elsewhere or when already started.</summary>
        public static void StartDesktopWatch()
        {
#if !WINDOWS
            if (!OperatingSystem.IsLinux())
            {
                return;
            }

            lock (Gate)
            {
                if (s_desktop is not null)
                {
                    return;
                }

                s_desktop = new Linux.PortalAccentWatcher();
                s_desktop.Start();
            }
#endif
        }

        /// <summary>
        /// The accent from the platform's answers: a DWM colorization (ARGB) when it is neither near black nor near white
        /// (an empty alpha made opaque), else the desktop's accent, else <see cref="Fallback"/>.
        /// </summary>
        public static (byte A, byte R, byte G, byte B) Resolve(uint? dwmColorization, (byte R, byte G, byte B)? desktop)
        {
            if (dwmColorization is uint color)
            {
                byte a = (byte)((color >> 24) & 0xFF);
                byte r = (byte)((color >> 16) & 0xFF);
                byte g = (byte)((color >> 8) & 0xFF);
                byte b = (byte)(color & 0xFF);
                if (r + g + b is > 30 and < 750)
                {
                    return (a == 0 ? (byte)255 : a, r, g, b);
                }
            }

            return desktop is (byte dr, byte dg, byte db) ? ((byte)255, dr, dg, db) : Fallback;
        }
    }
}
