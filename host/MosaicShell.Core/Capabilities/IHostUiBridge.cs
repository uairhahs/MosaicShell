namespace MosaicShell.Core.Capabilities;

/// <summary>
/// Host shell actions (overlays, module config, flyout preview) — implemented by Avalonia Host, mocked in tests.
/// Module-specific preview content is built by the caller into a <see cref="FlyoutRequest"/>.
/// </summary>
public interface IHostUiBridge
{
    Task OpenOverlayAsync(string moduleId);
    void CloseOverlay(string moduleId);
    void FocusOverlay(string moduleId);
    void OpenModuleConfig(string moduleId);
    void RefreshOverlay(string moduleId);
    void PreviewFlyout(FlyoutRequest request);
}

/// <summary>No-op bridge for design-time / tests that do not exercise host chrome.</summary>
public sealed class NullHostUiBridge : IHostUiBridge
{
    public static NullHostUiBridge Instance { get; } = new();

    public Task OpenOverlayAsync(string moduleId) => Task.CompletedTask;
    public void CloseOverlay(string moduleId) { }
    public void FocusOverlay(string moduleId) { }
    public void OpenModuleConfig(string moduleId) { }
    public void RefreshOverlay(string moduleId) { }
    public void PreviewFlyout(FlyoutRequest request) { }
}
