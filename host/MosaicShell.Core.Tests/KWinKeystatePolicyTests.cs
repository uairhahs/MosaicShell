using FluentAssertions;
using MosaicShell.Core.Services;
using MosaicShell.Core.Services.Kde;

namespace MosaicShell.Core.Tests
{
    /// <summary>
    /// Lock keys from KWin's org_kde_kwin_keystate, spoken in the Wayland wire format. KWin 6.6 offers the global to an
    /// ordinary client (version 5, checked with wayland-info on daishin, 2026-11-24).
    /// </summary>
    public sealed class KWinKeystatePolicyTests
    {
        [Theory]
        [InlineData("wayland-0", "/run/user/1000", "/run/user/1000/wayland-0")]
        [InlineData(null, "/run/user/1000/", "/run/user/1000/wayland-0")]
        [InlineData("/tmp/kwin-sock", null, "/tmp/kwin-sock")]
        [InlineData("wayland-1", null, null)]
        public void The_socket_is_found_as_libwayland_finds_it(string? display, string? runtimeDir, string? expected)
        {
            _ = KWinKeystatePolicy.SocketPath(display, runtimeDir).Should().Be(expected);
        }

        [Fact]
        public void Hello_asks_for_the_registry_then_a_sync()
        {
            // wl_display (1).get_registry (opcode 1, new id 2), then .sync (opcode 0, new id 3); 12 bytes each.
            _ = KWinKeystatePolicy.Hello().Should().Equal(
                1, 0, 0, 0, 1, 0, 12, 0, 2, 0, 0, 0,
                1, 0, 0, 0, 0, 0, 12, 0, 3, 0, 0, 0);
        }

        [Fact]
        public void Binding_names_the_interface_at_version_1_then_fetches_the_states()
        {
            byte[] bytes = KWinKeystatePolicy.BindAndFetch(62);

            // "org_kde_kwin_keystate" is 21 bytes, 22 with its NUL, padded to 24.
            byte[] bind = KWinKeystatePolicy.Message(2, 0,
                [.. KWinKeystatePolicy.U32(62), .. KWinKeystatePolicy.String("org_kde_kwin_keystate"), .. KWinKeystatePolicy.U32(1), .. KWinKeystatePolicy.U32(4)]);
            _ = bind.Length.Should().Be(8 + 4 + 4 + 24 + 4 + 4);
            _ = bytes.Should().Equal([.. bind, 4, 0, 0, 0, 0, 0, 8, 0]);
        }

        [Fact]
        public void A_global_reads_back_its_name_interface_and_version()
        {
            byte[] args = [.. KWinKeystatePolicy.U32(62), .. KWinKeystatePolicy.String("org_kde_kwin_keystate"), .. KWinKeystatePolicy.U32(5)];

            _ = KWinKeystatePolicy.ReadGlobal(args).Should().Be((62u, "org_kde_kwin_keystate", 5u));
        }

        [Fact]
        public void A_message_is_read_only_once_all_of_it_has_arrived()
        {
            byte[] message = KWinKeystatePolicy.Message(4, 0, [.. KWinKeystatePolicy.U32(0), .. KWinKeystatePolicy.U32(2)]);
            byte[] stream = [.. message, .. message[..5]];

            _ = KWinKeystatePolicy.TryRead(stream, out WaylandMessage? first, out int consumed).Should().BeTrue();
            _ = KWinKeystatePolicy.TryRead(stream.AsSpan(consumed), out _, out _).Should().BeFalse();

            _ = consumed.Should().Be(16);
            _ = first!.ObjectId.Should().Be(4);
            _ = first.Opcode.Should().Be(0);
            _ = first.Args.Should().Equal(message[8..]);
        }

        [Theory]
        [InlineData(0u, 2u, LockKeyKind.CapsLock, true)]
        [InlineData(0u, 0u, LockKeyKind.CapsLock, false)]
        [InlineData(0u, 1u, LockKeyKind.CapsLock, false)]
        [InlineData(1u, 2u, LockKeyKind.NumLock, true)]
        [InlineData(2u, 2u, LockKeyKind.ScrollLock, true)]
        public void A_lock_key_is_on_only_when_locked(uint key, uint state, LockKeyKind kind, bool on)
        {
            byte[] args = [.. KWinKeystatePolicy.U32(key), .. KWinKeystatePolicy.U32(state)];

            _ = KWinKeystatePolicy.ReadStateChanged(args).Should().Be(new LockKeyState(kind, on));
        }

        [Fact]
        public void A_modifier_key_is_not_a_lock_key()
        {
            // Version 5 adds alt (3) to altgr (7); a version 1 binding never gets them, but they must not be misread.
            byte[] args = [.. KWinKeystatePolicy.U32(4), .. KWinKeystatePolicy.U32(3)];

            _ = KWinKeystatePolicy.ReadStateChanged(args).Should().BeNull();
        }
    }
}
