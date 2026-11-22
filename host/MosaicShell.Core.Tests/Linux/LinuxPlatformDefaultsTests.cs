using FluentAssertions;
using MosaicShell.Core.Services;
using MosaicShell.Core.Services.Linux;

namespace MosaicShell.Core.Tests.Linux
{
    /// <summary>
    /// The Linux composition root: media comes from MPRIS players through the same composite the Windows stack wraps
    /// SMTC in, every other service stays inert until it has a Linux implementation, and a missing or unreachable
    /// session bus means no media session, never a failed start.
    /// </summary>
    public sealed class LinuxPlatformDefaultsTests
    {
        private static readonly TimeSpan Patience = TimeSpan.FromSeconds(5);

        private static async Task<bool> EventuallyAsync(Func<bool> condition)
        {
            DateTime until = DateTime.UtcNow + Patience;
            while (DateTime.UtcNow < until)
            {
                if (condition())
                {
                    return true;
                }

                await Task.Delay(20);
            }

            return condition();
        }

        [LinuxBusFact]
        public async Task The_media_stack_presents_a_player_on_the_bus_and_drives_it()
        {
            using PrivateDBus dbus = PrivateDBus.Start();
            await using FakeMprisPlayer player = new("fake.instance7");
            player.SetMetadata("Song One", "Band", 200_000_000);
            await player.PublishAsync(dbus.Address);

            using IMediaSessionService media = HostServices.CreateLinuxMediaStack(dbus.Address);

            _ = media.Should().BeOfType<CompositeMediaSessionService>("Windows wraps SMTC in the composite too");
            _ = (await EventuallyAsync(() => media.Current?.Title == "Song One")).Should().BeTrue();
            _ = media.Current!.Artist.Should().Be("Band");

            await media.PlayPauseAsync();
            _ = (await EventuallyAsync(() => player.SnapshotCalls().Contains("PlayPause"))).Should().BeTrue();
        }

        [LinuxBusFact]
        public async Task An_unreachable_bus_means_no_session_rather_than_a_failure()
        {
            string missing = "unix:path=" + Path.Combine(Path.GetTempPath(), "mosaic-no-bus-" + Guid.NewGuid().ToString("N"));

            using IMediaSessionService media = HostServices.CreateLinuxMediaStack(missing);
            await Task.Delay(200);

            _ = media.Current.Should().BeNull();
            await media.PlayPauseAsync();
        }

        [LinuxBusFact]
        public async Task A_bus_without_an_address_fails_on_connect_not_on_construction()
        {
            using DBusMprisBus bus = new(address: "");

            Func<Task> connect = bus.ConnectAsync;

            _ = await connect.Should().ThrowAsync<InvalidOperationException>();
        }

        [LinuxBusFact]
        public void Platform_defaults_on_linux_wire_the_linux_services_and_keep_the_rest_inert()
        {
            using HostServices hub = HostServices.CreatePlatformDefaults();

            _ = hub.Media.Should().BeOfType<CompositeMediaSessionService>();
            _ = hub.Audio.Should().BeOfType<PulseAudioService>();
            _ = hub.Brightness.Should().BeOfType<KdeScreenBrightness>();
            _ = hub.BrightnessChanges.Should().BeSameAs(hub.Brightness, "one PowerDevil watcher serves both roles");
            _ = hub.Airplane.IsSupported.Should().BeFalse();
        }
    }
}
