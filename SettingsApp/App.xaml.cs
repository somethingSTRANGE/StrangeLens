// -------------------------------------------------------------------------------------
// <copyright file="App.xaml.cs">
//   Copyright (c) 2026
//   Licensed under the MIT License. See LICENSE file in the project root.
// </copyright>
// -------------------------------------------------------------------------------------

namespace StrangeLens.SettingsApp;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

public partial class App
{
   private Window? window;

   public App()
   {
      this.InitializeComponent();
   }

   protected override void OnLaunched(LaunchActivatedEventArgs args)
   {
      // Application.Resources isn't safe to touch from the constructor -- WinRT throws
      // COMException 0x8000FFFF (E_UNEXPECTED) calling get_Resources() that early, before the
      // rest of the app's native/composition setup has settled. OnLaunched is the first point
      // it's known-safe.
      this.ApplyAccentColorOverrides();

      if (LaunchArgs.IsAboutMode)
      {
         this.window = new AboutWindow();
      }
      else
      {
         Lens.Instance.Load();
         Lens.Instance.StartWatchingForExternalChanges();
         this.window = new MainWindow();
         this.window.Closed += (_, _) => Lens.Instance.Save();
      }

      this.window.Activate();
   }

   /// <summary>Fluent 2 generates a whole ramp of lighter/adjusted shades from the OS accent
   ///    color for button/toggle-style controls (ToggleSwitch's "on" track, AccentButtonStyle's
   ///    background, etc.) rather than using the raw accent color directly.
   ///    AccentFillColorSelectedTextBackgroundBrush is the one resource that matches the raw OS
   ///    accent color exactly. It's read here as the single base color, since Fluent 2's own
   ///    default ramp renders measurably lighter and less saturated than the real OS accent.
   ///    Hover, pressed, and border shades are then computed from that base via
   ///    <see cref="ExtensionMethods.AdjustLightness"/> (an HSL-lightness tint/shade) rather
   ///    than reused from WinUI3's own ramp. WinUI3's Default/Secondary/Tertiary triad is
   ///    actually one shared color with Opacity progressively reduced per state -- translucent,
   ///    not a solid shade -- while AdjustLightness produces fully opaque solid colors.
   ///    Foreground/stroke/knob colors instead use the app's own
   ///    TextFillColorPrimary/SecondaryBrush, for contrast against the accent fill rather than
   ///    a shade of it.</summary>
   private void ApplyAccentColorOverrides()
   {
      var textAccentPressed = ((SolidColorBrush)this.Resources["TextFillColorSecondaryBrush"]).Color;
      var textAccentNormal = ((SolidColorBrush)this.Resources["TextFillColorPrimaryBrush"]).Color;

      var colorAccent = ((SolidColorBrush)this.Resources["AccentFillColorSelectedTextBackgroundBrush"]).Color;
      var colorAccentBorder = colorAccent.AdjustLightness(0.05);
      var colorAccentHover = colorAccent.AdjustLightness(0.15);
      var colorAccentHoverBorder = colorAccent.AdjustLightness(0.2);
      var colorAccentPressed = colorAccentHover.AdjustLightness(-0.4);
      var colorAccentPressedBorder = colorAccentHover.AdjustLightness(-0.3);

      // ToggleSwitch
      this.Resources["ToggleSwitchFillOn"] = new SolidColorBrush(colorAccent);
      this.Resources["ToggleSwitchFillOnPointerOver"] = new SolidColorBrush(colorAccentHover);
      this.Resources["ToggleSwitchFillOnPressed"] = new SolidColorBrush(colorAccentPressed);
      this.Resources["ToggleSwitchFillOnDisabled"] = new SolidColorBrush(colorAccent);

      this.Resources["ToggleSwitchStrokeOn"] = new SolidColorBrush(textAccentNormal);
      this.Resources["ToggleSwitchStrokeOnPointerOver"] = new SolidColorBrush(textAccentNormal);
      this.Resources["ToggleSwitchStrokeOnPressed"] = new SolidColorBrush(textAccentNormal);

      // WinUI3's default dark-theme ToggleSwitchKnobFillOn is a hardcoded #000000 (not derived
      // from the accent color at all), which flips the knob to solid black once toggled on. The
      // classic Windows 10 Settings look keeps the knob a consistent near-white instead, so
      // Pressed is the only state that dims it.
      this.Resources["ToggleSwitchKnobFillOn"] = new SolidColorBrush(textAccentNormal);
      this.Resources["ToggleSwitchKnobFillOnPointerOver"] = new SolidColorBrush(textAccentNormal);
      this.Resources["ToggleSwitchKnobFillOnPressed"] = new SolidColorBrush(textAccentPressed);

      // Accent button
      this.Resources["AccentButtonBackground"] = new SolidColorBrush(colorAccent);
      this.Resources["AccentButtonBackgroundPointerOver"] = new SolidColorBrush(colorAccentHover);
      this.Resources["AccentButtonBackgroundPressed"] = new SolidColorBrush(colorAccentPressed);

      this.Resources["AccentButtonBorderBrush"] = new SolidColorBrush(colorAccentBorder);
      this.Resources["AccentButtonBorderBrushPointerOver"] = new SolidColorBrush(colorAccentHoverBorder);
      this.Resources["AccentButtonBorderBrushPressed"] = new SolidColorBrush(colorAccentPressedBorder);

      this.Resources["AccentButtonForeground"] = new SolidColorBrush(textAccentNormal);
      this.Resources["AccentButtonForegroundPointerOver"] = new SolidColorBrush(textAccentNormal);
      this.Resources["AccentButtonForegroundPressed"] = new SolidColorBrush(textAccentPressed);
   }
}
