using System.Buffers.Binary;
using System.Text;

namespace MosaicShell.Core.Services.Kde
{
    /// <summary>One Wayland message: the object it is for, its opcode, and its argument bytes.</summary>
    public sealed record WaylandMessage(uint ObjectId, ushort Opcode, byte[] Args);

    /// <summary>
    /// Lock keys on KDE from KWin's <c>org_kde_kwin_keystate</c> Wayland protocol, the one Plasma's Keyboard Indicator
    /// uses: KWin pushes each change, so nothing is sampled. The Host is an XWayland client, so it opens its own small
    /// Wayland connection for this; the few messages it needs pass no file descriptors, so they are written here in the
    /// Wayland wire format (little-endian 32-bit words, header then arguments) rather than through libwayland.
    /// Kept pure so it is tested on every OS.
    /// </summary>
    public static class KWinKeystatePolicy
    {
        public const string Interface = "org_kde_kwin_keystate";

        /// <summary>
        /// Version 1 reports Caps, Num and Scroll Lock; version 5 adds a press of every modifier, which would wake the
        /// Host for each Shift press.
        /// </summary>
        public const uint BindVersion = 1;

        public const uint DisplayId = 1;
        public const uint RegistryId = 2;
        public const uint SyncCallbackId = 3;
        public const uint KeystateId = 4;

        // Requests.
        private const ushort DisplaySync = 0;
        private const ushort DisplayGetRegistry = 1;
        private const ushort RegistryBind = 0;
        private const ushort KeystateFetchStates = 0;

        // Events.
        public const ushort DisplayError = 0;
        public const ushort RegistryGlobal = 0;
        public const ushort CallbackDone = 0;
        public const ushort KeystateStateChanged = 0;

        private const uint StateLocked = 2;

        /// <summary>
        /// The compositor socket, as libwayland finds it: WAYLAND_DISPLAY when absolute, else under XDG_RUNTIME_DIR,
        /// with <c>wayland-0</c> when unset. Null without a runtime directory for a relative name.
        /// </summary>
        public static string? SocketPath(string? waylandDisplay, string? runtimeDir)
        {
            string display = string.IsNullOrEmpty(waylandDisplay) ? "wayland-0" : waylandDisplay;
            return display.StartsWith('/')
                ? display
                : string.IsNullOrEmpty(runtimeDir) ? null : runtimeDir.TrimEnd('/') + "/" + display;
        }

        /// <summary>get_registry then sync: the sync's done marks the end of the globals, so a missing keystate is known.</summary>
        public static byte[] Hello()
        {
            return [.. Message(DisplayId, DisplayGetRegistry, U32(RegistryId)), .. Message(DisplayId, DisplaySync, U32(SyncCallbackId))];
        }

        /// <summary>Binds the keystate global at <see cref="BindVersion"/> and asks for the current states.</summary>
        public static byte[] BindAndFetch(uint globalName)
        {
            byte[] bind = [.. U32(globalName), .. String(Interface), .. U32(BindVersion), .. U32(KeystateId)];
            return [.. Message(RegistryId, RegistryBind, bind), .. Message(KeystateId, KeystateFetchStates, [])];
        }

        /// <summary>Reads one whole message from the front of <paramref name="buffer"/>; false until all of it has arrived.</summary>
        public static bool TryRead(ReadOnlySpan<byte> buffer, out WaylandMessage? message, out int consumed)
        {
            message = null;
            consumed = 0;
            if (buffer.Length < 8)
            {
                return false;
            }

            uint word = BinaryPrimitives.ReadUInt32LittleEndian(buffer[4..]);
            int size = (int)(word >> 16);
            if (size < 8)
            {
                throw new InvalidDataException($"Wayland message size {size} is below the header size.");
            }

            if (buffer.Length < size)
            {
                return false;
            }

            message = new WaylandMessage(BinaryPrimitives.ReadUInt32LittleEndian(buffer), (ushort)(word & 0xFFFF), buffer[8..size].ToArray());
            consumed = size;
            return true;
        }

        /// <summary>wl_registry.global: the global's name, interface and version.</summary>
        public static (uint Name, string Interface, uint Version) ReadGlobal(byte[] args)
        {
            uint name = BinaryPrimitives.ReadUInt32LittleEndian(args);
            int length = (int)BinaryPrimitives.ReadUInt32LittleEndian(args.AsSpan(4));
            string iface = length == 0 ? "" : Encoding.UTF8.GetString(args, 8, length - 1);
            uint version = BinaryPrimitives.ReadUInt32LittleEndian(args.AsSpan(8 + Padded(length)));
            return (name, iface, version);
        }

        /// <summary>
        /// stateChanged as a lock key and whether it is on; null for any other key (version 5's modifiers). On means
        /// locked: a latched lock key is not on.
        /// </summary>
        public static LockKeyState? ReadStateChanged(byte[] args)
        {
            uint key = BinaryPrimitives.ReadUInt32LittleEndian(args);
            uint state = BinaryPrimitives.ReadUInt32LittleEndian(args.AsSpan(4));
            LockKeyKind? kind = key switch
            {
                0 => LockKeyKind.CapsLock,
                1 => LockKeyKind.NumLock,
                2 => LockKeyKind.ScrollLock,
                _ => null,
            };
            return kind is { } k ? new LockKeyState(k, state == StateLocked) : null;
        }

        public static byte[] Message(uint objectId, ushort opcode, byte[] args)
        {
            byte[] message = new byte[8 + args.Length];
            BinaryPrimitives.WriteUInt32LittleEndian(message, objectId);
            BinaryPrimitives.WriteUInt32LittleEndian(message.AsSpan(4), ((uint)message.Length << 16) | opcode);
            args.CopyTo(message, 8);
            return message;
        }

        public static byte[] U32(uint value)
        {
            byte[] bytes = new byte[4];
            BinaryPrimitives.WriteUInt32LittleEndian(bytes, value);
            return bytes;
        }

        /// <summary>A Wayland string: its length with the terminating NUL, then the bytes padded to a 32-bit boundary.</summary>
        public static byte[] String(string value)
        {
            byte[] text = Encoding.UTF8.GetBytes(value);
            byte[] bytes = new byte[4 + Padded(text.Length + 1)];
            BinaryPrimitives.WriteUInt32LittleEndian(bytes, (uint)text.Length + 1);
            text.CopyTo(bytes, 4);
            return bytes;
        }

        private static int Padded(int length)
        {
            return (length + 3) & ~3;
        }
    }
}
