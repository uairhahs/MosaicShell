using FluentAssertions;
using MosaicShell.Core.Capabilities;
using MosaicShell.Core.Capabilities.Platform;

namespace MosaicShell.Core.Tests;

public class CapabilityPlatformTests
{
    [Fact]
    public void Flyout_platform_blocks_cold_present_after_transient_dismiss()
    {
        var flyouts = new SessionFlyouts();
        var platform = new CapabilityFlyoutPlatform(flyouts);
        var session = platform.CreateSession("Tessera");
        var request = new FlyoutRequest("Tessera", "media");

        session.Route(request, FlyoutSyncTrigger.ShellMedia, enableMediaFlyouts: true, "Fluent");
        flyouts.ShowCount.Should().Be(1);

        flyouts.RaiseTransientDismiss();
        flyouts.ShowCount = 0;

        session.Route(request, FlyoutSyncTrigger.MediaSession, enableMediaFlyouts: true, "Fluent");
        flyouts.ShowCount.Should().Be(0);
    }

    [Fact]
    public void Flyout_platform_user_intent_clears_suppress()
    {
        var flyouts = new SessionFlyouts();
        var platform = new CapabilityFlyoutPlatform(flyouts);
        var session = platform.CreateSession("Tessera");
        var request = new FlyoutRequest("Tessera", "vol");

        flyouts.RaiseTransientDismiss();
        session.Route(request, FlyoutSyncTrigger.Volume, enableMediaFlyouts: true, "Fluent");
        flyouts.ShowCount.Should().Be(1);
    }

    private sealed class SessionFlyouts : IFlyoutPresenter
    {
        public int ShowCount { get; set; }
        public bool Visible { get; private set; }
        public event Action<string>? TransientDismissed;

        public void RaiseTransientDismiss()
        {
            Visible = false;
            TransientDismissed?.Invoke("Tessera");
        }

        public void Show(FlyoutRequest request)
        {
            ShowCount++;
            Visible = true;
        }

        public void Update(FlyoutRequest request) { }
        public void SoftRefresh(FlyoutRequest request) { }
        public void Hide(string moduleId) => Visible = false;
        public void HideAll() => Visible = false;
        public bool IsVisible(string moduleId) => Visible;
    }
}
