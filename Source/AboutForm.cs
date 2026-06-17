// -------------------------------------------------------------------------------------
// <copyright file="AboutForm.cs" company="Greyborn Studios LLC">
//   Copyright 2015-2026 Greyborn Studios LLC. All rights reserved.
// </copyright>
// -------------------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Lens
{
   internal sealed class AboutForm : Form
   {
      private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

      private const int FormW = 360;
      private const int PadX = 16;
      private const int PadY = 16;
      private const int AppIconSize = 48;
      private const int IconGap = 16;
      private const int TextX = PadX + AppIconSize + IconGap; // 80
      private const int BtnSize = 20;

      private const string GitHubUrl = "https://github.com/somethingSTRANGE/Lens";

      private static readonly (string Label, string Url)[] DonateLinks =
         [
            // ("GitHub Sponsors", "https://github.com/sponsors/somethingSTRANGE"),
            // ("Buy Me a Coffee", "https://buymeacoffee.com/strange"),
            ("Ko-fi", "https://ko-fi.com/somethingstrange"),
            ("PayPal", "https://www.paypal.com/donate/?business=JFYPDTH5TA872")
         ];

      private readonly SvgImage iconCopy;
      private readonly SvgImage iconDonateBuyMeACoffee;

      private readonly SvgImage iconDonateGitHub;
      private readonly SvgImage iconDonateKoFi;
      private readonly SvgImage iconDonatePayPal;

      private readonly SvgImage iconWebsite;

      private readonly SvgImage imageLogo;
      private readonly SvgImage imageLogoCode;
      private Bitmap appIconBitmap;
      private Font attrFont;

      private Font textFont;
      private Font titleFont;


      internal AboutForm(Icon appIcon)
      {
         this.textFont = FontHelper.CreateLabelFont();
         this.titleFont = new Font(this.textFont.FontFamily, 22f, FontStyle.Regular, GraphicsUnit.Pixel);
         this.attrFont = new Font(this.textFont.FontFamily, 11f, FontStyle.Regular, GraphicsUnit.Pixel);


         // gpLogoCode = SvgImageFactory.LogoCode(width, height)
         // this.iconDonateGitHub = SvgImageFactory.DonateGitHub(BtnSize);
         this.iconDonatePayPal = SvgImageFactory.DonatePayPal(BtnSize);
         this.iconDonateKoFi = SvgImageFactory.DonateKoFi(BtnSize);
         // this.iconDonateBuyMeACoffee = SvgImageFactory.DonateBuyMeACoffee(BtnSize);
         this.iconWebsite = SvgImageFactory.Website(BtnSize);
         this.iconCopy = SvgImageFactory.Copy(BtnSize);
         this.imageLogo = SvgImageFactory.Logo(200, 24);
         this.imageLogoCode = SvgImageFactory.LogoCode(200, 2);

         using var icon48 = new Icon(appIcon, AppIconSize, AppIconSize);
         this.appIconBitmap = icon48.ToBitmap();

         this.Text = "About Lens";
         this.FormBorderStyle = FormBorderStyle.FixedDialog;
         this.MaximizeBox = false;
         this.MinimizeBox = false;
         this.ShowInTaskbar = false;
         this.StartPosition = FormStartPosition.CenterParent;

         this.BuildLayout();
      }

      [DllImport("dwmapi.dll")]
      private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

      protected override void OnHandleCreated(EventArgs e)
      {
         base.OnHandleCreated(e);
         if (Lens.IsOsDarkMode())
         {
            var dark = 1;
            DwmSetWindowAttribute(this.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref dark, sizeof(int));
         }
      }

      private void BuildLayout()
      {
         var assembly = Assembly.GetExecutingAssembly();
         var palette = Lens.Instance.ActivePalette;

         var infoVer = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion.Substring(0, 14) ?? "VERSION";
         var buildDate = assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(a => a.Key == "BuildDate")?.Value ?? "";
         var copyright = assembly.GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright ?? "";

         var versionLine = string.IsNullOrEmpty(buildDate) || buildDate == "dev"
            ? infoVer
            : $"{infoVer} — {buildDate}";

         versionLine = $"{buildDate} — {infoVer}";

         this.BackColor = palette.Background;

         var textW = FormW - TextX - PadX;
         var y = PadY;

         // ── Header: app icon + logo + copy button ─────────────────────────────
         var iconBox = new PictureBox
            {
               Image = this.appIconBitmap,
               SizeMode = PictureBoxSizeMode.Zoom,
               Size = new Size(AppIconSize, AppIconSize),
               Location = new Point(PadX, y),
               BackColor = palette.Background
            };
         this.Controls.Add(iconBox);

         y += AppIconSize;
         var logoPanel = new Panel
            {
               Size = new Size(this.imageLogo.Width, this.imageLogo.Height),
               // Location  = new Point(TextX, y - this.imageLogo.Height - 2),
               Location = new Point(TextX, y - AppIconSize),
               BackColor = this.BackColor
            };
         logoPanel.Paint += (_, e) =>
         {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var brush = new SolidBrush(palette.TextStrong);
            e.Graphics.FillPath(brush, this.imageLogo.Path);
         };
         this.Controls.Add(logoPanel);

         y += PadY - this.imageLogo.Height;
         var logoCodePanel = new Panel
            {
               Size = new Size(this.imageLogoCode.Width, this.imageLogoCode.Height),
               Location = new Point(TextX, y - PadY / 2 - 2),
               BackColor = this.BackColor
            };
         logoCodePanel.Paint += (_, e) =>
         {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var brush = new SolidBrush(palette.Border);
            e.Graphics.FillPath(brush, this.imageLogoCode.Path);
         };
         this.Controls.Add(logoCodePanel);

         var copyBtn = this.MakeIconButton(
            this.iconCopy, palette.TextSubtle, palette.Accent,
            () => Clipboard.SetText($"Lens{Environment.NewLine}{versionLine}"));
         copyBtn.Location = new Point(FormW - PadX - BtnSize, y + BtnSize);
         this.Controls.Add(copyBtn);

         y -= 8;

         // ── Version ────────────────────────────────────────────────────────────
         this.AddLabel(versionLine, this.textFont, palette.TextSubtle, textW, TextX, ref y, 14);

         // ── Donate buttons ─────────────────────────────────────────────────────
         var hoverLabel = new Label
            {
               Text = "",
               Font = this.textFont,
               ForeColor = palette.TextSubtle,
               BackColor = palette.Background,
               AutoSize = false,
               Size = new Size(textW, 20),
               TextAlign = ContentAlignment.MiddleLeft
            };

         var btnX = TextX;
         foreach (var (label, url) in DonateLinks)
         {
            var capturedUrl = url;
            var capturedLabel = label;
            var donateIcon = label[0] == 'G'
               ? this.iconDonateGitHub
               : label[0] == 'B'
                  ? this.iconDonateBuyMeACoffee
                  : label[0] == 'K'
                     ? this.iconDonateKoFi
                     : label[0] == 'P'
                        ? this.iconDonatePayPal
                        : null;
            if (donateIcon == null) return;
            var btn = this.MakeIconButton(
               donateIcon,
               palette.TextSubtle,
               palette.Accent,
               () => Process.Start(new ProcessStartInfo(capturedUrl) { UseShellExecute = true }),
               () => hoverLabel.Text = capturedLabel,
               () => hoverLabel.Text = "");
            btn.Location = new Point(btnX, y);
            this.Controls.Add(btn);
            btnX += BtnSize + 8;
         }

         y += BtnSize + 4;

         hoverLabel.Location = new Point(TextX, y);
         this.Controls.Add(hoverLabel);
         y += hoverLabel.Height + 16;

         // ── Author / copyright / license ───────────────────────────────────────
         this.AddLabel("Michael Ryan", this.textFont, palette.TextNormal, textW, TextX, ref y, 2);
         this.AddLabel(copyright, this.textFont, palette.TextSubtle, textW, TextX, ref y, 2);
         this.AddLabel("MIT License", this.textFont, palette.TextSubtle, textW, TextX, ref y, 20);

         // ── Separator ──────────────────────────────────────────────────────────
         var sepW = FormW - 2 * PadX;
         var sep = new Panel
               { BackColor = palette.Border, Size = new Size(sepW, 1), Location = new Point(PadX, y) };
         this.Controls.Add(sep);
         y += 1 + 12;

         // ── GitHub link ────────────────────────────────────────────────────────
         var websiteBtn = this.MakeIconButton(
            this.iconWebsite, palette.TextSubtle, palette.Accent,
            () => Process.Start(new ProcessStartInfo(GitHubUrl) { UseShellExecute = true }));
         websiteBtn.Location = new Point(PadX, y + 1);
         this.Controls.Add(websiteBtn);

         var link = new LinkLabel
            {
               Text = "github.com/somethingSTRANGE/Lens",
               Font = this.textFont,
               BackColor = palette.Background,
               AutoSize = false,
               Size = new Size(FormW - PadX - (PadX + BtnSize + 6), 20),
               Location = new Point(PadX + BtnSize + 6, y),
               TextAlign = ContentAlignment.MiddleLeft,
               LinkColor = palette.Accent,
               ActiveLinkColor = palette.Accent,
               VisitedLinkColor = palette.Accent
            };
         link.LinkClicked += (_, _) =>
            Process.Start(new ProcessStartInfo(GitHubUrl) { UseShellExecute = true });
         this.Controls.Add(link);
         y += 20 + 8;

         // ── Font Awesome attribution ───────────────────────────────────────────
         var attrLabel = new Label
            {
               Text = "Icons: Font Awesome Free (fontawesome.com), CC BY 4.0",
               Font = this.attrFont,
               ForeColor = palette.TextSubtle,
               BackColor = palette.Background,
               AutoSize = false,
               Size = new Size(sepW, 18),
               Location = new Point(PadX, y),
               TextAlign = ContentAlignment.MiddleLeft
            };
         this.Controls.Add(attrLabel);
         y += 18 + 20;

         // ── Close button ───────────────────────────────────────────────────────
         var closeBtn = new Button
            {
               Text = "Close",
               Font = this.textFont,
               Size = new Size(80, 26),
               FlatStyle = FlatStyle.Flat,
               BackColor = palette.Control,
               ForeColor = palette.TextNormal,
               Location = new Point(FormW - PadX - 80, y)
            };
         closeBtn.FlatAppearance.BorderColor = palette.Border;
         closeBtn.FlatAppearance.MouseOverBackColor = palette.Border;
         closeBtn.Click += (_, _) => this.Close();
         this.Controls.Add(closeBtn);
         y += closeBtn.Height;

         this.ClientSize = new Size(FormW, y + PadY);
      }

      private void AddLabel(string text, Font font, Color color, int width, int x, ref int y, int gapAfter)
      {
         var lbl = new Label
            {
               Text = text,
               Font = font,
               ForeColor = color,
               BackColor = this.BackColor,
               AutoSize = false,
               Size = new Size(width, 20),
               Location = new Point(x, y),
               TextAlign = ContentAlignment.MiddleLeft
            };
         this.Controls.Add(lbl);
         y += lbl.Height + gapAfter;
      }

      private void AddSvgImage(SvgImage image, Color color, int x, ref int y, int gapAfter)
      {
         var panel = new Panel
            {
               Size = new Size(image.Width, image.Height),
               Location = new Point(x, y),
               BackColor = this.BackColor
            };
         panel.Paint += (_, e) =>
         {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var brush = new SolidBrush(color);
            e.Graphics.FillPath(brush, image.Path);
         };
         this.Controls.Add(panel);
         y += image.Height + gapAfter;
      }

      private Panel MakeIconButton(SvgImage icon, Color color, Color hoverColor,
         Action onClick, Action onEnter = null, Action onLeave = null)
      {
         var panel = new Panel
            {
               Size = new Size(icon.Width, icon.Height),
               BackColor = this.BackColor,
               Cursor = Cursors.Hand
            };
         var hovered = false;
         panel.Paint += (_, e) =>
         {
            e.Graphics.SmoothingMode = SmoothingMode.HighQuality;
            var state = e.Graphics.Save();
            using var brush = new SolidBrush(hovered ? hoverColor : color);
            e.Graphics.FillPath(brush, icon.Path);
            e.Graphics.Restore(state);
         };
         panel.MouseEnter += (_, _) =>
         {
            hovered = true;
            panel.Invalidate();
            onEnter?.Invoke();
         };
         panel.MouseLeave += (_, _) =>
         {
            hovered = false;
            panel.Invalidate();
            onLeave?.Invoke();
         };
         panel.MouseClick += (_, _) => onClick();
         return panel;
      }

      protected override void Dispose(bool disposing)
      {
         if (disposing)
         {
            this.textFont?.Dispose();
            this.titleFont?.Dispose();
            this.attrFont?.Dispose();
            this.appIconBitmap?.Dispose();
         }

         base.Dispose(disposing);
      }
   }
}