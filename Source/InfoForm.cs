// -------------------------------------------------------------------------------------
// <copyright file="InfoForm.cs">
//   Copyright (c) 2026
//   Licensed under the MIT License. See LICENSE file in the project root.
// </copyright>
// -------------------------------------------------------------------------------------

namespace StrangeLens
{
   using System;
   using System.Diagnostics.CodeAnalysis;
   using System.Drawing;
   using System.Drawing.Drawing2D;
   using System.Drawing.Text;
   using System.Linq;
   using System.Runtime.InteropServices;
   using System.Windows.Forms;

   using static NativeMethods;

   /// <summary>Layered, non-activatable, click-through overlay that displays color and cursor
   ///    info beside the lens window. Rendered via <c>UpdateLayeredWindow</c> with the same
   ///    Gaussian drop shadow used by <see cref="LensForm"/>.</summary>
   internal class InfoForm : Form
   {
      private const int ColumnGap = 4;

      private const int IconSize = 16;

      private const int IconX = PanelPadding;

      private const int LabelWidth = 40;

      private const int LabelX = PanelPadding + IconSize + ColumnGap;
      // Shadow base consts (ShadowBlur/Sigma/OffsetY/MaxAlpha) must match LensForm -- both panels
      //   share the same drop shadow. Derived margins (shadowMarginL/R/T/B) are instance fields
      //   computed per-frame from these base values × dpiScale.
      // logicalContentW/H: panel dimensions in 96-DPI logical pixels used for drawing.
      // contentW/H: physical-pixel versions submitted to UpdateLayeredWindow (= logical × dpiScale).
      // layeredMemDC/layeredBitmap/layeredBits: content DC, sized contentW x contentH.
      // finalMemDC/finalBitmap/finalBits/finalW/finalH: shadow+content DC submitted to
      //   UpdateLayeredWindow, sized totalW x totalH.
      // iconColorPalette/iconColorValues/iconLensSize/iconMagnification/iconMousePosition:
      //   built once at IconSize; ScaleTransform in RenderContent scales them each frame.
      // EnsureLayeredResources/FreeLayeredResources/EnsureFinalResources/FreeFinalResources
      //   mirror the equivalent GDI resource-management methods in LensForm.

      /// <summary>Horizontal gap (px) between the lens content edge and this panel.</summary>
      private const int PanelMargin = 20;

      private const int PanelPadding = 6;

      private const int RowGap = 2;

      private const int RowHeight = 16;

      /// <summary>SectionGap is only drawn after a section, and a section contains at least one
      ///    row. Each row ends with a RowGap for simplicity. The desired SectionGap should account
      ///    for that extra RowGap.</summary>
      private const int SectionGap = (ColumnGap * 2) - RowGap;

      // Drop shadow base parameters (96-DPI design values; scaled per-frame in UpdateAndPosition).
      private const int ShadowBlur = 16;

      private const byte ShadowMaxAlpha = 160;

      private const int ShadowOffsetY = 6;

      private const float ShadowSigma = 4.5f;

      private const int SwatchSize = 24;

      private const int ValueX = LabelX + LabelWidth + ColumnGap;

      private readonly float charWidth;

      private readonly VectorImage iconColorPalette;

      private readonly VectorImage iconColorValues;

      private readonly VectorImage iconLensSize;

      private readonly VectorImage iconMagnification;

      private readonly VectorImage iconMousePosition;

      private readonly VectorImage iconRuler;

      private readonly InfoControl infoData;

      private readonly FontInfo labelFont;

      private int cachedLayeredW = -1, cachedLayeredH = -1;

      private int cachedShadowContentW = -1, cachedShadowContentH = -1;

      private int contentH; // dynamic; recomputed from enabled settings each frame

      private int contentW; // dynamic; recomputed from enabled settings each frame

      private float dpiScale = 1f;

      private IntPtr finalBitmap = IntPtr.Zero;

      private IntPtr finalBits = IntPtr.Zero;

      private IntPtr finalMemDC = IntPtr.Zero;

      private int finalW, finalH;

      private IntPtr layeredBitmap = IntPtr.Zero;

      private IntPtr layeredBits = IntPtr.Zero;

      private IntPtr layeredMemDC = IntPtr.Zero;

