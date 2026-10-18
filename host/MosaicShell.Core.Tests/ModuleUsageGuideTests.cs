using System.Text.Json;
using FluentAssertions;
using MosaicShell.Core.Modules;
using MosaicShell.Core.Runtime;
using MosaicShell.Core.Settings;

namespace MosaicShell.Core.Tests
{
    /// <summary>The how-to text the Hub shows for an armed module, against a private settings root.</summary>
    public class ModuleUsageGuideTests : IDisposable
    {
        private readonly string _root;

        public ModuleUsageGuideTests()
        {
            _root = Path.Combine(Path.GetTempPath(), "MosaicUsageGuide_" + Guid.NewGuid().ToString("N"));
            AppPaths.SetRootOverride(_root);
            AppPaths.EnsureLayout();
        }

        public void Dispose()
        {
            AppPaths.ClearRootOverride();
            try { Directory.Delete(_root, true); } catch { /* ignore */ }
        }

        private static void InstallManifest(string moduleId, ModuleManifest manifest)
        {
            string path = ModuleManifest.PathInModule(moduleId);
            _ = Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, JsonSerializer.Serialize(manifest));
        }

        [Theory]
        [InlineData("inlay", "Start-menu")]
        [InlineData("Chord", "Macro launcher")]
        [InlineData("SUBSTRATE", "Quick-settings")]
        [InlineData("slate", "Idle")]
        [InlineData("mixdeck", "Per-app volume mixer")]
        [InlineData("Tessera", "System flyouts")]
        public void Each_built_in_capability_has_its_own_summary_whatever_the_casing(string moduleId, string expectedFragment)
        {
            ModuleUsageGuide.Summary(moduleId).Should().Contain(expectedFragment);
        }

        [Fact]
        public void An_installed_module_summary_comes_from_its_manifest_before_its_description()
        {
            InstallManifest("Acme", new ModuleManifest { Id = "Acme", Description = "Catalog blurb", UsageSummary = "Does the thing." });

            ModuleUsageGuide.Summary("Acme").Should().Be("Does the thing.");
        }

        [Fact]
        public void An_installed_module_without_a_usage_summary_falls_back_to_its_description()
        {
            InstallManifest("Acme", new ModuleManifest { Id = "Acme", Description = "Catalog blurb" });

            ModuleUsageGuide.Summary("Acme").Should().Be("Catalog blurb");
        }

        [Fact]
        public void An_unknown_module_has_an_empty_summary_and_trigger()
        {
            ModuleUsageGuide.Summary("Nope").Should().BeEmpty();
            ModuleUsageGuide.HowToTrigger("Nope").Should().BeEmpty();
        }

        [Fact]
        public void A_widget_built_in_is_described_but_has_no_trigger()
        {
            ModuleUsageGuide.Summary("Pulse").Should().Be("Audio visualizer.");
            ModuleUsageGuide.HowToTrigger("Pulse").Should().BeEmpty("a widget is not armed from Tiles");
        }

        [Fact]
        public void A_hotkey_module_tells_the_user_which_gesture_to_press()
        {
            ModuleSettingsStore.Save("Inlay", new InlaySettings { HotkeyGesture = "Ctrl+Alt+J" });

            ModuleUsageGuide.HowToTrigger("Inlay").Should().Be("Arm from Tiles, then press Ctrl+Alt+J.");
        }

        [Fact]
        public void An_os_reserved_hotkey_is_shown_as_the_safe_default_that_will_actually_register()
        {
            ModuleSettingsStore.Save("Chord", new ChordSettings { HotkeyGesture = "Win+S" });

            ModuleUsageGuide.CurrentHotkey("Chord").Should().Be("Ctrl+Alt+K");
        }

        [Fact]
        public void An_unparseable_hotkey_is_shown_as_the_safe_default()
        {
            ModuleSettingsStore.Save("Mixdeck", new MixdeckSettings { HotkeyGesture = "not a gesture" });

            ModuleUsageGuide.CurrentHotkey("Mixdeck").Should().Be("Ctrl+Alt+M");
        }

        [Fact]
        public void A_blank_hotkey_tells_the_user_to_use_the_configured_one()
        {
            ModuleSettingsStore.Save("Substrate", new SubstrateSettings { HotkeyGesture = "" });

            ModuleUsageGuide.CurrentHotkey("Substrate").Should().BeEmpty();
            ModuleUsageGuide.HowToTrigger("Substrate").Should().Be("Arm from Tiles, then use the configured hotkey.");
        }

        [Theory]
        [InlineData("Tessera")]
        [InlineData("Pulse")]
        [InlineData("Slate")]
        [InlineData("Nope")]
        public void Modules_without_a_hotkey_have_no_current_hotkey(string moduleId)
        {
            ModuleUsageGuide.CurrentHotkey(moduleId).Should().BeEmpty();
        }

        [Theory]
        [InlineData(0, 30)]
        [InlineData(10, 30)]
        [InlineData(45, 45)]
        [InlineData(600, 600)]
        public void Slate_idle_time_is_never_shorter_than_thirty_seconds(int configured, int shown)
        {
            ModuleSettingsStore.Save("Slate", new SlateSettings { IdleSeconds = configured });

            ModuleUsageGuide.HowToTrigger("Slate").Should().Be($"Arm from Tiles, then leave the PC idle for {shown} seconds.");
            ModuleUsageGuide.ArmedStatus("Slate").Should().Be($"Armed - idle {shown}s");
        }

        [Fact]
        public void Tessera_is_triggered_by_system_events_not_a_hotkey()
        {
            ModuleUsageGuide.HowToTrigger("Tessera").Should().Contain("volume").And.Contain("Try now");
            ModuleUsageGuide.ArmedStatus("tessera").Should().Be("Armed - volume / media");
        }

        [Fact]
        public void The_armed_status_of_a_hotkey_module_names_its_gesture()
        {
            ModuleSettingsStore.Save("Inlay", new InlaySettings { HotkeyGesture = "Ctrl+Alt+J" });

            ModuleUsageGuide.ArmedStatus("Inlay").Should().Be("Armed - Ctrl+Alt+J");
        }

        [Fact]
        public void An_installed_capability_without_a_manifest_trigger_gets_the_generic_one()
        {
            InstallManifest("Acme", new ModuleManifest { Id = "Acme", Kind = "Capability" });

            ModuleUsageGuide.HowToTrigger("Acme").Should().Be("Arm from Tiles, then use the module's configured trigger.");
            ModuleUsageGuide.ArmedStatus("Acme").Should().Be("Armed");
        }

        [Fact]
        public void An_installed_module_trigger_comes_from_its_manifest()
        {
            InstallManifest("Acme", new ModuleManifest { Id = "Acme", HowToTrigger = "Double-tap the logo." });

            ModuleUsageGuide.HowToTrigger("Acme").Should().Be("Double-tap the logo.");
        }
    }
}
