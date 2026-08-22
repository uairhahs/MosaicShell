using MosaicShell.Core.Runtime;
using MosaicShell.Core.Services;
using MosaicShell.Core.Settings;
using MosaicShell.Core.Styles;

namespace MosaicShell.Core.Capabilities;

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
        var payload = BuildPayload(services, settings, kind, extraPayload, lastLock);
        return new FlyoutRequest(
            ModuleId,
            kind,
            settings.Style,
            settings.Position,
            settings.AutoDismissMs,
            payload,
            settings.MonitorIndex,
            settings.XPad,
            settings.YPad,
            settings.Ani,
            settings.AniDir);
    }

    public FlyoutRequest BuildPreview(HostServices services, TesseraSettings? settings = null) =>
        Build(services, settings ?? LoadSettings(), "vol");

    public Dictionary<string, string> BuildPayload(
        HostServices services,
        TesseraSettings settings,
        string kind,
        IReadOnlyDictionary<string, string>? extraPayload = null,
        LockKeyState? lastLock = null)
    {
        var p = extraPayload is null
            ? new Dictionary<string, string>()
            : new Dictionary<string, string>(extraPayload);

        p["volume"] = services.Audio.MasterVolume.ToString("0.###");
        p["muted"] = services.Audio.IsMuted ? "1" : "0";
        p["brightness"] = services.Brightness.IsSupported
            ? services.Brightness.Brightness.ToString("0.###")
            : "0.5";
        p["mediaTitle"] = services.Media.Current?.Title ?? "";
        p["mediaArtist"] = services.Media.Current?.Artist ?? "";
        p["mediaPlaying"] = services.Media.Current?.IsPlaying == true ? "1" : "0";
        p["showMediaStrip"] = settings.ShowMediaStripOnVolume
                              && TesseraLayoutCoverage.UsesStackedMediaStrip(settings.Style)
            ? "1"
            : "0";
        p["acrylic"] = settings.UseAcrylicBackdrop ? "1" : "0";
        p["focusDim"] = settings.UseFocusDim ? "1" : "0";
        p["flyoutScale"] = Math.Clamp(settings.FlyoutScalePercent, 50, 150).ToString();
        p["backdropBlur"] = settings.UseBackdropBlur ? "1" : "0";
        p["bakedFrost"] = settings.UseBackdropBlur ? "1" : "0";

        if (kind.Equals("locks", StringComparison.OrdinalIgnoreCase)
            && !p.ContainsKey("on")
            && lastLock is not null)
        {
            p["lock"] = lastLock.Key.ToString();
            p["on"] = lastLock.IsOn ? "1" : "0";
        }

        if (kind.Equals("flight", StringComparison.OrdinalIgnoreCase) && !p.ContainsKey("on"))
            p["on"] = services.Airplane.IsEnabled ? "1" : "0";

        return p;
    }

    /// <summary>Refresh locks/flight payload from live service state (open flyout pump).</summary>
    public static Dictionary<string, string> RefreshStatusPayload(
        HostServices services,
        string kind,
        IReadOnlyDictionary<string, string>? existing = null)
    {
        var p = existing is null
            ? new Dictionary<string, string>()
            : new Dictionary<string, string>(existing);

        if (kind.Equals("locks", StringComparison.OrdinalIgnoreCase))
        {
            var lockName = p.GetValueOrDefault("lock") ?? LockKeyKind.CapsLock.ToString();
            if (!Enum.TryParse<LockKeyKind>(lockName, out var lk))
                lk = LockKeyKind.CapsLock;

            var on = lk switch
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
        var p = BuildPayload(services, settings, "vol");
        if (showMediaStripOverride is { } on)
            p["showMediaStrip"] = on ? "1" : "0";
        return p;
    }

    public static TesseraSettings LoadSettings() =>
        ModuleSettingsStore.Load(ModuleId, () => new TesseraSettings());

    /// <summary>Read backdrop blur toggle from flyout payload (supports legacy bakedFrost key).</summary>
    public static bool BackdropBlurFromPayload(IReadOnlyDictionary<string, string>? payload)
    {
        if (payload is null)
            return true;
        if (payload.TryGetValue("backdropBlur", out var raw)
            || payload.TryGetValue("bakedFrost", out raw))
        {
            if (string.IsNullOrWhiteSpace(raw))
                return true;
            return raw is not ("0" or "false" or "False" or "off" or "Off");
        }
        return true;
    }
}
