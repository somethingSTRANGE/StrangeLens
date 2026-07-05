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
   using System.Diagnostics.CodeAnalysis;
   using System.Drawing;
   using System.Drawing.Text;
   using System.IO;
   using System.Reflection;
   using System.Runtime.InteropServices;

   internal static class FontHelper
   {
      // fontFamilies is a snapshot enumerated while a bitmap-backed GDI+ context is live —
      // GDI+ only makes AddMemoryFont additions visible to Families while a context is active.
      [SuppressMessage(
         "ReSharper",
         "PrivateFieldCanBeConvertedToLocalVariable",
         Justification =
            "PrivateFontCollection must outlive every Font created from it (AddMemoryFont docs).")]
      private static readonly PrivateFontCollection fontCollection;

      private static readonly IReadOnlyDictionary<string, FontFamily> fontFamilies;

      static FontHelper()
      {
         fontCollection = new PrivateFontCollection();

         // Hold a bitmap-backed GDI+ context open for the entire loading sequence.
         // Graphics.FromHwnd(IntPtr.Zero) can return a degenerate DC during early startup;
         // a 1×1 Bitmap gives a stable, always-valid context. GDI+ only makes AddMemoryFont
         // additions visible to Families enumeration while a context is active, so the
         // context must be alive from before the first addition through the final enumeration.
         using var bmp = new Bitmap(1, 1);
         using var g = Graphics.FromImage(bmp);

         AddEmbeddedFont(fontCollection, "Inter-Regular.ttf");
         AddEmbeddedFont(fontCollection, "Inter-Bold.ttf");
         AddEmbeddedFont(fontCollection, "JetBrainsMono-Regular.ttf");

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
