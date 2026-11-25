using Tmds.DBus.Protocol;

namespace MosaicShell.Core.Services.Linux
{
    /// <summary>Writes a method call's body. MessageWriter is a ref struct, so it is passed by ref, never captured.</summary>
    internal delegate void DBusBodyWriter(ref MessageWriter writer);

    /// <summary>
    /// The method call every Linux service makes: header, body, send, and the reply read or ignored. The writer is
    /// disposed in a finally rather than with <c>using</c>, which would stop it being passed by ref (writing to a copy
    /// sends a malformed message and the bus drops the connection).
    /// </summary>
    internal static class DBusCalls
    {
        public const string BusName = "org.freedesktop.DBus";
        public const string BusPath = "/org/freedesktop/DBus";
        public const string PropertiesInterface = "org.freedesktop.DBus.Properties";

        public static MessageBuffer Message(
            DBusConnection connection,
            string destination,
            string path,
            string iface,
            string member,
            string? signature = null,
            DBusBodyWriter? write = null,
            MessageFlags flags = MessageFlags.None)
        {
            MessageWriter writer = connection.GetMessageWriter();
            try
            {
                writer.WriteMethodCallHeader(destination, path, iface, member, signature, flags);
                write?.Invoke(ref writer);
                return writer.CreateMessage();
            }
            finally
            {
                writer.Dispose();
            }
        }

        public static Task<T> CallAsync<T>(
            DBusConnection connection,
            string destination,
            string path,
            string iface,
            string member,
            string? signature,
            DBusBodyWriter? write,
            MessageValueReader<T> reader)
        {
            return connection.CallMethodAsync(Message(connection, destination, path, iface, member, signature, write), reader);
        }

        /// <summary>A call whose reply carries nothing; it still waits for the reply (or the error).</summary>
        public static Task CallAsync(
            DBusConnection connection,
            string destination,
            string path,
            string iface,
            string member,
            string? signature = null,
            DBusBodyWriter? write = null)
        {
            return connection.CallMethodAsync(Message(connection, destination, path, iface, member, signature, write));
        }

        /// <summary>A call sent with no reply asked for; false when the connection could not take it.</summary>
        public static bool SendNoReply(
            DBusConnection connection,
            string destination,
            string path,
            string iface,
            string member,
            string? signature = null,
            DBusBodyWriter? write = null)
        {
            return connection.TrySendMessage(
                Message(connection, destination, path, iface, member, signature, write, MessageFlags.NoReplyExpected));
        }
    }
}
