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
            _ = hub.Metrics.Should().BeOfType<ProcSystemMetricsService>();
            _ = hub.Autostart.Should().BeOfType<XdgAutostartService>();
            _ = hub.Airplane.IsSupported.Should().BeFalse();
        }

        [Fact]
        public void The_desktops_own_pop_ups_are_hidden_by_the_route_each_desktop_allows()
        {
            // GNOME has no API to hide its pop-ups, so MosaicShell's Shell extension does; Plasma has osdService and KWin.
            using INativeOsdSuppressor gnome = HostServices.CreateLinuxOsdSuppressor("GNOME");
            using INativeOsdSuppressor kde = HostServices.CreateLinuxOsdSuppressor("KDE");
            using INativeOsdSuppressor unknown = HostServices.CreateLinuxOsdSuppressor(null);

            _ = gnome.Should().BeOfType<GnomeOsdSuppressor>();
            _ = kde.Should().BeOfType<PlasmaOsdSuppressor>();
            _ = unknown.Should().BeOfType<PlasmaOsdSuppressor>();
        }

        [Fact]
        public void Idle_comes_from_Mutter_on_GNOME_and_from_the_Wayland_idle_protocol_elsewhere()
        {
            // Mutter offers no ext_idle_notifier_v1, so Slate's idle never fired on GNOME.
            using IIdleService gnome = HostServices.CreateLinuxIdle("GNOME");
            using IIdleService kde = HostServices.CreateLinuxIdle("KDE");
            using IIdleService unknown = HostServices.CreateLinuxIdle(null);

            _ = gnome.Should().BeOfType<MutterIdleService>();
            _ = kde.Should().BeOfType<WaylandIdleService>();
            _ = unknown.Should().BeOfType<WaylandIdleService>();
        }

        [Fact]
        public void Lock_keys_come_from_KWin_on_KDE_and_from_the_keyboard_LEDs_elsewhere()
        {
            // GNOME offers no lock state to clients (no org_kde_kwin_keystate), so Caps Lock opened nothing there.
            using ILockKeysService kde = HostServices.CreateLinuxLockKeys("KDE");
            using ILockKeysService gnome = HostServices.CreateLinuxLockKeys("GNOME");
            using ILockKeysService unknown = HostServices.CreateLinuxLockKeys(null);

            _ = kde.Should().BeOfType<KWinKeystateLockKeys>();
            _ = gnome.Should().BeOfType<LedLockKeysService>();
            _ = unknown.Should().BeOfType<LedLockKeysService>();
        }

        [Fact]
        public void Airplane_mode_follows_Plasmas_setting_on_KDE_and_the_radios_elsewhere()
        {
            using IAirplaneModeService kde = HostServices.CreateLinuxAirplane("KDE");
            using IAirplaneModeService gnome = HostServices.CreateLinuxAirplane("GNOME");
            using IAirplaneModeService unknown = HostServices.CreateLinuxAirplane(null);

            _ = kde.Should().BeOfType<PlasmaAirplaneModeService>();
            _ = gnome.Should().BeOfType<RfkillAirplaneModeService>();
            _ = unknown.Should().BeOfType<RfkillAirplaneModeService>();
        }
    }
}
