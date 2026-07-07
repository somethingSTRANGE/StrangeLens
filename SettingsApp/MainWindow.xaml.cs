// -------------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs">
//   Copyright (c) 2026
//   Licensed under the MIT License. See LICENSE file in the project root.
// </copyright>
// -------------------------------------------------------------------------------------

namespace StrangeLens.SettingsApp;

using Microsoft.UI.Xaml;

public sealed partial class MainWindow
{
   public MainWindow()
   {
      this.InitializeComponent();
      this.Title = "Strange Lens Settings (spike)";
   }

   private void OnCloseClick(object sender, RoutedEventArgs e)
   {
      this.Close();
   }
}
