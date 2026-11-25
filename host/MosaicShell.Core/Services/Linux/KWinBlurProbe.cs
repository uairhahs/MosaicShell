using Tmds.DBus.Protocol;

namespace MosaicShell.Core.Services.Linux
{
    /// <summary>
    /// Asks KWin on the session bus whether its blur effect is loaded (<c>org.kde.KWin /Effects
    /// org.kde.kwin.Effects.isEffectLoaded("blur")</c>). Only then does setting <c>_KDE_NET_WM_BLUR_BEHIND_REGION</c>
    /// on a window draw anything. Every failure (no bus, no KWin, an error reply, no answer in time) reads as false,
    /// so Native Translucency is simply unavailable and the flyouts keep the frost.
    /// </summary>
    public static class KWinBlurProbe
    {
        public const string BusName = Kde.KWinOsdScriptPolicy.KWinBusName;
        public const string EffectsPath = "/Effects";
        public const string EffectsInterface = "org.kde.kwin.Effects";
        public const string IsEffectLoadedMember = "isEffectLoaded";
        public const string BlurEffect = "blur";

        private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(2);

        /// <param name="address">A bus address; null for the user's session bus, empty for none. Tests pass a private bus.</param>
        /// <param name="timeout">How long to wait for KWin; two seconds by default.</param>
        public static async Task<bool> IsBlurLoadedAsync(string? address = null, TimeSpan? timeout = null)
        {
            string? busAddress = address ?? DBusAddress.Session;
            if (string.IsNullOrEmpty(busAddress))
            {
                return false;
            }

            using DBusConnection connection = new(busAddress);
            try
            {
                Task<bool> ask = AskAsync(connection);
                Task finished = await Task.WhenAny(ask, Task.Delay(timeout ?? DefaultTimeout)).ConfigureAwait(false);
                if (finished != ask)
                {
                    // Disposing the connection fails the pending call; observe it so it is not reported as unobserved.
                    _ = ask.ContinueWith(static t => t.Exception, TaskContinuationOptions.OnlyOnFaulted);
                    return false;
                }

                return await ask.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[KWinBlurProbe] {ex.Message}");
                return false;
            }
        }

        private static async Task<bool> AskAsync(DBusConnection connection)
        {
            await connection.ConnectAsync().ConfigureAwait(false);
            return await DBusCalls.CallAsync(
                connection, BusName, EffectsPath, EffectsInterface, IsEffectLoadedMember, "s",
                static (ref w) => w.WriteString(BlurEffect),
                static (m, _) => m.GetBodyReader().ReadBool()).ConfigureAwait(false);
        }
    }
}
