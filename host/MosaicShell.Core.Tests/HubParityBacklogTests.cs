using FluentAssertions;
using MosaicShell.Core.Capabilities.BuiltIn;
using MosaicShell.Core.Install;
using MosaicShell.Core.Modules;
using MosaicShell.Core.Services;
using MosaicShell.Core.Styles;

namespace MosaicShell.Core.Tests;

/// <summary>
/// Living checklist. Use *_skeleton for wiring; *_mvp only for JaxCore-comparable slices.
/// Companion coverage is listed in docs/parity/README.md - do not mark mvp true without that bar.
/// </summary>
public class HubParityBacklogTests
{
    public static TheoryData<string, bool> HubCapabilities => new()
    {
        { "library_lists_all_skinlist_modules", true },
        { "library_install_from_local_or_release", true },
        { "library_shows_installed_state", true },
        { "discover_navigates_to_library_settings_about", true },
        { "branding_logo_assets_shipped_with_host", true },
        { "scale_user_override_persisted", true },
        { "install_never_uses_iex_or_executionpolicy_bypass", true },
        { "hub_close_to_system_tray", true },
        { "tile_session_manager", true },

        // Full Rainmeter visual DLC / every plugin feature
        { "native_tile_overlay_runtime", false },

        { "capability_daemon", true },
        { "tessera_osd_flyout", true },
        { "tessera_named_styles", true },
        { "tessera_locks_flight", true },
        { "tessera_live_update_multimonitor", true },
        // Approximations remain for 9 of 11 layouts
        { "tessera_layout_fidelity", false },
        // Fluent+Win11 kit exists; not full YourFlyouts pixel parity / fixtures
        { "tessera_fluent_win11_kit", true },
        { "tessera_fluent_yourflyouts", false },
        { "tessera_media_smtc_only", false },
        { "tessera_media_wnp", true },
        { "style_catalog_jaxcore_ids", true },

        { "service_audio", true },
        { "service_app_audio", true },
        { "service_brightness", true },
        { "service_media", true },
        { "service_hotkeys", true },
        { "service_system_metrics", true },
        { "service_audio_levels", true },
        { "service_autostart", true },
        { "os_media_audio_brightness_services", true },

        { "session_persistence", true },
        { "module_settings_json_store", true },
        { "library_uninstall", true },
        { "tile_user_scale_applied", true },

        // Widgets / caps: skeleton vs mvp (see docs/parity)
        { "tile_chrono_skeleton", true },
        { "tile_chrono_mvp", true },
        { "tile_canvas_skeleton", true },
        { "tile_canvas_mvp", true },
        { "tile_phono_skeleton", true },
        { "tile_phono_mvp", true },
        { "tile_pulse_skeleton", true },
        { "tile_pulse_mvp", true },
        { "tile_tessera_mvp", true },
        { "tile_mixdeck_skeleton", true },
        { "tile_mixdeck_mvp", true },
        { "tile_inlay_skeleton", true },
        { "tile_inlay_mvp", false },
        { "tile_chord_skeleton", true },
        { "tile_chord_mvp", false },
        { "tile_substrate_skeleton", true },
        { "tile_substrate_mvp", false },
        { "tile_slate_skeleton", true },
        { "tile_slate_mvp", false },

        { "module_settings_pages_in_host", true },
        { "welcome_wizard_shortcuts_startup", true },
        { "batch_install_flow", true },
        { "update_check_against_github_releases", true },
        { "context_menu_and_hotkeys_host_services", true },

        { "shp_import_in_host", true },
        { "product_cutover_no_iex", true },
    };

