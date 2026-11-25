using FluentAssertions;
using MosaicShell.Core.Capabilities.Platform;
using MosaicShell.Core.Modules.Tessera;
using MosaicShell.Core.Runtime;
using MosaicShell.Core.Settings;
using MosaicShell.Core.Styles;

namespace MosaicShell.Core.Tests
{
    public class TesseraNativeTranslucencyStackedPolicyTests : IDisposable
    {
        private readonly string _root;

        public TesseraNativeTranslucencyStackedPolicyTests()
        {
            _root = Path.Combine(Path.GetTempPath(), "MosaicNativeTranslucencyStacked_" + Guid.NewGuid().ToString("N"));
            AppPaths.SetRootOverride(_root);
            AppPaths.EnsureLayout();
            ModuleSettingsStore.Save("Tessera", new TesseraSettings());
            HostLaunchOptions.ResetForTests();
            HostLaunchOptions.Apply([]);
        }

        public void Dispose()
        {
            HostLaunchOptions.ResetForTests();
            HostLaunchOptions.Apply([]);
            AppPaths.ClearRootOverride();
            try { Directory.Delete(_root, true); } catch { /* ignore */ }
        }

        [WindowsFact]
        public void Unified_shell_styles_use_single_hwnd_acrylic_with_media_strip()
        {
            Dictionary<string, string> stacked = new() { ["showMediaStrip"] = "1", ["acrylic"] = "1" };
            HostLaunchOptions.Apply([HostLaunchOptions.TesseraNativeTranslucencyTrialFlag]);
            TesseraNativeTranslucencyTestHarness.EnableHubNativeTranslucency();

            _ = TesseraNativeTranslucencyStackedPolicy.UseMultiWindowFromPayload(stacked, StyleIds.Fluent).Should().BeFalse();
            _ = TesseraNativeTranslucencyPolicy.IsEligibleFromPayload(stacked, StyleIds.Fluent).Should().BeTrue();
            _ = TesseraFlyoutMaterialFactory.NativeTranslucencyEligibleFromPayload(stacked, StyleIds.Fluent).Should().BeTrue();

            _ = TesseraNativeTranslucencyStackedPolicy.UseMultiWindowFromPayload(stacked, StyleIds.Windows11).Should().BeFalse();
            _ = TesseraNativeTranslucencyPolicy.IsEligibleFromPayload(stacked, StyleIds.Windows11).Should().BeTrue();

            _ = TesseraNativeTranslucencyStackedPolicy.UseMultiWindowFromPayload(stacked, StyleIds.PlainText).Should().BeFalse();
            _ = TesseraNativeTranslucencyStackedPolicy.UseMultiWindowFromPayload(stacked, StyleIds.Radial).Should().BeFalse();
        }

        [WindowsFact]
        public void Split_styles_keep_n_window_path_and_block_single_shell()
        {
            Dictionary<string, string> stacked = new() { ["showMediaStrip"] = "1", ["acrylic"] = "1" };
            HostLaunchOptions.Apply([HostLaunchOptions.TesseraNativeTranslucencyTrialFlag]);
            TesseraNativeTranslucencyTestHarness.EnableHubNativeTranslucency();

            _ = TesseraNativeTranslucencyStackedPolicy.UseMultiWindowFromPayload(stacked, StyleIds.Meter).Should().BeTrue();
            _ = TesseraNativeTranslucencyPolicy.IsEligibleFromPayload(stacked, StyleIds.Meter).Should().BeFalse();
        }

        [WindowsFact]
        public void Stacked_acrylic_is_mutually_exclusive_with_single_shell_eligible_on_split_styles()
        {
            Dictionary<string, string> stacked = new() { ["showMediaStrip"] = "1", ["acrylic"] = "1" };
            HostLaunchOptions.Apply([HostLaunchOptions.TesseraNativeTranslucencyTrialFlag]);
            TesseraNativeTranslucencyTestHarness.EnableHubNativeTranslucency();

            _ = TesseraNativeTranslucencyPolicy.IsEligibleFromPayload(stacked, StyleIds.Meter).Should().BeFalse();
            _ = TesseraNativeTranslucencyStackedPolicy.UseMultiWindowFromPayload(stacked, StyleIds.Meter).Should().BeTrue();
        }

