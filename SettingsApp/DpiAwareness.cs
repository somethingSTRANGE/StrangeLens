// -------------------------------------------------------------------------------------
// <copyright file="DpiAwareness.cs">
//   Copyright (c) 2026
//   Licensed under the MIT License. See LICENSE file in the project root.
// </copyright>
// -------------------------------------------------------------------------------------

namespace StrangeLens.SettingsApp;

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

internal static class DpiAwareness
{
   private static readonly nint perMonitorAwareV2 = -4;

   [ModuleInitializer]
   internal static void SetPerMonitorV2()
   {
      SetProcessDpiAwarenessContext(perMonitorAwareV2);
   }

   [DllImport("user32.dll", SetLastError = true)]
   private static extern bool SetProcessDpiAwarenessContext(nint value);
}
