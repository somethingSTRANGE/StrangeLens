using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace Lens
{
   public static class ExtensionMethods
   {
      public static T Clamp<T>(this T value, T min, T max) where T : IComparable<T>
      {
         return value.CompareTo(min) < 0 ? min : value.CompareTo(max) > 0 ? max : value;
      }

      public static void Deconstruct(this Point point, out int X, out int Y)
      {
         X = point.X;
         Y = point.Y;
      }

      public static void Deconstruct(this RectangleF rect, out float top, out float bottom, out float left,
         out float right, out float width, out float height)
      {
         top = rect.Top;
         bottom = rect.Bottom;
         left = rect.Left;
         right = rect.Right;
         width = rect.Width;
         height = rect.Height;
      }

      public static DashStyle DashStyle(this GridStyleOptions gridStyle)
      {
         switch (gridStyle)
         {
            case GridStyleOptions.Solid: return System.Drawing.Drawing2D.DashStyle.Solid;
            case GridStyleOptions.Dash: return System.Drawing.Drawing2D.DashStyle.Dash;
            case GridStyleOptions.Dot: return System.Drawing.Drawing2D.DashStyle.Dot;
            case GridStyleOptions.DashDot:
               return System.Drawing.Drawing2D.DashStyle.DashDot;
            case GridStyleOptions.DashDotDot:
               return System.Drawing.Drawing2D.DashStyle.DashDotDot;
            case GridStyleOptions.None:
            default:
               throw new ArgumentOutOfRangeException(nameof(gridStyle), gridStyle, null);
         }
      }
   }
}
