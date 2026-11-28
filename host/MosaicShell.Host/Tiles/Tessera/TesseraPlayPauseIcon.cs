using System.Runtime.CompilerServices;
using Avalonia.Threading;
using Material.Icons;
using Material.Icons.Avalonia;
using MosaicShell.Core.Modules.Tessera;

namespace MosaicShell.Host.Tiles.Tessera
{
    /// <summary>
    /// The play/pause icon flips on release, before the player answers (audit U31), and stays flipped against stale
    /// reports for <see cref="OptimisticToggle.HoldMs"/> (<see cref="OptimisticToggle"/>). If the player never answers,
    /// the icon goes back to what the player last said when the hold ends.
    /// </summary>
    internal static class TesseraPlayPauseIcon
    {
        private static readonly ConditionalWeakTable<MaterialIcon, OptimisticToggle> Toggles = [];
        private static readonly ConditionalWeakTable<MaterialIcon, StrongBox<bool>> Reported = [];

        public static void Press(MaterialIcon icon, Func<bool?> playerIsPlaying)
        {
            bool shownPlaying = icon.Kind == MaterialIconKind.Pause;
            OptimisticToggle toggle = new();
            Toggles.AddOrUpdate(icon, toggle);
            Set(icon, toggle.Press(shownPlaying, Environment.TickCount64));
            _ = DispatcherTimer.RunOnce(
                () =>
                {
                    // The hold is over (a timer tick can land a little early, so it is ended here, not measured), unless
                    // a newer press started its own.
                    if (Toggles.TryGetValue(icon, out OptimisticToggle? current) && ReferenceEquals(current, toggle))
                    {
                        _ = Toggles.Remove(icon);
                        Apply(icon, playerIsPlaying() ?? Reported.GetOrCreateValue(icon).Value);
                    }
                },
                TimeSpan.FromMilliseconds(OptimisticToggle.HoldMs));
        }

        /// <summary>What the live patch calls with the player's state, instead of setting the icon itself.</summary>
        public static void Apply(MaterialIcon icon, bool playerIsPlaying)
        {
            Reported.GetOrCreateValue(icon).Value = playerIsPlaying;
            bool show = Toggles.TryGetValue(icon, out OptimisticToggle? toggle)
                ? toggle.Resolve(playerIsPlaying, Environment.TickCount64)
                : playerIsPlaying;
            Set(icon, show);
        }

        private static void Set(MaterialIcon icon, bool playing)
        {
            icon.Kind = playing ? MaterialIconKind.Pause : MaterialIconKind.Play;
        }
    }
}
