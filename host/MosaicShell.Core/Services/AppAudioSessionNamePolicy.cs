namespace MosaicShell.Core.Services
{
    /// <summary>
    /// The name a Windows audio session shows in Mixdeck. Most apps never set a session display name, and the system
    /// sounds session sets an indirect resource string (<c>@%SystemRoot%\System32\AudioSrv.Dll,-202</c>), so the raw
    /// value alone gave "Session 3" and a resource path. In order: the display name (an indirect string resolved, and
    /// dropped when it does not resolve), "System sounds" for that session, the process's file description ("Microsoft
    /// Edge"), its process name, then <see cref="UnknownApp"/>. Kept pure so it is tested on every OS.
    /// </summary>
    public static class AppAudioSessionNamePolicy
    {
        public const string SystemSounds = "System sounds";
        public const string UnknownApp = "Unknown app";

        /// <param name="displayName">The session's own display name, possibly an indirect <c>@</c> string.</param>
        /// <param name="isSystemSounds">The session is the system sounds session.</param>
        /// <param name="resolveIndirect">Resolves an <c>@</c> string (SHLoadIndirectString); null when it cannot.</param>
        /// <param name="processDescription">The playing process's file description, when it can be read.</param>
        /// <param name="processName">The playing process's name, when it can be read.</param>
        public static string Resolve(
            string? displayName,
            bool isSystemSounds,
            Func<string, string?> resolveIndirect,
            string? processDescription,
            string? processName)
        {
            string? own = displayName?.Trim();
            if (!string.IsNullOrEmpty(own) && own.StartsWith('@'))
            {
                own = resolveIndirect(own)?.Trim();
            }

            if (!string.IsNullOrEmpty(own))
            {
                return own;
            }

            if (isSystemSounds)
            {
                return SystemSounds;
            }

            string? description = processDescription?.Trim();
            if (!string.IsNullOrEmpty(description))
            {
                return description;
            }

            string? name = processName?.Trim();
            return string.IsNullOrEmpty(name) ? UnknownApp : name;
        }
    }
}
