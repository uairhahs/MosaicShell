# Native architecture

- **Host process** = tray CapabilityDaemon (`ShutdownMode.OnExplicitShutdown`)
- **Widgets** = TileRuntime overlays
- **Capabilities** (Tessera, Mixdeck, …) = `IModuleCapability` armed in-process
- **Settings** = JSON via `ModuleSettingsStore`
- **Styles** = `StyleCatalog` JaxCore ids → Avalonia factories

Tessera flow: OS events → `TesseraCapability` → OSD suppress → `AvaloniaFlyoutPresenter` → layout control.
