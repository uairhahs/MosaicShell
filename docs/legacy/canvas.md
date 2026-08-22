# Canvas (legacy promises)

Rainmeter **Canvas** (Plainext) promised a minimal **plain-text system information** widget.

## Archive reference

- Upstream: [Jax-Core/Plainext](https://github.com/Jax-Core/Plainext)
- Promised behavior below is derived from upstream README + skin structure, not Host implementation.

## Promised behavior

- Live CPU / RAM / disk / host (and related) text sections
- Section toggles and compact vs default layouts
- Dynamic window sizing for content
- Desktop widget chrome via shared Ctx

## Host today

Native Canvas widget via `ISystemMetricsService`. See parity `tile_canvas_mvp`. Install stub: [`Tiles/Canvas`](../../Tiles/Canvas).