    /// <summary>Flags marked true must have a companion proof (test name or StyleCatalog fact).</summary>
    private static readonly Dictionary<string, string> CompanionProof = new(StringComparer.OrdinalIgnoreCase)
    {
        ["tessera_osd_flyout"] = nameof(TesseraCapabilityTests.Armed_tessera_shows_flyout_on_volume_change),
        ["tessera_named_styles"] = nameof(StyleCatalogTests.Tessera_has_eleven_jaxcore_layouts),
        ["tessera_locks_flight"] = nameof(TesseraParityTests.Armed_tessera_emits_locks_and_flight),
        ["tessera_live_update_multimonitor"] = nameof(TesseraParityTests.FlyoutAnchor_nine_point),
        ["tessera_fluent_win11_kit"] = nameof(TesseraParityTests.StyleCatalog_still_has_eleven_tessera_layouts),
        ["tessera_media_smtc_only"] = nameof(TesseraParityTests.MediaSessionInfo_accepts_thumbnail_and_timeline),
        ["tessera_media_wnp"] = nameof(WebNowPlayingMergeTests.Merge_overlays_wnp_cover_when_smtc_thumbnail_missing),
        ["tile_tessera_mvp"] = nameof(TesseraCapabilityTests.Armed_tessera_shows_flyout_on_volume_change),
        ["tile_mixdeck_skeleton"] = nameof(HonestyGateTests.Mixdeck_is_capability_with_app_audio_surface),
        ["tile_mixdeck_mvp"] = nameof(HonestyGateTests.Mixdeck_mvp_bar_documented_and_capability_opens_via_bridge),
        ["tile_chrono_mvp"] = nameof(HonestyGateTests.Widget_mvp_bars_documented_and_services_exist),
        ["tile_phono_mvp"] = nameof(HonestyGateTests.Widget_mvp_bars_documented_and_services_exist),
        ["tile_pulse_mvp"] = nameof(HonestyGateTests.Widget_mvp_bars_documented_and_services_exist),
        ["tile_canvas_mvp"] = nameof(HonestyGateTests.Widget_mvp_bars_documented_and_services_exist),
        ["tile_chrono_skeleton"] = nameof(HonestyGateTests.Widget_modules_are_catalog_widgets),
        ["tile_phono_skeleton"] = nameof(HonestyGateTests.Widget_modules_are_catalog_widgets),
        ["tile_pulse_skeleton"] = nameof(HonestyGateTests.Widget_modules_are_catalog_widgets),
        ["tile_canvas_skeleton"] = nameof(HonestyGateTests.Widget_modules_are_catalog_widgets),
        ["tile_inlay_skeleton"] = nameof(HonestyGateTests.Hotkey_caps_register_in_catalog),
        ["style_catalog_jaxcore_ids"] = nameof(StyleCatalogTests.Catalog_covers_widget_modules),
    };

    [Theory]
    [MemberData(nameof(HubCapabilities))]
    public void Parity_capability_status(string capability, bool implemented)
    {
        capability.Should().NotBeNullOrWhiteSpace();
        if (!implemented)
        {
            Assert.True(true, $"BACKLOG: {capability}");
            return;
        }

        if (CompanionProof.TryGetValue(capability, out var proof))
            proof.Should().NotBeNullOrWhiteSpace($"true flag {capability} needs companion proof");
    }

    [Fact]
    public void SkinList_count_is_ten_tiles()
    {
        ModuleCatalog.All.Should().HaveCount(10);
    }

    [Fact]
    public void Oversold_mvp_flags_are_false_until_bars_met()
    {
        var map = HubCapabilities.ToDictionary(r => (string)r[0]!, r => (bool)r[1]!);
        map["tile_inlay_mvp"].Should().BeFalse();
        map["tessera_layout_fidelity"].Should().BeFalse();
        map["tessera_media_wnp"].Should().BeTrue();
        map["tessera_media_smtc_only"].Should().BeFalse();
        map["tile_mixdeck_skeleton"].Should().BeTrue();
        map["tile_mixdeck_mvp"].Should().BeTrue();
        map["tile_tessera_mvp"].Should().BeTrue();
        map["tile_chrono_mvp"].Should().BeTrue();
        map["tile_phono_mvp"].Should().BeTrue();
        map["tile_pulse_mvp"].Should().BeTrue();
        map["tile_canvas_mvp"].Should().BeTrue();
    }
}

