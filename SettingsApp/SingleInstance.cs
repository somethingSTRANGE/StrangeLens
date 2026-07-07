// -------------------------------------------------------------------------------------
// <copyright file="SingleInstance.cs">
//   Copyright (c) 2026
//   Licensed under the MIT License. See LICENSE file in the project root.
// </copyright>
// -------------------------------------------------------------------------------------

namespace StrangeLens.SettingsApp;

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

internal static class SingleInstance
{
   private const int SwRestore = 9;

   // Held for the lifetime of the process, so a second launch sees it already owned.
   [SuppressMessage("ReSharper", "NotAccessedField.Local")]
   private static Mutex? mutex;

   [ModuleInitializer]
   internal static void EnsureSingleInstance()
   {
      mutex = new Mutex(true, "strange-lens-settings-app-mutex", out var createdNew);
      if (createdNew)
      {
         return;
      }

      var existing = FindWindow(null, "Strange Lens Settings (spike)");
      if (existing != 0)
      {
         ShowWindow(existing, SwRestore);
         SetForegroundWindow(existing);
      }

      Environment.Exit(0);
   }

   [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
   private static extern nint FindWindow(string? lpClassName, string lpWindowName);

   [DllImport("user32.dll")]
   private static extern bool SetForegroundWindow(nint hWnd);

   [DllImport("user32.dll")]
   private static extern bool ShowWindow(nint hWnd, int nCmdShow);
}
