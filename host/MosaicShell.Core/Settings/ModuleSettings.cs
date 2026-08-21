namespace MosaicShell.Core.Settings;

public sealed class ChronoSettings
{
    public string Style { get; set; } = "Center";
    public bool TwentyFourHour { get; set; } = true;
    public bool ShowSeconds { get; set; } = true;
}

public sealed class CanvasSettings
{
    public bool ShowCpu { get; set; } = true;
    public bool ShowRam { get; set; } = true;
    public bool ShowDisk { get; set; } = true;
    public bool ShowHost { get; set; } = true;
}

public sealed class PhonoSettings
{
    public bool ShowArtist { get; set; } = true;
}

public sealed class PulseSettings
{
    public string VisualizerType { get; set; } = "Bar";
}

public sealed class TesseraSettings
{
    public int AutoDismissMs { get; set; } = 2500;
}

public sealed class MixdeckSettings
{
    public bool CloseOnEscape { get; set; } = true;
}

public sealed class InlaySettings
{
    public List<string> Pins { get; set; } = ["notepad", "calc"];
}

public sealed class ChordSettings
{
    public string HotkeyGesture { get; set; } = "Ctrl+Space";
    public List<ChordAction> Actions { get; set; } = [];
}

public sealed class ChordAction
{
    public string Name { get; set; } = "";
    public string Target { get; set; } = "";
}

public sealed class SubstrateSettings
{
    public bool ShowMute { get; set; } = true;
}

public sealed class SlateSettings
{
    public bool HideOnFullscreen { get; set; } = true;
}

public sealed class HubSettings
{
    public bool WelcomeCompleted { get; set; }
    public bool AutostartHost { get; set; }
    public List<string> BatchInstallSelection { get; set; } = [];
}
