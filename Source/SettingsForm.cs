// -------------------------------------------------------------------------------------
// <copyright file="SettingsForm.cs">
//   Copyright (c) 2026
//   Licensed under the MIT License. See LICENSE file in the project root.
// </copyright>
// -------------------------------------------------------------------------------------

namespace StrangeLens
{
   using System;
   using System.ComponentModel;
   using System.Diagnostics;
   using System.Drawing;
   using System.Drawing.Drawing2D;
   using System.Runtime.InteropServices;
   using System.Windows.Forms;

   public sealed partial class SettingsForm : Form
   {
      private const int ComboBoxW = 132;

      private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

      private const int FormW = 335;

      private const int HotkeyToggle = 1;

      private const int LabelIndent = 12;

      private const uint ModCtrlAltShift = 0x0007; // MOD_ALT | MOD_CONTROL | MOD_SHIFT

      private const int PadX = 16;

      private const int PadY = 16;

      private const int RowH = 27;

      private const int SectionGap = 20;

      private const int SubGroupGap = 10;

      private readonly Timer clickTimer;

      private readonly FontInfo textFont;

      private AboutForm? aboutForm;

      private LensForm? activeLens;

      private Button buttonLensGridColor = null!;

      private CheckBox checkBoxInfoShow12Bit = null!;

      private CheckBox checkBoxInfoShowHex = null!;

      private CheckBox checkBoxInfoShowHsl = null!;

      private CheckBox checkBoxInfoShowMouse = null!;

      private CheckBox checkBoxInfoShowRgb = null!;

      private CheckBox checkBoxInfoShowSize = null!;

      private CheckBox checkBoxInfoShowWeb = null!;

      private CheckBox checkBoxInfoShowZoom = null!;

      private int clickCount;

      private Color colorCtrlBg;

      private Color colorCtrlBorder;

      private Color colorFocusBorder;

      private ComboBox comboBoxLensGridSize = null!;

      private ComboBox comboBoxLensGridStyle = null!;

      private ComboBox comboBoxLensHeight = null!;

      private ComboBox comboBoxLensMagnification = null!;

      private ComboBox comboBoxLensScalingMode = null!;

      private ComboBox comboBoxLensWidth = null!;

      private ComboBox comboBoxPrecisionSpeed = null!;

      private bool shouldExitApplication;

      public SettingsForm()
      {
         Debug.WriteLine("SETTINGS FORM CONSTRUCTOR - before InitializeComponent");
         this.InitializeComponent();
         Debug.WriteLine("SETTINGS FORM CONSTRUCTOR - after InitializeComponent");

         this.clickTimer = new Timer();
         this.clickTimer.Tick += this.ClickTimer_Elapsed;

         this.textFont = FontHelper.CreateRegularFontInfo();

         this.BuildLayout();

         var menuItemOpen = new ToolStripMenuItem
            {
               Text = "Show",
            };
         var menuItemSettings = new ToolStripMenuItem
            {
               Text = "&Settings...",
               ShortcutKeys = Keys.Control | Keys.Shift | Keys.S,
               ShowShortcutKeys = true,
            };
         var menuItemAbout = new ToolStripMenuItem
            {
               Text = "&About...",
            };
         var menuItemSeparator = new ToolStripSeparator();
         var menuItemExit = new ToolStripMenuItem
            {
               Text = "E&xit",
            };

         menuItemExit.Click += this.menuItemExit_Click;
         menuItemSettings.Click += this.menuItemSettings_Click;
         menuItemAbout.Click += this.menuItemAbout_Click;
         menuItemOpen.Click += this.menuItemOpen_Click;

         this.contextMenu.Items.AddRange(
            menuItemOpen,
            menuItemSettings,
            menuItemAbout,
            menuItemSeparator,
            menuItemExit);

         this.notifyIcon.Icon = this.Icon;
         this.notifyIcon.ContextMenuStrip = this.contextMenu;

         // WinForms creates the Win32 handle lazily on first Show(). Force it now so
         // OnHandleCreated fires immediately and RegisterHotKey works from the start.
         this.CreateHandle();
      }

