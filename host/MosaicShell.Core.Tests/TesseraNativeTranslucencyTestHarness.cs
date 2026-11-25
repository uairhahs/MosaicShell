using MosaicShell.Core.Runtime;
using MosaicShell.Core.Settings;

namespace MosaicShell.Core.Tests
{
    /// <summary>Post sign-off, hub <see cref="TesseraSettings.UseNativeTranslucency"/> enables the trial.</summary>
    internal static class TesseraNativeTranslucencyTestHarness
    {
        public static void EnableHubNativeTranslucency(bool enabled = true)
        {
            ModuleSettingsStore.Save("Tessera", new TesseraSettings { UseNativeTranslucency = enabled });
        }
    }
}
