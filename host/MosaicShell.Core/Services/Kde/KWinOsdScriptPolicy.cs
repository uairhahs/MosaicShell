namespace MosaicShell.Core.Services.Kde
{
    /// <summary>
    /// Hides Plasma's volume and brightness pop-ups through KWin, for Plasma versions whose osdService has no
    /// <c>hide</c> (6.6, checked on NixOS 2026-11-23). While Tessera is armed the Host loads <see cref="Script"/> into
    /// KWin and owns <see cref="GuardBusName"/>. The script hides each OSD window as KWin adds it (opacity 0, before
    /// its first frame), then asks the Host whether to keep it hidden: the Host has seen the osdService call that
    /// opened it, so only volume and brightness stay hidden (see <see cref="PlasmaOsdPolicy"/>). Kept pure so it is
    /// tested on every OS.
    /// </summary>
    /// <remarks>
    /// KWin's <c>callDBus</c> never calls back when the target has no owner, so the script asks the bus
    /// (<c>NameHasOwner</c>) first. When the Host is gone, after a crash for instance, the script restores the window
    /// and stops watching, so a stale copy can never keep hiding pop-ups. Nothing is written to the user's settings.
    /// </remarks>
    public static class KWinOsdScriptPolicy
    {
        public const string KWinBusName = "org.kde.KWin";
        public const string ScriptingPath = "/Scripting";
        public const string ScriptingInterface = "org.kde.kwin.Scripting";

        /// <summary>Stable, so the Host can unload a copy a crashed run left loaded.</summary>
        public const string ScriptName = "mosaicshell-osd-guard";

        public const string GuardBusName = "org.mosaicshell.OsdGuard";
        public const string GuardPath = "/org/mosaicshell/OsdGuard";
        public const string GuardInterface = "org.mosaicshell.OsdGuard";

        /// <summary>No arguments; returns whether the OSD window KWin just added should stay hidden.</summary>
        public const string GuardMethod = "ShouldHide";

        /// <summary>How long after a volume or brightness call an added OSD window is taken to be its pop-up.</summary>
        public static TimeSpan RecentCall { get; } = TimeSpan.FromSeconds(1);

        /// <summary>
        /// The Host's answer to <see cref="GuardMethod"/>: hide when the last osdService call was one Tessera replaces
        /// and it was recent. Any other pop-up (keyboard layout, microphone, text) is shown again at once.
        /// </summary>
        public static bool ShouldKeepHidden(string? lastMember, TimeSpan sinceLastCall)
        {
            return lastMember is not null && PlasmaOsdPolicy.ShouldHide(lastMember) && sinceLastCall <= RecentCall;
        }

        public static string Script { get; } = $$"""
            // MosaicShell: hides Plasma's volume and brightness pop-ups while Tessera is armed.
            // Loaded and unloaded by the MosaicShell Host; inert once the Host is gone.
            var guard = "{{GuardBusName}}";

            function onAdded(w) {
                if (!w.onScreenDisplay) {
                    return;
                }

                w.opacity = 0;
                callDBus("org.freedesktop.DBus", "/org/freedesktop/DBus", "org.freedesktop.DBus", "NameHasOwner", guard, function (owned) {
                    if (!owned) {
                        try { w.opacity = 1; } catch (e) { }
                        workspace.windowAdded.disconnect(onAdded);
                        return;
                    }

                    callDBus(guard, "{{GuardPath}}", "{{GuardInterface}}", "{{GuardMethod}}", function (hide) {
                        if (!hide) {
                            try { w.opacity = 1; } catch (e) { }
                        }
                    });
                });
            }

            workspace.windowAdded.connect(onAdded);
            """;
    }
}
