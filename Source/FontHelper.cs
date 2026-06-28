// -------------------------------------------------------------------------------------
// <copyright file="FontHelper.cs">
//   Copyright (c) 2026
//   Licensed under the MIT License. See LICENSE file in the project root.
// </copyright>
// -------------------------------------------------------------------------------------

namespace Lens
{
   using System;
   using System.Drawing;
   using System.Drawing.Text;
   using System.IO;
   using System.Reflection;
   using System.Runtime.InteropServices;

   internal static class FontHelper
   {
      private static readonly PrivateFontCollection fonts = LoadFonts();

      public static FontInfo CreateAttributionFontInfo()
      {
         return new FontInfo(new Font(GetFamily("Inter"), 11f, FontStyle.Regular, GraphicsUnit.Pixel));
      }

      public static FontInfo CreateHeaderFontInfo()
      {
         return new FontInfo(new Font(GetFamily("Inter"), 12f, FontStyle.Bold, GraphicsUnit.Pixel));
      }

      internal static FontInfo CreateLabelFontInfo()
      {
         return new FontInfo(new Font(GetFamily("Inter"), 12f, FontStyle.Regular, GraphicsUnit.Pixel));
      }

      internal static FontInfo CreateValueFontInfo()
      {
         return new FontInfo(
            new Font(GetFamily("JetBrains Mono"), 13f, FontStyle.Regular, GraphicsUnit.Pixel));
      }

      /// <summary>PrivateFontCollection requires this memory to stay valid for as long as any Font
      ///    created from it is in use. Both the collection and this allocation are intentionally
      ///    never freed — they live for the process's lifetime, and the OS reclaims them on exit.</summary>
      private static void AddEmbeddedFont(PrivateFontCollection collection, string fileName)
      {
         var resourceName = $"Lens.Resources.Fonts.{fileName}";
         using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName)
                            ?? throw new FileNotFoundException(
                               $"Embedded font resource not found: {resourceName}");

         var bytes = new byte[stream.Length];
         stream.ReadExactly(bytes);

         var ptr = Marshal.AllocHGlobal(bytes.Length);
         Marshal.Copy(bytes, 0, ptr, bytes.Length);
         collection.AddMemoryFont(ptr, bytes.Length);
      }

      private static FontFamily GetFamily(string name)
      {
         foreach (var family in fonts.Families)
         {
            if (string.Equals(family.Name, name, StringComparison.OrdinalIgnoreCase))
            {
               return family;
            }
         }

         throw new InvalidOperationException($"Embedded font family not found: {name}");
      }

      private static PrivateFontCollection LoadFonts()
      {
         var collection = new PrivateFontCollection();
         AddEmbeddedFont(collection, "Inter-Regular.ttf");
         AddEmbeddedFont(collection, "Inter-Bold.ttf");
         AddEmbeddedFont(collection, "JetBrainsMono-Regular.ttf");
         return collection;
      }
   }
}
