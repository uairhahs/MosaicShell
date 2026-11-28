using System.Runtime.InteropServices;
using MosaicShell.Core.Install;
using MosaicShell.Core.Runtime;

namespace MosaicShell.Host
{
    /// <summary>
    /// Host commands that run from a terminal and exit before any window or capability starts. This is the one place the
    /// Host writes to the console: there is no UI yet, and the terminal is the only reader. On Windows the Host is a
    /// windowed app, so it attaches to the console of the terminal that started it first.
    /// </summary>
    internal static class HostCommandLine
    {
        /// <summary><c>--install-package</c>: 0 installed, 1 could not, 2 no path given.</summary>
        public static int InstallPackage(string? path)
        {
            AttachToParentConsole();
            if (path is null)
            {
                Console.Error.WriteLine(PackageInstallText.MissingPath);
                return 2;
            }

            try
            {
                string id = new ModuleInstaller().InstallFromPackage(path);
                Console.WriteLine(PackageInstallText.Installed(id, ModuleManifest.TryLoad(id)?.Kind));
                return 0;
            }
            catch (Exception ex) when (ex is InvalidOperationException or IOException or ArgumentException or UnauthorizedAccessException
                or System.Text.Json.JsonException or InvalidDataException)
            {
                Console.Error.WriteLine(PackageInstallText.Failed(ex.Message));
                return 1;
            }
        }

        private static void AttachToParentConsole()
        {
            if (OperatingSystem.IsWindows())
            {
                _ = AttachConsole(AttachParentProcess);
            }
        }

        private const int AttachParentProcess = -1;

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AttachConsole(int processId);
    }
}
