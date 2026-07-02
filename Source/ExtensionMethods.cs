// -------------------------------------------------------------------------------------
// <copyright file="ExtensionMethods.cs">
//   Copyright (c) 2026
//   Licensed under the MIT License. See LICENSE file in the project root.
// </copyright>
// -------------------------------------------------------------------------------------

namespace StrangeLens
{
   using System;
   using System.Drawing.Drawing2D;

   public static class ExtensionMethods
   {
      public static T Clamp<T>(this T value, T min, T max)
         where T : IComparable<T>
      {
         return value.CompareTo(min) < 0 ? min : value.CompareTo(max) > 0 ? max : value;
      }

      public static DashStyle DashStyle(this GridStyleOption gridStyle)
      {
         switch (gridStyle)
         {
            case GridStyleOption.Solid: return System.Drawing.Drawing2D.DashStyle.Solid;
            case GridStyleOption.Dash: return System.Drawing.Drawing2D.DashStyle.Dash;
            case GridStyleOption.Dot: return System.Drawing.Drawing2D.DashStyle.Dot;
            case GridStyleOption.DashDot: return System.Drawing.Drawing2D.DashStyle.DashDot;
            case GridStyleOption.DashDotDot: return System.Drawing.Drawing2D.DashStyle.DashDotDot;
            case GridStyleOption.None:
            default:
               throw new ArgumentOutOfRangeException(nameof(gridStyle), gridStyle, null);
         }
      }

      public static bool IsNearlyEqual(this float value, float other, float tolerance = 1e-6f)
      {
         return Math.Abs(value - other) < tolerance;
      }
   }
}
