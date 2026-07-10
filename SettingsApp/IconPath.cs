// -------------------------------------------------------------------------------------
// <copyright file="IconPath.cs">
//   Copyright (c) 2026
//   Licensed under the MIT License. See LICENSE file in the project root.
// </copyright>
// -------------------------------------------------------------------------------------

namespace StrangeLens.SettingsApp;

using System;
using System.Globalization;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;

/// <summary>Renders <see cref="VectorData"/> (the same viewBox/path data
///    <c>VectorImageFactory</c> uses for GDI+ in the WinForms app) as a WinUI 3 element, via
///    the XAML Path mini-language -- no standalone .svg file involved.</summary>
internal static class IconPath
{
   /// <summary>Renders at <paramref name="width"/>, with height scaled proportionally to the
   ///    source viewBox's own aspect ratio. For a square viewBox (all the icon data today) this
   ///    produces a square result, same as passing width as a fixed size -- but it also handles
   ///    non-square viewBoxes correctly (e.g. the 800x100 wordmark), where forcing equal
   ///    width/height would squash the image.</summary>
   internal static UIElement Create(
      VectorData data,
      double width,
      string primaryFill,
      string? secondaryFill = null)
   {
      var (viewBoxWidth, viewBoxHeight) = ParseViewBox(data.ViewBox);
      var height = viewBoxHeight * (width / viewBoxWidth);

      // Clip to the viewBox explicitly. Real SVG viewers always clip content to the viewBox;
      // this Grid doesn't by default, and font/glyph path data (e.g. the wordmark) commonly
      // has coordinates that overshoot the nominal bounds slightly, which then render
      // uncontained -- visible as stray shapes floating outside the icon's own area.
      var markup = $"""
                    <Grid xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" Width="{
                       viewBoxWidth}" Height="{viewBoxHeight}">
                       <Grid.Clip>
                          <RectangleGeometry Rect="0,0,{viewBoxWidth},{viewBoxHeight}" />
                       </Grid.Clip>
                       <Path Fill="{primaryFill}" Data="{string.Concat(data.Primary)}" />
                       {(data.Secondary != null
                          ? $"""<Path Fill="{secondaryFill ?? primaryFill}" Data="{
                             string.Concat(data.Secondary)}" />"""
                          : string.Empty)}
                    </Grid>
                    """;

      var grid = (UIElement)XamlReader.Load(markup);
      return new Viewbox
         {
            Width = width,
            Height = height,
            Child = grid,
         };
   }

   private static (double Width, double Height) ParseViewBox(string viewBox)
   {
      var parts = viewBox.Split(' ', StringSplitOptions.RemoveEmptyEntries);
      return (double.Parse(parts[2], CultureInfo.InvariantCulture),
         double.Parse(parts[3], CultureInfo.InvariantCulture));
   }
}
