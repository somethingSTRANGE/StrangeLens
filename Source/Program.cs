// -------------------------------------------------------------------------------------
// <copyright file="Program.cs">
//   Copyright (c) 2026
//   Licensed under the MIT License. See LICENSE file in the project root.
// </copyright>
// -------------------------------------------------------------------------------------

namespace StrangeLens
{
   using System;
   using System.Diagnostics;
   using System.IO;
   using System.Threading;
   using System.Windows.Forms;

   internal static class Program
   {
      /// <summary>The main entry point for the application.</summary>
      [STAThread]
      private static void Main()
      {
         var logsDir = Path.Combine(Application.StartupPath, "Logs");
         Directory.CreateDirectory(logsDir);
         Trace.Listeners.Add(new TextWriterTraceListener(Path.Combine(logsDir, "lens-debug.log")));
         Debug.AutoFlush = true;
         Debug.WriteLine("-----");
         Debug.WriteLine($"Lens started: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n");

         Application.ThreadException += (_, e) => AppLog.Error("ThreadException: " + e.Exception);
         AppDomain.CurrentDomain.UnhandledException +=
            (_, e) => AppLog.Error("UnhandledException: " + e.ExceptionObject);

         using (new Mutex(true, "strange-lens-app-mutex", out var createdNew))
         {
            if (createdNew)
            {
               Application.EnableVisualStyles();
               Application.SetCompatibleTextRenderingDefault(false);
               Lens.Instance.Load();
               var colorMode = Lens.Instance.Theme switch
                  {
                     "dark" => SystemColorMode.Dark,
                     "light" => SystemColorMode.Classic,
                     _ => SystemColorMode.System,
                  };

               Debug.WriteLine($"Theme: {Lens.Instance.Theme} -> {colorMode}");

               Application.SetColorMode(colorMode);
               _ = new SettingsForm();
               Application.ApplicationExit += (_, _) => Lens.Instance.Save();
               Application.Run();
            }
            else
            {
               var current = Process.GetCurrentProcess();
               foreach (var process in Process.GetProcessesByName(current.ProcessName))
               {
                  if (process.Id != current.Id)
                  {
                     Debug.WriteLine("Already open");
                     break;
                  }
               }
            }
         }
      }
   }
}
