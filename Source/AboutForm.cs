// -------------------------------------------------------------------------------------
// <copyright file="AboutForm.cs">
//   Copyright (c) 2026
//   Licensed under the MIT License. See LICENSE file in the project root.
// </copyright>
// -------------------------------------------------------------------------------------

namespace StrangeLens
{
   using System;
   using System.Diagnostics;
   using System.Drawing;
   using System.Linq;
   using System.Reflection;
   using System.Windows.Forms;

   using static NativeMethods;

   internal sealed class AboutForm : Form
   {
      private const int AppIconSize = 48;

      private const int BtnSize = 20;

      private const int ControlRowHeight = 17;

      private const int FormH = 563; // exact layout height at 100 % DPI; scale with Math.Round(FormH * s)

      private const int FormW = 360;

      private const int IconGap = 16;

      /// <summary>NoPadding keeps the measured (and rendered) bounds identical to the hover/click
      ///    hit-test area -- eliminates the dead zone LinkLabel has between its hit-test rect and
      ///    the padded bounds it needs to avoid clipping the last glyph.</summary>
      private const TextFormatFlags LinkTextFlags =
         TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine;

      private const int PadX = 16;

      private const int PadY = 16;

      private const int SepW = FormW - (2 * PadX); // 328

      private const int TextW = FormW - TextX - PadX; // 264

      private const int TextX = PadX + AppIconSize + IconGap; // 80

      private const int VerticalControlGap = 3;

      // Owned by the caller — AboutForm never disposes of this.
      private readonly Icon appIconSource;

      private readonly ThemePalette palette;

      private readonly ToolTip toolTip = new();

      // Recreated in BuildLayout whenever DPI changes.
      private Bitmap? appIconBitmap;

      private FontInfo? fontBold;

      private FontInfo? fontRegular;

      private FontInfo? fontSmall;

      private VectorImage? iconDonateBuyMeACoffee;

      private VectorImage? iconDonateGitHub;

      private VectorImage? iconDonateKoFi;

      private VectorImage? iconDonatePayPal;

      private VectorImage? iconResourcesIssues;

      private VectorImage? iconResourcesSource;

      private VectorImage? imageLogo;

      // Scale factor for the current display (DeviceDpi / 96). Updated in BuildLayout.
      private float layoutScale = 1f;

      private int yLocation;

      internal AboutForm(Icon appIcon)
      {
         this.appIconSource = appIcon;
         this.palette = Lens.Instance.ActivePalette;

         this.Text = $"About {this.ProductName}";
         this.FormBorderStyle = FormBorderStyle.FixedDialog;
         this.MaximizeBox = false;
         this.MinimizeBox = false;
         this.ShowInTaskbar = false;
         this.StartPosition = FormStartPosition.CenterParent;
         this.BackColor = this.palette.Background;

         // Disable WinForms auto-scaling. WinForms scales control positions by the DPI ratio,
         // but our pixel-unit fonts don't scale with it, producing mismatched layouts. We handle
         // all DPI scaling manually in BuildLayout, called from OnHandleCreated.
         this.AutoScaleMode = AutoScaleMode.None;
      }

      protected override CreateParams CreateParams
      {
         get
         {
            // Buffers the whole window before presenting, prevents white flash
            var cp = base.CreateParams;
            cp.ExStyle |= WS_EX_COMPOSITED;
            return cp;
         }
      }

      protected override void Dispose(bool disposing)
      {
         if (disposing)
         {
            this.fontRegular?.Dispose();
            this.fontSmall?.Dispose();
            this.fontBold?.Dispose();
            this.appIconBitmap?.Dispose();
            this.toolTip.Dispose();
         }

         base.Dispose(disposing);
      }

      protected override void OnHandleCreated(EventArgs e)
      {
         base.OnHandleCreated(e);
         if (Lens.IsOsDarkMode())
         {
            var dark = 1;
            DwmSetWindowAttribute(this.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref dark, sizeof(int));
         }

         // DeviceDpi is accurate at this point for PerMonitorV2-aware processes.
         this.BuildLayout(this.DeviceDpi / 96f);
      }

