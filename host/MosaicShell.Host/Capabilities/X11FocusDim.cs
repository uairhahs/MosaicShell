using System.Runtime.InteropServices;
using Avalonia.Controls;
using MosaicShell.Core.Modules.Tessera;

namespace MosaicShell.Host.Capabilities
{
    /// <summary>
    /// The X11 half of <see cref="FocusDimRoute.X11ArgbInputShape"/>: the facts that choose the route (an X display
    /// and a running compositor), an empty input shape so every click passes through the dim to the windows under
    /// it, and a WM_HINTS input of false so the window manager never gives the dim keyboard focus. The window's
    /// translucency itself is its Transparent level and background alpha, set by <see cref="FocusDimWindow"/>.
    /// </summary>
    /// <remarks>
    /// Keeps its own Xlib connection, used from the UI thread. Any failure leaves the route at None, so the dim stays
    /// off rather than appearing as an opaque sheet.
    /// </remarks>
    internal static class X11FocusDim
    {
        private const string LibX11 = "libX11.so.6";
        private const string LibXext = "libXext.so.6";
        private const int ShapeInput = 2;
        private const int ShapeSet = 0;
        private const int Unsorted = 0;
        private const nint InputHint = 1;

        private static readonly Lock Gate = new();
        private static nint _display;
        private static nint _compositorAtom;
        private static bool _unavailable;

        /// <summary>The route for this process, asked fresh each time: compositing can be switched off and on.</summary>
        public static FocusDimRoute ResolveRoute()
        {
            if (OperatingSystem.IsWindows())
            {
                return TesseraFocusDimPolicy.ResolveRoute(isWindows: true, isLinux: false, hasX11Display: false, x11Composited: false);
            }

            bool isLinux = OperatingSystem.IsLinux();
            lock (Gate)
            {
                bool hasDisplay = isLinux && TryOpen();
                bool composited = hasDisplay && XGetSelectionOwner(_display, _compositorAtom) != 0;
                return TesseraFocusDimPolicy.ResolveRoute(isWindows: false, isLinux, hasDisplay, composited);
            }
        }

        /// <summary>
        /// Makes the window click-through and unfocusable. Safe to repeat; both survive hide and show because they are
        /// properties of the X window, not of its mapped state.
        /// </summary>
        public static string Apply(Window window, bool clickThrough = true)
        {
            lock (Gate)
            {
                if (!TryOpen())
                {
                    return "no X display";
                }

                if (window.TryGetPlatformHandle() is not { } handle
                    || !string.Equals(handle.HandleDescriptor, "XID", StringComparison.Ordinal)
                    || handle.Handle == 0)
                {
                    return "no XID";
                }

                nint xid = handle.Handle;
                try
                {
                    if (clickThrough)
                    {
                        XShapeCombineRectangles(_display, xid, ShapeInput, 0, 0, 0, 0, ShapeSet, Unsorted);
                    }
                }
                catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException)
                {
                    return "no libXext: dim is not click-through";
                }

                RefuseFocus(xid);
                _ = XFlush(_display);
                return $"xid=0x{xid:X} input-shape=empty input-hint=false";
            }
        }

        private static void RefuseFocus(nint xid)
        {
            nint existing = XGetWMHints(_display, xid);
            nint hints = existing != 0 ? existing : XAllocWMHints();
            if (hints == 0)
            {
                return;
            }

            try
            {
                XWmHints value = Marshal.PtrToStructure<XWmHints>(hints);
                value.Flags |= InputHint;
                value.Input = 0;
                Marshal.StructureToPtr(value, hints, fDeleteOld: false);
                _ = XSetWMHints(_display, xid, hints);
            }
            finally
            {
                _ = XFree(hints);
            }
        }

        private static bool TryOpen()
        {
            if (_unavailable)
            {
                return false;
            }

            if (_display != 0)
            {
                return true;
            }

            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DISPLAY")))
            {
                _unavailable = true;
                return false;
            }

            try
            {
                _display = XOpenDisplay(0);
                if (_display != 0)
                {
                    int screen = XDefaultScreen(_display);
                    _compositorAtom = XInternAtom(_display, $"_NET_WM_CM_S{screen}", onlyIfExists: false);
                }
            }
            catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException)
            {
                _display = 0;
            }

            if (_display == 0 || _compositorAtom == 0)
            {
                System.Diagnostics.Debug.WriteLine("[X11FocusDim] no X display; the focus dim stays off");
                _unavailable = true;
                return false;
            }

            return true;
        }

        /// <summary>Xlib's XWMHints on LP64 (C long and XID are 64-bit, Bool and int are 32-bit).</summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct XWmHints
        {
            public nint Flags;
            public int Input;
            public int InitialState;
            public nint IconPixmap;
            public nint IconWindow;
            public int IconX;
            public int IconY;
            public nint IconMask;
            public nint WindowGroup;
        }

        [DllImport(LibX11)]
        private static extern nint XOpenDisplay(nint displayName);

        [DllImport(LibX11)]
        private static extern int XDefaultScreen(nint display);

        [DllImport(LibX11, CharSet = CharSet.Ansi)]
        private static extern nint XInternAtom(nint display, string atomName, [MarshalAs(UnmanagedType.Bool)] bool onlyIfExists);

        [DllImport(LibX11)]
        private static extern nint XGetSelectionOwner(nint display, nint selection);

        [DllImport(LibX11)]
        private static extern nint XGetWMHints(nint display, nint window);

        [DllImport(LibX11)]
        private static extern nint XAllocWMHints();

        [DllImport(LibX11)]
        private static extern int XSetWMHints(nint display, nint window, nint hints);

        [DllImport(LibX11)]
        private static extern int XFree(nint data);

        [DllImport(LibX11)]
        private static extern int XFlush(nint display);

        [DllImport(LibXext)]
        private static extern void XShapeCombineRectangles(
            nint display, nint window, int destKind, int xOffset, int yOffset, nint rectangles, int count, int op, int ordering);
    }
}
