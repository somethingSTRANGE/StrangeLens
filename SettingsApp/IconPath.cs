// -------------------------------------------------------------------------------------
// <copyright file="IconPath.cs">
//   Copyright (c) 2026
//   Licensed under the MIT License. See LICENSE file in the project root.
// </copyright>
// -------------------------------------------------------------------------------------

namespace StrangeLens.SettingsApp;

using System;
using System.Globalization;

using Windows.Foundation;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;

/// <summary>Renders <see cref="VectorData"/> (the same viewBox/path data
///    <c>VectorImageFactory</c> uses for GDI+ in the WinForms app) as a WinUI 3 element, via
///    the XAML Path mini-language -- no standalone .svg file involved.</summary>
internal static class IconPath
{
   /// <summary>Renders at <paramref name="width"/>, with height scaled proportionally to the
   ///    source viewBox's own aspect ratio. For a square viewBox (all the icon data today) this
   ///    produces a square result, the same as passing width as a fixed size. It also handles
   ///    non-square viewBoxes correctly (e.g., the 800x100 wordmark), where forcing equal
   ///    width/height would squash the image.</summary>
   internal static FrameworkElement Create(
      VectorData data,
      double width,
      string primaryFill,
      string? secondaryFill = null)
   {
      var (viewBoxWidth, viewBoxHeight) = ParseViewBox(data.ViewBox);
      var height = viewBoxHeight * (width / viewBoxWidth);

      // Clip to the viewBox explicitly. Real SVG viewers always clip content to the viewBox;
      // this Grid doesn't by default, and font/glyph path data (e.g., the wordmark) commonly
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

   /// <summary>Renders the same vector data twice -- a main layer plus an offset "shadow" copy
   ///    beneath it in a darker/translucent color -- for a simple hard-edged pseudo-3D effect
   ///    (not a blurred drop shadow). <paramref name="width"/> is the exact total footprint the
   ///    result must fit within, e.g., to exactly match a fixed-width layout cell; each layer
   ///    renders at <c>width - offsetX</c> so the shadow's offset copy never needs more
   ///    horizontal room than the main layer already occupies. An explicit Grid.Clip guarantees
   ///    the result never exceeds the requested width/height, regardless of how margin-based
   ///    offsets would otherwise propagate through ancestor layout.</summary>
   internal static FrameworkElement CreateWithOffsetShadow(
      VectorData data,
      double width,
      string primaryFill,
      string shadowFill,
      double offsetX = 2,
      double offsetY = 6,
      double shadowOpacity = 0.5,
      string? secondaryFill = null)
   {
      var absOffsetX = Math.Abs(offsetX);
      var absOffsetY = Math.Abs(offsetY);
      var layerWidth = width - absOffsetX;
      var (viewBoxWidth, viewBoxHeight) = ParseViewBox(data.ViewBox);
      var layerHeight = viewBoxHeight * (layerWidth / viewBoxWidth);
      var totalHeight = layerHeight + absOffsetY;

      // Whichever layer sits further in a given direction is the one pinned at 0; the other
      // absorbs the offset's magnitude. A negative offset (shadow above/left of the main
      // layer) would otherwise need a negative margin on the shadow, which the container's
      // Grid.Clip (starting at 0,0) would cut off instead of showing.
      var shadowMarginX = offsetX >= 0 ? offsetX : 0;
      var mainMarginX = offsetX >= 0 ? 0 : -offsetX;
      var shadowMarginY = offsetY >= 0 ? offsetY : 0;
      var mainMarginY = offsetY >= 0 ? 0 : -offsetY;

      var shadow = Create(data, layerWidth, shadowFill, shadowFill);
      shadow.Opacity = shadowOpacity;
      shadow.Margin = new Thickness(shadowMarginX, shadowMarginY, 0, 0);
      shadow.HorizontalAlignment = HorizontalAlignment.Left;
      shadow.VerticalAlignment = VerticalAlignment.Top;

      var main = Create(data, layerWidth, primaryFill, secondaryFill);
      main.Margin = new Thickness(mainMarginX, mainMarginY, 0, 0);
      main.HorizontalAlignment = HorizontalAlignment.Left;
      main.VerticalAlignment = VerticalAlignment.Top;

      var container = new Grid
         {
            Width = width,
            Height = totalHeight,
            Clip = new RectangleGeometry
               {
                  Rect = new Rect(0, 0, width, totalHeight),
               },
         };
      container.Children.Add(shadow);
      container.Children.Add(main);

      return container;
   }

   private static (double Width, double Height) ParseViewBox(string viewBox)
   {
      var parts = viewBox.Split(' ', StringSplitOptions.RemoveEmptyEntries);
      return (double.Parse(parts[2], CultureInfo.InvariantCulture),
         double.Parse(parts[3], CultureInfo.InvariantCulture));
   }
}
