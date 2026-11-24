using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace MosaicShell.Host.Theming
{
    /// <summary>
    /// Fluent 2 brush tokens for views built in code. The house palette is fed to Fluent 2 once (App.axaml), so a
    /// view asks for a token by role instead of carrying a colour of its own.
    /// </summary>
    internal static class ThemeTokens
    {
        public const string TextPrimary = "TextFillColorPrimaryBrush";
        public const string TextSecondary = "TextFillColorSecondaryBrush";
        public const string TextTertiary = "TextFillColorTertiaryBrush";
        public const string AccentFill = "AccentFillColorDefaultBrush";
        public const string SubtleFill = "SubtleFillColorSecondaryBrush";
        public const string ControlStroke = "ControlStrokeColorDefaultBrush";
        public const string CardFill = "CardBackgroundFillColorDefaultBrush";

        /// <summary>The token's brush for the app's current theme variant; transparent if the theme lacks it.</summary>
        public static IBrush Brush(string token)
        {
            Application? app = Application.Current;
            return app is not null
                   && app.TryFindResource(token, app.ActualThemeVariant, out object? value)
                   && value is IBrush brush
                ? brush
                : Brushes.Transparent;
        }

        /// <summary>The token's colour at <paramref name="opacity"/>, for translucent surfaces over a window backdrop.</summary>
        public static IBrush Brush(string token, double opacity)
        {
            return Brush(token) is ISolidColorBrush solid
                ? new SolidColorBrush(solid.Color, opacity)
                : Brushes.Transparent;
        }

        /// <summary>The theme's corner radius for overlays and cards (OverlayCornerRadius), else <paramref name="fallback"/>.</summary>
        public static CornerRadius OverlayRadius(double fallback = 12)
        {
            Application? app = Application.Current;
            return app is not null
                   && app.TryFindResource("OverlayCornerRadius", app.ActualThemeVariant, out object? value)
                   && value is CornerRadius radius
                ? radius
                : new CornerRadius(fallback);
        }
    }
}
