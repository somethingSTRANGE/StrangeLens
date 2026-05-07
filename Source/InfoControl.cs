// -------------------------------------------------------------------------------------
// <copyright file="InfoControl.cs" company="Strange Entertainment LLC">
//   Copyright 2004-2023 Strange Entertainment LLC. All rights reserved.
// </copyright>
// -------------------------------------------------------------------------------------

using System;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;

namespace Lens
{
   /// <summary>
   ///   Pure data class — computes and caches the formatted strings displayed in the info panel.
   ///   Rendering is handled by <see cref="InfoForm"/>.
   /// </summary>
   internal class InfoControl
   {
      // Last-computed values — updated each frame by UpdateInfo.
      public string ValueColorHex   { get; private set; } = string.Empty;
      public string ValueColor12Bit { get; private set; } = string.Empty;
      public string ValueColorWeb   { get; private set; } = string.Empty;
      public string ValueColorRGB   { get; private set; } = string.Empty;
      public string ValueColorHSL   { get; private set; } = string.Empty;
      public string MousePosition  { get; private set; } = string.Empty;
      public string LensSize       { get; private set; } = string.Empty;
      public string ZoomFactor     { get; private set; } = string.Empty;
      public Color  ColorSwatch    { get; private set; }
      public Color  Color12Bit     { get; private set; }
      public Color  ColorWeb       { get; private set; }

      private string copiedLabel = string.Empty;
      private DateTime copiedAt  = DateTime.MinValue;

      public void NotifyCopied(string label)
      {
         this.copiedLabel = label;
         this.copiedAt    = DateTime.UtcNow;
      }

      /// <summary>Returns true within 600 ms of the last copy of the named row.</summary>
      public bool IsCopied(string label) =>
         this.copiedLabel == label && (DateTime.UtcNow - this.copiedAt).TotalMilliseconds < 600;

      public void UpdateInfo(Point mousePosition, Color color)
      {
         this.ValueColorHex   = ColorAsHex(color);
         this.ValueColor12Bit = ColorAsHex(this.Color12Bit = ColorAs12Bit(color), shortForm: true);
         this.ValueColorWeb   = ColorAsHex(this.ColorWeb = ColorAsWeb(color), shortForm: true);
         this.ValueColorRGB   = ColorAsRGB(color);
         this.ValueColorHSL   = ColorAsHSL(color);
         this.MousePosition  = $"{mousePosition.X}, {mousePosition.Y}";
         this.LensSize       = $"{Lens.Instance.Width}×{Lens.Instance.Height}";
         this.ZoomFactor     = $"x{Lens.Instance.Magnification}";
         this.ColorSwatch    = color;
      }

      private static string ColorAsHex(Color color, bool shortForm = false)
      {
         if (shortForm && color.R % 17 == 0 && color.G % 17 == 0 && color.B % 17 == 0)
            return $"#{color.R / 17:X}{color.G / 17:X}{color.B / 17:X}";
         return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
      }

      private static Color ColorAs12Bit(Color color)
      {
         return Color.FromArgb(Snap(color.R), Snap(color.G), Snap(color.B));

         // Round each channel to the nearest multiple of 17 (0x11), then display as #XYZ.
         static byte Snap(byte c) => (byte)(Math.Round(c / 17.0) * 17);
      }

      private static Color ColorAsWeb(Color color)
      {
         return Color.FromArgb(Snap(color.R), Snap(color.G), Snap(color.B));

         // Round each channel to the nearest web-safe step (0x00, 0x33, 0x66, 0x99, 0xCC, 0xFF).
         // All six steps are multiples of 17, so the result is always a 3-char hex value.
         static byte Snap(byte c) => (byte)(Math.Round(c / 51.0) * 51);
      }

      [SuppressMessage("ReSharper", "InconsistentNaming")]
      private static string ColorAsHSL(Color color)
      {
         var hue        = Math.Round(color.GetHue(), 1);
         var saturation = Math.Round(color.GetSaturation() * 100, 1);
         var lightness  = Math.Round(color.GetBrightness() * 100, 1);
         return $"{hue}, {saturation}%, {lightness}%";
      }

      [SuppressMessage("ReSharper", "InconsistentNaming")]
      private static string ColorAsRGB(Color color) => $"{color.R}, {color.G}, {color.B}";
   }
}
