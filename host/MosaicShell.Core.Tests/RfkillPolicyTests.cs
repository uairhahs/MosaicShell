using FluentAssertions;
using MosaicShell.Core.Services.Rfkill;

namespace MosaicShell.Core.Tests
{
    /// <summary>Airplane mode on Linux from rfkill events: every radio blocked, as Windows' radio state reports it.</summary>
    public sealed class RfkillPolicyTests
    {
        private static byte[] Event(uint idx, byte type, byte op, bool soft, bool hard)
        {
            byte[] bytes = new byte[RfkillPolicy.EventSize];
            _ = BitConverter.TryWriteBytes(bytes, idx);
            bytes[4] = type;
            bytes[5] = op;
            bytes[6] = soft ? (byte)1 : (byte)0;
            bytes[7] = hard ? (byte)1 : (byte)0;
            return bytes;
        }

        [Fact]
        public void An_event_reads_its_radio_operation_and_blocks()
        {
            _ = RfkillPolicy.TryParse(Event(3, type: 1, op: RfkillPolicy.OpChange, soft: true, hard: false), out RfkillEvent ev).Should().BeTrue();
            _ = ev.Should().Be(new RfkillEvent(3, 1, RfkillPolicy.OpChange, SoftBlocked: true, HardBlocked: false));
        }

        [Fact]
        public void A_short_event_is_not_read()
        {
            _ = RfkillPolicy.TryParse(new byte[5], out _).Should().BeFalse();
        }

        [Fact]
        public void Without_radios_there_is_no_airplane_mode()
        {
            // A desktop without wireless: nothing to switch off, so no flight flyout either.
            RfkillRadios radios = new();
            _ = radios.IsSupported.Should().BeFalse();
            _ = radios.IsAirplane.Should().BeFalse();
        }

        [Fact]
        public void Airplane_mode_is_every_radio_blocked_soft_or_hard()
        {
            RfkillRadios radios = new();
            radios.Apply(new RfkillEvent(0, 1, RfkillPolicy.OpAdd, SoftBlocked: true, HardBlocked: false));
            radios.Apply(new RfkillEvent(1, 2, RfkillPolicy.OpAdd, SoftBlocked: false, HardBlocked: true));
            _ = radios.IsSupported.Should().BeTrue();
            _ = radios.IsAirplane.Should().BeTrue();
        }

        [Fact]
        public void One_radio_still_on_is_not_airplane_mode()
        {
            RfkillRadios radios = new();
            radios.Apply(new RfkillEvent(0, 1, RfkillPolicy.OpAdd, SoftBlocked: true, HardBlocked: false));
            radios.Apply(new RfkillEvent(1, 2, RfkillPolicy.OpAdd, SoftBlocked: false, HardBlocked: false));
            _ = radios.IsAirplane.Should().BeFalse();

            radios.Apply(new RfkillEvent(1, 2, RfkillPolicy.OpChange, SoftBlocked: true, HardBlocked: false));
            _ = radios.IsAirplane.Should().BeTrue();
        }

        [Fact]
        public void A_removed_radio_no_longer_counts()
        {
            RfkillRadios radios = new();
            radios.Apply(new RfkillEvent(0, 1, RfkillPolicy.OpAdd, SoftBlocked: true, HardBlocked: false));
            radios.Apply(new RfkillEvent(1, 2, RfkillPolicy.OpAdd, SoftBlocked: false, HardBlocked: false));

            radios.Apply(new RfkillEvent(1, 2, RfkillPolicy.OpDelete, SoftBlocked: false, HardBlocked: false));

            _ = radios.IsAirplane.Should().BeTrue();
            radios.Apply(new RfkillEvent(0, 1, RfkillPolicy.OpDelete, SoftBlocked: true, HardBlocked: false));
            _ = radios.IsSupported.Should().BeFalse();
        }
    }
}
