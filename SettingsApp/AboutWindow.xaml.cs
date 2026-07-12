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
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

public sealed partial class AboutWindow
{
   internal const string WindowTitle = "About Strange Lens";

   private const string ColorTextDisabled = "{ThemeResource TextFillColorDisabledBrush}";

   private const string ColorTextPrimary = "{ThemeResource TextFillColorPrimaryBrush}";

   private const string ColorTextSecondary = "{ThemeResource TextFillColorSecondaryBrush}";

   /// <summary>The window's total visible height, top border to the bottom border, including
   ///    the title bar.</summary>
   private const int VisibleHeight = 745;

   /// <summary>The window's total visible width, left border to the right border.</summary>
   private const int VisibleWidth = 408;

   private double lastScaleFactor;

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

      this.lastScaleFactor = DpiHelper.GetScaleFactor(this);
      this.ApplyWindowSize();

      // IsResizable = false stops the user from resizing, but the window can still be dragged
      // to a monitor with different DPI. Windows won't bitmap-stretch it for us (this app is
      // Per-Monitor-V2 aware), so without this the window would keep its old physical size, and
      // the same "correctly scaled content, wrong-size frame" problem would reappear on drag.
      this.AppWindow.Changed += this.OnAppWindowChanged;

      this.Title = WindowTitle;
      this.PopulateContent();
      this.PopulateIcons();
   }

   /// <summary>AppWindow.Resize takes physical pixels, not the DIPs VisibleWidth/VisibleHeight
   ///    were tuned in, so the target size needs multiplying by the window's current DPI scale
   ///    factor -- see <see cref="DpiHelper"/>. The ~14px/~7px offset is the invisible
   ///    resize/shadow margin AppWindow.Resize's "logical" size includes beyond the visible
   ///    bounds (DwmGetWindowAttribute DWMWA_EXTENDED_FRAME_BOUNDS vs. GetWindowRect, tested
   ///    with a real window) -- that margin scales with DPI too, so it's included before
   ///    scaling rather than added after.</summary>
   private void ApplyWindowSize()
   {
      var scale = DpiHelper.GetScaleFactor(this);
      var width = (int)Math.Round((VisibleWidth + 14) * scale);
      var height = (int)Math.Round((VisibleHeight + 7) * scale);
      this.AppWindow.Resize(new SizeInt32(width, height));
   }

   /// <summary>HyperlinkButtonForeground/PointerOver/Pressed (see Grid.Resources in the XAML)
   ///    correctly handle entering a state, but the default template doesn't reliably revert to
   ///    the Normal state's color on pointer-exit. Setting Foreground directly here is a local
   ///    value, which takes precedence over whatever the template's own (buggy) state
   ///    transition does, so it reliably wins regardless of the underlying cause.</summary>
   /// <param name="sender">The sender.</param>
   /// <param name="e">The event arguments.</param>
   private void CardLinkButton_PointerEntered(object sender, PointerRoutedEventArgs e)
   {
      if (sender is HyperlinkButton button)
      {
         button.Foreground = (Brush)Application.Current.Resources["TextFillColorPrimaryBrush"];
      }
   }

   /// <inheritdoc cref="CardLinkButton_PointerEntered"/>
   private void CardLinkButton_PointerExited(object sender, PointerRoutedEventArgs e)
   {
      if (sender is HyperlinkButton button)
      {
         button.Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"];
      }
   }

   private void CardsGrid_Loaded(object sender, RoutedEventArgs e)
   {
      // Shared by both Resources/Donate cards -- one receiver registration covers both,
      // since sharing the ThemeShadow resource just shares its blur/softness recipe, not a
      // single physical shadow instance. CardShadowReceiver is a sibling of the two cards,
      // not their ancestor -- registering an ancestor as receiver faults natively.
      this.CardShadow.Receivers.Add(this.CardShadowReceiver);
   }

   /// <summary>Re-applies the window's size if it was dragged to a monitor with a different DPI
   ///    scale factor than the one it was last sized for. AppWindow.Changed doesn't expose a
   ///    dedicated "DPI changed" flag, so DidPositionChange (which does fire on a cross-monitor
   ///    drag) is used as the trigger, then the scale factor itself is compared to confirm it
   ///    actually changed before doing any work.</summary>
   private void OnAppWindowChanged(AppWindow sender, AppWindowChangedEventArgs args)
   {
      if (!args.DidPositionChange)
      {
         return;
      }

      var scale = DpiHelper.GetScaleFactor(this);
      if (Math.Abs(scale - this.lastScaleFactor) > 0.01)
      {
         this.lastScaleFactor = scale;
         this.ApplyWindowSize();
      }
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
      // The wordmark is styled with a pseudo 3D effect.
      this.WordmarkHost.Children.Add(
         IconPath.CreateWithOffsetShadow(
            VectorImageFactory.AboutLogoData,
            232,
            ColorTextPrimary,
            ColorTextSecondary,
            -1,
            3,
            0.375));

      this.ResourceSourceIconHost.Children.Add(
         IconPath.Create(VectorImageFactory.AboutResourceSourceData, 20, ColorTextPrimary));
      this.ResourceIssuesIconHost.Children.Add(
         IconPath.Create(
            VectorImageFactory.AboutResourceIssuesData,
            20,
            ColorTextPrimary,
            ColorTextDisabled));
      this.DonateGitHubIconHost.Children.Add(
         IconPath.Create(VectorImageFactory.AboutDonateGitHubData, 20, ColorTextPrimary));
      this.DonateBmcIconHost.Children.Add(
         IconPath.Create(VectorImageFactory.AboutDonateBuyMeACoffeeData, 20, ColorTextPrimary));
      this.DonateKoFiIconHost.Children.Add(
         IconPath.Create(VectorImageFactory.AboutDonateKoFiData, 20, ColorTextPrimary));
      this.DonatePayPalIconHost.Children.Add(
         IconPath.Create(VectorImageFactory.AboutDonatePayPalData, 20, ColorTextPrimary));
   }
}
