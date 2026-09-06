namespace MosaicShell.Core.Modules.Tessera
{
    /// <summary>
    /// WH_KEYBOARD_LL legacy volume hook must install on the Host UI thread with a Win32 message pump.
    /// Lock keys use <see cref="LockKeyPollPolicy"/> polling instead.
    /// </summary>
    public static class TesseraArmPolicy
    {
        public const bool MustInstallKeyboardHooksOnHostMessagePumpThread = true;

        public static bool RequiresHostThreadForStart(string hookServiceId)
        {
            return hookServiceId is "LegacyVolumeKeys";
        }
    }
}
