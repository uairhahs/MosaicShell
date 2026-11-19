using FluentAssertions;
using MosaicShell.Core.Capabilities;
using MosaicShell.Core.Capabilities.Platform;
using MosaicShell.Core.Modules.Tessera;
using MosaicShell.Core.Services;

namespace MosaicShell.Core.Tests
{
    /// <summary>
    /// "Open kind" is what the presenter has open, not what was last asked for (audit F18). Before this the value
    /// was set from the request before the presenter acted, and kept when a cold present was suppressed.
    /// </summary>
    public sealed class CapabilityFlyoutSessionOpenKindTests
    {
        private sealed class Presenter : IFlyoutPresenter
        {
            public int ShowCount { get; private set; }
            public bool Visible { get; set; }
            public TesseraFlyoutSessionSnapshot Snapshot { get; set; }

            public event Action<string>? TransientDismissed
            {
                add { }
                remove { }
            }

            public void Show(FlyoutRequest request)
            {
                ShowCount++;
            }

            public void Update(FlyoutRequest request)
            {
            }

            public void SoftRefresh(FlyoutRequest request)
            {
            }

            public void Hide(string moduleId)
            {
            }

            public void HideAll()
            {
            }

            public bool IsVisible(string moduleId)
            {
                return Visible;
            }

            public TesseraFlyoutSessionSnapshot GetSessionSnapshot(string moduleId)
            {
                return Snapshot;
            }
        }

        private static CapabilityFlyoutSession NewSession(Presenter presenter)
        {
            return new CapabilityFlyoutPlatform(presenter).CreateSession("Tessera");
        }

        [Fact]
        public void Nothing_is_open_before_anything_is_presented()
        {
            NewSession(new Presenter()).OpenKind.Should().BeEmpty();
        }

        [Fact]
        public void A_dismissed_flyout_has_no_open_kind_and_a_suppressed_present_does_not_bring_one_back()
        {
            Presenter presenter = new() { Visible = true };
            CapabilityFlyoutSession session = NewSession(presenter);
            MediaSessionInfo media = new("Track A", "Artist", "app", true, null, 10, 180);
            FlyoutRequest request = new("Tessera", "media");
            session.Route(request, FlyoutSyncTrigger.ShellMedia, "Fluent", media);
            presenter.ShowCount.Should().Be(1);

            // The flyout is dismissed; a media tick for the same track follows and its cold present is suppressed.
            presenter.Visible = false;
            session.NotifyTransientDismissed();
            session.Route(request, FlyoutSyncTrigger.MediaSession, "Fluent", media);

            presenter.ShowCount.Should().Be(1, "the second present was suppressed");
            session.OpenKind.Should().BeEmpty();
            session.OpenStyle.Should().BeNull();
        }

        [Fact]
        public void The_open_kind_is_the_presenters_session_kind_even_when_the_last_request_was_another_kind()
        {
            Presenter presenter = new()
            {
                Visible = true,
                Snapshot = new(true, 3, TesseraFlyoutSessionMode.Single, "vol", "Fluent", TesseraFlyoutPhase.Shown),
            };
            CapabilityFlyoutSession session = NewSession(presenter);

            session.Route(new FlyoutRequest("Tessera", "media"), FlyoutSyncTrigger.Volume, "Fluent");

            session.OpenKind.Should().Be("vol");
            session.OpenStyle.Should().Be("Fluent");
        }

        [Fact]
        public void A_delivered_present_is_reported_through_the_snapshot_once_the_presenter_has_it_open()
        {
            Presenter presenter = new();
            CapabilityFlyoutSession session = NewSession(presenter);

            session.Route(new FlyoutRequest("Tessera", "vol"), FlyoutSyncTrigger.Volume, "Fluent");
            presenter.Visible = true;
            presenter.Snapshot = new(true, 1, TesseraFlyoutSessionMode.Single, "vol", "Fluent", TesseraFlyoutPhase.Entering);

            presenter.ShowCount.Should().Be(1);
            session.OpenKind.Should().Be("vol");
        }
    }
}
