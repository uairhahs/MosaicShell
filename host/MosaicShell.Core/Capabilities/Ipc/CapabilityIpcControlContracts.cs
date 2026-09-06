namespace MosaicShell.Core.Capabilities.Ipc
{
    public static class CapabilityIpcControlPolicy
    {
        public const string PipeName = "MosaicShell.CapabilityControl.v1";
        public const int ConnectTimeoutMs = 4000;
        public const int MaxMessageBytes = 64 * 1024;
    }

    public enum CapabilityControlMessageType
    {
        Arm,
        Disarm,
        ReArm,
        IsArmed,
        GetArmedIds,
        GetHotkeyError,
        Restore,
        DisarmAll,
        Persist,
        // Responses
        BoolResult,
        StringListResult,
        StringResult,
    }

    public sealed record CapabilityControlMessage(
        CapabilityControlMessageType Type,
        int RequestId = 0,
        string? ModuleId = null,
        bool Persist = true,
        bool BoolValue = false,
        string? StringValue = null,
        IReadOnlyList<string>? StringList = null);
}
