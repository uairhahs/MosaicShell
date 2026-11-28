using FluentAssertions;
using MosaicShell.Core.Capabilities;
using MosaicShell.Core.Capabilities.BuiltIn;
using MosaicShell.Core.HostPlatform;
using MosaicShell.Core.Modules.Settings;
using MosaicShell.Core.Services;

namespace MosaicShell.Core.Tests
{
    /// <summary>
    /// A desktop without a feature says so, in Core's words, wherever the feature would show: on a module page row and
    /// as the shortcut's error. GNOME is the baseline, so it never sees a "taken by another app" error for a route that
    /// does not exist.
    /// </summary>
    public sealed class DesktopFeatureAvailabilityTests : IDisposable
    {
        private readonly string _root;

        public DesktopFeatureAvailabilityTests()
        {
            _root = Path.Combine(Path.GetTempPath(), "MosaicFeatures_" + Guid.NewGuid().ToString("N"));
            AppPaths.SetRootOverride(_root);
            AppPaths.EnsureLayout();
        }

        public void Dispose()
        {
            AppPaths.ClearRootOverride();
            try { Directory.Delete(_root, true); } catch { /* ignore */ }
        }

        [Theory]
        [InlineData(true, null, "Windows")]
        [InlineData(false, "KDE", "Plasma")]
        [InlineData(false, "GNOME", "GNOME")]
        [InlineData(false, "ubuntu:GNOME", "GNOME")]
        [InlineData(false, "Hyprland", "Hyprland")]
        [InlineData(false, null, "this desktop")]
        public void Each_desktop_is_named_as_people_know_it(bool windows, string? desktop, string name)
        {
            _ = DesktopFeatures.Resolve(windows, desktop).DesktopName.Should().Be(name);
        }

        [Theory]
        [InlineData(DesktopFeature.GlobalHotkeys, "Shortcuts are not available on GNOME yet.")]
        [InlineData(DesktopFeature.MediaKeys, "Media keys are not available on GNOME yet.")]
        [InlineData(DesktopFeature.FullscreenProbe, "Telling when an app is full screen is not available on GNOME yet.")]
        public void A_missing_feature_says_what_and_where(DesktopFeature feature, string text)
        {
            _ = DesktopFeatures.Resolve(false, "GNOME").MissingText(feature).Should().Be(text);
        }

        [Fact]
        public void A_feature_the_desktop_has_needs_no_text()
        {
            _ = DesktopFeatures.Resolve(false, "KDE").MissingText(DesktopFeature.GlobalHotkeys).Should().BeNull();
        }

        [Theory]
        [InlineData("KDE", true, null)]
        [InlineData("GNOME", false, "Shortcuts are not available on GNOME yet.")]
        public void A_shortcut_row_works_only_where_shortcuts_do(string desktop, bool available, string? reason)
        {
            SettingRow hotkey = Schema("Mixdeck").Row("HotkeyGesture");

            SettingAvailability result = SettingAvailability.Of(hotkey, DesktopFeatures.Resolve(false, desktop));

            _ = result.IsAvailable.Should().Be(available);
            _ = result.Reason.Should().Be(reason);
        }

        [Fact]
        public void A_row_that_needs_nothing_works_everywhere()
        {
            SettingRow escape = Schema("Mixdeck").Row("CloseOnEscape");

            _ = SettingAvailability.Of(escape, DesktopFeatures.None).IsAvailable.Should().BeTrue();
        }

        [Fact]
        public async Task Without_shortcuts_a_capability_arms_with_the_honest_error_and_registers_nothing()
        {
            HostServices services = HostServicesFakes.Create(DesktopFeatures.Resolve(false, "GNOME"));
            FakeHotkeyService hotkeys = (FakeHotkeyService)services.Hotkeys;
            InlayCapability cap = new(TestCapabilityContext.Create(services, new BridgeUi(new NullFlyoutPresenter(), new RecordingHostUiBridge()), "Inlay"));

            await cap.ArmAsync();

            _ = cap.IsArmed.Should().BeTrue();
            _ = cap.HotkeyRegistered.Should().BeFalse();
            _ = cap.HotkeyError.Should().Be("Shortcuts are not available on GNOME yet.");
            _ = hotkeys.TryInvoke("cap:Inlay").Should().BeFalse("nothing was registered");
            await cap.DisarmAsync();
        }

        [Fact]
        public async Task With_shortcuts_a_taken_gesture_still_says_it_is_taken()
        {
            HostServices services = HostServicesFakes.Create(DesktopFeatures.Resolve(false, "KDE"));
            ((FakeHotkeyService)services.Hotkeys).FailRegistrations = true;
            InlayCapability cap = new(TestCapabilityContext.Create(services, new BridgeUi(new NullFlyoutPresenter(), new RecordingHostUiBridge()), "Inlay"));

            await cap.ArmAsync();

            _ = cap.HotkeyError.Should().StartWith("Could not register");
            await cap.DisarmAsync();
        }

        private static ModuleSettingsSchema Schema(string moduleId)
        {
            _ = ModuleSettingsSchemas.TryGet(moduleId, out ModuleSettingsSchema? schema).Should().BeTrue();
            return schema!;
        }
    }
}
