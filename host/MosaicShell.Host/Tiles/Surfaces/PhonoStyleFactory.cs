using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using MosaicShell.Core.Settings;

namespace MosaicShell.Host.Tiles.Surfaces;

internal static class PhonoStyleFactory
{
    public static Control Create(
        PhonoSettings settings,
        TextBlock title,
        TextBlock artist,
        Image art,
        StackPanel transport)
    {
        var style = settings.Style;
        if (style.Equals("Simple", StringComparison.OrdinalIgnoreCase)
            || style.Equals("Side", StringComparison.OrdinalIgnoreCase))
            return CreateHorizontal(settings, title, artist, art, transport);
        if (style.Equals("Center", StringComparison.OrdinalIgnoreCase)
            || style.Equals("Win11", StringComparison.OrdinalIgnoreCase)
            || style.Equals("Card", StringComparison.OrdinalIgnoreCase))
            return CreateCentered(settings, title, artist, art, transport, style);
        if (style.Equals("BigCirc", StringComparison.OrdinalIgnoreCase)
            || style.Equals("DoubleCirc", StringComparison.OrdinalIgnoreCase))
            return CreateCircular(settings, title, artist, art, transport);
        return CreateHorizontal(settings, title, artist, art, transport);
    }

    private static Control CreateHorizontal(
        PhonoSettings settings,
        TextBlock title,
        TextBlock artist,
        Image art,
        StackPanel transport)
    {
        art.Width = 72;
        art.Height = 72;
        var textCol = new StackPanel { Children = { title } };
        if (settings.ShowArtist) textCol.Children.Add(artist);
        textCol.Children.Add(transport);
        return WidgetChrome.Wrap(new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 14,
            VerticalAlignment = VerticalAlignment.Center,
            Children = { art, textCol }
        }, minWidth: 320);
    }

    private static Control CreateCentered(
        PhonoSettings settings,
        TextBlock title,
        TextBlock artist,
        Image art,
        StackPanel transport,
        string style)
    {
        art.Width = style.Equals("Win11", StringComparison.OrdinalIgnoreCase) ? 96 : 88;
        art.Height = art.Width;
        title.HorizontalAlignment = HorizontalAlignment.Center;
        artist.HorizontalAlignment = HorizontalAlignment.Center;
        transport.HorizontalAlignment = HorizontalAlignment.Center;
        var stack = new StackPanel
        {
            Spacing = 10,
            HorizontalAlignment = HorizontalAlignment.Center,
            Children = { art, title }
        };
        if (settings.ShowArtist) stack.Children.Add(artist);
        stack.Children.Add(transport);
        var pad = style.Equals("Card", StringComparison.OrdinalIgnoreCase) ? 16 : 8;
        var corner = style.Equals("Win11", StringComparison.OrdinalIgnoreCase) ? 14 : 10;
        return WidgetChrome.Wrap(new Border
        {
            Background = WidgetChrome.Brush("#181825"),
            CornerRadius = new Avalonia.CornerRadius(corner),
            Padding = new Avalonia.Thickness(pad),
            Child = stack
        }, minWidth: 280);
    }

    private static Control CreateCircular(
        PhonoSettings settings,
        TextBlock title,
        TextBlock artist,
        Image art,
        StackPanel transport)
    {
        art.Width = 96;
        art.Height = 96;
        var frame = new Border
        {
            Width = 104,
            Height = 104,
            CornerRadius = new Avalonia.CornerRadius(52),
            BorderBrush = WidgetChrome.Brush("#585b70"),
            BorderThickness = new Avalonia.Thickness(2),
            ClipToBounds = true,
            Child = art
        };
        title.HorizontalAlignment = HorizontalAlignment.Center;
        artist.HorizontalAlignment = HorizontalAlignment.Center;
        transport.HorizontalAlignment = HorizontalAlignment.Center;
        var stack = new StackPanel
        {
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Center,
            Children = { frame, title }
        };
        if (settings.ShowArtist) stack.Children.Add(artist);
        stack.Children.Add(transport);
        return WidgetChrome.Wrap(stack, minWidth: 280);
    }
}
