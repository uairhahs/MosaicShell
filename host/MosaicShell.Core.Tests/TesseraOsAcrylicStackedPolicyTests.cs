using FluentAssertions;
using MosaicShell.Core.Capabilities.Platform;
using MosaicShell.Core.Modules.Tessera;
using MosaicShell.Core.Runtime;
using MosaicShell.Core.Settings;
using MosaicShell.Core.Styles;

namespace MosaicShell.Core.Tests
{
    public class TesseraOsAcrylicStackedPolicyTests : IDisposable
    {
        private readonly string _root;

        public TesseraOsAcrylicStackedPolicyTests()
        {
            _root = Path.Combine(Path.GetTempPath(), "MosaicOsAcrylicStacked_" + Guid.NewGuid().ToString("N"));
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

        [Fact]
        public void Unified_shell_styles_use_single_hwnd_acrylic_with_media_strip()
        {
            Dictionary<string, string> stacked = new() { ["showMediaStrip"] = "1", ["acrylic"] = "1" };
            HostLaunchOptions.Apply([HostLaunchOptions.TesseraOsAcrylicTrialFlag]);
            TesseraOsAcrylicTestHarness.EnableHubOsAcrylic();

            _ = TesseraOsAcrylicStackedPolicy.UseMultiWindowFromPayload(stacked, StyleIds.Fluent).Should().BeFalse();
            _ = TesseraOsAcrylicTrialPolicy.IsEligibleFromPayload(stacked, StyleIds.Fluent).Should().BeTrue();
            _ = TesseraFlyoutMaterialFactory.OsAcrylicEligibleFromPayload(stacked, StyleIds.Fluent).Should().BeTrue();

            _ = TesseraOsAcrylicStackedPolicy.UseMultiWindowFromPayload(stacked, StyleIds.Windows11).Should().BeFalse();
            _ = TesseraOsAcrylicTrialPolicy.IsEligibleFromPayload(stacked, StyleIds.Windows11).Should().BeTrue();

            _ = TesseraOsAcrylicStackedPolicy.UseMultiWindowFromPayload(stacked, StyleIds.PlainText).Should().BeFalse();
            _ = TesseraOsAcrylicStackedPolicy.UseMultiWindowFromPayload(stacked, StyleIds.Radial).Should().BeFalse();
        }

        [Fact]
        public void Split_styles_keep_n_window_path_and_block_single_shell()
        {
            Dictionary<string, string> stacked = new() { ["showMediaStrip"] = "1", ["acrylic"] = "1" };
            HostLaunchOptions.Apply([HostLaunchOptions.TesseraOsAcrylicTrialFlag]);
            TesseraOsAcrylicTestHarness.EnableHubOsAcrylic();

            _ = TesseraOsAcrylicStackedPolicy.UseMultiWindowFromPayload(stacked, StyleIds.Meter).Should().BeTrue();
            _ = TesseraOsAcrylicTrialPolicy.IsEligibleFromPayload(stacked, StyleIds.Meter).Should().BeFalse();
        }

        [Fact]
        public void Stacked_acrylic_is_mutually_exclusive_with_single_shell_eligible_on_split_styles()
        {
            Dictionary<string, string> stacked = new() { ["showMediaStrip"] = "1", ["acrylic"] = "1" };
            HostLaunchOptions.Apply([HostLaunchOptions.TesseraOsAcrylicTrialFlag]);
            TesseraOsAcrylicTestHarness.EnableHubOsAcrylic();

            _ = TesseraOsAcrylicTrialPolicy.IsEligibleFromPayload(stacked, StyleIds.Meter).Should().BeFalse();
            _ = TesseraOsAcrylicStackedPolicy.UseMultiWindowFromPayload(stacked, StyleIds.Meter).Should().BeTrue();
        }

        [Fact]
        public void Single_shell_stays_on_single_window_acrylic_path()
        {
            Dictionary<string, string> single = new() { ["showMediaStrip"] = "0", ["acrylic"] = "1" };
            HostLaunchOptions.Apply([HostLaunchOptions.TesseraOsAcrylicTrialFlag]);
            TesseraOsAcrylicTestHarness.EnableHubOsAcrylic();

            _ = TesseraOsAcrylicTrialPolicy.IsEligibleFromPayload(single).Should().BeTrue();
            _ = TesseraOsAcrylicStackedPolicy.UseMultiWindowFromPayload(single, StyleIds.Fluent).Should().BeFalse();
        }

        [Fact]
        public void Stacked_acrylic_requires_trial_opt_in_and_acrylic_setting()
        {
            Dictionary<string, string> stacked = new() { ["showMediaStrip"] = "1", ["acrylic"] = "1" };

            _ = TesseraOsAcrylicStackedPolicy.UseMultiWindowFromPayload(stacked, StyleIds.Meter).Should().BeFalse();

            HostLaunchOptions.Apply([HostLaunchOptions.TesseraOsAcrylicTrialFlag]);
            TesseraOsAcrylicTestHarness.EnableHubOsAcrylic();
            _ = TesseraOsAcrylicStackedPolicy.UseMultiWindowFromPayload(stacked, StyleIds.Meter).Should().BeTrue();

            Dictionary<string, string> frost = new() { ["showMediaStrip"] = "1", ["acrylic"] = "0" };
            _ = TesseraOsAcrylicStackedPolicy.UseMultiWindowFromPayload(frost, StyleIds.Meter).Should().BeFalse();
        }

        [Fact]
        public void Stacked_acrylic_can_be_enabled_from_persisted_hub_setting()
        {
            Dictionary<string, string> stacked = new() { ["showMediaStrip"] = "1", ["acrylic"] = "1" };
            ModuleSettingsStore.Save("Tessera", new TesseraSettings { UseOsAcrylic = true });

            _ = TesseraOsAcrylicStackedPolicy.UseMultiWindowFromPayload(stacked, StyleIds.Meter).Should().BeTrue();
        }

        [Fact]
        public void Force_software_render_keeps_stacked_on_frost()
        {
            Dictionary<string, string> stacked = new() { ["showMediaStrip"] = "1", ["acrylic"] = "1" };
            TesseraOsAcrylicTestHarness.EnableHubOsAcrylic();
            HostLaunchOptions.Apply([HostLaunchOptions.TesseraForceSoftwareRenderFlag]);

            _ = TesseraOsAcrylicStackedPolicy.UseMultiWindowFromPayload(stacked, StyleIds.Fluent).Should().BeFalse();
        }

        [Fact]
        public void Resolve_volume_media_panels_returns_volume_and_media()
        {
            _ = TesseraOsAcrylicStackedPolicy.ResolveVolumeMediaPanels(StyleIds.Fluent)
                .Should().Equal(TesseraStackedPanelRole.Volume, TesseraStackedPanelRole.Media);
            _ = TesseraOsAcrylicStackedPolicy.ResolvePanels(
                    new Dictionary<string, string> { ["showMediaStrip"] = "1" },
                    StyleIds.Meter)
                .Should().Equal(TesseraStackedPanelRole.Volume, TesseraStackedPanelRole.Media);
        }

        [Fact]
        public void CoreUI_multi_window_stays_off_until_phase_two()
        {
            _ = TesseraOsAcrylicStackedPolicy.CoreUiMultiWindowEnabled.Should().BeFalse();
            _ = TesseraOsAcrylicStackedPolicy.IsCoreUiMultiTile(StyleIds.CoreUI).Should().BeFalse();

            HostLaunchOptions.Apply([HostLaunchOptions.TesseraOsAcrylicTrialFlag]);
            TesseraOsAcrylicTestHarness.EnableHubOsAcrylic();
            Dictionary<string, string> payload = new() { ["showMediaStrip"] = "0", ["acrylic"] = "1" };
            _ = TesseraOsAcrylicStackedPolicy.UseMultiWindowFromPayload(payload, StyleIds.CoreUI).Should().BeFalse();
        }

        [Fact]
        public void Window_slot_keys_are_stable()
        {
            _ = TesseraOsAcrylicStackedPolicy.WindowSlotKey("Tessera", TesseraStackedPanelRole.Volume)
                .Should().Be("Tessera:vol");
            _ = TesseraOsAcrylicStackedPolicy.WindowSlotKey("Tessera", TesseraStackedPanelRole.Media)
                .Should().Be("Tessera:media");
        }

        [Fact]
        public void Volume_slot_owns_live_host_and_wins_z_order()
        {
            _ = TesseraOsAcrylicStackedPolicy.RoleOwnsLiveHost(TesseraStackedPanelRole.Volume).Should().BeTrue();
            _ = TesseraOsAcrylicStackedPolicy.RoleOwnsLiveHost(TesseraStackedPanelRole.Media).Should().BeFalse();
            _ = TesseraOsAcrylicStackedPolicy.ZOrderRank(TesseraStackedPanelRole.Volume)
                .Should().BeGreaterThan(TesseraOsAcrylicStackedPolicy.ZOrderRank(TesseraStackedPanelRole.Media));
        }

        [Fact]
        public void Transient_dismiss_hides_every_stacked_slot()
        {
            _ = TesseraOsAcrylicStackedPolicy.TransientDismissMustHideAllSlots.Should().BeTrue();
            _ = TesseraOsAcrylicStackedPolicy.OutsideClickUsesUnionBounds.Should().BeTrue();
            _ = TesseraOsAcrylicStackedPolicy.SupersededSingleHwndMustCancelDismissBeforeClose.Should().BeTrue();
        }

        [Fact]
        public void Superseded_status_dismiss_must_not_cascade_into_stacked_volume()
        {
            string vol = TesseraOsAcrylicStackedPolicy.WindowSlotKey("Tessera", TesseraStackedPanelRole.Volume);
            string media = TesseraOsAcrylicStackedPolicy.WindowSlotKey("Tessera", TesseraStackedPanelRole.Media);
            string[] live = [vol, media];

            _ = TesseraOsAcrylicStackedPolicy
                .ShouldCascadeTransientDismissToStackedSession("Tessera", live)
                .Should().BeFalse("CapsLock HWND key must not dismiss volume/media after handoff");
            _ = TesseraOsAcrylicStackedPolicy
                .ShouldCascadeTransientDismissToStackedSession(vol, live)
                .Should().BeTrue();
            _ = TesseraOsAcrylicStackedPolicy
                .ShouldCascadeTransientDismissToStackedSession(media, live)
                .Should().BeTrue();
            _ = TesseraOsAcrylicStackedPolicy
                .ShouldCascadeTransientDismissToStackedSession(null, live)
                .Should().BeFalse();
            _ = TesseraStatusFlyoutPolicy.SupersededStatusMustCancelDismissBeforeClose.Should().BeTrue();
        }

        [Fact]
        public void Stacked_hwnd_transient_dismiss_notifies_slot_key_consumers_get_module_id()
        {
            _ = TesseraOsAcrylicStackedPolicy
                .ResolveTransientDismissNotifyKey("Tessera", TesseraStackedPanelRole.Volume)
                .Should().Be("Tessera:vol");
            _ = TesseraOsAcrylicStackedPolicy
                .ResolveTransientDismissNotifyKey("Tessera", TesseraStackedPanelRole.Media)
                .Should().Be("Tessera:media");
            _ = TesseraOsAcrylicStackedPolicy
                .ResolveTransientDismissNotifyKey("Tessera", role: null)
                .Should().Be("Tessera");

            _ = TesseraOsAcrylicStackedPolicy.ResolveTransientDismissConsumerKey("Tessera:vol")
                .Should().Be("Tessera");
            _ = TesseraOsAcrylicStackedPolicy.ResolveTransientDismissConsumerKey("Tessera:media")
                .Should().Be("Tessera");
            _ = TesseraOsAcrylicStackedPolicy.ResolveTransientDismissConsumerKey("Tessera")
                .Should().Be("Tessera");

            string[] live =
            [
                TesseraOsAcrylicStackedPolicy.WindowSlotKey("Tessera", TesseraStackedPanelRole.Volume),
                TesseraOsAcrylicStackedPolicy.WindowSlotKey("Tessera", TesseraStackedPanelRole.Media),
            ];
            string volumeNotify = TesseraOsAcrylicStackedPolicy.ResolveTransientDismissNotifyKey(
                "Tessera", TesseraStackedPanelRole.Volume);
            _ = TesseraOsAcrylicStackedPolicy
                .ShouldCascadeTransientDismissToStackedSession(volumeNotify, live)
                .Should().BeTrue();
            _ = TesseraOsAcrylicStackedPolicy
                .ShouldCascadeTransientDismissToStackedSession("Tessera", live)
                .Should().BeFalse();
        }

        [Fact]
        public void Stacked_volume_media_requests_acrylic_blur_on_each_hwnd()
        {
            HostLaunchOptions.Apply([HostLaunchOptions.TesseraOsAcrylicTrialFlag]);
            TesseraOsAcrylicTestHarness.EnableHubOsAcrylic();
            Dictionary<string, string> stacked = new() { ["showMediaStrip"] = "1", ["acrylic"] = "1" };
            _ = TesseraFlyoutMaterialFactory.OsAcrylicEligibleFromPayload(stacked, StyleIds.Meter).Should().BeTrue();
            TesseraFlyoutMaterial m = TesseraFlyoutMaterialFactory.FromPayload(stacked, StyleIds.Meter);
            _ = m.TransparencyHints.Should().Equal("AcrylicBlur", "Transparent");
        }

        [Fact]
        public void Outside_click_policy_uses_union_bounds_for_multi_window()
        {
            _ = TesseraFlyoutOutsideClickPolicy.MustUseUnionBounds(multiWindowStackedAcrylic: false).Should().BeFalse();
            _ = TesseraFlyoutOutsideClickPolicy.MustUseUnionBounds(multiWindowStackedAcrylic: true).Should().BeTrue();
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
            _ = TesseraOsAcrylicStackedPolicy.ResolvePanelCornerRadiusDip(styleId, role, widthDip, heightDip)
                .Should().Be(expected);
        }

        [Fact]
        public void Cluster_relayout_allows_meter_volume_spine_width()
        {
            _ = TesseraOsAcrylicStackedPolicy.ClusterRelayoutMinWidthDip.Should().BeLessThan(
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
