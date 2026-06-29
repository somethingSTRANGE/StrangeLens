# Strange Lens

![Magnification](Docs/lens-magnification-anim.webp)

A pixel-precise screen magnifier with color picker for Windows. Lives in the system tray.

## Keyboard & Mouse Shortcuts

| Input | Condition | Action |
| --- | --- | --- |
| `Ctrl+Alt+Shift+Z` | Always | Toggle lens open/closed |
| `Ctrl+Alt+Shift+Scroll` | Lens open, any focus | Zoom in/out |
| `Ctrl+=` / `Ctrl+-` | Lens focused | Zoom in/out |
| `[` / `]` | Lens focused | Width narrower/wider |
| `;` / `'` | Lens focused | Height shorter/taller |
| Arrow keys | Lens focused | Nudge cursor 1px |
| `Ctrl+Alt+Shift+H` | Lens open, any focus | Copy color as HSL |
| `Ctrl+Alt+Shift+R` | Lens open, any focus | Copy color as RGB |
| `Ctrl+Alt+Shift+X` | Lens open, any focus | Copy color as Hex |
| `Ctrl+Alt+Shift+W` | Lens open, any focus | Copy nearest web-safe color |
| `Ctrl+Alt+Shift+A` / `B` | Lens open, any focus | Copy nearest 12-bit color |
| `ESC` | Lens focused | Close lens |

`Ctrl+Alt+Shift+Z` is the primary way to open and close the lens. `ESC` is a fallback that only works when the lens window has focus — the border color indicates this (see below).
