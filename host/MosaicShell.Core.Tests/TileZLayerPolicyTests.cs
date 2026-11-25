using FluentAssertions;
using MosaicShell.Core.Modules;

namespace MosaicShell.Core.Tests
{
    public class TileZLayerPolicyTests
    {
        [Theory]
        [InlineData("Chrono", ModuleKind.Widget, TileZLayer.Desktop)]
        [InlineData("Canvas", ModuleKind.Widget, TileZLayer.Desktop)]
        [InlineData("Pulse", ModuleKind.Widget, TileZLayer.Desktop)]
        [InlineData("pulse", ModuleKind.Hybrid, TileZLayer.Desktop)]
        [InlineData("Mixdeck", ModuleKind.Hybrid, TileZLayer.AlwaysOnTop)]
        [InlineData("Tessera", ModuleKind.Capability, TileZLayer.AlwaysOnTop)]
        public void Widgets_and_pulse_start_on_the_desktop_other_tiles_on_top(string id, ModuleKind kind, TileZLayer expected)
        {
            _ = TileZLayerPolicy.InitialLayer(kind, id).Should().Be(expected);
        }

        [Theory]
        [InlineData(TileZLayer.Desktop, false, true)]
        [InlineData(TileZLayer.Normal, false, false)]
        [InlineData(TileZLayer.AlwaysOnTop, true, false)]
        public void Each_layer_is_one_window_manager_state(TileZLayer layer, bool topmost, bool below)
        {
            // Above and below are exclusive. Avalonia sends ABOVE through Topmost; it has no way to send BELOW, the
            // EWMH state that keeps a window under every other one on X11.
            _ = TileZLayerPolicy.IsTopmost(layer).Should().Be(topmost);
            _ = TileZLayerPolicy.IsBelow(layer).Should().Be(below);
        }

        [Theory]
        [InlineData(true, false, true)]
        [InlineData(false, true, true)]
        [InlineData(false, false, false)]
        public void Below_is_sent_by_the_host_wherever_the_platform_cannot_order_windows_itself(
            bool isWindows, bool hasX11Display, bool expected)
        {
            // Windows orders the HWND with SetWindowPos; X11 needs _NET_WM_STATE_BELOW; elsewhere nothing can.
            _ = TileZLayerPolicy.CanPlaceBelow(isWindows, hasX11Display).Should().Be(expected);
        }
    }
}
