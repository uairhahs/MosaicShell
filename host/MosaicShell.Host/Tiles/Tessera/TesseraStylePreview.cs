using System;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;

namespace MosaicShell.Host.Tiles.Tessera;

/// <summary>Scaled live Tessera flyout template for the module config panel.</summary>
public sealed class TesseraStylePreview : Border
{
    public static readonly StyledProperty<string?> StyleIdProperty =
        AvaloniaProperty.Register<TesseraStylePreview, string?>(nameof(StyleId), "Fluent");

    public static readonly StyledProperty<bool> ShowMediaStripProperty =
        AvaloniaProperty.Register<TesseraStylePreview, bool>(nameof(ShowMediaStrip), true);

    public static readonly StyledProperty<string?> AccentColorProperty =
        AvaloniaProperty.Register<TesseraStylePreview, string?>(nameof(AccentColor));

    private static int _suspendDepth;
    private static event Action? RebuildFlushRequested;

    private readonly ContentControl _host = new()
    {
        HorizontalAlignment = HorizontalAlignment.Center,
        VerticalAlignment = VerticalAlignment.Center,
        IsHitTestVisible = false
    };

    private bool _rebuildPending;
    private bool _rebuildPosted;

    public TesseraStylePreview()
    {
        Background = new SolidColorBrush(Color.Parse("#181825"));
        BorderBrush = new SolidColorBrush(Color.Parse("#313244"));
        BorderThickness = new Thickness(1);
        CornerRadius = new CornerRadius(12);
        Padding = new Thickness(16, 14);
        MinHeight = 148;
        ClipToBounds = true;
        Child = new Viewbox
        {
            Stretch = Stretch.Uniform,
            StretchDirection = StretchDirection.DownOnly,
            MaxHeight = 100,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Child = _host
        };
    }

    public string? StyleId
    {
        get => GetValue(StyleIdProperty);
        set => SetValue(StyleIdProperty, value);
    }

    public bool ShowMediaStrip
    {
        get => GetValue(ShowMediaStripProperty);
        set => SetValue(ShowMediaStripProperty, value);
    }

    public string? AccentColor
    {
        get => GetValue(AccentColorProperty);
        set => SetValue(AccentColorProperty, value);
    }

    public static IDisposable EnterSuspendRebuild()
    {
        Interlocked.Increment(ref _suspendDepth);
        return new SuspendScope();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        RebuildFlushRequested += HandleRebuildFlush;
        if (_rebuildPending || _host.Content is null)
            ScheduleRebuild();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        RebuildFlushRequested -= HandleRebuildFlush;
        base.OnDetachedFromVisualTree(e);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == IsVisibleProperty)
        {
            if (IsVisible && _rebuildPending)
                ScheduleRebuild();
            return;
        }

        if (change.Property == StyleIdProperty || change.Property == ShowMediaStripProperty
            || change.Property == AccentColorProperty)
        {
            ScheduleRebuild();
        }
    }

    private void HandleRebuildFlush()
    {
        if (_rebuildPending)
            ScheduleRebuild();
    }

    private void ScheduleRebuild()
    {
        if (Volatile.Read(ref _suspendDepth) > 0)
        {
            _rebuildPending = true;
            return;
        }

        if (!IsVisible)
        {
            _rebuildPending = true;
            return;
        }

        if (_rebuildPosted)
            return;

        _rebuildPosted = true;
        Dispatcher.UIThread.Post(() =>
        {
            _rebuildPosted = false;
            if (Volatile.Read(ref _suspendDepth) > 0 || !IsVisible)
            {
                _rebuildPending = true;
                return;
            }

            _rebuildPending = false;
            Rebuild();
        }, DispatcherPriority.Background);
    }

    private void Rebuild()
    {
        try
        {
            var style = string.IsNullOrWhiteSpace(StyleId) ? "Fluent" : StyleId!;
            _host.Content = TesseraPreviewExporter.BuildFlyout(style, ShowMediaStrip, AccentColor);
        }
        catch
        {
            _host.Content = new TextBlock
            {
                Text = "Preview unavailable",
                Foreground = new SolidColorBrush(Color.Parse("#6c7086")),
                FontSize = 12,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
        }
    }

    private sealed class SuspendScope : IDisposable
    {
        private int _disposed;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
                return;
            if (Interlocked.Decrement(ref _suspendDepth) == 0)
                RebuildFlushRequested?.Invoke();
        }
    }
}
