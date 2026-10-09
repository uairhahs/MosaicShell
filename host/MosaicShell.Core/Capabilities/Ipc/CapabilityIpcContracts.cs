
using MosaicShell.Core.Modules.Tessera;

namespace MosaicShell.Core.Capabilities.Ipc
{
    /// <summary>Named pipe and protocol constants for Worker to Host flyout IPC.</summary>
    public static class CapabilityIpcPolicy
    {
        public const string PipeName = "MosaicShell.CapabilityFlyout.v1";
        public const int ConnectTimeoutMs = 4000;

        /// <summary>Pause between the Worker's connection attempts while Host is not listening yet.</summary>
        public const int ConnectRetryDelayMs = 500;

        /// <summary>How long the Worker keeps waiting for Host's flyout pipe before it gives up.</summary>
        public const int WorkerConnectWindowMs = 120_000;

        public const int MaxMessageBytes = 256 * 1024;
    }

    public enum CapabilityIpcMessageType
    {
        FlyoutShow,
        FlyoutUpdate,
        FlyoutSoftRefresh,
        FlyoutHide,
        FlyoutHideAll,
        TransientDismissed,
        FlyoutSessionSnapshot,
    }

    public sealed record FlyoutRequestDto(
        string ModuleId,
        string Kind,
        string? StyleId = null,
        string? Anchor = null,
        int AutoDismissMs = 2500,
        Dictionary<string, string>? Payload = null,
        int MonitorIndex = 1,
        int XPad = 20,
        int YPad = 20,
        int Ani = 2,
        string AniDir = "Left",
        string AniEase = TesseraFlyoutAnimationPolicy.DefaultEase,
        int AniSteps = TesseraFlyoutAnimationPolicy.DefaultAniSteps,
        int AnimationDisplacement = TesseraFlyoutAnimationPolicy.DefaultDisplacementPx);

    public sealed record TesseraFlyoutIpcSnapshotDto(
        string ModuleId,
        bool EffectivelyShowing,
        int Generation,
        string Mode,
        string Kind,
        string? StyleId,
        string? Phase = null)
    {
        public static TesseraFlyoutIpcSnapshotDto From(string moduleId, TesseraFlyoutSessionSnapshot snapshot)
        {
            return new(
                moduleId,
                snapshot.EffectivelyShowing,
                snapshot.Generation,
                snapshot.Mode.ToString(),
                snapshot.Kind,
                snapshot.StyleId,
                snapshot.Phase.ToString());
        }
    }

    public sealed record CapabilityIpcMessage(
        CapabilityIpcMessageType Type,
        FlyoutRequestDto? Request = null,
        string? ModuleId = null,
        TesseraFlyoutIpcSnapshotDto? SessionSnapshot = null);
}
