using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using MosaicShell.Core.Capabilities.Platform;
using MosaicShell.Core.Modules.Tessera;

namespace MosaicShell.Host.Tiles.Tessera
{
    /// <summary>
    /// The glass finish over a stacked card whose own window is the OS blur. Those cards skip the Skia glass panel (its
    /// offscreen layer left a black plate on first paint under the blur), so without this they showed the bare blur and
    /// Frosted and Native Translucency looked the same. The finish is drawn with plain brushes, no offscreen layer:
    /// Frosted is a light shade, white frost, a highlight from the top and a crisp edge (not on Windows);
    /// Native Translucency is the plain veil and a faint edge on KWin and Windows' acrylic bare; both shadow their content. Values from <see cref="NativeTranslucencyAcrylicSpec"/>.
    /// The grain (<see cref="NativeTranslucencyAcrylicSpec.NoiseAlpha"/>, barely there) is left to the Skia panel.
    /// </summary>
    internal static class TesseraGlassFinish
    {
        public static Control Over(Control card, double radius)
        {
            if (!NativeTranslucencyAcrylicSpec.AppPaintsAcrylicLayers(NativeTranslucencyPlatform.Backend))
            {
                return card;
            }

            CornerRadius corners = new(radius);
            Panel panel = new()
            {
                HorizontalAlignment = card.HorizontalAlignment,
                VerticalAlignment = card.VerticalAlignment,
            };

            if (TesseraPalette.Material.FrostedGlass)
            {
                panel.Children.Add(Layer(corners, Solid(
                    NativeTranslucencyAcrylicSpec.TintAlpha,
                    NativeTranslucencyAcrylicSpec.TintR,
                    NativeTranslucencyAcrylicSpec.TintG,
                    NativeTranslucencyAcrylicSpec.TintB)));
                panel.Children.Add(Layer(corners, Solid(NativeTranslucencyAcrylicSpec.FrostAlpha, 255, 255, 255)));
                panel.Children.Add(Layer(corners, new LinearGradientBrush
                {
                    StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                    EndPoint = new RelativePoint(0, NativeTranslucencyAcrylicSpec.HighlightExtent, RelativeUnit.Relative),
                    GradientStops =
                    {
                        new GradientStop(Color.FromArgb(NativeTranslucencyAcrylicSpec.HighlightTopAlpha, 255, 255, 255), 0),
                        new GradientStop(Color.FromArgb(NativeTranslucencyAcrylicSpec.HighlightBottomAlpha, 255, 255, 255), 1),
                    },
                }));
                if (NativeTranslucencyAcrylicSpec.FrostedHasEdge(NativeTranslucencyPlatform.Backend))
                {
                    panel.Children.Add(Edge(corners, NativeTranslucencyAcrylicSpec.EdgeAlpha));
                }
            }
            else if (NativeTranslucencyAcrylicSpec.PaintsPlainVeil(NativeTranslucencyPlatform.Backend))
            {
                panel.Children.Add(Layer(corners, Solid(
                    NativeTranslucencyAcrylicSpec.PlainTintAlpha,
                    NativeTranslucencyAcrylicSpec.PlainTintR,
                    NativeTranslucencyAcrylicSpec.PlainTintG,
                    NativeTranslucencyAcrylicSpec.PlainTintB)));
                panel.Children.Add(Edge(corners, NativeTranslucencyAcrylicSpec.PlainEdgeAlpha));
            }

            if (NativeTranslucencyAcrylicSpec.ShadowsContent(NativeTranslucencyPlatform.Backend))
            {
                card.Effect = new DropShadowEffect
                {
                    OffsetX = 0,
                    OffsetY = NativeTranslucencyAcrylicSpec.ContentShadowOffsetY,
                    BlurRadius = NativeTranslucencyAcrylicSpec.ContentShadowBlurFor(TesseraPalette.Material.FrostedGlass),
                    Color = Colors.Black,
                    Opacity = NativeTranslucencyAcrylicSpec.ContentShadowOpacityFor(TesseraPalette.Material.FrostedGlass),
                };
            }

            panel.Children.Add(card);
            return panel;
        }

        private static SolidColorBrush Solid(byte a, byte r, byte g, byte b)
        {
            return new SolidColorBrush(Color.FromArgb(a, r, g, b));
        }

        private static Border Layer(CornerRadius corners, IBrush fill)
        {
            return new Border { CornerRadius = corners, Background = fill, IsHitTestVisible = false };
        }

        private static Border Edge(CornerRadius corners, byte alpha)
        {
            return new Border
            {
                CornerRadius = corners,
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(Color.FromArgb(alpha, 255, 255, 255)),
                IsHitTestVisible = false,
            };
        }
    }
}
