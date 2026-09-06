using FluentAssertions;
using MosaicShell.Core.Capabilities;
using MosaicShell.Core.Capabilities.BuiltIn;
using MosaicShell.Core.Runtime;
using MosaicShell.Core.Services;
using MosaicShell.Core.Settings;

namespace MosaicShell.Core.Tests
{
    public class HotkeyOverlayCapabilityTests
    {
        [Theory]
        [InlineData("Inlay")]
        [InlineData("Chord")]
        [InlineData("Substrate")]
        public async Task Armed_hotkey_invokes_overlay_bridge_not_flyout(string moduleId)
        {
            HostServices services = HostServicesFakes.Create();
            FakeHotkeyService hotkeys = (FakeHotkeyService)services.Hotkeys;
            CountingFlyouts flyouts = new();
            RecordingHostUiBridge hostUi = new();
            BridgeUi ui = new(flyouts, hostUi);

            IModuleCapability cap = moduleId switch
            {
                "Inlay" => new InlayCapability(TestCapabilityContext.Create(services, ui, moduleId)),
                "Chord" => new ChordCapability(TestCapabilityContext.Create(services, ui, moduleId)),
                _ => new SubstrateCapability(TestCapabilityContext.Create(services, ui, moduleId))
            };

            await cap.ArmAsync();
            _ = hotkeys.TryInvoke("cap:" + moduleId).Should().BeTrue();
            _ = hostUi.OpenCount.Should().Be(1);
            _ = hostUi.LastOpenedModule.Should().Be(moduleId);
            _ = flyouts.ShowCount.Should().Be(0);
            await cap.DisarmAsync();
        }

        [Fact]
        public async Task Slate_idle_opens_bridge_and_respects_fullscreen_hide()
        {
            HostServices services = HostServicesFakes.Create();
            FakeIdleService idle = (FakeIdleService)services.Idle;
            FakeFullscreenProbe fullscreen = (FakeFullscreenProbe)services.Fullscreen;
            CountingFlyouts flyouts = new();
            RecordingHostUiBridge hostUi = new();
            BridgeUi ui = new(flyouts, hostUi);

            ModuleSettingsStore.Save("Slate", new SlateSettings
            {
                IdleSeconds = 30,
                HideOnFullscreen = true,
                Style = "Square"
            });

            SlateCapability cap = new(TestCapabilityContext.Create(services, ui, "Slate"));
            await cap.ArmAsync();
            _ = idle.IsStarted.Should().BeTrue();
            _ = idle.Threshold.Should().Be(TimeSpan.FromSeconds(30));

            fullscreen.IsForegroundFullscreen = true;
            idle.RaiseIdle();
            _ = hostUi.OpenCount.Should().Be(0);

            fullscreen.IsForegroundFullscreen = false;
            idle.RaiseIdle();
            _ = hostUi.OpenCount.Should().Be(1);
            _ = hostUi.LastOpenedModule.Should().Be("Slate");
            _ = flyouts.ShowCount.Should().Be(0);

            await cap.DisarmAsync();
            _ = hostUi.CloseCount.Should().Be(1);
            _ = hostUi.LastClosedModule.Should().Be("Slate");
            _ = idle.IsStarted.Should().BeFalse();
        }

        [Fact]
        public void HotkeyGestureParser_parses_win_and_space()
        {
            _ = HotkeyGestureParser.TryParse("Ctrl+Alt+I", out ModifierKeys mods, out int vk).Should().BeTrue();
            _ = mods.Should().HaveFlag(ModifierKeys.Control);
            _ = mods.Should().HaveFlag(ModifierKeys.Alt);
            _ = vk.Should().Be('I');

            _ = HotkeyGestureParser.TryParse("Ctrl+Space", out mods, out vk).Should().BeTrue();
            _ = mods.Should().HaveFlag(ModifierKeys.Control);
            _ = vk.Should().Be(0x20);
        }

        [Fact]
        public void HotkeyGestureParser_replaces_os_reserved_defaults()
        {
            _ = HotkeyGestureParser.IsLikelyOsReserved("Win+S").Should().BeTrue();
            _ = HotkeyGestureParser.EnsureRegisterable("Inlay", "Win+S").Should().Be("Ctrl+Alt+I");
            _ = HotkeyGestureParser.EnsureRegisterable("Substrate", "Win+A").Should().Be("Ctrl+Alt+Q");
        }

        private sealed class CountingFlyouts : IFlyoutPresenter
        {
            public event Action<string>? TransientDismissed { add { } remove { } }
            public int ShowCount { get; private set; }
            public void Show(FlyoutRequest request)
            {
                ShowCount++;
            }

            public void Update(FlyoutRequest request)
            {
                ShowCount++;
            }

            public void SoftRefresh(FlyoutRequest request) { }
            public void Hide(string moduleId) { }
            public void HideAll() { }
            public bool IsVisible(string moduleId)
            {
                return false;
            }
        }
    }
}
