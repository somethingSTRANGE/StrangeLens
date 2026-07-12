// -------------------------------------------------------------------------------------
// <copyright file="ExtensionMethods.cs">
//   Copyright (c) 2026
//   Licensed under the MIT License. See LICENSE file in the project root.
// </copyright>
// -------------------------------------------------------------------------------------

namespace StrangeLens
{
   using System;
   using System.Diagnostics.CodeAnalysis;
   using System.Drawing.Drawing2D;

   using Windows.UI;

   public static class ExtensionMethods
   {
      public static Color AdjustLightness(this Color color, double amount)
      {
         // amount: -1.0 to +1.0
         var (h, s, l) = ToHsl(color);

         if (amount < 0)
         {
            l *= 1.0 + amount; // darker
         }
         else
         {
            l += (1.0 - l) * amount; // lighter
         }

         return FromHsl(h, s, l, color.A);
      }

      public static T Clamp<T>(this T value, T min, T max)
         where T : IComparable<T>
      {
         return value.CompareTo(min) < 0 ? min : value.CompareTo(max) > 0 ? max : value;
      }

      public static System.Drawing.Color Darken(this System.Drawing.Color color, int percent)
      {
         percent = Math.Clamp(percent, 0, 100);

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

         return System.Drawing.Color.FromArgb(
            Math.Clamp(r, 0, 255),
            Math.Clamp(g, 0, 255),
            Math.Clamp(b, 0, 255));
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

      public static System.Drawing.Color ToDrawingColor(this Color color)
      {
         return System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B);
      }

      public static Color ToWindowsColor(this System.Drawing.Color color)
      {
         return Color.FromArgb(color.A, color.R, color.G, color.B);
      }

      private static int ClampToByte(double value)
      {
         return (int)Math.Clamp(Math.Round(value), 0, 255);
      }

      private static Color FromHsl(double h, double s, double l, int alpha = 255)
      {
         double r, g, b;

         if (s == 0)
         {
            // Achromatic
            r = g = b = l;
         }
         else
         {
            var q = l < 0.5 ? l * (1 + s) : (l + s) - (l * s);

            var p = (2 * l) - q;

            r = HueToRgb(p, q, (h / 360) + (1.0 / 3.0));
            g = HueToRgb(p, q, h / 360);
            b = HueToRgb(p, q, (h / 360) - (1.0 / 3.0));
         }

         return Color.FromArgb(
            (byte)alpha,
            (byte)ClampToByte(r * 255),
            (byte)ClampToByte(g * 255),
            (byte)ClampToByte(b * 255));
      }

      private static double HueToRgb(double p, double q, double t)
      {
         if (t < 0)
         {
            t += 1;
         }

         if (t > 1)
         {
            t -= 1;
         }

         if (t < 1.0 / 6)
         {
            return p + ((q - p) * 6 * t);
         }

         if (t < 1.0 / 2)
         {
            return q;
         }

         if (t < 2.0 / 3)
         {
            return p + ((q - p) * ((2.0 / 3) - t) * 6);
         }

         return p;
      }

      [SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator")]
      private static (double H, double S, double L) ToHsl(Color color)
      {
         var r = color.R / 255.0;
         var g = color.G / 255.0;
         var b = color.B / 255.0;

         var max = Math.Max(r, Math.Max(g, b));
         var min = Math.Min(r, Math.Min(g, b));

         double h;
         double s;
         var l = (max + min) / 2.0;

         if (max == min)
         {
            // Achromatic
            s = 0;
            h = 0;
         }
         else
         {
            var delta = max - min;

            s = l > 0.5 ? delta / (2.0 - max - min) : delta / (max + min);

            if (max == r)
            {
               h = ((g - b) / delta) + (g < b ? 6 : 0);
            }
            else if (max == g)
            {
               h = ((b - r) / delta) + 2;
            }
            else
            {
               h = ((r - g) / delta) + 4;
            }

            h *= 60;
         }

         return (h, s, l);
      }
   }
}
