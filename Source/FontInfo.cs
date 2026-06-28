// -------------------------------------------------------------------------------------
// <copyright file="FontInfo.cs">
//   Copyright (c) 2026
//   Licensed under the MIT License. See LICENSE file in the project root.
// </copyright>
// -------------------------------------------------------------------------------------

namespace Lens;

using System;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;

/// <summary>Immutable, pre-calculated pixel metrics for a <see cref="Font"/>. Every metric
///    is only meaningful in pixels, so the font must be created with
///    <see cref="GraphicsUnit.Pixel"/>. Owns the wrapped <see cref="Font"/> exclusively —
///    disposing a <see cref="FontInfo"/> disposes its font.</summary>
[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
internal sealed class FontInfo : IDisposable
{
   public FontInfo(Font font)
   {
      ArgumentNullException.ThrowIfNull(font);
      if (font.Unit != GraphicsUnit.Pixel)
      {
         throw new ArgumentException(
            $"FontInfo requires a font created with GraphicsUnit.Pixel, but got {font.Unit}.",
            nameof(font));
      }

      this.Font = font;

      this.Size = font.Size;
      this.Height = font.Height;

      var emHeight = font.FontFamily.GetEmHeight(font.Style);
      var cellAscent = font.FontFamily.GetCellAscent(font.Style);
      var cellDescent = font.FontFamily.GetCellDescent(font.Style);

      this.Ascent = (font.Size * cellAscent) / emHeight;
      this.Descent = (font.Size * cellDescent) / emHeight;
      this.LineHeight = this.Ascent + this.Descent;

      this.PixelAscent = (int)Math.Round(this.Ascent);
      this.PixelDescent = (int)Math.Round(this.Descent);
      this.PixelLineHeight = this.PixelAscent + this.PixelDescent;

      // Per-font/size optical baseline correction. GDI's reported cell ascent doesn't always
      // match where a font visually appears to sit, and the same family can be off by a pixel
      // at one size and fine at another. Tuned empirically per (font, size) pair here, so every
      // Form applies one consistent nudge instead of scattering ad-hoc Y-offsets per call site.
      if (font.Name is "Courier New" or "Microsoft Sans Serif")
      {
         this.BaselineAdjustment = -1;
      }

      if (font.Name is "Segoe UI" or "Microsoft Sans Serif")
      {
         this.BaselineAdjustment = -1;
         this.OverageFavorsTop = false;
      }
   }

   public float Ascent { get; }

   /// <summary>A one-off pixel nudge for this exact font and size, applied when positioning
   ///    text by baseline (Y), to correct optical misalignment that <see cref="PixelAscent"/>
   ///    alone doesn't account for. Determined empirically per (font, size) — 0 unless tuned.</summary>
   public int BaselineAdjustment { get; }

   public float Descent { get; }

   public Font Font { get; }

   public int Height { get; }

   public float LineHeight { get; }

   /// <summary>When centering this font within a taller-than-needed area by splitting the
   ///    leftover height into top/bottom padding one pixel at a time, this says which side gets
   ///    the first (and, on an odd split, the extra) pixel. Determined empirically per (font,
   ///    size) — true (top-first) unless tuned.</summary>
   public bool OverageFavorsTop { get; } = true;

   public int PixelAscent { get; }

   public int PixelDescent { get; }

   public int PixelLineHeight { get; }

   public float Size { get; }

   public void Dispose()
   {
      this.Font.Dispose();
   }
}
