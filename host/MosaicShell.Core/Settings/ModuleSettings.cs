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
    /// <summary>Nine-point anchor: TL/TC/TR/CL/CC/CR/BL/BC/BR (JaxCore default TL)</summary>
    public string Position { get; set; } = "TL";
    public int MonitorIndex { get; set; } = 1;
    public int XPad { get; set; } = 20;
    public int YPad { get; set; } = 20;
    public int AutoDismissMs { get; set; } = 2000;
    /// <summary>0 = fade, 1 = fast slide+fade, 2 = fancy slide+fade</summary>
    public int Ani { get; set; } = 2;
    /// <summary>Left/Right/Top/Bottom</summary>
    public string AniDir { get; set; } = "Left";
    /// <summary>Default on - Win11 volume notifications are often unreliable (JaxCore guidance).</summary>
    public bool UseLegacyVolumeHooks { get; set; } = true;
    public double LegacyVolumeStep { get; set; } = 0.02;
    public bool EnableMediaFlyouts { get; set; } = true;
    public bool EnableLockFlyouts { get; set; } = true;
    public bool EnableFlightFlyouts { get; set; } = true;
    public bool ShowMediaStripOnVolume { get; set; } = true;
    /// <summary>Soft frost tint on flyout shell (not OS acrylic).</summary>
    public bool UseAcrylicBackdrop { get; set; } = true;
    /// <summary>Subtle click-through desktop dim behind flyout.</summary>
    public bool UseFocusDim { get; set; } = true;
}

public sealed class MixdeckSettings
{
    public string Style { get; set; } = "Fluent";
    public string HotkeyGesture { get; set; } = "Ctrl+Alt+M";
    public bool CloseOnEscape { get; set; } = true;
}

public sealed class InlaySettings
{
    public string Style { get; set; } = "Win11";
    public string HotkeyGesture { get; set; } = "Ctrl+Alt+I";
    public List<string> Pins { get; set; } = ["notepad", "calc"];
}

public sealed class ChordSettings
{
    public string Style { get; set; } = "Center";
    public string HotkeyGesture { get; set; } = "Ctrl+Alt+K";
    public List<ChordAction> Actions { get; set; } =
    [
        new() { Name = "Notepad", Target = "notepad" },
        new() { Name = "Calculator", Target = "calc" },
        new() { Name = "Settings", Target = "ms-settings:" }
    ];
}

public sealed class ChordAction
{
    public string Name { get; set; } = "";
    public string Target { get; set; } = "";
}

public sealed class SubstrateSettings
{
    public string Style { get; set; } = "DEFAULT";
    public string HotkeyGesture { get; set; } = "Ctrl+Alt+Q";
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
