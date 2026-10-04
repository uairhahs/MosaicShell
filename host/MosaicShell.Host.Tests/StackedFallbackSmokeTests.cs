using Avalonia.Headless.XUnit;
using FluentAssertions;
using MosaicShell.Core.Capabilities;
using MosaicShell.Core.Capabilities.Platform;
using MosaicShell.Core.Modules;
using MosaicShell.Core.Modules.Tessera;
using MosaicShell.Core.Services;
using MosaicShell.Host.Capabilities;

namespace MosaicShell.Host.Tests
{
    /// <summary>
    /// The stacked (N-window OS acrylic) path cannot run headless: it needs Windows composition.
    /// What can run is its gate. With OS acrylic forced off (<c>--tessera-software-render</c>), a
    /// request that would otherwise be stacked must take the single-window path, and the presenter's
    /// session bookkeeping (mode, kind, style, visibility) must say so through every entry point.
    /// The stacked windows themselves stay a desktop check.
    /// </summary>
    public sealed class StackedFallbackSmokeTests : HeadlessHostTest
    {
        private const string StackedStyle = "Meter";

        private static readonly Dictionary<string, string> StackedPayload = new()
        {
            ["showMediaStrip"] = "1",
            ["acrylic"] = "1",
        };

        public StackedFallbackSmokeTests()
            : base(HostLaunchOptions.TesseraForceSoftwareRenderFlag)
        {
        }

        private static FlyoutRequest StackedEligible(string style = StackedStyle)
        {
            return new FlyoutRequest(ModuleIds.Tessera, "vol", style, Payload: StackedPayload, Ani: 0);
        }

        private static AvaloniaFlyoutPresenter NewPresenter()
        {
            return new AvaloniaFlyoutPresenter(HostServicesFakes.Create());
        }

        private static void ShowAndWait(AvaloniaFlyoutPresenter presenter, FlyoutRequest request)
        {
            presenter.Show(request);
            _ = HeadlessPump.Until(() => presenter.IsVisible(ModuleIds.Tessera)).Should().BeTrue(
                $"Show must make the flyout visible; snapshot: {presenter.GetSessionSnapshot(ModuleIds.Tessera)}");
        }

        [AvaloniaFact]
        public void The_request_is_stacked_eligible_and_only_the_forced_software_render_keeps_it_single()
        {
            // Guards the premise: if this request stopped being stacked-eligible, the tests below
            // would pass without exercising the fallback at all.
            _ = TesseraOsAcrylicStackedPolicy.UseMultiWindow(
                StackedPayload, StackedStyle, trialRequested: true, osSupportsWinUiAcrylic: true,
                osAcrylicRenderingAvailable: true, kind: "vol").Should().BeTrue();
            _ = TesseraOsAcrylicStackedPolicy.UseMultiWindowFromPayload(StackedPayload, StackedStyle, "vol").Should().BeFalse();
        }

        [AvaloniaFact]
        public void Show_takes_the_single_window_path_and_records_a_single_session()
        {
            AvaloniaFlyoutPresenter presenter = NewPresenter();

            ShowAndWait(presenter, StackedEligible());

            TesseraFlyoutSessionSnapshot snap = presenter.GetSessionSnapshot(ModuleIds.Tessera);
            _ = snap.Mode.Should().Be(TesseraFlyoutSessionMode.Single);
            _ = snap.Kind.Should().Be("vol");
            _ = snap.StyleId.Should().Be(StackedStyle);
            _ = snap.EffectivelyShowing.Should().BeTrue();
            presenter.Hide(ModuleIds.Tessera);
        }

        [AvaloniaFact]
        public void Update_and_SoftRefresh_keep_the_single_session()
        {
            AvaloniaFlyoutPresenter presenter = NewPresenter();
            ShowAndWait(presenter, StackedEligible());
            int generation = presenter.GetSessionSnapshot(ModuleIds.Tessera).Generation;

            presenter.Update(StackedEligible());
            HeadlessPump.For();
            presenter.SoftRefresh(StackedEligible());
            HeadlessPump.For();

            TesseraFlyoutSessionSnapshot snap = presenter.GetSessionSnapshot(ModuleIds.Tessera);
            _ = snap.Mode.Should().Be(TesseraFlyoutSessionMode.Single);
            _ = snap.EffectivelyShowing.Should().BeTrue();
            _ = snap.Generation.Should().Be(generation, "a patch on a visible flyout is not a new session");
            presenter.Hide(ModuleIds.Tessera);
        }

        [AvaloniaFact]
        public void A_style_change_while_visible_moves_the_session_to_the_new_style()
        {
            AvaloniaFlyoutPresenter presenter = NewPresenter();
            ShowAndWait(presenter, StackedEligible());

            presenter.Show(StackedEligible("Gnome"));
            _ = HeadlessPump.Until(() => presenter.GetSessionSnapshot(ModuleIds.Tessera).StyleId == "Gnome").Should().BeTrue();

            TesseraFlyoutSessionSnapshot snap = presenter.GetSessionSnapshot(ModuleIds.Tessera);
            _ = snap.Mode.Should().Be(TesseraFlyoutSessionMode.Single);
            _ = presenter.IsVisible(ModuleIds.Tessera).Should().BeTrue();
            presenter.Hide(ModuleIds.Tessera);
        }

        [AvaloniaFact]
        public void Hide_ends_the_session()
        {
            AvaloniaFlyoutPresenter presenter = NewPresenter();
            ShowAndWait(presenter, StackedEligible());

            presenter.Hide(ModuleIds.Tessera);

            _ = HeadlessPump.Until(() => !presenter.IsVisible(ModuleIds.Tessera)).Should().BeTrue();
            _ = presenter.GetSessionSnapshot(ModuleIds.Tessera).EffectivelyShowing.Should().BeFalse();
        }
    }
}
