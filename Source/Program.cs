// -------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Greyborn Studios LLC">
//   Copyright 2015-2026 Greyborn Studios LLC. All rights reserved.
// </copyright>
// -------------------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace Lens
{
   internal static class Program
   {
      private const uint SW_RESTORE = 0x09;

      private static SettingsForm settingsForm;

      [DllImport("user32.dll")]
      [return: MarshalAs(UnmanagedType.Bool)]
      private static extern bool SetForegroundWindow(IntPtr hWnd);


      [DllImport("user32.dll")]
      private static extern IntPtr SetActiveWindow(IntPtr hWnd);


      [DllImport("user32.dll")]
      private static extern int ShowWindow(IntPtr hWnd, uint Msg);

      public static void Restore(this Form form)
      {
         if (form.WindowState == FormWindowState.Minimized) ShowWindow(form.Handle, SW_RESTORE);
      }


      /// <summary>
      ///    The main entry point for the application.
      /// </summary>
      [STAThread]
      private static void Main()
      {
         var logsDir = System.IO.Path.Combine(Application.StartupPath, "Logs");
         System.IO.Directory.CreateDirectory(logsDir);
         Trace.Listeners.Add(new TextWriterTraceListener(
            System.IO.Path.Combine(logsDir, "lens-debug.log")));
         Debug.AutoFlush = true;
         Debug.WriteLine("-----");
         Debug.WriteLine($"Lens started: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n");

         Application.ThreadException += (s, e) =>
         {
            Debug.WriteLine("ThreadException: " + e.Exception);
            LensForm.EmergencyRestoreMouseSpeed();
         };
         AppDomain.CurrentDomain.UnhandledException += (s, e) =>
         {
            Debug.WriteLine("UnhandledException: " + e.ExceptionObject);
            LensForm.EmergencyRestoreMouseSpeed();
         };

         var createdNew = true;
         using (var mutex = new Mutex(true, "strange-lens-app-mutex", out createdNew))
         {
            if (createdNew)
            {
               Application.EnableVisualStyles();
               Application.SetCompatibleTextRenderingDefault(false);
               Lens.Instance.Load();
               var colorMode = Lens.Instance.DebugTheme switch
               {
                  "dark"  => SystemColorMode.Dark,
                  "light" => SystemColorMode.Classic,
                  _       => SystemColorMode.System
               };
               Debug.WriteLine($"Theme: {colorMode} (debugTheme={Lens.Instance.DebugTheme ?? "none"})");
               Application.SetColorMode(colorMode);
               settingsForm = new SettingsForm();
               Application.ApplicationExit += (_, _) => Lens.Instance.Save();
               Application.Run();
            }
            else
            {
               var current = Process.GetCurrentProcess();
               foreach (var process in Process.GetProcessesByName(current.ProcessName))
                  if (process.Id != current.Id)
                  {
                     Console.WriteLine("Already open");
                     break;
                  }
            }
         }
      }
   }
}