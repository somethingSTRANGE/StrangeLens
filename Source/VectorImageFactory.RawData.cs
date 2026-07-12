// -------------------------------------------------------------------------------------
// <copyright file="VectorImageFactory.RawData.cs">
//   Copyright (c) 2026
//   Licensed under the MIT License. See LICENSE file in the project root.
// </copyright>
// -------------------------------------------------------------------------------------

namespace StrangeLens
{
   internal static partial class VectorImageFactory
   {
      /// <summary>Exposes the raw viewBox/path data (no GDI+ involved) for SettingsApp, which
      ///    renders icons via WinUI 3's own <c>Path</c>/<c>Geometry</c> instead.</summary>
      internal static VectorData AboutDonateBuyMeACoffeeData => Data.AboutDonateBuyMeACoffeeIcon;

      internal static VectorData AboutDonateGitHubData => Data.AboutDonateGitHubSponsorsIcon;

      internal static VectorData AboutDonateKoFiData => Data.AboutDonateKoFiIcon;

      internal static VectorData AboutDonatePayPalData => Data.AboutDonatePayPalIcon;

      internal static VectorData AboutLogoData => Data.AboutLogoImage;

      internal static VectorData AboutResourceIssuesData => Data.AboutResourceIssuesIcon;

      internal static VectorData AboutResourceSourceData => Data.AboutResourceSourceIcon;
   }
}
