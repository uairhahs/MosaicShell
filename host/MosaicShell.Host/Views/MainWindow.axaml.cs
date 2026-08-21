using Avalonia.Controls;
using Avalonia.Input;
using MosaicShell.Host.ViewModels;

namespace MosaicShell.Host.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (DataContext is MainViewModel vm && vm.IsCapturingHotkey && vm.TryCaptureHotkey(e))
            return;
        base.OnKeyDown(e);
    }

    private void OnDiscoverCardPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Border border || border.DataContext is not DiscoverCard card)
            return;
        if (DataContext is MainViewModel vm)
            vm.OpenCardCommand.Execute(card);
    }
}
