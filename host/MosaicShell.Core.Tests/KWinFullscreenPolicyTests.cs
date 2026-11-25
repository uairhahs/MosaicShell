using FluentAssertions;
using MosaicShell.Core.Services.Kde;

namespace MosaicShell.Core.Tests
{
    /// <summary>
    /// Whether KWin's active window is fullscreen, from the facts the probe script reports, by the rule Windows' probe
    /// uses: it covers its screen (within 2 px). The desktop itself never counts.
    /// </summary>
    public sealed class KWinFullscreenPolicyTests
    {
        private static string Report(bool full, bool desktop, string frame, string screen)
        {
            return $"full={(full ? 1 : 0)};desktop={(desktop ? 1 : 0)};frame={frame};screen={screen}";
        }

        [Fact]
        public void A_window_kwin_calls_fullscreen_is_fullscreen()
        {
            _ = KWinFullscreenPolicy.IsFullscreen(Report(true, false, "0,0,2560,1440", "0,0,2560,1440")).Should().BeTrue();
        }

        [Fact]
        public void A_borderless_window_covering_its_screen_is_fullscreen_as_on_windows()
        {
            // Many games run borderless at screen size without asking for fullscreen.
            _ = KWinFullscreenPolicy.IsFullscreen(Report(false, false, "-1,1,2561,1440", "0,0,2560,1440")).Should().BeTrue();
        }

        [Fact]
        public void Coverage_is_measured_against_the_window_s_own_screen()
        {
            _ = KWinFullscreenPolicy.IsFullscreen(Report(false, false, "2560,0,1920,1080", "2560,0,1920,1080")).Should().BeTrue();
            _ = KWinFullscreenPolicy.IsFullscreen(Report(false, false, "2560,0,1920,1080", "0,0,2560,1440")).Should().BeFalse();
        }

        [Fact]
        public void A_maximized_window_beside_a_panel_is_not_fullscreen()
        {
            _ = KWinFullscreenPolicy.IsFullscreen(Report(false, false, "0,0,2560,1396", "0,0,2560,1440")).Should().BeFalse();
        }

        [Fact]
        public void The_desktop_is_never_fullscreen()
        {
            // On Windows the focused desktop covers the monitor and reads as fullscreen; that is not carried over.
            _ = KWinFullscreenPolicy.IsFullscreen(Report(true, true, "0,0,2560,1440", "0,0,2560,1440")).Should().BeFalse();
        }

        [Fact]
        public void Fractional_geometry_from_a_scaled_screen_is_read()
        {
            _ = KWinFullscreenPolicy.IsFullscreen(Report(false, false, "0,0,1706.5,960.5", "0,0,1706.67,960")).Should().BeTrue();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("none")]
        [InlineData("full=1")]
        [InlineData("full=0;desktop=0;frame=a,b,c,d;screen=0,0,1,1")]
        public void No_active_window_or_a_report_it_cannot_read_is_not_fullscreen(string? report)
        {
            _ = KWinFullscreenPolicy.IsFullscreen(report).Should().BeFalse();
        }

        [Fact]
        public void The_script_reports_the_active_window_to_the_probe_and_nothing_else()
        {
            _ = KWinFullscreenPolicy.Script.Should().Contain("workspace.activeWindow");
            _ = KWinFullscreenPolicy.Script.Should().Contain($"\"{KWinFullscreenPolicy.ProbeBusName}\"");
            _ = KWinFullscreenPolicy.Script.Should().Contain($"\"{KWinFullscreenPolicy.ReportMethod}\"");
            _ = KWinFullscreenPolicy.Script.Should().NotContain(".connect(", "it is one-shot: nothing stays watching");
        }
    }
}
