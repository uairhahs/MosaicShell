using FluentAssertions;
using MosaicShell.Core.Capabilities;
using MosaicShell.Core.Capabilities.Platform;
using MosaicShell.Core.Services;

namespace MosaicShell.Core.Tests
{
    public class CapabilityPlatformTests
    {
        [Fact]
        public void Flyout_platform_blocks_cold_present_after_transient_dismiss()
        {
            SessionFlyouts flyouts = new();
            CapabilityFlyoutPlatform platform = new(flyouts);
            CapabilityFlyoutSession session = platform.CreateSession("Tessera");
            FlyoutRequest request = new("Tessera", "media");
            MediaSessionInfo media = new("Track A", "Artist", "app", true, null, 10, 180);

            session.Route(request, FlyoutSyncTrigger.ShellMedia, enableMediaFlyouts: true, "Fluent", media);
            _ = flyouts.ShowCount.Should().Be(1);

            flyouts.RaiseTransientDismiss();
            flyouts.ShowCount = 0;

            session.Route(request, FlyoutSyncTrigger.MediaSession, enableMediaFlyouts: true, "Fluent", media);
            _ = flyouts.ShowCount.Should().Be(0);
        }

        [Fact]
        public void Flyout_platform_track_boundary_clears_suppress()
        {
            SessionFlyouts flyouts = new();
            CapabilityFlyoutPlatform platform = new(flyouts);
            CapabilityFlyoutSession session = platform.CreateSession("Tessera");
            FlyoutRequest request = new("Tessera", "media");
            MediaSessionInfo trackA = new("Track A", "Artist", "app", true, null, 40, 180);

            session.Route(request, FlyoutSyncTrigger.ShellMedia, enableMediaFlyouts: true, "Fluent", trackA);
            _ = flyouts.ShowCount.Should().Be(1);

            flyouts.RaiseTransientDismiss();
            flyouts.ShowCount = 0;

            MediaSessionInfo trackB = new("Track B", "Artist", "app", true, null, 0.5, 180);
            session.Route(request, FlyoutSyncTrigger.MediaSession, enableMediaFlyouts: true, "Fluent", trackB);
            _ = flyouts.ShowCount.Should().Be(1);
        }

        [Fact]
        public void Flyout_platform_user_intent_clears_suppress()
        {
            SessionFlyouts flyouts = new();
            CapabilityFlyoutPlatform platform = new(flyouts);
            CapabilityFlyoutSession session = platform.CreateSession("Tessera");
            FlyoutRequest request = new("Tessera", "vol");

            flyouts.RaiseTransientDismiss();
            session.Route(request, FlyoutSyncTrigger.Volume, enableMediaFlyouts: true, "Fluent");
            _ = flyouts.ShowCount.Should().Be(1);
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
            public void Hide(string moduleId)
            {
                Visible = false;
            }

            public void HideAll()
            {
                Visible = false;
            }

            public bool IsVisible(string moduleId)
            {
                return Visible;
            }
        }
    }
}
