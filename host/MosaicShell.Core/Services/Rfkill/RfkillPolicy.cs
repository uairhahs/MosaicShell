using System.Buffers.Binary;

namespace MosaicShell.Core.Services.Rfkill
{
    /// <summary>One <c>struct rfkill_event</c>: a radio's index and type, what happened to it, and its blocks.</summary>
    public readonly record struct RfkillEvent(uint Index, byte Type, byte Op, bool SoftBlocked, bool HardBlocked);

    /// <summary>
    /// Airplane mode on Linux from the kernel's rfkill events (<c>/dev/rfkill</c>): on when there is at least one radio and
    /// every radio is blocked, by software or by a hardware switch, as Windows' system radio state is off when every
    /// radio is. Kept pure so it is tested on every OS.
    /// </summary>
    public static class RfkillPolicy
    {
        /// <summary>The kernel's version 1 event, which a reader gets unless it asks for more.</summary>
        public const int EventSize = 8;

        public const byte OpAdd = 0;
        public const byte OpDelete = 1;
        public const byte OpChange = 2;

        /// <summary>Reads idx (u32), type, op, soft and hard (u8 each), in the machine's byte order (little-endian here).</summary>
        public static bool TryParse(ReadOnlySpan<byte> bytes, out RfkillEvent ev)
        {
            ev = default;
            if (bytes.Length < EventSize)
            {
                return false;
            }

            ev = new RfkillEvent(BinaryPrimitives.ReadUInt32LittleEndian(bytes), bytes[4], bytes[5], bytes[6] != 0, bytes[7] != 0);
            return true;
        }
    }

    /// <summary>The radios rfkill has reported, kept current from its events.</summary>
    public sealed class RfkillRadios
    {
        private readonly Dictionary<uint, bool> _blocked = [];

        /// <summary>Without a radio there is nothing to switch off, so no airplane mode.</summary>
        public bool IsSupported => _blocked.Count > 0;

        public bool IsAirplane => _blocked.Count > 0 && _blocked.Values.All(static b => b);

        public void Apply(RfkillEvent ev)
        {
            switch (ev.Op)
            {
                case RfkillPolicy.OpAdd:
                case RfkillPolicy.OpChange:
                    _blocked[ev.Index] = ev.SoftBlocked || ev.HardBlocked;
                    break;
                case RfkillPolicy.OpDelete:
                    _ = _blocked.Remove(ev.Index);
                    break;
            }
        }
    }
}
