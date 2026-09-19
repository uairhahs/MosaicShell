using MosaicShell.Core.Capabilities;
using MosaicShell.Core.Runtime;
using MosaicShell.Core.Services;
using MosaicShell.Core.Settings;
using MosaicShell.Core.Styles;

namespace MosaicShell.Core.Modules.Tessera
{
    /// <summary>Single source of truth for Tessera flyout request payloads and layout metadata.</summary>
    public sealed class TesseraFlyoutRequestBuilder
    {
        public const string ModuleId = "Tessera";

        public FlyoutRequest Build(
            HostServices services,
            TesseraSettings settings,
            string kind,
            IReadOnlyDictionary<string, string>? extraPayload = null,
            LockKeyState? lastLock = null)
        {
            Dictionary<string, string> payload = BuildPayload(services, settings, kind, extraPayload, lastLock);
            return new FlyoutRequest(
                ModuleId,
                kind,
                StyleIds.Normalize(settings.Style),
                settings.Position,
                settings.AutoDismissMs,
                payload,
                settings.MonitorIndex,
                settings.XPad,
                settings.YPad,
                settings.Ani,
                settings.AniDir,
                TesseraFlyoutAnimationPolicy.NormalizeEase(settings.AniEase),
                TesseraFlyoutAnimationPolicy.NormalizeAniSteps(settings.AniSteps),
                TesseraFlyoutAnimationPolicy.NormalizeDisplacementPx(settings.AnimationDisplacement));
        }

        public FlyoutRequest BuildPreview(HostServices services, TesseraSettings? settings = null)
        {
            return Build(services, settings ?? LoadSettings(), "vol");
        }

        public Dictionary<string, string> BuildPayload(
            HostServices services,
            TesseraSettings settings,
            string kind,
            IReadOnlyDictionary<string, string>? extraPayload = null,
            LockKeyState? lastLock = null)
        {
            Dictionary<string, string> p = extraPayload is null
                ? []
                : new Dictionary<string, string>(extraPayload);

            p["volume"] = services.Audio.MasterVolume.ToString("0.###");
            p["muted"] = services.Audio.IsMuted ? "1" : "0";
            p["brightness"] = services.Brightness.IsSupported
                ? services.Brightness.Brightness.ToString("0.###")
                : "0.5";

            // Only populate media fields for non-status kinds
            if (!TesseraStatusFlyoutPolicy.IsStatusKind(kind))
            {
                p["mediaTitle"] = services.Media.Current?.Title ?? "";
                p["mediaArtist"] = services.Media.Current?.Artist ?? "";
                p["mediaPlaying"] = services.Media.Current?.IsPlaying == true ? "1" : "0";
            }
            else
            {
                p["mediaTitle"] = "";
                p["mediaArtist"] = "";
                p["mediaPlaying"] = "0";
            }

            // A standalone "media" flyout is always itself a media presentation - it must not
            // depend on ShowMediaStripOnVolume, which is specifically about whether the "vol"
            // flyout also shows media. Reading false here skips the media card's own phase-2
            // reveal entirely (FlyoutMotionSession.Filter finds nothing phase-2-eligible) while
            // phase 1 still runs, which looks like a partial, broken entrance/exit.
            p["showMediaStrip"] = kind.Equals("media", StringComparison.OrdinalIgnoreCase)
                                  || (!TesseraStatusFlyoutPolicy.IsStatusKind(kind)
                                      && settings.ShowMediaStripOnVolume
                                      && TesseraFlyoutTweenTargetCatalog.StyleRequestsVolumeMediaChrome(settings.Style))
                ? "1"
                : "0";
            p["acrylic"] = settings.UseAcrylicBackdrop ? "1" : "0";
            p["focusDim"] = settings.UseFocusDim ? "1" : "0";
            p["flyoutScale"] = Math.Clamp(settings.FlyoutScalePercent, 50, 150).ToString();
            p["backdropBlur"] = settings.UseBackdropBlur ? "1" : "0";
            p["bakedFrost"] = settings.UseBackdropBlur ? "1" : "0";
            p["accent"] = TesseraAccentColor.NormalizeOrEmpty(settings.AccentColor);

            if (kind.Equals("locks", StringComparison.OrdinalIgnoreCase)
                && !p.ContainsKey("on")
                && lastLock is not null)
            {
                p["lock"] = lastLock.Key.ToString();
                p["on"] = lastLock.IsOn ? "1" : "0";
            }

            if (kind.Equals("flight", StringComparison.OrdinalIgnoreCase) && !p.ContainsKey("on"))
            {
                p["on"] = services.Airplane.IsEnabled ? "1" : "0";
            }

            return p;
        }

