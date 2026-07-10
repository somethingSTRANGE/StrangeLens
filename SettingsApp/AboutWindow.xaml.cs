// -------------------------------------------------------------------------------------
// <copyright file="AboutWindow.xaml.cs">
//   Copyright (c) 2026
//   Licensed under the MIT License. See LICENSE file in the project root.
// </copyright>
// -------------------------------------------------------------------------------------

namespace StrangeLens.SettingsApp;

using System;
using System.Linq;
using System.Reflection;

using Windows.ApplicationModel.DataTransfer;
using Windows.Graphics;

using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;

public sealed partial class AboutWindow
{
   internal const string WindowTitle = "About Strange Lens";

   private const string SystemFillColorCriticalBackground =
      "{ThemeResource SystemFillColorCriticalBackgroundBrush}";

   private const string TextFillColorPrimary = "{ThemeResource TextFillColorPrimaryBrush}";

   private const int VisibleHeight = 745;

   // Visible width/height the window should render at. AppWindow.Resize takes a "logical"
   // size (matches GetWindowRect) that's larger than the visible bounds by an invisible
   // resize/shadow margin -- empirically ~14px wide / ~7px tall (DwmGetWindowAttribute
   // DWMWA_EXTENDED_FRAME_BOUNDS vs GetWindowRect, tested with a real window). Content
   // naturally needs ~595px, footer ~62px (measured via DesiredSize); title bar adds ~32px.
   private const int VisibleWidth = 410;

   private string versionSummary = string.Empty;

   public AboutWindow()
   {
      this.InitializeComponent();

      this.AppWindow.SetIcon("Icons/AppIcon.ico");
      this.AppWindow.TitleBar.PreferredTheme = TitleBarTheme.UseDefaultAppMode;

      var presenter = OverlappedPresenter.Create();
      presenter.IsAlwaysOnTop = true;
      presenter.IsMaximizable = false;
      presenter.IsMinimizable = false;
      presenter.IsResizable = false;
      presenter.SetBorderAndTitleBar(true, true);
      this.AppWindow.SetPresenter(presenter);
      this.AppWindow.Resize(new SizeInt32(VisibleWidth + 14, VisibleHeight + 7));

      this.Title = WindowTitle;
      this.PopulateContent();
      this.PopulateIcons();
   }

   private void CardsGrid_Loaded(object sender, RoutedEventArgs e)
   {
      // Shared by both Resources/Donate cards -- one receiver registration covers both,
      // since sharing the ThemeShadow resource just shares its blur/softness recipe, not a
      // single physical shadow instance. CardShadowReceiver is a sibling of the two cards,
      // not their ancestor -- registering an ancestor as receiver faults natively.
      this.CardShadow.Receivers.Add(this.CardShadowReceiver);
   }

   private void OnCloseClick(object sender, RoutedEventArgs e)
   {
      this.Close();
   }

   private void OnCopyAndCloseClick(object sender, RoutedEventArgs e)
   {
      var dataPackage = new DataPackage();
      dataPackage.SetText(this.versionSummary);
      Clipboard.SetContent(dataPackage);
      this.Close();
   }

   private void PopulateContent()
   {
      // Reads from the main StrangeLens assembly (via the shared Lens project reference)
      // rather than this exe's own attributes -- this window is about that app, not about
      // itself.
      var assembly = typeof(Lens).Assembly;

      var informationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
         ?.InformationalVersion;
      var plusIdx = informationalVersion?.IndexOf('+') ?? -1;
      var rawHash = plusIdx >= 0 ? informationalVersion![(plusIdx + 1)..] : null;

      var asmBuildDate = assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
         .FirstOrDefault(a => a.Key == "BuildDate")?.Value;

      var buildVersion = plusIdx > 0 ? informationalVersion![..plusIdx] : informationalVersion ?? "0.0.0";
      var buildDate = string.IsNullOrEmpty(asmBuildDate) || (asmBuildDate == "dev")
         ? DateTime.Today.ToString("yyyy-MM-dd")
         : asmBuildDate;
      var commitHash = rawHash?.Length >= 7 ? rawHash[..7] : "HASH";
      var copyright = assembly.GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright ?? string.Empty;
      var productName = assembly.GetCustomAttribute<AssemblyProductAttribute>()?.Product ?? "Strange Lens";

      this.VersionText.Text = $"Version {buildVersion}";
      this.BuildDateText.Text = buildDate; // "Built on {buildDate} from commit {commitHash}";
      this.CopyrightText.Text = $"{copyright} — MIT License";

      this.versionSummary = $"{productName} {buildVersion}\nBuilt on {buildDate} from commit {commitHash}";
   }

   private void PopulateIcons()
   {
      this.WordmarkHost.Children.Add(
         IconPath.Create(VectorImageFactory.AboutLogoData, 200, TextFillColorPrimary));

      this.ResourceSourceIconHost.Children.Add(
         IconPath.Create(VectorImageFactory.AboutResourceSourceData, 20, TextFillColorPrimary));
      this.ResourceIssuesIconHost.Children.Add(
         IconPath.Create(
            VectorImageFactory.AboutResourceIssuesData,
            20,
            TextFillColorPrimary,
            SystemFillColorCriticalBackground));
      this.DonateGitHubIconHost.Children.Add(
         IconPath.Create(VectorImageFactory.AboutDonateGitHubData, 20, TextFillColorPrimary));
      this.DonateBmcIconHost.Children.Add(
         IconPath.Create(VectorImageFactory.AboutDonateBuyMeACoffeeData, 20, TextFillColorPrimary));
      this.DonateKoFiIconHost.Children.Add(
         IconPath.Create(VectorImageFactory.AboutDonateKoFiData, 20, TextFillColorPrimary));
      this.DonatePayPalIconHost.Children.Add(
         IconPath.Create(VectorImageFactory.AboutDonatePayPalData, 20, TextFillColorPrimary));
   }
}