        [WindowsFact]
        public void Single_shell_stays_on_single_window_acrylic_path()
        {
            Dictionary<string, string> single = new() { ["showMediaStrip"] = "0", ["acrylic"] = "1" };
            HostLaunchOptions.Apply([HostLaunchOptions.TesseraNativeTranslucencyTrialFlag]);
            TesseraNativeTranslucencyTestHarness.EnableHubNativeTranslucency();

            _ = TesseraNativeTranslucencyPolicy.IsEligibleFromPayload(single).Should().BeTrue();
            _ = TesseraNativeTranslucencyStackedPolicy.UseMultiWindowFromPayload(single, StyleIds.Fluent).Should().BeFalse();
        }

        [WindowsFact]
        public void Stacked_acrylic_requires_trial_opt_in_and_acrylic_setting()
        {
            Dictionary<string, string> stacked = new() { ["showMediaStrip"] = "1", ["acrylic"] = "1" };

            _ = TesseraNativeTranslucencyStackedPolicy.UseMultiWindowFromPayload(stacked, StyleIds.Meter).Should().BeFalse();

            HostLaunchOptions.Apply([HostLaunchOptions.TesseraNativeTranslucencyTrialFlag]);
            TesseraNativeTranslucencyTestHarness.EnableHubNativeTranslucency();
            _ = TesseraNativeTranslucencyStackedPolicy.UseMultiWindowFromPayload(stacked, StyleIds.Meter).Should().BeTrue();

            Dictionary<string, string> frost = new() { ["showMediaStrip"] = "1", ["acrylic"] = "0" };
            _ = TesseraNativeTranslucencyStackedPolicy.UseMultiWindowFromPayload(frost, StyleIds.Meter).Should().BeFalse();
        }

        [WindowsFact]
        public void Stacked_acrylic_can_be_enabled_from_persisted_hub_setting()
        {
            Dictionary<string, string> stacked = new() { ["showMediaStrip"] = "1", ["acrylic"] = "1" };
            ModuleSettingsStore.Save("Tessera", new TesseraSettings { UseNativeTranslucency = true });

            _ = TesseraNativeTranslucencyStackedPolicy.UseMultiWindowFromPayload(stacked, StyleIds.Meter).Should().BeTrue();
        }

        [Fact]
        public void Force_software_render_keeps_stacked_on_frost()
        {
            Dictionary<string, string> stacked = new() { ["showMediaStrip"] = "1", ["acrylic"] = "1" };
            TesseraNativeTranslucencyTestHarness.EnableHubNativeTranslucency();
            HostLaunchOptions.Apply([HostLaunchOptions.TesseraForceSoftwareRenderFlag]);

            _ = TesseraNativeTranslucencyStackedPolicy.UseMultiWindowFromPayload(stacked, StyleIds.Fluent).Should().BeFalse();
        }

        [Fact]
        public void Resolve_volume_media_panels_returns_volume_and_media()
        {
            _ = TesseraNativeTranslucencyStackedPolicy.ResolveVolumeMediaPanels(StyleIds.Fluent)
                .Should().Equal(TesseraStackedPanelRole.Volume, TesseraStackedPanelRole.Media);
            _ = TesseraNativeTranslucencyStackedPolicy.ResolvePanels(
                    new Dictionary<string, string> { ["showMediaStrip"] = "1" },
                    StyleIds.Meter)
                .Should().Equal(TesseraStackedPanelRole.Volume, TesseraStackedPanelRole.Media);
        }

