using System.Runtime.InteropServices;
using Avalonia.Controls;
using MosaicShell.Core.Capabilities.Platform;

namespace MosaicShell.Host.Capabilities
{
    /// <summary>
    /// Native Translucency on KDE Plasma: writes <c>_KDE_NET_WM_BLUR_BEHIND_REGION</c> on a flyout's X11 window, and
    /// KWin's blur effect blurs the desktop behind those rectangles. Avalonia can set the property too (its Blur
    /// level), but only for the whole window rectangle; the card is rounded, so the Host writes the shape itself, as
    /// it writes the Win32 round region on Windows.
    /// </summary>
    /// <remarks>
    /// Acts only when <see cref="NativeTranslucencyPlatform.Backend"/> is KWinBlur, which the Host sets after asking
    /// KWin. It keeps its own Xlib connection, used from the UI thread. Any failure leaves the window unblurred.
    /// </remarks>
    internal static class X11BlurBehind
    {
        private const string LibX11 = X11Native.LibX11;
        private const string RegionAtomName = "_KDE_NET_WM_BLUR_BEHIND_REGION";
        private const nint XaCardinal = 6;
        private const int PropModeReplace = 0;

        private static readonly Lock Gate = new();
        private static nint _display;
        private static nint _regionAtom;
        private static bool _unavailable;

        private static bool IsActive =>
            !OperatingSystem.IsWindows() && NativeTranslucencyPlatform.Backend == NativeTranslucencyBackend.KWinBlur;

        /// <summary>
        /// Blurs behind the given rectangles (x, y, width, height quads in window pixels, from
        /// <see cref="Core.Services.Kde.KWinBlurRegion"/>). An empty region removes the blur, since KWin reads an empty
        /// property as the whole window.
        /// </summary>
        public static void SetRegion(Window window, uint[] region)
        {
            if (!IsActive)
            {
                return;
            }

            if (region.Length == 0)
            {
                Clear(window);
                return;
            }

            lock (Gate)
            {
                if (!TryResolve(window, out nint xid))
                {
                    return;
                }

                // Format 32 data is passed to Xlib as C longs, which are 64-bit here.
                nint[] data = new nint[region.Length];
                for (int i = 0; i < region.Length; i++)
                {
                    data[i] = (nint)region[i];
                }

                _ = XChangeProperty(_display, xid, _regionAtom, XaCardinal, 32, PropModeReplace, data, data.Length);
                _ = XFlush(_display);
            }
        }

        /// <summary>Removes the blur from a window that stops using Native Translucency.</summary>
        public static void Clear(Window window)
        {
            if (!IsActive)
            {
                return;
            }

            lock (Gate)
            {
                if (!TryResolve(window, out nint xid))
                {
                    return;
                }

                _ = XDeleteProperty(_display, xid, _regionAtom);
                _ = XFlush(_display);
            }
        }

        private static bool TryResolve(Window window, out nint xid)
        {
            xid = 0;
            if (_unavailable)
            {
                return false;
            }

            if (window.TryGetPlatformHandle() is not { } handle
                || !string.Equals(handle.HandleDescriptor, "XID", StringComparison.Ordinal)
                || handle.Handle == 0)
            {
                return false;
            }

            if (_display == 0)
            {
                try
                {
                    _display = XOpenDisplay(0);
                    _regionAtom = _display == 0 ? 0 : XInternAtom(_display, RegionAtomName, onlyIfExists: false);
                }
                catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException)
                {
                    _display = 0;
                }

                if (_display == 0 || _regionAtom == 0)
                {
                    System.Diagnostics.Debug.WriteLine("[X11BlurBehind] no X display; native translucency stays off");
                    _unavailable = true;
                    return false;
                }
            }

            xid = handle.Handle;
            return true;
        }

        [DllImport(LibX11)]
        private static extern nint XOpenDisplay(nint displayName);

        [DllImport(LibX11, CharSet = CharSet.Ansi)]
        private static extern nint XInternAtom(nint display, string atomName, [MarshalAs(UnmanagedType.Bool)] bool onlyIfExists);

        [DllImport(LibX11)]
        private static extern int XChangeProperty(
            nint display, nint window, nint property, nint type, int format, int mode, nint[] data, int elementCount);

        [DllImport(LibX11)]
        private static extern int XDeleteProperty(nint display, nint window, nint property);

        [DllImport(LibX11)]
        private static extern int XFlush(nint display);
    }
}
