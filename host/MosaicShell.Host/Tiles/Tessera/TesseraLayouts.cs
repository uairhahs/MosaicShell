using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Material.Icons;
using Material.Icons.Avalonia;

namespace MosaicShell.Host.Tiles.Tessera;

public static class TesseraStyleFactory
{
    public static Control Create(string styleId, TesseraFlyoutViewModel vm)
    {
        TesseraPalette.RefreshAccent();
        return styleId.ToLowerInvariant() switch
        {
            "win11" => TesseraLayouts.Win11(vm),
            "simple" => TesseraLayouts.Simple(vm),
            "pixel" => TesseraLayouts.Pixel(vm),
            "center" => TesseraLayouts.Center(vm),
            "modern" => TesseraLayouts.Modern(vm),
            "amber" => TesseraLayouts.Amber(vm),
            "gnome" => TesseraLayouts.Gnome(vm),
            "smouti" => TesseraLayouts.Smouti(vm),
            "plainext" => TesseraLayouts.Plainext(vm),
            "coreui" => TesseraLayouts.CoreUI(vm),
            _ => TesseraLayouts.Fluent(vm),
        };
    }
}

/// <summary>YourFlyouts layout recreations — Fluent/Win11 use the transfer kit.</summary>
internal static class TesseraLayouts
{
    // —— Fluent (YourFlyouts Main/Layout/Fluent.inc + Vars/Fluent.inc) ——

    public static Control Fluent(TesseraFlyoutViewModel vm)
    {
        if (IsStatus(vm))
            return FluentLocks(vm);

        if (vm.Kind.Equals("media", StringComparison.OrdinalIgnoreCase))
        {
            return TesseraShell.Create(
                TesseraMediaPanel.Create(vm, TesseraMediaMode.FluentSide),
                cornerRadius: 0,
                fill: TesseraPalette.Primary);
        }

        const double volumeW = TesseraFluentMetrics.VolumeWidth;
        const double h = TesseraFluentMetrics.Height;
        const double pad = TesseraFluentMetrics.Pad;

        var glyph = TesseraVolumeGlyph.Create(vm, 18);
        glyph.Name = "TesseraGlyph";
        glyph.HorizontalAlignment = HorizontalAlignment.Center;
        glyph.Margin = new Thickness(0, pad, 0, 0);

        var track = new TesseraTrack
        {
            IsVertical = true,
            Width = 28,
            Height = h - pad * 2 - 40,
            HorizontalAlignment = HorizontalAlignment.Center,
            Value = vm.PrimaryValue,
            Name = "TesseraTrack"
        };
        track.ValueChanged += (_, v) => vm.ApplyPrimary(v);

        var percent = new TextBlock
        {
            Text = vm.PrimaryPercent,
            FontSize = 10,
            Foreground = TesseraPalette.FontMutedBrush,
            FontFamily = new FontFamily("Segoe UI"),
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, pad),
            Name = "TesseraPercent"
        };

        var volPanel = new DockPanel { LastChildFill = true };
        DockPanel.SetDock(glyph, Avalonia.Controls.Dock.Top);
        DockPanel.SetDock(percent, Avalonia.Controls.Dock.Bottom);
        volPanel.Children.Add(glyph);
        volPanel.Children.Add(percent);
        volPanel.Children.Add(track);

        var volCol = new Border
        {
            Width = volumeW,
            Height = h,
            Background = Brushes.Transparent,
            Child = volPanel
        };
        BindWheel(volCol, vm);

