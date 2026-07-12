// -------------------------------------------------------------------------------------
// <copyright file="VectorImageFactory.Data.Info.cs">
//   Copyright (c) 2026
//   Licensed under the MIT License. See LICENSE file in the project root.
// </copyright>
// -------------------------------------------------------------------------------------

namespace StrangeLens;

internal static partial class VectorImageFactory
{
   /// <summary>Icons used only by <c>InfoForm</c>/<c>InfoControl</c> -- WinForms/GDI+-only,
   ///    never needed by SettingsApp.</summary>
   private static partial class Data
   {
      internal static readonly VectorData InfoColorPaletteIcon = new(
         "0 0 640 640",
            [
               "M576 320v3c0 36-34 61-70 61h-98a48 48 0 0 0-47 58q4 15 11 30 11 20 12 42c0 32-22 6"
               + "1-53 62h-11a256 256 0 1 1 256-256m-384 32a32 32 0 1 0-64 0 32 32 0 0 0 64 0m0-96"
               + "a32 32 0 1 0 0-64 32 32 0 0 0 0 64m160-96a32 32 0 1 0-64 0 32 32 0 0 0 64 0m96 9"
               + "6a32 32 0 1 0 0-64 32 32 0 0 0 0 64",
            ]);

      internal static readonly VectorData InfoColorValuesIcon = new(
         "0 0 640 640",
            [
               "M112 64c-26 0-48 22-48 48v368a96 96 0 0 0 192 0V112c0-26-21-48-48-48zm32 64h32q15 "
               + "1 16 16v32q-1 15-16 16h-32q-15-1-16-16v-32q1-15 16-16m-16 144q1-15 16-16h32q15 1"
               + " 16 16v32q-1 15-16 16h-32q-15-1-16-16zm32 184a24 24 0 1 1 0 48 24 24 0 1 1 0-48",
            ],
            [
               "M160 576a95 95 0 0 0 68-28q27-26 28-68V250l96-96c19-19 49-19 68 0l68 68c19 19 19 4"
               + "9 0 68L230 548a96 96 0 0 1-70 28m109 0 192-192h67c27 0 48 22 48 48v96c0 27-21 48"
               + "-48 48z",
            ]);

      internal static readonly VectorData InfoLensSizeIcon = new(
         "0 0 640 640",
            [
               "M128 96c-18 0-32 14-32 32v96a32 32 0 1 0 64 0v-64h64a32 32 0 1 0 0-64zm32 320a32 3"
               + "2 0 1 0-64 0v96c0 18 14 32 32 32h96a32 32 0 1 0 0-64h-64zM416 96a32 32 0 1 0 0 6"
               + "4h64v64a32 32 0 1 0 64 0v-96c0-18-14-32-32-32zm128 320a32 32 0 1 0-64 0v64h-64a3"
               + "2 32 0 1 0 0 64h96c18 0 32-14 32-32z",
            ]);

      internal static readonly VectorData InfoMagnificationIcon = new(
         "0 0 640 640",
            [
               "M480 272c0 45.9-14.9 88.3-40 122.7l126.6 126.7a32 32 0 0 1-45.3 45.3L394.7 440A208"
               + " 208 0 1 1 480 272M272 416a144 144 0 1 0 0-288 144 144 0 0 0 0 288",
            ],
            [
               "M128 272a144 144 0 1 0 288 0 144 144 0 0 0-288 0",
            ]);

      internal static readonly VectorData InfoMouseCursorIcon = new(
         "0 0 640 640",
            [
               "M173 67q13-7 25 2l320 240q13 10 9 27-6 15-23 16H352l89 178a32 32 0 1 1-58 28l-88-1"
               + "77-92 121q-11 13-27 9-15-6-16-23V88q1-14 13-21",
            ]);

      internal static readonly VectorData InfoRulerIcon = new(
         "0 0 640 640",
            [
               "M210 60q30 0 30 30v30h-40c-10 0-20 10-20 20s10 20 20 20h40v40h-80c-10 0-20 10-20 2"
               + "0s10 20 20 20h80v40h-40c-10 0-20 10-20 20s10 20 20 20h40v40h-80c-10 0-20 10-20 2"
               + "0s10 20 20 20h80v80c0 10 10 20 20 20s20-10 20-20v-80h40v40c0 10 10 20 20 20s20-1"
               + "0 20-20v-40h40v80c0 10 10 20 20 20s20-10 20-20v-80h40v40c0 10 10 20 20 20s20-10 "
               + "20-20v-40h30q30 0 30 30v110c0 20-20 40-40 40H100c-20 0-40-20-40-40V100c0-20 20-4"
               + "0 40-40z",
            ],
            [
               "M240 120v40h-40c-10 0-20-10-20-20s10-20 20-20zm0 80v40h-80c-10 0-20-10-20-20s10-20"
               + " 20-20zm0 80v40h-40c-10 0-20-10-20-20s10-20 20-20zm0 80v40h-80c-10 0-20-10-20-20"
               + "s10-20 20-20zm0 40h40v80c0 10-10 20-20 20s-20-10-20-20zm280 0v40c0 10-10 20-20 2"
               + "0s-20-10-20-20v-40zm-80 0v80c0 10-10 20-20 20s-20-10-20-20v-80zm-80 0v40c0 10-10"
               + " 20-20 20s-20-10-20-20v-40z",
            ]);
   }
}
