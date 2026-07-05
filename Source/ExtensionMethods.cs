// -------------------------------------------------------------------------------------
// <copyright file="ExtensionMethods.cs">
//   Copyright (c) 2026
//   Licensed under the MIT License. See LICENSE file in the project root.
// </copyright>
// -------------------------------------------------------------------------------------

namespace StrangeLens
{
   using System;
   using System.Drawing;
   using System.Drawing.Drawing2D;

   public static class ExtensionMethods
   {
      public static T Clamp<T>(this T value, T min, T max)
         where T : IComparable<T>
      {
         return value.CompareTo(min) < 0 ? min : value.CompareTo(max) > 0 ? max : value;
      }

      public static Color Darken(this Color color, int percent)
      {
         // percent should be 0-100

         double hue = color.GetHue();  
         double saturation = color.GetSaturation();
         double lightness = color.GetBrightness();

         lightness *= 1 - (percent * 0.01);

         hue %= 360;
         if (hue < 0)
         {
            hue += 360;
         }

         saturation = Math.Clamp(saturation, 0.0, 1.0);
         lightness = Math.Clamp(lightness, 0.0, 1.0);

         var c = (1.0 - Math.Abs((2.0 * lightness) - 1.0)) * saturation;
         var h = hue / 60.0;
         var x = c * (1.0 - Math.Abs((h % 2.0) - 1.0));

         double r1 = 0, g1 = 0, b1 = 0;

         if (h < 1)
         {
            r1 = c;
            g1 = x;
         }
         else if (h < 2)
         {
            r1 = x;
            g1 = c;
         }
         else if (h < 3)
         {
            g1 = c;
            b1 = x;
         }
         else if (h < 4)
         {
            g1 = x;
            b1 = c;
         }
         else if (h < 5)
         {
            r1 = x;
            b1 = c;
         }
         else
         {
            r1 = c;
            b1 = x;
         }

         var m = lightness - (c / 2.0);

         var r = (int)Math.Round((r1 + m) * 255);
         var g = (int)Math.Round((g1 + m) * 255);
         var b = (int)Math.Round((b1 + m) * 255);

         return Color.FromArgb(Math.Clamp(r, 0, 255), Math.Clamp(g, 0, 255), Math.Clamp(b, 0, 255));
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
