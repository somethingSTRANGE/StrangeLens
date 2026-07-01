// -------------------------------------------------------------------------------------
// <copyright file="LensSetting.cs">
//   Copyright (c) 2026
//   Licensed under the MIT License. See LICENSE file in the project root.
// </copyright>
// -------------------------------------------------------------------------------------

using Timer = System.Timers.Timer;

namespace StrangeLens
{
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

   using Microsoft.Win32;

   public class ThemePalette
   {
      [JsonPropertyOrder(5)]
      public Color AccentNormal { get; init; }

      [JsonPropertyOrder(6)]
      public Color AccentStrong { get; init; }

      [JsonPropertyOrder(4)]
      public Color AccentSubtle { get; init; }

      [JsonPropertyOrder(1)]
      public Color Background { get; init; }

      [JsonPropertyOrder(3)]
      public Color Border { get; init; }

      [JsonPropertyOrder(2)]
      public Color Control { get; init; }

      [JsonPropertyOrder(0)]
      public Color Inset { get; init; }

      [JsonPropertyOrder(8)]
      public Color TextNormal { get; init; }

      [JsonPropertyOrder(9)]
      public Color TextStrong { get; init; }

      [JsonPropertyOrder(7)]
      public Color TextSubtle { get; init; }
   }

   public class Lens : INotifyPropertyChanged
   {
      public static readonly int[] GridOpacityOptions = [20, 40, 60, 80, 100];

      public static readonly int[] PrecisionSpeedOptions = [10, 25, 45, 70];

      private static readonly ThemePalette defaultDark = new()
         {
            Inset = ColorTranslator.FromHtml("#191C22"), // Nord0, -10% L
            Background = ColorTranslator.FromHtml("#2E3440"), // Nord0
            Control = ColorTranslator.FromHtml("#434C5E"), // Nord2
            Border = ColorTranslator.FromHtml("#4C566A"), // Nord3
            AccentSubtle = ColorTranslator.FromHtml("#55749b"), // Nord10, -10% L
            AccentNormal = ColorTranslator.FromHtml("#5E81AC"), // Nord10
            AccentStrong = ColorTranslator.FromHtml("#5791d8"), // Nord10, +15% L +30% S
            TextSubtle = ColorTranslator.FromHtml("#A4B2CB"), // Nord5, -20% L
            TextNormal = ColorTranslator.FromHtml("#E5E9F0"), // Nord5
            TextStrong = ColorTranslator.FromHtml("#FFFFFF"), // Nord5, +10% L
         };

      private static readonly ThemePalette defaultLight = new()
         {
            Inset = ColorTranslator.FromHtml("#B7C2D7"), // Nord4, -10% L
            Background = ColorTranslator.FromHtml("#E5E9F0"), // Nord5
            Control = ColorTranslator.FromHtml("#ECEFF4"), // Nord6
            Border = ColorTranslator.FromHtml("#D8DEE9"), // Nord4
            AccentSubtle = ColorTranslator.FromHtml("#55749b"), // Nord10, +15% L +30% S
            AccentNormal = ColorTranslator.FromHtml("#5E81AC"), // Nord10
            AccentStrong = ColorTranslator.FromHtml("#5791d8"), // Nord10, +10% L +20% S
            TextSubtle = ColorTranslator.FromHtml("#4C566A"), // Nord3
            TextNormal = ColorTranslator.FromHtml("#3B4252"), // Nord1
            TextStrong = ColorTranslator.FromHtml("#2E3440"), // Nord0
         };

      private static readonly JsonSerializerOptions jsonOptions = new()
         {
            WriteIndented = true,
            Converters =
               {
                  new ColorHexConverter(),
               },
         };

      private static Lens? instance;

      private readonly Timer saveTimer;

      private int gridOpacity = 20;

      private byte gridSize = 4;

      private int gridStyle = (int)GridStyleOption.Dash;

      private short height = 160;

      private bool infoShow12Bit = true;

      private bool infoShowHex = true;

      private bool infoShowHsl = true;

      private bool infoShowMouse = true;

      private bool infoShowRgb = true;

