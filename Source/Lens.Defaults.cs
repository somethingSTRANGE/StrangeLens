// -------------------------------------------------------------------------------------
// <copyright file="Lens.Defaults.cs">
//   Copyright (c) 2026
//   Licensed under the MIT License. See LICENSE file in the project root.
// </copyright>
// -------------------------------------------------------------------------------------

namespace StrangeLens
{
   using System.Diagnostics;

   public partial class Lens
   {
      public static class Defaults
      {
         public const byte MaxGridSize = 16;

         public const short MaxHeight = 400;

         public const byte MaxMagnification = 16;

         public const short MaxWidth = 400;

         public const byte MinGridSize = 1;

         public const short MinHeight = 100;

         public const byte MinMagnification = 2;

         public const short MinWidth = 100;

         public const byte SizeIncrement = 20;

         static Defaults()
         {
            Debug.Assert(SizeIncrement % 2 == 0);
            Debug.Assert(MaxHeight % SizeIncrement == 0);
            Debug.Assert(MinHeight % SizeIncrement == 0);
            Debug.Assert(MaxWidth % SizeIncrement == 0);
            Debug.Assert(MinWidth % SizeIncrement == 0);
         }
      }
   }
}
