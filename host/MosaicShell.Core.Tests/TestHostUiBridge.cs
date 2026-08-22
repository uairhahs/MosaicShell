using MosaicShell.Core.Capabilities;

namespace MosaicShell.Core.Tests;

internal sealed class RecordingHostUiBridge : IHostUiBridge
{
    public int OpenCount { get; private set; }
    public int CloseCount { get; private set; }
    public string? LastOpenedModule { get; private set; }
    public string? LastClosedModule { get; private set; }
    public int PreviewCount { get; private set; }

    public Task OpenOverlayAsync(string moduleId)
    {
        OpenCount++;
        LastOpenedModule = moduleId;
        return Task.CompletedTask;
    }

    public void CloseOverlay(string moduleId)
    {
        CloseCount++;
        LastClosedModule = moduleId;
    }

    public void FocusOverlay(string moduleId) { }
    public void OpenModuleConfig(string moduleId) { }
    public void RefreshOverlay(string moduleId) { }

    public void PreviewTesseraFlyout(string kind = "vol") => PreviewCount++;
}

internal sealed class BridgeUi(IFlyoutPresenter flyouts, IHostUiBridge? hostUi = null) : ICapabilityUiBridge
{
    public IFlyoutPresenter Flyouts { get; } = flyouts;
    public IHostUiBridge HostUi { get; } = hostUi ?? NullHostUiBridge.Instance;
}