      private bool infoShowSize = true;

      private bool infoShowWeb = true;

      private bool infoShowZoom = true;

      private byte magnification = 4;

      private int precisionSpeed = 45;

      private ScalingModeOption scalingMode = ScalingModeOption.NearestNeighbor;

      private string theme = "system";

      private Dictionary<string, ThemePalette> themes;

      private short width = 150;

      private Lens()
      {
         this.themes = new Dictionary<string, ThemePalette>(StringComparer.OrdinalIgnoreCase)
            {
               ["dark"] = defaultDark,
               ["light"] = defaultLight,
            };
         this.saveTimer = new Timer(1500)
            {
               AutoReset = false,
            };
         this.saveTimer.Elapsed += (_, _) => this.Save();
         this.PropertyChanged += (_, _) =>
            {
               this.saveTimer.Stop();
               this.saveTimer.Start();
            };
      }

      public event PropertyChangedEventHandler? PropertyChanged;

      public static Lens Instance => instance ??= new Lens();

      public ThemePalette ActivePalette
      {
         get
         {
            var name = this.theme == "system" ? IsOsDarkMode() ? "dark" : "light" : this.theme;
            return this.themes.GetValueOrDefault(name, defaultDark);
         }
      }

      public int GridOpacity
      {
         get => this.gridOpacity;
         set =>
            this.SetPersisted(
               ref this.gridOpacity,
               Array.IndexOf(GridOpacityOptions, value) >= 0 ? value : this.gridOpacity);
      }

      public byte GridSize
      {
         get => this.gridSize;
         set => this.SetPersisted(ref this.gridSize, value.Clamp(Defaults.MinGridSize, Defaults.MaxGridSize));
      }

      public int GridStyle
      {
         get => this.gridStyle;
         set =>
            this.SetPersisted(
               ref this.gridStyle,
               value.Clamp((int)GridStyleOption.None, (int)GridStyleOption.DashDotDot));
      }

      public short Height
      {
         get => this.height;
         set =>
            this.SetPersisted(
               ref this.height,
               (short)((value.Clamp(Defaults.MinHeight, Defaults.MaxHeight) / Defaults.SizeIncrement)
                       * Defaults.SizeIncrement));
      }

      public bool InfoShow12Bit
      {
         get => this.infoShow12Bit;
         set => this.SetPersisted(ref this.infoShow12Bit, value);
      }

      public bool InfoShowHex
      {
         get => this.infoShowHex;
         set => this.SetPersisted(ref this.infoShowHex, value);
      }

      public bool InfoShowHsl
      {
         get => this.infoShowHsl;
         set => this.SetPersisted(ref this.infoShowHsl, value);
      }

      public bool InfoShowMouse
      {
         get => this.infoShowMouse;
         set => this.SetPersisted(ref this.infoShowMouse, value);
      }

      public bool InfoShowRgb
      {
         get => this.infoShowRgb;
         set => this.SetPersisted(ref this.infoShowRgb, value);
      }

      public bool InfoShowSize
      {
         get => this.infoShowSize;
         set => this.SetPersisted(ref this.infoShowSize, value);
      }

      public bool InfoShowWeb
      {
         get => this.infoShowWeb;
         set => this.SetPersisted(ref this.infoShowWeb, value);
      }

      public bool InfoShowZoom
      {
         get => this.infoShowZoom;
         set => this.SetPersisted(ref this.infoShowZoom, value);
      }

      public byte Magnification
      {
         get => this.magnification;
         set =>
            this.SetPersisted(
               ref this.magnification,
               value.Clamp(Defaults.MinMagnification, Defaults.MaxMagnification));
      }

      public int PrecisionSpeed
      {
         get => this.precisionSpeed;
         set =>
            this.SetPersisted(
               ref this.precisionSpeed,
               Array.IndexOf(PrecisionSpeedOptions, value) >= 0 ? value : this.precisionSpeed);
      }

      public ScalingModeOption Scaling
      {
         get => this.scalingMode;
         set =>
            this.SetPersisted(
               ref this.scalingMode,
               Enum.IsDefined(typeof(ScalingModeOption), value) ? value : this.scalingMode);
      }

