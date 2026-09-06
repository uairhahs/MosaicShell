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

        public static Control CoreUI(TesseraFlyoutViewModel vm)
        {
            if (IsStatus(vm))
            {
                return StatusChip(vm, 8);
            }

            const double w = TesseraStyleMetrics.CoreUiWidth;
            const double gap = TesseraStyleMetrics.CoreUiGap;
            const double mediaH = TesseraStyleMetrics.CoreUiMediaH;

            Control device = TesseraChrome.CoreUiTile(
                new MaterialIcon
                {
                    Kind = MaterialIconKind.Headphones,
                    Width = 18,
                    Height = 18,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Foreground = TesseraPalette.FontBrush
                },
                w: TesseraStyleMetrics.CoreUiDevice,
                h: TesseraStyleMetrics.CoreUiDevice);

            Control glyph = TesseraVolumeGlyph.Create(vm, 12);
            glyph.Name = "TesseraGlyph";
            TesseraTrack track = new()
            {
                IsVertical = false,
                Height = 20,
                Value = vm.PrimaryValue,
                Name = "TesseraTrack",
                TrackThickness = 8,
                ShowThumb = false,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                AccentBrushOverride = TesseraStylePalette.CoreUi.AccentBrush
            };
            track.ValueChanged += (_, v) => vm.ApplyPrimary(v);
            TextBlock percent = new()
            {
                Text = vm.PrimaryPercent,
                FontSize = 12,
                FontWeight = FontWeight.SemiBold,
                VerticalAlignment = VerticalAlignment.Center,
                MinWidth = 36,
                Foreground = TesseraPalette.FontBrush,
                Name = "TesseraPercent"
            };
            TesseraLiveAmbient.RegisterVolume(track, percent, glyph as MaterialIcon);
            Grid volInner = new()
            {
                ColumnDefinitions = new ColumnDefinitions("Auto,Auto,*"),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(glyph, 0);
            Grid.SetColumn(percent, 1);
            Grid.SetColumn(track, 2);
            glyph.Margin = new Thickness(0, 0, 8, 0);
            percent.Margin = new Thickness(0, 0, 8, 0);
            volInner.Children.Add(glyph);
            volInner.Children.Add(percent);
            volInner.Children.Add(track);
            Control volBar = TesseraChrome.CoreUiTile(volInner, h: TesseraStyleMetrics.CoreUiVolumeH, pad: new Thickness(10, 0));
            BindWheel(volBar, vm);

            MaterialIcon playIcon = new()
            {
                Kind = vm.IsPlaying ? MaterialIconKind.Pause : MaterialIconKind.Play,
                Width = 12,
                Height = 12,
                Foreground = Brushes.Black,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            double playSize = TesseraCoreUiLayoutSpec.PlayButtonDip;
            Border playBtn = new()
            {
                Width = playSize,
                Height = playSize,
                CornerRadius = new CornerRadius(TesseraCoreUiLayoutSpec.PlayButtonCornerRadiusDip),
                ClipToBounds = true,
                Background = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                Child = playIcon
            };
            TesseraChrome.ApplyHoverHighlight(playBtn, Brushes.White,
                new SolidColorBrush(Color.FromRgb(245, 245, 250)));
            playBtn.PointerPressed += (_, e) => { _ = vm.PlayPauseAsync(); e.Handled = true; };

            const double transportBtn = 22;
            Control media = TesseraMediaPanel.Create(vm, TesseraMediaMode.CoreUiBlock, mediaH);
            Control transport = TesseraChrome.CoreUiTile(
                new StackPanel
                {
                    Spacing = 4,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Children =
                    {
                        CoreUiIconBtn(MaterialIconKind.SkipPrevious, () => _ = vm.PreviousAsync(), transportBtn),
                        playBtn,
                        CoreUiIconBtn(MaterialIconKind.SkipNext, () => _ = vm.NextAsync(), transportBtn)
                    }
                },
                w: TesseraStyleMetrics.CoreUiTransportW,
                h: mediaH,
                pad: new Thickness(4));

            Grid top = new()
            {
                Width = w - (gap * 2),
                ColumnDefinitions = new ColumnDefinitions($"{TesseraStyleMetrics.CoreUiDevice},{gap},*"),
                Height = TesseraStyleMetrics.CoreUiVolumeH
            };
            Grid.SetColumn(device, 0);
            Grid.SetColumn(volBar, 2);
            top.Children.Add(device);
            top.Children.Add(volBar);

            Control body = top;
            if (vm.ShowMediaStrip)
            {
                Grid bottom = new()
                {
                    Width = w - (gap * 2),
                    ColumnDefinitions = new ColumnDefinitions($"*,{gap},{TesseraStyleMetrics.CoreUiTransportW}"),
                    Height = mediaH
                };
                media.HorizontalAlignment = HorizontalAlignment.Stretch;
                Grid.SetColumn(media, 0);
                Grid.SetColumn(transport, 2);
                bottom.Children.Add(media);
                bottom.Children.Add(transport);
                double rowW = TesseraCoreUiLayoutSpec.InnerRowWidthDip;
                TesseraRevealHost reveal = TesseraRevealHost.WrapCoreUi(vm, bottom, track, rowW, baseTrackThickness: 8);
                body = new StackPanel { Spacing = gap, Width = w - (gap * 2), Children = { top, reveal } };
            }

            return TesseraChrome.GlassTinted(body, 8, TesseraStylePalette.CoreUi.ShellBrush,
                new Thickness(gap), w, useSharedBackdrop: true);

        }
    }
}
