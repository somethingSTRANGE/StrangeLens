// -------------------------------------------------------------------------------------
// <copyright file="Lens.ColorHexConverter.cs">
//   Copyright (c) 2026
//   Licensed under the MIT License. See LICENSE file in the project root.
// </copyright>
// -------------------------------------------------------------------------------------

namespace StrangeLens
{
   using System;
   using System.Drawing;
   using System.Text.Json;
   using System.Text.Json.Serialization;

   public partial class Lens
   {
      private sealed class ColorHexConverter : JsonConverter<Color>
      {
         public override Color Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
         {
            return ColorTranslator.FromHtml(reader.GetString() ?? "#000000");
         }

         public override void Write(Utf8JsonWriter writer, Color value, JsonSerializerOptions options)
         {
            writer.WriteStringValue($"#{value.R:X2}{value.G:X2}{value.B:X2}");
         }
      }
   }
}
