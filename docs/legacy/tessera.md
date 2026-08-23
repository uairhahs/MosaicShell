# Tessera (legacy promises)

Rainmeter **Tessera** promised YourFlyouts-class volume / brightness / media / lock / airplane **flyouts** that replace the stock Windows OSD while the skin was active.

## Archive reference

- Upstream: [Jax-Core/YourFlyouts](https://github.com/Jax-Core/YourFlyouts)
- Screenshots: [YourFlyouts/Screenshots.md](https://github.com/Jax-Core/YourFlyouts/blob/main/Screenshots.md)
- Promised behavior below is derived from upstream README + skin structure, not Host implementation.

## Promised behavior

- Volume, brightness, media, caps/num/scroll lock, and airplane-mode flyouts
- Multiple named visual styles (Fluent, Windows11, Square, Material You, …)
- Soft frost / focus dim Host chrome
- Material You layout deep-link into the volume mixer (Mixdeck)
- Position anchors and multi-monitor placement
- Media NowPlaying (including browser / plugin paths) with album art when available
- Appearance / size / blur DLC-style customization via Core settings

## Host today

Native capability in `MosaicShell.Host` — see [`docs/native-rewrite.md`](../native-rewrite.md) and [`docs/parity/README.md`](../parity/README.md). Install stub: [`Tiles/Tessera`](../../Tiles/Tessera).
