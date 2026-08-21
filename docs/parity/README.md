# Parity checklist

Living flags live in `host/MosaicShell.Core.Tests/HubParityBacklogTests.cs`.

## Tessera

| Flag | Meaning |
|------|---------|
| `tessera_osd_flyout` | Armed flyout + OSD suppress path |
| `tessera_named_styles` | Style catalog JaxCore ids |
| `tessera_locks_flight` | Lock-key + airplane flyouts |
| `tessera_layout_fidelity` | Per-layout Avalonia chrome (11 styles) |
| `tessera_live_update_multimonitor` | Reuse/update window; monitor + anchor math |
| `tessera_fluent_yourflyouts` | Fluent transfer kit (Shape track, side media, SysAccent) |

Reference layout source: [Jax-Core/YourFlyouts](https://github.com/Jax-Core/YourFlyouts).

## Explicit non-goals (this pass)

- Interpreting Tessera layout `.inc` files
- Rainmeter FrostedGlass / Focus plugins
- Other tiles beyond Tessera → Mixdeck deep-link
