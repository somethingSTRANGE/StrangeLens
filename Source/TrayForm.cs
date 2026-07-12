// -------------------------------------------------------------------------------------
// <copyright file="TrayForm.cs">
//   Copyright (c) 2026
//   Licensed under the MIT License. See LICENSE file in the project root.
// </copyright>
// -------------------------------------------------------------------------------------

namespace StrangeLens
{
   using System;
   using System.ComponentModel;
   using System.Diagnostics;
   using System.Diagnostics.CodeAnalysis;
   using System.Drawing;
   using System.Drawing.Drawing2D;
   using System.IO;
   using System.Runtime.InteropServices;
   using System.Windows.Forms;

   using Microsoft.Win32;

   using static NativeMethods;

   public sealed partial class TrayForm : Form
   {
      private const int Hotkey12Bit = 6;

      private const int HotkeyHex = 2;

      private const int HotkeyHsl = 4;

      private const int HotkeyMeasure = 7;

      private const int HotkeyRgb = 3;

      private const int HotkeyToggle = 1;

      private const int HotkeyWeb = 5;

      private const uint ModCtrlAltShift = MOD_ALT | MOD_CONTROL | MOD_SHIFT;

      private readonly Timer clickTimer;

      private LensForm? activeLens;

      private int clickCount;

      private bool shouldExitApplication;

      public TrayForm()
      {
         this.InitializeComponent();

         this.clickTimer = new Timer();
         this.clickTimer.Tick += this.ClickTimer_Elapsed;

         this.notifyIcon.Text = Application.ProductName;

         var miStartWithWindows = new ToolStripMenuItem("Start with Windows")
            {
               CheckOnClick = true,
               Checked = StartWithWindowsEnabled(),
            };
         miStartWithWindows.Click += (_, _) => SetStartWithWindows(miStartWithWindows.Checked);

         this.contextMenu.Items.AddRange(
            new ToolStripMenuItem("Toggle Lens", null, this.menuItemOpen_Click)
               {
                  ShortcutKeyDisplayString = "Ctrl+Alt+Shift+Z",
                  ShowShortcutKeys = true,
               },
            new ToolStripSeparator(),
            new ToolStripMenuItem("&Settings...", null, this.menuItemSettings_Click),
            miStartWithWindows,
            new ToolStripSeparator(),
            new ToolStripMenuItem("&About...", null, this.menuItemAbout_Click),
            new ToolStripSeparator(),
            new ToolStripMenuItem("E&xit", null, this.menuItemExit_Click));

         this.notifyIcon.Icon = this.Icon;
         this.notifyIcon.ContextMenuStrip = this.contextMenu;

         // WinForms creates the Win32 handle lazily on first Show(). Force it now so
         // OnHandleCreated fires immediately and RegisterHotKey works from the start.
         this.CreateHandle();

         // Pre-warm child control handles on the first message pump tick. The public
         // CreateControl() skips hidden controls (checks the parent Visible chain), so the
         // only way to force creation is a real Show()/Hide() cycle. Opacity=0 keeps it
         // invisible. This prevents the ~950ms UI freeze on the first manual Settings open.
         this.BeginInvoke(() =>
            {
               this.Opacity = 0;
               this.Show();
               this.Hide();
               this.Opacity = 1;
            });
      }

      protected override CreateParams CreateParams
      {
         get
         {
            var cp = base.CreateParams;
            cp.ExStyle |= WS_EX_COMPOSITED;
            return cp;
         }
      }

      /// <summary>Draws a transparent black rectangle, that's useful for visualizing Control
      ///    placement and alignment.</summary>
      /// <param name="graphics">The <see cref="Graphics"/> handle.</param>
      /// <param name="rect">The <see cref="Rectangle"/> to draw.</param>
      /// <example><c>TrayForm.DrawDebugRect(e.Graphics, this.ClientRectangle);</c></example>
      public static void DrawDebugRect(Graphics graphics, Rectangle rect)
      {
         var smoothingMode = graphics.SmoothingMode;
         graphics.SmoothingMode = SmoothingMode.None;
         using var clientRectBrush = new SolidBrush(Color.FromArgb(0x33, Color.Black));
         graphics.FillRectangle(clientRectBrush, rect);
         graphics.SmoothingMode = smoothingMode;
      }

      protected override void OnFormClosed(FormClosedEventArgs e)
      {
         base.OnFormClosed(e);
         UnregisterHotKey(this.Handle, HotkeyToggle);
         UnregisterHotKey(this.Handle, HotkeyHex);
         UnregisterHotKey(this.Handle, HotkeyRgb);
         UnregisterHotKey(this.Handle, HotkeyHsl);
         UnregisterHotKey(this.Handle, HotkeyWeb);
         UnregisterHotKey(this.Handle, Hotkey12Bit);
         UnregisterHotKey(this.Handle, HotkeyMeasure);
      }

      protected override void OnHandleCreated(EventArgs e)
      {
         base.OnHandleCreated(e);

         // Force dark title bar (focused + unfocused) -- SetColorMode alone doesn't set the DWM attribute per-window.
         if (IsDarkMode())
         {
            var dark = 1;
            DwmSetWindowAttribute(this.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref dark, sizeof(int));
         }

         void TryRegister(int id, Keys key)
         {
            if (!RegisterHotKey(this.Handle, id, ModCtrlAltShift, (uint)key))
            {
               AppLog.Error(
                  $"RegisterHotKey Ctrl+Alt+Shift+{key} failed: error {Marshal.GetLastWin32Error()}");
            }
         }

         TryRegister(HotkeyToggle, Keys.Z);
         TryRegister(HotkeyHex, Keys.X);
         TryRegister(HotkeyRgb, Keys.R);
         TryRegister(HotkeyHsl, Keys.S);
         TryRegister(HotkeyWeb, Keys.W);
         TryRegister(Hotkey12Bit, Keys.D1);
         TryRegister(HotkeyMeasure, Keys.Q);
      }

