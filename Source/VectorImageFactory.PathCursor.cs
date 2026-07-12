// -------------------------------------------------------------------------------------
// <copyright file="VectorImageFactory.PathCursor.cs">
//   Copyright (c) 2026
//   Licensed under the MIT License. See LICENSE file in the project root.
// </copyright>
// -------------------------------------------------------------------------------------

namespace StrangeLens
{
   internal static partial class VectorImageFactory
   {
      private struct PathCursor
      {
         public char PrevCmd;

         public float Cx, Cy;

         public float Mx, My;

         public float PrevCp2X, PrevCp2Y;

         public float PrevQuadCpX, PrevQuadCpY;

         public PathCursor(float startX, float startY)
         {
            this.Cx = this.Mx = startX;
            this.Cy = this.My = startY;
         }
      }
   }
}
