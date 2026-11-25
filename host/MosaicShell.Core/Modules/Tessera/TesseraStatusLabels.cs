using MosaicShell.Core.Capabilities;
using MosaicShell.Core.Capabilities.Platform;

namespace MosaicShell.Core.Modules.Tessera
{
    /// <summary>Status chip text for locks / flight flyouts.</summary>
    public static class TesseraStatusLabels
    {
        public static string Format(FlyoutRequest request)
        {
            bool on = request.Payload?.GetValueOrDefault("on") == "1";
            if (FlyoutKinds.Is(request.Kind, FlyoutKinds.Flight))
            {
                return on ? "Airplane mode On" : "Airplane mode Off";
            }

            string lockName = request.Payload?.GetValueOrDefault("lock") ?? "CapsLock";
            return on ? $"{lockName} On" : $"{lockName} Off";
        }
    }
}