      private int logicalContentH, logicalContentW;

      private bool panelShown;

      private byte[]? shadowAlpha;

      private int shadowMarginB, shadowMarginL, shadowMarginR, shadowMarginT;

      private float shadowSigmaScaled;

      private FontInfo? valueFont;

      public InfoForm(InfoControl infoData)
      {
         this.infoData = infoData;
         this.FormBorderStyle = FormBorderStyle.None;
         this.ShowInTaskbar = false;
         this.StartPosition = FormStartPosition.Manual;
         // Font must be created before ComputeContentW(), which needs charWidth.
         this.labelFont = FontHelper.CreateRegularFontInfo();
         this.valueFont = FontHelper.CreateValueFontInfo();
         this.charWidth = MeasureCharWidth(this.valueFont.Font);
         this.logicalContentH = this.ComputeContentH();
         this.logicalContentW = this.ComputeContentW();
         this.contentH = this.logicalContentH;
         this.contentW = this.logicalContentW;
         this.shadowMarginL = this.shadowMarginR = ShadowBlur;
         this.shadowMarginT = ShadowBlur - ShadowOffsetY;
         this.shadowMarginB = ShadowBlur + ShadowOffsetY;
         this.shadowSigmaScaled = ShadowSigma;
         // Window size includes shadow margins on all sides.
         this.ClientSize = new Size(
            this.contentW + this.shadowMarginL + this.shadowMarginR,
            this.contentH + this.shadowMarginT + this.shadowMarginB);
         // Start off-screen; shown lazily by UpdateAndPosition after first position set.
         this.Location = new Point(-32000, -32000);
         this.iconColorPalette = VectorImageFactory.InfoColorPalette(IconSize);
         this.iconColorValues = VectorImageFactory.InfoColorValues(IconSize);
         this.iconLensSize = VectorImageFactory.InfoLensSize(IconSize);
         this.iconMagnification = VectorImageFactory.InfoMagnification(IconSize);
         this.iconMousePosition = VectorImageFactory.InfoMousePosition(IconSize);
         this.iconRuler = VectorImageFactory.InfoRuler(IconSize);
      }

      internal int ContentH => this.contentH;

      internal int ContentW => this.contentW;

      internal bool HasVisibleContent
      {
         get
         {
            var lens = Lens.Instance;
            return lens.InfoShowHex || lens.InfoShowRgb || lens.InfoShowHsl || lens.InfoShow12Bit
                   || lens.InfoShowWeb || lens.InfoShowMouse || lens.InfoShowSize || lens.InfoShowZoom;
         }
      }

      protected override CreateParams CreateParams
      {
         get
         {
            var cp = base.CreateParams;
            cp.ExStyle |= WS_EX_TOPMOST;
            cp.ExStyle |= WS_EX_LAYERED;
            cp.ExStyle |= WS_EX_NOACTIVATE;
            return cp;
         }
      }

      /// <summary>Never activates on Show() -- must remain true for the window to be
      ///    non-activatable.</summary>
      protected override bool ShowWithoutActivation => true;

      /// <summary>Convenience overload -- computes the bounding rect from <paramref name="path"/>
      ///    and delegates to the explicit (x, y, w, h) overload.</summary>
      public static void DrawDebugBounds(Graphics g, GraphicsPath path, float x, float y)
      {
         var b = path.GetBounds();
         DrawDebugBounds(g, x + b.X, y + b.Y, b.Width, b.Height);
      }