      protected override void WndProc(ref Message m)
      {
         if (m.Msg == WM_DPICHANGED)
         {
            base.WndProc(ref m); // updates DeviceDpi field; may or may not resize FixedDialog

            // Read the authoritative new DPI directly from the message (HIWORD of wParam).
            // DeviceDpi property may still lag — it calls GetDpiForWindow live, which can
            // return the old value if the window hasn't fully settled on the new monitor yet.
            var newDpi = (int)(m.WParam >>> 16);
            var newScale = newDpi / 96f;

            // Resize to the fixed layout dimensions immediately, so there's no stale-size flash
            // between this message and the deferred BuildLayout. base.WndProc skips SetBoundsCore
            // for FixedDialog when AutoScaleMode is None, so we must drive the resize ourselves.
            this.ClientSize = new Size((int)Math.Round(FormW * newScale), (int)Math.Round(FormH * newScale));

            // Defer the control rebuild so this WndProc fully unwinds first — clearing controls
            // while WinForms has queued repaints for them causes drawing into disposed objects.
            // Capture newScale in the closure so BuildLayout uses the correct value regardless
            // of when BeginInvoke fires relative to further DeviceDpi updates.
            this.BeginInvoke(() => this.BuildLayout(newScale));
            return;
         }

         // Re-apply dark title bar on every focus change -- WM_NCACTIVATE fires when Windows
         // redraws the non-client area, and something (SetColorMode/WinForms internals) can
         // reset the DWM attribute before we see the message.
         if ((m.Msg == WM_NCACTIVATE) && Lens.IsOsDarkMode())
         {
            var dark = 1;
            DwmSetWindowAttribute(this.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref dark, sizeof(int));
         }

         base.WndProc(ref m);
      }

      private void AddControl(Control control)
      {
         this.Controls.Add(control);
         this.yLocation += control.Height + (int)Math.Round(VerticalControlGap * this.layoutScale);
      }

      private void AddSpace(int size)
      {
         this.yLocation += size;
      }

