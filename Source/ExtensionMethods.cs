// -------------------------------------------------------------------------------------
// <copyright file="ExtensionMethods.cs">
//   Copyright (c) 2026
//   Licensed under the MIT License. See LICENSE file in the project root.
// </copyright>
// -------------------------------------------------------------------------------------

namespace Lens
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

      public static DashStyle DashStyle(this GridStyleOptions gridStyle)
      {
         switch (gridStyle)
         {
            case GridStyleOptions.Solid: return System.Drawing.Drawing2D.DashStyle.Solid;
            case GridStyleOptions.Dash: return System.Drawing.Drawing2D.DashStyle.Dash;
            case GridStyleOptions.Dot: return System.Drawing.Drawing2D.DashStyle.Dot;
            case GridStyleOptions.DashDot:
               return System.Drawing.Drawing2D.DashStyle.DashDot;
            case GridStyleOptions.DashDotDot:
               return System.Drawing.Drawing2D.DashStyle.DashDotDot;
            case GridStyleOptions.None:
            default:
               throw new ArgumentOutOfRangeException(nameof(gridStyle), gridStyle, null);
         }
      }
   }
}