      /// <summary>Public update entry-point, called each frame from
      ///    <see cref="LensForm.RenderFrame"/>.</summary>
      public void UpdateAndPosition(
         Point cursorPos,
         Color color,
         Rectangle contentBounds,
         bool infoLeft,
         bool precisionActive,
         int precisionSpeed,
         float newDpiScale)
      {
         this.infoData.UpdateInfo(cursorPos, color, precisionActive, precisionSpeed);

         // Update DPI-dependent fields whenever the display DPI changes, even when hidden.
         if (Math.Abs(newDpiScale - this.dpiScale) > 0.001f)
         {
            this.dpiScale = newDpiScale;
            this.shadowMarginL = this.shadowMarginR = (int)Math.Round(ShadowBlur * this.dpiScale);
            this.shadowMarginT = (int)Math.Round((ShadowBlur - ShadowOffsetY) * this.dpiScale);
            this.shadowMarginB = (int)Math.Round((ShadowBlur + ShadowOffsetY) * this.dpiScale);
            this.shadowSigmaScaled = ShadowSigma * this.dpiScale;
            this.cachedShadowContentW = -1; // sigma changed; force shadow rebuild
         }

         if (!this.HasVisibleContent)
         {
            if (this.panelShown)
            {
               SetWindowPos(
                  this.Handle,
                  IntPtr.Zero,
                  0,
                  0,
                  0,
                  0,
                  SWP_NOSIZE | SWP_NOMOVE | SWP_NOACTIVATE | SWP_HIDEWINDOW);
               this.panelShown = false;
            }

            return;
         }

         // Recompute content size from current settings; free layered bitmap if either dimension changed.
         var newLogicalH = this.ComputeContentH();
         var newLogicalW = this.ComputeContentW();
         var newContentH = (int)Math.Round(newLogicalH * this.dpiScale);
         var newContentW = (int)Math.Round(newLogicalW * this.dpiScale);
         if ((newContentH != this.contentH) || (newContentW != this.contentW))
         {
            this.logicalContentH = newLogicalH;
            this.logicalContentW = newLogicalW;
            this.contentH = newContentH;
            this.contentW = newContentW;
            this.FreeLayeredResources();
         }

         var scaledPanelMargin = (int)Math.Round(PanelMargin * this.dpiScale);
         var infoContentLeft = infoLeft
            ? contentBounds.Left - scaledPanelMargin - this.ContentW
            : contentBounds.Right + scaledPanelMargin;
         var infoContentTop = contentBounds.Top;

         var totalW = this.ContentW + this.shadowMarginL + this.shadowMarginR;
         var totalH = this.contentH + this.shadowMarginT + this.shadowMarginB;
         var winPos = new Point(infoContentLeft - this.shadowMarginL, infoContentTop - this.shadowMarginT);

         this.EnsureLayeredResources(this.ContentW, this.contentH);
         this.EnsureFinalResources(totalW, totalH);
         this.RenderContent();
         this.CompositeFinalFrame(totalW, totalH);
         this.CommitLayeredWindow(winPos, totalW, totalH);

         {
            // Re-assert topmost every frame so popup menus, taskbar thumbnails, and tooltips
            // (also topmost, but created after us) don't permanently cover the info panel.
            // LensForm calls SetWindowPos for itself just before this, so InfoForm ends up
            // above LensForm -- consistent with the current Z-order.
            //
            // Show via SetWindowPos rather than Form.Show() for two reasons:
            //   1. Form.Show() triggers WinForms paint events (OnPaint, OnPaintBackground) that
            //      are suppressed here -- all rendering goes through UpdateLayeredWindow.
            //   2. CommitLayeredWindow runs above, so content is already in DWM's buffer before
            //      SWP_SHOWWINDOW fires. The window appears with content, never blank. Calling
            //      Show() would make the window visible before the layered content is committed,
            //      producing a one-frame blank or positional offset between the two panels.
            var flags = SWP_NOSIZE | SWP_NOMOVE | SWP_NOACTIVATE;
            if (!this.panelShown)
            {
               flags |= SWP_SHOWWINDOW;
            }

            SetWindowPos(this.Handle, HWND_TOPMOST, 0, 0, 0, 0, flags);
            this.panelShown = true;
         }
      }

      protected override void OnFormClosing(FormClosingEventArgs e)
      {
         this.labelFont.Dispose();
         this.valueFont?.Dispose();
         this.valueFont = null;
         this.FreeLayeredResources();
         this.FreeFinalResources();
         this.shadowAlpha = null;
         base.OnFormClosing(e);
      }

      /// <summary>Fully non-interactive: mouse input falls through to whatever is underneath, and
      ///    the window cannot be activated by click.</summary>
      protected override void WndProc(ref Message m)
      {
         switch (m.Msg)
         {
            case WM_MOUSEACTIVATE:
               m.Result = MA_NOACTIVATE;
               return;
            case WM_NCHITTEST:
               m.Result = HTTRANSPARENT;
               return;
         }

         base.WndProc(ref m);
      }

