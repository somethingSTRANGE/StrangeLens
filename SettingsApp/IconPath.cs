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
   internal static UIElement Create(
      VectorData data,
      double size,
      string primaryFill,
      string? secondaryFill = null)
   {
      var (width, height) = ParseViewBox(data.ViewBox);

      var markup = $"""
                    <Grid xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" Width="{width}" Height="{
             height}">
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
            Width = size,
            Height = size,
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
