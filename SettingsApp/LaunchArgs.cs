// -------------------------------------------------------------------------------------
// <copyright file="LaunchArgs.cs">
//   Copyright (c) 2026
//   Licensed under the MIT License. See LICENSE file in the project root.
// </copyright>
// -------------------------------------------------------------------------------------

namespace StrangeLens.SettingsApp;

using System;
using System.Linq;

/// <summary>Settings and About share one process/exe (avoids re-solving DPI-awareness,
///    self-contained deployment, and single-instance plumbing a third time) and pick which
///    window to show from a command-line flag instead.</summary>
internal static class LaunchArgs
{
   internal static bool IsAboutMode =>
      Environment.GetCommandLineArgs().Contains("--about", StringComparer.OrdinalIgnoreCase);
}