      /// <summary>Adds one section's worth of height. Prepends <see cref="SectionGap"/> if a prior
      ///    section was already emitted, then accumulates <c>RowHeight + RowGap</c> per enabled
      ///    row. Sets <paramref name="needGap"/> if any row was added.</summary>
      private static int AddSectionH(int h, ref bool needGap, params bool[] rows)
      {
         if (!Array.Exists(rows, r => r))
         {
            return h;
         }

         if (needGap)
         {
            h += SectionGap;
         }

         foreach (var r in rows)
         {
            if (r)
            {
               h += RowHeight + RowGap;
            }
         }

         needGap = true;
         return h;
      }

      private static void DrawDebugBounds(Graphics g, float x, float y, float w, float h)
      {
         var left = MathF.Floor(x);
         var top = MathF.Floor(y);
         var rect = new RectangleF(left, top, MathF.Ceiling(x + w) - left, MathF.Ceiling(y + h) - top);
         DrawRect(g, rect, Color.Blue);
         DrawOutline(g, rect);
      }

      private static void DrawOutline(Graphics g, RectangleF rect)
      {
         var smoothingMode = g.SmoothingMode;
         var pixelOffsetMode = g.PixelOffsetMode;
         g.SmoothingMode = SmoothingMode.None;
         g.PixelOffsetMode = PixelOffsetMode.Half;

         using var outlinePen = new Pen(Color.DarkSlateGray);
         outlinePen.Alignment = PenAlignment.Inset;

         g.DrawLine(outlinePen, rect.Left, rect.Top + 1, rect.Right, rect.Top + 1);
         g.DrawLine(outlinePen, rect.Right, rect.Top, rect.Right, rect.Bottom);
         g.DrawLine(outlinePen, rect.Right, rect.Bottom, rect.Left, rect.Bottom);
         g.DrawLine(outlinePen, rect.Left + 1, rect.Bottom, rect.Left + 1, rect.Top + 1);

         g.SmoothingMode = smoothingMode;
         g.PixelOffsetMode = pixelOffsetMode;
      }

      private static void DrawRect(Graphics g, RectangleF rect, Color color)
      {
         var smoothingMode = g.SmoothingMode;
         var pixelOffsetMode = g.PixelOffsetMode;
         g.SmoothingMode = SmoothingMode.None;
         g.PixelOffsetMode = PixelOffsetMode.Half;

         using (var brush = new SolidBrush(color))
         {
            g.FillRectangle(brush, rect);
         }

         g.SmoothingMode = smoothingMode;
         g.PixelOffsetMode = pixelOffsetMode;
      }

      private static void DrawSwatch(Color color, Graphics g, RectangleF rect, float dpiScale)
      {
         using var blackBrush = new SolidBrush(Color.Black);
         g.FillRectangle(blackBrush, rect);
         // Border = lineWidth physical pixels; convert to logical, so ScaleTransform yields an exact
         // whole-pixel border regardless of DPI (e.g., 2/1.5 = 1.333 logical → 2.0 physical at 150%).
         var border = Math.Max(1, (int)Math.Round(dpiScale)) / dpiScale;
         var inner = new RectangleF(
            rect.X + border,
            rect.Y + border,
            rect.Width - (2 * border),
            rect.Height - (2 * border));
         using var colorBrush = new SolidBrush(color);
         g.FillRectangle(colorBrush, inner);
      }

      [SuppressMessage("ReSharper", "CognitiveComplexity")]
      private static float[] GaussianBlur1D(float[] src, int w, int h, float sigma, bool horizontal)
      {
         var radius = (int)Math.Ceiling(sigma * 3);
         var kernel = new float[(2 * radius) + 1];
         float sum = 0;
         for (var i = -radius; i <= radius; i++)
         {
            kernel[i + radius] = (float)Math.Exp((-i * i) / (2.0 * sigma * sigma));
            sum += kernel[i + radius];
         }

         for (var i = 0; i < kernel.Length; i++)
         {
            kernel[i] /= sum;
         }

         var dst = new float[w * h];
         if (horizontal)
         {
            for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
            {
               float val = 0;
               for (var k = -radius; k <= radius; k++)
               {
                  var xx = x + k;
                  if ((xx >= 0) && (xx < w))
                  {
                     val += src[(y * w) + xx] * kernel[k + radius];
                  }
               }

               dst[(y * w) + x] = val;
            }
         }
         else
         {
            for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
            {
               float val = 0;
               for (var k = -radius; k <= radius; k++)
               {
                  var yy = y + k;
                  if ((yy >= 0) && (yy < h))
                  {
                     val += src[(yy * w) + x] * kernel[k + radius];
                  }
               }

               dst[(y * w) + x] = val;
            }
         }

         return dst;
      }

