// -------------------------------------------------------------------------------------
// <copyright file="AppLog.cs">
//   Copyright (c) 2026
//   Licensed under the MIT License. See LICENSE file in the project root.
// </copyright>
// -------------------------------------------------------------------------------------

namespace StrangeLens
{
   using System;
   using System.Diagnostics;
   using System.IO;
   using System.Windows.Forms;

   internal static class AppLog
   {
      private static readonly string LogPath = Path.Combine(
         Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
         "Strange",
         Application.ProductName ?? "Strange Lens",
         "log.txt");

      internal static void Error(string message)
      {
         Debug.WriteLine($"ERROR: {message}");
         try
         {
            Directory.CreateDirectory(Path.GetDirectoryName(LogPath)!);
            File.AppendAllText(
               LogPath,
               $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}  ERROR  {message}{Environment.NewLine}");
         }
         catch { }
      }
   }
}
