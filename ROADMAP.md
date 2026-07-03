# Roadmap

Planned work for Lens. Completed items are removed; see git history for what shipped.

## Measurement

Add a ruler/measurement mode that lets the user measure pixel distances on the desktop without counting grid squares.

**Behavior**

- A global hotkey (registered at app start alongside existing shortcuts) drops the measurement anchor at the current cursor position. A second press dismisses. Closing the Lens also dismisses; reopening does not reactivate.
- The anchor pixel is `lastCursorPos` if precision mode is active, `Cursor.Position` otherwise — same logic RenderFrame already uses.
- The live corner opposite the anchor is always `lastCursorPos` (the sampled pixel — the lower-right quadrant pixel highlighted by the crosshair box). Both endpoints are inclusive: no cursor movement yields 1×1, moving right 10px yields 11×1.
- The hotkey handler no-ops if the Lens is not open (same pattern as existing global shortcuts).
- Pressing the hotkey a second time, or closing the Lens, dismisses the measurement.

**MeasureForm**

- New `MeasureForm` class: `WS_EX_LAYERED` + `WS_EX_TOPMOST` + `WS_EX_NOACTIVATE`, click-through, no activation. Follows InfoForm's UpdateLayeredWindow pattern, without the Gaussian shadow pipeline.
- 1px border, fully transparent interior. Border is 1px to avoid rendering artifacts on narrow rects (e.g. 100×1).
- Border animates white↔dark-gray ping-pong, driven by LensForm's existing timer, so it reads clearly against any background.
- Positioned at the bounding rect of anchor and live corner; resized and repositioned every frame.
- Because it is a real DWM-composited window at screen coordinates, LensForm's CopyFromScreen captures and magnifies its border naturally. The crosshair and grid draw over it inside the Lens. The two edges of the rect that pass through the crosshair center are always aligned to the current sampled pixel.

**InfoPanel**

- A dynamic "Measure" row is added to InfoPanel. No user-facing settings toggle — it appears only while measuring and is absent otherwise. Existing `ComputeContentH` / `RenderContent` conditional structure handles this without structural changes.
- Displays `W × H` (e.g. `124 × 48`).
- Uses a placeholder icon from the existing set for the initial implementation; a dedicated ruler SVG will be substituted once the feature is working.

**Hotkey**

- Suggested binding: Ctrl+Alt+Shift+Q (left-hand accessible alongside existing modifier-key chords; avoids existing bindings).
- Will become user-configurable once the broader configurable-shortcuts system is implemented.

## Settings

- Make the global toggle hotkey user-configurable in the Settings panel (currently hardcoded to Ctrl+Alt+Shift+Z).

## Keyboard Shortcuts

- **Configurable shortcuts:** Store shortcut bindings in the settings JSON, edited by the user directly. Use a human-friendly string format (e.g. `ctrl + alt + shift + win + w`), case-insensitive and flexible about separators. AHK-style symbols (`^!+#`) are compact but require looking up which symbol maps to which modifier — not worth the cognitive overhead for a general audience. Parse entirely in-app, no AHK runtime dependency.

## Documentation

- Add ESC / Ctrl+Alt+Shift+Z behavior to the README: Ctrl+Alt+Shift+Z is the primary toggle; ESC only works when the lens window has focus and is a fallback, not the intended gesture. Consider a visual focus indicator on the lens border (e.g. a color change) so users know when ESC will work.
- Update the README with screenshots, key shortcuts, and full app details.
