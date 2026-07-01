// -------------------------------------------------------------------------------------
// <copyright file="Lens.SettingsData.cs">
//   Copyright (c) 2026
//   Licensed under the MIT License. See LICENSE file in the project root.
// </copyright>
// -------------------------------------------------------------------------------------

namespace StrangeLens
{
   using System.Collections.Generic;

   public partial class Lens
   {
      private class SettingsData
      {
         public int GridOpacity { get; init; } = 20;

         public byte GridSize { get; init; } = 4;

         public int GridStyle { get; init; } = (int)GridStyleOption.Dash;

         public short Height { get; init; } = 160;

         public bool InfoShow12Bit { get; init; } = true;

         public bool InfoShowHex { get; init; } = true;

         public bool InfoShowHsl { get; init; } = true;

         public bool InfoShowMouse { get; init; } = true;

         public bool InfoShowRgb { get; init; } = true;

         public bool InfoShowSize { get; init; } = true;

         public bool InfoShowWeb { get; init; } = true;

         public bool InfoShowZoom { get; init; } = true;

         public byte Magnification { get; init; } = 4;

         public int PrecisionSpeed { get; init; } = 45;

         public int Scaling { get; init; } = (int)ScalingModeOption.NearestNeighbor;

         public string Theme { get; init; } = "system";

         public Dictionary<string, ThemePalette>? Themes { get; init; }

         public short Width { get; init; } = 150;
      }
   }
}
