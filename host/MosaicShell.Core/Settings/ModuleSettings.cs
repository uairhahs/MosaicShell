namespace MosaicShell.Core.Settings;

public sealed class ChronoSettings
{
    public string Style { get; set; } = "Center";
    public bool TwentyFourHour { get; set; } = true;
    public bool ShowSeconds { get; set; } = true;
}

public sealed class CanvasSettings
{
    public string Style { get; set; } = "DEFAULT";
    public bool ShowCpu { get; set; } = true;
    public bool ShowRam { get; set; } = true;
    public bool ShowDisk { get; set; } = true;
    public bool ShowHost { get; set; } = true;
}

public sealed class PhonoSettings
{
    public string Style { get; set; } = "Simple";
    public bool ShowArtist { get; set; } = true;
}

public sealed class PulseSettings
{
    public string Style { get; set; } = "Regular";
    public string VisualizerType { get; set; } = "Bar";
}

public sealed class TesseraSettings
{
    public string Style { get; set; } = "Fluent";
    public string Anchor { get; set; } = "BR";
    public int AutoDismissMs { get; set; } = 2500;
    public bool UseLegacyVolumeHooks { get; set; }
    public bool EnableMediaFlyouts { get; set; } = true;
    public bool EnableLockFlyouts { get; set; } = true;
    public double LegacyVolumeStep { get; set; } = 0.05;
}

public sealed class MixdeckSettings
{
    public string Style { get; set; } = "Fluent";
    public string HotkeyGesture { get; set; } = "Win+Q";
    public bool CloseOnEscape { get; set; } = true;
}

public sealed class InlaySettings
{
    public string Style { get; set; } = "Win11";
    public string HotkeyGesture { get; set; } = "Win+S";
    public List<string> Pins { get; set; } = ["notepad", "calc"];
}

public sealed class ChordSettings
{
    public string Style { get; set; } = "Center";
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
    public string Style { get; set; } = "DEFAULT";
    public string HotkeyGesture { get; set; } = "Win+A";
    public bool ShowMute { get; set; } = true;
}

public sealed class SlateSettings
{
    public string Style { get; set; } = "Center";
    public bool HideOnFullscreen { get; set; } = true;
    public int IdleSeconds { get; set; } = 300;
}

public sealed class HubSettings
{
    public bool WelcomeCompleted { get; set; }
    public bool AutostartHost { get; set; }
    /// <summary>When true, window close hides to tray; when false, close exits the host.</summary>
    public bool CloseMinimizesToTray { get; set; } = true;
    public List<string> BatchInstallSelection { get; set; } = [];
}
