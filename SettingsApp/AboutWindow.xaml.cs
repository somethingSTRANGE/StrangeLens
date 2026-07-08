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

using Microsoft.UI.Xaml;

public sealed partial class AboutWindow
{
   internal const string WindowTitle = "About Strange Lens";

   private string versionSummary = string.Empty;

   public AboutWindow()
   {
      this.InitializeComponent();
      this.Title = WindowTitle;
      this.PopulateContent();
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

      this.ProductNameText.Text = productName;
      this.VersionText.Text = $"Version {buildVersion}";
      this.BuildDateText.Text = $"Built on {buildDate} from commit {commitHash}";
      this.CopyrightText.Text = $"{copyright} — MIT License";

      this.versionSummary = $"{productName} {buildVersion}\nBuilt on {buildDate} from commit {commitHash}";
   }
}
