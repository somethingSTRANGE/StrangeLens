// -------------------------------------------------------------------------------------
// <copyright file="VectorImage.cs">
//   Copyright (c) 2026
//   Licensed under the MIT License. See LICENSE file in the project root.
// </copyright>
// -------------------------------------------------------------------------------------

namespace StrangeLens
{
   using System.Drawing;
   using System.Drawing.Drawing2D;

   /// <summary>Immutable result of building an SVG path at a specific pixel size. Produced and
   ///    cached by <see cref="VectorImageFactory"/>; callers must not dispose it.</summary>
   internal sealed class VectorImage
   {
      internal VectorImage(GraphicsPath path1, GraphicsPath? path2, int width, int height)
      {
         this.Path1 = path1;
         this.Path2 = path2;
         this.Width = width;
         this.Height = height;
      }

      public int Height { get; }

      public int Width { get; }

      private GraphicsPath Path1 { get; }

      private GraphicsPath? Path2 { get; }

      internal void Draw(Graphics g, Color color, float x, float y)
      {
         var savedSmoothing = g.SmoothingMode;
         g.SmoothingMode = SmoothingMode.HighQuality;

         var state = g.Save();
         g.TranslateTransform(x, y);

         using var b1 = new SolidBrush(color);
         g.FillPath(b1, this.Path1);

         if (this.Path2 != null)
         {
            using var b2 = new SolidBrush(Color.FromArgb((int)(255 * 0.4f), color));
            g.FillPath(b2, this.Path2);
         }

         g.Restore(state);
         g.SmoothingMode = savedSmoothing;
      }
   }
}
