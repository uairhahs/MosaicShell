using MosaicShell.Core.Capabilities;
using MosaicShell.Core.Capabilities.Platform;
using MosaicShell.Core.Services;

namespace MosaicShell.Core.Tests
{
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

        public void PreviewFlyout(FlyoutRequest request)
        {
            PreviewCount++;
        }
    }

    internal sealed class BridgeUi(IFlyoutPresenter flyouts, IHostUiBridge? hostUi = null) : ICapabilityUiBridge
    {
        public IFlyoutPresenter Flyouts { get; } = flyouts;
        public IHostUiBridge HostUi { get; } = hostUi ?? NullHostUiBridge.Instance;

        private int _depth;

        /// <summary>
        /// True while a RunOnHostThread delegate is executing (test assertions). A depth, not a flag: capabilities nest
        /// these calls, and an inner one ending must not mark the outer one as off the host thread.
        /// </summary>
        public bool InHostThread => _depth > 0;

        public void RunOnHostThread(Action action)
        {
            _depth++;
            try { action(); }
            finally { _depth--; }
        }
    }

    /// <summary>Builds <see cref="ICapabilityContext"/> for unit tests without a full daemon.</summary>
    internal static class TestCapabilityContext
    {
        public static ICapabilityContext Create(
            HostServices services,
            ICapabilityUiBridge ui,
            string moduleId = "Tessera",
            ICapabilityEventBus? events = null)
        {
            ICapabilityEventBus bus = events ?? new CapabilityEventBus();
            CapabilityFlyoutPlatform platform = new(ui.Flyouts);
            MediaSessionPlatform media = new(services.Media);
            return new CapabilityContext(services, ui, platform.CreateSession(moduleId), media, bus);
        }
    }

    internal sealed class CaptureFlyouts(List<FlyoutRequest> shown) : IFlyoutPresenter
    {
        public event Action<string>? TransientDismissed { add { } remove { } }

        public void Show(FlyoutRequest request)
        {
            shown.Add(request);
        }

        public void Update(FlyoutRequest request)
        {
            shown.Add(request);
        }

        public void SoftRefresh(FlyoutRequest request) { }
        public void Hide(string moduleId) { }
        public void HideAll() { }
        public bool IsVisible(string moduleId)
        {
            return shown.Any(r => r.ModuleId.Equals(moduleId, StringComparison.OrdinalIgnoreCase));
        }
    }
}
