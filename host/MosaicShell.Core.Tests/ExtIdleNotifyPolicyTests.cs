using System.Buffers.Binary;
using FluentAssertions;
using MosaicShell.Core.Services.Kde;
using MosaicShell.Core.Services.Wayland;

namespace MosaicShell.Core.Tests
{
    /// <summary>The <c>ext_idle_notifier_v1</c> requests the idle service writes in the Wayland wire format.</summary>
    public sealed class ExtIdleNotifyPolicyTests
    {
        private static (uint ObjectId, ushort Opcode, byte[] Args) Single(byte[] bytes)
        {
            _ = KWinKeystatePolicy.TryRead(bytes, out WaylandMessage? message, out int used).Should().BeTrue();
            _ = used.Should().Be(bytes.Length, "one message");
            return (message!.ObjectId, message.Opcode, message.Args);
        }

        [Theory]
        [InlineData(1u, 1u)]
        [InlineData(2u, 2u)]
        [InlineData(9u, 2u)]
        public void The_notifier_is_bound_at_most_at_version_2(uint offered, uint bound)
        {
            // Version 2 adds the input-only notification; nothing later is used.
            _ = ExtIdleNotifyPolicy.BindVersion(offered).Should().Be(bound);
        }

        [Fact]
        public void Version_2_asks_for_input_idle_so_inhibitors_do_not_hold_it_back()
        {
            // As Windows' last-input time: a playing video's idle inhibitor must not stop the idle threshold.
            (uint objectId, ushort opcode, byte[] args) = Single(ExtIdleNotifyPolicy.Notify(7, notifierVersion: 2, TimeSpan.FromMinutes(5)));

            _ = objectId.Should().Be(ExtIdleNotifyPolicy.NotifierId);
            _ = opcode.Should().Be(2);
            _ = BinaryPrimitives.ReadUInt32LittleEndian(args).Should().Be(7u);
            _ = BinaryPrimitives.ReadUInt32LittleEndian(args.AsSpan(4)).Should().Be(300_000u);
            _ = BinaryPrimitives.ReadUInt32LittleEndian(args.AsSpan(8)).Should().Be(ExtIdleNotifyPolicy.SeatId);
        }

        [Fact]
        public void Version_1_falls_back_to_the_plain_idle_notification()
        {
            (_, ushort opcode, _) = Single(ExtIdleNotifyPolicy.Notify(7, notifierVersion: 1, TimeSpan.FromSeconds(30)));
            _ = opcode.Should().Be(1);
        }

        [Fact]
        public void Destroying_a_notification_is_its_opcode_0_with_no_arguments()
        {
            (uint objectId, ushort opcode, byte[] args) = Single(ExtIdleNotifyPolicy.Destroy(7));
            _ = objectId.Should().Be(7u);
            _ = opcode.Should().Be(0);
            _ = args.Should().BeEmpty();
        }

        [Fact]
        public void Binding_takes_the_seat_and_the_notifier()
        {
            byte[] bytes = ExtIdleNotifyPolicy.Bind(seatName: 10, notifierName: 20, notifierVersion: 2);
            _ = KWinKeystatePolicy.TryRead(bytes, out WaylandMessage? seat, out int used).Should().BeTrue();
            _ = KWinKeystatePolicy.TryRead(bytes.AsSpan(used), out WaylandMessage? notifier, out _).Should().BeTrue();

            _ = seat!.ObjectId.Should().Be(KWinKeystatePolicy.RegistryId);
            _ = BinaryPrimitives.ReadUInt32LittleEndian(seat.Args).Should().Be(10u);
            _ = BinaryPrimitives.ReadUInt32LittleEndian(seat.Args.AsSpan(seat.Args.Length - 4)).Should().Be(ExtIdleNotifyPolicy.SeatId);
            _ = BinaryPrimitives.ReadUInt32LittleEndian(notifier!.Args).Should().Be(20u);
            _ = BinaryPrimitives.ReadUInt32LittleEndian(notifier.Args.AsSpan(notifier.Args.Length - 8)).Should().Be(2u);
            _ = BinaryPrimitives.ReadUInt32LittleEndian(notifier.Args.AsSpan(notifier.Args.Length - 4)).Should().Be(ExtIdleNotifyPolicy.NotifierId);
        }

        [Theory]
        [InlineData(0.0, 1u)]
        [InlineData(1.5, 1500u)]
        public void Timeouts_are_whole_milliseconds_of_at_least_one(double seconds, uint expected)
        {
            _ = ExtIdleNotifyPolicy.TimeoutMs(TimeSpan.FromSeconds(seconds)).Should().Be(expected);
        }
    }
}
