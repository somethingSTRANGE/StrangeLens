# Strange Lens

![Magnification](Docs/lens-magnification-anim.webp)

A pixel-precise screen magnifier, color picker, and measurement tool for Windows 10+, designed for UI development, pixel art, graphic design, and accessibility.

## Features

- Pixel-accurate screen magnifier with adjustable size and zoom
- Live color picker — Hex, RGB, HSL, 12-bit, and web-safe formats
- On-screen measurement tool with anchor points
- Precision mouse mode for sub-pixel alignment
- Global keyboard shortcuts
- System tray integration
- Portable — no installation required

## Installation

Strange Lens is fully portable. It requires no installer, writes no registry entries, and needs no administrator privileges to run.

1. Download the latest release
2. Unzip both `StrangeLens.exe` and `StrangeLens.Settings.exe` into the same folder — Settings and About are launched as a separate process that Strange Lens locates next to its own executable, so keep them together. A good choice is `%LocalAppData%\Programs\StrangeLens\`, because it keeps user-scoped apps in one place and requires no admin rights.

The optional **Start with Windows** feature writes a single registry key to launch the app at login; if you prefer to avoid that, drop a shortcut to the exe in your startup folder (`shell:startup`) instead.

## Usage

Single-click the system tray icon to toggle the lens, or use <kbd>Ctrl</kbd>+<kbd>Alt</kbd>+<kbd>Shift</kbd>+<kbd>Z</kbd> from anywhere.

Right-click the icon to open the context menu.

### Context Menu

- **Toggle Lens** — yet another way to toggle the lens.
- **Settings** — opens the configuration panel. Double-clicking the tray icon does the same.
- **Start with Windows** — toggles whether Strange Lens launches at login.
- **About** — opens the app version info. Click **Copy and Close** to copy the version string to the clipboard — useful when filing a bug report.

## Precision Mouse

Hold <kbd>Ctrl</kbd>+<kbd>Alt</kbd>+<kbd>Shift</kbd> while the lens is open to enter precision mode. Mouse movement is scaled to a fraction of its normal speed, making it easy to land the crosshair on a specific pixel. Adjust the speed in Settings under **Mouse Precision Speed**.

The Info panel shows current cursor coordinates and the active speed percentage while precision mode is active.

When the lens has focus, arrow keys nudge the cursor one pixel at a time regardless of precision mode.

## Measuring

Precision mode pairs naturally with measurement — use it to align the crosshair accurately before dropping the anchor.

1. Position the crosshair over one edge of the area you want to measure.
2. Press <kbd>Ctrl</kbd>+<kbd>Alt</kbd>+<kbd>Shift</kbd>+<kbd>Q</kbd> to drop the anchor.
3. Move the cursor to the opposite edge — the Info panel updates live with the current **Width × Height** delta.
4. Press <kbd>Ctrl</kbd>+<kbd>Alt</kbd>+<kbd>Shift</kbd>+<kbd>Q</kbd> again to dismiss, or close the lens.

Measurement mode remains active until you toggle it off or close the lens.

## Color Picking

The Info panel displays the color beneath the cursor in Hex, RGB, and HSL color formats, as well as 12-bit and web-safe color conversions. Each format has a dedicated copy shortcut (see table below). The copied format is confirmed briefly in the Info panel.

## Keyboard & Mouse Shortcuts

| Input | Action |
| --- | --- |
| <kbd>Ctrl</kbd>+<kbd>Alt</kbd>+<kbd>Shift</kbd>+<kbd>Z</kbd> | Toggle lens window open/closed |

The following shortcuts are only available when the lens is open.

| Input | Action |
| --- | --- |
| <kbd>Ctrl</kbd>+<kbd>Alt</kbd>+<kbd>Shift</kbd> | Precision mode is active while held |
| <kbd>Ctrl</kbd>+<kbd>Alt</kbd>+<kbd>Shift</kbd>+<kbd>X</kbd> | Copy color as Hex |
| <kbd>Ctrl</kbd>+<kbd>Alt</kbd>+<kbd>Shift</kbd>+<kbd>R</kbd> | Copy color as RGB |
| <kbd>Ctrl</kbd>+<kbd>Alt</kbd>+<kbd>Shift</kbd>+<kbd>S</kbd> | Copy color as HSL |
| <kbd>Ctrl</kbd>+<kbd>Alt</kbd>+<kbd>Shift</kbd>+<kbd>1</kbd> | Copy nearest 12-bit color |
| <kbd>Ctrl</kbd>+<kbd>Alt</kbd>+<kbd>Shift</kbd>+<kbd>W</kbd> | Copy nearest web-safe color |
| <kbd>Ctrl</kbd>+<kbd>Alt</kbd>+<kbd>Shift</kbd>+<kbd>Q</kbd> | Toggle measurement mode on/off |
| <kbd>Ctrl</kbd>+<kbd>Alt</kbd>+<kbd>Shift</kbd>+<kbd>Scroll</kbd> | Magnification zoom in/out |

The following shortcuts are only available when the lens is open and has focus, which is indicated by the blue border around the lens window.

| Input | Action |
| --- | --- |
| <kbd>Ctrl</kbd>+<kbd>=</kbd> and <kbd>Ctrl</kbd>+<kbd>-</kbd> | Magnification zoom in/out |
| <kbd>Arrow keys</kbd> | Nudge mouse cursor 1px |
| <kbd>ESC</kbd> | Close lens window |

<kbd>Ctrl</kbd>+<kbd>Alt</kbd>+<kbd>Shift</kbd>+<kbd>Z</kbd> is the primary way to open and close the lens. <kbd>ESC</kbd> is a fallback that only works when the lens window has focus. The lens window border color indicates when the window has focus.

## Troubleshooting

- **Lens doesn't appear or hotkeys don't respond**: Another application may have registered the same key combination. Check the system tray to confirm Strange Lens is running, then look for conflicts in other running apps (AutoHotkey scripts, keyboard utilities, etc.).

- **Colors look wrong on HDR displays**: GDI screen capture works in the SDR color space. Colors sampled from HDR content may not match what is visually rendered on screen.

- **Content from some windows appears black or missing**: Windows prevents GDI from reading pixels from processes running at a higher integrity level (e.g., apps launched as administrator). The lens will show a checkerboard pattern where that content would be.

## Privacy

- All processing happens locally — no data leaves your machine
- No network requests of any kind
- No analytics or telemetry
- Screen content is never stored or logged

## Building

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download).

```
dotnet build StrangeLens.sln
```

## License

Released under the [MIT License](LICENSE).