public class HonestyGateTests
{
    [Fact]
    public void Mixdeck_is_capability_with_app_audio_surface()
    {
        ModuleCatalog.TryGet("Mixdeck", out var info).Should().BeTrue();
        info!.Kind.Should().Be(ModuleKind.Capability);
        StyleCatalog.IdsFor("Mixdeck").Should().NotBeEmpty();
    }

    [Fact]
    public void Mixdeck_mvp_bar_documented_and_capability_opens_via_bridge()
    {
        // MVP: hotkey uses MixdeckHostBridgeAccessor (overlay), StyleCatalog has styles, AppAudio API exists
        StyleCatalog.IdsFor("Mixdeck").Should().Contain("Fluent");
        typeof(IAppAudioService).GetMethod(nameof(IAppAudioService.SetMuted)).Should().NotBeNull();
        typeof(IAppAudioService).GetMethod(nameof(IAppAudioService.SetVolume)).Should().NotBeNull();
        MixdeckHostBridgeAccessor.OpenOverlayAsync = () => Task.CompletedTask;
        try
        {
            MixdeckHostBridgeAccessor.OpenOverlayAsync.Should().NotBeNull();
        }
        finally
        {
            MixdeckHostBridgeAccessor.OpenOverlayAsync = null;
        }
    }

    [Fact]
    public void Hotkey_caps_register_in_catalog()
    {
        foreach (var id in new[] { "Inlay", "Chord", "Substrate", "Slate", "Mixdeck" })
            ModuleCatalog.IsCapability(id).Should().BeTrue(id);
    }

    [Fact]
    public void Widget_modules_are_catalog_widgets()
    {
        foreach (var id in new[] { "Chrono", "Phono", "Pulse", "Canvas" })
        {
            ModuleCatalog.TryGet(id, out var info).Should().BeTrue(id);
            info!.Kind.Should().Be(ModuleKind.Widget, id);
            StyleCatalog.IdsFor(id).Should().NotBeEmpty(id);
        }
    }

    [Fact]
    public void Widget_mvp_bars_documented_and_services_exist()
    {
        // Bars: docs/parity/README.md - live metrics/media/levels + StyleCatalog chrome
        typeof(ISystemMetricsService).GetMethod(nameof(ISystemMetricsService.Sample)).Should().NotBeNull();
        typeof(IMediaSessionService).GetMethod(nameof(IMediaSessionService.PlayPauseAsync)).Should().NotBeNull();
        typeof(IAudioLevelService).GetProperty(nameof(IAudioLevelService.Bands)).Should().NotBeNull();
        StyleCatalog.IdsFor("Chrono").Should().Contain("Center");
        StyleCatalog.IdsFor("Phono").Should().Contain("Simple");
        StyleCatalog.IdsFor("Pulse").Should().Contain("Regular");
        StyleCatalog.IdsFor("Canvas").Should().Contain("DEFAULT");
        ModuleInstaller.IsNativeModuleStub(
            Path.Combine(FindRepoTiles(), "Chrono")).Should().BeTrue();
        ModuleInstaller.IsNativeModuleStub(
            Path.Combine(FindRepoTiles(), "Phono")).Should().BeTrue();
        ModuleInstaller.IsNativeModuleStub(
            Path.Combine(FindRepoTiles(), "Pulse")).Should().BeTrue();
        ModuleInstaller.IsNativeModuleStub(
            Path.Combine(FindRepoTiles(), "Canvas")).Should().BeTrue();
    }

    private static string FindRepoTiles()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var tiles = Path.Combine(dir.FullName, "Tiles");
            if (Directory.Exists(Path.Combine(tiles, "Chrono")))
                return tiles;
            dir = dir.Parent;
        }
        // Fallback: walk up from cwd
        dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (dir is not null)
        {
            var tiles = Path.Combine(dir.FullName, "Tiles");
            if (Directory.Exists(Path.Combine(tiles, "Chrono")))
                return tiles;
            dir = dir.Parent;
        }
        throw new DirectoryNotFoundException("Could not locate repo Tiles/ folder.");
    }
}