        Control body = volCol;
        if (vm.ShowMediaStrip)
        {
            var divider = new Border
            {
                Width = 1,
                Height = h - pad * 2,
                Background = TesseraPalette.StrokeBrush,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, pad, 0, pad)
            };
            var media = TesseraMediaPanel.Create(vm, TesseraMediaMode.FluentSide);
            body = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Children = { volCol, divider, media }
            };
        }

        return TesseraShell.Create(body, cornerRadius: 0, fill: TesseraPalette.Primary);
    }

    private static Control FluentLocks(TesseraFlyoutViewModel vm)
    {
        const double w = TesseraFluentMetrics.LocksWidth;
        const double h = TesseraFluentMetrics.LocksHeight;
        var row = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 12,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(20, 0),
            Children =
            {
                TesseraVolumeGlyph.Create(vm, 16),
                new TextBlock
                {
                    Text = vm.KindLabel,
                    FontSize = 14,
                    Foreground = TesseraPalette.FontBrush,
                    VerticalAlignment = VerticalAlignment.Center,
                    FontFamily = new FontFamily("Segoe UI")
                }
            }
        };
        return TesseraShell.Create(row, 0, fill: TesseraPalette.Primary, width: w, height: h);
    }

    // —— Win11 (transfer proof) ——

    public static Control Win11(TesseraFlyoutViewModel vm)
    {
        if (IsStatus(vm))
        {
            return TesseraShell.Create(
                new TextBlock
                {
                    Text = vm.KindLabel,
                    FontSize = 12,
                    Foreground = TesseraPalette.FontBrush,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    FontFamily = new FontFamily("Segoe UI")
                },
                cornerRadius: 12,
                fill: TesseraPalette.PrimarySolid,
                width: 320,
                height: 50);
        }

        if (vm.Kind.Equals("media", StringComparison.OrdinalIgnoreCase))
        {
            return TesseraShell.Create(
                TesseraMediaPanel.Create(vm, TesseraMediaMode.Win11Below),
                cornerRadius: 12,
                fill: TesseraPalette.PrimarySolid,
                width: 320);
        }

        const double w = TesseraWin11Metrics.Width;
        const double volH = TesseraWin11Metrics.VolumeHeight;

        var glyph = TesseraVolumeGlyph.Create(vm, 16);
        glyph.Name = "TesseraGlyph";
        glyph.Width = 40;
        glyph.HorizontalAlignment = HorizontalAlignment.Center;
        glyph.VerticalAlignment = VerticalAlignment.Center;

        var track = new TesseraTrack
        {
            IsVertical = false,
            Width = w - 120,
            Height = 28,
            VerticalAlignment = VerticalAlignment.Center,
            Value = vm.PrimaryValue,
            Name = "TesseraTrack"
        };
        track.ValueChanged += (_, v) => vm.ApplyPrimary(v);

        var percent = new TextBlock
        {
            Text = vm.PrimaryPercent,
            FontSize = 12,
            Width = 40,
            TextAlignment = TextAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = TesseraPalette.FontBrush,
            FontFamily = new FontFamily("Segoe UI"),
            Name = "TesseraPercent"
        };

        var row = new Grid
        {
            Width = w,
            Height = volH,
            ColumnDefinitions = new ColumnDefinitions("50,*,50")
        };
        Grid.SetColumn(glyph, 0);
        Grid.SetColumn(track, 1);
        Grid.SetColumn(percent, 2);
        row.Children.Add(glyph);
        row.Children.Add(track);
        row.Children.Add(percent);
        BindWheel(row, vm);

        Control body = row;
        if (vm.ShowMediaStrip)
        {
            var media = TesseraMediaPanel.Create(vm, TesseraMediaMode.Win11Below);
            var divider = new Border
            {
                Height = 1,
                Background = TesseraPalette.StrokeBrush,
                Margin = new Thickness(15, 0)
            };
            body = new StackPanel { Children = { row, divider, media } };
        }

        return TesseraShell.Create(body, cornerRadius: 12, fill: TesseraPalette.PrimarySolid, width: w);
    }

    // —— Remaining layouts (kit pieces, less fidelity) ——

    public static Control Gnome(TesseraFlyoutViewModel vm) => CompactPill(vm, 40);
    public static Control Simple(TesseraFlyoutViewModel vm) => VerticalCard(vm, 130);
    // Natural Win11 bar sized to content — do not ClipToBounds-squeeze media into a fixed shell
    public static Control Modern(TesseraFlyoutViewModel vm) => HorizontalBar(vm, 400);
    public static Control CoreUI(TesseraFlyoutViewModel vm) => HorizontalBar(vm, 400);
    public static Control Amber(TesseraFlyoutViewModel vm) => VerticalCard(vm, 160, thin: true);
    public static Control Center(TesseraFlyoutViewModel vm)
    {
        if (IsStatus(vm)) return FluentLocks(vm);
        var card = TesseraShell.Create(
            new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Spacing = 8,
                Children =
                {
                    TesseraVolumeGlyph.Create(vm, 28),
                    new TextBlock
                    {
                        Text = vm.PrimaryPercent + (vm.Kind.Equals("bright", StringComparison.OrdinalIgnoreCase) ? "%" : ""),
                        FontSize = 20,
                        Foreground = TesseraPalette.FontBrush,
                        HorizontalAlignment = HorizontalAlignment.Center
                    }
                }
            },
            18, new Thickness(16), TesseraPalette.Primary, width: 140, height: 140);
        BindWheel(card, vm, 0.02);
        return card;
    }

    public static Control Pixel(TesseraFlyoutViewModel vm)
    {
        if (IsStatus(vm)) return FluentLocks(vm);
        var track = new TesseraTrack { IsVertical = true, Width = 24, Height = 100, Value = vm.PrimaryValue };
        track.ValueChanged += (_, v) => vm.ApplyPrimary(v);
        var devices = new ComboBox
        {
            Width = 120,
            ItemsSource = vm.Devices.Select(d => d.Name).ToList(),
            SelectedItem = vm.Devices.FirstOrDefault(d => d.IsDefault)?.Name
        };
        var mixer = new Button
        {
            Width = 32, Height = 32, Padding = new Thickness(0), Background = Brushes.Transparent,
            Content = new MaterialIcon { Kind = MaterialIconKind.Tune, Width = 18, Height = 18, Foreground = TesseraPalette.AccentBrush }
        };
        mixer.Click += (_, _) => _ = TesseraHostBridge.ArmMixdeckAsync?.Invoke();
        return TesseraShell.Create(new StackPanel
        {
            Spacing = 10,
            Width = 150,
            Children =
            {
                new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 10,
                    Children = { TesseraVolumeGlyph.Create(vm, 20), track, mixer }
                },
                new TextBlock
                {
                    Text = vm.PrimaryPercent,
                    FontSize = 14,
                    FontWeight = FontWeight.Bold,
                    Foreground = TesseraPalette.AccentBrush
                },
                devices
            }
        }, 4, new Thickness(12), Color.FromRgb(27, 27, 30));
    }

    public static Control Plainext(TesseraFlyoutViewModel vm)
    {
        var n = (int)Math.Round(vm.PrimaryValue * 16);
        var bar = new string('█', n) + new string('·', 16 - n);
        var text = new TextBlock
        {
            Text = $"{vm.KindLabel}: {vm.PrimaryPercent}\n{bar}",
            FontFamily = new FontFamily("Cascadia Mono, Consolas"),
            FontSize = 13,
            Foreground = new SolidColorBrush(Color.FromRgb(30, 30, 46))
        };
        BindWheel(text, vm);
        return TesseraShell.Create(text, 4, new Thickness(10), Color.FromArgb(240, 205, 214, 244));
    }

    public static Control Smouti(TesseraFlyoutViewModel vm)
    {
        if (IsStatus(vm)) return FluentLocks(vm);
        var track = new TesseraTrack { IsVertical = false, Width = 180, Height = 24, Value = vm.PrimaryValue };
        track.ValueChanged += (_, v) => vm.ApplyPrimary(v);
        return TesseraShell.Create(new StackPanel
        {
            Spacing = 6,
            Children =
            {
                new TextBlock { Text = vm.KindLabel.ToUpperInvariant(), FontSize = 11, Foreground = TesseraPalette.AccentBrush },
                new TextBlock { Text = vm.PrimaryPercent, FontSize = 22, FontWeight = FontWeight.Bold, Foreground = TesseraPalette.FontBrush },
                track
            }
        }, 16, new Thickness(14), TesseraPalette.Primary, minWidth: 200);
    }

    private static Control CompactPill(TesseraFlyoutViewModel vm, double radius)
    {
        if (IsStatus(vm)) return FluentLocks(vm);
        var track = new TesseraTrack { IsVertical = false, Width = 160, Height = 24, Value = vm.PrimaryValue, Name = "TesseraTrack" };
        track.ValueChanged += (_, v) => vm.ApplyPrimary(v);
        var glyph = TesseraVolumeGlyph.Create(vm, 16);
        glyph.Name = "TesseraGlyph";
        var row = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10,
            VerticalAlignment = VerticalAlignment.Center,
            Children =
            {
                glyph,
                track,
                new TextBlock
                {
                    Text = vm.PrimaryPercent,
                    FontSize = 12,
                    VerticalAlignment = VerticalAlignment.Center,
                    Foreground = TesseraPalette.FontBrush,
                    Name = "TesseraPercent"
                }
            }
        };
        BindWheel(row, vm);
        return TesseraShell.Create(row, radius, new Thickness(12, 8), TesseraPalette.PrimarySolid);
    }

    private static Control VerticalCard(TesseraFlyoutViewModel vm, double h, bool thin = false)
    {
        if (IsStatus(vm)) return FluentLocks(vm);
        var track = new TesseraTrack
        {
            IsVertical = true,
            Width = thin ? 20 : 28,
            Height = h - 60,
            Value = vm.PrimaryValue,
            Name = "TesseraTrack"
        };
        track.ValueChanged += (_, v) => vm.ApplyPrimary(v);
        var glyph = TesseraVolumeGlyph.Create(vm, 18);
        glyph.Name = "TesseraGlyph";
        var percent = new TextBlock
        {
            Text = vm.PrimaryPercent,
            FontSize = 18,
            FontWeight = FontWeight.SemiBold,
            Foreground = TesseraPalette.FontBrush,
            Name = "TesseraPercent"
        };
        Control vol = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 12,
            Children =
            {
                new StackPanel { Spacing = 4, Children = { glyph, percent } },
                track
            }
        };
        if (vm.ShowMediaStrip)
        {
            // Win11-sized media under volume — SizeToContent, no forced squeeze
            vol = new StackPanel
            {
                Spacing = 8,
                Children =
                {
                    vol,
                    TesseraMediaPanel.Create(vm, TesseraMediaMode.Win11Below)
                }
            };
        }
        return TesseraShell.Create(vol, 6, new Thickness(14), TesseraPalette.Primary);
    }

    /// <summary>Horizontal volume bar at natural size (Modern / CoreUI). Media sits below without clipping.</summary>
    private static Control HorizontalBar(TesseraFlyoutViewModel vm, double width)
    {
        if (IsStatus(vm))
        {
            return TesseraShell.Create(
                new TextBlock
                {
                    Text = vm.KindLabel,
                    FontSize = 12,
                    Foreground = TesseraPalette.FontBrush,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                },
                12, fill: TesseraPalette.PrimarySolid, width: width, height: 50);
        }

        const double volH = TesseraWin11Metrics.VolumeHeight;
        var glyph = TesseraVolumeGlyph.Create(vm, 16);
        glyph.Name = "TesseraGlyph";
        glyph.VerticalAlignment = VerticalAlignment.Center;

        var track = new TesseraTrack
        {
            IsVertical = false,
            Width = Math.Max(80, width - 120),
            Height = 28,
            VerticalAlignment = VerticalAlignment.Center,
            Value = vm.PrimaryValue,
            Name = "TesseraTrack"
        };
        track.ValueChanged += (_, v) => vm.ApplyPrimary(v);

        var percent = new TextBlock
        {
            Text = vm.PrimaryPercent,
            FontSize = 12,
            Width = 44,
            TextAlignment = TextAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = TesseraPalette.FontBrush,
            Name = "TesseraPercent"
        };

        var row = new Grid
        {
            Width = width,
            Height = volH,
            ColumnDefinitions = new ColumnDefinitions("50,*,50")
        };
        Grid.SetColumn(glyph, 0);
        Grid.SetColumn(track, 1);
        Grid.SetColumn(percent, 2);
        row.Children.Add(glyph);
        row.Children.Add(track);
        row.Children.Add(percent);
        BindWheel(row, vm);

        Control body = row;
        double shellW = width;
        if (vm.ShowMediaStrip)
        {
            var media = TesseraMediaPanel.Create(vm, TesseraMediaMode.Win11Below);
            shellW = Math.Max(width, TesseraWin11Metrics.Width);
            body = new StackPanel
            {
                Children =
                {
                    row,
                    new Border { Height = 1, Background = TesseraPalette.StrokeBrush, Margin = new Thickness(12, 0) },
                    media
                }
            };
        }

        return TesseraShell.Create(body, 12, fill: TesseraPalette.PrimarySolid, width: shellW);
    }

    private static void BindWheel(Control c, TesseraFlyoutViewModel vm, double step = 0.02) =>
        c.PointerWheelChanged += (_, e) =>
        {
            vm.Nudge(e.Delta.Y > 0 ? step : -step);
            e.Handled = true;
        };

    private static bool IsStatus(TesseraFlyoutViewModel vm) =>
        vm.Kind.Equals("locks", StringComparison.OrdinalIgnoreCase)
        || vm.Kind.Equals("flight", StringComparison.OrdinalIgnoreCase);
}