      public string Theme
      {
         get => this.theme;
         private set =>
            this.SetPersisted(
               ref this.theme,
               string.IsNullOrEmpty(value) ? "system" :
               (value == "system") || this.themes.ContainsKey(value) ? value :
               IsOsDarkMode() ? "dark" : "light");
      }

      public IReadOnlyDictionary<string, ThemePalette> Themes
      {
         get => this.themes;
         private set
         {
            this.themes = new Dictionary<string, ThemePalette>(StringComparer.OrdinalIgnoreCase)
               {
                  ["dark"] = defaultDark,
                  ["light"] = defaultLight,
               };

            foreach (var kvp in value)
            {
               this.themes[kvp.Key] = this.themes.TryGetValue(kvp.Key, out var fallback)
                  ? FillMissing(kvp.Value, fallback)
                  : kvp.Value;
            }
         }
      }

      public short Width
      {
         get => this.width;
         set =>
            this.SetPersisted(
               ref this.width,
               (short)((value.Clamp(Defaults.MinWidth, Defaults.MaxWidth) / Defaults.SizeIncrement)
                       * Defaults.SizeIncrement));
      }

      private static string SettingsFilePath
      {
         get
         {
            var company = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyCompanyAttribute>()
               ?.Company;
            if (string.IsNullOrWhiteSpace(company))
            {
               company = "Strange";
            }

            var product = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyProductAttribute>()
               ?.Product ?? "StrangeLens";
            return Path.Combine(
               Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
               company,
               product,
               "settings.json");
         }
      }

