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
using System.Text.Json.Serialization;
using System.Timers;
using Microsoft.Win32;

namespace Lens
{
   public class ThemePalette
   {
      [JsonPropertyOrder(0)] public Color Inset      { get; init; }
      [JsonPropertyOrder(1)] public Color Background { get; init; }
      [JsonPropertyOrder(2)] public Color Control    { get; init; }
      [JsonPropertyOrder(3)] public Color Border     { get; init; }
      [JsonPropertyOrder(4)] public Color Accent     { get; init; }
      [JsonPropertyOrder(5)] public Color TextSubtle { get; init; }
      [JsonPropertyOrder(6)] public Color TextNormal { get; init; }
      [JsonPropertyOrder(7)] public Color TextStrong { get; init; }
   }

   public class Lens : INotifyPropertyChanged
   {
      private static Lens instance;

      private Color       _gridColor      = Color.Black;
      private byte        _gridSize       = 4;
      private int         _gridStyle      = 2;
      private short       _height         = 160;
      private byte        _magnification  = 4;
      private ScalingMode _scalingMode    = ScalingMode.NearestNeighbor;
      private int         _precisionSpeed = 45;
      private short       _width          = 150;

      private bool _infoShowHex   = true;
      private bool _infoShowRgb   = true;
      private bool _infoShowHsl   = true;
      private bool _infoShow12Bit = true;
      private bool _infoShowWeb   = true;
      private bool _infoShowMouse = true;
      private bool _infoShowSize  = true;
      private bool _infoShowZoom  = true;

      private string _theme = "system";
      private Dictionary<string, ThemePalette> _themes;

      private readonly Timer _saveTimer;

      // ── Default palettes ──────────────────────────────────────────────────────────────

      private static readonly ThemePalette DefaultDark = new()
      {
         Inset      = ColorTranslator.FromHtml("#191C22"),
         Background = ColorTranslator.FromHtml("#2E3440"),
         Control    = ColorTranslator.FromHtml("#434C5E"),
         Border     = ColorTranslator.FromHtml("#4C566A"),
         Accent     = ColorTranslator.FromHtml("#5E81AC"),
         TextSubtle = ColorTranslator.FromHtml("#D8DEE9"),
         TextNormal = ColorTranslator.FromHtml("#E5E9F0"),
         TextStrong = ColorTranslator.FromHtml("#FFFFFF"),
      };

      private static readonly ThemePalette DefaultLight = new()
      {
         Inset      = ColorTranslator.FromHtml("#B7C2D7"),
         Background = ColorTranslator.FromHtml("#E5E9F0"),
         Control    = ColorTranslator.FromHtml("#ECEFF4"),
         Border     = ColorTranslator.FromHtml("#D8DEE9"),
         Accent     = ColorTranslator.FromHtml("#5E81AC"),
         TextSubtle = ColorTranslator.FromHtml("#4C566A"),
         TextNormal = ColorTranslator.FromHtml("#3B4252"),
         TextStrong = ColorTranslator.FromHtml("#2E3440"),
      };

      private Lens()
      {
         _themes = new Dictionary<string, ThemePalette>(StringComparer.OrdinalIgnoreCase)
         {
            ["dark"]  = DefaultDark,
            ["light"] = DefaultLight,
         };
         _saveTimer = new Timer(1500) { AutoReset = false };
         _saveTimer.Elapsed += (_, _) => Save();
         PropertyChanged += (_, _) => { _saveTimer.Stop(); _saveTimer.Start(); };
      }

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

      public static readonly int[] PrecisionSpeedOptions = { 10, 25, 45, 70 };

      public int PrecisionSpeed
      {
         get => _precisionSpeed;
         set => SetPersisted(ref _precisionSpeed, value);
      }

      public short Width
      {
         get => _width;
         set => SetPersisted(ref _width,
            (short)(value.Clamp(Defaults.MinWidth, Defaults.MaxWidth) / Defaults.SizeIncrement *
                    Defaults.SizeIncrement));
      }

      public string Theme
      {
         get => _theme;
         set => SetPersisted(ref _theme, value);
      }

      public IReadOnlyDictionary<string, ThemePalette> Themes => _themes;

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