      private void BuildLayout(float scale)
      {
         this.layoutScale = scale;
         var s = scale;

         // -- Fonts ---------------------------------------------------------------------------------
         this.fontBold?.Dispose();
         this.fontBold = FontHelper.CreateBoldFontInfo(s);
         this.fontRegular?.Dispose();
         this.fontRegular = FontHelper.CreateRegularFontInfo(s);
         this.fontSmall?.Dispose();
         this.fontSmall = FontHelper.CreateSmallFontInfo(s);

         // -- Icons ---------------------------------------------------------------------------------
         var btnSize = (int)Math.Round(BtnSize * s);
         this.iconDonateGitHub = VectorImageFactory.AboutDonateGitHub(btnSize);
         this.iconDonatePayPal = VectorImageFactory.AboutDonatePayPal(btnSize);
         this.iconDonateKoFi = VectorImageFactory.AboutDonateKoFi(btnSize);
         this.iconDonateBuyMeACoffee = VectorImageFactory.AboutDonateBuyMeACoffee(btnSize);
         this.iconResourcesSource = VectorImageFactory.AboutResourceSource(btnSize);
         this.iconResourcesIssues = VectorImageFactory.AboutResourceIssues(btnSize);
         this.imageLogo = VectorImageFactory.AboutLogo((int)Math.Round(200 * s), (int)Math.Round(36 * s));

         this.appIconBitmap?.Dispose();
         using var icon = new Icon(
            this.appIconSource,
            (int)Math.Round(AppIconSize * s),
            (int)Math.Round(AppIconSize * s));
         this.appIconBitmap = icon.ToBitmap();

         // -- Clear previous controls ---------------------------------------------------------------
         this.toolTip.RemoveAll();
         var old = this.Controls.Cast<Control>().ToArray();
         this.Controls.Clear();
         this.yLocation = 0;
         this.SuspendLayout();
         try
         {
            foreach (var c in old)
            {
               c.Dispose();
            }

            // -- Version / metadata strings ------------------------------------------------------------
            var assembly = Assembly.GetExecutingAssembly();

            var informationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
               ?.InformationalVersion;
            var plusIdx = informationalVersion?.IndexOf('+') ?? -1;
            var rawHash = plusIdx >= 0 ? informationalVersion![(plusIdx + 1)..] : null;

            var asmBuildDate = assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
               .FirstOrDefault(a => a.Key == "BuildDate")?.Value;

            var buildVersion =
               plusIdx > 0 ? informationalVersion![..plusIdx] : informationalVersion ?? "0.0.0";
            var buildDate = string.IsNullOrEmpty(asmBuildDate) || (asmBuildDate == "dev")
               ? DateTime.Today.ToString("yyyy-MM-dd")
               : asmBuildDate;
            var commitHash = rawHash?.Length >= 7 ? rawHash[..7] : "HASH";
            var versionCopy = $"{this.ProductName} {buildVersion}\nBuilt on {buildDate} from commit {
               commitHash}";
            var copyright = assembly.GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright ?? "";

            var attributions = new[]
               {
                  "Wordmark: Slackey (fonts.google.com), Apache 2.0",
                  "Icons: Font Awesome Pro (fontawesome.com), Commercial License",
                  "Font: JetBrains Mono (jetbrains.com/lp/mono), SIL OFL 1.1",
                  "Font: Inter (rsms.me/inter), SIL OFL 1.1",
               };

            (VectorImage Icon, string Label, string Url)[] donationLinks =
               [
                  (this.iconDonateGitHub!, "GitHub Sponsors", "https://github.com/sponsors/somethingSTRANGE"),
                  (this.iconDonateBuyMeACoffee!, "Buy Me a Coffee", "https://buymeacoffee.com/strange"),
                  (this.iconDonateKoFi!, "Ko-fi", "https://ko-fi.com/somethingstrange"),
                  (this.iconDonatePayPal!, "PayPal", "https://www.paypal.com/donate/?business=JFYPDTH5TA872"),
               ];

            (VectorImage Icon, string Label, string Url)[] resourceLinks =
               [
                  (this.iconResourcesSource!, "Source Code", "https://github.com/somethingSTRANGE/Lens"),
                  (this.iconResourcesIssues!, "Report Issues",
                     "https://github.com/somethingSTRANGE/Lens/issues"),
               ];

            var scaledPadX = (int)Math.Round(PadX * s);
            var scaledPadY = (int)Math.Round(PadY * s);
            var scaledAppIconSize = (int)Math.Round(AppIconSize * s);

            // -- App icon ------------------------------------------------------------------------------
            this.Controls.Add(
               new PictureBox
                  {
                     Image = this.appIconBitmap,
                     SizeMode = PictureBoxSizeMode.Zoom,
                     Size = new Size(scaledAppIconSize, scaledAppIconSize),
                     Location = new Point(scaledPadX, scaledPadY),
                     BackColor = this.palette.Background,
                  });

            // -- Logo ----------------------------------------------------------------------------------
            this.AddSpace(scaledPadY);
            this.AddControl(
               this.VectorImage(this.imageLogo!, this.palette.TextStrong, this.palette.AccentSubtle, 0.5f));

            // -- Version & metadata --------------------------------------------------------------------
            this.AddSpace((int)Math.Round(8 * s));
            this.AddControl(this.LabelBuildVersion($"Version {buildVersion}"));
            this.AddControl(this.LabelBuildDate($"{buildDate}"));

            // -- Resources -----------------------------------------------------------------------------
            this.AddSpace((int)Math.Round((PadY + PadY) * s));
            this.AddControl(this.LabelHeader("Resources"));
            foreach (var link in resourceLinks)
            {
               this.AddControl(this.LinkButton(link.Icon, link.Label, link.Url));
            }

            // -- Donation links ------------------------------------------------------------------------
            this.AddSpace(scaledPadY);
            this.AddControl(this.LabelHeader("Give support and donate"));
            foreach (var link in donationLinks)
            {
               this.AddControl(this.LinkButton(link.Icon, link.Label, link.Url));
            }

            // -- Copyright -----------------------------------------------------------------------------
            this.AddSpace((int)Math.Round((PadY + PadY) * s));
            this.AddControl(this.Separator());
            this.AddSpace(scaledPadY);
            this.AddControl(this.LabelCopyright($"{copyright} — MIT License"));

            // -- Attribution ---------------------------------------------------------------------------
            this.AddSpace((int)Math.Round(8 * s));
            foreach (var attribution in attributions)
            {
               this.AddControl(this.LabelAttribution(attribution));
            }

            // -- Buttons -------------------------------------------------------------------------------
            this.AddSpace((int)Math.Round(PadY * 2 * s));
            this.AddControl(this.ButtonsPanel(versionCopy));

            this.ClientSize = new Size((int)Math.Round(FormW * s), (int)Math.Round(FormH * s));
         }
         finally
         {
            this.ResumeLayout(performLayout: true);
         }
      }

