// -------------------------------------------------------------------------------------
// <copyright file="LensSetting.cs" company="Strange Entertainment LLC">
//   Copyright 2004-2023 Strange Entertainment LLC. All rights reserved.
// </copyright>
// -------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Lens
{
   public class Lens : INotifyPropertyChanged
   {
      private static Lens instance;

      private Color  _gridColor       = Color.Black;
      private byte   _gridSize        = 4;
      private int    _gridStyle       = 2;
      private short  _height          = 160;
      private byte   _magnification   = 4;
      private ScalingMode _scalingMode = ScalingMode.NearestNeighbor;
      private byte   _speedFactor     = 4;
      private short  _width           = 150;

      private bool   _infoShowHex   = true;
      private bool   _infoShowRgb   = true;
      private bool   _infoShowHsl   = true;
      private bool   _infoShow12Bit = true;
      private bool   _infoShowWeb   = true;
      private bool   _infoShowMouse = true;
      private bool   _infoShowSize  = true;
      private bool   _infoShowZoom  = true;

      // Debug-only: force a theme regardless of OS setting. Values: "light", "dark", or omit/null for OS default.
      // Remove this field and all references to it once theme implementation is complete.
      private string _debugTheme    = null;

      private Lens() { }

      public static Lens Instance => instance ?? (instance = new Lens());

      public static string SettingsFilePath
      {
         get
         {
            var company = Assembly.GetExecutingAssembly()
               .GetCustomAttribute<AssemblyCompanyAttribute>()?.Company;
            if (string.IsNullOrWhiteSpace(company)) company = "Strange";
            var product = Assembly.GetExecutingAssembly()
               .GetCustomAttribute<AssemblyProductAttribute>()?.Product ?? "Lens";
            return Path.Combine(
               Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
               company, product, "settings.json");
         }
      }

      public Color GridColor
      {
         get => _gridColor;
         set => SetPersisted(ref _gridColor, value);
      }

      public byte GridSize
      {
         get => _gridSize;
         set => SetPersisted(ref _gridSize, value.Clamp(Defaults.MinGridSize, Defaults.MaxGridSize));
      }

      public int GridStyle
      {
         get => _gridStyle;
         set => SetPersisted(ref _gridStyle,
            value.Clamp((int)GridStyleOptions.None, (int)GridStyleOptions.DashDotDot));
      }

      public short Height
      {
         get => _height;
         set => SetPersisted(ref _height,
            (short)(value.Clamp(Defaults.MinHeight, Defaults.MaxHeight) / Defaults.SizeIncrement *
                    Defaults.SizeIncrement));
      }

      public byte Magnification
      {
         get => _magnification;
         set => SetPersisted(ref _magnification,
            value.Clamp(Defaults.MinMagnification, Defaults.MaxMagnification));
      }

      public ScalingMode Scaling
      {
         get => _scalingMode;
         set => SetPersisted(ref _scalingMode, value);
      }

      public byte SpeedFactor
      {
         get => _speedFactor;
         set => SetPersisted(ref _speedFactor,
            value.Clamp(Defaults.MinSpeedFactor, Defaults.MaxSpeedFactor));
      }

      public short Width
      {
         get => _width;
         set => SetPersisted(ref _width,
            (short)(value.Clamp(Defaults.MinWidth, Defaults.MaxWidth) / Defaults.SizeIncrement *
                    Defaults.SizeIncrement));
      }

      // Debug-only: remove once theme implementation is complete.
      public string DebugTheme => _debugTheme;

      // ── Info panel display toggles — persisted; UI to be added later. ──────────────────
      public bool InfoShowHex   { get => _infoShowHex;   set => SetPersisted(ref _infoShowHex,   value); }
      public bool InfoShowRgb   { get => _infoShowRgb;   set => SetPersisted(ref _infoShowRgb,   value); }
      public bool InfoShowHsl   { get => _infoShowHsl;   set => SetPersisted(ref _infoShowHsl,   value); }
      public bool InfoShow12Bit { get => _infoShow12Bit; set => SetPersisted(ref _infoShow12Bit, value); }
      public bool InfoShowWeb   { get => _infoShowWeb;   set => SetPersisted(ref _infoShowWeb,   value); }
      public bool InfoShowMouse { get => _infoShowMouse; set => SetPersisted(ref _infoShowMouse, value); }
      public bool InfoShowSize  { get => _infoShowSize;  set => SetPersisted(ref _infoShowSize,  value); }
      public bool InfoShowZoom  { get => _infoShowZoom;  set => SetPersisted(ref _infoShowZoom,  value); }

      public event PropertyChangedEventHandler PropertyChanged;

      private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

      public void Load()
      {
         var path = SettingsFilePath;
         if (!File.Exists(path)) return;
         try
         {
            var data = JsonSerializer.Deserialize<SettingsData>(File.ReadAllText(path));
            if (data == null) return;
            _width           = data.Width;
            _height          = data.Height;
            _magnification   = data.Magnification;
            _gridSize        = data.GridSize;
            _gridStyle       = data.GridStyle;
            _gridColor       = ColorTranslator.FromHtml(data.GridColor);
            _scalingMode     = (ScalingMode)data.Scaling;
            _speedFactor     = data.SpeedFactor;
            _infoShowHex     = data.InfoShowHex;
            _infoShowRgb     = data.InfoShowRgb;
            _infoShowHsl     = data.InfoShowHsl;
            _infoShow12Bit   = data.InfoShow12Bit;
            _infoShowWeb     = data.InfoShowWeb;
            _infoShowMouse   = data.InfoShowMouse;
            _infoShowSize    = data.InfoShowSize;
            _infoShowZoom    = data.InfoShowZoom;
            _debugTheme      = data.DebugTheme;
            Debug.WriteLine($"Settings loaded from {path}");
         }
         catch (Exception ex)
         {
            Debug.WriteLine($"Failed to load settings: {ex.Message}");
         }
      }

      public void Save()
      {
         var path = SettingsFilePath;
         try
         {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var data = new SettingsData
            {
               Width            = _width,
               Height           = _height,
               Magnification    = _magnification,
               GridSize         = _gridSize,
               GridStyle        = _gridStyle,
               GridColor        = $"#{_gridColor.R:X2}{_gridColor.G:X2}{_gridColor.B:X2}",
               Scaling          = (int)_scalingMode,
               SpeedFactor      = _speedFactor,
               InfoShowHex      = _infoShowHex,
               InfoShowRgb      = _infoShowRgb,
               InfoShowHsl      = _infoShowHsl,
               InfoShow12Bit    = _infoShow12Bit,
               InfoShowWeb      = _infoShowWeb,
               InfoShowMouse    = _infoShowMouse,
               InfoShowSize     = _infoShowSize,
               InfoShowZoom     = _infoShowZoom
            };
            File.WriteAllText(path, JsonSerializer.Serialize(data, JsonOptions));
         }
         catch (Exception ex)
         {
            Debug.WriteLine($"Failed to save settings: {ex.Message}");
         }
      }

      private void OnPropertyChanged([CallerMemberName] string propertyName = null)
      {
         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
      }

      private bool SetPersisted<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
      {
         if (EqualityComparer<T>.Default.Equals(field, value)) return false;
         field = value;
         Debug.WriteLine($"{propertyName} = {value}");
         OnPropertyChanged(propertyName);
         return true;
      }

      private bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
      {
         if (EqualityComparer<T>.Default.Equals(field, value)) return false;
         field = value;
         OnPropertyChanged(propertyName);
         return true;
      }

      private class SettingsData
      {
         public short  Width            { get; set; } = 150;
         public short  Height           { get; set; } = 160;
         public byte   Magnification    { get; set; } = 4;
         public byte   GridSize         { get; set; } = 4;
         public int    GridStyle        { get; set; } = 2;
         public string GridColor        { get; set; } = "#000000";
         public int    Scaling           { get; set; } = 0; // ScalingMode.NearestNeighbor
         public byte   SpeedFactor      { get; set; } = 4;

         public bool   InfoShowHex      { get; set; } = true;
         public bool   InfoShowRgb      { get; set; } = true;
         public bool   InfoShowHsl      { get; set; } = true;
         public bool   InfoShow12Bit    { get; set; } = true;
         public bool   InfoShowWeb      { get; set; } = true;
         public bool   InfoShowMouse    { get; set; } = true;
         public bool   InfoShowSize     { get; set; } = true;
         public bool   InfoShowZoom     { get; set; } = true;

         [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
         public string DebugTheme       { get; set; } = null;
      }

      public static class Defaults
      {
         public const byte MaxGridSize = 16;
         public const byte MinGridSize = 1;

         public const byte MaxMouseSpeed = 20;
         public const byte MinMouseSpeed = 1;

         public const byte MaxMagnification = 16;
         public const byte MinMagnification = 2;

         public const short MaxHeight = 400;
         public const short MinHeight = 100;

         public const short MaxWidth = 400;
         public const short MinWidth = 100;

         public const byte MaxSpeedFactor = 10;
         public const byte MinSpeedFactor = 1;

         public const byte SizeIncrement = 20;

         static Defaults()
         {
            Debug.Assert(SizeIncrement % 2 == 0);
            Debug.Assert(MaxHeight % SizeIncrement == 0);
            Debug.Assert(MinHeight % SizeIncrement == 0);
            Debug.Assert(MaxWidth % SizeIncrement == 0);
            Debug.Assert(MinWidth % SizeIncrement == 0);
         }
      }
   }
}