      private static BITMAPINFO MakeBmi(int w, int h)
      {
         return new BITMAPINFO
            {
               bmiHeader = new BITMAPINFOHEADER
                  {
                     biSize = (uint)Marshal.SizeOf<BITMAPINFOHEADER>(),
                     biWidth = w,
                     biHeight = -h, // negative = top-down
                     biPlanes = 1,
                     biBitCount = 32,
                     biCompression = 0, // BI_RGB
                  },
            };
      }

      /// <summary>Measures the advance width of one character in <paramref name="font"/> using the
      ///    same GDI+ / GenericTypographic path used at draw time.</summary>
      private static float MeasureCharWidth(Font font)
      {
         using var bmp = new Bitmap(1, 1);
         using var g = Graphics.FromImage(bmp);
         return g.MeasureString("0", font, PointF.Empty, StringFormat.GenericTypographic).Width;
      }

      private void CommitLayeredWindow(Point winPos, int w, int h)
      {
         var winSize = new Size(w, h);
         var srcPos = Point.Empty;
         var blend = new BLENDFUNCTION
            {
               BlendOp = 0,
               BlendFlags = 0,
               SourceConstantAlpha = 255,
               AlphaFormat = 1,
            };
         UpdateLayeredWindow(
            this.Handle,
            IntPtr.Zero,
            ref winPos,
            ref winSize,
            this.finalMemDC,
            ref srcPos,
            0,
            ref blend,
            ULW_ALPHA);
      }

      private void CompositeFinalFrame(int tw, int th)
      {
         if ((this.finalBits == IntPtr.Zero) || (this.layeredBits == IntPtr.Zero))
         {
            return;
         }

         this.EnsureShadow();

         // Clear final to transparent.
         var totalPx = tw * th;
         for (var i = 0; i < totalPx; i++)
         {
            Marshal.WriteInt32(this.finalBits, i * 4, 0);
         }

         // Write shadow (pre-multiplied black with per-pixel alpha).
         var alpha = this.shadowAlpha!;
         for (var i = 0; i < totalPx; i++)
         {
            var a = alpha[i];
            if (a > 0)
            {
               Marshal.WriteInt32(this.finalBits, i * 4, unchecked((int)((uint)a << 24)));
            }
         }

         // Stamp content at (shadowMarginL, shadowMarginT), forcing alpha=255.
         var cStride = this.ContentW * 4;
         var fStride = tw * 4;
         for (var y = 0; y < this.contentH; y++)
         for (var x = 0; x < this.ContentW; x++)
         {
            var src = Marshal.ReadInt32(this.layeredBits, (y * cStride) + (x * 4));
            var dst = (src & 0x00FFFFFF) | unchecked((int)0xFF000000u);
            Marshal.WriteInt32(
               this.finalBits,
               ((y + this.shadowMarginT) * fStride) + ((x + this.shadowMarginL) * 4),
               dst);
         }
      }

      /// <summary>Computes the dynamic content height from the current display-toggle settings.
      ///    Must stay in sync with the draw order in <see cref="RenderContent"/>.</summary>
      private int ComputeContentH()
      {
         var lens = Lens.Instance;
         var h = PanelPadding;
         var needGap = false;

         h = AddSectionH(h, ref needGap, lens.InfoShowHex, lens.InfoShowRgb, lens.InfoShowHsl);
         h = AddSectionH(h, ref needGap, lens.InfoShow12Bit, lens.InfoShowWeb);
         h = AddSectionH(h, ref needGap, lens.InfoShowMouse, lens.InfoShowSize, lens.InfoShowZoom);
         h = AddSectionH(h, ref needGap, this.infoData.MeasureActive);

         // The final enabled row appended a trailing RowGap; remove it.
         if (h > PanelPadding)
         {
            h -= RowGap;
         }

         return h + PanelPadding;
      }