      protected override CreateParams CreateParams
      {
         get
         {
            var cp = base.CreateParams;
            cp.ExStyle |=
               0x02000000; // WS_EX_COMPOSITED -- buffers the whole window before presenting, prevents white flash
            return cp;
         }
      }

      /// <summary>Draws a transparent black rectangle, that's useful for visualizing Control
      ///    placement and alignment.</summary>
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

      protected override void OnFormClosed(FormClosedEventArgs e)
      {
         base.OnFormClosed(e);
         UnregisterHotKey(this.Handle, HotkeyToggle);
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

         if (!RegisterHotKey(this.Handle, HotkeyToggle, ModCtrlAltShift, (uint)Keys.Z))
         {
            Debug.WriteLine($"RegisterHotKey failed: error {Marshal.GetLastWin32Error()}");
         }
      }

      protected override void WndProc(ref Message m)
      {
         const int WmHotkey = 0x0312;
         const int WmNcActivate = 0x0086;

         // Re-apply dark title bar on every focus change -- WM_NCACTIVATE fires when Windows
         // redraws the non-client area, and something (SetColorMode/WinForms internals) can
         // reset the DWM attribute before we see the message.
         if ((m.Msg == WmNcActivate) && IsDarkMode())
         {
            var dark = 1;
            DwmSetWindowAttribute(this.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref dark, sizeof(int));
         }

         if ((m.Msg == WmHotkey) && (m.WParam.ToInt32() == HotkeyToggle))
         {
            this.ToggleLens();
         }

         base.WndProc(ref m);
      }

      private static ComboBox ByteRangeComboBox(byte min, byte max, Func<int, string> label)
      {
         var cb = new ComboBox
            {
               DropDownStyle = ComboBoxStyle.DropDownList,
               Width = ComboBoxW,
            };
         for (int i = min; i <= max; i++)
         {
            cb.Items.Add(label(i));
         }

         return cb;
      }

      [DllImport("dwmapi.dll")]
      private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

      private static bool IsDarkMode()
      {
         return Lens.IsOsDarkMode();
      }

      [DllImport("user32.dll")]
      private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

      [DllImport("user32.dll")]
      private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

      private int BuildInfoSection(Lens ds, int y)
      {
         var palette = ds.ActivePalette;
         var colorAccentNormal = palette.AccentNormal;
         var colorBorder = palette.Border;
         var colorBackground = palette.Background;
         var colorTextNormal = palette.TextNormal;

         y = this.SectionHeader("INFO", y, colorAccentNormal, colorBorder);

         this.checkBoxInfoShowHex = this.InfoToggle(ds, nameof(ds.InfoShowHex), colorBackground);
         y = this.LayoutRow("Show hex color value", this.checkBoxInfoShowHex, 0, y, colorTextNormal);

         this.checkBoxInfoShowRgb = this.InfoToggle(ds, nameof(ds.InfoShowRgb), colorBackground);
         y = this.LayoutRow("Show RGB color value", this.checkBoxInfoShowRgb, 0, y, colorTextNormal);

         this.checkBoxInfoShowHsl = this.InfoToggle(ds, nameof(ds.InfoShowHsl), colorBackground);
         y = this.LayoutRow("Show HSL color value", this.checkBoxInfoShowHsl, 0, y, colorTextNormal);

         y += SubGroupGap;
         this.checkBoxInfoShow12Bit = this.InfoToggle(ds, nameof(ds.InfoShow12Bit), colorBackground);
         y = this.LayoutRow(
            "Show 12-bit color conversion",
            this.checkBoxInfoShow12Bit,
            0,
            y,
            colorTextNormal);

         this.checkBoxInfoShowWeb = this.InfoToggle(ds, nameof(ds.InfoShowWeb), colorBackground);
         y = this.LayoutRow(
            "Show web safe color conversion",
            this.checkBoxInfoShowWeb,
            0,
            y,
            colorTextNormal);

         y += SubGroupGap;
         this.checkBoxInfoShowMouse = this.InfoToggle(ds, nameof(ds.InfoShowMouse), colorBackground);
         y = this.LayoutRow("Show mouse position", this.checkBoxInfoShowMouse, 0, y, colorTextNormal);

         this.checkBoxInfoShowSize = this.InfoToggle(ds, nameof(ds.InfoShowSize), colorBackground);
         y = this.LayoutRow("Show lens size", this.checkBoxInfoShowSize, 0, y, colorTextNormal);

         this.checkBoxInfoShowZoom = this.InfoToggle(ds, nameof(ds.InfoShowZoom), colorBackground);
         y = this.LayoutRow("Show zoom level", this.checkBoxInfoShowZoom, 0, y, colorTextNormal);

         return y;
      }