      private Panel ButtonsPanel(string versionLine)
      {
         const string CopyAndCloseText = "Copy and Close";
         const TextFormatFlags BtnFlags =
            TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine;
         var s = this.layoutScale;
         var buttonH = (int)Math.Round(26 * s);
         var closeW = (int)Math.Round(80 * s);
         var sepW = (int)Math.Round(SepW * s);
         var padX = (int)Math.Round(PadX * s);
         var penWidth = Math.Max(1, (int)Math.Round(s));

         // Capture font into a local so Paint closures bypass WinForms Font-property scaling.
         var capturedFont = this.fontRegular!.Font;
         var copyAndCloseWidth = TextRenderer.MeasureText(CopyAndCloseText, capturedFont).Width
                                 + (int)Math.Round(24 * s);

         var panel = new Panel
            {
               Location = new Point(padX, this.yLocation),
               Size = new Size(sepW, buttonH),
               BackColor = Color.Transparent,
            };

         // -- Close button -----------------------------------------------------------------
         var closeBg = this.palette.Control;
         var closeBgHover = this.palette.Border;
         var closeTextColor = this.palette.TextNormal;
         var closeBorderColor = this.palette.Border;

         var closeHovered = false;
         var closeFocused = false;

         var closeBtn = new Button
            {
               Text = string.Empty,
               Location = new Point(panel.Width - closeW, 0),
               Size = new Size(closeW, buttonH),
               FlatStyle = FlatStyle.Flat,
               Cursor = Cursors.Hand,
            };
         closeBtn.FlatAppearance.BorderSize = 0;

         closeBtn.Paint += (_, e) =>
            {
               var g = e.Graphics;
               g.Clear(closeHovered ? closeBgHover : closeBg);
               using var pen = new Pen(closeBorderColor, penWidth);
               g.DrawRectangle(pen, 0, 0, closeBtn.Width - 1, closeBtn.Height - 1);
               var measured = TextRenderer.MeasureText(g, "Close", capturedFont, Size.Empty, BtnFlags);
               TextRenderer.DrawText(
                  g,
                  "Close",
                  capturedFont,
                  new Point((closeBtn.Width - measured.Width) / 2, (closeBtn.Height - measured.Height) / 2),
                  closeTextColor,
                  BtnFlags);
               if (closeFocused)
               {
                  ControlPaint.DrawFocusRectangle(g, Rectangle.Inflate(closeBtn.ClientRectangle, -2, -2));
               }
            };
         closeBtn.MouseEnter += (_, _) =>
            {
               closeHovered = true;
               closeBtn.Invalidate();
            };
         closeBtn.MouseLeave += (_, _) =>
            {
               closeHovered = false;
               closeBtn.Invalidate();
            };
         closeBtn.GotFocus += (_, _) =>
            {
               closeFocused = true;
               closeBtn.Invalidate();
            };
         closeBtn.LostFocus += (_, _) =>
            {
               closeFocused = false;
               closeBtn.Invalidate();
            };
         closeBtn.Click += (_, _) => this.Close();

         // -- Copy and Close button --------------------------------------------------------
         var copyBgNormal = this.palette.AccentSubtle;
         var copyBgHover = this.palette.AccentNormal;
         var copyBgPress = this.palette.AccentStrong;
         var copyTextColor = this.palette.TextStrong;

         var copyHovered = false;
         var copyPressed = false;
         var copyFocused = false;

         var copyAndCloseBtn = new Button
            {
               Text = string.Empty,
               Location = new Point(closeBtn.Left - padX - copyAndCloseWidth, 0),
               Size = new Size(copyAndCloseWidth, buttonH),
               FlatStyle = FlatStyle.Flat,
               Cursor = Cursors.Hand,
            };
         copyAndCloseBtn.FlatAppearance.BorderSize = 0;

         copyAndCloseBtn.Paint += (_, e) =>
            {
               var g = e.Graphics;
               g.Clear(copyPressed ? copyBgPress : copyHovered ? copyBgHover : copyBgNormal);
               var measured = TextRenderer.MeasureText(
                  g,
                  CopyAndCloseText,
                  capturedFont,
                  Size.Empty,
                  BtnFlags);
               TextRenderer.DrawText(
                  g,
                  CopyAndCloseText,
                  capturedFont,
                  new Point(
                     (copyAndCloseBtn.Width - measured.Width) / 2,
                     (copyAndCloseBtn.Height - measured.Height) / 2),
                  copyTextColor,
                  BtnFlags);
               if (copyFocused)
               {
                  ControlPaint.DrawFocusRectangle(
                     g,
                     Rectangle.Inflate(copyAndCloseBtn.ClientRectangle, -2, -2));
               }
            };
         copyAndCloseBtn.MouseEnter += (_, _) =>
            {
               copyHovered = true;
               copyAndCloseBtn.Invalidate();
            };
         copyAndCloseBtn.MouseLeave += (_, _) =>
            {
               copyHovered = false;
               copyPressed = false;
               copyAndCloseBtn.Invalidate();
            };
         copyAndCloseBtn.MouseDown += (_, _) =>
            {
               copyPressed = true;
               copyAndCloseBtn.Invalidate();
            };
         copyAndCloseBtn.MouseUp += (_, _) =>
            {
               copyPressed = false;
               copyAndCloseBtn.Invalidate();
            };
         copyAndCloseBtn.GotFocus += (_, _) =>
            {
               copyFocused = true;
               copyAndCloseBtn.Invalidate();
            };
         copyAndCloseBtn.LostFocus += (_, _) =>
            {
               copyFocused = false;
               copyAndCloseBtn.Invalidate();
            };
         copyAndCloseBtn.Click += (_, _) =>
            {
               Clipboard.SetText(versionLine);
               this.Close();
            };
         this.toolTip.SetToolTip(copyAndCloseBtn, "Copy version info and close this window");

         // Defer: copyAndCloseBtn isn't in the form hierarchy until the caller adds the panel.
         this.BeginInvoke(() =>
            {
               if (!this.IsDisposed && this.Contains(copyAndCloseBtn))
               {
                  this.ActiveControl = copyAndCloseBtn;
               }
            });

         panel.Controls.Add(copyAndCloseBtn);
         panel.Controls.Add(closeBtn);
         return panel;
      }