      /// <summary>Computes the dynamic panel width from the currently visible rows and the
      ///    measured character width of the value font. Color rows (HEX/RGB/HSL/12-bit/Web) drive
      ///    the swatch column position; non-color rows (Mouse) determine the fallback minimum.</summary>
      private int ComputeContentW()
      {
         var lens = Lens.Instance;

         var sampleHsl = string.Format(InfoControl.PatternHsl, 359.9, 99.9, 99.9);
         var sampleRgb = string.Format(InfoControl.PatternRgb, 255, 255, 255);
         var sampleHex = string.Format(InfoControl.PatternHex, 0xFF, 0xFF, 0xFF);
         var sampleColor4 = string.Format(InfoControl.PatternHexShort, 0xF, 0xF, 0xF);

         var colorChars = new[]
            {
               lens.InfoShowHsl ? sampleHsl.Length : 0,
               lens.InfoShowRgb ? sampleRgb.Length : 0,
               lens.InfoShowHex ? sampleHex.Length : 0,
               lens.InfoShow12Bit || lens.InfoShowWeb ? sampleColor4.Length : 0,
            }.Max();

         // Color rows need value text + gap + swatch column.
         var colorValueW = colorChars > 0
            ? (int)Math.Ceiling(colorChars * this.charWidth) + (ColumnGap * 2) + SwatchSize
            : 0;

         // Measurement is transient (not a persisted setting), so always reserve its width so
         // the panel never needs resizing when the user activates measure mode mid-session.
         var sampleMouse = string.Format(
            InfoControl.PatternMousePrecision,
            99999,
            99999,
            Lens.PrecisionSpeedOptions[^1]);
         var sampleLensSize = string.Format(
            InfoControl.PatternLensSize,
            Lens.Defaults.MaxWidth,
            Lens.Defaults.MaxHeight);
         var sampleZoom = string.Format(InfoControl.PatternZoom, Lens.Defaults.MaxMagnification);
         var sampleMeasure = string.Format(InfoControl.PatternMeasure, 99999, 99999);

         var nonColorChars = new[]
            {
               lens.InfoShowMouse ? sampleMouse.Length : 0,
               lens.InfoShowSize ? sampleLensSize.Length : 0,
               lens.InfoShowZoom ? sampleZoom.Length : 0,
               sampleMeasure.Length,
            }.Max();

         var nonColorValueW = (int)Math.Ceiling(nonColorChars * this.charWidth);

         // Take the max so neither side clips the other when both are visible.
         var valuePixels = Math.Max(colorValueW, nonColorValueW);

         // Fall back to a minimum if nothing is enabled (shouldn't render, but be safe).
         if (valuePixels == 0)
         {
            valuePixels = (int)Math.Ceiling(4 * this.charWidth);
         }

         return ValueX + valuePixels + PanelPadding;
      }

      private void EnsureFinalResources(int w, int h)
      {
         if (this.finalMemDC == IntPtr.Zero)
         {
            this.finalMemDC = CreateCompatibleDC(IntPtr.Zero);
         }

         if ((this.finalBitmap != IntPtr.Zero) && (this.finalW == w) && (this.finalH == h))
         {
            return;
         }

         if (this.finalBitmap != IntPtr.Zero)
         {
            DeleteObject(this.finalBitmap);
            this.finalBits = IntPtr.Zero;
         }

         var bmi = MakeBmi(w, h);
         this.finalBitmap = CreateDIBSection(IntPtr.Zero, ref bmi, 0, out this.finalBits, IntPtr.Zero, 0);
         SelectObject(this.finalMemDC, this.finalBitmap);
         this.finalW = w;
         this.finalH = h;
      }

