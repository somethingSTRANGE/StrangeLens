// -------------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs">
//   Copyright (c) 2026
//   Licensed under the MIT License. See LICENSE file in the project root.
// </copyright>
// -------------------------------------------------------------------------------------

namespace StrangeLens.SettingsApp;

using System;
using System.ComponentModel;

using Windows.Foundation;
using Windows.Graphics;

using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml.Controls;

public sealed partial class MainWindow
{
   internal const string WindowTitle = "Strange Lens Settings";

   public MainWindow()
   {
      this.InitializeComponent();

      this.AppWindow.SetIcon("Icons/AppIcon.ico");

      // Layout is finalized on fixed-width controls/cards, not content that benefits from
      // resizing, so the window is locked to its natural size -- same rationale as
      // AboutWindow's presenter setup. Minimize is left enabled (unlike AboutWindow), since
      // there's a real reason to want Settings out of the way temporarily without closing it;
      // Maximize doesn't make sense once resizing itself is disabled.
      var presenter = OverlappedPresenter.Create();
      presenter.IsMaximizable = false;
      presenter.IsResizable = false;
      presenter.SetBorderAndTitleBar(true, true);
      this.AppWindow.SetPresenter(presenter);

      this.Title = WindowTitle;

      this.PopulateComboBoxes();
      this.WireComboBoxHandlers();

      this.Settings.PropertyChanged += this.OnSettingChanged;
      this.Closed += (_, _) => this.Settings.PropertyChanged -= this.OnSettingChanged;

      this.SizeWindowToContent();
   }

   public Lens Settings { get; } = Lens.Instance;

   private void OnGridOpacityChanged(object sender, SelectionChangedEventArgs e)
   {
      if (this.GridOpacityComboBox.SelectedIndex >= 0)
      {
         this.Settings.GridOpacity = Lens.GridOpacityOptions[this.GridOpacityComboBox.SelectedIndex];
      }
   }

   private void OnGridSizeChanged(object sender, SelectionChangedEventArgs e)
   {
      if (this.GridSizeComboBox.SelectedIndex >= 0)
      {
         this.Settings.GridSize = (byte)(this.GridSizeComboBox.SelectedIndex + Lens.Defaults.MinGridSize);
      }
   }

   private void OnGridStyleChanged(object sender, SelectionChangedEventArgs e)
   {
      if (this.GridStyleComboBox.SelectedIndex >= 0)
      {
         this.Settings.GridStyle = (GridStyleOption)this.GridStyleComboBox.SelectedIndex;
         this.UpdateGridDependentControls();
      }
   }

   private void OnHeightChanged(object sender, SelectionChangedEventArgs e)
   {
      if (this.HeightComboBox.SelectedIndex >= 0)
      {
         this.Settings.Height = (short)(Lens.Defaults.MinHeight
                                        + (this.HeightComboBox.SelectedIndex * Lens.Defaults.SizeIncrement));
      }
   }

   private void OnMagnificationChanged(object sender, SelectionChangedEventArgs e)
   {
      if (this.MagnificationComboBox.SelectedIndex >= 0)
      {
         this.Settings.Magnification =
            (byte)(this.MagnificationComboBox.SelectedIndex + Lens.Defaults.MinMagnification);
      }
   }

   private void OnPrecisionSpeedChanged(object sender, SelectionChangedEventArgs e)
   {
      if (this.PrecisionSpeedComboBox.SelectedIndex >= 0)
      {
         this.Settings.PrecisionSpeed = Lens.PrecisionSpeedOptions[this.PrecisionSpeedComboBox.SelectedIndex];
      }
   }

   private void OnScalingModeChanged(object sender, SelectionChangedEventArgs e)
   {
      if (this.ScalingComboBox.SelectedIndex >= 0)
      {
         this.Settings.Scaling = (ScalingModeOption)this.ScalingComboBox.SelectedIndex;
      }
   }

