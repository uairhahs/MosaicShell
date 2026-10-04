# Capability platform

Core platform services sit **between** `HostServices` (OS adapters) and `IModuleCapability` (modules). Modules should build UI requests and subscribe to normalized signals; they should not reimplement flyout routing, dismiss suppress, or media polling.

## Design goal

| Layer                                              | Responsibility                                                          |
| -------------------------------------------------- | ----------------------------------------------------------------------- |
| `HostServices`                                     | Win32 / SMTC / UI Automation / shell hook adapters                      |
| `CapabilityDaemon`                                 | Arm/disarm, persist, create `ICapabilityContext`, `ICapabilityEventBus` |
| `CapabilityFlyoutPlatform`                         | Per-module flyout sessions, transient-dismiss wiring                    |
| `CapabilityFlyoutSession`                          | Present vs Patch vs SoftRefresh, auto-dismiss reset policy              |
| `MediaSessionPlatform`                             | Classify `Changed` / `Progress`, track boundaries                       |
| `ICapabilityEventBus`                              | Cross-module signals (media track boundary, volume, lifecycle)          |
| `IModuleCapability`                                | Module settings, `FlyoutRequest` builders, module-specific hooks        |
| Host `IFlyoutPresenter`                            | Avalonia HWND, coalesced patch, outside-click                           |
| `CapabilityIpcFlyoutServer` / `IpcFlyoutPresenter` | Worker process flyout forwarding over named pipe                        |

## Module API: `ICapabilityContext`

Factories receive context instead of raw `HostServices` + `ICapabilityUiBridge`:

```csharp
public interface ICapabilityContext
{
    HostServices Services { get; }
    ICapabilityUiBridge Ui { get; }
    CapabilityFlyoutSession Flyouts { get; }
    MediaSessionPlatform Media { get; }
    ICapabilityEventBus Events { get; }
}
```

### Typical capability pattern

1. **Arm**: start module hooks; call `Media.Acquire()` if the module cares about media.
2. **React**: build `FlyoutRequest`, call `Flyouts.Route(...)`.
3. **Media**: subscribe to `Media.Signal` or `Events.Subscribe(MediaTrackBoundary, ...)`.
4. **Disarm**: release hooks, `Media.Release()`, `Flyouts.Hide()`.

### What modules must not do

- Duplicate SMTC timeline timers (use `MediaSessionPlatform` / `HostServices.Media`)
- Call `IFlyoutPresenter` directly for routed flyouts (use `CapabilityFlyoutSession.Route`)
- Reimplement dismiss-after-outside-click suppress
- Encode Present vs Patch policy in Host Avalonia code

## Mosaic: many armed modules

One `CapabilityDaemon` runs **every** armed capability in `capabilities.json` (for example Tessera, Mixdeck and Slate together). That is the default mosaic model.

`CapabilityStore.SaveArmed` accepts a list; Hub toggles add/remove ids without disarming others.

## Tray-only Host

Start armed capabilities without showing the Hub:

```text
MosaicShell.Host.exe --tray-only
```

Tray icon remains; flyout presenter and IPC server start normally. Open Hub from the tray menu when needed.

## Worker process (optional split)

Use when you want the daemon in a headless process and flyout UI on Host. **Not** a single-module limit; Worker restores **all** armed modules from `capabilities.json`.

```text
1. MosaicShell.Worker.exe (acquires the daemon mutex; runs every armed module)
2. Within 4 seconds, start Host (full or --tray-only) for tray, Hub, widgets and the flyout IPC server
3. Host finds the mutex taken and uses RemoteCapabilityHost, so Hub arm/disarm still works (control IPC)
4. Flyouts render on Host via MosaicShell.CapabilityFlyout.v1
```

Order matters. Whichever process starts first owns the daemon, so a Host started first runs the
capabilities itself and the Worker exits. Worker waits `CapabilityIpcPolicy.ConnectTimeoutMs` (4 s)
for the Host's flyout server before giving up.

Control plane pipe: `MosaicShell.CapabilityControl.v1`.

Normal use: **Host only** with Tessera, Mixdeck and Slate armed together. Worker is optional, and it
has no browser media sources (Grout), so its media flyouts carry SMTC data only.

Overlay hotkeys (Mixdeck) in Worker still need Host for overlay UI until overlay IPC exists.

Build:

```powershell
dotnet build host/MosaicShell.Worker
dotnet run --project host/MosaicShell.Worker
```

## Event bus

`CapabilityDaemon.Events` publishes:

| Kind                                     | When                          |
| ---------------------------------------- | ----------------------------- |
| `MediaTrackBoundary`                     | Track change detected         |
| `MediaSessionChanged`                    | Metadata/session change       |
| `MediaProgress`                          | Timeline tick                 |
| `VolumeChanged`                          | Master volume changed         |
| `FlyoutTransientDismissed`               | Auto-dismiss or outside click |
| `CapabilityArmed` / `CapabilityDisarmed` | Daemon lifecycle              |

Subscribe from any armed capability via `context.Events.Subscribe(...)`.

## Platform policies (Core)

Shared routing policies live under `host/MosaicShell.Core/Capabilities/Platform/`.

Tessera-specific policies (`Modules/Tessera/Tessera*Policy.cs`) hold Tessera's own decisions, such as
dismiss, focus dim, glass and stacked placement, and each has its own Core tests.

## IPC flyout protocol

Under `host/MosaicShell.Core/Capabilities/Ipc/`:

- Length-prefixed JSON frames (`CapabilityIpcCodec`)
- Worker client: `IpcFlyoutPresenter` implements `IFlyoutPresenter`
- Host server: `CapabilityIpcFlyoutServer` applies messages on the UI thread

## Tests

- `CapabilityPlatformRuntimeTests` (launch options, event bus, IPC codec, daemon events)
- `CapabilityPlatformTests` (flyout suppress)
- `TestCapabilityContext.Create(...)` for module tests
