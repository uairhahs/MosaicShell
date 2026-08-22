# Pulse (legacy promises)

Rainmeter **Pulse** promised a desktop **audio visualizer**.

## Archive reference

- Upstream: [Jax-Core/ModularVisualizer](https://github.com/Jax-Core/ModularVisualizer)
- Promised behavior below is derived from upstream README + skin structure, not Host implementation.

## Promised behavior

- Spectrum / bar / round visualizer driven by audio analysis plugins
- Style and visualizer-type options
- Desktop widget chrome (often always-on-top / desktop Z)
- JaxCore visualizer skin variants

## Host today

Native Pulse widget via `IAudioLevelService` bands. See parity `tile_pulse_mvp`. Install stub: [`Tiles/Pulse`](../../Tiles/Pulse).