        /// <summary>Refresh locks/flight payload from live service state (open flyout pump).</summary>
        public static Dictionary<string, string> RefreshStatusPayload(
            HostServices services,
            string kind,
            IReadOnlyDictionary<string, string>? existing = null)
        {
            Dictionary<string, string> p = existing is null
                ? []
                : new Dictionary<string, string>(existing);

            if (kind.Equals("locks", StringComparison.OrdinalIgnoreCase))
            {
                string lockName = p.GetValueOrDefault("lock") ?? LockKeyKind.CapsLock.ToString();
                if (!Enum.TryParse<LockKeyKind>(lockName, out LockKeyKind lk))
                {
                    lk = LockKeyKind.CapsLock;
                }

                bool on = lk switch
                {
                    LockKeyKind.NumLock => services.LockKeys.Num.IsOn,
                    LockKeyKind.ScrollLock => services.LockKeys.Scroll.IsOn,
                    _ => services.LockKeys.Caps.IsOn
                };
                p["lock"] = lk.ToString();
                p["on"] = on ? "1" : "0";
            }
            else if (kind.Equals("flight", StringComparison.OrdinalIgnoreCase))
            {
                p["on"] = services.Airplane.IsEnabled ? "1" : "0";
            }

            return p;
        }

        /// <summary>Live-pump patch keys for an open Tessera flyout.</summary>
        public Dictionary<string, string> BuildLivePayload(
            HostServices services,
            TesseraSettings settings,
            bool? showMediaStripOverride = null)
        {
            Dictionary<string, string> p = BuildPayload(services, settings, "vol");
            if (showMediaStripOverride is { } on)
            {
                p["showMediaStrip"] = on ? "1" : "0";
            }

            return p;
        }

        public static TesseraSettings LoadSettings()
        {
            return ModuleSettingsStore.Load(ModuleId, () => new TesseraSettings());
        }

        /// <summary>Whether volume flyout should include media strip (YourFlyouts MusicVisible).</summary>
        public static bool ShowMediaStripFromPayload(IReadOnlyDictionary<string, string>? payload)
        {
            return payload?.GetValueOrDefault("showMediaStrip") is "1";
        }

        /// <summary>Read backdrop blur toggle from flyout payload (supports legacy bakedFrost key).</summary>
        public static bool BackdropBlurFromPayload(IReadOnlyDictionary<string, string>? payload)
        {
            return payload is null
                || (!payload.TryGetValue("backdropBlur", out string? raw)
                    && !payload.TryGetValue("bakedFrost", out raw))
                || string.IsNullOrWhiteSpace(raw)
                || raw is not ("0" or "false" or "False" or "off" or "Off");
        }

        /// <summary>Flyout UI scale from payload (50..150 percent). Default 1.0.</summary>
        public static double FlyoutScaleFromPayload(IReadOnlyDictionary<string, string>? payload)
        {
            return payload is null || !payload.TryGetValue("flyoutScale", out string? raw)
                ? 1.0
                : !int.TryParse(raw, out int pct) ? 1.0 : Math.Clamp(pct, 50, 150) / 100.0;
        }

        /// <summary>
        /// Custom accent from flyout payload (#RRGGBB). Null/empty = Windows system accent.
        /// Host must pass this into TesseraStyleFactory so live flyouts match config preview.
        /// </summary>
        public static string? AccentFromPayload(IReadOnlyDictionary<string, string>? payload)
        {
            if (payload is null || !payload.TryGetValue("accent", out string? raw))
            {
                return null;
            }

            string normalized = TesseraAccentColor.NormalizeOrEmpty(raw);
            return string.IsNullOrEmpty(normalized) ? null : normalized;
        }
    }
}