      /// <summary>Builds the entire Settings window layout -- sections, controls, and data
      ///    bindings.</summary>
      private void BuildLayout()
      {
         var ds = Lens.Instance;
         var palette = ds.ActivePalette;

         this.BackColor = palette.Background;
         this.colorCtrlBg = palette.Control;
         this.colorCtrlBorder = palette.Border;
         this.colorFocusBorder = palette.AccentNormal;

         Toggle.Colors.Focus = palette.Border;
         Toggle.Colors.Thumb = palette.TextSubtle;
         Toggle.Colors.ThumbHover = palette.TextStrong;
         Toggle.Colors.TrackBase = palette.Inset;
         Toggle.Colors.TrackActive = palette.AccentNormal;
         Toggle.Colors.TrackHover = palette.AccentStrong;

         var y = PadY / 2;
         y = this.BuildLensSection(ds, y);
         y += SectionGap;
         y = this.BuildInfoSection(ds, y);

         this.ClientSize = new Size(FormW, y + PadY);
         ds.PropertyChanged += this.OnSettingChanged;
      }

      private int BuildLensSection(Lens ds, int y)
      {
         var palette = ds.ActivePalette;
         var colorAccentNormal = palette.AccentNormal;
         var colorBorder = palette.Border;
         var colorTextSubtle = palette.TextSubtle;
         var colorTextNormal = palette.TextNormal;

         y = this.SectionHeader("LENS", y, colorAccentNormal, colorBorder);

         this.comboBoxLensWidth = new ComboBox
            {
               DropDownStyle = ComboBoxStyle.DropDownList,
               Width = ComboBoxW,
            };
         for (int i = Lens.Defaults.MinWidth; i <= Lens.Defaults.MaxWidth; i += Lens.Defaults.SizeIncrement)
         {
            this.comboBoxLensWidth.Items.Add($"{i} px");
         }

         this.comboBoxLensWidth.SelectedIndex =
            (ds.Width - Lens.Defaults.MinWidth) / Lens.Defaults.SizeIncrement;
         this.comboBoxLensWidth.SelectedIndexChanged += this.OnWidthChanged;
         y = this.LayoutRow("Panel width", this.comboBoxLensWidth, 0, y, colorTextNormal);

         this.comboBoxLensHeight = new ComboBox
            {
               DropDownStyle = ComboBoxStyle.DropDownList,
               Width = ComboBoxW,
            };
         for (int i = Lens.Defaults.MinHeight; i <= Lens.Defaults.MaxHeight; i += Lens.Defaults.SizeIncrement)
         {
            this.comboBoxLensHeight.Items.Add($"{i} px");
         }

         this.comboBoxLensHeight.SelectedIndex =
            (ds.Height - Lens.Defaults.MinHeight) / Lens.Defaults.SizeIncrement;
         this.comboBoxLensHeight.SelectedIndexChanged += this.OnHeightChanged;
         y = this.LayoutRow("Panel height", this.comboBoxLensHeight, 0, y, colorTextNormal);

         this.comboBoxPrecisionSpeed = new ComboBox
            {
               DropDownStyle = ComboBoxStyle.DropDownList,
               Width = ComboBoxW,
            };
         foreach (var pct in Lens.PrecisionSpeedOptions)
         {
            this.comboBoxPrecisionSpeed.Items.Add($"{pct}%");
         }

         this.comboBoxPrecisionSpeed.SelectedIndex =
            Array.IndexOf(Lens.PrecisionSpeedOptions, ds.PrecisionSpeed);
         this.comboBoxPrecisionSpeed.SelectedIndexChanged += this.OnPrecisionSpeedChanged;
         y = this.LayoutRow("Mouse Precision Speed", this.comboBoxPrecisionSpeed, 0, y, colorTextNormal);

         y += SubGroupGap;
         y = this.SubHeader("Grid", y, colorTextSubtle);

         this.comboBoxLensGridStyle = new ComboBox
            {
               DropDownStyle = ComboBoxStyle.DropDownList,
               Width = ComboBoxW,
            };
         this.comboBoxLensGridStyle.Items.AddRange(
            "None",
            "Solid",
            "Dash",
            "Dot",
            "Dash, Dot",
            "Dash, Dot, Dot");
         this.comboBoxLensGridStyle.SelectedIndex = ds.GridStyle;
         this.comboBoxLensGridStyle.SelectedIndexChanged += this.OnGridStyleChanged;
         y = this.LayoutRow("Style", this.comboBoxLensGridStyle, LabelIndent, y, colorTextNormal);

         this.comboBoxLensGridSize = ByteRangeComboBox(
            Lens.Defaults.MinGridSize,
            Lens.Defaults.MaxGridSize,
            i => i == 1 ? "1 pixel" : $"{i} pixels");
         this.comboBoxLensGridSize.SelectedIndex = ds.GridSize - Lens.Defaults.MinGridSize;
         this.comboBoxLensGridSize.SelectedIndexChanged += this.OnGridSizeChanged;
         y = this.LayoutRow("Size", this.comboBoxLensGridSize, LabelIndent, y, colorTextNormal);

         this.buttonLensGridColor = new Button
            {
               Width = 24,
               Height = 24,
               FlatStyle = FlatStyle.Flat,
               BackColor = ds.GridColor,
               UseVisualStyleBackColor = false,
            };
         this.buttonLensGridColor.FlatAppearance.BorderColor = colorBorder;
         this.buttonLensGridColor.DataBindings.Add(
            nameof(this.buttonLensGridColor.BackColor),
            ds,
            nameof(ds.GridColor),
            false,
            DataSourceUpdateMode.OnPropertyChanged);
         this.buttonLensGridColor.Click += this.button1_Click;
         y = this.LayoutRow("Color", this.buttonLensGridColor, LabelIndent, y, colorTextNormal);

         y += SubGroupGap;
         y = this.SubHeader("Magnification", y, colorTextSubtle);

         this.comboBoxLensMagnification = ByteRangeComboBox(
            Lens.Defaults.MinMagnification,
            Lens.Defaults.MaxMagnification,
            i => $"×{i}");
         this.comboBoxLensMagnification.SelectedIndex = ds.Magnification - Lens.Defaults.MinMagnification;
         this.comboBoxLensMagnification.SelectedIndexChanged += this.OnMagnificationChanged;
         y = this.LayoutRow("Power level", this.comboBoxLensMagnification, LabelIndent, y, colorTextNormal);

         this.comboBoxLensScalingMode = new ComboBox
            {
               DropDownStyle = ComboBoxStyle.DropDownList,
               Width = ComboBoxW,
            };
         this.comboBoxLensScalingMode.Items.AddRange(
            "Nearest neighbor",
            "Bilinear",
            "High quality bilinear",
            "Bicubic",
            "High quality bicubic");
         this.comboBoxLensScalingMode.SelectedIndex = (int)ds.Scaling;
         this.comboBoxLensScalingMode.SelectedIndexChanged += this.OnScalingModeChanged;
         y = this.LayoutRow("Scaling", this.comboBoxLensScalingMode, LabelIndent, y, colorTextNormal);

         return y;
      }