   /// <summary>Keeps the combo boxes in sync when <see cref="Lens"/> changes from outside this
   ///    window's own controls -- an external settings.json reload (another process), or (for
   ///    width/height/magnification/grid size) a lens keyboard shortcut.</summary>
   private void OnSettingChanged(object? sender, PropertyChangedEventArgs e)
   {
      switch (e.PropertyName)
      {
         case nameof(this.Settings.Width):
            this.WidthComboBox.SelectedIndex =
               (this.Settings.Width - Lens.Defaults.MinWidth) / Lens.Defaults.SizeIncrement;
            break;
         case nameof(this.Settings.Height):
            this.HeightComboBox.SelectedIndex = (this.Settings.Height - Lens.Defaults.MinHeight)
                                                / Lens.Defaults.SizeIncrement;
            break;
         case nameof(this.Settings.GridStyle):
            this.GridStyleComboBox.SelectedIndex = (int)this.Settings.GridStyle;
            this.UpdateGridDependentControls();
            break;
         case nameof(this.Settings.GridSize):
            this.GridSizeComboBox.SelectedIndex = this.Settings.GridSize - Lens.Defaults.MinGridSize;
            break;
         case nameof(this.Settings.GridOpacity):
            var opacityIdx = Array.IndexOf(Lens.GridOpacityOptions, this.Settings.GridOpacity);
            if (opacityIdx >= 0)
            {
               this.GridOpacityComboBox.SelectedIndex = opacityIdx;
            }

            break;
         case nameof(this.Settings.Magnification):
            this.MagnificationComboBox.SelectedIndex =
               this.Settings.Magnification - Lens.Defaults.MinMagnification;
            break;
         case nameof(this.Settings.PrecisionSpeed):
            var precisionIdx = Array.IndexOf(Lens.PrecisionSpeedOptions, this.Settings.PrecisionSpeed);
            if (precisionIdx >= 0)
            {
               this.PrecisionSpeedComboBox.SelectedIndex = precisionIdx;
            }

            break;
         case nameof(this.Settings.Scaling):
            this.ScalingComboBox.SelectedIndex = (int)this.Settings.Scaling;
            break;
      }
   }

   private void OnWidthChanged(object sender, SelectionChangedEventArgs e)
   {
      if (this.WidthComboBox.SelectedIndex >= 0)
      {
         this.Settings.Width = (short)(Lens.Defaults.MinWidth
                                       + (this.WidthComboBox.SelectedIndex * Lens.Defaults.SizeIncrement));
      }
   }

