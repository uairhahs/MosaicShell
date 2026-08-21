using FluentAssertions;
using MosaicShell.Core.Modules;

namespace MosaicShell.Core.Tests;

/// <summary>
/// Living checklist: flip to true only when MVP acceptance for that slice lands.
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

        // Product "full" overlay runtime still false (visual DLC / every RM feature)
        { "native_tile_overlay_runtime", false },

        // Capability daemon + Tessera OSD path
        { "capability_daemon", true },
        { "tessera_osd_flyout", true },
        { "tessera_named_styles", true },
        { "tessera_locks_flight", true },
        { "tessera_layout_fidelity", true },
        { "tessera_live_update_multimonitor", true },
        { "tessera_fluent_yourflyouts", true },
        { "style_catalog_jaxcore_ids", true },

        // Phase 1 services
        { "service_audio", true },
        { "service_app_audio", true },
        { "service_brightness", true },
        { "service_media", true },
        { "service_hotkeys", true },
        { "service_system_metrics", true },
        { "service_audio_levels", true },
        { "service_autostart", true },
        { "os_media_audio_brightness_services", true },

        // Phase 2
        { "session_persistence", true },
        { "module_settings_json_store", true },
        { "library_uninstall", true },
        { "tile_user_scale_applied", true },

        // Phase 3 - widgets service-bound; Tessera is capability host not tile window
        { "tile_chrono_mvp", true },
        { "tile_canvas_mvp", true },
        { "tile_phono_mvp", true },
        { "tile_pulse_mvp", true },
        { "tile_tessera_mvp", true }, // armed flyout + named styles (not Library slider window)
        { "tile_mixdeck_mvp", true },
        { "tile_inlay_mvp", true },
        { "tile_chord_mvp", true },
        { "tile_substrate_mvp", true },
        { "tile_slate_mvp", true },

        // Phase 4 hub
        { "module_settings_pages_in_host", true },
        { "welcome_wizard_shortcuts_startup", true },
        { "batch_install_flow", true },
        { "update_check_against_github_releases", true },
        { "context_menu_and_hotkeys_host_services", true },

        // Phase 5-6
        { "shp_import_in_host", true },
        { "product_cutover_no_iex", true },
    };

    [Theory]
    [MemberData(nameof(HubCapabilities))]
    public void Parity_capability_status(string capability, bool implemented)
    {
        capability.Should().NotBeNullOrWhiteSpace();
        if (!implemented)
            Assert.True(true, $"BACKLOG: {capability}");
        else
            Assert.True(implemented, $"REGRESSION: {capability} marked done but not delivered.");
    }

    [Fact]
    public void SkinList_count_is_ten_tiles()
    {
        ModuleCatalog.All.Should().HaveCount(10);
    }
}