      [SuppressMessage("ReSharper", "CognitiveComplexity")]
      protected override void WndProc(ref Message m)
      {
         // Re-apply dark title bar on every focus change -- WM_NCACTIVATE fires when Windows
         // redraws the non-client area, and something (SetColorMode/WinForms internals) can
         // reset the DWM attribute before we see the message.
         if ((m.Msg == WM_NCACTIVATE) && IsDarkMode())
         {
            var dark = 1;
            DwmSetWindowAttribute(this.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref dark, sizeof(int));
         }

         if (m.Msg == WM_HOTKEY)
         {
            switch (m.WParam.ToInt32())
            {
               case HotkeyToggle: this.ToggleLens(); break;
               case HotkeyHex: this.activeLens?.CopyToClipboardColorHex(); break;
               case HotkeyRgb: this.activeLens?.CopyToClipboardColorRGB(); break;
               case HotkeyHsl: this.activeLens?.CopyToClipboardColorHSL(); break;
               case HotkeyWeb: this.activeLens?.CopyToClipboardColorWeb(); break;
               case Hotkey12Bit: this.activeLens?.CopyToClipboardColor12Bit(); break;
               case HotkeyMeasure: this.activeLens?.ToggleMeasure(); break;
            }
         }

         base.WndProc(ref m);
      }

      private static bool IsDarkMode()
      {
         return Lens.IsOsDarkMode();
      }

      private static void SetStartWithWindows(bool enable)
      {
         using var key = Registry.CurrentUser.OpenSubKey(
            @"Software\Microsoft\Windows\CurrentVersion\Run",
            writable: true);
         if (enable)
         {
            key?.SetValue("StrangeLens", Application.ExecutablePath);
         }
         else
         {
            key?.DeleteValue("StrangeLens", throwOnMissingValue: false);
         }
      }

      private static bool StartWithWindowsEnabled()
      {
         using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run");
         return key?.GetValue("StrangeLens") != null;
      }

      private void ClickTimer_Elapsed(object? sender, EventArgs e)
      {
         this.clickTimer.Stop();

         if (this.clickCount >= 2)
         {
            if (this.WindowState == FormWindowState.Minimized)
            {
               this.WindowState = FormWindowState.Normal;
            }

            this.LaunchSettingsAppWindow();
         }
         else
         {
            this.ToggleLens();
         }

         this.clickCount = 0;
      }

      private void CloseToSystemTray(CancelEventArgs e)
      {
         e.Cancel = true;
         this.Hide();
      }

      private void LaunchSettingsAppWindow(string? arguments = null)
      {
         // TODO: replace the Debug-config-only relative path once a Release/packaging
         // pipeline exists (see feature/winui3-migration plan notes).
         var settingsExe = Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "SettingsApp",
            "bin",
            "x64",
            "Debug",
            "net10.0-windows10.0.19041.0",
            "StrangeLens.Settings.exe");

         var startInfo = new ProcessStartInfo(Path.GetFullPath(settingsExe))
            {
               UseShellExecute = true,
            };
         if (arguments != null)
         {
            startInfo.Arguments = arguments;
         }

         var process = Process.Start(startInfo);

         // Ties the Settings/About process's lifetime to this one, so it doesn't linger
         // as an orphaned window (with no tray icon left to reopen it from) after this
         // process exits -- by any means, not just the normal Tray -> Exit.
         if (process != null)
         {
            ChildProcessTracker.Add(process);
         }
      }

      private void menuItemAbout_Click(object? sender, EventArgs e)
      {
         this.LaunchSettingsAppWindow("--about");
      }

      private void menuItemExit_Click(object? sender, EventArgs e)
      {
         this.shouldExitApplication = true;
         this.Close();
         Application.Exit();
      }

      private void menuItemOpen_Click(object? sender, EventArgs e)
      {
         this.ToggleLens();
      }

      private void menuItemSettings_Click(object? sender, EventArgs e)
      {
         this.LaunchSettingsAppWindow();
      }

      private void notifyIcon_MouseClick(object? sender, MouseEventArgs e)
      {
         if (e.Button != MouseButtons.Left)
         {
            return;
         }

         this.clickCount++;
         this.clickTimer.Stop();
         this.clickTimer.Interval = SystemInformation.DoubleClickTime;
         this.clickTimer.Start();
      }

      private void ToggleLens()
      {
         if (this.activeLens != null)
         {
            this.activeLens.Close();
            return;
         }

         LensForm lensForm;
         try
         {
            lensForm = new LensForm();
         }
         catch (Exception ex)
         {
            AppLog.Error("ToggleLens: failed to create LensForm: " + ex.Message);
            return;
         }

         lensForm.FormClosed += (_, _) => this.activeLens = null;
         this.activeLens = lensForm;
         lensForm.Show();
         lensForm.Activate();
      }

      private void TrayForm_FormClosing(object? sender, FormClosingEventArgs e)
      {
         if (!this.shouldExitApplication)
         {
            this.CloseToSystemTray(e);
         }
      }
   }
}
