using FluentAssertions;
using MosaicShell.Core.Modules;
using MosaicShell.Core.Runtime;
using MosaicShell.Core.Settings;

namespace MosaicShell.Core.Tests
{
    public class OverlayBehaviorTests
    {
        [Fact]
        public void Inlay_catalog_search_finds_builtins_and_pins()
        {
            _ = LaunchTargetCatalog.Search("notepad").Should().Contain(t =>
                t.DisplayName.Equals("Notepad", StringComparison.OrdinalIgnoreCase));
            _ = LaunchTargetCatalog.Search("settings").Should().Contain(t =>
                t.Target.StartsWith("ms-settings", StringComparison.OrdinalIgnoreCase));

            List<string> pins = ["calc", "notepad"];
            IReadOnlyList<LaunchTarget> all = InlayLaunchLogic.BuildTargets("", pins);
            _ = all.Should().Contain(t => t.Group == "Pinned" && t.DisplayName == "Calculator");
            _ = all.Should().Contain(t => t.DisplayName == "Notepad");
        }

        [Fact]
        public void Inlay_search_filters_catalog_by_display_target_or_group()
        {
            IReadOnlyList<LaunchTarget> filtered = InlayLaunchLogic.BuildTargets("explorer", ["notepad"]);
            _ = filtered.Should().OnlyContain(t =>
                t.DisplayName.Contains("explorer", StringComparison.OrdinalIgnoreCase)
                || t.Target.Contains("explorer", StringComparison.OrdinalIgnoreCase)
                || t.Group.Contains("explorer", StringComparison.OrdinalIgnoreCase)
                || t.Group == "Pinned");
        }

        [Fact]
        public void Inlay_enter_resolves_catalog_labels_and_freeform_targets()
        {
            _ = LaunchTargetCatalog.TryResolveLabel("Notepad  (notepad)", out string? target, out string? display)
                .Should().BeTrue();
            _ = target.Should().Be("notepad");
            _ = display.Should().Be("Notepad");

            _ = LaunchTargetCatalog.TryResolveLabel("ms-settings:", out string? settingsTarget, out _)
                .Should().BeTrue();
            _ = settingsTarget.Should().Be("ms-settings:");
        }

        [Fact]
        public void CloseOnEscape_honors_capability_settings()
        {
            ModuleSettingsStore.Save("Mixdeck", new MixdeckSettings { CloseOnEscape = false });
            ModuleSettingsStore.Save("Inlay", new InlaySettings { CloseOnEscape = false });
            try
            {
                _ = ModuleOverlaySettings.CloseOnEscape("Mixdeck").Should().BeFalse();
                _ = ModuleOverlaySettings.CloseOnEscape("Inlay").Should().BeFalse();
                _ = ModuleOverlaySettings.CloseOnEscape("Chord").Should().BeTrue();
            }
            finally
            {
                ModuleSettingsStore.Save("Mixdeck", new MixdeckSettings());
                ModuleSettingsStore.Save("Inlay", new InlaySettings());
            }
        }
    }
}