        [Fact]
        public void CoreUI_multi_window_stays_off_until_phase_two()
        {
            _ = TesseraNativeTranslucencyStackedPolicy.CoreUiMultiWindowEnabled.Should().BeFalse();
            _ = TesseraNativeTranslucencyStackedPolicy.IsCoreUiMultiTile(StyleIds.CoreUI).Should().BeFalse();

            HostLaunchOptions.Apply([HostLaunchOptions.TesseraNativeTranslucencyTrialFlag]);
            TesseraNativeTranslucencyTestHarness.EnableHubNativeTranslucency();
            Dictionary<string, string> payload = new() { ["showMediaStrip"] = "0", ["acrylic"] = "1" };
            _ = TesseraNativeTranslucencyStackedPolicy.UseMultiWindowFromPayload(payload, StyleIds.CoreUI).Should().BeFalse();
        }

        [Fact]
        public void Window_slot_keys_are_stable()
        {
            _ = TesseraNativeTranslucencyStackedPolicy.WindowSlotKey("Tessera", TesseraStackedPanelRole.Volume)
                .Should().Be("Tessera:vol");
            _ = TesseraNativeTranslucencyStackedPolicy.WindowSlotKey("Tessera", TesseraStackedPanelRole.Media)
                .Should().Be("Tessera:media");
        }

        [Fact]
        public void Volume_slot_owns_live_host_and_wins_z_order()
        {
            _ = TesseraNativeTranslucencyStackedPolicy.ZOrderRank(TesseraStackedPanelRole.Volume)
                .Should().BeGreaterThan(TesseraNativeTranslucencyStackedPolicy.ZOrderRank(TesseraStackedPanelRole.Media));
        }

