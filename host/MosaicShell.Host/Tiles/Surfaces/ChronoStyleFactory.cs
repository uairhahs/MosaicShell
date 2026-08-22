using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Media;
using MosaicShell.Core.Settings;

namespace MosaicShell.Host.Tiles.Surfaces;

internal static class ChronoStyleFactory
{
    public static Control Create(ChronoSettings settings, TextBlock time, TextBlock date)
    {
        ApplyTypography(settings, time, date);
        var style = settings.Style;
        if (style.Equals("Center", StringComparison.OrdinalIgnoreCase))
            return CreateCenter(time, date);
        if (style.Equals("Text", StringComparison.OrdinalIgnoreCase)
            || style.Equals("Minimal", StringComparison.OrdinalIgnoreCase))
            return CreateText(time, date);
        if (style.Equals("Tech", StringComparison.OrdinalIgnoreCase)
            || style.Equals("CircTech", StringComparison.OrdinalIgnoreCase))
            return CreateTech(time, date);
        if (style.Equals("Light", StringComparison.OrdinalIgnoreCase))
            return CreateLight(time, date);
        return CreateDefault(time, date);
    }

    private static Control CreateCenter(TextBlock time, TextBlock date)
    {
        time.HorizontalAlignment = HorizontalAlignment.Center;
        date.HorizontalAlignment = HorizontalAlignment.Center;
        var arc = new Ellipse
        {
            Width = 220,
            Height = 110,
            Stroke = WidgetChrome.Brush("#45475a"),
            StrokeThickness = 2,
            Fill = Brushes.Transparent,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Avalonia.Thickness(0, 0, 0, -48)
        };
        var stack = new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Center,
            Spacing = 4,
            Children = { arc, time, date }
        };
        return WidgetChrome.Wrap(stack, minWidth: 300);
    }

    private static Control CreateText(TextBlock time, TextBlock date) =>
        WidgetChrome.Wrap(new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Center,
            Children = { time, date }
        }, minWidth: 260);

    private static Control CreateTech(TextBlock time, TextBlock date) =>
        WidgetChrome.Wrap(new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Center,
            Children = { time, date }
        }, minWidth: 300);

    private static Control CreateLight(TextBlock time, TextBlock date)
    {
        time.Foreground = WidgetChrome.Brush("#1e1e2e");
        date.Foreground = WidgetChrome.Brush("#45475a");
        return WidgetChrome.Wrap(new Border
        {
            Background = WidgetChrome.Brush("#f5f5f7"),
            CornerRadius = new Avalonia.CornerRadius(12),
            Padding = new Avalonia.Thickness(20, 16),
            Child = new StackPanel { Children = { time, date } }
        }, minWidth: 280);
    }

    private static Control CreateDefault(TextBlock time, TextBlock date) =>
        WidgetChrome.Wrap(new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Center,
            Children = { time, date }
        }, minWidth: 280);

    private static void ApplyTypography(ChronoSettings settings, TextBlock time, TextBlock date)
    {
        time.Foreground = WidgetChrome.Brush("#cdd6f4");
        date.Foreground = WidgetChrome.Brush("#a6adc8");
        var style = settings.Style;
        if (style.Equals("Text", StringComparison.OrdinalIgnoreCase)
            || style.Equals("Minimal", StringComparison.OrdinalIgnoreCase))
        {
            time.FontSize = 36;
            time.FontWeight = FontWeight.SemiBold;
            date.FontSize = 12;
        }
        else if (style.Equals("Tech", StringComparison.OrdinalIgnoreCase)
                 || style.Equals("CircTech", StringComparison.OrdinalIgnoreCase))
        {
            time.FontSize = 44;
            time.FontFamily = new FontFamily("Consolas, Cascadia Mono, monospace");
            date.FontSize = 13;
            date.FontFamily = time.FontFamily;
        }
        else if (style.Equals("Center", StringComparison.OrdinalIgnoreCase))
        {
            time.FontSize = 52;
            time.FontWeight = FontWeight.Light;
            date.FontSize = 15;
            date.Margin = new Avalonia.Thickness(0, 10, 0, 0);
        }
        else if (style.Equals("Light", StringComparison.OrdinalIgnoreCase))
        {
            time.FontSize = 52;
            time.FontWeight = FontWeight.Thin;
            date.FontSize = 14;
        }
        else
        {
            time.FontSize = 48;
            date.FontSize = 14;
        }
    }
}
