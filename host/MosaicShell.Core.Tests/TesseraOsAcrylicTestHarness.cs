using MosaicShell.Core.Runtime;
using MosaicShell.Core.Settings;

namespace MosaicShell.Core.Tests
{
    /// <summary>Post sign-off, hub <see cref="TesseraSettings.UseOsAcrylic"/> enables the trial.</summary>
    internal static class TesseraOsAcrylicTestHarness
    {
        public static void EnableHubOsAcrylic(bool enabled = true)
        {
            ModuleSettingsStore.Save("Tessera", new TesseraSettings { UseOsAcrylic = enabled });
        }
    }
}
