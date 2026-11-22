namespace MosaicShell.Core.Services.Kde
{
    /// <summary>
    /// KDE's global shortcut daemon (KGlobalAccel, inside KWin) announces every shortcut press as
    /// <c>org.kde.kglobalaccel.Component.globalShortcutPressed(component, shortcut, timestamp)</c>. Listening to that
    /// signal observes the keys without grabbing them, as SHELLHOOK does on Windows. This decides which presses open
    /// which flyout; kept pure so it is tested on every OS.
    /// </summary>
    /// <remarks>
    /// The set matches <see cref="ShellFlyoutTriggerDecoder"/>: play/pause, stop, next and previous open Media; volume
    /// up, down and mute open Volume; screen brightness opens Brightness. Separate play, pause and seek keys, player
    /// volume, the microphone and the keyboard backlight open nothing, as they open nothing on Windows. The timestamp
    /// argument is not used: Plasma sent 0 for every press.
    /// </remarks>
    public static class KGlobalAccelPolicy
    {
        public const string BusName = "org.kde.kglobalaccel";
        public const string ComponentInterface = "org.kde.kglobalaccel.Component";
        public const string PressedMember = "globalShortcutPressed";

        private static readonly Dictionary<(string Component, string Shortcut), ShellFlyoutKind> Triggers = new()
        {
            [("mediacontrol", "playpausemedia")] = ShellFlyoutKind.Media,
            [("mediacontrol", "stopmedia")] = ShellFlyoutKind.Media,
            [("mediacontrol", "nextmedia")] = ShellFlyoutKind.Media,
            [("mediacontrol", "previousmedia")] = ShellFlyoutKind.Media,

            // The "_small" variants are Shift plus the same key, which Windows reports as the plain volume command.
            [("kmix", "increase_volume")] = ShellFlyoutKind.Volume,
            [("kmix", "decrease_volume")] = ShellFlyoutKind.Volume,
            [("kmix", "mute")] = ShellFlyoutKind.Volume,
            [("kmix", "increase_volume_small")] = ShellFlyoutKind.Volume,
            [("kmix", "decrease_volume_small")] = ShellFlyoutKind.Volume,

            [("org_kde_powerdevil", "Increase Screen Brightness")] = ShellFlyoutKind.Brightness,
            [("org_kde_powerdevil", "Decrease Screen Brightness")] = ShellFlyoutKind.Brightness,
            [("org_kde_powerdevil", "Increase Screen Brightness Small")] = ShellFlyoutKind.Brightness,
            [("org_kde_powerdevil", "Decrease Screen Brightness Small")] = ShellFlyoutKind.Brightness,
        };

        /// <summary>The flyout a pressed shortcut opens. Names are compared exactly, as KGlobalAccel sends them.</summary>
        public static bool TryDecode(string component, string shortcut, out ShellFlyoutKind kind)
        {
            return Triggers.TryGetValue((component, shortcut), out kind);
        }
    }
}
