// -------------------------------------------------------------------------------------
// <copyright file="DpiHelper.cs">
//   Copyright (c) 2026
//   Licensed under the MIT License. See LICENSE file in the project root.
// </copyright>
// -------------------------------------------------------------------------------------

namespace StrangeLens.SettingsApp;

using System;
using System.Runtime.InteropServices;

using Microsoft.UI.Xaml;

using WinRT.Interop;

/// <summary>AppWindow.Resize (and MoveAndResize) take physical pixels, not the DIPs
///    everything else in WinUI3 -- Measure()'s DesiredSize, hardcoded "visible size"
///    constants, XAML layout in general -- works in. A size computed in DIPs needs
///    multiplying by the window's actual DPI scale factor before being handed to Resize(),
///    or the window ends up physically smaller than intended on any monitor above 100%
///    scaling: content still renders correctly scaled internally (WinUI3 handles that
///    automatically), but the window frame itself doesn't grow to match, so the correctly
///    scaled content ends up cramped/clipped inside a too-small window. This app is
///    explicitly Per-Monitor-V2 DPI aware (see DpiAwareness.SetPerMonitorV2), which is what
///    makes Windows leave this entirely up to the app instead of bitmap-stretching the
///    window for us.</summary>
internal static class DpiHelper
{
   /// <summary>The window's current DPI scale factor (1.0 at 100% scaling, 1.5 at 150%, etc.),
   ///    based on whichever monitor the window is actually on right now.</summary>
   internal static double GetScaleFactor(Window window)
   {
      var hwnd = WindowNative.GetWindowHandle(window);
      return GetDpiForWindow(hwnd) / 96.0;
   }

   [DllImport("user32.dll")]
   private static extern uint GetDpiForWindow(IntPtr hwnd);
}
