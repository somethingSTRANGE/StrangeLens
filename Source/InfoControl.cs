// -------------------------------------------------------------------------------------
// <copyright file="InfoControl.cs" company="Strange Entertainment LLC">
//   Copyright 2004-2023 Strange Entertainment LLC. All rights reserved.
// </copyright>
// -------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
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
      private static readonly Dictionary<int, string> knownColors = GetColorNames();

      // Last-computed values — updated each frame by UpdateInfo.
      public string ValueColorHex   { get; private set; } = string.Empty;
      public string ValueColor12Bit { get; private set; } = string.Empty;
      public string ValueColorWeb   { get; private set; } = string.Empty;
      public string ValueColorRGB   { get; private set; } = string.Empty;
      public string ValueColorHSL   { get; private set; } = string.Empty;
      public string ValueColorName  { get; private set; } = string.Empty;
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
         this.ValueColorName = ColorAsName(color);
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

      private static string ColorAsName(Color color)
      {
         return knownColors.TryGetValue(color.ToArgb(), out var name) ? name : string.Empty;
      }

      [SuppressMessage("ReSharper", "InconsistentNaming")]
      private static string ColorAsRGB(Color color) => $"{color.R}, {color.G}, {color.B}";

      private static Dictionary<int, string> GetColorNames()
      {
         return new Dictionary<int, string>
            {
               { Color.FromArgb(255, 0xF0, 0xF8, 0xFF).ToArgb(), "AliceBlue" },
               { Color.FromArgb(255, 0xFA, 0xEB, 0xD7).ToArgb(), "AntiqueWhite" },
               { Color.FromArgb(255, 0x7F, 0xFF, 0xD4).ToArgb(), "Aquamarine" },
               { Color.FromArgb(255, 0x00, 0x80, 0xFF).ToArgb(), "Azure" },
               { Color.FromArgb(255, 0xF0, 0xFF, 0xFF).ToArgb(), "Azure (CSS)" },
               { Color.FromArgb(255, 0xF5, 0xF5, 0xDC).ToArgb(), "Beige" },
               { Color.FromArgb(255, 0xFF, 0xE4, 0xC4).ToArgb(), "Bisque" },
               { Color.FromArgb(255, 0x00, 0x00, 0x00).ToArgb(), "Black" },
               { Color.FromArgb(255, 0xFF, 0xEB, 0xCD).ToArgb(), "BlanchedAlmond" },
               { Color.FromArgb(255, 0x00, 0x00, 0xFF).ToArgb(), "Blue" },
               { Color.FromArgb(255, 0x8A, 0x2B, 0xE2).ToArgb(), "BlueViolet" },
               { Color.FromArgb(255, 0x99, 0x4C, 0x00).ToArgb(), "Brown" },
               { Color.FromArgb(255, 0xA5, 0x2A, 0x2A).ToArgb(), "Brown (CSS)" },
               { Color.FromArgb(255, 0xDE, 0xB8, 0x87).ToArgb(), "Burlywood" },
               { Color.FromArgb(255, 0x5F, 0x9E, 0xA0).ToArgb(), "CadetBlue" },
               { Color.FromArgb(255, 0x80, 0xFF, 0x00).ToArgb(), "Chartreuse" },
               { Color.FromArgb(255, 0xD2, 0x69, 0x1E).ToArgb(), "Chocolate" },
               { Color.FromArgb(255, 0xFF, 0x7F, 0x50).ToArgb(), "Coral" },
               { Color.FromArgb(255, 0x64, 0x95, 0xED).ToArgb(), "CornflowerBlue" },
               { Color.FromArgb(255, 0xFF, 0xF8, 0xDC).ToArgb(), "CornSilk" },
               { Color.FromArgb(255, 0xDC, 0x14, 0x3C).ToArgb(), "Crimson" },
               { Color.FromArgb(255, 0x00, 0xFF, 0xFF).ToArgb(), "Cyan" },
               { Color.FromArgb(255, 0x00, 0x00, 0x8B).ToArgb(), "DarkBlue" },
               { Color.FromArgb(255, 0x00, 0x8B, 0x8B).ToArgb(), "DarkCyan" },
               { Color.FromArgb(255, 0xB8, 0x86, 0x0B).ToArgb(), "DarkGoldenrod" },
               { Color.FromArgb(255, 0xA9, 0xA9, 0xA9).ToArgb(), "DarkGray" },
               { Color.FromArgb(255, 0x00, 0x64, 0x00).ToArgb(), "DarkGreen" },
               { Color.FromArgb(255, 0xBD, 0xB7, 0x6B).ToArgb(), "DarkKhaki" },
               { Color.FromArgb(255, 0x8B, 0x00, 0x8B).ToArgb(), "DarkMagenta" },
               { Color.FromArgb(255, 0x55, 0x6B, 0x2F).ToArgb(), "DarkOliveGreen" },
               { Color.FromArgb(255, 0xFF, 0x8C, 0x00).ToArgb(), "DarkOrange" },
               { Color.FromArgb(255, 0x99, 0x32, 0xCC).ToArgb(), "DarkOrchid" },
               { Color.FromArgb(255, 0x8B, 0x00, 0x00).ToArgb(), "DarkRed" },
               { Color.FromArgb(255, 0xE9, 0x96, 0x7A).ToArgb(), "DarkSalmon" },
               { Color.FromArgb(255, 0x8F, 0xBC, 0x8F).ToArgb(), "DarkSeaGreen" },
               { Color.FromArgb(255, 0x48, 0x3D, 0x8B).ToArgb(), "DarkSlateBlue" },
               { Color.FromArgb(255, 0x2F, 0x4F, 0x4F).ToArgb(), "DarkSlateGray" },
               { Color.FromArgb(255, 0x00, 0xCE, 0xD1).ToArgb(), "DarkTurquoise" },
               { Color.FromArgb(255, 0x94, 0x00, 0xD3).ToArgb(), "DarkViolet" },
               { Color.FromArgb(255, 0xFF, 0x14, 0x93).ToArgb(), "DeepPink" },
               { Color.FromArgb(255, 0x00, 0xBF, 0xFF).ToArgb(), "DeepSkyBlue" },
               { Color.FromArgb(255, 0x69, 0x69, 0x69).ToArgb(), "DimGray" },
               { Color.FromArgb(255, 0x1E, 0x90, 0xFF).ToArgb(), "DodgerBlue" },
               { Color.FromArgb(255, 0xB2, 0x22, 0x22).ToArgb(), "FireBrick" },
               { Color.FromArgb(255, 0xFF, 0xFA, 0xF0).ToArgb(), "FloralWhite" },
               { Color.FromArgb(255, 0x22, 0x8B, 0x22).ToArgb(), "ForestGreen" },
               { Color.FromArgb(255, 0xDC, 0xDC, 0xDC).ToArgb(), "Gainsboro" },
               { Color.FromArgb(255, 0xF8, 0xF8, 0xFF).ToArgb(), "GhostWhite" },
               { Color.FromArgb(255, 0xFF, 0xD7, 0x00).ToArgb(), "Gold" },
               { Color.FromArgb(255, 0xDA, 0xA5, 0x20).ToArgb(), "Goldenrod" },
               { Color.FromArgb(255, 0x80, 0x80, 0x80).ToArgb(), "Gray" },
               { Color.FromArgb(255, 0x7F, 0x7F, 0x7F).ToArgb(), "Gray (Unity)" },
               { Color.FromArgb(255, 0x00, 0x80, 0x00).ToArgb(), "Green (CSS)" },
               { Color.FromArgb(255, 0x00, 0xFF, 0x00).ToArgb(), "Green" },
               { Color.FromArgb(255, 0xAD, 0xFF, 0x2F).ToArgb(), "GreenYellow" },
               { Color.FromArgb(255, 0xF0, 0xFF, 0xF0).ToArgb(), "Honeydew" },
               { Color.FromArgb(255, 0xFF, 0x69, 0xB4).ToArgb(), "HotPink" },
               { Color.FromArgb(255, 0xCD, 0x5C, 0x5C).ToArgb(), "IndianRed" },
               { Color.FromArgb(255, 0x4B, 0x00, 0x82).ToArgb(), "Indigo" },
               { Color.FromArgb(255, 0xFF, 0xFF, 0xF0).ToArgb(), "Ivory" },
               { Color.FromArgb(255, 0xF0, 0xE6, 0x8C).ToArgb(), "Khaki" },
               { Color.FromArgb(255, 0xE6, 0xE6, 0xFA).ToArgb(), "Lavender" },
               { Color.FromArgb(255, 0xFF, 0xF0, 0xF5).ToArgb(), "LavenderBlush" },
               { Color.FromArgb(255, 0x7C, 0xFC, 0x00).ToArgb(), "LawnGreen" },
               { Color.FromArgb(255, 0xFF, 0xFA, 0xCD).ToArgb(), "LemonChiffon" },
               { Color.FromArgb(255, 0xAD, 0xD8, 0xE6).ToArgb(), "LightBlue" },
               { Color.FromArgb(255, 0xF0, 0x80, 0x80).ToArgb(), "LightCoral" },
               { Color.FromArgb(255, 0xE0, 0xFF, 0xFF).ToArgb(), "LightCyan" },
               { Color.FromArgb(255, 0xFA, 0xFA, 0xD2).ToArgb(), "LightGoldenrodYellow" },
               { Color.FromArgb(255, 0xD3, 0xD3, 0xD3).ToArgb(), "LightGray" },
               { Color.FromArgb(255, 0x90, 0xEE, 0x90).ToArgb(), "LightGreen" },
               { Color.FromArgb(255, 0xFF, 0xB6, 0xC1).ToArgb(), "LightPink" },
               { Color.FromArgb(255, 0xFF, 0xA0, 0x7A).ToArgb(), "LightSalmon" },
               { Color.FromArgb(255, 0x20, 0xB2, 0xAA).ToArgb(), "LightSeaGreen" },
               { Color.FromArgb(255, 0x87, 0xCE, 0xFA).ToArgb(), "LightSkyBlue" },
               { Color.FromArgb(255, 0x77, 0x88, 0x99).ToArgb(), "LightSlateGray" },
               { Color.FromArgb(255, 0xB0, 0xC4, 0xDE).ToArgb(), "LightSteelBlue" },
               { Color.FromArgb(255, 0xFF, 0xFF, 0xE0).ToArgb(), "LightYellow" },
               { Color.FromArgb(255, 0x32, 0xCD, 0x32).ToArgb(), "LimeGreen" },
               { Color.FromArgb(255, 0xFA, 0xF0, 0xE6).ToArgb(), "Linen" },
               { Color.FromArgb(255, 0xFF, 0x00, 0xFF).ToArgb(), "Magenta" },
               { Color.FromArgb(255, 0x80, 0x00, 0x00).ToArgb(), "Maroon" },
               { Color.FromArgb(255, 0x66, 0xCD, 0xAA).ToArgb(), "MediumAquamarine" },
               { Color.FromArgb(255, 0x00, 0x00, 0xCD).ToArgb(), "MediumBlue" },
               { Color.FromArgb(255, 0xBA, 0x55, 0xD3).ToArgb(), "MediumOrchid" },
               { Color.FromArgb(255, 0x93, 0x70, 0xDB).ToArgb(), "MediumPurple" },
               { Color.FromArgb(255, 0x3C, 0xB3, 0x71).ToArgb(), "MediumSeaGreen" },
               { Color.FromArgb(255, 0x7B, 0x68, 0xEE).ToArgb(), "MediumSlateBlue" },
               { Color.FromArgb(255, 0x00, 0xFA, 0x9A).ToArgb(), "MediumSpringGreen" },
               { Color.FromArgb(255, 0x48, 0xD1, 0xCC).ToArgb(), "MediumTurquoise" },
               { Color.FromArgb(255, 0xC7, 0x15, 0x85).ToArgb(), "MediumVioletRed" },
               { Color.FromArgb(255, 0x19, 0x19, 0x70).ToArgb(), "MidnightBlue" },
               { Color.FromArgb(255, 0xF5, 0xFF, 0xFA).ToArgb(), "MintCream" },
               { Color.FromArgb(255, 0xFF, 0xE4, 0xE1).ToArgb(), "MistyRose" },
               { Color.FromArgb(255, 0xFF, 0xE4, 0xB5).ToArgb(), "Moccasin" },
               { Color.FromArgb(255, 0xFF, 0xDE, 0xAD).ToArgb(), "NavajoWhite" },
               { Color.FromArgb(255, 0x00, 0x00, 0x80).ToArgb(), "Navy" },
               { Color.FromArgb(255, 0xFD, 0xF5, 0xE6).ToArgb(), "OldLace" },
               { Color.FromArgb(255, 0x80, 0x80, 0x00).ToArgb(), "Olive" },
               { Color.FromArgb(255, 0x6B, 0x8E, 0x23).ToArgb(), "OliveDrab" },
               { Color.FromArgb(255, 0xFF, 0x80, 0x00).ToArgb(), "Orange" },
               { Color.FromArgb(255, 0xFF, 0xA5, 0x00).ToArgb(), "Orange (CSS)" },
               { Color.FromArgb(255, 0xFF, 0x45, 0x00).ToArgb(), "OrangeRed" },
               { Color.FromArgb(255, 0xDA, 0x70, 0xD6).ToArgb(), "Orchid" },
               { Color.FromArgb(255, 0xEE, 0xE8, 0xAA).ToArgb(), "PaleGoldenrod" },
               { Color.FromArgb(255, 0x98, 0xFB, 0x98).ToArgb(), "PaleGreen" },
               { Color.FromArgb(255, 0xAF, 0xEE, 0xEE).ToArgb(), "PaleTurquoise" },
               { Color.FromArgb(255, 0xDB, 0x70, 0x93).ToArgb(), "PaleVioletRed" },
               { Color.FromArgb(255, 0xFF, 0xEF, 0xD5).ToArgb(), "PapayaWhip" },
               { Color.FromArgb(255, 0xFF, 0xDA, 0xB9).ToArgb(), "PeachPuff" },
               { Color.FromArgb(255, 0xCD, 0x85, 0x3F).ToArgb(), "Peru" },
               { Color.FromArgb(255, 0xFF, 0xBF, 0xCC).ToArgb(), "Pink" },
               { Color.FromArgb(255, 0xFF, 0xC0, 0xCB).ToArgb(), "Pink (CSS)" },
               { Color.FromArgb(255, 0xDD, 0xA0, 0xDD).ToArgb(), "Plum" },
               { Color.FromArgb(255, 0xB0, 0xE0, 0xE6).ToArgb(), "PowderBlue" },
               { Color.FromArgb(255, 0x80, 0x00, 0xFF).ToArgb(), "Purple" },
               { Color.FromArgb(255, 0x80, 0x00, 0x80).ToArgb(), "Purple (CSS)" },
               { Color.FromArgb(255, 0x66, 0x33, 0x99).ToArgb(), "RebeccaPurple" },
               { Color.FromArgb(255, 0xFF, 0x00, 0x00).ToArgb(), "Red" },
               { Color.FromArgb(255, 0xFF, 0x00, 0x80).ToArgb(), "Rose" },
               { Color.FromArgb(255, 0xBC, 0x8F, 0x8F).ToArgb(), "RosyBrown" },
               { Color.FromArgb(255, 0x41, 0x69, 0xE1).ToArgb(), "RoyalBlue" },
               { Color.FromArgb(255, 0x8B, 0x45, 0x13).ToArgb(), "SaddleBrown" },
               { Color.FromArgb(255, 0xFA, 0x80, 0x72).ToArgb(), "Salmon" },
               { Color.FromArgb(255, 0xF4, 0xA4, 0x60).ToArgb(), "SandyBrown" },
               { Color.FromArgb(255, 0x2E, 0x8B, 0x57).ToArgb(), "SeaGreen" },
               { Color.FromArgb(255, 0xFF, 0xF5, 0xEE).ToArgb(), "Seashell" },
               { Color.FromArgb(255, 0xA0, 0x52, 0x2D).ToArgb(), "Sienna" },
               { Color.FromArgb(255, 0xC0, 0xC0, 0xC0).ToArgb(), "Silver" },
               { Color.FromArgb(255, 0x87, 0xCE, 0xEB).ToArgb(), "SkyBlue" },
               { Color.FromArgb(255, 0x6A, 0x5A, 0xCD).ToArgb(), "SlateBlue" },
               { Color.FromArgb(255, 0x70, 0x80, 0x90).ToArgb(), "SlateGray" },
               { Color.FromArgb(255, 0xFF, 0xFA, 0xFA).ToArgb(), "Snow" },
               { Color.FromArgb(255, 0x00, 0xFF, 0x80).ToArgb(), "SpringGreen" },
               { Color.FromArgb(255, 0x46, 0x82, 0xB4).ToArgb(), "SteelBlue" },
               { Color.FromArgb(255, 0xD2, 0xB4, 0x8C).ToArgb(), "Tan" },
               { Color.FromArgb(255, 0x00, 0x80, 0x80).ToArgb(), "Teal" },
               { Color.FromArgb(255, 0xD8, 0xBF, 0xD8).ToArgb(), "Thistle" },
               { Color.FromArgb(255, 0xFF, 0x63, 0x47).ToArgb(), "Tomato" },
               { Color.FromArgb(255, 0x40, 0xE0, 0xD0).ToArgb(), "Turquoise" },
               { Color.FromArgb(255, 0xEE, 0x82, 0xEE).ToArgb(), "Violet" },
               { Color.FromArgb(255, 0xF5, 0xDE, 0xB3).ToArgb(), "Wheat" },
               { Color.FromArgb(255, 0xFF, 0xFF, 0xFF).ToArgb(), "White" },
               { Color.FromArgb(255, 0xF5, 0xF5, 0xF5).ToArgb(), "WhiteSmoke" },
               { Color.FromArgb(255, 0xFF, 0xFF, 0x00).ToArgb(), "Yellow" },
               { Color.FromArgb(255, 0xFF, 0xEB, 0x04).ToArgb(), "Yellow (Unity)" },
               { Color.FromArgb(255, 0x9A, 0xCD, 0x32).ToArgb(), "YellowGreen" }
            };
      }
   }
}
