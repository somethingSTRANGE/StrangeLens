using System.Drawing.Drawing2D;

namespace Lens
{
   /// <summary>
   ///    Immutable result of building an SVG path at a specific pixel size.
   ///    Produced and cached by <see cref="SvgImageFactory" />; callers must not dispose it.
   /// </summary>
   internal sealed class SvgImage
   {
      internal SvgImage(GraphicsPath path, int width, int height)
      {
         this.Path = path;
         this.Width = width;
         this.Height = height;
      }

      public GraphicsPath Path { get; }
      public int Width { get; }
      public int Height { get; }
   }
}