      private void EnsureLayeredResources(int w, int h)
      {
         if (this.layeredMemDC == IntPtr.Zero)
         {
            this.layeredMemDC = CreateCompatibleDC(IntPtr.Zero);
         }

         if ((this.layeredBitmap != IntPtr.Zero) && (this.cachedLayeredW == w) && (this.cachedLayeredH == h))
         {
            return;
         }

         if (this.layeredBitmap != IntPtr.Zero)
         {
            DeleteObject(this.layeredBitmap);
            this.layeredBitmap = IntPtr.Zero;
         }

         var bmi = MakeBmi(w, h);
         this.layeredBitmap = CreateDIBSection(IntPtr.Zero, ref bmi, 0, out this.layeredBits, IntPtr.Zero, 0);
         SelectObject(this.layeredMemDC, this.layeredBitmap);
         this.cachedLayeredW = w;
         this.cachedLayeredH = h;
      }

      private void EnsureShadow()
      {
         if ((this.shadowAlpha != null) && (this.cachedShadowContentW == this.ContentW)
                                        && (this.cachedShadowContentH == this.contentH))
         {
            return;
         }

         this.cachedShadowContentW = this.ContentW;
         this.cachedShadowContentH = this.contentH;

         var tw = this.ContentW + this.shadowMarginL + this.shadowMarginR;
         var th = this.contentH + this.shadowMarginT + this.shadowMarginB;

         // sx=sy=shadowMarginL: ShadowOffsetX=0, and shadowMarginT+scaledOffsetY=ShadowBlur=shadowMarginL.
         var sx = this.shadowMarginL;
         var sy = this.shadowMarginL;
         var src = new float[tw * th];
         for (var y = sy; y < sy + this.contentH; y++)
         for (var x = sx; x < sx + this.ContentW; x++)
         {
            src[(y * tw) + x] = 1f;
         }

         var temp = GaussianBlur1D(src, tw, th, this.shadowSigmaScaled, horizontal: true);
         var result = GaussianBlur1D(temp, tw, th, this.shadowSigmaScaled, horizontal: false);

         this.shadowAlpha = new byte[tw * th];
         for (var i = 0; i < result.Length; i++)
         {
            this.shadowAlpha[i] = (byte)Math.Round(result[i] * ShadowMaxAlpha);
         }
      }

      private void FreeFinalResources()
      {
         if (this.finalBitmap != IntPtr.Zero)
         {
            DeleteObject(this.finalBitmap);
            this.finalBitmap = IntPtr.Zero;
            this.finalBits = IntPtr.Zero;
         }

         if (this.finalMemDC != IntPtr.Zero)
         {
            DeleteDC(this.finalMemDC);
            this.finalMemDC = IntPtr.Zero;
         }
      }

      private void FreeLayeredResources()
      {
         if (this.layeredBitmap != IntPtr.Zero)
         {
            DeleteObject(this.layeredBitmap);
            this.layeredBitmap = IntPtr.Zero;
            this.layeredBits = IntPtr.Zero;
         }

         if (this.layeredMemDC != IntPtr.Zero)
         {
            DeleteDC(this.layeredMemDC);
            this.layeredMemDC = IntPtr.Zero;
         }

         this.cachedLayeredW = -1;
         this.cachedLayeredH = -1;
      }

