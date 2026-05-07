# Roadmap

Planned work for Lens. Completed items are removed; see git history for what shipped.

## Settings

- Add **Show Grid / Show Crosshair** checkbox to the Settings panel to toggle the crosshair overlay (and possibly the entire grid).
- Make the global toggle hotkey user-configurable in the Settings panel (currently hardcoded to Ctrl+Shift+Z).

## Info Panel

- Consider adding a thin border around the Info panel to visually separate it from the desktop.

## UX / Interaction

- Add a held-key override (e.g. Space) that temporarily restores normal mouse speed while the lens is open, bypassing the zoom-proportional slowdown. Useful on multi-monitor setups where moving the lens across displays at high zoom is impractical.
- Beautification pass on the out-of-bounds checkerboard: tune tile size (currently 8×8px), colors, and consider a non-tiling pattern (e.g. diagonal hazard stripes) at high zoom levels. Evaluate whether the tile should be defined in screen pixels vs. capture pixels so it doesn't scale with magnification.
- Panel placement axis fallback: when neither left nor right of the cursor has enough room (e.g. large panel on a portrait display), fall back to placing panels above or below the cursor based on which half of the screen the cursor is in. Currently the panels stay on the last stable side but may extend off-screen.

## Documentation

- Add ESC / Ctrl+Shift+Z behavior to the README: Ctrl+Shift+Z is the primary toggle; ESC only works when the lens window has focus and is a fallback, not the intended gesture. Consider a visual focus indicator on the lens border (e.g. a color change) so users know when ESC will work.
- Update the README with screenshots, key shortcuts, and full app details.

## Theming

- Theme colors currently apply only to the Settings panel. Any future panels (About, Shortcuts/Help) should use the active theme palette.
- The Lens and Info panel colors are hardcoded. If they become user-configurable, expose them as additional `ThemePalette` properties rather than standalone settings.
- `GridColor` stays outside the theme as a runtime user choice — switching themes shouldn't reset it. If a per-theme default grid color is ever added to `ThemePalette`, add a **Reset** button next to the Grid Color picker in Settings that restores the color to the active theme's default.

## Future Considerations

- Add an About panel.
- Consider localization support.
