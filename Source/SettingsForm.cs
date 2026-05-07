// -------------------------------------------------------------------------------------
// <copyright file="SettingsForm.cs" company="Strange Entertainment LLC">
//   Copyright 2004-2023 Strange Entertainment LLC. All rights reserved.
// </copyright>
// -------------------------------------------------------------------------------------

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
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
      private Timer    clickTimer;
      private bool     shouldExitApplication;

      // ── Nord palette ──────────────────────────────────────────────────────────────────

      private static readonly Color DarkBg      = ColorTranslator.FromHtml("#2e3440"); // Nord0
      private static readonly Color DarkControl = ColorTranslator.FromHtml("#434c5e"); // Nord2
      private static readonly Color DarkBorder  = ColorTranslator.FromHtml("#4c566a"); // Nord3
      private static readonly Color DarkMuted   = ColorTranslator.FromHtml("#d8dee9"); // Nord4
      private static readonly Color DarkText    = ColorTranslator.FromHtml("#e5e9f0"); // Nord5
      private static readonly Color DarkAccent  = ColorTranslator.FromHtml("#5e81ac"); //.Saturate(20); // Nord10

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

      private ComboBox      comboBoxLensGridStyle;
      private ComboBox      comboBoxLensGridSize;
      private Button        buttonLensGridColor;
      private ComboBox      comboBoxLensMagnification;
      private ComboBox      comboBoxLensScalingMode;
      private ComboBox      comboBoxLensSpeedFactor;
      private CheckBox      checkBoxInfoShowHex;
      private CheckBox      checkBoxInfoShowRgb;
      private CheckBox      checkBoxInfoShowHsl;
      private CheckBox      checkBoxInfoShow12Bit;
      private CheckBox      checkBoxInfoShowWeb;
      private CheckBox      checkBoxInfoShowMouse;
      private CheckBox      checkBoxInfoShowSize;
      private CheckBox      checkBoxInfoShowZoom;

      private const int FormW       = 320;
      private const int RowH        = 27;
      private const int PadX        = 16;
      private const int PadY        = 16;
      private const int SubGroupGap = 10;
      private const int SectionGap  = 20;
      private const int ComboBoxW   = 132;
      private const int LabelIndent = 12;

      private Color colorCtrlBg;
      private Color colorCtrlBorder;
      private Color colorFocusBorder;

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

         Toggle.Colors.Focus = border;
         Toggle.Colors.Thumb = muted;
         Toggle.Colors.ThumbHover = Color.White;
         Toggle.Colors.TrackActive = accent;
         Toggle.Colors.TrackBase = Color.Black;

         this.BackColor        = bg;
         this.colorCtrlBg      = ctrlBg;
         this.colorCtrlBorder  = border;
         this.colorFocusBorder = accent;
         int y = PadY / 2;

         // ── Lens ──────────────────────────────────────────────────────────────────────
         y = SectionHeader("LENS", y, accent, border);
         y = SubHeader("Grid", y, muted);

         this.comboBoxLensGridStyle = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = ComboBoxW };
         this.comboBoxLensGridStyle.Items.AddRange(new object[]
            { "None", "Solid", "Dash", "Dot", "Dash, Dot", "Dash, Dot, Dot" });
         this.comboBoxLensGridStyle.SelectedIndex = ds.GridStyle;
         this.comboBoxLensGridStyle.SelectedIndexChanged += (_, _) => {
            if (this.comboBoxLensGridStyle.SelectedIndex >= 0)
               ds.GridStyle = this.comboBoxLensGridStyle.SelectedIndex;
         };
         y = this.LayoutRow("Style", this.comboBoxLensGridStyle, LabelIndent, y, text);

         this.comboBoxLensGridSize = ByteRangeComboBox(Lens.Defaults.MinGridSize, Lens.Defaults.MaxGridSize,
            i => i == 1 ? "1 pixel" : $"{i} pixels");
         this.comboBoxLensGridSize.SelectedIndex = ds.GridSize - Lens.Defaults.MinGridSize;
         this.comboBoxLensGridSize.SelectedIndexChanged += (_, _) => {
            if (this.comboBoxLensGridSize.SelectedIndex >= 0)
               ds.GridSize = (byte)(this.comboBoxLensGridSize.SelectedIndex + Lens.Defaults.MinGridSize);
         };
         y = this.LayoutRow("Size", this.comboBoxLensGridSize, LabelIndent, y, text);

         this.buttonLensGridColor = new Button
         {
            Width                  = 24,
            Height                 = 24,
            FlatStyle              = FlatStyle.Flat,
            BackColor              = ds.GridColor,
            UseVisualStyleBackColor = false
         };
         this.buttonLensGridColor.FlatAppearance.BorderColor = border;
         this.buttonLensGridColor.DataBindings.Add(nameof(this.buttonLensGridColor.BackColor), ds,
            nameof(ds.GridColor), false, DataSourceUpdateMode.OnPropertyChanged);
         this.buttonLensGridColor.Click += this.button1_Click;
         y = this.LayoutRow("Color", this.buttonLensGridColor, LabelIndent, y, text);

         y += SubGroupGap;
         y = SubHeader("Magnification", y, muted);

         this.comboBoxLensMagnification = ByteRangeComboBox(Lens.Defaults.MinMagnification, Lens.Defaults.MaxMagnification,
            i => $"×{i}");
         this.comboBoxLensMagnification.SelectedIndex = ds.Magnification - Lens.Defaults.MinMagnification;
         this.comboBoxLensMagnification.SelectedIndexChanged += (_, _) => {
            if (this.comboBoxLensMagnification.SelectedIndex >= 0)
               ds.Magnification = (byte)(this.comboBoxLensMagnification.SelectedIndex + Lens.Defaults.MinMagnification);
         };
         y = this.LayoutRow("Power level", this.comboBoxLensMagnification, LabelIndent, y, text);

         this.comboBoxLensScalingMode = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = ComboBoxW };
         this.comboBoxLensScalingMode.Items.AddRange(new object[]
         {
            "Nearest neighbor", "Bilinear", "High quality bilinear", "Bicubic", "High quality bicubic"
         });
         this.comboBoxLensScalingMode.SelectedIndex = (int)ds.Scaling;
         this.comboBoxLensScalingMode.SelectedIndexChanged += (_, _) => {
            if (this.comboBoxLensScalingMode.SelectedIndex >= 0)
               ds.Scaling = (ScalingMode)this.comboBoxLensScalingMode.SelectedIndex;
         };
         y = this.LayoutRow("Scaling", this.comboBoxLensScalingMode, LabelIndent, y, text);

         this.comboBoxLensSpeedFactor = ByteRangeComboBox(Lens.Defaults.MinSpeedFactor, Lens.Defaults.MaxSpeedFactor,
            i => i.ToString());
         this.comboBoxLensSpeedFactor.SelectedIndex = ds.SpeedFactor - Lens.Defaults.MinSpeedFactor;
         this.comboBoxLensSpeedFactor.SelectedIndexChanged += (_, _) => {
            if (this.comboBoxLensSpeedFactor.SelectedIndex >= 0)
               ds.SpeedFactor = (byte)(this.comboBoxLensSpeedFactor.SelectedIndex + Lens.Defaults.MinSpeedFactor);
         };
         y = this.LayoutRow("Speed factor", this.comboBoxLensSpeedFactor, LabelIndent, y, text);

         // ── Info ──────────────────────────────────────────────────────────────────────
         y += SectionGap;
         y = SectionHeader("INFO", y, accent, border);

         this.checkBoxInfoShowHex = InfoToggle(ds, nameof(ds.InfoShowHex), bg);
         y = this.LayoutRow("Show hex color value", this.checkBoxInfoShowHex, 0, y, text);

         this.checkBoxInfoShowRgb = InfoToggle(ds, nameof(ds.InfoShowRgb), bg);
         y = this.LayoutRow("Show RGB color value", this.checkBoxInfoShowRgb, 0, y, text);

         this.checkBoxInfoShowHsl = InfoToggle(ds, nameof(ds.InfoShowHsl), bg);
         y = this.LayoutRow("Show HSL color value", this.checkBoxInfoShowHsl, 0, y, text);

         y += SubGroupGap;
         this.checkBoxInfoShow12Bit = InfoToggle(ds, nameof(ds.InfoShow12Bit), bg);
         y = this.LayoutRow("Show 12-bit color conversion", this.checkBoxInfoShow12Bit, 0, y, text);

         this.checkBoxInfoShowWeb = InfoToggle(ds, nameof(ds.InfoShowWeb), bg);
         y = this.LayoutRow("Show web safe color conversion", this.checkBoxInfoShowWeb, 0, y, text);

         y += SubGroupGap;
         this.checkBoxInfoShowMouse = InfoToggle(ds, nameof(ds.InfoShowMouse), bg);
         y = this.LayoutRow("Show mouse position", this.checkBoxInfoShowMouse, 0, y, text);

         this.checkBoxInfoShowSize = InfoToggle(ds, nameof(ds.InfoShowSize), bg);
         y = this.LayoutRow("Show lens size", this.checkBoxInfoShowSize, 0, y, text);

         this.checkBoxInfoShowZoom = InfoToggle(ds, nameof(ds.InfoShowZoom), bg);
         y = this.LayoutRow("Show zoom level", this.checkBoxInfoShowZoom, 0, y, text);

         this.ClientSize = new Size(FormW, y + PadY);

         ds.PropertyChanged += this.OnSettingChanged;
      }

      private void OnSettingChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
      {
         var ds = Lens.Instance;
         switch (e.PropertyName)
         {
            case nameof(ds.GridStyle):
               this.comboBoxLensGridStyle.SelectedIndex = ds.GridStyle;
               break;
            case nameof(ds.GridSize):
               this.comboBoxLensGridSize.SelectedIndex = ds.GridSize - Lens.Defaults.MinGridSize;
               break;
            case nameof(ds.Magnification):
               this.comboBoxLensMagnification.SelectedIndex = ds.Magnification - Lens.Defaults.MinMagnification;
               break;
            case nameof(ds.SpeedFactor):
               this.comboBoxLensSpeedFactor.SelectedIndex = ds.SpeedFactor - Lens.Defaults.MinSpeedFactor;
               break;
            case nameof(ds.Scaling):
               this.comboBoxLensScalingMode.SelectedIndex = (int)ds.Scaling;
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

      /// <summary>
      /// Draws a transparent black rectangle, that's useful for visualizing Control placement and alignment.
      /// </summary>
      /// <param name="graphics">The <see cref="Graphics"/> handle.</param>
      /// <param name="rect">The <see cref="Rectangle"/> to draw.</param>
      /// <example><c>SettingsForm.DrawDebugRect(e.Graphics, this.ClientRectangle);</c></example>
      public static void DrawDebugRect(Graphics graphics, Rectangle rect)
      {
         var smoothingMode = graphics.SmoothingMode;
         graphics.SmoothingMode = SmoothingMode.None;
         using var clientRectBrush = new SolidBrush(Color.FromArgb(0x33, Color.Black));
         graphics.FillRectangle(clientRectBrush, rect);
         graphics.SmoothingMode = smoothingMode;
      }

      private CheckBox InfoToggle(Lens lens, string propertyName, Color backgroundColor)
      {
         var toggle = new Toggle
            {
               Text = propertyName,
               BackColor = backgroundColor,
               CheckAlign = ContentAlignment.MiddleCenter
            };

         toggle.Name = $"Toggle_{propertyName}";
         toggle.DataBindings.Add(nameof(toggle.Checked), lens, propertyName, false,
            DataSourceUpdateMode.OnPropertyChanged);

         return toggle;
      }

      private int SectionHeader(string text, int y, Color accent, Color border)
      {
         const int HeaderH = 28;
         const int SepH    = 1;
         const int After   = 8;

         this.Controls.Add(new Label
         {
            Text      = text,
            Location  = new Point(PadX - 2, y),
            Size      = new Size(FormW + 2 - PadX * 2, HeaderH),
            ForeColor = accent,
            BackColor = Color.Transparent,
            Font      = new Font(this.Font.FontFamily, 12, FontStyle.Bold),
            TextAlign = ContentAlignment.BottomLeft
         });
         y += HeaderH;

         this.Controls.Add(new Panel
         {
            Location  = new Point(PadX + 3, y),
            Size      = new Size(FormW - 3 - PadX * 2, SepH),
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

      private int LayoutRow(string labelText, Control ctrl, int xOffset, int y, Color labelColor)
      {
         // Wrap ComboBoxes in a Panel that acts as a 1px focus border.
         Control host = ctrl;
         if (ctrl is ComboBox combo)
         {
            var focusPanel = new Panel
            {
               Size      = new Size(combo.Width + 2, combo.Height + 2),
               BackColor = this.colorCtrlBorder,
            };
            combo.Location  = new Point(1, 1);
            combo.BackColor = this.colorCtrlBg;
            combo.Enter += (_, _) => focusPanel.BackColor = this.colorFocusBorder;
            combo.Leave += (_, _) => focusPanel.BackColor = this.colorCtrlBorder;
            focusPanel.Controls.Add(combo);
            host = focusPanel;
         }

         var label = new Label
         {
            Text      = labelText,
            Location  = new Point(PadX + xOffset, y),
            Size      = new Size(FormW - PadX - host.Width - PadX - xOffset - PadX, RowH),
            ForeColor = labelColor,
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.MiddleLeft,
            Cursor    = Cursors.Hand
         };
         EventHandler activate = (_, _) =>
         {
            if (ctrl is CheckBox cb)
            {
               cb.Focus();
               cb.Checked = !cb.Checked;
            }
            else if (ctrl is Button btn)
            {
               btn.Focus();
               btn.PerformClick();
            }
            else
            {
               ctrl.Focus();
            }
         };
         label.Click       += activate;
         label.DoubleClick += activate;
         this.Controls.Add(label);

         host.Top  = y + (RowH - host.Height) / 2;
         host.Left = FormW - PadX - host.Width;
         this.Controls.Add(host);

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
         this.colorGrid.Color = this.buttonLensGridColor.BackColor;
         if (this.colorGrid.ShowDialog() == DialogResult.OK)
            this.buttonLensGridColor.BackColor = this.colorGrid.Color;
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
