# Mixdeck (legacy promises)

Rainmeter **Mixdeck** promised a fully customizable replacement for the Windows Volume Mixer.

## Archive reference

- Upstream: [Jax-Core/YourMixer](https://github.com/Jax-Core/YourMixer)
- Screenshots: [YourMixer/Screenshots.md](https://github.com/Jax-Core/YourMixer/blob/main/Screenshots.md)
- Promised behavior below is derived from upstream README + skin structure, not Host implementation.

## Promised behavior

- Per-application session list with mute and volume
- Multiple color schemes and appearance customization
- Hotkey / Tessera Pixel integration to open the mixer
- Redraw hooks driven by volume-change events (`Plugin=Tessera` in the Rainmeter tree)

## Host today

Native Mixdeck overlay MVP via Host capability + `MixdeckTileView`. See parity `tile_mixdeck_mvp`. Install stub: [`Tiles/Mixdeck`](../../Tiles/Mixdeck).
