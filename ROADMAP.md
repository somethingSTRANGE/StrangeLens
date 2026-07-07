# Roadmap

Planned work for Lens. Completed items are removed; see git history for what shipped.

## UI Framework Migration (Top Priority)

The Settings and About panels are built on WinForms, which has fundamental limitations that make high-quality multi-monitor DPI support difficult to achieve cleanly. Both panels work correctly — text scales, layout reflows, and windows resize when moving between displays with different scaling — but the transition itself can flicker or glitch due to WinForms internals.

Migrating to **WinUI 3** (via Windows App SDK) will resolve this and unlock a range of improvements:

- Smooth, flicker-free DPI transitions — WinUI 3 handles per-monitor scaling natively with no manual WM_DPICHANGED plumbing
- Vector-based rendering throughout — controls, icons, and text all scale crisply at any DPI without custom Paint workarounds
- Modern control set — native toggle switches, sliders, combo boxes, and navigation patterns that match the Windows 11 design language
- Easier theming — system dark/light mode and accent color are first-class citizens
- Simpler layout model — no fixed pixel coordinates; adaptive layouts via Grid, StackPanel, etc.

The lens window itself (LensForm) performs GDI+ pixel capture and rendering that is inherently screen-coordinate-dependent and will stay as-is.

## Settings

- Make the global toggle hotkey user-configurable in the Settings panel (currently hardcoded to Ctrl+Alt+Shift+Z).

## Keyboard Shortcuts

- **Configurable shortcuts:** Store shortcut bindings in the settings JSON, edited by the user directly. Use a human-friendly string format (e.g. `ctrl + alt + shift + win + w`), case-insensitive and flexible about separators. AHK-style symbols (`^!+#`) are compact but require looking up which symbol maps to which modifier — not worth the cognitive overhead for a general audience. Parse entirely in-app, no AHK runtime dependency.

## Documentation

- Add ESC / Ctrl+Alt+Shift+Z behavior to the README: Ctrl+Alt+Shift+Z is the primary toggle; ESC only works when the lens window has focus and is a fallback, not the intended gesture. Consider a visual focus indicator on the lens border (e.g. a color change) so users know when ESC will work.
- Update the README with screenshots, key shortcuts, and full app details.
