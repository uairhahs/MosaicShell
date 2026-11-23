using System.Runtime.InteropServices;
using Avalonia.Controls;

namespace MosaicShell.Host.Capabilities
{
    /// <summary>
    /// Puts a window under every other one on X11 with the EWMH _NET_WM_STATE_BELOW state, which Avalonia has no way to
    /// send (it sends ABOVE through Topmost). The state is asked of the window manager with a client message to the
    /// root window, so it applies to a mapped window only; a window shown again must ask again, because the window
    /// manager clears the state when the window is hidden.
    /// </summary>
    /// <remarks>Keeps its own Xlib connection, used from the UI thread.</remarks>
    internal static class X11WindowLayer
    {
        private const string LibX11 = "libX11.so.6";
        private const int ClientMessage = 33;
        private const nint StateRemove = 0;
        private const nint StateAdd = 1;
        private const nint SourceApplication = 1;
        private const nint SubstructureNotifyMask = 1 << 19;
        private const nint SubstructureRedirectMask = 1 << 20;

        private static readonly Lock Gate = new();
        private static nint _display;
        private static nint _stateAtom;
        private static nint _belowAtom;
        private static bool _unavailable;

        /// <summary>Whether this process can reach an X display to ask for the state.</summary>
        public static bool HasDisplay()
        {
            if (OperatingSystem.IsWindows())
            {
                return false;
            }

            lock (Gate)
            {
                return TryOpen();
            }
        }

        /// <summary>Adds or removes the below state on <paramref name="window"/>; returns what it did, for the log.</summary>
        public static string SetBelow(Window window, bool below)
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

                XClientMessageEvent message = new()
                {
                    Type = ClientMessage,
                    SendEvent = 1,
                    Display = _display,
                    Window = handle.Handle,
                    MessageType = _stateAtom,
                    Format = 32,
                    L0 = below ? StateAdd : StateRemove,
                    L1 = _belowAtom,
                    L2 = 0,
                    L3 = SourceApplication,
                };
                nint root = XDefaultRootWindow(_display);
                int sent = XSendEvent(_display, root, propagate: 0, SubstructureRedirectMask | SubstructureNotifyMask, ref message);
                _ = XFlush(_display);
                return $"xid=0x{handle.Handle:X} below={below} sent={sent != 0}";
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
                    _stateAtom = XInternAtom(_display, "_NET_WM_STATE", onlyIfExists: false);
                    _belowAtom = XInternAtom(_display, "_NET_WM_STATE_BELOW", onlyIfExists: false);
                }
            }
            catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException)
            {
                _display = 0;
            }

            if (_display == 0 || _stateAtom == 0 || _belowAtom == 0)
            {
                _unavailable = true;
                return false;
            }

            return true;
        }

        /// <summary>Xlib's XClientMessageEvent on LP64, padded to the size of the XEvent union XSendEvent reads.</summary>
        [StructLayout(LayoutKind.Sequential, Size = 192)]
        private struct XClientMessageEvent
        {
            public int Type;
            public nint Serial;
            public int SendEvent;
            public nint Display;
            public nint Window;
            public nint MessageType;
            public int Format;
            public nint L0;
            public nint L1;
            public nint L2;
            public nint L3;
            public nint L4;
        }

        [DllImport(LibX11)]
        private static extern nint XOpenDisplay(nint displayName);

        [DllImport(LibX11, CharSet = CharSet.Ansi)]
        private static extern nint XInternAtom(nint display, string atomName, [MarshalAs(UnmanagedType.Bool)] bool onlyIfExists);

        [DllImport(LibX11)]
        private static extern nint XDefaultRootWindow(nint display);

        [DllImport(LibX11)]
        private static extern int XSendEvent(nint display, nint window, int propagate, nint eventMask, ref XClientMessageEvent sendEvent);

        [DllImport(LibX11)]
        private static extern int XFlush(nint display);
    }
}
