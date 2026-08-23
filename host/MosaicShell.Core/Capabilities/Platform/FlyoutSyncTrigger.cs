namespace MosaicShell.Core.Capabilities.Platform;

/// <summary>
/// Platform-neutral reason a capability is refreshing a flyout (Present vs Patch routing).
/// Modules map module-specific events to these triggers; Core owns dismiss and revive policy.
/// </summary>
public enum FlyoutSyncTrigger
{
    Volume,
    Brightness,
    MediaSession,
    ShellMedia,
    StatusToggle,
}

public enum FlyoutSyncAction
{
    Present,
    Patch,
}