      private Panel LabelAttribution(string text)
      {
         var s = this.layoutScale;
         return this.TextPanel(
            text,
            this.fontSmall!.Font,
            this.palette.TextSubtle,
            (int)Math.Round(PadX * s),
            (int)Math.Round(SepW * s),
            center: true);
      }

      private Panel LabelBuildDate(string text)
      {
         var s = this.layoutScale;
         return this.TextPanel(
            text,
            this.fontRegular!.Font,
            this.palette.TextSubtle,
            (int)Math.Round(PadX * s),
            (int)Math.Round(SepW * s),
            center: true);
      }

      private Panel LabelBuildVersion(string text)
      {
         var s = this.layoutScale;
         return this.TextPanel(
            text,
            this.fontBold!.Font,
            this.palette.TextSubtle,
            (int)Math.Round(PadX * s),
            (int)Math.Round(SepW * s),
            center: true);
      }

      private Panel LabelCopyright(string text)
      {
         var s = this.layoutScale;
         return this.TextPanel(
            text,
            this.fontRegular!.Font,
            this.palette.TextSubtle,
            (int)Math.Round(PadX * s),
            (int)Math.Round(SepW * s),
            center: true);
      }

      private Panel LabelHeader(string label)
      {
         var s = this.layoutScale;
         return this.TextPanel(
            label,
            this.fontBold!.Font,
            this.palette.AccentNormal,
            (int)Math.Round(TextX * s),
            (int)Math.Round(TextW * s),
            center: false);
      }

