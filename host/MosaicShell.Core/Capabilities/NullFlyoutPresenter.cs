namespace MosaicShell.Core.Capabilities;

/// <summary>No-op flyout presenter for tests and headless capability wiring.</summary>
public sealed class NullFlyoutPresenter : IFlyoutPresenter
{
    public void Show(FlyoutRequest request) { }
    public void Update(FlyoutRequest request) { }
    public void SoftRefresh(FlyoutRequest request) { }
    public void Hide(string moduleId) { }
    public void HideAll() { }
    public bool IsVisible(string moduleId) => false;
}
