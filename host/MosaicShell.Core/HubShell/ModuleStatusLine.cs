using MosaicShell.Core.Capabilities.BuiltIn;
using MosaicShell.Core.HostPlatform;
using MosaicShell.Core.Modules;

namespace MosaicShell.Core.HubShell
{
    /// <summary>A module's line on Home and the rail, and whether it marks a problem to fix.</summary>
    public sealed record ModuleStatus(string Line, bool IsProblem);

    /// <summary>
    /// One plain line per module on what it is doing now ("Ctrl+Alt+X opens the mixer"), in place of "Armed - ...".
    /// A feature the desktop lacks is said plainly but is not a problem; a shortcut another app holds is.
    /// </summary>
    public static class ModuleStatusLine
    {
        public static ModuleStatus For(ModuleInfo info, bool isOn, string? hotkeyError, DesktopFeatures features)
        {
            if (!isOn)
            {
                return new ModuleStatus("Off", false);
            }

            if (!string.IsNullOrWhiteSpace(hotkeyError))
            {
                bool noRoute = features.MissingText(DesktopFeature.GlobalHotkeys) == hotkeyError;
                return new ModuleStatus(hotkeyError, IsProblem: !noRoute);
            }

            if (info.Kind == ModuleKind.Widget)
            {
                return new ModuleStatus("On the desktop", false);
            }

            string? opens = ModuleUsageGuide.Opens(info.Id);
            return opens is not null
                ? features.MissingText(DesktopFeature.GlobalHotkeys) is string missing
                    ? new ModuleStatus(missing, false)
                    : new ModuleStatus($"{HotkeyGestureParser.EnsureRegisterable(info.Id, ModuleUsageGuide.CurrentHotkey(info.Id))} opens {opens}", false)
                : info.Id.ToLowerInvariant() switch
                {
                    "tessera" => new ModuleStatus("Replacing the volume, brightness and media pop-ups", false),
                    "slate" => new ModuleStatus($"Shows after {ModuleUsageGuide.IdleText()} without input", false),
                    _ => new ModuleStatus("On", false),
                };
        }

    }
}
