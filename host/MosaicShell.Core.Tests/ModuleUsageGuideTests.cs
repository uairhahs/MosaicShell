using System.Text.Json;
using FluentAssertions;
using MosaicShell.Core.Modules;
using MosaicShell.Core.Runtime;
using MosaicShell.Core.Settings;

namespace MosaicShell.Core.Tests
{
    /// <summary>
    /// The how-to text a module's page and its turn-on toast show, against a private settings root. U07: none of it says
    /// "arm", "Tiles", "capability" or "OSD", which name the old Hub, not anything the user can see.
    /// </summary>
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
        [InlineData("inlay", "start-menu")]
        [InlineData("Chord", "macro launcher")]
        [InlineData("SUBSTRATE", "quick-settings")]
        [InlineData("slate", "clock")]
        [InlineData("mixdeck", "volume mixer")]
        [InlineData("Tessera", "pop-ups")]
        public void Each_built_in_capability_has_its_own_summary_whatever_the_casing(string moduleId, string expectedFragment)
        {
            _ = ModuleUsageGuide.Summary(moduleId).Should().Contain(expectedFragment);
        }

        [Fact]
        public void An_installed_module_summary_comes_from_its_manifest_before_its_description()
        {
            InstallManifest("Acme", new ModuleManifest { Id = "Acme", Description = "Catalog blurb", UsageSummary = "Does the thing." });

            _ = ModuleUsageGuide.Summary("Acme").Should().Be("Does the thing.");
        }

        [Fact]
        public void An_installed_module_without_a_usage_summary_falls_back_to_its_description()
        {
            InstallManifest("Acme", new ModuleManifest { Id = "Acme", Description = "Catalog blurb" });

            _ = ModuleUsageGuide.Summary("Acme").Should().Be("Catalog blurb");
        }

        [Fact]
        public void An_unknown_module_has_an_empty_summary_and_trigger()
        {
            _ = ModuleUsageGuide.Summary("Nope").Should().BeEmpty();
            _ = ModuleUsageGuide.HowToTrigger("Nope").Should().BeEmpty();
        }

        [Fact]
        public void A_widget_built_in_is_described_but_has_no_trigger()
        {
            _ = ModuleUsageGuide.Summary("Pulse").Should().Be("Audio visualizer.");
            _ = ModuleUsageGuide.HowToTrigger("Pulse").Should().BeEmpty("a widget has nothing to press");
        }

        [Fact]
        public void A_hotkey_module_tells_the_user_which_gesture_to_press()
        {
            ModuleSettingsStore.Save("Inlay", new InlaySettings { HotkeyGesture = "Ctrl+Alt+J" });

            _ = ModuleUsageGuide.HowToTrigger("Inlay").Should().Be("Press Ctrl+Alt+J to open the launcher.");
        }

        [Fact]
        public void An_os_reserved_hotkey_is_shown_as_the_safe_default_that_will_actually_register()
        {
            ModuleSettingsStore.Save("Chord", new ChordSettings { HotkeyGesture = "Win+S" });

            _ = ModuleUsageGuide.CurrentHotkey("Chord").Should().Be("Ctrl+Alt+K");
        }

        [Fact]
        public void An_unparseable_hotkey_is_shown_as_the_safe_default()
        {
            ModuleSettingsStore.Save("Mixdeck", new MixdeckSettings { HotkeyGesture = "not a gesture" });

            _ = ModuleUsageGuide.CurrentHotkey("Mixdeck").Should().Be("Ctrl+Alt+X");
        }

        [Fact]
        public void A_blank_hotkey_tells_the_user_to_use_the_configured_one()
        {
            ModuleSettingsStore.Save("Substrate", new SubstrateSettings { HotkeyGesture = "" });

            _ = ModuleUsageGuide.CurrentHotkey("Substrate").Should().BeEmpty();
            _ = ModuleUsageGuide.HowToTrigger("Substrate").Should().Be("Set a shortcut below to open the quick settings.");
        }

        [Theory]
        [InlineData("Tessera")]
        [InlineData("Pulse")]
        [InlineData("Slate")]
        [InlineData("Nope")]
        public void Modules_without_a_hotkey_have_no_current_hotkey(string moduleId)
        {
            _ = ModuleUsageGuide.CurrentHotkey(moduleId).Should().BeEmpty();
        }

        [Theory]
        [InlineData(0, "30 s")]
        [InlineData(10, "30 s")]
        [InlineData(45, "45 s")]
        [InlineData(600, "10 min")]
        public void Slate_idle_time_is_never_shorter_than_thirty_seconds(int configured, string shown)
        {
            ModuleSettingsStore.Save("Slate", new SlateSettings { IdleSeconds = configured });

            _ = ModuleUsageGuide.HowToTrigger("Slate").Should().Be($"Shows after {shown} without input.");
            _ = ModuleUsageGuide.IdleText().Should().Be(shown, "Home's status line says it the same way");
        }

        [Fact]
        public void Tessera_is_triggered_by_system_events_not_a_hotkey()
        {
            _ = ModuleUsageGuide.HowToTrigger("Tessera").Should().Contain("volume").And.NotContain("Try now", "step 4 retired it");
        }

        [Fact]
        public void An_installed_module_without_a_manifest_trigger_says_nothing_rather_than_guess()
        {
            InstallManifest("Acme", new ModuleManifest { Id = "Acme", Kind = "Capability" });

            _ = ModuleUsageGuide.HowToTrigger("Acme").Should().BeEmpty();
        }

        [Theory]
        [InlineData("Tessera")]
        [InlineData("Mixdeck")]
        [InlineData("Inlay")]
        [InlineData("Slate")]
        [InlineData("Chord")]
        [InlineData("Substrate")]
        [InlineData("Chrono")]
        [InlineData("Phono")]
        [InlineData("Pulse")]
        [InlineData("Canvas")]
        public void No_built_in_text_uses_the_old_Hub_words(string moduleId)
        {
            _ = ModuleCatalog.TryGet(moduleId, out ModuleInfo? info).Should().BeTrue();
            string[] texts = [info!.Description, ModuleUsageGuide.Summary(moduleId), ModuleUsageGuide.HowToTrigger(moduleId)];
            foreach (string text in texts)
            {
                _ = text.Should().NotMatchRegex(@"(?i)\barm(ed)?\b|\btiles?\b|capabilit|\bOSD\b|Try now|Material You", moduleId);
            }
        }

        [Fact]
        public void An_installed_module_trigger_comes_from_its_manifest()
        {
            InstallManifest("Acme", new ModuleManifest { Id = "Acme", HowToTrigger = "Double-tap the logo." });

            _ = ModuleUsageGuide.HowToTrigger("Acme").Should().Be("Double-tap the logo.");
        }

        [Fact]
        public void Without_shortcuts_a_hotkey_module_says_so_instead_of_naming_a_key()
        {
            _ = ModuleUsageGuide.HowToTrigger("Mixdeck", HostPlatform.DesktopFeatures.Resolve(false, "GNOME"))
                .Should().Be("Shortcuts are not available on GNOME yet.");
        }

        [Fact]
        public void With_shortcuts_the_desktop_aware_text_is_the_usual_one()
        {
            _ = ModuleUsageGuide.HowToTrigger("Mixdeck", HostPlatform.DesktopFeatures.Resolve(false, "KDE"))
                .Should().Be(ModuleUsageGuide.HowToTrigger("Mixdeck"));
        }
    }
}
