// -------------------------------------------------------------------------------------
// <copyright file="ToggleSwitchRow.xaml.cs">
//   Copyright (c) 2026
//   Licensed under the MIT License. See LICENSE file in the project root.
// </copyright>
// -------------------------------------------------------------------------------------

namespace StrangeLens.SettingsApp;

using Microsoft.UI.Xaml;

/// <summary>The label+ToggleSwitch row shape repeated for every Info Panel setting: the same
///    Grid / column layout, the same OnContent / OffContent / Margin quirks, only the label
///    text and bound value actually differ per instance. Pulling that shape into one place
///    means a future tweak (spacing, toggle width, etc.) can't drift between instances the
///    way copied markup can.</summary>
public sealed partial class ToggleSwitchRow
{
   public static readonly DependencyProperty IsOnProperty = DependencyProperty.Register(
      nameof(IsOn),
      typeof(bool),
      typeof(ToggleSwitchRow),
      new PropertyMetadata(false));

   public static readonly DependencyProperty LabelProperty = DependencyProperty.Register(
      nameof(Label),
      typeof(string),
      typeof(ToggleSwitchRow),
      new PropertyMetadata(string.Empty));

   public ToggleSwitchRow()
   {
      this.InitializeComponent();
   }

   public bool IsOn
   {
      get => (bool)this.GetValue(IsOnProperty);
      set => this.SetValue(IsOnProperty, value);
   }

   public string Label
   {
      get => (string)this.GetValue(LabelProperty);
      set => this.SetValue(LabelProperty, value);
   }
}
