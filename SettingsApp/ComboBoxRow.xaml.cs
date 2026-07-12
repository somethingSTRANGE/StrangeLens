// -------------------------------------------------------------------------------------
// <copyright file="ComboBoxRow.xaml.cs">
//   Copyright (c) 2026
//   Licensed under the MIT License. See LICENSE file in the project root.
// </copyright>
// -------------------------------------------------------------------------------------

namespace StrangeLens.SettingsApp;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

/// <summary>The label+ComboBox row shape repeated for every Lens setting: the same
///    Grid/column layout, the same fixed ComboBox width, only the label text, the subsection
///    indent, and the bound value actually differ per instance. Pulling that shape into one
///    place means a future tweak can't drift between instances the way copied markup can.
///    Unlike <see cref="ToggleSwitchRow"/>, the inner ComboBox is exposed rather than
///    wrapping a bound value directly -- MainWindow populates Items and wires
///    SelectionChanged per box in code-behind, so the row needs to hand out the real control
///    rather than a single property.</summary>
public sealed partial class ComboBoxRow
{
   public static readonly DependencyProperty IndentProperty = DependencyProperty.Register(
      nameof(Indent),
      typeof(bool),
      typeof(ComboBoxRow),
      new PropertyMetadata(false));

   public static readonly DependencyProperty LabelProperty = DependencyProperty.Register(
      nameof(Label),
      typeof(string),
      typeof(ComboBoxRow),
      new PropertyMetadata(string.Empty));

   public ComboBoxRow()
   {
      this.InitializeComponent();
   }

   /// <summary>The row's ComboBox, for the owning window's code-behind to populate Items and
   ///    wire SelectionChanged against.</summary>
   public ComboBox ComboBox => this.InnerComboBox;

   /// <summary>Matches the 16px left indent the Grid/Magnification sub-rows use to read as
   ///    nested under their section header, versus the top-level Lens rows with none.</summary>
   public bool Indent
   {
      get => (bool)this.GetValue(IndentProperty);
      set => this.SetValue(IndentProperty, value);
   }

   public string Label
   {
      get => (string)this.GetValue(LabelProperty);
      set => this.SetValue(LabelProperty, value);
   }

   private Thickness GetLabelMargin(bool indent)
   {
      return indent ? new Thickness(16, 0, 0, 0) : new Thickness(0);
   }
}
