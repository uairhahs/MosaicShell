namespace MosaicShell.Core.Services
{
    /// <summary>
    /// Windows lock keys, event-driven: a message-only window sinks keyboard Raw Input (it arrives while any app has
    /// focus, injected presses included) and each keyboard event resamples the Caps, Num and Scroll toggles. It replaced
    /// a 100 ms GetKeyState poll (about 10 wakeups a second while armed), and matches KDE's KWin keystate route: runs
    /// while started or subscribed, first states taken quietly, Changed on each edge. The toggle bit already shows the
    /// new state when the key's WM_INPUT arrives (Windows 11 probe, 2026-11-24). Raw input is not delivered on the secure
    /// desktop, so a return to the session resamples too. Pure, so it is tested on every OS.
    /// </summary>
    public static class LockKeyEventPolicy
    {
        /// <summary>HID generic desktop page.</summary>
        public const ushort RawUsagePage = 0x01;

        /// <summary>HID keyboard usage.</summary>
        public const ushort RawUsageKeyboard = 0x06;

        /// <summary>RIDEV_INPUTSINK: deliver raw input to the target window even when it is not in the foreground.</summary>
        public const uint RidevInputSink = 0x00000100;

        /// <summary>RIM_TYPEKEYBOARD in RAWINPUTHEADER.dwType.</summary>
        public const uint RimTypeKeyboard = 1;

        /// <summary>WM_WTSSESSION_CHANGE events after which the toggles may have changed unseen.</summary>
        public const int WtsConsoleConnect = 0x1;
        public const int WtsRemoteConnect = 0x3;
        public const int WtsSessionUnlock = 0x8;

        /// <summary>
        /// Do not defer Changed to the thread pool; marshaling to the Host UI thread belongs in TesseraCapability.
        /// </summary>
        public const bool MustInvokeChangedSynchronously = true;

        public static bool ShouldResampleOnRawInput(uint rawInputType)
        {
            return rawInputType == RimTypeKeyboard;
        }

        public static bool ShouldResampleOnSessionChange(int wtsEvent)
        {
            return wtsEvent is WtsConsoleConnect or WtsRemoteConnect or WtsSessionUnlock;
        }

        /// <summary>One state per toggle that flipped from <paramref name="was"/> to <paramref name="now"/>: Caps, Num, Scroll.</summary>
        public static IReadOnlyList<LockKeyState> Edges(
            (bool Caps, bool Num, bool Scroll) was,
            (bool Caps, bool Num, bool Scroll) now)
        {
            List<LockKeyState> edges = [];
            if (was.Caps != now.Caps)
            {
                edges.Add(new LockKeyState(LockKeyKind.CapsLock, now.Caps));
            }

            if (was.Num != now.Num)
            {
                edges.Add(new LockKeyState(LockKeyKind.NumLock, now.Num));
            }

            if (was.Scroll != now.Scroll)
            {
                edges.Add(new LockKeyState(LockKeyKind.ScrollLock, now.Scroll));
            }

            return edges;
        }
    }
}
