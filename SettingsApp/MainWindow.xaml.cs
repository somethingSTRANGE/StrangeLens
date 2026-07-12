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

   private double lastScaleFactor;

   public MainWindow()
   {
      this.InitializeComponent();

      this.AppWindow.SetIcon("Icons/AppIcon.ico");
      this.AppWindow.TitleBar.PreferredTheme = TitleBarTheme.UseDefaultAppMode;

      // Layout is finalized on fixed-width controls/cards, not content that benefits from
      // resizing, so the window is locked to its natural size -- the same rationale as
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

      this.lastScaleFactor = DpiHelper.GetScaleFactor(this);
      this.SizeWindowToContent();

      // IsResizable = false stops the user from resizing, but the window can still be dragged
      // to a monitor with different DPI. Windows won't bitmap-stretch it for us (this app is
      // Per-Monitor-V2 aware), so without this the window would keep its old physical size, and
      // the same "correctly scaled content, wrong-size frame" problem would reappear on drag.
      this.AppWindow.Changed += this.OnAppWindowChanged;
   }

   public Lens Settings { get; } = Lens.Instance;

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
         this.SizeWindowToContent();
      }
   }

   private void OnGridOpacityChanged(object sender, SelectionChangedEventArgs e)
   {
      if (this.GridOpacityComboBoxRow.ComboBox.SelectedIndex >= 0)
      {
         this.Settings.GridOpacity =
            Lens.GridOpacityOptions[this.GridOpacityComboBoxRow.ComboBox.SelectedIndex];
      }
   }

   private void OnGridSizeChanged(object sender, SelectionChangedEventArgs e)
   {
      if (this.GridSizeComboBoxRow.ComboBox.SelectedIndex >= 0)
      {
         this.Settings.GridSize =
            (byte)(this.GridSizeComboBoxRow.ComboBox.SelectedIndex + Lens.Defaults.MinGridSize);
      }
   }

   private void OnGridStyleChanged(object sender, SelectionChangedEventArgs e)
   {
      if (this.GridStyleComboBoxRow.ComboBox.SelectedIndex >= 0)
      {
         this.Settings.GridStyle = (GridStyleOption)this.GridStyleComboBoxRow.ComboBox.SelectedIndex;
         this.UpdateGridDependentControls();
      }
   }

   private void OnHeightChanged(object sender, SelectionChangedEventArgs e)
   {
      if (this.HeightComboBoxRow.ComboBox.SelectedIndex >= 0)
      {
         this.Settings.Height = (short)(Lens.Defaults.MinHeight
                                        + (this.HeightComboBoxRow.ComboBox.SelectedIndex
                                           * Lens.Defaults.SizeIncrement));
      }
   }

   private void OnMagnificationChanged(object sender, SelectionChangedEventArgs e)
   {
      if (this.MagnificationComboBoxRow.ComboBox.SelectedIndex >= 0)
      {
         this.Settings.Magnification = (byte)(this.MagnificationComboBoxRow.ComboBox.SelectedIndex
                                              + Lens.Defaults.MinMagnification);
      }
   }

   private void OnPrecisionSpeedChanged(object sender, SelectionChangedEventArgs e)
   {
      if (this.PrecisionSpeedComboBoxRow.ComboBox.SelectedIndex >= 0)
      {
         this.Settings.PrecisionSpeed =
            Lens.PrecisionSpeedOptions[this.PrecisionSpeedComboBoxRow.ComboBox.SelectedIndex];
      }
   }

   private void OnScalingModeChanged(object sender, SelectionChangedEventArgs e)
   {
      if (this.ScalingComboBoxRow.ComboBox.SelectedIndex >= 0)
      {
         this.Settings.Scaling = (ScalingModeOption)this.ScalingComboBoxRow.ComboBox.SelectedIndex;
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
            this.WidthComboBoxRow.ComboBox.SelectedIndex = (this.Settings.Width - Lens.Defaults.MinWidth)
                                                           / Lens.Defaults.SizeIncrement;
            break;
         case nameof(this.Settings.Height):
            this.HeightComboBoxRow.ComboBox.SelectedIndex = (this.Settings.Height - Lens.Defaults.MinHeight)
                                                            / Lens.Defaults.SizeIncrement;
            break;
         case nameof(this.Settings.GridStyle):
            this.GridStyleComboBoxRow.ComboBox.SelectedIndex = (int)this.Settings.GridStyle;
            this.UpdateGridDependentControls();
            break;
         case nameof(this.Settings.GridSize):
            this.GridSizeComboBoxRow.ComboBox.SelectedIndex =
               this.Settings.GridSize - Lens.Defaults.MinGridSize;
            break;
         case nameof(this.Settings.GridOpacity):
            var opacityIdx = Array.IndexOf(Lens.GridOpacityOptions, this.Settings.GridOpacity);
            if (opacityIdx >= 0)
            {
               this.GridOpacityComboBoxRow.ComboBox.SelectedIndex = opacityIdx;
            }

            break;
         case nameof(this.Settings.Magnification):
            this.MagnificationComboBoxRow.ComboBox.SelectedIndex =
               this.Settings.Magnification - Lens.Defaults.MinMagnification;
            break;
         case nameof(this.Settings.PrecisionSpeed):
            var precisionIdx = Array.IndexOf(Lens.PrecisionSpeedOptions, this.Settings.PrecisionSpeed);
            if (precisionIdx >= 0)
            {
               this.PrecisionSpeedComboBoxRow.ComboBox.SelectedIndex = precisionIdx;
            }

            break;
         case nameof(this.Settings.Scaling):
            this.ScalingComboBoxRow.ComboBox.SelectedIndex = (int)this.Settings.Scaling;
            break;
      }
   }

   private void OnWidthChanged(object sender, SelectionChangedEventArgs e)
   {
      if (this.WidthComboBoxRow.ComboBox.SelectedIndex >= 0)
      {
         this.Settings.Width = (short)(Lens.Defaults.MinWidth
                                       + (this.WidthComboBoxRow.ComboBox.SelectedIndex
                                          * Lens.Defaults.SizeIncrement));
      }
   }

   private void PopulateComboBoxes()
   {
      for (var i = Lens.Defaults.MinWidth; i <= Lens.Defaults.MaxWidth; i += Lens.Defaults.SizeIncrement)
      {
         this.WidthComboBoxRow.ComboBox.Items.Add($"{i} px");
      }

      this.WidthComboBoxRow.ComboBox.SelectedIndex =
         (this.Settings.Width - Lens.Defaults.MinWidth) / Lens.Defaults.SizeIncrement;

      for (var i = Lens.Defaults.MinHeight; i <= Lens.Defaults.MaxHeight; i += Lens.Defaults.SizeIncrement)
      {
         this.HeightComboBoxRow.ComboBox.Items.Add($"{i} px");
      }

      this.HeightComboBoxRow.ComboBox.SelectedIndex =
         (this.Settings.Height - Lens.Defaults.MinHeight) / Lens.Defaults.SizeIncrement;

      foreach (var pct in Lens.PrecisionSpeedOptions)
      {
         this.PrecisionSpeedComboBoxRow.ComboBox.Items.Add($"{pct}%");
      }

      this.PrecisionSpeedComboBoxRow.ComboBox.SelectedIndex = Array.IndexOf(
         Lens.PrecisionSpeedOptions,
         this.Settings.PrecisionSpeed);

      this.GridStyleComboBoxRow.ComboBox.Items.Add("None");
      this.GridStyleComboBoxRow.ComboBox.Items.Add("Solid");
      this.GridStyleComboBoxRow.ComboBox.Items.Add("Dash");
      this.GridStyleComboBoxRow.ComboBox.Items.Add("Dot");
      this.GridStyleComboBoxRow.ComboBox.Items.Add("Dash, Dot");
      this.GridStyleComboBoxRow.ComboBox.Items.Add("Dash, Dot, Dot");
      this.GridStyleComboBoxRow.ComboBox.SelectedIndex = (int)this.Settings.GridStyle;

      for (var i = Lens.Defaults.MinGridSize; i <= Lens.Defaults.MaxGridSize; i++)
      {
         this.GridSizeComboBoxRow.ComboBox.Items.Add(i == 1 ? "1 pixel" : $"{i} pixels");
      }

      this.GridSizeComboBoxRow.ComboBox.SelectedIndex = this.Settings.GridSize - Lens.Defaults.MinGridSize;

      foreach (var pct in Lens.GridOpacityOptions)
      {
         this.GridOpacityComboBoxRow.ComboBox.Items.Add($"{pct}%");
      }

      this.GridOpacityComboBoxRow.ComboBox.SelectedIndex = Array.IndexOf(
         Lens.GridOpacityOptions,
         this.Settings.GridOpacity);

      this.UpdateGridDependentControls();

      for (var i = Lens.Defaults.MinMagnification; i <= Lens.Defaults.MaxMagnification; i++)
      {
         this.MagnificationComboBoxRow.ComboBox.Items.Add($"×{i}");
      }

      this.MagnificationComboBoxRow.ComboBox.SelectedIndex =
         this.Settings.Magnification - Lens.Defaults.MinMagnification;

      this.ScalingComboBoxRow.ComboBox.Items.Add("Nearest neighbor");
      this.ScalingComboBoxRow.ComboBox.Items.Add("Bilinear");
      this.ScalingComboBoxRow.ComboBox.Items.Add("High quality bilinear");
      this.ScalingComboBoxRow.ComboBox.Items.Add("Bicubic");
      this.ScalingComboBoxRow.ComboBox.Items.Add("High quality bicubic");
      this.ScalingComboBoxRow.ComboBox.SelectedIndex = (int)this.Settings.Scaling;
   }

   /// <summary>WinUI3 has no WPF-style SizeToContent -- left alone, the window keeps whatever
   ///    size Windows last remembered for it, regardless of how much the actual content needs.
   ///    Measures the real content once at startup and resizes to fit, using the same
   ///    empirically confirmed offsets AboutWindow's fixed sizing relies on: AppWindow.Resize
   ///    takes a "logical" size larger than the true visible bounds by an invisible
   ///    resize/shadow margin (~14px wide / ~7px tall), and Measure() doesn't include the title
   ///    bar's own height (~32px). Resizing is disabled via the presenter setup in the
   ///    constructor, so unlike when this was first added, this size now sticks for the life of
   ///    the window rather than being just an initial hint. Content.Measure()/DesiredSize work
   ///    in DIPs, same as the rest of XAML, but AppWindow.Resize wants physical pixels -- see
   ///    <see cref="DpiHelper"/>. The chrome offsets scale with DPI too, so they're added to
   ///    the DIP measurement before scaling, not tacked on afterward as raw pixels.</summary>
   private void SizeWindowToContent()
   {
      this.Content.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

      var scale = DpiHelper.GetScaleFactor(this);
      var width = (int)Math.Round((this.Content.DesiredSize.Width + 14) * scale);
      var height = (int)Math.Round((this.Content.DesiredSize.Height + 32 + 7) * scale);
      this.AppWindow.Resize(new SizeInt32(width, height));
   }

   /// <summary>Grid size/opacity are meaningless with no grid drawn.</summary>
   private void UpdateGridDependentControls()
   {
      var hasGrid = this.GridStyleComboBoxRow.ComboBox.SelectedIndex != (int)GridStyleOption.None;
      this.GridSizeComboBoxRow.ComboBox.IsEnabled = hasGrid;
      this.GridOpacityComboBoxRow.ComboBox.IsEnabled = hasGrid;
   }

   /// <summary>Attached after initial <see cref="ComboBox.SelectedIndex"/> is set, so
   ///    population doesn't write the just-loaded value straight back into <see cref="Lens"/>.</summary>
   private void WireComboBoxHandlers()
   {
      this.WidthComboBoxRow.ComboBox.SelectionChanged += this.OnWidthChanged;
      this.HeightComboBoxRow.ComboBox.SelectionChanged += this.OnHeightChanged;
      this.PrecisionSpeedComboBoxRow.ComboBox.SelectionChanged += this.OnPrecisionSpeedChanged;
      this.GridStyleComboBoxRow.ComboBox.SelectionChanged += this.OnGridStyleChanged;
      this.GridSizeComboBoxRow.ComboBox.SelectionChanged += this.OnGridSizeChanged;
      this.GridOpacityComboBoxRow.ComboBox.SelectionChanged += this.OnGridOpacityChanged;
      this.MagnificationComboBoxRow.ComboBox.SelectionChanged += this.OnMagnificationChanged;
      this.ScalingComboBoxRow.ComboBox.SelectionChanged += this.OnScalingModeChanged;
   }
}