      private Panel LinkButton(VectorImage icon, string label, string url)
      {
         var s = this.layoutScale;
         var scaledTextX = (int)Math.Round(TextX * s);
         var scaledTextW = (int)Math.Round(TextW * s);
         var scaledBtnSize = (int)Math.Round(BtnSize * s);
         var scaledPadX = (int)Math.Round(PadX * s);
         var scaledGap = (int)Math.Round(6 * s);
         var scaledNudge = (int)Math.Round(2 * s);

         var panel = new Panel
            {
               Location = new Point(scaledTextX, this.yLocation),
               Size = new Size(scaledTextW, scaledBtnSize),
               BackColor = Color.Transparent,
            };

         Action onClick = () => Process.Start(
            new ProcessStartInfo(url)
               {
                  UseShellExecute = true,
               });

         var iconButton = this.MakeIconButton(
            icon,
            this.palette.TextNormal,
            this.palette.AccentStrong,
            onClick);
         iconButton.Location = new Point(scaledPadX, 0);
         panel.Controls.Add(iconButton);
         this.toolTip.SetToolTip(iconButton, url);

         var linkButton = this.MakeLinkButton(
            label,
            this.palette.TextNormal,
            this.palette.AccentStrong,
            this.palette.Control,
            onClick);
         linkButton.Location = new Point(
            scaledPadX + scaledBtnSize + scaledGap,
            ((iconButton.Height - linkButton.Height) / 2) + scaledNudge);
         panel.Controls.Add(linkButton);
         this.toolTip.SetToolTip(linkButton, url);

         return panel;
      }

      private Button MakeIconButton(
         VectorImage icon,
         Color color,
         Color hoverColor,
         Action onClick,
         Action? onEnter = null,
         Action? onLeave = null)
      {
         var btn = new Button
            {
               Size = new Size(icon.Width, icon.Height),
               BackColor = this.BackColor,
               FlatStyle = FlatStyle.Flat,
               Cursor = Cursors.Hand,
               TabStop = false,
            };
         btn.FlatAppearance.BorderSize = 0;
         btn.FlatAppearance.MouseOverBackColor = this.BackColor;

         var hovered = false;
         btn.Paint += (_, e) =>
            {
               e.Graphics.Clear(btn.BackColor);
               icon.Draw(e.Graphics, hovered ? hoverColor : color, 0, 0);
            };
         btn.MouseEnter += (_, _) =>
            {
               hovered = true;
               btn.Invalidate();
               onEnter?.Invoke();
            };
         btn.MouseLeave += (_, _) =>
            {
               hovered = false;
               btn.Invalidate();
               onLeave?.Invoke();
            };
         btn.Click += (_, _) => onClick();
         return btn;
      }

