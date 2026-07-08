namespace StrangeLens.SettingsApp;

using System;
using System.ComponentModel;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using StrangeLens;

public sealed partial class MainWindow
{
   internal const string WindowTitle = "Strange Lens Settings";

   public MainWindow()
   {
      this.InitializeComponent();
      this.Title = WindowTitle;

      this.PopulateComboBoxes();
      this.WireComboBoxHandlers();

      this.Settings.PropertyChanged += this.OnSettingChanged;
      this.Closed += (_, _) => this.Settings.PropertyChanged -= this.OnSettingChanged;
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

   /// <summary>Keeps the combo boxes in sync when <see cref="Lens"/> changes from outside
   ///    this window's own controls -- an external settings.json reload (another process),
   ///    or (for width/height/magnification/grid size) a lens keyboard shortcut.</summary>
   private void OnSettingChanged(object? sender, PropertyChangedEventArgs e)
   {
      switch (e.PropertyName)
      {
         case nameof(this.Settings.Width):
            this.WidthComboBox.SelectedIndex =
               (this.Settings.Width - Lens.Defaults.MinWidth) / Lens.Defaults.SizeIncrement;
            break;
         case nameof(this.Settings.Height):
            this.HeightComboBox.SelectedIndex =
               (this.Settings.Height - Lens.Defaults.MinHeight) / Lens.Defaults.SizeIncrement;
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

      this.PrecisionSpeedComboBox.SelectedIndex =
         Array.IndexOf(Lens.PrecisionSpeedOptions, this.Settings.PrecisionSpeed);

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

      this.GridOpacityComboBox.SelectedIndex = Array.IndexOf(Lens.GridOpacityOptions, this.Settings.GridOpacity);

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

   /// <summary>Grid size/opacity are meaningless with no grid drawn.</summary>
   private void UpdateGridDependentControls()
   {
      var hasGrid = this.GridStyleComboBox.SelectedIndex != (int)GridStyleOption.None;
      this.GridSizeComboBox.IsEnabled = hasGrid;
      this.GridOpacityComboBox.IsEnabled = hasGrid;
   }

   /// <summary>Attached after initial <see cref="ComboBox.SelectedIndex"/> is set, so
   ///    population doesn't write the just-loaded value straight back into
   ///    <see cref="Lens"/>.</summary>
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
