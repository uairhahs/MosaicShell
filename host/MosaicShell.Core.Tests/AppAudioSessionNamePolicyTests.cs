using FluentAssertions;
using MosaicShell.Core.Services;

namespace MosaicShell.Core.Tests
{
    /// <summary>
    /// Mixdeck's names for Windows audio sessions. The raw values are from Windows 11 (2026-11-24): Edge's session has no
    /// display name, and the system sounds session reports an unresolved AudioSrv resource string.
    /// </summary>
    public class AppAudioSessionNamePolicyTests
    {
        private const string SystemSoundsResource = @"@%SystemRoot%\System32\AudioSrv.Dll,-202";

        private static string? NoResolve(string _)
        {
            return null;
        }

        [Fact]
        public void An_app_that_sets_no_display_name_is_named_by_its_file_description()
        {
            string name = AppAudioSessionNamePolicy.Resolve("", false, NoResolve, "Microsoft Edge", "msedge");

            _ = name.Should().Be("Microsoft Edge");
        }

        [Fact]
        public void Without_a_file_description_the_process_name_is_used()
        {
            string name = AppAudioSessionNamePolicy.Resolve(null, false, NoResolve, "  ", "game");

            _ = name.Should().Be("game");
        }

        [Fact]
        public void A_display_name_the_app_set_wins()
        {
            string name = AppAudioSessionNamePolicy.Resolve("Discord Voice", false, NoResolve, "Discord", "Discord");

            _ = name.Should().Be("Discord Voice");
        }

        [Fact]
        public void An_indirect_display_name_is_resolved()
        {
            string name = AppAudioSessionNamePolicy.Resolve(
                SystemSoundsResource, true, s => s == SystemSoundsResource ? "System Sounds" : null, null, null);

            _ = name.Should().Be("System Sounds");
        }

        [Fact]
        public void The_system_sounds_session_is_named_even_when_its_resource_does_not_resolve()
        {
            string name = AppAudioSessionNamePolicy.Resolve(SystemSoundsResource, true, NoResolve, null, "Idle");

            _ = name.Should().Be(AppAudioSessionNamePolicy.SystemSounds);
        }

        [Fact]
        public void An_unresolved_indirect_string_is_never_shown()
        {
            string name = AppAudioSessionNamePolicy.Resolve("@shell32.dll,-1", false, NoResolve, "Player", "player");

            _ = name.Should().Be("Player");
        }

        [Fact]
        public void Nothing_known_reads_as_an_unknown_app()
        {
            string name = AppAudioSessionNamePolicy.Resolve(null, false, NoResolve, null, null);

            _ = name.Should().Be(AppAudioSessionNamePolicy.UnknownApp);
        }
    }
}
