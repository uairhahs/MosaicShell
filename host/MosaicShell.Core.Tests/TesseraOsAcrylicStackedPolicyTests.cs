using FluentAssertions;
using MosaicShell.Core;
using MosaicShell.Core.Capabilities.Platform;
using MosaicShell.Core.Runtime;
using MosaicShell.Core.Settings;
using MosaicShell.Core.Styles;
using MosaicShell.Core.Modules.Tessera;

namespace MosaicShell.Core.Tests;

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
        HostLaunchOptions.Apply(Array.Empty<string>());
    }

    public void Dispose()
    {
        HostLaunchOptions.ResetForTests();
        HostLaunchOptions.Apply(Array.Empty<string>());
        AppPaths.ClearRootOverride();
        try { Directory.Delete(_root, true); } catch { /* ignore */ }
    }

    [Fact]
    public void Unified_shell_styles_use_single_hwnd_acrylic_with_media_strip()
    {
        var stacked = new Dictionary<string, string> { ["showMediaStrip"] = "1", ["acrylic"] = "1" };
        HostLaunchOptions.Apply([HostLaunchOptions.TesseraOsAcrylicTrialFlag]);
        TesseraOsAcrylicTestHarness.EnableHubOsAcrylic();

        TesseraOsAcrylicStackedPolicy.UseMultiWindowFromPayload(stacked, StyleIds.Fluent).Should().BeFalse();
        TesseraOsAcrylicTrialPolicy.IsEligibleFromPayload(stacked, StyleIds.Fluent).Should().BeTrue();
        TesseraFlyoutMaterialFactory.OsAcrylicEligibleFromPayload(stacked, StyleIds.Fluent).Should().BeTrue();

        TesseraOsAcrylicStackedPolicy.UseMultiWindowFromPayload(stacked, StyleIds.Windows11).Should().BeFalse();
        TesseraOsAcrylicTrialPolicy.IsEligibleFromPayload(stacked, StyleIds.Windows11).Should().BeTrue();

        TesseraOsAcrylicStackedPolicy.UseMultiWindowFromPayload(stacked, StyleIds.PlainText).Should().BeFalse();
        TesseraOsAcrylicStackedPolicy.UseMultiWindowFromPayload(stacked, StyleIds.Radial).Should().BeFalse();
    }

    [Fact]
    public void Split_styles_keep_n_window_path_and_block_single_shell()
    {
        var stacked = new Dictionary<string, string> { ["showMediaStrip"] = "1", ["acrylic"] = "1" };
        HostLaunchOptions.Apply([HostLaunchOptions.TesseraOsAcrylicTrialFlag]);
        TesseraOsAcrylicTestHarness.EnableHubOsAcrylic();

        TesseraOsAcrylicStackedPolicy.UseMultiWindowFromPayload(stacked, StyleIds.Meter).Should().BeTrue();
        TesseraOsAcrylicTrialPolicy.IsEligibleFromPayload(stacked, StyleIds.Meter).Should().BeFalse();
    }

    [Fact]
    public void Stacked_acrylic_is_mutually_exclusive_with_single_shell_eligible_on_split_styles()
    {
        var stacked = new Dictionary<string, string> { ["showMediaStrip"] = "1", ["acrylic"] = "1" };
        HostLaunchOptions.Apply([HostLaunchOptions.TesseraOsAcrylicTrialFlag]);
        TesseraOsAcrylicTestHarness.EnableHubOsAcrylic();

        TesseraOsAcrylicTrialPolicy.IsEligibleFromPayload(stacked, StyleIds.Meter).Should().BeFalse();
        TesseraOsAcrylicStackedPolicy.UseMultiWindowFromPayload(stacked, StyleIds.Meter).Should().BeTrue();
    }

    [Fact]
    public void Single_shell_stays_on_single_window_acrylic_path()
    {
        var single = new Dictionary<string, string> { ["showMediaStrip"] = "0", ["acrylic"] = "1" };
        HostLaunchOptions.Apply([HostLaunchOptions.TesseraOsAcrylicTrialFlag]);
        TesseraOsAcrylicTestHarness.EnableHubOsAcrylic();

        TesseraOsAcrylicTrialPolicy.IsEligibleFromPayload(single).Should().BeTrue();
        TesseraOsAcrylicStackedPolicy.UseMultiWindowFromPayload(single, StyleIds.Fluent).Should().BeFalse();
    }

    [Fact]
    public void Stacked_acrylic_requires_trial_opt_in_and_acrylic_setting()
    {
        var stacked = new Dictionary<string, string> { ["showMediaStrip"] = "1", ["acrylic"] = "1" };

        TesseraOsAcrylicStackedPolicy.UseMultiWindowFromPayload(stacked, StyleIds.Meter).Should().BeFalse();

        HostLaunchOptions.Apply([HostLaunchOptions.TesseraOsAcrylicTrialFlag]);
        TesseraOsAcrylicTestHarness.EnableHubOsAcrylic();
        TesseraOsAcrylicStackedPolicy.UseMultiWindowFromPayload(stacked, StyleIds.Meter).Should().BeTrue();

        var frost = new Dictionary<string, string> { ["showMediaStrip"] = "1", ["acrylic"] = "0" };
        TesseraOsAcrylicStackedPolicy.UseMultiWindowFromPayload(frost, StyleIds.Meter).Should().BeFalse();
    }

    [Fact]
    public void Stacked_acrylic_can_be_enabled_from_persisted_hub_setting()
    {
        var stacked = new Dictionary<string, string> { ["showMediaStrip"] = "1", ["acrylic"] = "1" };
        ModuleSettingsStore.Save("Tessera", new TesseraSettings { UseOsAcrylic = true });

        TesseraOsAcrylicStackedPolicy.UseMultiWindowFromPayload(stacked, StyleIds.Meter).Should().BeTrue();
    }

    [Fact]
    public void Force_software_render_keeps_stacked_on_frost()
    {
        var stacked = new Dictionary<string, string> { ["showMediaStrip"] = "1", ["acrylic"] = "1" };
        TesseraOsAcrylicTestHarness.EnableHubOsAcrylic();
        HostLaunchOptions.Apply([HostLaunchOptions.TesseraForceSoftwareRenderFlag]);

        TesseraOsAcrylicStackedPolicy.UseMultiWindowFromPayload(stacked, StyleIds.Fluent).Should().BeFalse();
    }

    [Fact]
    public void Resolve_volume_media_panels_returns_volume_and_media()
    {
        TesseraOsAcrylicStackedPolicy.ResolveVolumeMediaPanels(StyleIds.Fluent)
            .Should().Equal(TesseraStackedPanelRole.Volume, TesseraStackedPanelRole.Media);
        TesseraOsAcrylicStackedPolicy.ResolvePanels(
                new Dictionary<string, string> { ["showMediaStrip"] = "1" },
                StyleIds.Meter)
            .Should().Equal(TesseraStackedPanelRole.Volume, TesseraStackedPanelRole.Media);
    }

    [Fact]
    public void CoreUI_multi_window_stays_off_until_phase_two()
    {
        TesseraOsAcrylicStackedPolicy.CoreUiMultiWindowEnabled.Should().BeFalse();
        TesseraOsAcrylicStackedPolicy.IsCoreUiMultiTile(StyleIds.CoreUI).Should().BeFalse();

        HostLaunchOptions.Apply([HostLaunchOptions.TesseraOsAcrylicTrialFlag]);
        TesseraOsAcrylicTestHarness.EnableHubOsAcrylic();
        var payload = new Dictionary<string, string> { ["showMediaStrip"] = "0", ["acrylic"] = "1" };
        TesseraOsAcrylicStackedPolicy.UseMultiWindowFromPayload(payload, StyleIds.CoreUI).Should().BeFalse();
    }

    [Fact]
    public void Window_slot_keys_are_stable()
    {
        TesseraOsAcrylicStackedPolicy.WindowSlotKey("Tessera", TesseraStackedPanelRole.Volume)
            .Should().Be("Tessera:vol");
        TesseraOsAcrylicStackedPolicy.WindowSlotKey("Tessera", TesseraStackedPanelRole.Media)
            .Should().Be("Tessera:media");
    }

    [Fact]
    public void Volume_slot_owns_live_host_and_wins_z_order()
    {
        TesseraOsAcrylicStackedPolicy.RoleOwnsLiveHost(TesseraStackedPanelRole.Volume).Should().BeTrue();
        TesseraOsAcrylicStackedPolicy.RoleOwnsLiveHost(TesseraStackedPanelRole.Media).Should().BeFalse();
        TesseraOsAcrylicStackedPolicy.ZOrderRank(TesseraStackedPanelRole.Volume)
            .Should().BeGreaterThan(TesseraOsAcrylicStackedPolicy.ZOrderRank(TesseraStackedPanelRole.Media));
    }

    [Fact]
    public void Transient_dismiss_hides_every_stacked_slot()
    {
        TesseraOsAcrylicStackedPolicy.TransientDismissMustHideAllSlots.Should().BeTrue();
        TesseraOsAcrylicStackedPolicy.OutsideClickUsesUnionBounds.Should().BeTrue();
    }

    [Fact]
    public void Stacked_volume_media_requests_acrylic_blur_on_each_hwnd()
    {
        HostLaunchOptions.Apply([HostLaunchOptions.TesseraOsAcrylicTrialFlag]);
        TesseraOsAcrylicTestHarness.EnableHubOsAcrylic();
        var stacked = new Dictionary<string, string> { ["showMediaStrip"] = "1", ["acrylic"] = "1" };
        TesseraFlyoutMaterialFactory.OsAcrylicEligibleFromPayload(stacked, StyleIds.Meter).Should().BeTrue();
        var m = TesseraFlyoutMaterialFactory.FromPayload(stacked, StyleIds.Meter);
        m.TransparencyHints.Should().Equal("AcrylicBlur", "Transparent");
    }

    [Fact]
    public void Outside_click_policy_uses_union_bounds_for_multi_window()
    {
        TesseraFlyoutOutsideClickPolicy.MustUseUnionBounds(multiWindowStackedAcrylic: false).Should().BeFalse();
        TesseraFlyoutOutsideClickPolicy.MustUseUnionBounds(multiWindowStackedAcrylic: true).Should().BeTrue();
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
        TesseraOsAcrylicStackedPolicy.ResolvePanelCornerRadiusDip(styleId, role, widthDip, heightDip)
            .Should().Be(expected);
    }

    [Fact]
    public void Cluster_relayout_allows_meter_volume_spine_width()
    {
        TesseraOsAcrylicStackedPolicy.ClusterRelayoutMinWidthDip.Should().BeLessThan(
            TesseraStackedPlacementSpec.MeterVolumeWidthDip);
        TesseraFlyoutWindowPolicy.MeetsRelayoutSizeGate(
                TesseraStackedPlacementSpec.MeterVolumeWidthDip,
                TesseraStackedPlacementSpec.MeterVolumeHeightDip,
                stackedClusterPanel: true)
            .Should().BeTrue();
        TesseraFlyoutWindowPolicy.MeetsRelayoutSizeGate(
                TesseraStackedPlacementSpec.MeterVolumeWidthDip,
                TesseraStackedPlacementSpec.MeterVolumeHeightDip,
                stackedClusterPanel: false)
            .Should().BeFalse();
    }
}
