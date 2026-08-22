using FluentAssertions;
using MosaicShell.Core.Capabilities;
using MosaicShell.Core.Capabilities.BuiltIn;
using MosaicShell.Core.Runtime;
using MosaicShell.Core.Services;
using MosaicShell.Core.Settings;
using MosaicShell.Core.Styles;

namespace MosaicShell.Core.Tests;

public class HotkeyOverlayCapabilityTests
{
    [Theory]
    [InlineData("Inlay")]
    [InlineData("Chord")]
    [InlineData("Substrate")]
    public async Task Armed_hotkey_invokes_overlay_bridge_not_flyout(string moduleId)
    {
        var services = HostServicesFakes.Create();
        var hotkeys = (FakeHotkeyService)services.Hotkeys;
        var flyouts = new CountingFlyouts();
        var hostUi = new RecordingHostUiBridge();
        var ui = new BridgeUi(flyouts, hostUi);

        IModuleCapability cap = moduleId switch
        {
            "Inlay" => new InlayCapability(services, ui),
            "Chord" => new ChordCapability(services, ui),
            _ => new SubstrateCapability(services, ui)
        };

        await cap.ArmAsync();
        hotkeys.TryInvoke("cap:" + moduleId).Should().BeTrue();
        hostUi.OpenCount.Should().Be(1);
        hostUi.LastOpenedModule.Should().Be(moduleId);
        flyouts.ShowCount.Should().Be(0);
        await cap.DisarmAsync();
    }

    [Fact]
    public async Task Slate_idle_opens_bridge_and_respects_fullscreen_hide()
    {
        var services = HostServicesFakes.Create();
        var idle = (FakeIdleService)services.Idle;
        var fullscreen = (FakeFullscreenProbe)services.Fullscreen;
        var flyouts = new CountingFlyouts();
        var hostUi = new RecordingHostUiBridge();
        var ui = new BridgeUi(flyouts, hostUi);

        ModuleSettingsStore.Save("Slate", new SlateSettings
        {
            IdleSeconds = 30,
            HideOnFullscreen = true,
            Style = "Center"
        });

        var cap = new SlateCapability(services, ui);
        await cap.ArmAsync();
        idle.IsStarted.Should().BeTrue();
        idle.Threshold.Should().Be(TimeSpan.FromSeconds(30));

        fullscreen.IsForegroundFullscreen = true;
        idle.RaiseIdle();
        hostUi.OpenCount.Should().Be(0);

        fullscreen.IsForegroundFullscreen = false;
        idle.RaiseIdle();
        hostUi.OpenCount.Should().Be(1);
        hostUi.LastOpenedModule.Should().Be("Slate");
        flyouts.ShowCount.Should().Be(0);

        await cap.DisarmAsync();
        hostUi.CloseCount.Should().Be(1);
        hostUi.LastClosedModule.Should().Be("Slate");
        idle.IsStarted.Should().BeFalse();
    }

    [Fact]
    public void HotkeyGestureParser_parses_win_and_space()
    {
        HotkeyGestureParser.TryParse("Ctrl+Alt+I", out var mods, out var vk).Should().BeTrue();
        mods.Should().HaveFlag(ModifierKeys.Control);
        mods.Should().HaveFlag(ModifierKeys.Alt);
        vk.Should().Be('I');

        HotkeyGestureParser.TryParse("Ctrl+Space", out mods, out vk).Should().BeTrue();
        mods.Should().HaveFlag(ModifierKeys.Control);
        vk.Should().Be(0x20);
    }

    [Fact]
    public void HotkeyGestureParser_replaces_os_reserved_defaults()
    {
        HotkeyGestureParser.IsLikelyOsReserved("Win+S").Should().BeTrue();
        HotkeyGestureParser.EnsureRegisterable("Inlay", "Win+S").Should().Be("Ctrl+Alt+I");
        HotkeyGestureParser.EnsureRegisterable("Substrate", "Win+A").Should().Be("Ctrl+Alt+Q");
    }

    private sealed class CountingFlyouts : IFlyoutPresenter
    {
        public int ShowCount { get; private set; }
        public void Show(FlyoutRequest request) => ShowCount++;
        public void Update(FlyoutRequest request) => ShowCount++;
        public void SoftRefresh(FlyoutRequest request) { }
        public void Hide(string moduleId) { }
        public void HideAll() { }
        public bool IsVisible(string moduleId) => false;
    }
}
