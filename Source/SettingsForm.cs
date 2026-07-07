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
   using System.Diagnostics.CodeAnalysis;
   using System.Drawing;
   using System.Drawing.Drawing2D;
   using System.Runtime.InteropServices;
   using System.Windows.Forms;

   using Microsoft.Win32;

   using static NativeMethods;

   public sealed partial class SettingsForm : Form
   {
      private const int ComboBoxW = 132;

      private const int FormW = 335;

      private const int Hotkey12Bit = 6;

      private const int HotkeyHex = 2;

      private const int HotkeyHsl = 4;

      private const int HotkeyMeasure = 7;

      private const int HotkeyRgb = 3;

      private const int HotkeyToggle = 1;

      private const int HotkeyWeb = 5;

      private const int LabelIndent = 12;

      private const uint ModCtrlAltShift = MOD_ALT | MOD_CONTROL | MOD_SHIFT;

      private const int PadX = 16;

      private const int PadY = 16;

      private const int RowH = 27;

      private const int SectionGap = 20;

      private const int SubGroupGap = 10;

      private readonly Timer clickTimer;

      private AboutForm? aboutForm;

      private LensForm? activeLens;

      private CheckBox checkBoxInfoShow12Bit = null!;

      private CheckBox checkBoxInfoShowHex = null!;

      private CheckBox checkBoxInfoShowHsl = null!;

      private CheckBox checkBoxInfoShowMouse = null!;

      private CheckBox checkBoxInfoShowRgb = null!;

      private CheckBox checkBoxInfoShowSize = null!;

      private CheckBox checkBoxInfoShowWeb = null!;

      private CheckBox checkBoxInfoShowZoom = null!;

      private int clickCount;

      private Color comboBoxBackground;

      private ComboBox comboBoxLensGridOpacity = null!;

      private ComboBox comboBoxLensGridSize = null!;

      private ComboBox comboBoxLensGridStyle = null!;

      private ComboBox comboBoxLensHeight = null!;

      private ComboBox comboBoxLensMagnification = null!;

      private ComboBox comboBoxLensScalingMode = null!;

      private ComboBox comboBoxLensWidth = null!;

      private ComboBox comboBoxPrecisionSpeed = null!;

      private float layoutScale = 1f;

      private ThemePalette palette = null!;

      private bool shouldExitApplication;

      private FontInfo? textFont;

      public SettingsForm()
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

         this.BuildLayout(this.DeviceDpi / 96f);
      }

      [SuppressMessage("ReSharper", "CognitiveComplexity")]
      protected override void WndProc(ref Message m)
      {
         if (m.Msg == WM_DPICHANGED)
         {
            base.WndProc(ref m);
            var newDpi = (int)(m.WParam >>> 16);
            var newScale = newDpi / 96f;
            // Scale current client size proportionally as a placeholder; BuildLayout sets exact size.
            this.ClientSize = new Size(
               (int)Math.Round(FormW * newScale),
               (int)Math.Round((this.ClientSize.Height * newScale) / this.layoutScale));
            this.BeginInvoke(() => this.BuildLayout(newScale));
            return;
         }

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

      private static ComboBox ByteRangeComboBox(byte min, byte max, Func<int, string> label, int width)
      {
         var cb = new ComboBox
            {
               DropDownStyle = ComboBoxStyle.DropDownList,
               Width = width,
            };
         for (int i = min; i <= max; i++)
         {
            cb.Items.Add(label(i));
         }

         return cb;
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

      private int BuildInfoSection(Lens ds, int y)
      {
         var s = this.layoutScale;
         var ap = ds.ActivePalette;

         y = this.SectionHeader("INFO", y, ap.AccentNormal, ap.Border);

         this.checkBoxInfoShowHex = this.InfoToggle(ds, nameof(ds.InfoShowHex), ap.Background);
         y = this.LayoutRow("Show hex color value", this.checkBoxInfoShowHex, 0, y, ap.TextNormal);

         this.checkBoxInfoShowRgb = this.InfoToggle(ds, nameof(ds.InfoShowRgb), ap.Background);
         y = this.LayoutRow("Show RGB color value", this.checkBoxInfoShowRgb, 0, y, ap.TextNormal);

         this.checkBoxInfoShowHsl = this.InfoToggle(ds, nameof(ds.InfoShowHsl), ap.Background);
         y = this.LayoutRow("Show HSL color value", this.checkBoxInfoShowHsl, 0, y, ap.TextNormal);

         y += (int)Math.Round(SubGroupGap * s);
         this.checkBoxInfoShow12Bit = this.InfoToggle(ds, nameof(ds.InfoShow12Bit), ap.Background);
         y = this.LayoutRow("Show 12-bit color conversion", this.checkBoxInfoShow12Bit, 0, y, ap.TextNormal);

         this.checkBoxInfoShowWeb = this.InfoToggle(ds, nameof(ds.InfoShowWeb), ap.Background);
         y = this.LayoutRow("Show web safe color conversion", this.checkBoxInfoShowWeb, 0, y, ap.TextNormal);

         y += (int)Math.Round(SubGroupGap * s);
         this.checkBoxInfoShowMouse = this.InfoToggle(ds, nameof(ds.InfoShowMouse), ap.Background);
         y = this.LayoutRow("Show mouse position", this.checkBoxInfoShowMouse, 0, y, ap.TextNormal);

         this.checkBoxInfoShowSize = this.InfoToggle(ds, nameof(ds.InfoShowSize), ap.Background);
         y = this.LayoutRow("Show lens size", this.checkBoxInfoShowSize, 0, y, ap.TextNormal);

         this.checkBoxInfoShowZoom = this.InfoToggle(ds, nameof(ds.InfoShowZoom), ap.Background);
         y = this.LayoutRow("Show zoom level", this.checkBoxInfoShowZoom, 0, y, ap.TextNormal);

         return y;
      }

      /// <summary>Builds the entire Settings window layout -- sections, controls, and data
      ///    bindings.</summary>
      private void BuildLayout(float scale)
      {
         this.layoutScale = scale;
         var s = scale;
         var ds = Lens.Instance;

         // Unsubscribe before clearing to prevent duplicate handlers on rebuild.
         ds.PropertyChanged -= this.OnSettingChanged;

         // Dispose old controls first (their Paint closures hold a reference to the old
         // textFont.Font; the font must outlive the controls that reference it).
         var old = new Control[this.Controls.Count];
         this.Controls.CopyTo(old, 0);
         this.Controls.Clear();
         this.SuspendLayout();
         try
         {
            foreach (var c in old)
            {
               c.Dispose();
            }

            this.textFont?.Dispose();
            this.textFont = FontHelper.CreateRegularFontInfo(scale);

            var activePalette = ds.ActivePalette;
            this.palette = activePalette;
            this.BackColor = activePalette.Background;
            this.comboBoxBackground = this.palette.Control.Darken(50);

            Toggle.Colors.Focus = activePalette.Border;
            Toggle.Colors.Thumb = activePalette.TextSubtle;
            Toggle.Colors.ThumbHover = activePalette.TextStrong;
            Toggle.Colors.TrackBase = activePalette.Inset;
            Toggle.Colors.TrackActive = activePalette.AccentNormal;
            Toggle.Colors.TrackHover = activePalette.AccentStrong;

            var y = (int)Math.Round((PadY / 2f) * s);
            y = this.BuildLensSection(ds, y);
            y += (int)Math.Round(SectionGap * s);
            y = this.BuildInfoSection(ds, y);

            this.ClientSize = new Size((int)Math.Round(FormW * s), y + (int)Math.Round(PadY * s));

            ds.PropertyChanged += this.OnSettingChanged;
         }
         finally
         {
            this.ResumeLayout(performLayout: true);
         }
      }

      private int BuildLensSection(Lens ds, int y)
      {
         var s = this.layoutScale;
         var ap = ds.ActivePalette;
         var comboW = (int)Math.Round(ComboBoxW * s);

         y = this.SectionHeader("LENS", y, ap.AccentNormal, ap.Border);

         this.comboBoxLensWidth = new ComboBox
            {
               DropDownStyle = ComboBoxStyle.DropDownList,
               Width = comboW,
            };
         for (int i = Lens.Defaults.MinWidth; i <= Lens.Defaults.MaxWidth; i += Lens.Defaults.SizeIncrement)
         {
            this.comboBoxLensWidth.Items.Add($"{i} px");
         }

         this.comboBoxLensWidth.SelectedIndex =
            (ds.Width - Lens.Defaults.MinWidth) / Lens.Defaults.SizeIncrement;
         this.comboBoxLensWidth.SelectedIndexChanged += this.OnWidthChanged;
         y = this.LayoutRow("Panel width", this.comboBoxLensWidth, 0, y, ap.TextNormal);

         this.comboBoxLensHeight = new ComboBox
            {
               DropDownStyle = ComboBoxStyle.DropDownList,
               Width = comboW,
            };
         for (int i = Lens.Defaults.MinHeight; i <= Lens.Defaults.MaxHeight; i += Lens.Defaults.SizeIncrement)
         {
            this.comboBoxLensHeight.Items.Add($"{i} px");
         }

         this.comboBoxLensHeight.SelectedIndex =
            (ds.Height - Lens.Defaults.MinHeight) / Lens.Defaults.SizeIncrement;
         this.comboBoxLensHeight.SelectedIndexChanged += this.OnHeightChanged;
         y = this.LayoutRow("Panel height", this.comboBoxLensHeight, 0, y, ap.TextNormal);

         this.comboBoxPrecisionSpeed = new ComboBox
            {
               DropDownStyle = ComboBoxStyle.DropDownList,
               Width = comboW,
            };
         foreach (var pct in Lens.PrecisionSpeedOptions)
         {
            this.comboBoxPrecisionSpeed.Items.Add($"{pct}%");
         }

         this.comboBoxPrecisionSpeed.SelectedIndex =
            Array.IndexOf(Lens.PrecisionSpeedOptions, ds.PrecisionSpeed);
         this.comboBoxPrecisionSpeed.SelectedIndexChanged += this.OnPrecisionSpeedChanged;
         y = this.LayoutRow("Mouse Precision Speed", this.comboBoxPrecisionSpeed, 0, y, ap.TextNormal);

         y += (int)Math.Round(SubGroupGap * s);
         y = this.SubHeader("Grid", y, ap.TextSubtle);

         this.comboBoxLensGridStyle = new ComboBox
            {
               DropDownStyle = ComboBoxStyle.DropDownList,
               Width = comboW,
            };
         this.comboBoxLensGridStyle.Items.AddRange(
            "None",
            "Solid",
            "Dash",
            "Dot",
            "Dash, Dot",
            "Dash, Dot, Dot");
         this.comboBoxLensGridStyle.SelectedIndex = (int)ds.GridStyle;
         this.comboBoxLensGridStyle.SelectedIndexChanged += this.OnGridStyleChanged;
         y = this.LayoutRow("Style", this.comboBoxLensGridStyle, LabelIndent, y, ap.TextNormal);

         this.comboBoxLensGridSize = ByteRangeComboBox(
            Lens.Defaults.MinGridSize,
            Lens.Defaults.MaxGridSize,
            i => i == 1 ? "1 pixel" : $"{i} pixels",
            comboW);
         this.comboBoxLensGridSize.SelectedIndex = ds.GridSize - Lens.Defaults.MinGridSize;
         this.comboBoxLensGridSize.SelectedIndexChanged += this.OnGridSizeChanged;
         y = this.LayoutRow("Size", this.comboBoxLensGridSize, LabelIndent, y, ap.TextNormal);

         this.comboBoxLensGridOpacity = new ComboBox
            {
               DropDownStyle = ComboBoxStyle.DropDownList,
               Width = comboW,
            };
         foreach (var pct in Lens.GridOpacityOptions)
         {
            this.comboBoxLensGridOpacity.Items.Add($"{pct}%");
         }

         this.comboBoxLensGridOpacity.SelectedIndex = Array.IndexOf(Lens.GridOpacityOptions, ds.GridOpacity);
         this.comboBoxLensGridOpacity.SelectedIndexChanged += this.OnGridOpacityChanged;
         y = this.LayoutRow("Opacity", this.comboBoxLensGridOpacity, LabelIndent, y, ap.TextNormal);

         this.UpdateGridDependentControls();

         y += (int)Math.Round(SubGroupGap * s);
         y = this.SubHeader("Magnification", y, ap.TextSubtle);

         this.comboBoxLensMagnification = ByteRangeComboBox(
            Lens.Defaults.MinMagnification,
            Lens.Defaults.MaxMagnification,
            i => $"×{i}",
            comboW);
         this.comboBoxLensMagnification.SelectedIndex = ds.Magnification - Lens.Defaults.MinMagnification;
         this.comboBoxLensMagnification.SelectedIndexChanged += this.OnMagnificationChanged;
         y = this.LayoutRow("Power level", this.comboBoxLensMagnification, LabelIndent, y, ap.TextNormal);

         this.comboBoxLensScalingMode = new ComboBox
            {
               DropDownStyle = ComboBoxStyle.DropDownList,
               Width = comboW,
            };
         this.comboBoxLensScalingMode.Items.AddRange(
            "Nearest neighbor",
            "Bilinear",
            "High quality bilinear",
            "Bicubic",
            "High quality bicubic");
         this.comboBoxLensScalingMode.SelectedIndex = (int)ds.Scaling;
         this.comboBoxLensScalingMode.SelectedIndexChanged += this.OnScalingModeChanged;
         y = this.LayoutRow("Scaling", this.comboBoxLensScalingMode, LabelIndent, y, ap.TextNormal);

         return y;
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
         e.Cancel = true;
         this.Hide();
      }

      private CheckBox InfoToggle(Lens lens, string propertyName, Color backgroundColor)
      {
         var s = this.layoutScale;
         var w = (int)Math.Round(40 * s);
         var h = (int)Math.Round(23 * s);

         var toggle = new Toggle
            {
               Text = propertyName,
               BackColor = backgroundColor,
               CheckAlign = ContentAlignment.MiddleCenter,
            };
         toggle.MinimumSize = new Size(w, h);
         toggle.Size = new Size(w, h);

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
         var s = this.layoutScale;
         var rowH = (int)Math.Round(RowH * s);
         var padX = (int)Math.Round(PadX * s);
         var formW = (int)Math.Round(FormW * s);
         var scaledOffset = (int)Math.Round(xOffset * s);

         var host = ctrl;
         if (ctrl is ComboBox combo)
         {
            combo.Font = this.textFont!.Font;
            combo.ForeColor = this.palette.TextNormal;
            combo.DrawMode = DrawMode.OwnerDrawFixed;
            combo.ItemHeight = this.textFont.Font.Height + 2;
            combo.DrawItem += this.OnComboDrawItem;
            var border = (int)Math.Round(2 * s);
            var focusPanel = new Panel
               {
                  Size = new Size(combo.Width + (border * 2), combo.Height + (border * 2)),
                  BackColor = this.BackColor,
               };
            combo.Location = new Point(border, border);
            combo.BackColor = this.comboBoxBackground;
            combo.Enter += (_, _) => focusPanel.BackColor = this.palette.Border;
            combo.Leave += (_, _) => focusPanel.BackColor = this.BackColor;
            focusPanel.Controls.Add(combo);
            host = focusPanel;
         }

         var capturedFont = this.textFont!.Font;
         var labelW = formW - padX - scaledOffset - host.Width - padX;
         var label = new Panel
            {
               Location = new Point(padX + scaledOffset, y),
               Size = new Size(labelW, rowH),
               BackColor = Color.Transparent,
               Cursor = Cursors.Hand,
            };
         label.Paint += (_, e) =>
            {
               const TextFormatFlags Flags = TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix
                                                                       | TextFormatFlags.SingleLine;
               var measured = TextRenderer.MeasureText(
                  e.Graphics,
                  labelText,
                  capturedFont,
                  Size.Empty,
                  Flags);
               TextRenderer.DrawText(
                  e.Graphics,
                  labelText,
                  capturedFont,
                  new Point(0, (rowH - measured.Height) / 2),
                  labelColor,
                  Flags);
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
         label.Click += activate;
         label.DoubleClick += activate;
         this.Controls.Add(label);

         host.Top = y + ((rowH - host.Height) / 2);
         host.Left = formW - padX - host.Width;
         this.Controls.Add(host);

         return y + rowH;
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

      private void OnComboDrawItem(object? sender, DrawItemEventArgs e)
      {
         if (sender is not ComboBox cb || (e.Index < 0))
         {
            return;
         }

         var isEnabled = cb.Enabled;
         var inEditArea = (e.State & DrawItemState.ComboBoxEdit) != 0;
         var isSelected = isEnabled && !inEditArea && ((e.State & DrawItemState.Selected) != 0);

         var bg = isSelected ? this.palette.AccentSubtle : this.comboBoxBackground;
         var fg = !isEnabled ? this.palette.TextSubtle :
            isSelected ? this.palette.TextStrong : this.palette.TextNormal;

         using var bgBrush = new SolidBrush(bg);
         e.Graphics.FillRectangle(bgBrush, e.Bounds);
         TextRenderer.DrawText(
            e.Graphics,
            cb.Items[e.Index]?.ToString() ?? string.Empty,
            this.textFont!.Font,
            e.Bounds,
            fg,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
      }

      private void OnGridOpacityChanged(object? sender, EventArgs e)
      {
         if (this.comboBoxLensGridOpacity.SelectedIndex >= 0)
         {
            Lens.Instance.GridOpacity = Lens.GridOpacityOptions[this.comboBoxLensGridOpacity.SelectedIndex];
         }
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
            Lens.Instance.GridStyle = (GridStyleOption)this.comboBoxLensGridStyle.SelectedIndex;
            this.UpdateGridDependentControls();
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
            Lens.Instance.Scaling = (ScalingModeOption)this.comboBoxLensScalingMode.SelectedIndex;
         }
      }

      [SuppressMessage("ReSharper", "CognitiveComplexity")]
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
               this.comboBoxLensGridStyle.SelectedIndex = (int)ds.GridStyle;
               this.UpdateGridDependentControls();
               break;
            case nameof(ds.GridSize):
               this.comboBoxLensGridSize.SelectedIndex = ds.GridSize - Lens.Defaults.MinGridSize;
               break;
            case nameof(ds.GridOpacity):
               var opacityIdx = Array.IndexOf(Lens.GridOpacityOptions, ds.GridOpacity);
               if (opacityIdx >= 0)
               {
                  this.comboBoxLensGridOpacity.SelectedIndex = opacityIdx;
               }

               break;
            case nameof(ds.Magnification):
               this.comboBoxLensMagnification.SelectedIndex =
                  ds.Magnification - Lens.Defaults.MinMagnification;
               break;
            case nameof(ds.PrecisionSpeed):
               var precisionIdx = Array.IndexOf(Lens.PrecisionSpeedOptions, ds.PrecisionSpeed);
               if (precisionIdx >= 0)
               {
                  this.comboBoxPrecisionSpeed.SelectedIndex = precisionIdx;
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
         if (!this.Visible)
         {
            // Opacity fade via SetLayeredWindowAttributes covers the entire composited
            // window (DWM chrome included), unlike AnimateWindow, which only affects the
            // client area under DWM and causes chrome-before-content flash.
            this.Opacity = 0;
            this.Show();
            var fadeTimer = new Timer
               {
                  Interval = 15,
               };
            fadeTimer.Tick += (_, _) =>
               {
                  this.Opacity = Math.Min(1.0, this.Opacity + 0.1);
                  if (this.Opacity >= 1.0)
                  {
                     fadeTimer.Stop();
                     fadeTimer.Dispose();
                  }
               };
            fadeTimer.Start();
         }
         else
         {
            this.Show();
         }

         this.Activate();
      }

      private int SectionHeader(string text, int y, Color accent, Color border)
      {
         var s = this.layoutScale;
         const int HeaderH = 28;
         const int SepH = 1;
         const int After = 8;
         var headerH = (int)Math.Round(HeaderH * s);
         var after = (int)Math.Round(After * s);
         var padX = (int)Math.Round(PadX * s);
         var formW = (int)Math.Round(FormW * s);

         // 16px at 100% DPI matches the original 12pt (= 16px at 96 dpi) section header.
         var headerFont = new Font(
            this.textFont!.Font.FontFamily,
            16f * s,
            FontStyle.Bold,
            GraphicsUnit.Pixel);
         var headerPanel = new Panel
            {
               Location = new Point(padX - (int)Math.Round(2 * s), y),
               Size = new Size((formW + (int)Math.Round(2 * s)) - (padX * 2), headerH),
               BackColor = Color.Transparent,
            };
         headerPanel.Disposed += (_, _) => headerFont.Dispose();
         headerPanel.Paint += (_, e) =>
            {
               const TextFormatFlags Flags = TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix
                                                                       | TextFormatFlags.SingleLine;
               var measured = TextRenderer.MeasureText(e.Graphics, text, headerFont, Size.Empty, Flags);
               TextRenderer.DrawText(
                  e.Graphics,
                  text,
                  headerFont,
                  new Point(0, headerH - measured.Height),
                  accent,
                  Flags);
            };
         this.Controls.Add(headerPanel);
         y += headerH;

         this.Controls.Add(
            new Panel
               {
                  Location = new Point(padX + (int)Math.Round(3 * s), y),
                  Size = new Size(formW - (int)Math.Round(3 * s) - (padX * 2), SepH),
                  BackColor = border,
               });

         return y + SepH + after;
      }

      private void SettingsForm_FormClosing(object? sender, FormClosingEventArgs e)
      {
         if (!this.shouldExitApplication)
         {
            this.CloseToSystemTray(e);
         }
      }

      private int SubHeader(string text, int y, Color color)
      {
         var s = this.layoutScale;
         const int SubH = 24;
         const int After = 2;
         var subH = (int)Math.Round(SubH * s);
         var after = (int)Math.Round(After * s);
         var padX = (int)Math.Round(PadX * s);
         var formW = (int)Math.Round(FormW * s);

         var subFont = new Font(this.textFont!.Font, FontStyle.Bold);
         var subPanel = new Panel
            {
               Location = new Point(padX, y),
               Size = new Size(formW - (padX * 2), subH),
               BackColor = Color.Transparent,
            };
         subPanel.Disposed += (_, _) => subFont.Dispose();
         subPanel.Paint += (_, e) =>
            {
               const TextFormatFlags Flags = TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix
                                                                       | TextFormatFlags.SingleLine;
               var measured = TextRenderer.MeasureText(e.Graphics, text, subFont, Size.Empty, Flags);
               TextRenderer.DrawText(
                  e.Graphics,
                  text,
                  subFont,
                  new Point(0, (subH - measured.Height) / 2),
                  color,
                  Flags);
            };
         this.Controls.Add(subPanel);

         return y + subH + after;
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

      private void UpdateGridDependentControls()
      {
         var hasGrid = this.comboBoxLensGridStyle.SelectedIndex != (int)GridStyleOption.None;
         this.comboBoxLensGridSize.Enabled = hasGrid;
         this.comboBoxLensGridOpacity.Enabled = hasGrid;
      }
   }
}
