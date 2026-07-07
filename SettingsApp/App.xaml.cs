// -------------------------------------------------------------------------------------
// <copyright file="App.xaml.cs">
//   Copyright (c) 2026
//   Licensed under the MIT License. See LICENSE file in the project root.
// </copyright>
// -------------------------------------------------------------------------------------

namespace StrangeLens.SettingsApp;

using Microsoft.UI.Xaml;

public partial class App
{
   private Window? window;

   public App()
   {
      this.InitializeComponent();
   }

   protected override void OnLaunched(LaunchActivatedEventArgs args)
   {
      this.window = new MainWindow();
      this.window.Activate();
   }
}