      private Button MakeLinkButton(
         string text,
         Color color,
         Color hoverColor,
         Color underlineColor,
         Action onClick)
      {
         var nudge = (int)Math.Round(2 * this.layoutScale);
         var penWidth = Math.Max(1, (int)Math.Round(this.layoutScale));
         var textRect = new Rectangle(
            new Point(0, -nudge),
            TextRenderer.MeasureText(text, this.fontRegular!.Font, Size.Empty, LinkTextFlags));
         textRect.Height -= nudge;

         var btn = new Button
            {
               Size = textRect.Size,
               BackColor = this.BackColor,
               FlatStyle = FlatStyle.Flat,
               Cursor = Cursors.Hand,
               Margin = new Padding(),
               TabStop = true,
            };
         btn.FlatAppearance.BorderSize = 0;
         btn.FlatAppearance.MouseOverBackColor = this.BackColor;

         var hovered = false;
         var focused = false;

         var underlineY = textRect.Height - 1;
         textRect.Height += nudge;

         btn.Paint += (_, e) =>
            {
               var g = e.Graphics;
               g.Clear(btn.BackColor);
               if (hovered)
               {
                  using var pen = new Pen(underlineColor, penWidth);
                  g.DrawLine(pen, 0, underlineY, textRect.Width - 1, underlineY);
               }

               TextRenderer.DrawText(
                  g,
                  text,
                  this.fontRegular!.Font,
                  textRect,
                  hovered ? hoverColor : color,
                  LinkTextFlags);
               if (focused)
               {
                  ControlPaint.DrawFocusRectangle(g, Rectangle.Inflate(btn.ClientRectangle, -1, -1));
               }
            };
         btn.MouseEnter += (_, _) =>
            {
               hovered = true;
               btn.Invalidate();
            };
         btn.MouseLeave += (_, _) =>
            {
               hovered = false;
               btn.Invalidate();
            };
         btn.GotFocus += (_, _) =>
            {
               focused = true;
               btn.Invalidate();
            };
         btn.LostFocus += (_, _) =>
            {
               focused = false;
               btn.Invalidate();
            };
         btn.Click += (_, _) => onClick();
         return btn;
      }

      private Panel Separator()
      {
         var s = this.layoutScale;
         return new Panel
            {
               BackColor = this.palette.Border,
               Location = new Point((int)Math.Round(PadX * s), this.yLocation),
               Size = new Size((int)Math.Round(SepW * s), Math.Max(1, (int)Math.Round(s))),
            };
      }

      private Panel VectorImage(VectorImage image, Color color, Color shadowColor, float shadowOpacity)
      {
         var s = this.layoutScale;
         var shadowDx = (int)Math.Round(2 * s);
         var shadowDy = (int)Math.Round(3 * s);
         var panel = new Panel
            {
               Size = new Size(image.Width, image.Height),
               Location = new Point((int)Math.Round(TextX * s), this.yLocation),
               BackColor = Color.Transparent,
            };
         panel.Paint += (_, e) =>
            {
               var shadow = Color.FromArgb((byte)(shadowOpacity * 255), shadowColor);
               image.Draw(e.Graphics, shadow, shadowDx, shadowDy);
               image.Draw(e.Graphics, color, 0, 0);
            };
         return panel;
      }

      // Renders static text via TextRenderer.DrawText in a Paint event, bypassing the WinForms
      // Label.Font-property scaling that fires when a control's HWND is created on a monitor
      // whose DPI differs from the system (primary-monitor) DPI.
      private Panel TextPanel(string text, Font font, Color color, int x, int width, bool center)
      {
         var height = (int)Math.Round(ControlRowHeight * this.layoutScale);
         var panel = new Panel
            {
               Location = new Point(x, this.yLocation),
               Size = new Size(width, height),
               BackColor = Color.Transparent,
            };
         panel.Paint += (_, e) =>
            {
               const TextFormatFlags Flags = TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix
                                                                       | TextFormatFlags.SingleLine;
               var measured = TextRenderer.MeasureText(e.Graphics, text, font, Size.Empty, Flags);
               var drawX = center ? (width - measured.Width) / 2 : 0;
               var drawY = (height - measured.Height) / 2;
               TextRenderer.DrawText(e.Graphics, text, font, new Point(drawX, drawY), color, Flags);
            };
         return panel;
      }
   }
}
