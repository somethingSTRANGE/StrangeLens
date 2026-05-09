using System;
using System.Diagnostics;
using System.Drawing;

namespace Lens
{
   internal static class FontHelper
   {
      internal static Font CreateLabelFont() => Create(new[]
      {
         ("Inter",      12f),
         ("Roboto",     13f),
         ("Arial Nova", 13f),
         ("Segoe UI",   13f),
      }, FontFamily.GenericSansSerif, 13f);

      internal static Font CreateValueFont() => Create(new[]
      {
         ("JetBrains Mono", 13f),
         ("Fira Code",      13f),
         ("Noto Mono",      13f),
         ("Consolas",       14f),
         ("Lucida Console", 13f),
      }, FontFamily.GenericMonospace, 13f);

      internal static Font Create(
         (string Name, float Size)[] candidates,
         FontFamily fallback,
         float fallbackSize)
      {
         foreach (var (name, size) in candidates)
         {
            var font = new Font(name, size, FontStyle.Regular, GraphicsUnit.Pixel);
            if (string.Equals(font.Name, name, StringComparison.OrdinalIgnoreCase))
            {
               LogFontMetrics(font);
               return font;
            }

            font.Dispose();
         }

         var fallbackFont = new Font(fallback, fallbackSize, FontStyle.Regular, GraphicsUnit.Pixel);
         LogFontMetrics(fallbackFont);
         return fallbackFont;
      }

      [Conditional("DEBUG")]
      private static void LogFontMetrics(Font font)
      {
         var family  = font.FontFamily;
         var ascent  = font.Size * family.GetCellAscent(font.Style)  / family.GetEmHeight(font.Style);
         var descent = font.Size * family.GetCellDescent(font.Style) / family.GetEmHeight(font.Style);

         Debug.WriteLine(
            $"{font.Name} {font.Size}px  ascent:{ascent:F4} ({Math.Round(ascent)}) descent:{descent:F4} ({Math.Round(descent)})");
      }
   }
}