      [SuppressMessage("ReSharper", "CognitiveComplexity")]
      private void RenderContent()
      {
         var d = this.infoData;
         var lens = Lens.Instance;

         using var g = Graphics.FromHdc(this.layeredMemDC);
         g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
         g.SmoothingMode = SmoothingMode.None;
         g.PixelOffsetMode = PixelOffsetMode.Half;
         g.ScaleTransform(this.dpiScale, this.dpiScale);

         // Derive background dimensions from physical contentW/H to avoid a 1px gap on the right or
         // bottom when round(logical * dpiScale) != logical * dpiScale (noninteger scale factors).
         var bgW = this.contentW / this.dpiScale;
         var bgH = this.contentH / this.dpiScale;
         var bgRect = new RectangleF(0, 0, bgW, bgH);
         using (var bg = new LinearGradientBrush(bgRect, Color.Black, Color.FromArgb(51, 51, 51), 45f))
         {
            g.FillRectangle(bg, bgRect);
         }

         using var labelBrush = new SolidBrush(Color.FromArgb(0xFF, 0xFF, 0xE1));
         using var valueBrush = new SolidBrush(Color.White);
         using var valueCopyBrush = new SolidBrush(Color.FromArgb(0xFF, 0xE5, 0x66));

         void DrawStringAtBaseline(string text, FontInfo font, Brush brush, float x, float baselineY)
         {
            baselineY += font.BaselineAdjustment;

            // GenericTypographic: PointF.Y is exactly the top of the cell ascent -- no internal-leading shift.
            g.DrawString(
               text,
               font.Font,
               brush,
               new PointF(x, baselineY - font.PixelAscent),
               StringFormat.GenericTypographic);
         }

         void DrawRow(string label, string value, float y)
         {
            var baseline = (y + RowHeight) - 3;

            // var layout = new RectangleF(LabelX, y, 40, RowHeight);
            // DrawOutline(g, Rectangle.Round(layout));
            // layout = new RectangleF(ValueX, y, valueW, RowHeight);
            // DrawOutline(g, Rectangle.Round(layout));
            // g.DrawLine(outlinePen, new PointF(LabelX - 1, baseline), new PointF(layout.Right + 2, baseline));

            DrawStringAtBaseline(label, this.labelFont, labelBrush, LabelX, baseline);

            var display = value;
            var brush = valueBrush;
            if (d.IsCopied(label))
            {
               display = "Copied";
               brush = valueCopyBrush;
            }

            DrawStringAtBaseline(display, this.valueFont!, brush, ValueX, baseline);
         }

         float y = PanelPadding; // tracked Y; advances as sections are drawn
         // Anchor swatch X to bgW (= contentW / dpiScale) so rounding never leaves a gap between
         // the swatch right edge and the panel right edge.
         var swatchRect = new RectangleF(bgW - PanelPadding - SwatchSize, y, SwatchSize, 0);

         // -- color-values section --------------------------------------------
         var cvAny = lens.InfoShowHex || lens.InfoShowRgb || lens.InfoShowHsl;
         var rowOffset = RowHeight + RowGap;
         if (cvAny)
         {
            // Icon baseline-aligned with the section's first row.
            this.iconColorValues.Draw(g, Color.White, IconX, y);
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
               DrawSwatch(d.ColorSwatch, g, swatchRect, this.dpiScale);
            }
         }

         // -- color-palette section (swatch) ----------------------------------
         if (cvAny)
         {
            y += SectionGap;
         }

         cvAny = lens.InfoShow12Bit || lens.InfoShowWeb;
         if (cvAny)
         {
            swatchRect.Height = RowHeight;

            this.iconColorPalette.Draw(g, Color.White, IconX, y);
            if (lens.InfoShow12Bit)
            {
               DrawRow("12-Bit", d.ValueColor12Bit, y);

               swatchRect.Y = y;
               DrawSwatch(d.Color12Bit, g, swatchRect, this.dpiScale);

               y += rowOffset;
            }

            if (lens.InfoShowWeb)
            {
               DrawRow("Web", d.ValueColorWeb, y);

               swatchRect.Y = y;
               DrawSwatch(d.ColorWeb, g, swatchRect, this.dpiScale);

               y += rowOffset;
            }
         }

         // -- mouse-position section ------------------------------------------
         if (cvAny)
         {
            y += SectionGap;
         }

         if (lens.InfoShowMouse)
         {
            this.iconMousePosition.Draw(g, Color.White, IconX, y);
            DrawRow("Mouse", d.MousePosition, y);
            y += rowOffset;
         }

         // -- lens-size section (no gap) --------------------------------------
         if (lens.InfoShowSize)
         {
            this.iconLensSize.Draw(g, Color.White, IconX, y);
            DrawRow("Lens", d.LensSize, y);
            y += rowOffset;
         }

         // -- magnification section (no gap) ----------------------------------
         if (lens.InfoShowZoom)
         {
            this.iconMagnification.Draw(g, Color.White, IconX, y);
            DrawRow("Zoom", d.ZoomFactor, y);
            y += rowOffset;
         }

         // -- measure section (active only while measuring) -------------------
         if (d.MeasureActive)
         {
            if (this.HasVisibleContent)
            {
               y += SectionGap;
            }

            this.iconRuler.Draw(g, Color.White, IconX, y);
            DrawRow("Ruler", d.MeasureValue, y);
         }
      }
   }
}
