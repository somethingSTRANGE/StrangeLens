# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

**Strange Lens** is a Windows desktop magnifying glass utility with pixel color picking. It renders a floating zoomed panel that follows the mouse, draws a precise crosshair between pixels, highlights the pixel closest to the origin (used for color sampling), and shows color info (hex/RGB/HSL) plus mouse position in a companion info panel. It lives in the system tray.

**Stack:** C# 12 / .NET 10 / WinForms / GDI+

## Build & Run

```bash
# Build (from solution root)
msbuild StrangeLens.sln /p:Configuration=Debug /p:Platform="Any CPU"
msbuild StrangeLens.sln /p:Configuration=Release /p:Platform="Any CPU"

# Run
./bin/Debug/net10.0-windows10.0.19041.0/StrangeLens.exe
```

Tests live in `Tests/StrangeLens.Tests.csproj` (NUnit). Run via Rider or `dotnet test`.

## Architecture

### Entry & Lifetime
- **Program.cs** — enforces single-instance via Mutex (`strange-lens-app-mutex`). `Application.Run` starts with `TrayForm` as the host; `LensForm` is created/shown on demand.

### Settings Singleton
- **LensSetting.cs** (`Lens` class) — `INotifyPropertyChanged` singleton holding all configuration (magnification, grid size/color/style, window dimensions, speed factor, info panel row toggles). Persisted as JSON to `%LOCALAPPDATA%\Strange\Strange Lens\settings.json` via `System.Text.Json`. Debounced save fires 500ms after the last change; also flushed on clean exit. Tracks per-property pending-save state so a reload (see `Lens.FileWatcher.cs`) never clobbers a local edit that hasn't been flushed to disk yet — needed because Settings now runs as a separate process (see `SettingsApp/`) that can write the same file concurrently.
- Default: 150×160px window, 4× magnification, 4px grid, Dash style.

### Rendering (LensForm)
- **LensForm.cs** — the magnifier window. A timer (~55ms) drives `Invalidate()`. In `OnPaint`:
  1. `CopyFromScreen` captures the virtual desktop.
  2. A `Graphics` transform scales the capture by the zoom factor with nearest-neighbor interpolation.
  3. Crosshair lines are drawn *between* pixels (not through them) at sub-pixel offsets derived from the zoom factor so they stay pixel-exact at every zoom level.
  4. A rectangle is drawn around the upper-left quadrant pixel nearest the origin — this is the sampled pixel.
- Mouse wheel / keyboard shortcuts adjust magnification (`-/+`) and move the cursor (arrow keys). Width, height, grid, and precision-speed are Settings-only — Lens itself only changes magnification, since that's the one thing worth adjusting without breaking flow to open Settings.
- When the cursor is hidden, system mouse speed is lowered proportionally to the zoom factor so movement feels natural.

### Info Panel (InfoControl)
- **InfoControl.cs** — a `UserControl` rendered via GDI+ layered painting. Displays: mouse X/Y, lens dimensions, zoom factor, color swatch, and color in Hex/RGB/HSL/12-bit/web-safe formats. Each row is individually togglable via settings. Panel hides entirely when all rows are disabled. Positions itself to the right of the lens, falling back to the left near the screen edge.

### Tray Host
- **TrayForm.cs** — never shown to the user; it's the app's permanent root for the whole run, independent of whether a lens session is active. Hosts the system tray icon and context menu (Toggle Lens / Settings / Start with Windows / About / Exit), registers the global hotkeys (`RegisterHotKey`/`WM_HOTKEY`), and is the form `Application.Run` uses to host the WinForms message loop. Settings and About are no longer WinForms UI — `TrayForm` launches them as a separate WinUI 3 process (see `SettingsApp/`).

### Helpers
- **ExtensionMethods.cs** — `Clamp<T>`, `Point.Deconstruct`, `RectangleF.Deconstruct`, `GridStyle.DashStyle()`.
- **GridStyle.cs** — `GridStyleOptions` enum: None, Solid, Dash, Dot, DashDot, DashDotDot.
- **PipeServer.cs** — named-pipe IPC stub, currently fully commented out.

## Key Design Constraints

- The crosshair must always fall *between* pixels regardless of zoom level. The rendering math in `LensForm.OnPaint` is intentional and precise — changes here need careful verification at multiple zoom levels.
- The sampled pixel is always the upper-left quadrant pixel nearest the crosshair origin. Color display in `InfoControl` depends on this contract.
- The project targets .NET 10 / `net10.0-windows10.0.19041.0`, minimum supported OS Windows 10 1809 (`SupportedOSPlatformVersion` 10.0.17763.0) — matches `SettingsApp`'s own floor. Windows 7/8/8.1 are not supported.

## Roadmap

See [ROADMAP.md](ROADMAP.md) for planned work.
