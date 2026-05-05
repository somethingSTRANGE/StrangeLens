// -------------------------------------------------------------------------------------
// <copyright file="SettingsForm.cs" company="Strange Entertainment LLC">
//   Copyright 2004-2023 Strange Entertainment LLC. All rights reserved.
// </copyright>
// -------------------------------------------------------------------------------------

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Lens
{
   public partial class SettingsForm : Form
   {
      private const int  HotkeyToggle    = 1;
      private const uint ModCtrlAltShift = 0x0007; // MOD_ALT | MOD_CONTROL | MOD_SHIFT

      [DllImport("user32.dll")] private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
      [DllImport("user32.dll")] private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
      [DllImport("dwmapi.dll")] private static extern int  DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

      private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

      private LensForm activeLens;
      private int      clickCount;
      private System.Windows.Forms.Timer clickTimer;
      private bool     shouldExitApplication;

      // ── Nord palette ──────────────────────────────────────────────────────────────────

      private static readonly Color DarkBg      = Color.FromArgb(0x2E, 0x34, 0x40); // Nord0
      private static readonly Color DarkControl = Color.FromArgb(0x43, 0x4C, 0x5E); // Nord2
      private static readonly Color DarkBorder  = Color.FromArgb(0x4C, 0x56, 0x6A); // Nord3
      private static readonly Color DarkText    = Color.FromArgb(0xEC, 0xEF, 0xF4); // Nord6
      private static readonly Color DarkMuted   = Color.FromArgb(0xD8, 0xDE, 0xE9); // Nord4
      private static readonly Color DarkAccent  = Color.FromArgb(0x88, 0xC0, 0xD0); // Nord8

      private static readonly Color LightBg      = Color.FromArgb(0xFA, 0xFA, 0xFA);
      private static readonly Color LightControl = Color.White;
      private static readonly Color LightBorder  = Color.FromArgb(0xCC, 0xCC, 0xCC);
      private static readonly Color LightText    = Color.FromArgb(0x2E, 0x34, 0x40); // Nord0 as dark text
      private static readonly Color LightMuted   = Color.FromArgb(0x4C, 0x56, 0x6A); // Nord3
      private static readonly Color LightAccent  = Color.FromArgb(0x5E, 0x81, 0xAC); // Nord10

      private static bool IsDarkMode()
      {
         var debug = Lens.Instance.DebugTheme;
         if (debug == "dark")  return true;
         if (debug == "light") return false;
         try
         {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
               @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            return key?.GetValue("AppsUseLightTheme") is int v && v == 0;
         }
         catch { return false; }
      }

      // ── Layout fields ─────────────────────────────────────────────────────────────────

      private ComboBox      valueGridStyle;
      private ComboBox      valueGridSize;
      private Button        valueGridColor;
      private ComboBox      valueMagnification;
      private ComboBox      valueScalingMode;
      private ComboBox      valueSpeedFactor;
      private CheckBox      valueInfoShowHex;
      private CheckBox      valueInfoShowRgb;
      private CheckBox      valueInfoShowHsl;
      private CheckBox      valueInfoShow12Bit;
      private CheckBox      valueInfoShowWeb;
      private CheckBox      valueInfoShowMouse;
      private CheckBox      valueInfoShowSize;
      private CheckBox      valueInfoShowZoom;

      private const int FormW       = 320;
      private const int RowH        = 27;
      private const int PadX        = 16;
      private const int PadY        = 16;
      private const int SubGroupGap = 10;
      private const int SectionGap  = 20;
      private const int ComboBoxW   = 132;
      private const int LabelIndent = 12;

      // ── Constructor ───────────────────────────────────────────────────────────────────

      public SettingsForm()
      {
         Debug.WriteLine("SETTINGS FORM CONSTRUCTOR - before InitializeComponent");
         this.InitializeComponent();
         Debug.WriteLine("SETTINGS FORM CONSTRUCTOR - after InitializeComponent");

         this.clickTimer      = new System.Windows.Forms.Timer();
         this.clickTimer.Tick += this.ClickTimer_Elapsed;

         this.BuildLayout();

         var menuItemOpen = new ToolStripMenuItem { Text = "Show" };
         var menuItemSettings = new ToolStripMenuItem
            { Text = "&Settings...", ShortcutKeys = Keys.Control | Keys.Shift | Keys.S, ShowShortcutKeys = true };
         var menuItemSeparator = new ToolStripSeparator();
         var menuItemExit = new ToolStripMenuItem { Text = "E&xit" };

         menuItemExit.Click     += this.menuItemExit_Click;
         menuItemSettings.Click += this.menuItemSettings_Click;
         menuItemOpen.Click     += this.menuItemOpen_Click;

         this.contextMenu.Items.AddRange(new ToolStripItem[]
            { menuItemOpen, menuItemSettings, menuItemSeparator, menuItemExit });

         this.notifyIcon.Icon            = this.Icon;
         this.notifyIcon.ContextMenuStrip = this.contextMenu;

         // WinForms creates the Win32 handle lazily on first Show(). Force it now so
         // OnHandleCreated fires immediately and RegisterHotKey works from the start.
         this.CreateHandle();
      }

      // ── BuildLayout ───────────────────────────────────────────────────────────────────

      private void BuildLayout()
      {
         bool dark   = IsDarkMode();
         var  bg     = dark ? DarkBg      : LightBg;
         var  ctrlBg = dark ? DarkControl : LightControl;
         var  border = dark ? DarkBorder  : LightBorder;
         var  text   = dark ? DarkText    : LightText;
         var  muted  = dark ? DarkMuted   : LightMuted;
         var  accent = dark ? DarkAccent  : LightAccent;
         var  ds     = Lens.Instance;

         this.BackColor = bg;
         int y = PadY;

         // ── Lens ──────────────────────────────────────────────────────────────────────
         y = SectionHeader("LENS", y, accent, border);
         y = SubHeader("Grid", y, muted);

         valueGridStyle = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = ComboBoxW };
         valueGridStyle.Items.AddRange(new object[]
            { "None", "Solid", "Dash", "Dot", "Dash, Dot", "Dash, Dot, Dot" });
         valueGridStyle.SelectedIndex = ds.GridStyle;
         valueGridStyle.SelectedIndexChanged += (_, _) => {
            if (valueGridStyle.SelectedIndex >= 0)
               ds.GridStyle = valueGridStyle.SelectedIndex;
         };
         y = Row("Style", valueGridStyle, LabelIndent, y, text);

         valueGridSize = ByteRangeComboBox(Lens.Defaults.MinGridSize, Lens.Defaults.MaxGridSize,
            i => i == 1 ? "1 pixel" : $"{i} pixels");
         valueGridSize.SelectedIndex = ds.GridSize - Lens.Defaults.MinGridSize;
         valueGridSize.SelectedIndexChanged += (_, _) => {
            if (valueGridSize.SelectedIndex >= 0)
               ds.GridSize = (byte)(valueGridSize.SelectedIndex + Lens.Defaults.MinGridSize);
         };
         y = Row("Size", valueGridSize, LabelIndent, y, text);

         valueGridColor = new Button
         {
            Width                  = 24,
            Height                 = 24,
            FlatStyle              = FlatStyle.Flat,
            BackColor              = ds.GridColor,
            UseVisualStyleBackColor = false
         };
         valueGridColor.FlatAppearance.BorderColor = border;
         valueGridColor.DataBindings.Add(nameof(valueGridColor.BackColor), ds,
            nameof(ds.GridColor), false, DataSourceUpdateMode.OnPropertyChanged);
         valueGridColor.Click += this.button1_Click;
         y = Row("Color", valueGridColor, LabelIndent, y, text);

         y += SubGroupGap;
         y = SubHeader("Magnification", y, muted);

         valueMagnification = ByteRangeComboBox(Lens.Defaults.MinMagnification, Lens.Defaults.MaxMagnification,
            i => $"×{i}");
         valueMagnification.SelectedIndex = ds.Magnification - Lens.Defaults.MinMagnification;
         valueMagnification.SelectedIndexChanged += (_, _) => {
            if (valueMagnification.SelectedIndex >= 0)
               ds.Magnification = (byte)(valueMagnification.SelectedIndex + Lens.Defaults.MinMagnification);
         };
         y = Row("Power level", valueMagnification, LabelIndent, y, text);

         valueScalingMode = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = ComboBoxW };
         valueScalingMode.Items.AddRange(new object[]
         {
            "Nearest neighbor", "Bilinear", "High quality bilinear", "Bicubic", "High quality bicubic"
         });
         valueScalingMode.SelectedIndex = (int)ds.Scaling;
         valueScalingMode.SelectedIndexChanged += (_, _) => {
            if (valueScalingMode.SelectedIndex >= 0)
               ds.Scaling = (ScalingMode)valueScalingMode.SelectedIndex;
         };
         y = Row("Scaling", valueScalingMode, LabelIndent, y, text);

         valueSpeedFactor = ByteRangeComboBox(Lens.Defaults.MinSpeedFactor, Lens.Defaults.MaxSpeedFactor,
            i => i.ToString());
         valueSpeedFactor.SelectedIndex = ds.SpeedFactor - Lens.Defaults.MinSpeedFactor;
         valueSpeedFactor.SelectedIndexChanged += (_, _) => {
            if (valueSpeedFactor.SelectedIndex >= 0)
               ds.SpeedFactor = (byte)(valueSpeedFactor.SelectedIndex + Lens.Defaults.MinSpeedFactor);
         };
         y = Row("Speed factor", valueSpeedFactor, LabelIndent, y, text);

         // ── Info ──────────────────────────────────────────────────────────────────────
         y += SectionGap;
         y = SectionHeader("INFO", y, accent, border);

         valueInfoShowHex = InfoToggle(ds, nameof(ds.InfoShowHex), bg);
         y = Row("Show hex color value", valueInfoShowHex, 0, y, text);

         valueInfoShowRgb = InfoToggle(ds, nameof(ds.InfoShowRgb), bg);
         y = Row("Show RGB color value", valueInfoShowRgb, 0, y, text);

         valueInfoShowHsl = InfoToggle(ds, nameof(ds.InfoShowHsl), bg);
         y = Row("Show HSL color value", valueInfoShowHsl, 0, y, text);

         y += SubGroupGap;
         valueInfoShow12Bit = InfoToggle(ds, nameof(ds.InfoShow12Bit), bg);
         y = Row("Show 12-bit color conversion", valueInfoShow12Bit, 0, y, text);

         valueInfoShowWeb = InfoToggle(ds, nameof(ds.InfoShowWeb), bg);
         y = Row("Show web safe color conversion", valueInfoShowWeb, 0, y, text);

         y += SubGroupGap;
         valueInfoShowMouse = InfoToggle(ds, nameof(ds.InfoShowMouse), bg);
         y = Row("Show mouse position", valueInfoShowMouse, 0, y, text);

         valueInfoShowSize = InfoToggle(ds, nameof(ds.InfoShowSize), bg);
         y = Row("Show lens size", valueInfoShowSize, 0, y, text);

         valueInfoShowZoom = InfoToggle(ds, nameof(ds.InfoShowZoom), bg);
         y = Row("Show zoom level", valueInfoShowZoom, 0, y, text);

         this.ClientSize = new Size(FormW, y + PadY);

         ds.PropertyChanged += this.OnSettingChanged;
      }

      private void OnSettingChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
      {
         var ds = Lens.Instance;
         switch (e.PropertyName)
         {
            case nameof(ds.GridStyle):
               valueGridStyle.SelectedIndex = ds.GridStyle;
               break;
            case nameof(ds.GridSize):
               valueGridSize.SelectedIndex = ds.GridSize - Lens.Defaults.MinGridSize;
               break;
            case nameof(ds.Magnification):
               valueMagnification.SelectedIndex = ds.Magnification - Lens.Defaults.MinMagnification;
               break;
            case nameof(ds.SpeedFactor):
               valueSpeedFactor.SelectedIndex = ds.SpeedFactor - Lens.Defaults.MinSpeedFactor;
               break;
            case nameof(ds.Scaling):
               valueScalingMode.SelectedIndex = (int)ds.Scaling;
               break;
         }
      }

      private static ComboBox ByteRangeComboBox(byte min, byte max, Func<int, string> label)
      {
         var cb = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = ComboBoxW };
         for (int i = min; i <= max; i++)
            cb.Items.Add(label(i));
         return cb;
      }

      private CheckBox InfoToggle(Lens ds, string propertyName, Color bg)
      {
         var cb = new CheckBox
            { Width = 20, Height = 20, Text = "", BackColor = bg, CheckAlign = ContentAlignment.MiddleCenter };
         cb.DataBindings.Add(nameof(cb.Checked), ds, propertyName, false, DataSourceUpdateMode.OnPropertyChanged);
         return cb;
      }

      private int SectionHeader(string text, int y, Color accent, Color border)
      {
         const int HeaderH = 28;
         const int SepH    = 1;
         const int After   = 8;

         this.Controls.Add(new Label
         {
            Text      = text,
            Location  = new Point(PadX, y),
            Size      = new Size(FormW - PadX * 2, HeaderH),
            ForeColor = accent,
            BackColor = Color.Transparent,
            Font      = new Font(this.Font, FontStyle.Bold),
            TextAlign = ContentAlignment.BottomLeft
         });
         y += HeaderH;

         this.Controls.Add(new Panel
         {
            Location  = new Point(PadX, y),
            Size      = new Size(FormW - PadX * 2, SepH),
            BackColor = border
         });

         return y + SepH + After;
      }

      private int SubHeader(string text, int y, Color color)
      {
         const int SubH  = 24;
         const int After = 2;

         this.Controls.Add(new Label
         {
            Text      = text,
            Font      = new Font(this.Font, FontStyle.Bold),
            Location  = new Point(PadX, y),
            Size      = new Size(FormW - PadX * 2, SubH),
            ForeColor = color,
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.MiddleLeft
         });

         return y + SubH + After;
      }

      private int Row(string labelText, Control ctrl, int xOffset, int y, Color labelColor)
      {
         this.Controls.Add(new Label
         {
            Text      = labelText,
            Location  = new Point(PadX + xOffset, y),
            Size      = new Size(FormW - PadX - ctrl.Width - PadX - xOffset - PadX, RowH),
            ForeColor = labelColor,
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.MiddleLeft
         });

         ctrl.Top  = y + (RowH - ctrl.Height) / 2;
         ctrl.Left = FormW - PadX - ctrl.Width;
         this.Controls.Add(ctrl);

         return y + RowH;
      }

      // ── Existing event handlers ───────────────────────────────────────────────────────

      private void ClickTimer_Elapsed(object sender, EventArgs e)
      {
         this.clickTimer.Stop();

         if (this.clickCount >= 2)
         {
            if (this.WindowState == FormWindowState.Minimized) this.WindowState = FormWindowState.Normal;
            this.OpenSettings();
         }
         else
         {
            this.ToggleLens();
         }

         this.clickCount = 0;
      }

      private void OpenSettings()
      {
         Console.WriteLine("Open Settings");
         this.Show();
         this.Activate();
      }

      protected override CreateParams CreateParams
      {
         get
         {
            var cp = base.CreateParams;
            cp.ExStyle |= 0x02000000; // WS_EX_COMPOSITED — buffers the whole window before presenting, prevents white flash
            return cp;
         }
      }

      protected override void OnHandleCreated(EventArgs e)
      {
         base.OnHandleCreated(e);

         // Force dark title bar (focused + unfocused) — SetColorMode alone doesn't set the DWM attribute per-window.
         if (IsDarkMode())
         {
            int dark = 1;
            DwmSetWindowAttribute(this.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref dark, sizeof(int));
         }

         if (!RegisterHotKey(this.Handle, HotkeyToggle, ModCtrlAltShift, (uint)Keys.Z))
            Debug.WriteLine($"RegisterHotKey failed: error {Marshal.GetLastWin32Error()}");
      }

      protected override void OnFormClosed(FormClosedEventArgs e)
      {
         base.OnFormClosed(e);
         UnregisterHotKey(this.Handle, HotkeyToggle);
      }

      protected override void WndProc(ref Message m)
      {
         const int WmHotkey    = 0x0312;
         const int WmNcActivate = 0x0086;

         // Re-apply dark title bar on every focus change — WM_NCACTIVATE fires when Windows
         // redraws the non-client area, and something (SetColorMode/WinForms internals) can
         // reset the DWM attribute before we see the message.
         if (m.Msg == WmNcActivate && IsDarkMode())
         {
            int dark = 1;
            DwmSetWindowAttribute(this.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref dark, sizeof(int));
         }

         if (m.Msg == WmHotkey && m.WParam.ToInt32() == HotkeyToggle)
            this.ToggleLens();
         base.WndProc(ref m);
      }

      private void ToggleLens()
      {
         if (this.activeLens != null)
         {
            this.activeLens.Close();
            return;
         }
         var lensForm = new LensForm { TargetLocation = Cursor.Position };
         lensForm.FormClosed += (s, e) => this.activeLens = null;
         this.activeLens = lensForm;
         lensForm.Show();
         lensForm.Activate();
      }

      private void button1_Click(object sender, EventArgs e)
      {
         this.colorGrid.Color = this.valueGridColor.BackColor;
         if (this.colorGrid.ShowDialog() == DialogResult.OK)
            this.valueGridColor.BackColor = this.colorGrid.Color;
      }

      private void menuItemExit_Click(object sender, EventArgs e)
      {
         this.shouldExitApplication = true;
         this.Close();
         Application.Exit();
      }

      private void menuItemOpen_Click(object sender, EventArgs e)     => this.ToggleLens();
      private void menuItemSettings_Click(object sender, EventArgs e) => this.OpenSettings();

      private void notifyIcon_MouseClick(object sender, MouseEventArgs e)
      {
         if (e.Button != MouseButtons.Left) return;

         this.clickCount++;
         this.clickTimer.Stop();
         this.clickTimer.Interval = SystemInformation.DoubleClickTime;
         this.clickTimer.Start();
      }

      private void SettingsForm_FormClosing(object sender, FormClosingEventArgs e)
      {
         if (!this.shouldExitApplication)
            this.CloseToSystemTray(e);
         else
            Console.WriteLine("Exiting Application");
      }

      private void CloseToSystemTray(CancelEventArgs e)
      {
         Console.WriteLine("Closing to System Tray");
         e.Cancel = true;
         this.Hide();
      }
   }
}
