using MosaicShell.Core.Services.Kde;

namespace MosaicShell.Core.Services.Wayland
{
    /// <summary>
    /// Idle on Wayland from the standard <c>ext_idle_notifier_v1</c> protocol (KWin and wlroots compositors offer it):
    /// the compositor sends idled once the seat has had no input for the timeout, and resumed on the next input, so
    /// nothing is sampled. Written in the Wayland wire format with <see cref="KWinKeystatePolicy"/>'s helpers, on the
    /// Host's own connection as the lock keys are. Kept pure so it is tested on every OS.
    /// </summary>
    public static class ExtIdleNotifyPolicy
    {
        public const string NotifierInterface = "ext_idle_notifier_v1";
        public const string SeatInterface = "wl_seat";

        /// <summary>The registry, the sync callback and the first ids follow <see cref="KWinKeystatePolicy"/>'s hello.</summary>
        public const uint SeatId = 4;
        public const uint NotifierId = 5;

        /// <summary>Each notification takes a fresh id from here: an id is not reused until the compositor frees it.</summary>
        public const uint FirstNotificationId = 6;

        /// <summary>Version 2 adds the input-only notification; nothing later is used.</summary>
        public const uint MaxNotifierVersion = 2;

        /// <summary>The seat's version 1 is enough to name it; its events are not read.</summary>
        private const uint SeatVersion = 1;

        // Requests.
        private const ushort RegistryBind = 0;
        private const ushort NotifierGetIdleNotification = 1;
        private const ushort NotifierGetInputIdleNotification = 2;
        private const ushort NotificationDestroy = 0;

        // Notification events.
        public const ushort NotificationIdled = 0;
        public const ushort NotificationResumed = 1;

        public static uint BindVersion(uint offered)
        {
            return Math.Min(offered, MaxNotifierVersion);
        }

        /// <summary>Binds the seat and the notifier, at <see cref="BindVersion"/> for the notifier.</summary>
        public static byte[] Bind(uint seatName, uint notifierName, uint notifierVersion)
        {
            return [.. BindGlobal(seatName, SeatInterface, SeatVersion, SeatId), .. BindGlobal(notifierName, NotifierInterface, notifierVersion, NotifierId)];
        }

        /// <summary>
        /// A notification for the seat after <paramref name="timeout"/> without input. From version 2 it is the input-only
        /// one, so an idle inhibitor (a playing video) does not hold it back, as Windows' last-input time is not.
        /// </summary>
        public static byte[] Notify(uint notificationId, uint notifierVersion, TimeSpan timeout)
        {
            ushort opcode = notifierVersion >= 2 ? NotifierGetInputIdleNotification : NotifierGetIdleNotification;
            byte[] args = [.. KWinKeystatePolicy.U32(notificationId), .. KWinKeystatePolicy.U32(TimeoutMs(timeout)), .. KWinKeystatePolicy.U32(SeatId)];
            return KWinKeystatePolicy.Message(NotifierId, opcode, args);
        }

        public static byte[] Destroy(uint notificationId)
        {
            return KWinKeystatePolicy.Message(notificationId, NotificationDestroy, []);
        }

        /// <summary>Whole milliseconds, at least 1 and at most the protocol's 32-bit limit.</summary>
        public static uint TimeoutMs(TimeSpan timeout)
        {
            return (uint)Math.Clamp(Math.Round(timeout.TotalMilliseconds), 1, uint.MaxValue);
        }

        private static byte[] BindGlobal(uint name, string iface, uint version, uint newId)
        {
            byte[] args = [.. KWinKeystatePolicy.U32(name), .. KWinKeystatePolicy.String(iface), .. KWinKeystatePolicy.U32(version), .. KWinKeystatePolicy.U32(newId)];
            return KWinKeystatePolicy.Message(KWinKeystatePolicy.RegistryId, RegistryBind, args);
        }
    }
}