        [Fact]
        public void Transient_dismiss_hides_every_stacked_slot()
        {
            _ = TesseraNativeTranslucencyStackedPolicy.TransientDismissMustHideAllSlots.Should().BeTrue();
            _ = TesseraNativeTranslucencyStackedPolicy.SupersededSingleHwndMustCancelDismissBeforeClose.Should().BeTrue();
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Stacked_slots_stay_presenter_driven_whether_or_not_the_entrance_replays(bool entranceReplays)
        {
            // ApplyRequest clears the window's flag, and only an entrance replay set it again. A slot rebuilt while
            // visible then ran its own exit after the presenter's, so it faded out twice (KDE, 2026-11-23).
            _ = TesseraNativeTranslucencyStackedPolicy.SlotMotionIsPresenterDriven(entranceReplays).Should().BeTrue();
        }

        [Fact]
        public void Superseded_status_dismiss_must_not_cascade_into_stacked_volume()
        {
            string vol = TesseraNativeTranslucencyStackedPolicy.WindowSlotKey("Tessera", TesseraStackedPanelRole.Volume);
            string media = TesseraNativeTranslucencyStackedPolicy.WindowSlotKey("Tessera", TesseraStackedPanelRole.Media);
            string[] live = [vol, media];

            _ = TesseraNativeTranslucencyStackedPolicy
                .ShouldCascadeTransientDismissToStackedSession("Tessera", live)
                .Should().BeFalse("CapsLock HWND key must not dismiss volume/media after handoff");
            _ = TesseraNativeTranslucencyStackedPolicy
                .ShouldCascadeTransientDismissToStackedSession(vol, live)
                .Should().BeTrue();
            _ = TesseraNativeTranslucencyStackedPolicy
                .ShouldCascadeTransientDismissToStackedSession(media, live)
                .Should().BeTrue();
            _ = TesseraNativeTranslucencyStackedPolicy
                .ShouldCascadeTransientDismissToStackedSession(null, live)
                .Should().BeFalse();
            _ = TesseraStatusFlyoutPolicy.SupersededStatusMustCancelDismissBeforeClose.Should().BeTrue();
        }

        [Fact]
        public void Stacked_hwnd_transient_dismiss_notifies_slot_key_consumers_get_module_id()
        {
            _ = TesseraNativeTranslucencyStackedPolicy
                .ResolveTransientDismissNotifyKey("Tessera", TesseraStackedPanelRole.Volume)
                .Should().Be("Tessera:vol");
            _ = TesseraNativeTranslucencyStackedPolicy
                .ResolveTransientDismissNotifyKey("Tessera", TesseraStackedPanelRole.Media)
                .Should().Be("Tessera:media");
            _ = TesseraNativeTranslucencyStackedPolicy
                .ResolveTransientDismissNotifyKey("Tessera", role: null)
                .Should().Be("Tessera");

            _ = TesseraNativeTranslucencyStackedPolicy.ResolveTransientDismissConsumerKey("Tessera:vol")
                .Should().Be("Tessera");
            _ = TesseraNativeTranslucencyStackedPolicy.ResolveTransientDismissConsumerKey("Tessera:media")
                .Should().Be("Tessera");
            _ = TesseraNativeTranslucencyStackedPolicy.ResolveTransientDismissConsumerKey("Tessera")
                .Should().Be("Tessera");

            string[] live =
            [
                TesseraNativeTranslucencyStackedPolicy.WindowSlotKey("Tessera", TesseraStackedPanelRole.Volume),
                TesseraNativeTranslucencyStackedPolicy.WindowSlotKey("Tessera", TesseraStackedPanelRole.Media),
            ];
            string volumeNotify = TesseraNativeTranslucencyStackedPolicy.ResolveTransientDismissNotifyKey(
                "Tessera", TesseraStackedPanelRole.Volume);
            _ = TesseraNativeTranslucencyStackedPolicy
                .ShouldCascadeTransientDismissToStackedSession(volumeNotify, live)
                .Should().BeTrue();
            _ = TesseraNativeTranslucencyStackedPolicy
                .ShouldCascadeTransientDismissToStackedSession("Tessera", live)
                .Should().BeFalse();
        }

        [WindowsFact]
        public void Stacked_volume_media_requests_acrylic_blur_on_each_hwnd()
        {
            HostLaunchOptions.Apply([HostLaunchOptions.TesseraNativeTranslucencyTrialFlag]);
            TesseraNativeTranslucencyTestHarness.EnableHubNativeTranslucency();
            Dictionary<string, string> stacked = new() { ["showMediaStrip"] = "1", ["acrylic"] = "1" };
            _ = TesseraFlyoutMaterialFactory.NativeTranslucencyEligibleFromPayload(stacked, StyleIds.Meter).Should().BeTrue();
            TesseraFlyoutMaterial m = TesseraFlyoutMaterialFactory.FromPayload(stacked, StyleIds.Meter);
            _ = m.TransparencyHints.Should().Equal("AcrylicBlur", "Transparent");
        }

        [Theory]
        [InlineData(StyleIds.Meter, TesseraStackedPanelRole.Volume, 28, 200, 14f)]
        [InlineData(StyleIds.Meter, TesseraStackedPanelRole.Media, 220, 236, 16f)]
        [InlineData(StyleIds.Gnome, TesseraStackedPanelRole.Volume, 240, 48, 24f)]
        [InlineData(StyleIds.Gnome, TesseraStackedPanelRole.Media, 340, 64, 28f)]
        public void Resolve_panel_corner_radius_matches_pill_geometry(
            string styleId,
            TesseraStackedPanelRole role,
            double widthDip,
            double heightDip,
            float expected)
        {
            _ = TesseraNativeTranslucencyStackedPolicy.ResolvePanelCornerRadiusDip(styleId, role, widthDip, heightDip)
                .Should().Be(expected);
        }

        [Fact]
        public void Cluster_relayout_allows_meter_volume_spine_width()
        {
            _ = TesseraNativeTranslucencyStackedPolicy.ClusterRelayoutMinWidthDip.Should().BeLessThan(
                TesseraStackedPlacementSpec.MeterVolumeWidthDip);
            _ = TesseraFlyoutWindowPolicy.MeetsRelayoutSizeGate(
                    TesseraStackedPlacementSpec.MeterVolumeWidthDip,
                    TesseraStackedPlacementSpec.MeterVolumeHeightDip,
                    stackedClusterPanel: true)
                .Should().BeTrue();
            _ = TesseraFlyoutWindowPolicy.MeetsRelayoutSizeGate(
                    TesseraStackedPlacementSpec.MeterVolumeWidthDip,
                    TesseraStackedPlacementSpec.MeterVolumeHeightDip,
                    stackedClusterPanel: false)
                .Should().BeFalse();
        }
    }
}