   private void PopulateComboBoxes()
   {
      for (var i = Lens.Defaults.MinWidth; i <= Lens.Defaults.MaxWidth; i += Lens.Defaults.SizeIncrement)
      {
         this.WidthComboBox.Items.Add($"{i} px");
      }

      this.WidthComboBox.SelectedIndex =
         (this.Settings.Width - Lens.Defaults.MinWidth) / Lens.Defaults.SizeIncrement;

      for (var i = Lens.Defaults.MinHeight; i <= Lens.Defaults.MaxHeight; i += Lens.Defaults.SizeIncrement)
      {
         this.HeightComboBox.Items.Add($"{i} px");
      }

      this.HeightComboBox.SelectedIndex =
         (this.Settings.Height - Lens.Defaults.MinHeight) / Lens.Defaults.SizeIncrement;

      foreach (var pct in Lens.PrecisionSpeedOptions)
      {
         this.PrecisionSpeedComboBox.Items.Add($"{pct}%");
      }

      this.PrecisionSpeedComboBox.SelectedIndex = Array.IndexOf(
         Lens.PrecisionSpeedOptions,
         this.Settings.PrecisionSpeed);

      this.GridStyleComboBox.Items.Add("None");
      this.GridStyleComboBox.Items.Add("Solid");
      this.GridStyleComboBox.Items.Add("Dash");
      this.GridStyleComboBox.Items.Add("Dot");
      this.GridStyleComboBox.Items.Add("Dash, Dot");
      this.GridStyleComboBox.Items.Add("Dash, Dot, Dot");
      this.GridStyleComboBox.SelectedIndex = (int)this.Settings.GridStyle;

      for (var i = Lens.Defaults.MinGridSize; i <= Lens.Defaults.MaxGridSize; i++)
      {
         this.GridSizeComboBox.Items.Add(i == 1 ? "1 pixel" : $"{i} pixels");
      }

      this.GridSizeComboBox.SelectedIndex = this.Settings.GridSize - Lens.Defaults.MinGridSize;

      foreach (var pct in Lens.GridOpacityOptions)
      {
         this.GridOpacityComboBox.Items.Add($"{pct}%");
      }

      this.GridOpacityComboBox.SelectedIndex = Array.IndexOf(
         Lens.GridOpacityOptions,
         this.Settings.GridOpacity);

      this.UpdateGridDependentControls();

      for (var i = Lens.Defaults.MinMagnification; i <= Lens.Defaults.MaxMagnification; i++)
      {
         this.MagnificationComboBox.Items.Add($"×{i}");
      }

      this.MagnificationComboBox.SelectedIndex = this.Settings.Magnification - Lens.Defaults.MinMagnification;

      this.ScalingComboBox.Items.Add("Nearest neighbor");
      this.ScalingComboBox.Items.Add("Bilinear");
      this.ScalingComboBox.Items.Add("High quality bilinear");
      this.ScalingComboBox.Items.Add("Bicubic");
      this.ScalingComboBox.Items.Add("High quality bicubic");
      this.ScalingComboBox.SelectedIndex = (int)this.Settings.Scaling;
   }

   /// <summary>WinUI3 has no WPF-style SizeToContent -- left alone, the window keeps whatever
   ///    size Windows last remembered for it, regardless of how much the actual content needs.
   ///    Measures the real content once at startup and resizes to fit, using the same
   ///    empirically confirmed offsets AboutWindow's fixed sizing relies on: AppWindow.Resize
   ///    takes a "logical" size larger than the true visible bounds by an invisible
   ///    resize/shadow margin (~14px wide / ~7px tall), and Measure() doesn't include the title
   ///    bar's own height (~32px). Resizing is disabled via the presenter set up in the
   ///    constructor, so unlike when this was first added, this size now sticks for the life of
   ///    the window rather than being just an initial hint.</summary>
   private void SizeWindowToContent()
   {
      this.Content.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
      var width = (int)Math.Ceiling(this.Content.DesiredSize.Width) + 14;
      var height = (int)Math.Ceiling(this.Content.DesiredSize.Height) + 32 + 7;
      this.AppWindow.Resize(new SizeInt32(width, height));
   }

   /// <summary>Grid size/opacity are meaningless with no grid drawn.</summary>
   private void UpdateGridDependentControls()
   {
      var hasGrid = this.GridStyleComboBox.SelectedIndex != (int)GridStyleOption.None;
      this.GridSizeComboBox.IsEnabled = hasGrid;
      this.GridOpacityComboBox.IsEnabled = hasGrid;
   }

   /// <summary>Attached after initial <see cref="ComboBox.SelectedIndex"/> is set, so
   ///    population doesn't write the just-loaded value straight back into <see cref="Lens"/>.</summary>
   private void WireComboBoxHandlers()
   {
      this.WidthComboBox.SelectionChanged += this.OnWidthChanged;
      this.HeightComboBox.SelectionChanged += this.OnHeightChanged;
      this.PrecisionSpeedComboBox.SelectionChanged += this.OnPrecisionSpeedChanged;
      this.GridStyleComboBox.SelectionChanged += this.OnGridStyleChanged;
      this.GridSizeComboBox.SelectionChanged += this.OnGridSizeChanged;
      this.GridOpacityComboBox.SelectionChanged += this.OnGridOpacityChanged;
      this.MagnificationComboBox.SelectionChanged += this.OnMagnificationChanged;
      this.ScalingComboBox.SelectionChanged += this.OnScalingModeChanged;
   }
}
