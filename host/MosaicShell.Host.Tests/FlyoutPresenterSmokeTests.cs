using Avalonia.Headless.XUnit;
using FluentAssertions;
using MosaicShell.Core.Capabilities;
using MosaicShell.Core.Modules;
using MosaicShell.Core.Services;
using MosaicShell.Host.Capabilities;

namespace MosaicShell.Host.Tests
{
    /// <summary>
    /// A3 smoke tests: each presenter entry point runs end to end under Avalonia headless, with no
    /// desktop session, and leaves the presenter in the state its contract says. These are the
    /// scaffold the A5 scenario driver builds on, not behavior specs.
    /// </summary>
    public sealed class FlyoutPresenterSmokeTests : HeadlessHostTest
    {
        private static FlyoutRequest Volume(string style = "Fluent", IReadOnlyDictionary<string, string>? payload = null)
        {
            return new FlyoutRequest(ModuleIds.Tessera, "vol", style, Payload: payload, Ani: 0);
        }

        private static AvaloniaFlyoutPresenter NewPresenter()
        {
            return new AvaloniaFlyoutPresenter(HostServicesFakes.Create());
        }

        private static void ShowAndWait(AvaloniaFlyoutPresenter presenter)
        {
            presenter.Show(Volume());
            bool shown = HeadlessPump.Until(() => presenter.IsVisible(ModuleIds.Tessera));
            _ = shown.Should().BeTrue($"Show must make the flyout visible; snapshot: {presenter.GetSessionSnapshot(ModuleIds.Tessera)}");
        }

        [AvaloniaFact]
        public void Show_makes_the_flyout_visible()
        {
            AvaloniaFlyoutPresenter presenter = NewPresenter();

            ShowAndWait(presenter);

            presenter.Hide(ModuleIds.Tessera);
        }

        [AvaloniaFact]
        public void Update_on_a_visible_flyout_keeps_it_visible()
        {
            AvaloniaFlyoutPresenter presenter = NewPresenter();
            ShowAndWait(presenter);

            presenter.Update(Volume());
            HeadlessPump.For();

            _ = presenter.IsVisible(ModuleIds.Tessera).Should().BeTrue();
            presenter.Hide(ModuleIds.Tessera);
        }

        [AvaloniaFact]
        public void SoftRefresh_on_a_visible_flyout_keeps_it_visible()
        {
            AvaloniaFlyoutPresenter presenter = NewPresenter();
            ShowAndWait(presenter);

            presenter.SoftRefresh(Volume());
            HeadlessPump.For();

            _ = presenter.IsVisible(ModuleIds.Tessera).Should().BeTrue();
            presenter.Hide(ModuleIds.Tessera);
        }

        [AvaloniaFact]
        public void Hide_hides_the_flyout()
        {
            AvaloniaFlyoutPresenter presenter = NewPresenter();
            ShowAndWait(presenter);

            presenter.Hide(ModuleIds.Tessera);

            _ = HeadlessPump.Until(() => !presenter.IsVisible(ModuleIds.Tessera)).Should().BeTrue();
        }

        [AvaloniaFact]
        public void Nothing_is_visible_before_the_first_show()
        {
            _ = NewPresenter().IsVisible(ModuleIds.Tessera).Should().BeFalse();
        }
    }
}
