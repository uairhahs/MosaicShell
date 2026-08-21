# MosaicShell native rewrite

Avalonia + Windows APIs only. No Rainmeter bridge, no `.ini` interpreter.

## Tessera

Armed capability (not a Library overlay widget). Replaces OS volume/brightness HUD while armed.

### Delivered

| Area | Status |
|------|--------|
| Flyout kinds | `vol`, `bright`, `media`, `locks`, `flight` |
| Media backend | SMTC only (JaxCore “Modern”) |
| Layouts | Fluent thin rail + Win11 ~50px bar; other 9 approximations |
| Placement | Default **TL**; 9-point Position; re-anchor after measure |
| Settings | Placement / Timing / Motion / What to show / Advanced |
| OSD | Burst suppress; legacy volume hooks default on |
| Pixel | Device list + Mixdeck deep-link |

### Layout fidelity (Fluent-first)

- **Fluent** is the YourFlyouts transfer hub: 80×200 vertical rail + sideways media (~500), custom Shape track, SysAccent, acrylic hint
- **Win11** reuses the same kit: 320×50 row + media below
- Remaining 9 layouts consume kit pieces at lower fidelity next

Reference: https://github.com/Jax-Core/YourFlyouts and `Tiles/Tessera/Main/Layout/Fluent.inc`
