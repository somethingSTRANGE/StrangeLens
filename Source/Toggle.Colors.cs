// -------------------------------------------------------------------------------------
// <copyright file="Toggle.Colors.cs">
//   Copyright (c) 2026
//   Licensed under the MIT License. See LICENSE file in the project root.
// </copyright>
// -------------------------------------------------------------------------------------

namespace StrangeLens;

using System.Drawing;

public sealed partial class Toggle
{
   public static class Colors
   {
      public static Color Focus { get; set; } = Color.MediumSlateBlue;

      public static Color Thumb { get; set; } = Color.Gainsboro;

      public static Color ThumbHover { get; set; } = Color.White;

      public static Color TrackActive { get; set; } = Color.MediumSlateBlue;

      public static Color TrackBase { get; set; } = Color.Gray;

      public static Color TrackHover { get; set; } = Color.RoyalBlue;
   }
}