      private void button1_Click(object? sender, EventArgs e)
      {
         this.colorGrid.Color = this.buttonLensGridColor.BackColor;
         if (this.colorGrid.ShowDialog() == DialogResult.OK)
         {
            this.buttonLensGridColor.BackColor = this.colorGrid.Color;
         }
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

            this.OpenSettings();
         }
         else
         {
            this.ToggleLens();
         }

         this.clickCount = 0;
      }

      private void CloseToSystemTray(CancelEventArgs e)
      {
         Console.WriteLine("Closing to System Tray");
         e.Cancel = true;
         this.Hide();
      }

      private CheckBox InfoToggle(Lens lens, string propertyName, Color backgroundColor)
      {
         var toggle = new Toggle
            {
               Text = propertyName,
               BackColor = backgroundColor,
               CheckAlign = ContentAlignment.MiddleCenter,
            };

         toggle.Name = $"Toggle_{propertyName}";
         toggle.DataBindings.Add(
            nameof(toggle.Checked),
            lens,
            propertyName,
            false,
            DataSourceUpdateMode.OnPropertyChanged);

         return toggle;
      }

      private int LayoutRow(string labelText, Control ctrl, int xOffset, int y, Color labelColor)
      {
         var host = ctrl;
         if (ctrl is ComboBox combo)
         {
            var focusPanel = new Panel
               {
                  Size = new Size(combo.Width + 2, combo.Height + 2),
                  BackColor = this.colorCtrlBorder,
               };
            combo.Location = new Point(1, 1);
            combo.BackColor = this.colorCtrlBg;
            combo.Enter += (_, _) => focusPanel.BackColor = this.colorFocusBorder;
            combo.Leave += (_, _) => focusPanel.BackColor = this.colorCtrlBorder;
            focusPanel.Controls.Add(combo);
            host = focusPanel;
         }

         var label = new Label
            {
               Text = labelText,
               Font = this.textFont.Font,
               Location = new Point(PadX + xOffset, y),
               Size = new Size(FormW - PadX - host.Width - PadX - xOffset - PadX, RowH),
               ForeColor = labelColor,
               BackColor = Color.Transparent,
               TextAlign = ContentAlignment.MiddleLeft,
               Cursor = Cursors.Hand,
            };

         if (label.Font.Name == "Segoe UI")
         {
            label.Padding = new Padding(0, 0, 0, 2);
         }

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
         label.Click += activate;
         label.DoubleClick += activate;
         this.Controls.Add(label);

         host.Top = y + ((RowH - host.Height) / 2);
         host.Left = FormW - PadX - host.Width;
         this.Controls.Add(host);

         return y + RowH;
      }

