using FluentAssertions;
using MosaicShell.Core.Services.Kde;

namespace MosaicShell.Core.Tests
{
    /// <summary>
    /// Plasma 6.6 has no osdService <c>hide</c> (checked on NixOS, 2026-11-23), so the pop-ups are hidden by a KWin
    /// script that asks the Host about each OSD window. Facts from a probe on KWin 6.6: OSD windows are flagged
    /// <c>onScreenDisplay</c>, setting <c>opacity</c> to 0 hides them, and <c>callDBus</c> never calls back when the
    /// target has no owner, so ownership is asked first.
    /// </summary>
    public sealed class KWinOsdScriptPolicyTests
    {
        [Fact]
        public void The_script_hides_each_osd_window_as_it_is_added()
        {
            string script = KWinOsdScriptPolicy.Script;

            _ = script.Should().Contain("workspace.windowAdded.connect(");
            _ = script.Should().Contain("w.onScreenDisplay");
            _ = script.Should().Contain("w.opacity = 0;");
        }

        [Fact]
        public void The_script_asks_whether_the_host_is_there_before_asking_it_anything()
        {
            string script = KWinOsdScriptPolicy.Script;
            int ownerCheck = script.IndexOf("\"NameHasOwner\"", StringComparison.Ordinal);
            int question = script.IndexOf($"\"{KWinOsdScriptPolicy.GuardMethod}\"", StringComparison.Ordinal);

            _ = ownerCheck.Should().BeGreaterThan(0);
            _ = question.Should().BeGreaterThan(ownerCheck);
            _ = script.Should().Contain($"\"{KWinOsdScriptPolicy.GuardBusName}\"");
            _ = script.Should().Contain($"\"{KWinOsdScriptPolicy.GuardPath}\"");
            _ = script.Should().Contain($"\"{KWinOsdScriptPolicy.GuardInterface}\"");
        }

        [Fact]
        public void Without_the_host_the_script_restores_the_window_and_stops_watching()
        {
            string script = KWinOsdScriptPolicy.Script;

            _ = script.Should().Contain("w.opacity = 1;");
            _ = script.Should().Contain("workspace.windowAdded.disconnect(");
        }

        [Theory]
        [InlineData("volumeChanged", 100, true)]
        [InlineData("screenBrightnessChanged", 100, true)]
        [InlineData("brightnessChanged", 900, true)]
        [InlineData("volumeChanged", 1500, false)]
        [InlineData("kbdLayoutChanged", 50, false)]
        [InlineData("showText", 50, false)]
        [InlineData(null, 0, false)]
        public void The_host_keeps_an_osd_hidden_only_after_a_recent_volume_or_brightness_call(
            string? lastMember, int sinceMs, bool expected)
        {
            _ = KWinOsdScriptPolicy.ShouldKeepHidden(lastMember, TimeSpan.FromMilliseconds(sinceMs)).Should().Be(expected);
        }

        [Fact]
        public void The_script_name_is_stable_so_a_stale_copy_can_be_unloaded()
        {
            _ = KWinOsdScriptPolicy.ScriptName.Should().Be("mosaicshell-osd-guard");
        }
    }
}
