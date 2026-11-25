using MosaicShell.Core.Services.Kde;
using Tmds.DBus.Protocol;

namespace MosaicShell.Core.Services.Linux
{
    /// <summary>
    /// KWin's <c>org.kde.kwin.Scripting</c>: load a script file under a name, start the loaded scripts, and unload a
    /// script by name. The OSD suppressor and the fullscreen probe both drive their KWin scripts through it.
    /// </summary>
    internal static class KWinScripting
    {
        public const string LoadScriptMember = "loadScript";
        public const string UnloadScriptMember = "unloadScript";
        public const string StartMember = "start";

        /// <summary>Loads <paramref name="file"/> as <paramref name="name"/>; KWin answers with the script's id.</summary>
        public static Task<int> LoadAsync(DBusConnection connection, string file, string name)
        {
            return DBusCalls.CallAsync(
                connection,
                KWinOsdScriptPolicy.KWinBusName,
                KWinOsdScriptPolicy.ScriptingPath,
                KWinOsdScriptPolicy.ScriptingInterface,
                LoadScriptMember,
                "ss",
                (ref w) =>
                {
                    w.WriteString(file);
                    w.WriteString(name);
                },
                static (m, _) => m.GetBodyReader().ReadInt32());
        }

        /// <summary>Unloads the script loaded as <paramref name="name"/>; KWin answers whether there was one.</summary>
        public static Task<bool> UnloadAsync(DBusConnection connection, string name)
        {
            return DBusCalls.CallAsync(
                connection,
                KWinOsdScriptPolicy.KWinBusName,
                KWinOsdScriptPolicy.ScriptingPath,
                KWinOsdScriptPolicy.ScriptingInterface,
                UnloadScriptMember,
                "s",
                (ref w) => w.WriteString(name),
                static (m, _) => m.GetBodyReader().ReadBool());
        }

        /// <summary>Unloads without waiting, for a connection that is about to close.</summary>
        public static bool UnloadNoReply(DBusConnection connection, string name)
        {
            return DBusCalls.SendNoReply(
                connection,
                KWinOsdScriptPolicy.KWinBusName,
                KWinOsdScriptPolicy.ScriptingPath,
                KWinOsdScriptPolicy.ScriptingInterface,
                UnloadScriptMember,
                "s",
                (ref w) => w.WriteString(name));
        }

        /// <summary>Runs the scripts loaded so far.</summary>
        public static Task StartAsync(DBusConnection connection)
        {
            return DBusCalls.CallAsync(
                connection,
                KWinOsdScriptPolicy.KWinBusName,
                KWinOsdScriptPolicy.ScriptingPath,
                KWinOsdScriptPolicy.ScriptingInterface,
                StartMember);
        }
    }
}
