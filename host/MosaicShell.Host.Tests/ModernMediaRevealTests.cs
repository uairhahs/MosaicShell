using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using FluentAssertions;
using MosaicShell.Core.Capabilities;
using MosaicShell.Core.Modules;
using MosaicShell.Core.Services;
using MosaicShell.Core.Styles;
using MosaicShell.Host.Tiles.Tessera;

namespace MosaicShell.Host.Tests
{
    /// <summary>
    /// Modern Flyouts media card, desktop report 2026-10-04: "the media card below shifts" while it
    /// reveals. The reveal grows the clip host's MaxHeight with progress, and the stacked card has a
    /// fixed height. Avalonia arranges a stretch-aligned child that is taller than its slot centred
    /// in that slot, so the card started half its height above the top and drifted down as the clip
    /// grew. A reveal uncovers the card from the top; the card itself must not move.
    /// </summary>
    public sealed class ModernMediaRevealTests : HeadlessHostTest
    {
        private const double CardHeight = 190;

        private static (Window Window, TesseraRevealHost Host, Border Card) Build()
        {
            TesseraFlyoutViewModel vm = TesseraFlyoutViewModel.FromRequest(
                HostServicesFakes.Create(),
                new FlyoutRequest(
                    ModuleIds.Tessera,
                    "media",
                    StyleIds.ModernFlyouts,
                    Payload: new Dictionary<string, string> { ["showMediaStrip"] = "1", ["mediaTitle"] = "Song" },
                    Ani: 2));
            Border card = new() { Width = 320, Height = CardHeight };
            TesseraRevealHost host = TesseraRevealHost.WrapMedia(vm, card, fullMediaWidth: 320, fullMediaHeight: CardHeight);
            Window window = new() { Width = 400, Height = 300, Content = host };
            host.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top;
            window.Show();
            HeadlessPump.For(50);
            return (window, host, card);
        }

        [AvaloniaTheory]
        [InlineData(0.1)]
        [InlineData(0.5)]
        [InlineData(0.9)]
        public void The_card_does_not_move_while_the_reveal_uncovers_it(double progress)
        {
            (Window window, TesseraRevealHost host, Border card) = Build();
            host.Phase2Engaged = true;

            double CardTop()
            {
                HeadlessPump.For(50);
                return card.TranslatePoint(default, window)!.Value.Y;
            }

            host.RevealProgress = 1;
            double atRest = CardTop();
            host.RevealProgress = progress;
            double during = CardTop();

            _ = during.Should().BeApproximately(atRest, 0.5, $"at progress {progress} the card must sit where it rests");
            window.Close();
        }
    }

    /// <summary>
    /// Windows 11 grows the same kind of clip host around its media panel (user, 2026-10-04: "apply
    /// the same fix for win11"). The real panel, the real metrics, the real wrap.
    /// </summary>
    public sealed class Win11MediaRevealTests : HeadlessHostTest
    {
        [AvaloniaTheory]
        [InlineData(0.1)]
        [InlineData(0.5)]
        [InlineData(0.9)]
        public void The_media_panel_does_not_move_while_the_reveal_uncovers_it(double progress)
        {
            TesseraFlyoutViewModel vm = TesseraFlyoutViewModel.FromRequest(
                HostServicesFakes.Create(),
                new FlyoutRequest(
                    ModuleIds.Tessera,
                    "media",
                    StyleIds.Windows11,
                    Payload: new Dictionary<string, string> { ["showMediaStrip"] = "1", ["mediaTitle"] = "Song" },
                    Ani: 2));
            Control media = TesseraMediaPanel.Create(vm, TesseraMediaMode.Windows11Below);
            TesseraRevealHost host = TesseraRevealHost.WrapWin11Fancy(
                vm,
                new Border { Height = TesseraWindows11Metrics.VolumeHeight },
                media,
                new Border { Height = 1 },
                TesseraWindows11Metrics.VolumeHeight,
                TesseraWindows11Metrics.MediaHeight,
                TesseraWindows11Metrics.Width,
                TesseraWindows11Metrics.CornerRadius,
                Avalonia.Media.Brushes.Transparent);
            host.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top;
            Window window = new() { Width = 400, Height = 400, Content = host };
            window.Show();
            host.Phase2Engaged = true;

            double MediaTop()
            {
                HeadlessPump.For(50);
                return media.TranslatePoint(default, window)!.Value.Y;
            }

            host.RevealProgress = 1;
            double atRest = MediaTop();
            host.RevealProgress = progress;
            double during = MediaTop();

            _ = during.Should().BeApproximately(atRest, 0.5, $"at progress {progress} the media panel must sit where it rests");
            window.Close();
        }
    }
}
