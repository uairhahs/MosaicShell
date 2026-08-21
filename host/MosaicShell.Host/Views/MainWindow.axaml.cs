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

    private void OnDiscoverCardPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Border border || border.DataContext is not DiscoverCard card)
            return;
        if (DataContext is MainViewModel vm)
            vm.OpenCardCommand.Execute(card);
    }
}