      public static bool IsOsDarkMode()
      {
         try
         {
            using var key = Registry.CurrentUser.OpenSubKey(
               @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            return key?.GetValue("AppsUseLightTheme") is 0;
         }
         catch
         {
            return false;
         }
      }

      public void Load()
      {
         this.Load(SettingsFilePath);
      }

      public void Save()
      {
         var path = SettingsFilePath;
         try
         {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var data = new SettingsData
               {
                  Width = this.width,
                  Height = this.height,
                  Magnification = this.magnification,
                  GridSize = this.gridSize,
                  GridStyle = this.gridStyle,
                  GridOpacity = this.gridOpacity,
                  Scaling = (int)this.scalingMode,
                  PrecisionSpeed = this.precisionSpeed,
                  InfoShowHex = this.infoShowHex,
                  InfoShowRgb = this.infoShowRgb,
                  InfoShowHsl = this.infoShowHsl,
                  InfoShow12Bit = this.infoShow12Bit,
                  InfoShowWeb = this.infoShowWeb,
                  InfoShowMouse = this.infoShowMouse,
                  InfoShowSize = this.infoShowSize,
                  InfoShowZoom = this.infoShowZoom,
                  Theme = this.theme,
                  Themes = this.themes,
               };
            File.WriteAllText(path, JsonSerializer.Serialize(data, jsonOptions));
         }
         catch (Exception ex)
         {
            Debug.WriteLine($"Failed to save settings: {ex.Message}");
         }
      }

      internal static void ResetForTesting()
      {
         instance?.saveTimer.Stop();
         instance?.saveTimer.Dispose();
         instance = null;
      }

      internal void Load(string path)
      {
         if (!File.Exists(path))
         {
            return;
         }

         try
         {
            var data = JsonSerializer.Deserialize<SettingsData>(File.ReadAllText(path), jsonOptions);
            if (data == null)
            {
               return;
            }

            this.Width = data.Width;
            this.Height = data.Height;
            this.Magnification = data.Magnification;
            this.GridSize = data.GridSize;
            this.GridStyle = data.GridStyle;
            this.GridOpacity = data.GridOpacity;
            this.Scaling = (ScalingModeOption)data.Scaling;
            this.PrecisionSpeed = data.PrecisionSpeed;
            this.infoShowHex = data.InfoShowHex;
            this.infoShowRgb = data.InfoShowRgb;
            this.infoShowHsl = data.InfoShowHsl;
            this.infoShow12Bit = data.InfoShow12Bit;
            this.infoShowWeb = data.InfoShowWeb;
            this.infoShowMouse = data.InfoShowMouse;
            this.infoShowSize = data.InfoShowSize;
            this.infoShowZoom = data.InfoShowZoom;

            if (data.Themes != null)
            {
               this.Themes = data.Themes;
            }

            this.Theme = data.Theme;

            Debug.WriteLine($"Settings loaded from {path}");
         }
         catch (Exception ex)
         {
            Debug.WriteLine($"Failed to load settings: {ex.Message}");
         }
      }

      /// <summary>JSON properties absent from an older settings.json deserialize to default
      ///    (Color) (Color.Empty, A=0). Every persisted color is opaque (A=255), so A==0 reliably
      ///    means "never set" -- backfill those from the built-in palette for that theme name.</summary>
      private static ThemePalette FillMissing(ThemePalette loaded, ThemePalette fallback)
      {
         return new ThemePalette
            {
               Inset = loaded.Inset.A == 0 ? fallback.Inset : loaded.Inset,
               Background = loaded.Background.A == 0 ? fallback.Background : loaded.Background,
               Control = loaded.Control.A == 0 ? fallback.Control : loaded.Control,
               Border = loaded.Border.A == 0 ? fallback.Border : loaded.Border,
               AccentSubtle = loaded.AccentSubtle.A == 0 ? fallback.AccentSubtle : loaded.AccentSubtle,
               AccentNormal = loaded.AccentNormal.A == 0 ? fallback.AccentNormal : loaded.AccentNormal,
               AccentStrong = loaded.AccentStrong.A == 0 ? fallback.AccentStrong : loaded.AccentStrong,
               TextSubtle = loaded.TextSubtle.A == 0 ? fallback.TextSubtle : loaded.TextSubtle,
               TextNormal = loaded.TextNormal.A == 0 ? fallback.TextNormal : loaded.TextNormal,
               TextStrong = loaded.TextStrong.A == 0 ? fallback.TextStrong : loaded.TextStrong,
            };
      }

      private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
      {
         this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
      }

      private void SetPersisted<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
      {
         if (EqualityComparer<T>.Default.Equals(field, value))
         {
            return;
         }

         field = value;
         Debug.WriteLine($"{propertyName} = {value}");
         this.OnPropertyChanged(propertyName);
      }

      public static class Defaults
      {
         public const byte MaxGridSize = 16;

         public const short MaxHeight = 400;

         public const byte MaxMagnification = 16;

         public const short MaxWidth = 400;

         public const byte MinGridSize = 1;

         public const short MinHeight = 100;

         public const byte MinMagnification = 2;

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

      private sealed class ColorHexConverter : JsonConverter<Color>
      {
         public override Color Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
         {
            return ColorTranslator.FromHtml(reader.GetString() ?? "#000000");
         }

         public override void Write(Utf8JsonWriter writer, Color value, JsonSerializerOptions options)
         {
            writer.WriteStringValue($"#{value.R:X2}{value.G:X2}{value.B:X2}");
         }
      }

      private class SettingsData
      {
         public int GridOpacity { get; init; } = 20;

         public byte GridSize { get; init; } = 4;

         public int GridStyle { get; init; } = (int)GridStyleOption.Dash;

         public short Height { get; init; } = 160;

         public bool InfoShow12Bit { get; init; } = true;

         public bool InfoShowHex { get; init; } = true;

         public bool InfoShowHsl { get; init; } = true;

         public bool InfoShowMouse { get; init; } = true;

         public bool InfoShowRgb { get; init; } = true;

         public bool InfoShowSize { get; init; } = true;

         public bool InfoShowWeb { get; init; } = true;

         public bool InfoShowZoom { get; init; } = true;

         public byte Magnification { get; init; } = 4;

         public int PrecisionSpeed { get; init; } = 45;

         public int Scaling { get; init; } = (int)ScalingModeOption.NearestNeighbor;

         public string Theme { get; init; } = "system";

         public Dictionary<string, ThemePalette>? Themes { get; init; }

         public short Width { get; init; } = 150;
      }
   }
}
