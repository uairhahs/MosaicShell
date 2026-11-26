using MosaicShell.Core.Capabilities;

namespace MosaicShell.Core.HostPlatform
{
    /// <summary>
    /// The OS "reduce motion" setting (audit U30), read once at start: Windows "Animation effects", GNOME
    /// <c>enable-animations</c>, Plasma's animation speed set to instant. When it is on, flyouts fade instead of sliding,
    /// the Hub's motion previews hold still unless the pointer is over them, and flyout buttons do not sink when pressed
    /// (the pressed fill still shows).
    /// </summary>
    public static class ReducedMotionPolicy
    {
        /// <summary>Shown over Tessera's motion settings while reduced motion is in effect, which they then cannot change.</summary>
        public const string MotionSettingsNotice =
            "Reduce motion is on in Settings and your system's animation effects are off, so pop-ups fade in place. "
            + "Turn it off in Settings to use these.";

        /// <summary>Opacity of the motion settings while reduced motion makes them do nothing.</summary>
        public const double MotionSettingsDimmedOpacity = 0.45;

        public const string GnomeSchema = "org.gnome.desktop.interface";
        public const string GnomeKey = "enable-animations";

        /// <summary>From <c>SPI_GETCLIENTAREAANIMATION</c>: Windows' "Animation effects" switch.</summary>
        public static bool FromWindowsAnimationEffects(bool animationEffectsOn)
        {
            return !animationEffectsOn;
        }

        /// <summary>From <c>gsettings get org.gnome.desktop.interface enable-animations</c>; unreadable means not reduced.</summary>
        public static bool FromGnomeEnableAnimations(string? value)
        {
            return string.Equals(value?.Trim(), "false", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>From the text of <c>kdeglobals</c>: <c>AnimationDurationFactor=0</c> under <c>[KDE]</c> is "Instant".</summary>
        public static bool FromKdeGlobals(string? kdeglobals)
        {
            if (string.IsNullOrEmpty(kdeglobals))
            {
                return false;
            }

            bool inKde = false;
            foreach (string raw in kdeglobals.Split('\n'))
            {
                string line = raw.Trim();
                if (line.StartsWith('['))
                {
                    inKde = line.Equals("[KDE]", StringComparison.Ordinal);
                    continue;
                }

                if (!inKde)
                {
                    continue;
                }

                int eq = line.IndexOf('=');
                if (eq > 0 && line[..eq].Trim() == "AnimationDurationFactor")
                {
                    return double.TryParse(line[(eq + 1)..].Trim(), System.Globalization.NumberStyles.Float,
                               System.Globalization.CultureInfo.InvariantCulture, out double factor)
                           && factor <= 0;
                }
            }

            return false;
        }

        /// <summary>
        /// Whether motion is reduced: only when the user asked MosaicShell to follow the system and the system says so.
        /// The OS setting alone never overrides the motion look chosen in the Hub (it did once, and turned every flyout
        /// into a fade on a machine with Windows animations off for speed).
        /// </summary>
        public static bool IsReduced(bool followSystem, bool systemReduced)
        {
            return followSystem && systemReduced;
        }

        /// <summary>The request as it plays: a plain fade (<c>Ani = 0</c>) when motion is reduced.</summary>
        public static FlyoutRequest ApplyTo(FlyoutRequest request, bool reduced)
        {
            return reduced && request.Ani != 0 ? request with { Ani = 0 } : request;
        }

        /// <summary>Whether a Hub motion preview loops: always, or only while the pointer is over it.</summary>
        public static bool PreviewPlays(bool reduced, bool pointerOver)
        {
            return !reduced || pointerOver;
        }

        /// <summary>Whether a pressed flyout button sinks; the pressed fill shows either way.</summary>
        public static bool PressSinks(bool reduced)
        {
            return !reduced;
        }
    }
}
