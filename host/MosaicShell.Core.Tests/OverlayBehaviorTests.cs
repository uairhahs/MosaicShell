using FluentAssertions;
using MosaicShell.Core.Modules;
using MosaicShell.Core.Runtime;
using MosaicShell.Core.Settings;

namespace MosaicShell.Core.Tests;

public class OverlayBehaviorTests
{
    [Fact]
    public void Inlay_catalog_search_finds_builtins_and_pins()
    {
        LaunchTargetCatalog.Search("notepad").Should().Contain(t =>
            t.DisplayName.Equals("Notepad", StringComparison.OrdinalIgnoreCase));
        LaunchTargetCatalog.Search("settings").Should().Contain(t =>
            t.Target.StartsWith("ms-settings", StringComparison.OrdinalIgnoreCase));

        var pins = new List<string> { "calc", "notepad" };
        var all = InlayLaunchLogic.BuildTargets("", pins);
        all.Should().Contain(t => t.Group == "Pinned" && t.DisplayName == "Calculator");
        all.Should().Contain(t => t.DisplayName == "Notepad");
    }

    [Fact]
    public void Inlay_search_filters_catalog_by_display_target_or_group()
    {
        var filtered = InlayLaunchLogic.BuildTargets("explorer", ["notepad"]);
        filtered.Should().OnlyContain(t =>
            t.DisplayName.Contains("explorer", StringComparison.OrdinalIgnoreCase)
            || t.Target.Contains("explorer", StringComparison.OrdinalIgnoreCase)
            || t.Group.Contains("explorer", StringComparison.OrdinalIgnoreCase)
            || t.Group == "Pinned");
    }

    [Fact]
    public void Inlay_enter_resolves_catalog_labels_and_freeform_targets()
    {
        LaunchTargetCatalog.TryResolveLabel("Notepad  (notepad)", out var target, out var display)
            .Should().BeTrue();
        target.Should().Be("notepad");
        display.Should().Be("Notepad");

        LaunchTargetCatalog.TryResolveLabel("ms-settings:", out var settingsTarget, out _)
            .Should().BeTrue();
        settingsTarget.Should().Be("ms-settings:");
    }

    [Fact]
    public void CloseOnEscape_honors_capability_settings()
    {
        ModuleSettingsStore.Save("Mixdeck", new MixdeckSettings { CloseOnEscape = false });
        ModuleSettingsStore.Save("Inlay", new InlaySettings { CloseOnEscape = false });
        try
        {
            ModuleOverlaySettings.CloseOnEscape("Mixdeck").Should().BeFalse();
            ModuleOverlaySettings.CloseOnEscape("Inlay").Should().BeFalse();
            ModuleOverlaySettings.CloseOnEscape("Chord").Should().BeTrue();
        }
        finally
        {
            ModuleSettingsStore.Save("Mixdeck", new MixdeckSettings());
            ModuleSettingsStore.Save("Inlay", new InlaySettings());
        }
    }
}
