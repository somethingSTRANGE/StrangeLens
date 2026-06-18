using System.Drawing.Drawing2D;

namespace Lens
{
   /// <summary>
   ///    Immutable result of building an SVG path at a specific pixel size.
   ///    Produced and cached by <see cref="SvgImageFactory" />; callers must not dispose it.
   /// </summary>
   internal sealed class SvgImage
   {
      internal SvgImage(GraphicsPath path1, GraphicsPath? path2, int width, int height)
      {
         this.Path1   = path1;
         this.Path2   = path2;
         this.Width   = width;
         this.Height  = height;
      }

      public GraphicsPath  Path1  { get; }
      public GraphicsPath? Path2  { get; }
      public int           Width  { get; }
      public int           Height { get; }
   }
}