      public static bool IsOsDarkMode()
      {
         try
         {
            using var key = Registry.CurrentUser.OpenSubKey(
               @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            return key?.GetValue("AppsUseLightTheme") is int v && v == 0;
         }
         catch { return false; }
      }

      private static readonly JsonSerializerOptions JsonOptions = new()
      {
         WriteIndented = true,
         Converters    = { new ColorHexConverter() }
      };

      public void Load()
      {
         var path = SettingsFilePath;
         if (!File.Exists(path)) return;
         try
         {
            var data = JsonSerializer.Deserialize<SettingsData>(File.ReadAllText(path), JsonOptions);
            if (data == null) return;
            Width          = data.Width;
            Height         = data.Height;
            _magnification = data.Magnification;
            _gridSize      = data.GridSize;
            _gridStyle     = data.GridStyle;
            _gridColor     = ColorTranslator.FromHtml(data.GridColor);
            _scalingMode    = (ScalingMode)data.Scaling;
            _precisionSpeed = Array.IndexOf(PrecisionSpeedOptions, data.PrecisionSpeed) >= 0
               ? data.PrecisionSpeed : 45;
            _infoShowHex   = data.InfoShowHex;
            _infoShowRgb   = data.InfoShowRgb;
            _infoShowHsl   = data.InfoShowHsl;
            _infoShow12Bit = data.InfoShow12Bit;
            _infoShowWeb   = data.InfoShowWeb;
            _infoShowMouse = data.InfoShowMouse;
            _infoShowSize  = data.InfoShowSize;
            _infoShowZoom  = data.InfoShowZoom;

            // Build themes: start with built-in defaults, overlay with anything from the file.
            _themes = new Dictionary<string, ThemePalette>(StringComparer.OrdinalIgnoreCase)
            {
               ["dark"]  = DefaultDark,
               ["light"] = DefaultLight,
            };
            if (data.Themes != null)
               foreach (var kvp in data.Themes)
                  _themes[kvp.Key] = kvp.Value;

            var osDark = IsOsDarkMode();

            // Resolve the active theme name.
            var raw = data.Theme;
            if (string.IsNullOrEmpty(raw))
               _theme = "system";
            else if (raw == "system" || _themes.ContainsKey(raw))
               _theme = raw;
            else
               _theme = osDark ? "dark" : "light";

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
               Width         = _width,
               Height        = _height,
               Magnification = _magnification,
               GridSize      = _gridSize,
               GridStyle     = _gridStyle,
               GridColor     = $"#{_gridColor.R:X2}{_gridColor.G:X2}{_gridColor.B:X2}",
               Scaling        = (int)_scalingMode,
               PrecisionSpeed = _precisionSpeed,
               InfoShowHex   = _infoShowHex,
               InfoShowRgb   = _infoShowRgb,
               InfoShowHsl   = _infoShowHsl,
               InfoShow12Bit = _infoShow12Bit,
               InfoShowWeb   = _infoShowWeb,
               InfoShowMouse = _infoShowMouse,
               InfoShowSize  = _infoShowSize,
               InfoShowZoom  = _infoShowZoom,
               Theme         = _theme,
               Themes        = _themes,
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

      private sealed class ColorHexConverter : JsonConverter<Color>
      {
         public override Color Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => ColorTranslator.FromHtml(reader.GetString() ?? "#000000");

         public override void Write(Utf8JsonWriter writer, Color value, JsonSerializerOptions options)
            => writer.WriteStringValue($"#{value.R:X2}{value.G:X2}{value.B:X2}");
      }

      private class SettingsData
      {
         public short  Width         { get; set; } = 150;
         public short  Height        { get; set; } = 160;
         public byte   Magnification { get; set; } = 4;
         public byte   GridSize      { get; set; } = 4;
         public int    GridStyle     { get; set; } = 2;
         public string GridColor     { get; set; } = "#000000";
         public int    Scaling        { get; set; } = 0; // ScalingMode.NearestNeighbor
         public int    PrecisionSpeed { get; set; } = 45;

         public bool   InfoShowHex   { get; set; } = true;
         public bool   InfoShowRgb   { get; set; } = true;
         public bool   InfoShowHsl   { get; set; } = true;
         public bool   InfoShow12Bit { get; set; } = true;
         public bool   InfoShowWeb   { get; set; } = true;
         public bool   InfoShowMouse { get; set; } = true;
         public bool   InfoShowSize  { get; set; } = true;
         public bool   InfoShowZoom  { get; set; } = true;

         public string Theme  { get; set; } = "system";
         public Dictionary<string, ThemePalette> Themes { get; set; } = null;
      }

      public static class Defaults
      {
         public const byte MaxGridSize = 16;
         public const byte MinGridSize = 1;

         public const byte MaxMagnification = 16;
         public const byte MinMagnification = 2;

         public const short MaxHeight = 400;
         public const short MinHeight = 100;

         public const short MaxWidth = 400;
         public const short MinWidth = 100;

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