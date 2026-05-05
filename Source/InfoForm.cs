// -------------------------------------------------------------------------------------
// <copyright file="InfoForm.cs" company="Greyborn Studios LLC">
//   Copyright 2015-2026 Greyborn Studios LLC. All rights reserved.
// </copyright>
// -------------------------------------------------------------------------------------

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Lens
{
   /// <summary>
   ///   Layered, non-activatable, click-through overlay that displays color and cursor info
   ///   beside the lens window. Rendered via <c>UpdateLayeredWindow</c> with the same
   ///   Gaussian drop shadow used by <see cref="LensForm"/>.
   /// </summary>
   internal class InfoForm : Form
   {
      // ── Shadow parameters — must match LensForm. ───────────────────────────────────────
      private const int   ShadowBlur     = 16;
      private const float ShadowSigma    = 4.5f;
      private const int   ShadowOffsetX  = 0;
      private const int   ShadowOffsetY  = 6;
      private const byte  ShadowMaxAlpha = 160;
      private const int   ShadowMarginL  = ShadowBlur;
      private const int   ShadowMarginT  = ShadowBlur - ShadowOffsetY;  // 10
      private const int   ShadowMarginR  = ShadowBlur;
      private const int   ShadowMarginB  = ShadowBlur + ShadowOffsetY;  // 22

      // ── Content dimensions. ────────────────────────────────────────────────────────────
      private int contentW;               // dynamic; recomputed from enabled settings each frame
      private int contentH;               // dynamic; recomputed from enabled settings each frame
      internal int ContentW => this.contentW;

      /// <summary>Horizontal gap (px) between the lens content edge and this panel.</summary>
      internal const int PanelMargin = 20;
      private const int PanelPadding = 6;
      private const int ColumnGap = 4;
      private const int RowGap = 2;
      private const int RowHeight = 16;

      // Max character counts for each value type — used to compute dynamic panel width.
      private const int MaxCharsHex      =  7;  // #FFFFFF
      private const int MaxCharsRgb      = 13;  // 255, 255, 255
      private const int MaxCharsHsl      = 19;  // 359.9, 99.9%, 99.9%
      private const int MaxCharsColor4   =  4;  // #RGB  (12-bit, Web)
      private const int MaxCharsMouse    = 11;  // 99999, 99999
      private const int MaxCharsSize     =  7;  // 400×400
      private const int MaxCharsZoom     =  3;  // x16

      /// <summary>
      ///    SectionGap is only drawn after a section, and a section contains at least one row. Each row
      ///    ends with a RowGap for simplicity. The desired SectionGap should account for that extra RowGap.
      /// </summary>
      private const int SectionGap = ColumnGap * 2 - RowGap; 

      private const int IconSize = 16;
      private const int IconX = PanelPadding;
      
      private const int LabelWidth = 40;
      private const int LabelX = PanelPadding + IconSize + ColumnGap;

      private const int ValueX = LabelX + LabelWidth + ColumnGap;

      private const int SwatchSize = 24;

      // ── Win32 interop. ─────────────────────────────────────────────────────────────────
      private const uint ULW_ALPHA = 0x00000002;

      [StructLayout(LayoutKind.Sequential, Pack = 1)]
      private struct BLENDFUNCTION
      {
         public byte BlendOp, BlendFlags, SourceConstantAlpha, AlphaFormat;
      }

      [StructLayout(LayoutKind.Sequential)]
      private struct BITMAPINFOHEADER
      {
         public uint biSize; public int biWidth, biHeight;
         public ushort biPlanes, biBitCount; public uint biCompression;
         public uint biSizeImage; public int biXPelsPerMeter, biYPelsPerMeter;
         public uint biClrUsed, biClrImportant;
      }

      [StructLayout(LayoutKind.Sequential)]
      private struct BITMAPINFO { public BITMAPINFOHEADER bmiHeader; }

      [DllImport("Gdi32.dll", ExactSpelling = true, SetLastError = true)]
      private static extern IntPtr CreateCompatibleDC(IntPtr hdc);
      [DllImport("Gdi32.dll", ExactSpelling = true, SetLastError = true)]
      private static extern IntPtr CreateDIBSection(IntPtr hdc, ref BITMAPINFO pbmi, uint usage,
         out IntPtr ppvBits, IntPtr hSection, uint offset);
      [DllImport("Gdi32.dll", ExactSpelling = true)]
      private static extern IntPtr SelectObject(IntPtr hdc, IntPtr h);
      [DllImport("Gdi32.dll", ExactSpelling = true, SetLastError = true)]
      private static extern bool DeleteDC(IntPtr hdc);
      [DllImport("Gdi32.dll", ExactSpelling = true, SetLastError = true)]
      private static extern bool DeleteObject(IntPtr hobj);
      [DllImport("User32.dll", ExactSpelling = true, SetLastError = true)]
      private static extern bool UpdateLayeredWindow(IntPtr hwnd, IntPtr hdcDst, ref Point pptDst,
         ref Size psize, IntPtr hdcSrc, ref Point pptSrc, uint crKey,
         ref BLENDFUNCTION pblend, uint dwFlags);
      [DllImport("user32.dll", SetLastError = true)]
      private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
         int x, int y, int cx, int cy, uint uFlags);

      // ── State. ─────────────────────────────────────────────────────────────────────────
      private readonly InfoControl infoData;
      private Font valueFont;
      private float charWidth;
      private bool panelShown;

      // Content DC — ContentW × ContentH.
      private IntPtr layeredMemDC = IntPtr.Zero;
      private IntPtr layeredBitmap = IntPtr.Zero;
      private IntPtr layeredBits = IntPtr.Zero;

      // Final (shadow + content) DC — totalW × totalH.
      private IntPtr finalMemDC = IntPtr.Zero;
      private IntPtr finalBitmap = IntPtr.Zero;
      private IntPtr finalBits = IntPtr.Zero;
      private int finalW, finalH;

      private byte[] shadowAlpha;
      private int cachedShadowContentW = -1, cachedShadowContentH = -1;
      private int cachedLayeredW = -1, cachedLayeredH = -1;

      // ── Icon paths — built once at IconSize, reused every frame. ───────────────────────
      private readonly GraphicsPath iconColorPalette;
      private readonly GraphicsPath iconColorValues;
      private readonly GraphicsPath iconLensSize;
      private readonly GraphicsPath iconMagnification;
      private readonly GraphicsPath iconMousePosition;

      // ── Constructor. ───────────────────────────────────────────────────────────────────
      public InfoForm(InfoControl infoData)
      {
         this.infoData = infoData;
         this.FormBorderStyle = FormBorderStyle.None;
         this.ShowInTaskbar   = false;
         this.StartPosition   = FormStartPosition.Manual;
         // Font must be created before ComputeContentW(), which needs charWidth.
         this.valueFont = CreateValueFont(13f);
         this.charWidth = MeasureCharWidth(this.valueFont);
         this.contentH = this.ComputeContentH();
         this.contentW = this.ComputeContentW();
         // Window size includes shadow margins on all sides.
         this.ClientSize = new Size(this.contentW + ShadowMarginL + ShadowMarginR,
                                    this.contentH + ShadowMarginT + ShadowMarginB);
         // Start off-screen; shown lazily by UpdateAndPosition after first position set.
         this.Location = new Point(-32000, -32000);
         this.iconColorPalette   = IconPaths.Build(IconPaths.ColorPalette,   IconSize);
         this.iconColorValues    = IconPaths.Build(IconPaths.ColorValues,    IconSize);
         this.iconLensSize       = IconPaths.Build(IconPaths.LensSize,       IconSize);
         this.iconMagnification  = IconPaths.Build(IconPaths.Magnification,  IconSize);
         this.iconMousePosition  = IconPaths.Build(IconPaths.MousePosition,  IconSize);
      }

      // Never activates on Show() — must remain true for the window to be non-activatable.
      protected override bool ShowWithoutActivation => true;

      protected override CreateParams CreateParams
      {
         get
         {
            var cp = base.CreateParams;
            cp.ExStyle |= 0x00000008; // WS_EX_TOPMOST — always above non-topmost windows
            cp.ExStyle |= 0x00080000; // WS_EX_LAYERED — required for UpdateLayeredWindow
            cp.ExStyle |= 0x08000000; // WS_EX_NOACTIVATE — never activated by the OS
            return cp;
         }
      }

      // Fully non-interactive: mouse input falls through to whatever is underneath,
      // and the window cannot be activated by click.
      protected override void WndProc(ref Message m)
      {
         const int WM_MOUSEACTIVATE = 0x0021;
         const int WM_NCHITTEST     = 0x0084;
         const int MA_NOACTIVATE    = 3;
         const int HTTRANSPARENT    = -1;
         switch (m.Msg)
         {
            case WM_MOUSEACTIVATE: m.Result = (IntPtr)MA_NOACTIVATE; return;
            case WM_NCHITTEST:     m.Result = (IntPtr)HTTRANSPARENT; return;
         }
         base.WndProc(ref m);
      }

      protected override void OnClosing(CancelEventArgs e)
      {
         this.valueFont?.Dispose();
         this.valueFont = null;
         this.iconColorPalette?.Dispose();
         this.iconColorValues?.Dispose();
         this.iconLensSize?.Dispose();
         this.iconMagnification?.Dispose();
         this.iconMousePosition?.Dispose();
         this.FreeLayeredResources();
         this.FreeFinalResources();
         this.shadowAlpha = null;
         base.OnClosing(e);
      }

      /// <summary>
      ///   Creates the value font, preferring a clean monospace in order:
      ///   JetBrains Mono → Consolas → system generic monospace (Courier New).
      ///   Uses <c>Font.Name</c> to detect silent GDI+ substitution.
      /// </summary>
      private static Font CreateValueFont(float emSize)
      {
         var fontInfos = new[]
            {
               new { Name = "JetBrains Mono",  Size = emSize },
               new { Name = "Fira Code",       Size = emSize },  // y-1
               new { Name = "Noto Mono",       Size = emSize },   // y-1
               new { Name = "Consolas",        Size = emSize + 1},   // 14px
               new { Name = "Lucida Console",  Size = emSize }, // y-3
               new { Name = "Courier New",     Size = emSize }, // y-1
            };
         
         foreach (var fontInfo in fontInfos)
         {
            var font = new Font(fontInfo.Name, fontInfo.Size, FontStyle.Regular, GraphicsUnit.Pixel);
            if (string.Equals(font.Name, fontInfo.Name, StringComparison.OrdinalIgnoreCase))
            {
               float ascentPx  = font.FontFamily.GetCellAscent(font.Style) * font.Size / font.FontFamily.GetEmHeight(font.Style);
               float descentPx = font.FontFamily.GetCellDescent(font.Style) * font.Size / font.FontFamily.GetEmHeight(font.Style);
               var top = 0 - ascentPx; 
               Debug.WriteLine(
                  $"{font}\n{font.Size}, {font.SizeInPoints}, {font.Height}, ascent: {ascentPx}, descent: {descentPx}, top: {top}");
               return font;
            }
            font.Dispose();
         }
         // FontFamily.GenericMonospace always resolves — Courier New on Windows.
         var systemMonospaceFont = new Font(FontFamily.GenericMonospace, emSize, FontStyle.Regular, GraphicsUnit.Pixel);
         // Debug.WriteLine(font.Name);
         return systemMonospaceFont; // y-2
      }

      // ── Public update entry-point, called each frame from LensForm.RenderFrame. ────────

      internal bool HasVisibleContent
      {
         get
         {
            var lens = Lens.Instance;
            return lens.InfoShowHex || lens.InfoShowRgb || lens.InfoShowHsl ||
                   lens.InfoShow12Bit || lens.InfoShowWeb ||
                   lens.InfoShowMouse || lens.InfoShowSize || lens.InfoShowZoom;
         }
      }

      public void UpdateAndPosition(Point cursorPos, Color color, Rectangle contentBounds)
      {
         this.infoData.UpdateInfo(cursorPos, color);

         if (!this.HasVisibleContent)
         {
            if (this.panelShown)
            {
               const uint SWP_NOSIZE      = 0x0001;
               const uint SWP_NOMOVE      = 0x0002;
               const uint SWP_NOACTIVATE  = 0x0010;
               const uint SWP_HIDEWINDOW  = 0x0080;
               SetWindowPos(this.Handle, IntPtr.Zero, 0, 0, 0, 0,
                  SWP_NOSIZE | SWP_NOMOVE | SWP_NOACTIVATE | SWP_HIDEWINDOW);
               this.panelShown = false;
            }
            return;
         }

         // Recompute content size from current settings; free layered bitmap if either dimension changed.
         int newContentH = this.ComputeContentH();
         int newContentW = this.ComputeContentW();
         if (newContentH != this.contentH || newContentW != this.contentW)
         {
            this.contentH = newContentH;
            this.contentW = newContentW;
            this.FreeLayeredResources();
         }

         // Info mirrors the lens side — already flipped by LensForm.RenderFrame.
         bool lensIsRightOfCursor = contentBounds.Left >= cursorPos.X;
         int infoContentLeft = lensIsRightOfCursor
            ? contentBounds.Right + PanelMargin
            : contentBounds.Left - PanelMargin - ContentW;
         int infoContentTop = contentBounds.Top;

         int totalW = ContentW + ShadowMarginL + ShadowMarginR;
         int totalH = this.contentH + ShadowMarginT + ShadowMarginB;
         var winPos = new Point(infoContentLeft - ShadowMarginL, infoContentTop - ShadowMarginT);

         this.EnsureLayeredResources(ContentW, this.contentH);
         this.EnsureFinalResources(totalW, totalH);
         this.RenderContent();
         this.CompositeFinalFrame(totalW, totalH);
         this.CommitLayeredWindow(winPos, totalW, totalH);

         {
            // Re-assert topmost every frame so popup menus, taskbar thumbnails, and tooltips
            // (also topmost, but created after us) don't permanently cover the info panel.
            // LensForm calls SetWindowPos for itself just before this, so InfoForm ends up
            // above LensForm — consistent with the current Z-order.
            //
            // Show via SetWindowPos rather than Form.Show() for two reasons:
            //   1. Form.Show() triggers WinForms paint events (OnPaint, OnPaintBackground) that
            //      are suppressed here — all rendering goes through UpdateLayeredWindow.
            //   2. CommitLayeredWindow runs above, so content is already in DWM's buffer before
            //      SWP_SHOWWINDOW fires. The window appears with content, never blank. Calling
            //      Show() would make the window visible before the layered content is committed,
            //      producing a one-frame blank or positional offset between the two panels.
            var hwndTopmost = new IntPtr(-1);
            const uint SWP_NOSIZE     = 0x0001;
            const uint SWP_NOMOVE     = 0x0002;
            const uint SWP_NOACTIVATE = 0x0010;
            const uint SWP_SHOWWINDOW = 0x0040;
            uint flags = SWP_NOSIZE | SWP_NOMOVE | SWP_NOACTIVATE;
            if (!this.panelShown) flags |= SWP_SHOWWINDOW;
            SetWindowPos(this.Handle, hwndTopmost, 0, 0, 0, 0, flags);
            this.panelShown = true;
         }
      }

      // ── Rendering. ─────────────────────────────────────────────────────────────────────

      /// <summary>
      ///   Measures the advance width of one character in <paramref name="font"/> using the
      ///   same GDI+ / GenericTypographic path used at draw time.
      /// </summary>
      private static float MeasureCharWidth(Font font)
      {
         using var bmp = new Bitmap(1, 1);
         using var g   = Graphics.FromImage(bmp);
         return g.MeasureString("0", font, PointF.Empty, StringFormat.GenericTypographic).Width;
      }

      /// <summary>
      ///   Computes the dynamic panel width from the currently visible rows and the measured
      ///   character width of the value font. Color rows (HEX/RGB/HSL/12-bit/Web) drive the
      ///   swatch column position; non-color rows (Mouse) determine the fallback minimum.
      /// </summary>
      private int ComputeContentW()
      {
         var lens = Lens.Instance;

         // Widest visible color-row value in characters. HSL is always the ceiling at 19.
         int colorChars = 0;
         if      (lens.InfoShowHsl)                       colorChars = MaxCharsHsl;
         else if (lens.InfoShowRgb)                       colorChars = MaxCharsRgb;
         else if (lens.InfoShowHex)                       colorChars = MaxCharsHex;
         else if (lens.InfoShow12Bit || lens.InfoShowWeb) colorChars = MaxCharsColor4;

         // Color rows need value text + gap + swatch column.
         int colorValueW = colorChars > 0
            ? (int)Math.Ceiling(colorChars * this.charWidth) + ColumnGap * 2 + SwatchSize
            : 0;

         // Non-color rows: take the widest visible one (Mouse > Size > Zoom).
         int nonColorChars = 0;
         if (lens.InfoShowMouse) nonColorChars = MaxCharsMouse;
         else if (lens.InfoShowSize) nonColorChars = MaxCharsSize;
         else if (lens.InfoShowZoom) nonColorChars = MaxCharsZoom;
         int nonColorValueW = (int)Math.Ceiling(nonColorChars * this.charWidth);

         // Take the max so neither side clips the other when both are visible.
         int valuePixels = Math.Max(colorValueW, nonColorValueW);

         // Fall back to a minimum if nothing is enabled (shouldn't render, but be safe).
         if (valuePixels == 0) valuePixels = (int)Math.Ceiling(4 * this.charWidth);

         return ValueX + valuePixels + PanelPadding;
      }

      /// <summary>
      ///   Computes the dynamic content height from the current display-toggle settings.
      ///   Must stay in sync with the draw order in <see cref="RenderContent"/>.
      /// </summary>
      private int ComputeContentH()
      {
         var lens = Lens.Instance;
         var h = PanelPadding;

         // color-values section.
         var needGap = false;
         var showSection = lens.InfoShowHex || lens.InfoShowRgb || lens.InfoShowHsl;
         if (showSection)
         {
            if (lens.InfoShowHex) h += RowHeight + RowGap;
            if (lens.InfoShowRgb) h += RowHeight + RowGap;
            if (lens.InfoShowHsl) h += RowHeight + RowGap;
            needGap = true;
         }

         // color-palette section
         showSection = lens.InfoShow12Bit || lens.InfoShowWeb;
         if (showSection)
         {
            if (needGap) h += SectionGap;
            if (lens.InfoShow12Bit) h += RowHeight + RowGap;
            if (lens.InfoShowWeb) h += RowHeight + RowGap;
            needGap = true;
         }

         showSection = lens.InfoShowMouse || lens.InfoShowSize || lens.InfoShowZoom;
         if (showSection)
         {
            if (needGap) h += SectionGap;
            if (lens.InfoShowMouse) h += RowHeight + RowGap;
            if (lens.InfoShowSize) h += RowHeight + RowGap;
            if (lens.InfoShowZoom) h += RowHeight + RowGap;
         }
         
         // if at least one row was drawn, the final row added an unnecessary trailing gap
         if (h > PanelPadding) h -= RowGap;

         h += PanelPadding;
         return h;
      }

      private void RenderContent()
      {
         var d = this.infoData;
         var lens = Lens.Instance;

         using var g = Graphics.FromHdc(this.layeredMemDC);
         g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
         g.SmoothingMode = SmoothingMode.None;
         g.PixelOffsetMode = PixelOffsetMode.Half;

         // Background: diagonal gradient, black → #333333.
         var bgRect = new Rectangle(0, 0, ContentW, this.contentH);
         using (var bg = new LinearGradientBrush(bgRect, Color.Black, Color.FromArgb(51, 51, 51), 45f))
            g.FillRectangle(bg, bgRect);

         using var labelFont = new Font("Segoe UI", 12f, FontStyle.Regular, GraphicsUnit.Pixel);
         using var labelBrush = new SolidBrush(Color.FromArgb(0xFF, 0xFF, 0xE1));
         using var valueBrush = new SolidBrush(Color.White);
         using var valueCopyBrush = new SolidBrush(Color.FromArgb(0xFF, 0xE5, 0x66));
         using var outlinePen = new Pen(Color.DarkSlateGray) { Alignment = PenAlignment.Inset };

         void DrawStringAtBaseline(string text, Font font, Brush brush, float x, float baselineY)
         {
            if (font.Name == "Courier New")
            {
               baselineY--;
            }

            var ff = font.FontFamily;
            var style = font.Style;
            float ascentPx = (float)Math.Round(font.Size * ff.GetCellAscent(style) / ff.GetEmHeight(style));
            // GenericTypographic: PointF.Y is exactly the top of the cell ascent — no internal-leading shift.
            g.DrawString(text, font, brush, new PointF(x, baselineY - ascentPx),
               StringFormat.GenericTypographic);
         }

         void DrawRow(string label, string value, float y)
         {
            var baseline = y + RowHeight - 3;

            // var layout = new RectangleF(LabelX, y, 40, RowHeight);
            // DrawOutline(g, Rectangle.Round(layout));
            // layout = new RectangleF(ValueX, y, valueW, RowHeight);
            // DrawOutline(g, Rectangle.Round(layout));
            // g.DrawLine(outlinePen, new PointF(LabelX - 1, baseline), new PointF(layout.Right + 2, baseline));

            DrawStringAtBaseline(label, labelFont, labelBrush, LabelX, baseline);

            var display = value;
            var brush = valueBrush;
            if (d.IsCopied(label))
            {
               display = "Copied";
               brush = valueCopyBrush;
            }
            DrawStringAtBaseline(display, this.valueFont, brush, ValueX, baseline);
         }

         float y = PanelPadding; // tracked Y; advances as sections are drawn
         var swatchRect = new Rectangle(ContentW - PanelPadding - SwatchSize, (int)y, SwatchSize, 0);

         // ── color-values section ────────────────────────────────────────────────────────
         bool cvAny = lens.InfoShowHex || lens.InfoShowRgb || lens.InfoShowHsl;
         var rowOffset = RowHeight + RowGap;
         if (cvAny)
         {
            // Icon baseline-aligned with the section's first row.
            DrawIcon(g, this.iconColorValues, valueBrush, IconX, y, IconSize);
            if (lens.InfoShowHex)
            {
               DrawRow("HEX", d.ValueColorHex, y);
               y += rowOffset;
               swatchRect.Height += rowOffset;
            }

            if (lens.InfoShowRgb)
            {
               DrawRow("RGB", d.ValueColorRGB, y);
               y += rowOffset;
               swatchRect.Height += rowOffset;
            }

            if (lens.InfoShowHsl)
            {
               DrawRow("HSL", d.ValueColorHSL, y);
               y += rowOffset;
               swatchRect.Height += rowOffset;
            }

            swatchRect.Height -= RowGap;
            if (swatchRect.Height > 0)
            {
               DrawSwatch(d.ColorSwatch, g, swatchRect);
            }
         }

         // ── color-palette section (swatch) ─────────────────────────────────────────────
         if (cvAny) y += SectionGap;

         cvAny = lens.InfoShow12Bit || lens.InfoShowWeb;
         if (cvAny)
         {
            swatchRect.Height = RowHeight;

            DrawIcon(g, this.iconColorPalette, valueBrush, IconX, y, IconSize);
            if (lens.InfoShow12Bit)
            {
               DrawRow("12-Bit", d.ValueColor12Bit, y);

               swatchRect.Y = (int)y;
               DrawSwatch(d.Color12Bit, g, swatchRect);
               
               y += rowOffset;
            }

            if (lens.InfoShowWeb)
            {
               DrawRow("Web", d.ValueColorWeb, y);

               swatchRect.Y = (int)y;
               DrawSwatch(d.ColorWeb, g, swatchRect);

               y += rowOffset;
            }
         }

         // ── mouse-position section ─────────────────────────────────────────────────────
         if (cvAny) y += SectionGap;

         if (lens.InfoShowMouse)
         {
            DrawIcon(g, this.iconMousePosition, valueBrush, IconX, y, IconSize);
            DrawRow("Mouse", d.MousePosition, y);
            y += rowOffset;
         }

         // ── lens-size section (no gap) ─────────────────────────────────────────────────
         if (lens.InfoShowSize)
         {
            DrawIcon(g, this.iconLensSize, valueBrush, IconX, y, IconSize);
            DrawRow("Size", d.LensSize, y);
            y += rowOffset;
         }

         // ── magnification section (no gap) ─────────────────────────────────────────────
         if (lens.InfoShowZoom)
         {
            DrawIcon(g, this.iconMagnification, valueBrush, IconX, y, IconSize);
            DrawRow("Zoom", d.ZoomFactor, y);
         }

         /// <summary>
         ///   Fills a pre-built <see cref="GraphicsPath"/> icon using <paramref name="brush"/>.
         ///   (<paramref name="x"/>, <paramref name="y"/>) is the lower-left (baseline) corner,
         ///   so the same y can be passed to both this and <c>DrawStringAtBaseline</c> to align
         ///   an icon with adjacent text. <paramref name="size"/> must match the value used when
         ///   the path was built — it is used to shift the path's upper-left to the correct origin.
         /// </summary>
         void DrawIcon(Graphics g, GraphicsPath path, Brush brush, float x, float y, float size)
         {
            // var rect = new RectangleF(x, y, size, size);
            // DrawRect(g, rect, Color.Blue);
            // DrawOutline(g, rect);

            var smoothingMode = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.HighQuality;

            var state = g.Save();
            g.TranslateTransform(x, y);
            g.FillPath(brush, path);
            g.Restore(state);
            
            g.SmoothingMode = smoothingMode;
         }

#pragma warning disable CS8321 // Local function is declared but never used
         void DrawRect(Graphics g, RectangleF rect, Color color)
         {
            var smoothingMode = g.SmoothingMode;
            var pixelOffsetMode = g.PixelOffsetMode;
            g.SmoothingMode = SmoothingMode.None;
            g.PixelOffsetMode = PixelOffsetMode.Half;

            using (var brush = new SolidBrush(color))
               g.FillRectangle(brush, rect);

            g.SmoothingMode = smoothingMode;
            g.PixelOffsetMode = pixelOffsetMode;
         }
#pragma warning restore CS8321 // Local function is declared but never used

         void DrawOutline(Graphics g, RectangleF rect)
         {
            var smoothingMode = g.SmoothingMode;
            var pixelOffsetMode = g.PixelOffsetMode;
            g.SmoothingMode = SmoothingMode.None;
            g.PixelOffsetMode = PixelOffsetMode.Half;

            g.DrawLine(outlinePen, rect.Left, rect.Top + 1, rect.Right, rect.Top + 1);
            g.DrawLine(outlinePen, rect.Right, rect.Top, rect.Right, rect.Bottom);
            g.DrawLine(outlinePen, rect.Right, rect.Bottom, rect.Left, rect.Bottom);
            g.DrawLine(outlinePen, rect.Left + 1, rect.Bottom, rect.Left + 1, rect.Top + 1);

            g.SmoothingMode = smoothingMode;
            g.PixelOffsetMode = pixelOffsetMode;
         }
      }

      private static void DrawSwatch(Color color, Graphics graphics, Rectangle rect)
      {
         using (var swatchBrush = new SolidBrush(Color.Black))
         {
            graphics.FillRectangle(swatchBrush, rect);
         }

         using (var swatchBrush = new SolidBrush(color))
         {
            rect = new Rectangle(rect.X + 1, rect.Y + 1, rect.Width - 2, rect.Height - 2); 
            graphics.FillRectangle(swatchBrush, rect);
         }
      }

      // ── GDI resource management (mirrors LensForm). ────────────────────────────────────

      private void EnsureLayeredResources(int w, int h)
      {
         if (this.layeredMemDC == IntPtr.Zero)
            this.layeredMemDC = CreateCompatibleDC(IntPtr.Zero);
         if (this.layeredBitmap != IntPtr.Zero && this.cachedLayeredW == w && this.cachedLayeredH == h) return;

         if (this.layeredBitmap != IntPtr.Zero) { DeleteObject(this.layeredBitmap); this.layeredBitmap = IntPtr.Zero; }
         var bmi = MakeBmi(w, h);
         this.layeredBitmap = CreateDIBSection(IntPtr.Zero, ref bmi, 0, out this.layeredBits, IntPtr.Zero, 0);
         SelectObject(this.layeredMemDC, this.layeredBitmap);
         this.cachedLayeredW = w;
         this.cachedLayeredH = h;
      }

      private void FreeLayeredResources()
      {
         if (this.layeredBitmap != IntPtr.Zero)
         {
            DeleteObject(this.layeredBitmap);
            this.layeredBitmap = IntPtr.Zero;
            this.layeredBits   = IntPtr.Zero;
         }
         if (this.layeredMemDC != IntPtr.Zero) { DeleteDC(this.layeredMemDC); this.layeredMemDC = IntPtr.Zero; }
         this.cachedLayeredW = -1;
         this.cachedLayeredH = -1;
      }

      private void EnsureFinalResources(int w, int h)
      {
         if (this.finalMemDC == IntPtr.Zero)
            this.finalMemDC = CreateCompatibleDC(IntPtr.Zero);
         if (this.finalBitmap != IntPtr.Zero && this.finalW == w && this.finalH == h) return;

         if (this.finalBitmap != IntPtr.Zero) { DeleteObject(this.finalBitmap); this.finalBits = IntPtr.Zero; }
         var bmi = MakeBmi(w, h);
         this.finalBitmap = CreateDIBSection(IntPtr.Zero, ref bmi, 0, out this.finalBits, IntPtr.Zero, 0);
         SelectObject(this.finalMemDC, this.finalBitmap);
         this.finalW = w;
         this.finalH = h;
      }

      private void FreeFinalResources()
      {
         if (this.finalBitmap != IntPtr.Zero) { DeleteObject(this.finalBitmap); this.finalBitmap = IntPtr.Zero; this.finalBits = IntPtr.Zero; }
         if (this.finalMemDC  != IntPtr.Zero) { DeleteDC(this.finalMemDC);  this.finalMemDC  = IntPtr.Zero; }
      }

      private static BITMAPINFO MakeBmi(int w, int h) => new BITMAPINFO
      {
         bmiHeader = new BITMAPINFOHEADER
         {
            biSize        = (uint)Marshal.SizeOf<BITMAPINFOHEADER>(),
            biWidth       = w,
            biHeight      = -h, // negative = top-down
            biPlanes      = 1,
            biBitCount    = 32,
            biCompression = 0   // BI_RGB
         }
      };

      private void CompositeFinalFrame(int tw, int th)
      {
         if (this.finalBits == IntPtr.Zero || this.layeredBits == IntPtr.Zero) return;

         this.EnsureShadow();

         // Clear final to transparent.
         int totalPx = tw * th;
         for (int i = 0; i < totalPx; i++)
            Marshal.WriteInt32(this.finalBits, i * 4, 0);

         // Write shadow (pre-multiplied black with per-pixel alpha).
         var alpha = this.shadowAlpha;
         for (int i = 0; i < totalPx; i++)
         {
            byte a = alpha[i];
            if (a > 0)
               Marshal.WriteInt32(this.finalBits, i * 4, unchecked((int)((uint)a << 24)));
         }

         // Stamp content at (ShadowMarginL, ShadowMarginT), forcing alpha=255.
         int cStride = ContentW * 4;
         int fStride = tw * 4;
         for (int y = 0; y < this.contentH; y++)
         for (int x = 0; x < ContentW; x++)
         {
            int src = Marshal.ReadInt32(this.layeredBits, y * cStride + x * 4);
            int dst = (src & 0x00FFFFFF) | unchecked((int)0xFF000000u);
            Marshal.WriteInt32(this.finalBits, (y + ShadowMarginT) * fStride + (x + ShadowMarginL) * 4, dst);
         }
      }

      private void CommitLayeredWindow(Point winPos, int w, int h)
      {
         var winSize = new Size(w, h);
         var srcPos  = Point.Empty;
         var blend   = new BLENDFUNCTION { BlendOp = 0, BlendFlags = 0, SourceConstantAlpha = 255, AlphaFormat = 1 };
         UpdateLayeredWindow(this.Handle, IntPtr.Zero, ref winPos, ref winSize,
            this.finalMemDC, ref srcPos, 0, ref blend, ULW_ALPHA);
      }

      // ── Shadow (Gaussian blur). ────────────────────────────────────────────────────────

      private void EnsureShadow()
      {
         if (this.shadowAlpha != null &&
             this.cachedShadowContentW == ContentW &&
             this.cachedShadowContentH == this.contentH)
            return;

         this.cachedShadowContentW = ContentW;
         this.cachedShadowContentH = this.contentH;

         int tw = ContentW + ShadowMarginL + ShadowMarginR;
         int th = this.contentH + ShadowMarginT + ShadowMarginB;

         int sx = ShadowMarginL + ShadowOffsetX;
         int sy = ShadowMarginT + ShadowOffsetY;
         var src = new float[tw * th];
         for (int y = sy; y < sy + this.contentH; y++)
         for (int x = sx; x < sx + ContentW; x++)
            src[y * tw + x] = 1f;

         var temp   = GaussianBlur1D(src,  tw, th, ShadowSigma, horizontal: true);
         var result = GaussianBlur1D(temp, tw, th, ShadowSigma, horizontal: false);

         this.shadowAlpha = new byte[tw * th];
         for (int i = 0; i < result.Length; i++)
            this.shadowAlpha[i] = (byte)Math.Round(result[i] * ShadowMaxAlpha);
      }

      private static float[] GaussianBlur1D(float[] src, int w, int h, float sigma, bool horizontal)
      {
         int radius = (int)Math.Ceiling(sigma * 3);
         var kernel = new float[2 * radius + 1];
         float sum = 0;
         for (int i = -radius; i <= radius; i++)
         {
            kernel[i + radius] = (float)Math.Exp(-i * i / (2.0 * sigma * sigma));
            sum += kernel[i + radius];
         }
         for (int i = 0; i < kernel.Length; i++) kernel[i] /= sum;

         var dst = new float[w * h];
         if (horizontal)
         {
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
               float val = 0;
               for (int k = -radius; k <= radius; k++)
               {
                  int xx = x + k;
                  if (xx >= 0 && xx < w) val += src[y * w + xx] * kernel[k + radius];
               }
               dst[y * w + x] = val;
            }
         }
         else
         {
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
               float val = 0;
               for (int k = -radius; k <= radius; k++)
               {
                  int yy = y + k;
                  if (yy >= 0 && yy < h) val += src[yy * w + x] * kernel[k + radius];
               }
               dst[y * w + x] = val;
            }
         }
         return dst;
      }
   }
}
