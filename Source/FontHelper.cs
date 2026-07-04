// -------------------------------------------------------------------------------------
// <copyright file="FontHelper.cs">
//   Copyright (c) 2026
//   Licensed under the MIT License. See LICENSE file in the project root.
// </copyright>
// -------------------------------------------------------------------------------------

namespace StrangeLens
{
   using System;
   using System.Collections.Generic;
   using System.Drawing;
   using System.Drawing.Text;
   using System.IO;
   using System.Reflection;
   using System.Runtime.InteropServices;

   internal static class FontHelper
   {
      // fontCollection must outlive any Font created from it (AddMemoryFont docs).
      // fontFamilies is a snapshot enumerated while a Graphics context is live —
      // GDI+ may not flush AddMemoryFont additions to its internal registry without one.
      private static readonly PrivateFontCollection fontCollection;

      private static readonly IReadOnlyDictionary<string, FontFamily> fontFamilies;

      static FontHelper()
      {
         fontCollection = new PrivateFontCollection();
         AddEmbeddedFont(fontCollection, "Inter-Regular.ttf");
         AddEmbeddedFont(fontCollection, "Inter-Bold.ttf");
         AddEmbeddedFont(fontCollection, "JetBrainsMono-Regular.ttf");

         // All fonts must be added before the Graphics context is created; GDI+ only
         // flushes AddMemoryFont additions to its enumerable registry when a drawing
         // context exists at query time, not at add time.
         using var g = Graphics.FromHwnd(IntPtr.Zero);
         var map = new Dictionary<string, FontFamily>(StringComparer.OrdinalIgnoreCase);
         foreach (var family in fontCollection.Families)
         {
            map[family.Name] = family;
         }

         fontFamilies = map;
      }

      public static FontInfo CreateBoldFontInfo()
      {
         return new FontInfo(new Font(GetFamily("Inter"), 12f, FontStyle.Bold, GraphicsUnit.Pixel));
      }

      public static FontInfo CreateSmallFontInfo()
      {
         return new FontInfo(new Font(GetFamily("Inter"), 11f, FontStyle.Regular, GraphicsUnit.Pixel));
      }

      internal static FontInfo CreateRegularFontInfo()
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
      ///    never freed -- they live for the process's lifetime, and the OS reclaims them on exit.</summary>
      private static void AddEmbeddedFont(PrivateFontCollection collection, string fileName)
      {
         var resourceName = $"StrangeLens.Resources.Fonts.{fileName}";
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
         if (fontFamilies.TryGetValue(name, out var family))
         {
            return family;
         }

         throw new InvalidOperationException(
            $"Embedded font family not found: {name}. Available: {string.Join(", ", fontFamilies.Keys)}");
      }
   }
}
