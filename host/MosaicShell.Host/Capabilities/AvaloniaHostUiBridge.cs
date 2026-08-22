using Avalonia.Threading;
using MosaicShell.Core.Capabilities;
using MosaicShell.Core.Runtime;
using MosaicShell.Core.Services;
using MosaicShell.Host.Tiles;

namespace MosaicShell.Host.Capabilities;

/// <summary>Avalonia implementation of overlay / config / Tessera preview host actions.</summary>
public sealed class AvaloniaHostUiBridge : IHostUiBridge
{
    private readonly Func<TileRuntime> _runtime;
    private readonly Func<AvaloniaTileSurfaceHost> _tileHost;
    private readonly IFlyoutPresenter _flyouts;
    private readonly HostServices _services;
    private readonly TesseraFlyoutRequestBuilder _tesseraRequests = new();
    private readonly Action<string> _openModuleConfig;

    public AvaloniaHostUiBridge(
        Func<TileRuntime> runtime,
        Func<AvaloniaTileSurfaceHost> tileHost,
        IFlyoutPresenter flyouts,
        HostServices services,
        Action<string> openModuleConfig)
    {
        _runtime = runtime;
        _tileHost = tileHost;
        _flyouts = flyouts;
        _services = services;
        _openModuleConfig = openModuleConfig;
    }

    public Task OpenOverlayAsync(string moduleId) =>
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            var runtime = _runtime();
            var tileHost = _tileHost();
            if (runtime.IsRunning(moduleId))
                tileHost.Focus(moduleId);
            else
                runtime.Start(moduleId);
        }).GetTask();

    public void CloseOverlay(string moduleId) =>
        Dispatcher.UIThread.Post(() => _tileHost().Close(moduleId));

    public void FocusOverlay(string moduleId) =>
        Dispatcher.UIThread.Post(() => _tileHost().Focus(moduleId));

    public void OpenModuleConfig(string moduleId) =>
        Dispatcher.UIThread.Post(() => _openModuleConfig(moduleId));

    public void RefreshOverlay(string moduleId) =>
        Dispatcher.UIThread.Post(() => _tileHost().Refresh(moduleId));

    public void PreviewTesseraFlyout(string kind = "vol") =>
        Dispatcher.UIThread.Post(() =>
        {
            var settings = TesseraFlyoutRequestBuilder.LoadSettings();
            _flyouts.Show(_tesseraRequests.Build(_services, settings, kind));
        });
}
