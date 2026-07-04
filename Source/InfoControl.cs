// -------------------------------------------------------------------------------------
// <copyright file="InfoControl.cs">
//   Copyright (c) 2026
//   Licensed under the MIT License. See LICENSE file in the project root.
// </copyright>
// -------------------------------------------------------------------------------------

namespace StrangeLens
{
   using System;
   using System.Diagnostics.CodeAnalysis;
   using System.Drawing;

   /// <summary>Pure data class -- computes and caches the formatted strings displayed in the
   ///    info panel. Rendering is handled by <see cref="InfoForm"/>.</summary>
   internal class InfoControl
   {
      // Format patterns — used by the formatters below; most also referenced by InfoForm.ComputeContentW.
      internal const string PatternHex = "#{0:X2}{1:X2}{2:X2}";

      internal const string PatternHexShort = "#{0:X}{1:X}{2:X}";

      internal const string PatternHsl = "{0}, {1}%, {2}%";

      internal const string PatternLensSize = "{0}×{1}";

      internal const string PatternMeasure = "{0} × {1}";

      internal const string PatternMousePrecision = "{0}, {1} — {2}%";

      internal const string PatternRgb = "{0}, {1}, {2}";

      internal const string PatternZoom = "x{0}";

      private const string PatternMouse = "{0}, {1}";

      private DateTime copiedAt = DateTime.MinValue;

      private string copiedLabel = string.Empty;

      // Last-computed values below -- updated each frame by UpdateInfo.

      public Color Color12Bit { get; private set; }

      public Color ColorSwatch { get; private set; }

      public Color ColorWeb { get; private set; }

      public string LensSize { get; private set; } = string.Empty;

      public bool MeasureActive { get; private set; }

      public string MeasureValue { get; private set; } = string.Empty;

      public string MousePosition { get; private set; } = string.Empty;

      public string ValueColor12Bit { get; private set; } = string.Empty;

      public string ValueColorHex { get; private set; } = string.Empty;

      public string ValueColorHSL { get; private set; } = string.Empty;

      public string ValueColorRGB { get; private set; } = string.Empty;

      public string ValueColorWeb { get; private set; } = string.Empty;

      public string ZoomFactor { get; private set; } = string.Empty;

      /// <summary>Returns true within 600 ms of the last copy of the named row.</summary>
      public bool IsCopied(string label)
      {
         return (this.copiedLabel == label) && ((DateTime.UtcNow - this.copiedAt).TotalMilliseconds < 600);
      }

      public void NotifyCopied(string label)
      {
         this.copiedLabel = label;
         this.copiedAt = DateTime.UtcNow;
      }

      public void SetMeasure(bool active, int w = 0, int h = 0)
      {
         this.MeasureActive = active;
         this.MeasureValue = active ? string.Format(PatternMeasure, w, h) : string.Empty;
      }

      public void UpdateInfo(Point mousePosition, Color color, bool precisionActive, int precisionSpeed)
      {
         this.ValueColorHex = ColorAsHex(color);
         this.ValueColor12Bit = ColorAsHex(this.Color12Bit = ColorAs12Bit(color), shortForm: true);
         this.ValueColorWeb = ColorAsHex(this.ColorWeb = ColorAsWeb(color), shortForm: true);
         this.ValueColorRGB = ColorAsRGB(color);
         this.ValueColorHSL = ColorAsHSL(color);
         this.MousePosition = precisionActive
            ? string.Format(PatternMousePrecision, mousePosition.X, mousePosition.Y, precisionSpeed)
            : string.Format(PatternMouse, mousePosition.X, mousePosition.Y);
         this.LensSize = string.Format(PatternLensSize, Lens.Instance.Width, Lens.Instance.Height);
         this.ZoomFactor = string.Format(PatternZoom, Lens.Instance.Magnification);
         this.ColorSwatch = color;
      }

      private static Color ColorAs12Bit(Color color)
      {
         return Color.FromArgb(Snap(color.R), Snap(color.G), Snap(color.B));

         // Round each channel to the nearest multiple of 17 (0x11), then display as #XYZ.
         static byte Snap(byte c)
         {
            return (byte)(Math.Round(c / 17.0) * 17);
         }
      }

      private static string ColorAsHex(Color color, bool shortForm = false)
      {
         if (shortForm && (color.R % 17 == 0) && (color.G % 17 == 0) && (color.B % 17 == 0))
         {
            return string.Format(PatternHexShort, color.R / 17, color.G / 17, color.B / 17);
         }

         return string.Format(PatternHex, color.R, color.G, color.B);
      }

      [SuppressMessage("ReSharper", "InconsistentNaming")]
      private static string ColorAsHSL(Color color)
      {
         return string.Format(
            PatternHsl,
            Math.Round(color.GetHue(), 1),
            Math.Round(color.GetSaturation() * 100, 1),
            Math.Round(color.GetBrightness() * 100, 1));
      }

      [SuppressMessage("ReSharper", "InconsistentNaming")]
      private static string ColorAsRGB(Color color)
      {
         return string.Format(PatternRgb, color.R, color.G, color.B);
      }

      private static Color ColorAsWeb(Color color)
      {
         return Color.FromArgb(Snap(color.R), Snap(color.G), Snap(color.B));

         // Round each channel to the nearest web-safe step (0x00, 0x33, 0x66, 0x99, 0xCC, 0xFF).
         // All six steps are multiples of 17, so the result is always a 3-char hex value.
         static byte Snap(byte c)
         {
            return (byte)(Math.Round(c / 51.0) * 51);
         }
      }
   }
}