      private void menuItemAbout_Click(object? sender, EventArgs e)
      {
         if (this.aboutForm is { IsDisposed: false })
         {
            this.aboutForm.Activate();
            return;
         }

         this.aboutForm = new AboutForm(this.Icon!);
         this.aboutForm.ShowDialog(this);
         this.aboutForm.Dispose();
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
         this.OpenSettings();
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

      private void OnGridSizeChanged(object? sender, EventArgs e)
      {
         if (this.comboBoxLensGridSize.SelectedIndex >= 0)
         {
            Lens.Instance.GridSize =
               (byte)(this.comboBoxLensGridSize.SelectedIndex + Lens.Defaults.MinGridSize);
         }
      }

      private void OnGridStyleChanged(object? sender, EventArgs e)
      {
         if (this.comboBoxLensGridStyle.SelectedIndex >= 0)
         {
            Lens.Instance.GridStyle = this.comboBoxLensGridStyle.SelectedIndex;
         }
      }

      private void OnHeightChanged(object? sender, EventArgs e)
      {
         if (this.comboBoxLensHeight.SelectedIndex >= 0)
         {
            Lens.Instance.Height = (short)(Lens.Defaults.MinHeight
                                           + (this.comboBoxLensHeight.SelectedIndex
                                              * Lens.Defaults.SizeIncrement));
         }
      }

      private void OnMagnificationChanged(object? sender, EventArgs e)
      {
         if (this.comboBoxLensMagnification.SelectedIndex >= 0)
         {
            Lens.Instance.Magnification =
               (byte)(this.comboBoxLensMagnification.SelectedIndex + Lens.Defaults.MinMagnification);
         }
      }

      private void OnPrecisionSpeedChanged(object? sender, EventArgs e)
      {
         if (this.comboBoxPrecisionSpeed.SelectedIndex >= 0)
         {
            Lens.Instance.PrecisionSpeed =
               Lens.PrecisionSpeedOptions[this.comboBoxPrecisionSpeed.SelectedIndex];
         }
      }

      private void OnScalingModeChanged(object? sender, EventArgs e)
      {
         if (this.comboBoxLensScalingMode.SelectedIndex >= 0)
         {
            Lens.Instance.Scaling = (ScalingMode)this.comboBoxLensScalingMode.SelectedIndex;
         }
      }

      private void OnSettingChanged(object? sender, PropertyChangedEventArgs e)
      {
         var ds = Lens.Instance;
         switch (e.PropertyName)
         {
            case nameof(ds.Width):
               this.comboBoxLensWidth.SelectedIndex =
                  (ds.Width - Lens.Defaults.MinWidth) / Lens.Defaults.SizeIncrement;
               break;
            case nameof(ds.Height):
               this.comboBoxLensHeight.SelectedIndex =
                  (ds.Height - Lens.Defaults.MinHeight) / Lens.Defaults.SizeIncrement;
               break;
            case nameof(ds.GridStyle):
               this.comboBoxLensGridStyle.SelectedIndex = ds.GridStyle;
               break;
            case nameof(ds.GridSize):
               this.comboBoxLensGridSize.SelectedIndex = ds.GridSize - Lens.Defaults.MinGridSize;
               break;
            case nameof(ds.Magnification):
               this.comboBoxLensMagnification.SelectedIndex =
                  ds.Magnification - Lens.Defaults.MinMagnification;
               break;
            case nameof(ds.PrecisionSpeed):
               var pidx = Array.IndexOf(Lens.PrecisionSpeedOptions, ds.PrecisionSpeed);
               if (pidx >= 0)
               {
                  this.comboBoxPrecisionSpeed.SelectedIndex = pidx;
               }

               break;
            case nameof(ds.Scaling):
               this.comboBoxLensScalingMode.SelectedIndex = (int)ds.Scaling;
               break;
         }
      }

      private void OnWidthChanged(object? sender, EventArgs e)
      {
         if (this.comboBoxLensWidth.SelectedIndex >= 0)
         {
            Lens.Instance.Width = (short)(Lens.Defaults.MinWidth
                                          + (this.comboBoxLensWidth.SelectedIndex
                                             * Lens.Defaults.SizeIncrement));
         }
      }

      private void OpenSettings()
      {
         Console.WriteLine("Open Settings");
         this.Show();
         this.Activate();
      }

      private int SectionHeader(string text, int y, Color accent, Color border)
      {
         const int HeaderH = 28;
         const int SepH = 1;
         const int After = 8;

         this.Controls.Add(
            new Label
               {
                  Text = text,
                  Location = new Point(PadX - 2, y),
                  Size = new Size((FormW + 2) - (PadX * 2), HeaderH),
                  ForeColor = accent,
                  BackColor = Color.Transparent,
                  Font = new Font(this.textFont.Font.FontFamily, 12, FontStyle.Bold),
                  TextAlign = ContentAlignment.BottomLeft,
               });
         y += HeaderH;

         this.Controls.Add(
            new Panel
               {
                  Location = new Point(PadX + 3, y),
                  Size = new Size(FormW - 3 - (PadX * 2), SepH),
                  BackColor = border,
               });

         return y + SepH + After;
      }

      private void SettingsForm_FormClosing(object? sender, FormClosingEventArgs e)
      {
         if (!this.shouldExitApplication)
         {
            this.CloseToSystemTray(e);
         }
         else
         {
            Console.WriteLine("Exiting Application");
         }
      }

      private int SubHeader(string text, int y, Color color)
      {
         const int SubH = 24;
         const int After = 2;

         this.Controls.Add(
            new Label
               {
                  Text = text,
                  Font = new Font(this.textFont.Font, FontStyle.Bold),
                  Location = new Point(PadX, y),
                  Size = new Size(FormW - (PadX * 2), SubH),
                  ForeColor = color,
                  BackColor = Color.Transparent,
                  TextAlign = ContentAlignment.MiddleLeft,
               });

         return y + SubH + After;
      }

      private void ToggleLens()
      {
         if (this.activeLens != null)
         {
            this.activeLens.Close();
            return;
         }

         var lensForm = new LensForm
            {
               TargetLocation = Cursor.Position,
            };
         lensForm.FormClosed += (_, _) => this.activeLens = null;
         this.activeLens = lensForm;
         lensForm.Show();
         lensForm.Activate();
      }
   }
}
