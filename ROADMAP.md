# Roadmap

Planned work for Lens. Completed items are removed; see git history for what shipped.

## System Tray

- **Start with Windows:** Toggle in the tray context menu with a checkmark reflecting current state. Writes/removes `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` to enable/disable.

## Settings

- Make the global toggle hotkey user-configurable in the Settings panel (currently hardcoded to Ctrl+Alt+Shift+Z).

## Keyboard Shortcuts

- **Configurable shortcuts:** Store shortcut bindings in the settings JSON, edited by the user directly. Use a human-friendly string format (e.g. `ctrl + alt + shift + win + w`), case-insensitive and flexible about separators. AHK-style symbols (`^!+#`) are compact but require looking up which symbol maps to which modifier — not worth the cognitive overhead for a general audience. Parse entirely in-app, no AHK runtime dependency.

## Documentation

- Add ESC / Ctrl+Alt+Shift+Z behavior to the README: Ctrl+Alt+Shift+Z is the primary toggle; ESC only works when the lens window has focus and is a fallback, not the intended gesture. Consider a visual focus indicator on the lens border (e.g. a color change) so users know when ESC will work.
- Update the README with screenshots, key shortcuts, and full app details.
