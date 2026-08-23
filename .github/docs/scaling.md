# Scaling

MosaicShell Host uses Avalonia on Windows. Layout is in **logical DIPs**; per-monitor OS scale is handled by Avalonia, not multiplied again in custom layout.

## Hub window

The Hub sizes as a **fraction of the monitor work area** (`MainWindow`). On DPI or monitor changes it re-applies using `Screens`, `WorkingArea`, and each screen's `Scaling`.

Avoid hard-coded 1920x1080 layout. Read screen metrics at runtime or use Core layout helpers.

## Tessera flyouts

| Setting | Where | Range |
|---------|-------|-------|
| `FlyoutScalePercent` | `TesseraSettings` / `%LocalAppData%\MosaicShell\Config\modules\Tessera.json` | 50 to 150 |

Core: [`TesseraFlyoutRequestBuilder`](../../host/MosaicShell.Core/Modules/Tessera/TesseraFlyoutRequestBuilder.cs) (`flyoutScale` payload).

Host: [`AvaloniaFlyoutPresenter`](../../host/MosaicShell.Host/Capabilities/AvaloniaFlyoutPresenter.cs) (`LayoutTransform`).

Position: [`FlyoutAnchor`](../../host/MosaicShell.Core/Modules/Tessera/FlyoutAnchor.cs) + monitor index from settings.

## Third-party modules

Do not multiply OS DPI into module layout. See Avalonia [Windows high DPI](https://docs.avaloniaui.net/docs/platform-specific-guides/windows#high-dpi-and-per-monitor-scaling).
