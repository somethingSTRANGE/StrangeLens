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
   using System.Runtime.InteropServices;
   using System.Windows.Forms;

   internal sealed class AboutForm : Form
   {
      private const int AppIconSize = 48;

      private const int BtnSize = 20;

      private const int ControlRowHeight = 17;

      /// <summary>
      ///    <para>Desktop Window Manager (DWM) attribute applied to a window.</para>
      ///    <para>Use with DwmSetWindowAttribute. Allows the window frame for this window to be
      ///       drawn in dark mode colors when the dark mode system setting is enabled. For
      ///       compatibility reasons, all windows default to light mode regardless of the system
      ///       setting. The pvAttribute parameter points to a value of type BOOL. TRUE to honor
      ///       dark mode for the window, FALSE to always use light mode.</para>
      ///    <para>This value is supported starting with Windows 11 Build 22000.</para>
      /// </summary>
      private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

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

      /// <summary>Paints all descendants of a window in bottom-to-top painting order using
      ///    double-buffering. Bottom-to-top painting order allows a descendent window to have
      ///    translucency (alpha) and transparency (color-key) effects, but only if the descendent
      ///    window also has the WS_EX_TRANSPARENT bit set. Double-buffering allows the window and
      ///    its descendents to be painted without a flicker.</summary>
      private const int WS_EX_COMPOSITED = 0x02000000;

      private readonly Bitmap appIconBitmap;

      private readonly FontInfo fontBold;

      private readonly FontInfo fontRegular;

      private readonly FontInfo fontSmall;

      private readonly SvgImage iconDonateBuyMeACoffee;

      private readonly SvgImage iconDonateGitHub;

      private readonly SvgImage iconDonateKoFi;

      private readonly SvgImage iconDonatePayPal;

      private readonly SvgImage iconResourcesIssues;

      private readonly SvgImage iconResourcesSource;

      private readonly SvgImage imageLogo;

      private readonly ThemePalette palette;

      private readonly ToolTip toolTip = new();

      private int yLocation;

      internal AboutForm(Icon appIcon)
      {
         this.fontBold = FontHelper.CreateBoldFontInfo();
         this.fontRegular = FontHelper.CreateRegularFontInfo();
         this.fontSmall = FontHelper.CreateSmallFontInfo();

         this.palette = Lens.Instance.ActivePalette;

         this.iconDonateGitHub = SvgImageFactory.AboutDonateGitHub(BtnSize);
         this.iconDonatePayPal = SvgImageFactory.AboutDonatePayPal(BtnSize);
         this.iconDonateKoFi = SvgImageFactory.AboutDonateKoFi(BtnSize);
         this.iconDonateBuyMeACoffee = SvgImageFactory.AboutDonateBuyMeACoffee(BtnSize);
         this.iconResourcesSource = SvgImageFactory.AboutResourceSource(BtnSize);
         this.iconResourcesIssues = SvgImageFactory.AboutResourceIssues(BtnSize);
         this.imageLogo = SvgImageFactory.AboutLogo(200, 36);

         using var icon48 = new Icon(appIcon, AppIconSize, AppIconSize);
         this.appIconBitmap = icon48.ToBitmap();

         this.Text = $"About {this.ProductName}";
         this.FormBorderStyle = FormBorderStyle.FixedDialog;
         this.MaximizeBox = false;
         this.MinimizeBox = false;
         this.ShowInTaskbar = false;
         this.StartPosition = FormStartPosition.CenterParent;
         this.BackColor = this.palette.Background;

         this.BuildLayout();
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
            this.fontRegular.Dispose();
            this.fontSmall.Dispose();
            this.fontBold.Dispose();
            this.appIconBitmap.Dispose();
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
      }

      protected override void WndProc(ref Message m)
      {
         const int WmNcActivate = 0x0086;

         // Re-apply dark title bar on every focus change -- WM_NCACTIVATE fires when Windows
         // redraws the non-client area, and something (SetColorMode/WinForms internals) can
         // reset the DWM attribute before we see the message.
         if ((m.Msg == WmNcActivate) && Lens.IsOsDarkMode())
         {
            var dark = 1;
            DwmSetWindowAttribute(this.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref dark, sizeof(int));
         }

         base.WndProc(ref m);
      }

      [DllImport("dwmapi.dll")]
      private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

      private void AddControl(Control control)
      {
         this.Controls.Add(control);
         this.yLocation += control.Height + VerticalControlGap;
      }

      private void AddSpace(int size = 8)
      {
         this.yLocation += size;
      }

      private void BuildLayout()
      {
         var assembly = Assembly.GetExecutingAssembly();

         var informationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion;
         var plusIdx = informationalVersion?.IndexOf('+') ?? -1;
         var rawHash = plusIdx >= 0 ? informationalVersion![(plusIdx + 1)..] : null;

         var asmBuildDate = assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(a => a.Key == "BuildDate")?.Value;

         var buildVersion = plusIdx > 0 ? informationalVersion![..plusIdx] : informationalVersion ?? "0.0.0";
         var buildDate = string.IsNullOrEmpty(asmBuildDate) || (asmBuildDate == "dev")
            ? DateTime.Today.ToString("yyyy-MM-dd")
            : asmBuildDate;
         var commitHash = rawHash?.Length >= 7 ? rawHash[..7] : "HASH";
         var versionCopy = $"{this.ProductName} {buildVersion}\nBuilt on {buildDate} from commit {commitHash
         }";
         var copyright = assembly.GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright ?? "";

         var attributions = new[]
            {
               "Wordmark: Slackey (fonts.google.com), Apache 2.0",
               "Icons: Font Awesome Free (fontawesome.com), CC BY 4.0",
               "Font: JetBrains Mono (jetbrains.com/lp/mono), SIL OFL 1.1",
               "Font: Inter (rsms.me/inter), SIL OFL 1.1",
            };

         (SvgImage Icon, string Label, string Url)[] donationLinks =
            [
               (this.iconDonateGitHub, "GitHub Sponsors", "https://github.com/sponsors/somethingSTRANGE"),
               (this.iconDonateBuyMeACoffee, "Buy Me a Coffee", "https://buymeacoffee.com/strange"),
               (this.iconDonateKoFi, "Ko-fi", "https://ko-fi.com/somethingstrange"),
               (this.iconDonatePayPal, "PayPal", "https://www.paypal.com/donate/?business=JFYPDTH5TA872"),
            ];

         (SvgImage Icon, string Label, string Url)[] resourceLinks =
            [
               (this.iconResourcesSource, "Source Code", "https://github.com/somethingSTRANGE/Lens"),
               (this.iconResourcesIssues, "Report Issues", "https://github.com/somethingSTRANGE/Lens/issues"),
            ];

         // -- Icon -------------------------------------------------------------------------------
         this.Controls.Add(
            new PictureBox
               {
                  Image = this.appIconBitmap,
                  SizeMode = PictureBoxSizeMode.Zoom,
                  Size = new Size(AppIconSize, AppIconSize),
                  Location = new Point(PadX, PadY),
                  BackColor = this.palette.Background,
               });

         // -- Header -----------------------------------------------------------------------------
         this.AddSpace(PadY);
         this.AddControl(
            this.SvgImage(this.imageLogo, this.palette.TextStrong, this.palette.AccentSubtle, 0.5f));

         // -- Version & metadata -----------------------------------------------------------------
         this.AddSpace();
         this.AddControl(this.LabelBuildVersion($"Version {buildVersion}"));
         this.AddControl(this.LabelBuildDate($"{buildDate}"));

         // -- Product Links ----------------------------------------------------------------------
         this.AddSpace(PadY + PadY);
         this.AddControl(this.LabelHeader("Resources"));
         foreach (var link in resourceLinks)
         {
            this.AddControl(this.LinkButton(link.Icon, link.Label, link.Url));
         }

         // -- Donation Links ---------------------------------------------------------------------
         this.AddSpace(PadY);
         this.AddControl(this.LabelHeader("Give support and donate"));
         foreach (var link in donationLinks)
         {
            this.AddControl(this.LinkButton(link.Icon, link.Label, link.Url));
         }

         // -- Copyright --------------------------------------------------------------------------
         this.AddSpace(PadY + PadY);
         this.AddControl(this.Separator());
         this.AddSpace(PadY);
         this.AddControl(this.LabelCopyright($"{copyright} — MIT License"));

         // -- Attribution ------------------------------------------------------------------------
         this.AddSpace();
         foreach (var attribution in attributions)
         {
            this.AddControl(this.LabelAttribution(attribution));
         }

         // -- Actions ----------------------------------------------------------------------------
         this.AddSpace(PadY * 2);
         this.AddControl(this.ButtonsPanel(versionCopy));

         this.ClientSize = new Size(FormW, this.yLocation + PadY);
      }

      private Panel ButtonsPanel(string versionLine)
      {
         const int ButtonHeight = 26;
         const int CloseWidth = 80;
         const string CopyAndCloseText = "Copy and Close";

         var copyAndCloseWidth = TextRenderer.MeasureText(CopyAndCloseText, this.fontRegular.Font).Width + 24;

         var panel = new Panel
            {
               Location = new Point(PadX, this.yLocation),
               Size = new Size(SepW, ButtonHeight),
               BackColor = Color.Transparent,
            };

         var closeBtn = new Button
            {
               Font = this.fontRegular.Font,
               Text = "Close",
               BackColor = this.palette.Control,
               ForeColor = this.palette.TextNormal,
               Location = new Point(panel.Width - CloseWidth, 0),
               Size = new Size(CloseWidth, ButtonHeight),
               FlatStyle = FlatStyle.Flat,
            };
         closeBtn.FlatAppearance.BorderColor = this.palette.Border;
         closeBtn.FlatAppearance.MouseOverBackColor = this.palette.Border;
         closeBtn.Click += (_, _) => this.Close();

         var copyAndCloseBtn = new Button
            {
               Font = this.fontRegular.Font,
               Text = CopyAndCloseText,
               BackColor = this.palette.AccentSubtle,
               ForeColor = this.palette.TextStrong,
               Location = new Point(closeBtn.Left - PadX - copyAndCloseWidth, 0),
               Size = new Size(copyAndCloseWidth, ButtonHeight),
               FlatStyle = FlatStyle.Flat,
            };
         copyAndCloseBtn.FlatAppearance.BorderSize = 0;
         copyAndCloseBtn.FlatAppearance.MouseOverBackColor = this.palette.AccentNormal;
         copyAndCloseBtn.FlatAppearance.MouseDownBackColor = this.palette.AccentStrong;
         copyAndCloseBtn.Click += (_, _) =>
            {
               Clipboard.SetText(versionLine);
               this.Close();
            };
         this.toolTip.SetToolTip(copyAndCloseBtn, "Copy version info and close this window");

         // Give the call-to-action initial keyboard focus instead of whatever control
         // happens to be first in tab order. Deferred to Load since ActiveControl only
         // takes effect once the control is actually parented into the form.
         this.Load += (_, _) => this.ActiveControl = copyAndCloseBtn;

         panel.Controls.Add(copyAndCloseBtn);
         panel.Controls.Add(closeBtn);
         return panel;
      }

      private Label LabelAttribution(string text)
      {
         var control = new Label
            {
               Text = text,
               Font = this.fontSmall.Font,
               ForeColor = this.palette.TextSubtle,
               BackColor = Color.Transparent,
               Location = new Point(PadX, this.yLocation),
               Size = new Size(SepW, this.fontSmall.PixelLineHeight),
               TextAlign = ContentAlignment.MiddleCenter,
            };
         return control;
      }

      private Label LabelBuildDate(string text)
      {
         return new Label
            {
               Text = text,
               Font = this.fontRegular.Font,
               ForeColor = this.palette.TextSubtle,
               BackColor = Color.Transparent,
               Location = new Point(PadX, this.yLocation),
               Size = new Size(SepW, ControlRowHeight),
               TextAlign = ContentAlignment.MiddleCenter,
               Padding = Padding.Empty,
            };
      }

      private Label LabelBuildVersion(string text)
      {
         return new Label
            {
               Text = text,
               Font = this.fontBold.Font,
               ForeColor = this.palette.TextSubtle,
               BackColor = Color.Transparent,
               Location = new Point(PadX, this.yLocation),
               Size = new Size(SepW, ControlRowHeight),
               TextAlign = ContentAlignment.MiddleCenter,
               Padding = Padding.Empty,
            };
      }

      private Label LabelCopyright(string text)
      {
         return new Label
            {
               Text = text,
               Font = this.fontRegular.Font,
               ForeColor = this.palette.TextSubtle,
               BackColor = Color.Transparent,
               Location = new Point(PadX, this.yLocation),
               Size = new Size(SepW, ControlRowHeight),
               TextAlign = ContentAlignment.MiddleCenter,
               Padding = Padding.Empty,
            };
      }

      private Label LabelHeader(string label)
      {
         return new Label
            {
               Text = label,
               Font = this.fontBold.Font,
               ForeColor = this.palette.AccentNormal,
               BackColor = Color.Transparent,
               Location = new Point(TextX, this.yLocation),
               Size = new Size(TextW, ControlRowHeight),
               TextAlign = ContentAlignment.MiddleLeft,
               Padding = Padding.Empty,
            };
      }

      private Panel LinkButton(SvgImage icon, string label, string url)
      {
         var panel = new Panel
            {
               Location = new Point(TextX, this.yLocation),
               Size = new Size(TextW, 20),
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
         iconButton.Location = new Point(PadX, 0);
         panel.Controls.Add(iconButton);
         this.toolTip.SetToolTip(iconButton, url);

         var linkButton = this.MakeLinkButton(
            label,
            this.palette.TextNormal,
            this.palette.AccentStrong,
            this.palette.Control,
            onClick);
         linkButton.Location = new Point(PadX + BtnSize + 6, 0 + ((BtnSize - linkButton.Height) / 2) + 2);
         panel.Controls.Add(linkButton);
         this.toolTip.SetToolTip(linkButton, url);

         return panel;
      }

      private Button MakeIconButton(
         SvgImage icon,
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
         var textRect = new Rectangle(
            new Point(0, -2),
            TextRenderer.MeasureText(text, this.fontRegular.Font, Size.Empty, LinkTextFlags));
         textRect.Height -= 2;

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
         textRect.Height += 2;

         btn.Paint += (_, e) =>
            {
               var g = e.Graphics;
               g.Clear(btn.BackColor);
               if (hovered)
               {
                  using var pen = new Pen(underlineColor);
                  g.DrawLine(pen, 0, underlineY, textRect.Width - 1, underlineY);
               }

               TextRenderer.DrawText(
                  g,
                  text,
                  this.fontRegular.Font,
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
         var control = new Panel
            {
               BackColor = this.palette.Border,
               Location = new Point(PadX, this.yLocation),
               Size = new Size(SepW, 1),
            };
         return control;
      }

      private Panel SvgImage(SvgImage image, Color color, Color shadowColor, float shadowOpacity)
      {
         var location = new Point(TextX, this.yLocation);
         var panel = new Panel
            {
               Size = new Size(image.Width, image.Height),
               Location = location,
               // BackColor = this.BackColor,
               BackColor = Color.Transparent,
            };
         panel.Paint += (_, e) =>
            {
               shadowColor = Color.FromArgb((byte)(shadowOpacity * 255), shadowColor);
               image.Draw(e.Graphics, shadowColor, 2, 3);
               image.Draw(e.Graphics, color, 0, 0);
            };
         return panel;
      }
   }
}
