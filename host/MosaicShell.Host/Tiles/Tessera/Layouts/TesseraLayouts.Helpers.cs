using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Material.Icons;
using Material.Icons.Avalonia;
using MosaicShell.Core.Modules.Tessera;

namespace MosaicShell.Host.Tiles.Tessera
{
    internal static partial class TesseraLayouts
    {

        private static Control StatusChip(TesseraFlyoutViewModel vm, double radius, double? w = null, double? h = null)
        {
            // Never pin status to volume Width×Height (black square AcrylicBlur HWND around the pill).
            if (TesseraStatusFlyoutPolicy.ForbidFixedVolumeShellSize)
            {
                w = null;
                h = TesseraStatusFlyoutPolicy.ChipHeightDip;
            }

            TextBlock label = TesseraChrome.Label(vm.KindLabel, 14);
            label.Name = "TesseraStatusLabel";
            TesseraLiveAmbient.RegisterStatus(label);
            StackPanel body = new()
            {
                Orientation = Orientation.Horizontal,
                Spacing = 12,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(16, 0),
                Children =
                {
                    TesseraVolumeGlyph.Create(vm, 16),
                    label
                }
            };

            // OS acrylic: HWND is the material; nested Skia Glass left a square black plate on first paint.
            return TesseraStatusFlyoutPolicy.PreferOsAcrylicEdgeOnlyChrome && TesseraGlass.UseOsAcrylicChrome
                ? new Border
                {
                    Background = Brushes.Transparent,
                    CornerRadius = new CornerRadius(radius),
                    Padding = new Thickness(12, 10),
                    Height = h ?? TesseraStatusFlyoutPolicy.ChipHeightDip,
                    ClipToBounds = true,
                    Child = body
                }
                : TesseraChrome.Glass(body, radius, new Thickness(12, 10), w, h ?? TesseraStatusFlyoutPolicy.ChipHeightDip);
        }

        private static Control CoreUiIconBtn(MaterialIconKind kind, Action act, double size)
        {
            MaterialIcon icon = new()
            {
                Kind = kind,
                Width = size * 0.55,
                Height = size * 0.55,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = TesseraPalette.FontBrush
            };
            return TesseraChrome.IconButton(icon, act, size, circularHighlight: false,
                hover: TesseraStylePalette.CoreUi.IconHoverBrush);
        }

        private static MaterialIcon MaterialYouIcon(MaterialIconKind kind, bool muted = false)
        {
            return new()
            {
                Kind = kind,
                Width = TesseraStyleMetrics.MaterialYouIconSize,
                Height = TesseraStyleMetrics.MaterialYouIconSize,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = muted
                    ? TesseraStylePalette.MaterialYou.SecondaryBrush
                    : TesseraStylePalette.MaterialYou.AccentBrush
            };
        }

        private static Border MaterialYouIconTile(MaterialIconKind kind, double iconSize, double tile)
        {
            Border tileBorder = new()
            {
                Width = tile,
                Height = tile,
                CornerRadius = new CornerRadius(tile / 2),
                Background = TesseraStylePalette.MaterialYou.ShellBrush,
                Child = new MaterialIcon
                {
                    Kind = kind,
                    Width = iconSize,
                    Height = iconSize,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Foreground = TesseraStylePalette.MaterialYou.AccentBrush
                }
            };
            TesseraChrome.ApplyHoverHighlight(tileBorder, TesseraStylePalette.MaterialYou.ShellBrush,
                new SolidColorBrush(Color.FromRgb(40, 40, 44)));
            return tileBorder;
        }

        private static Control MaterialYouIconBtn(MaterialIconKind kind, Action act)
        {
            return TesseraChrome.IconButton(MaterialYouIcon(kind), act, TesseraStyleMetrics.MaterialYouHitTarget, circularHighlight: true);
        }

        private static Control MaterialYouIconBtn(MaterialIcon icon, Action act)
        {
            return TesseraChrome.IconButton(icon, act, TesseraStyleMetrics.MaterialYouHitTarget, circularHighlight: true);
        }

        private static Control MaterialYouPlayPill(TesseraFlyoutViewModel vm)
        {
            MaterialIcon icon = new()
            {
                Kind = vm.IsPlaying ? MaterialIconKind.Pause : MaterialIconKind.Play,
                Width = TesseraStyleMetrics.MaterialYouIconSize,
                Height = TesseraStyleMetrics.MaterialYouIconSize,
                Foreground = TesseraStylePalette.MaterialYou.OnAccentBrush,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            if (TesseraLiveAmbient.Current is { } live)
            {
                live.PlayPauseIcon = icon;
            }

            IBrush normal = TesseraStylePalette.MaterialYou.AccentBrush;
            SolidColorBrush hover = new(Color.FromRgb(240, 244, 255));
            Border pill = new()
            {
                Width = TesseraStyleMetrics.MaterialYouPlayW,
                Height = TesseraStyleMetrics.MaterialYouPlayH,
                CornerRadius = new CornerRadius(12),
                Background = normal,
                ClipToBounds = true,
                Child = icon
            };
            TesseraChrome.ApplyHoverHighlight(pill, normal, hover);
            pill.PointerPressed += (_, e) => { _ = vm.PlayPauseAsync(); e.Handled = true; };
            return pill;
        }

        private static async Task MaterialYouToggleShuffleAsync(TesseraFlyoutViewModel vm, MaterialIcon icon)
        {
            await vm.Services.Media.ToggleShuffleAsync();
            bool on = icon.Kind != MaterialIconKind.ShuffleVariant;
            icon.Kind = on ? MaterialIconKind.ShuffleVariant : MaterialIconKind.Shuffle;
            icon.Foreground = on
                ? TesseraStylePalette.MaterialYou.AccentBrush
                : TesseraStylePalette.MaterialYou.SecondaryBrush;
        }

        private static async Task MaterialYouToggleRepeatAsync(TesseraFlyoutViewModel vm, MaterialIcon icon)
        {
            await vm.Services.Media.ToggleRepeatAsync();
            if (icon.Kind == MaterialIconKind.RepeatOne)
            {
                icon.Kind = MaterialIconKind.Repeat;
                icon.Foreground = TesseraStylePalette.MaterialYou.SecondaryBrush;
            }
            else if (icon.Kind == MaterialIconKind.Repeat)
            {
                icon.Kind = MaterialIconKind.RepeatOne;
                icon.Foreground = TesseraStylePalette.MaterialYou.AccentBrush;
            }
            else
            {
                icon.Kind = MaterialIconKind.Repeat;
                icon.Foreground = TesseraStylePalette.MaterialYou.AccentBrush;
            }
        }

        private static async Task MaterialYouToggleLikeAsync(TesseraFlyoutViewModel vm, MaterialIcon icon)
        {
            await vm.ToggleLikeAsync(icon);
        }

        private static async Task MaterialYouToggleDislikeAsync(TesseraFlyoutViewModel vm, MaterialIcon icon)
        {
            await vm.ToggleDislikeAsync(icon);
        }

        private static void BindWheel(Control c, TesseraFlyoutViewModel vm, double step = 0.02)
        {
            c.PointerWheelChanged += (_, e) =>
            {
                vm.Nudge(e.Delta.Y > 0 ? step : -step);
                e.Handled = true;
            };
        }

        private static bool IsStatus(TesseraFlyoutViewModel vm)
        {
            return vm.Kind.Equals("locks", StringComparison.OrdinalIgnoreCase)
            || vm.Kind.Equals("flight", StringComparison.OrdinalIgnoreCase);
        }

        private static string FormatTime(double seconds)
        {
            if (seconds <= 0 || double.IsNaN(seconds))
            {
                return "0:00";
            }

            TimeSpan t = TimeSpan.FromSeconds(seconds);
            return t.TotalHours >= 1 ? t.ToString(@"h\:mm\:ss") : t.ToString(@"m\:ss");

        }
    }
}
