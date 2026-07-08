// -------------------------------------------------------------------------------------
// <copyright file="LensForm.cs">
//   Copyright (c) 2026
//   Licensed under the MIT License. See LICENSE file in the project root.
// </copyright>
// -------------------------------------------------------------------------------------

namespace StrangeLens
{
   using System;
   using System.Diagnostics;
   using System.Diagnostics.CodeAnalysis;
   using System.Drawing;
   using System.Drawing.Drawing2D;
   using System.Drawing.Imaging;
   using System.Runtime.InteropServices;
   using System.Windows.Forms;

   using static NativeMethods;

   public partial class LensForm : Form
   {
      // -- Field groups -------------------------------------------------------------------
      // Screen capture fields (scrBmp/scrGrp/scrCaptureOrigin/checkerTile/checkerBrush): the
      //   small localized region captured around the cursor each frame.
      // Precision movement state (_lastCursorPos/_isSyntheticMove/_accumX/_accumY): used by
      //   the low-level mouse hook to implement subpixel precision movement.
      // Layered/final GDI resources (layered*/final*): cached DCs and DIBSections for
      //   UpdateLayeredWindow, allocated once and reused every frame.
      // Grid cache (gridBmp/cachedGrid*): rebuilt only when a relevant setting changes.
      // Shadow alpha cache (shadowAlpha/cachedShadowContent*): the Gaussian-blurred shadow
      //   alpha map, rebuilt only when the content size changes.

      private const Keys CtrlAltShift = Keys.Control | Keys.Alt | Keys.Shift;

      // Drop shadow base parameters (96-DPI design values; scaled per-frame in RenderFrame).
      private const int ShadowBlur = 16;

      private const byte ShadowMaxAlpha = 160;

      private const int ShadowOffsetY = 6;

      private const float ShadowSigma = 4.5f;

      private readonly InfoControl infoControl;

      private readonly InfoForm infoForm;

      private readonly MeasureForm measureForm;

      private readonly Timer timer;

      private double accumX;

      private double accumY;

      private GridStyleOption cachedGridStyle;

      private int cachedGridW, cachedGridH, cachedGridMag, cachedGridSize, cachedGridLineWidth;

      private int cachedShadowContentW = -1, cachedShadowContentH = -1;

      private TextureBrush? checkerBrush;

      private Bitmap? checkerTile;

      private IntPtr finalBitmap = IntPtr.Zero;

      private IntPtr finalBits = IntPtr.Zero;

      private IntPtr finalMemDC = IntPtr.Zero;

      private int finalW, finalH;

      private Bitmap? gridBmp;

      /// <summary>L-R mode (landscape): true when Lens+Info are positioned left of the cursor.</summary>
      private bool infoOnLeft;

      private bool isRendering;

      private bool isSyntheticMove;

      private Point lastCursorPos;

      /// <summary>Tracks the last screen the cursor was on; used to re-initialize flip state on
      ///    display change.</summary>
      private string? lastScreenName;

      private IntPtr layeredBitmap = IntPtr.Zero;

      private IntPtr layeredBits = IntPtr.Zero; // raw pixel pointer into the DIBSection

      private Graphics? layeredGrp;

      private IntPtr layeredMemDC = IntPtr.Zero;

      private int layeredW, layeredH;

      private int lineWidth = 1;

      private bool measureActive;

      private Point measureAnchor;

      private float measureAnimDir = 1f;

      private float measureAnimPhase;

      private IntPtr mouseHook = IntPtr.Zero;

      /// <summary>Keeps the delegate alive so the GC doesn't collect it while the native hook
      ///    holds a raw pointer to it.</summary>
      private LowLevelMouseProc? mouseHookProc;

      private int scaledPanelMargin;

      private Bitmap? scrBmp;

      private Point scrCaptureOrigin;

      private Graphics? scrGrp;

      private byte[]? shadowAlpha;

      // Shadow geometry, crosshair line width, and info-panel margin for the current DPI — recomputed each frame.
      private int shadowMarginL, shadowMarginR, shadowMarginT, shadowMarginB;

      private float shadowSigmaScaled;

      /// <summary>U-D mode (portrait): true when Info is positioned left of Lens.</summary>
      private bool udInfoLeft;

      /// <summary>U-D mode (portrait): true when Lens is positioned above the cursor.</summary>
      private bool udLensAbove;

      public LensForm()
      {
         this.InitializeComponent();

         this.FormBorderStyle = FormBorderStyle.None;
         this.ControlBox = false;
         this.ShowInTaskbar = false;
         this.TopMost = true;
         this.StartPosition = FormStartPosition.Manual;

         this.ApplyWidth();
         this.ApplyHeight();

         this.timer = new Timer
            {
               Interval = 16,
               Enabled = false,
            };
         this.timer.Tick += (_, _) => this.RenderFrame();

         this.infoControl = new InfoControl();
         this.infoForm = new InfoForm(this.infoControl);
         this.measureForm = new MeasureForm();
      }

      protected override CreateParams CreateParams
      {
         get
         {
            var cp = base.CreateParams;
            cp.ExStyle |= WS_EX_LAYERED;
            return cp;
         }
      }

      internal void CopyToClipboardColor12Bit()
      {
         Clipboard.SetText(this.infoControl.ValueColor12Bit);
         this.infoControl.NotifyCopied("12-Bit");
      }

      internal void CopyToClipboardColorHex()
      {
         Clipboard.SetText(this.infoControl.ValueColorHex);
         this.infoControl.NotifyCopied("HEX");
      }

      internal void CopyToClipboardColorHSL()
      {
         Clipboard.SetText($"hsl({this.infoControl.ValueColorHSL})");
         this.infoControl.NotifyCopied("HSL");
      }

      internal void CopyToClipboardColorRGB()
      {
         Clipboard.SetText($"rgb({this.infoControl.ValueColorRGB})");
         this.infoControl.NotifyCopied("RGB");
      }

      internal void CopyToClipboardColorWeb()
      {
         Clipboard.SetText(this.infoControl.ValueColorWeb);
         this.infoControl.NotifyCopied("Web");
      }

      internal void ToggleMeasure()
      {
         if (!this.Visible)
         {
            return;
         }

         if (this.measureActive)
         {
            this.measureActive = false;
            this.measureForm.Dismiss();
            this.infoControl.SetMeasure(false);
         }
         else
         {
            this.measureActive = true;
            this.measureAnchor = (ModifierKeys & CtrlAltShift) == CtrlAltShift
               ? this.lastCursorPos
               : Cursor.Position;
            this.measureAnimPhase = 0f;
            this.measureAnimDir = 1f;
         }
      }

      protected override void OnFormClosing(FormClosingEventArgs e)
      {
         this.timer.Stop();
         if (this.mouseHook != IntPtr.Zero)
         {
            UnhookWindowsHookEx(this.mouseHook);
            this.mouseHook = IntPtr.Zero;
         }

         this.infoForm.Close();
         if (this.measureActive)
         {
            this.measureActive = false;
            this.measureForm.Dismiss();
            this.infoControl.SetMeasure(false);
         }

         this.measureForm.Close();
         this.FreeLayeredResources();
         this.FreeFinalResources();
         this.shadowAlpha = null;
         this.gridBmp?.Dispose();
         this.checkerBrush?.Dispose();
         this.checkerTile?.Dispose();
         this.scrGrp?.Dispose();
         this.scrBmp?.Dispose();
         base.OnFormClosing(e);
      }

      protected override void OnKeyDown(KeyEventArgs e)
      {
         base.OnKeyDown(e);

         switch (e.KeyCode)
         {
            case Keys.Right: this.ChangePosition(1, 0); break;
            case Keys.Up: this.ChangePosition(0, -1); break;
            case Keys.Down: this.ChangePosition(0, 1); break;
            case Keys.Left: this.ChangePosition(-1, 0); break;
         }
      }

      protected override void OnKeyUp(KeyEventArgs e)
      {
         base.OnKeyUp(e);

         switch (e.KeyCode)
         {
            case Keys.Escape: this.Close(); break;
            case Keys.Oemplus when e.Control: this.ChangeMagnification(1); break;
            case Keys.OemMinus when e.Control: this.ChangeMagnification(-1); break;
         }
      }

      protected override void OnMouseWheel(MouseEventArgs e)
      {
         base.OnMouseWheel(e);

         switch (ModifierKeys)
         {
            case Keys.Control:
               if (e.Delta > 0)
               {
                  this.IncreaseSize();
               }
               else
               {
                  this.DecreaseSize();
               }

               break;
         }
      }

      /// <summary>Layered window content is managed entirely by <c>UpdateLayeredWindow</c> via
      ///    <see cref="RenderFrame"/> -- no GDI+ painting here.</summary>
      protected override void OnPaint(PaintEventArgs e)
      {
      }

      /// <summary>See <see cref="OnPaint"/>.</summary>
      protected override void OnPaintBackground(PaintEventArgs e)
      {
      }

      protected override void OnShown(EventArgs e)
      {
         base.OnShown(e);
         // SetWindowDisplayAffinity(WDA_EXCLUDEFROMCAPTURE) deadlocks with DWM on WS_EX_LAYERED windows.

         // Rough initial position -- first RenderFrame will correct it via UpdateLayeredWindow.
         var pos = Cursor.Position;
         this.lastCursorPos = pos;
         this.Left = pos.X + 20;
         this.Top = pos.Y - (this.Height / 2);

         this.timer.Enabled = true;

         // Low-level mouse hook for Ctrl+Alt+Shift+scroll zoom -- works even when the lens lacks focus.
         this.mouseHookProc = this.MouseHookCallback;
         this.mouseHook = SetWindowsHookEx(WH_MOUSE_LL, this.mouseHookProc, GetModuleHandle(null), 0);
         if (this.mouseHook == IntPtr.Zero)
         {
            Debug.WriteLine($"SetWindowsHookEx failed: error {Marshal.GetLastWin32Error()}");
         }
      }

      private static void DrawBorder(Graphics g, int w, int h, bool focused, int lineWidth)
      {
         var color = focused ? SystemColors.Highlight : SystemColors.ControlDarkDark;
         var hdc = g.GetHdc();
         try
         {
            var pen = CreatePen(0 /*PS_SOLID*/, 1, ToColorRef(color));
            var oldPen = SelectObject(hdc, pen);
            // Outer 2*lineWidth px in the focus/unfocus color, inner 1px in black.
            // LineTo excludes its endpoint, so (0, 0)->(w, 0) covers columns 0...w-1 exactly.
            var outerW = 2 * lineWidth;
            for (var i = 0; i < outerW; i++)
            {
               MoveToEx(hdc, 0, i, IntPtr.Zero);
               LineTo(hdc, w, i); // top
               MoveToEx(hdc, 0, h - 1 - i, IntPtr.Zero);
               LineTo(hdc, w, h - 1 - i); // bottom
               MoveToEx(hdc, i, 0, IntPtr.Zero);
               LineTo(hdc, i, h); // left
               MoveToEx(hdc, w - 1 - i, 0, IntPtr.Zero);
               LineTo(hdc, w - 1 - i, h); // right
            }

            SelectObject(hdc, oldPen);
            DeleteObject(pen);

            // Inner black band of lineWidth px, starting at offset outerW.
            var b = outerW;
            var blackPen = CreatePen(0 /*PS_SOLID*/, 1, 0x00000000 /*black*/);
            SelectObject(hdc, blackPen);
            for (var j = 0; j < lineWidth; j++)
            {
               MoveToEx(hdc, b, b + j, IntPtr.Zero);
               LineTo(hdc, w - b, b + j); // top
               MoveToEx(hdc, b, h - b - 1 - j, IntPtr.Zero);
               LineTo(hdc, w - b, h - b - 1 - j); // bottom
               MoveToEx(hdc, b + j, b, IntPtr.Zero);
               LineTo(hdc, b + j, h - b); // left
               MoveToEx(hdc, w - b - 1 - j, b, IntPtr.Zero);
               LineTo(hdc, w - b - 1 - j, h - b); // right
            }

            SelectObject(hdc, oldPen);
            DeleteObject(blackPen);
         }
         finally
         {
            g.ReleaseHdc(hdc);
         }
      }

      private static void DrawHorizontalLines(Graphics g, Pen pen, int cy, int step, int w, int h)
      {
         for (var d = step; (cy - d >= 0) || (cy + d < h); d += step)
         {
            if (cy - d >= 0)
            {
               g.DrawLine(pen, 0, cy - d, w, cy - d);
            }

            if (cy + d < h)
            {
               g.DrawLine(pen, 0, cy + d, w, cy + d);
            }
         }
      }

      private static void DrawVerticalLines(Graphics g, Pen pen, int cx, int step, int w, int h)
      {
         for (var d = step; (cx - d >= 0) || (cx + d < w); d += step)
         {
            if (cx - d >= 0)
            {
               g.DrawLine(pen, cx - d, 0, cx - d, h);
            }

            if (cx + d < w)
            {
               g.DrawLine(pen, cx + d, 0, cx + d, h);
            }
         }
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

      private static bool IsOnCrosshairBoundary(int x, int y, int cx, int cy, int mag, int lineWidth)
      {
         return ((y >= cy) && (y < cy + lineWidth)) || ((x >= cx) && (x < cx + lineWidth))
                                                    || ((y >= cy + mag) && (y < cy + mag + lineWidth)
                                                                        && (x >= cx)
                                                                        && (x <= (cx + mag + lineWidth) - 1))
                                                    || ((x >= cx + mag) && (x < cx + mag + lineWidth)
                                                                        && (y >= cy)
                                                                        && (y <= (cy + mag + lineWidth) - 1));
      }

      private static uint ToColorRef(Color c)
      {
         return (uint)(c.R | (c.G << 8) | (c.B << 16));
      }

      /// <summary>Applies per-pixel difference blend to crosshair lines directly in the
      ///    DIBSection. Result = |penColor - dst| per channel, so the line always contrasts with
      ///    the content. lineWidth scales with DPI so the crosshair stays visible at high DPI.</summary>
      [SuppressMessage("ReSharper", "CognitiveComplexity")]
      private void ApplyDifferenceCrosshair(int w, int h)
      {
         if (this.layeredBits == IntPtr.Zero)
         {
            return;
         }

         var lens = Lens.Instance;
         int cx = w / 2, cy = h / 2, mag = lens.Magnification;
         var lw = this.lineWidth;
         var stride = w * 4;

         // Horizontal band — rows cy...cy+lw-1, all columns.
         for (var row = cy; row < cy + lw; row++)
         for (var x = 0; x < w; x++)
         {
            this.ApplyDiffPixel((row * stride) + (x * 4));
         }

         // Vertical band — cols cx..cx+lw-1, skipping rows owned by horizontal band.
         for (var y = 0; y < h; y++)
         {
            if ((y >= cy) && (y < cy + lw))
            {
               continue;
            }

            for (var col = cx; col < cx + lw; col++)
            {
               this.ApplyDiffPixel((y * stride) + (col * 4));
            }
         }

         // Bottom of sampled-pixel box — lw rows, skip columns owned by vertical band.
         for (var row = cy + mag; row < cy + mag + lw; row++)
         for (var x = cx + lw; x < cx + mag; x++)
         {
            this.ApplyDiffPixel((row * stride) + (x * 4));
         }

         // Right side of box — lw columns, skip rows owned by horizontal band; close corner.
         for (var y = cy + lw; y <= (cy + mag + lw) - 1; y++)
         for (var col = cx + mag; col < cx + mag + lw; col++)
         {
            this.ApplyDiffPixel((y * stride) + (col * 4));
         }
      }

      private void ApplyDifferenceGrid(int w, int h)
      {
         if (this.layeredBits == IntPtr.Zero)
         {
            return;
         }

         this.EnsureGridBitmap(w, h);
         if (this.gridBmp == null)
         {
            return;
         }

         var lens = Lens.Instance;
         int cx = w / 2, cy = h / 2, mag = lens.Magnification;
         var opacity = (byte)((lens.GridOpacity * 255) / 100);
         var contentStride = w * 4;
         var bmpData = this.gridBmp.LockBits(
            new Rectangle(0, 0, w, h),
            ImageLockMode.ReadOnly,
            PixelFormat.Format32bppArgb);
         try
         {
            for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
            {
               var gridPixel = Marshal.ReadInt32(bmpData.Scan0, (y * bmpData.Stride) + (x * 4));
               // Skip transparent pixels and pixels the crosshair will overwrite.
               if (((byte)(gridPixel >> 24) == 0) || IsOnCrosshairBoundary(x, y, cx, cy, mag, this.lineWidth))
               {
                  continue;
               }

               this.ApplyDiffPixel((y * contentStride) + (x * 4), opacity);
            }
         }
         finally
         {
            this.gridBmp.UnlockBits(bmpData);
         }
      }

      private void ApplyDiffPixel(int offset, byte opacity = 0xFF)
      {
         var pixel = Marshal.ReadInt32(this.layeredBits, offset);
         var b = (byte)(pixel & 0xFF);
         var gr = (byte)((pixel >> 8) & 0xFF);
         var r = (byte)((pixel >> 16) & 0xFF);
         // BT.601 luma (integer, >>8 scale): white on dark content, black on light.
         var luma = ((77 * r) + (150 * gr) + (29 * b)) >> 8;
         var pen = luma < 128 ? 255 : 0;
         int rb, rg, rr;
         if (opacity == 0xFF)
         {
            rb = rg = rr = pen;
         }
         else
         {
            var inv = 255 - opacity;
            rb = ((opacity * pen) + (inv * b)) >> 8;
            rg = ((opacity * pen) + (inv * gr)) >> 8;
            rr = ((opacity * pen) + (inv * r)) >> 8;
         }

         var result = unchecked((int)((uint)rb | ((uint)rg << 8) | ((uint)rr << 16) | 0xFF000000u));
         Marshal.WriteInt32(this.layeredBits, offset, result);
      }

      private void ApplyHeight()
      {
         this.Height = Lens.Instance.Height;
      }

      private void ApplyWidth()
      {
         this.Width = Lens.Instance.Width;
      }

      private void ChangeMagnification(short amount)
      {
         var before = Lens.Instance.Magnification;
         Lens.Instance.Magnification = (byte)(Lens.Instance.Magnification + amount);
         if (Lens.Instance.Magnification == before)
         {
            return;
         }

         this.RenderFrame();
      }

      private void ChangePosition(int x, int y)
      {
         var p = Cursor.Position;
         p.X += x;
         p.Y += y;
         this.lastCursorPos = p;
         this.isSyntheticMove = true;
         Cursor.Position = p;
      }

      private void CommitLayeredWindow(Point winPos, int w, int h)
      {
         var winSize = new Size(w, h);
         var srcPos = Point.Empty;
         // AlphaFormat=1 (AC_SRC_ALPHA): use per-pixel alpha from the DIBSection.
         // finalBits: pre-multiplied BGRA -- content pixels have alpha=255,
         // shadow pixels have alpha < 255, transparent margin pixels have alpha = 0.
         var blend = new BLENDFUNCTION
            {
               BlendOp = 0,
               BlendFlags = 0,
               SourceConstantAlpha = 255,
               AlphaFormat = 1,
            };
         var ok = UpdateLayeredWindow(
            this.Handle,
            IntPtr.Zero,
            ref winPos,
            ref winSize,
            this.finalMemDC,
            ref srcPos,
            0,
            ref blend,
            ULW_ALPHA);
         if (!ok)
         {
            Debug.WriteLine($"UpdateLayeredWindow failed: error {Marshal.GetLastWin32Error()}");
         }
      }

      /// <summary>Composites shadow plus content into <c>finalBits</c> for
      ///    <c>UpdateLayeredWindow</c> . Content pixels are stamped alpha=255 (fully opaque).
      ///    Shadow pixels carry their Gaussian-blurred alpha. Regions outside both are transparent
      ///    (alpha=0).</summary>
      private void CompositeFinalFrame(int cw, int ch, int tw, int th)
      {
         if ((this.finalBits == IntPtr.Zero) || (this.layeredBits == IntPtr.Zero))
         {
            return;
         }

         this.EnsureShadow(cw, ch);

         // Clear final bitmap to transparent.
         var totalPixels = tw * th;
         for (var i = 0; i < totalPixels; i++)
         {
            Marshal.WriteInt32(this.finalBits, i * 4, 0);
         }

         // Write shadow. Pixel is pre-multiplied BGRA: black with alpha=a -> (a<<24)|0.
         var alpha = this.shadowAlpha!;
         for (var i = 0; i < totalPixels; i++)
         {
            var a = alpha[i];
            if (a > 0)
            {
               Marshal.WriteInt32(this.finalBits, i * 4, unchecked((int)((uint)a << 24)));
            }
         }

         // Copy content pixels at (shadowMarginL, shadowMarginT), forcing alpha=255.
         // Content is fully opaque -- GDI writes alpha=0, so we override it here.
         var contentStride = cw * 4;
         var finalStride = tw * 4;
         for (var y = 0; y < ch; y++)
         {
            for (var x = 0; x < cw; x++)
            {
               var src = Marshal.ReadInt32(this.layeredBits, (y * contentStride) + (x * 4));
               var dst = (src & 0x00FFFFFF) | unchecked((int)0xFF000000u);
               Marshal.WriteInt32(
                  this.finalBits,
                  ((y + this.shadowMarginT) * finalStride) + ((x + this.shadowMarginL) * 4),
                  dst);
            }
         }
      }

      /// <summary>L-R mode (landscape): positions Lens left or right of the cursor with
      ///    two-threshold hysteresis to avoid edge flicker.</summary>
      private (int lensLeft, int lensTop, bool infoLeft) ComputeLandscapeLayout(
         Point cursorPos,
         Screen screen,
         int w,
         int h,
         int infoW,
         int maxH,
         bool screenChanged)
      {
         // Two independent thresholds on opposite edges give a large dead zone in the
         // center, so neither flip fires until the panel genuinely clips an edge.
         var gapX = (int)(Math.Ceiling(w / (float)Lens.Defaults.MinMagnification) / 2) + 10;
         var rightClips = cursorPos.X + gapX + w + infoW + this.scaledPanelMargin > screen.Bounds.Right;
         var leftClips = cursorPos.X - gapX - w - infoW - this.scaledPanelMargin < screen.Bounds.Left;

         if (screenChanged)
         {
            this.infoOnLeft = rightClips;
         }
         else if (!this.infoOnLeft && rightClips && !leftClips)
         {
            this.infoOnLeft = true;
         }
         else if (this.infoOnLeft && leftClips && !rightClips)
         {
            this.infoOnLeft = false;
         }

         var lensLeft = this.infoOnLeft ? cursorPos.X - gapX - w : cursorPos.X + gapX;
         var lensTop = Math.Max(
            screen.Bounds.Top + this.scaledPanelMargin,
            Math.Min(cursorPos.Y - (h / 2), screen.Bounds.Bottom - maxH - this.scaledPanelMargin));
         return (lensLeft, lensTop, this.infoOnLeft);
      }

      /// <summary>Computes the lens window position and info-panel side for one frame. Updates
      ///    hysteresis state fields so repeated calls converge without flickering.</summary>
      private (int lensLeft, int lensTop, bool infoLeft) ComputeLayout(
         Point cursorPos,
         Screen screen,
         int w,
         int h,
         int infoW,
         int maxH,
         bool screenChanged,
         bool portrait)
      {
         return portrait
            ? this.ComputePortraitLayout(cursorPos, screen, w, h, infoW, maxH, screenChanged)
            : this.ComputeLandscapeLayout(cursorPos, screen, w, h, infoW, maxH, screenChanged);
      }

      /// <summary>U-D mode (portrait): positions Lens above or below the cursor; Info hangs left
      ///    or right of the Lens.</summary>
      private (int lensLeft, int lensTop, bool infoLeft) ComputePortraitLayout(
         Point cursorPos,
         Screen screen,
         int w,
         int h,
         int infoW,
         int maxH,
         bool screenChanged)
      {
         // Lens is centered horizontally on the cursor; Info hangs off left or right.
         // Horizontal free-travel zone: lensLeft is clamped to screen edges so the
         // cursor can move near the left/right boundary without clipping the panel.
         var gapY = (int)(Math.Ceiling(h / (float)Lens.Defaults.MinMagnification) / 2) + 10;
         var topClips = cursorPos.Y - gapY - h < screen.Bounds.Top + this.scaledPanelMargin;
         var bottomClips = cursorPos.Y + gapY + maxH > screen.Bounds.Bottom - this.scaledPanelMargin;

         if (screenChanged)
         {
            this.udLensAbove = !topClips;
         }
         else if (this.udLensAbove && topClips && !bottomClips)
         {
            this.udLensAbove = false;
         }
         else if (!this.udLensAbove && bottomClips && !topClips)
         {
            this.udLensAbove = true;
         }

         var lensLeft = Math.Clamp(
            cursorPos.X - (w / 2),
            screen.Bounds.Left + this.scaledPanelMargin,
            screen.Bounds.Right - w - this.scaledPanelMargin);
         var lensTop = this.udLensAbove
            ? Math.Clamp(
               cursorPos.Y - gapY - h,
               screen.Bounds.Top + this.scaledPanelMargin,
               screen.Bounds.Bottom - maxH - this.scaledPanelMargin)
            : Math.Min(cursorPos.Y + gapY, screen.Bounds.Bottom - maxH - this.scaledPanelMargin);

         // Info left/right of Lens, with the same two-threshold hysteresis.
         var rightClipsUD = lensLeft + w + infoW + this.scaledPanelMargin > screen.Bounds.Right;
         var leftClipsUD = lensLeft - infoW - this.scaledPanelMargin < screen.Bounds.Left;

         if (screenChanged)
         {
            this.udInfoLeft = rightClipsUD;
         }
         else if (!this.udInfoLeft && rightClipsUD && !leftClipsUD)
         {
            this.udInfoLeft = true;
         }
         else if (this.udInfoLeft && leftClipsUD && !rightClipsUD)
         {
            this.udInfoLeft = false;
         }

         return (lensLeft, lensTop, this.udInfoLeft);
      }

      private void CopyScreen(Point cursorPos, int w, int h)
      {
         var lens = Lens.Instance;
         var captureW = (int)Math.Ceiling(w / (float)lens.Magnification) + 2;
         var captureH = (int)Math.Ceiling(h / (float)lens.Magnification) + 2;

         if ((this.scrBmp == null) || (this.scrBmp.Width != captureW) || (this.scrBmp.Height != captureH))
         {
            this.checkerBrush?.Dispose();
            this.checkerTile?.Dispose();
            this.scrGrp?.Dispose();
            this.scrBmp?.Dispose();

            this.scrBmp = new Bitmap(captureW, captureH);
            this.scrGrp = Graphics.FromImage(this.scrBmp);

            // Build a 16x16 checkerboard tile -- the standard "no content" indicator.
            this.checkerTile = new Bitmap(16, 16);
            using (var tg = Graphics.FromImage(this.checkerTile))
            using (var light = new SolidBrush(Color.FromArgb(200, 200, 200)))
            using (var dark = new SolidBrush(Color.FromArgb(120, 120, 120)))
            {
               tg.FillRectangle(light, 0, 0, 8, 8);
               tg.FillRectangle(dark, 8, 0, 8, 8);
               tg.FillRectangle(dark, 0, 8, 8, 8);
               tg.FillRectangle(light, 8, 8, 8, 8);
            }

            this.checkerBrush = new TextureBrush(this.checkerTile);
         }

         this.scrCaptureOrigin = new Point(cursorPos.X - (captureW / 2), cursorPos.Y - (captureH / 2));
         var captureRect = new Rectangle(this.scrCaptureOrigin, new Size(captureW, captureH));

         // Pre-fill with a checkerboard. Anything not overwritten below is out of monitor bounds.
         this.scrGrp!.FillRectangle(this.checkerBrush!, 0, 0, captureW, captureH);

         // Copy only the portions that fall within actual monitor bounds.
         foreach (var screen in Screen.AllScreens)
         {
            var valid = Rectangle.Intersect(captureRect, screen.Bounds);
            if (valid.IsEmpty)
            {
               continue;
            }

            var dest = new Point(valid.X - captureRect.X, valid.Y - captureRect.Y);
            this.scrGrp.CopyFromScreen(valid.Location, dest, valid.Size);
         }
      }

      private void DecreaseSize()
      {
         Debug.WriteLine("DECREASE FORM SIZE KEEPING ASPECT RATIO");
      }

      private void EnsureFinalResources(int w, int h)
      {
         if (this.finalMemDC == IntPtr.Zero)
         {
            this.finalMemDC = CreateCompatibleDC(IntPtr.Zero);
         }

         if ((this.finalBitmap == IntPtr.Zero) || (this.finalW != w) || (this.finalH != h))
         {
            if (this.finalBitmap != IntPtr.Zero)
            {
               DeleteObject(this.finalBitmap);
               this.finalBits = IntPtr.Zero;
            }

            var bmi = new BITMAPINFO
               {
                  bmiHeader = new BITMAPINFOHEADER
                     {
                        biSize = (uint)Marshal.SizeOf<BITMAPINFOHEADER>(),
                        biWidth = w,
                        biHeight = -h,
                        biPlanes = 1,
                        biBitCount = 32,
                        biCompression = 0,
                     },
               };
            this.finalBitmap = CreateDIBSection(IntPtr.Zero, ref bmi, 0, out this.finalBits, IntPtr.Zero, 0);
            SelectObject(this.finalMemDC, this.finalBitmap);
            this.finalW = w;
            this.finalH = h;
         }
      }

      /// <summary>Rebuilds the cached grid bitmap when any relevant setting has changed.</summary>
      private void EnsureGridBitmap(int w, int h)
      {
         var lens = Lens.Instance;
         var gridStyle = lens.GridStyle;
         if ((this.gridBmp != null) && (this.cachedGridW == w) && (this.cachedGridH == h)
             && (this.cachedGridMag == lens.Magnification) && (this.cachedGridSize == lens.GridSize)
             && (this.cachedGridStyle == gridStyle) && (this.cachedGridLineWidth == this.lineWidth))
         {
            return;
         }

         this.gridBmp?.Dispose();
         this.gridBmp = null;
         this.cachedGridW = w;
         this.cachedGridH = h;
         this.cachedGridMag = lens.Magnification;
         this.cachedGridSize = lens.GridSize;
         this.cachedGridStyle = gridStyle;
         this.cachedGridLineWidth = this.lineWidth;

         if (gridStyle == GridStyleOption.None)
         {
            return;
         }

         this.gridBmp = new Bitmap(w, h, PixelFormat.Format32bppArgb);
         using var g = Graphics.FromImage(this.gridBmp);
         g.Clear(Color.Transparent);
         int cx = w / 2, cy = h / 2;
         var step = lens.GridSize * lens.Magnification;
         using var pen = new Pen(Color.White, this.lineWidth);
         pen.DashStyle = gridStyle.DashStyle();
         DrawHorizontalLines(g, pen, cy, step, w, h);
         DrawVerticalLines(g, pen, cx, step, w, h);
      }

      /// <summary>Ensures the GDI memory DC and bitmap are allocated and match the requested size.</summary>
      private void EnsureLayeredResources(int w, int h)
      {
         if (this.layeredMemDC == IntPtr.Zero)
         {
            this.layeredMemDC = CreateCompatibleDC(IntPtr.Zero);
         }

         if ((this.layeredBitmap == IntPtr.Zero) || (this.layeredW != w) || (this.layeredH != h))
         {
            this.layeredGrp?.Dispose();
            this.layeredGrp = null;
            if (this.layeredBitmap != IntPtr.Zero)
            {
               DeleteObject(this.layeredBitmap);
            }

            // UpdateLayeredWindow requires a 32-bit DIB section -- a DDB is not reliable.
            var bmi = new BITMAPINFO
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
            this.layeredBitmap = CreateDIBSection(
               IntPtr.Zero,
               ref bmi,
               0,
               out this.layeredBits,
               IntPtr.Zero,
               0);
            SelectObject(this.layeredMemDC, this.layeredBitmap);
            this.layeredGrp = Graphics.FromHdc(this.layeredMemDC);
            this.layeredW = w;
            this.layeredH = h;
         }
      }

      /// <summary>Rebuilds the Gaussian shadow alpha map when content size changes.</summary>
      private void EnsureShadow(int contentW, int contentH)
      {
         if ((this.shadowAlpha != null) && (this.cachedShadowContentW == contentW)
                                        && (this.cachedShadowContentH == contentH))
         {
            return;
         }

         this.cachedShadowContentW = contentW;
         this.cachedShadowContentH = contentH;

         var tw = contentW + this.shadowMarginL + this.shadowMarginR;
         var th = contentH + this.shadowMarginT + this.shadowMarginB;

         // Shadow source: the content rectangle offset by (ShadowOffsetX, ShadowOffsetY).
         // ShadowOffsetX=0, so sx==shadowMarginL; shadowMarginT+scaledOffsetY also equals shadowMarginL.
         var sx = this.shadowMarginL;
         var sy = this.shadowMarginL;
         var src = new float[tw * th];
         for (var y = sy; y < sy + contentH; y++)
         for (var x = sx; x < sx + contentW; x++)
         {
            src[(y * tw) + x] = 1f;
         }

         // Separable Gaussian blur (two passes, zero-padded boundary).
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
         this.layeredGrp?.Dispose();
         this.layeredGrp = null;
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
      }

      /// <summary>Handles WM_MOUSEMOVE inside the low-level hook. Returns a hook return value when
      ///    the event should be consumed or forwarded immediately; returns null to fall through to
      ///    CallNextHookEx.</summary>
      [SuppressMessage("ReSharper", "CognitiveComplexity")]
      private IntPtr? HandlePrecisionMove(IntPtr lParam, bool hasCtrlAltShift, int nCode, IntPtr wParam)
      {
         // MSLLHOOKSTRUCT: POINT is at offset 0 (two int32s).
         var ptX = Marshal.ReadInt32(lParam, 0);
         var ptY = Marshal.ReadInt32(lParam, 4);

         if (this.isSyntheticMove)
         {
            // Always clear -- safety valve against the flag getting stuck (e.g., OS clamps
            // a SetCursorPos to the screen edge and the synthetic arrives at a different
            // coordinate than the lastCursorPos).
            this.isSyntheticMove = false;
            if ((ptX == this.lastCursorPos.X) && (ptY == this.lastCursorPos.Y))
            {
               // Coordinates match our SetCursorPos target: this is the synthetic.
               return CallNextHookEx(this.mouseHook, nCode, wParam, lParam);
            }

            // Coordinates don't match: stale real event queued before our SetCursorPos.
            // Consume it so the unscaled position never reaches Cursor.Position.
            return 1;
         }

         if (hasCtrlAltShift)
         {
            var dx = ptX - this.lastCursorPos.X;
            var dy = ptY - this.lastCursorPos.Y;
            if ((dx != 0) || (dy != 0))
            {
               var scale = Lens.Instance.PrecisionSpeed / 100.0;
               this.accumX += dx * scale;
               this.accumY += dy * scale;
               var moveX = (int)this.accumX;
               var moveY = (int)this.accumY;
               this.accumX -= moveX;
               this.accumY -= moveY;
               var newX = this.lastCursorPos.X + moveX;
               var newY = this.lastCursorPos.Y + moveY;
               this.lastCursorPos = new Point(newX, newY);
               if ((moveX != 0) || (moveY != 0))
               {
                  // Only call SetCursorPos when the cursor actually moves to a new position.
                  // A same-position call generates no WM_MOUSEMOVE synthetic, which would leave
                  // isSyntheticMove stuck true and cause the next real event to be wrongly
                  // consumed as stale -- leaving the lastCursorPos stale and causing a jump on
                  // the next precision entry.
                  this.isSyntheticMove = true;
                  SetCursorPos(newX, newY);
               }

               return 1;
            }
         }
         else
         {
            this.lastCursorPos = new Point(ptX, ptY);
            this.accumX = 0;
            this.accumY = 0;
         }

         return null;
      }

      private void HandleScrollZoom(IntPtr lParam)
      {
         // MSLLHOOKSTRUCT.mouseData is at offset 8; high word is the wheel delta (signed short).
         var mouseData = Marshal.ReadInt32(lParam, 8);
         var wheelDelta = (short)(mouseData >> 16);
         this.ChangeMagnification((short)(wheelDelta > 0 ? 1 : -1));
      }

      private void IncreaseSize()
      {
         Debug.WriteLine("INCREASE FORM SIZE KEEPING ASPECT RATIO");
      }

      private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
      {
         if (nCode >= 0)
         {
            var msg = wParam.ToInt32();
            var hasCtrlAltShift = (ModifierKeys & CtrlAltShift) == CtrlAltShift;

            if (msg == WM_MOUSEMOVE)
            {
               var consumed = this.HandlePrecisionMove(lParam, hasCtrlAltShift, nCode, wParam);
               if (consumed.HasValue)
               {
                  return consumed.Value;
               }
            }
            else if ((msg == WM_MOUSEWHEEL) && hasCtrlAltShift)
            {
               this.HandleScrollZoom(lParam);
               return 1;
            }
         }

         return CallNextHookEx(this.mouseHook, nCode, wParam, lParam);
      }

      private void RenderFrame()
      {
         if (this.isRendering)
         {
            return;
         }

         this.isRendering = true;
         try
         {
            var precisionActive = (ModifierKeys & CtrlAltShift) == CtrlAltShift;
            var cursorPos = precisionActive ? this.lastCursorPos : Cursor.Position;

            if (this.measureActive)
            {
               this.measureAnimPhase = Math.Clamp(
                  this.measureAnimPhase + (this.measureAnimDir * 0.1f),
                  0f,
                  1f);
               if ((this.measureAnimPhase >= 1f) || (this.measureAnimPhase <= 0f))
               {
                  this.measureAnimDir = -this.measureAnimDir;
               }

               var mw = Math.Abs(cursorPos.X - this.measureAnchor.X);
               var mh = Math.Abs(cursorPos.Y - this.measureAnchor.Y);
               this.measureForm.Update(this.measureAnchor, cursorPos, this.measureAnimPhase);
               this.infoControl.SetMeasure(true, mw, mh);
            }

            var lens = Lens.Instance;
            var dpiScale = this.DeviceDpi / 96f;
            var w = (int)Math.Round(lens.Width * dpiScale);
            var h = (int)Math.Round(lens.Height * dpiScale);
            this.lineWidth = Math.Max(1, (int)Math.Round(dpiScale));
            this.shadowMarginL = this.shadowMarginR = (int)Math.Round(ShadowBlur * dpiScale);
            this.shadowMarginT = (int)Math.Round((ShadowBlur - ShadowOffsetY) * dpiScale);
            this.shadowMarginB = (int)Math.Round((ShadowBlur + ShadowOffsetY) * dpiScale);
            this.shadowSigmaScaled = ShadowSigma * dpiScale;
            this.scaledPanelMargin = (int)Math.Round(this.scaledPanelMargin * dpiScale);

            // Select the axis based on screen orientation.
            // Portrait screens (H > W) use U-D placement to avoid constant left/right flipping
            // when the combined panel width exceeds half the display width.
            // Gaps are derived from the capture region at minimum zoom, so the panel never
            // overlaps the captured area regardless of the current zoom level.
            var screen = Screen.FromPoint(cursorPos);
            var portrait = screen.Bounds.Height > screen.Bounds.Width;
            var infoW = this.infoForm.HasVisibleContent ? this.infoForm.ContentW + this.scaledPanelMargin : 0;
            var infoH = this.infoForm.HasVisibleContent ? this.infoForm.ContentH : 0;
            var maxH = Math.Max(h, infoH);
            var screenChanged = screen.DeviceName != this.lastScreenName;
            if (screenChanged)
            {
               this.lastScreenName = screen.DeviceName;
            }

            var (lensLeft, lensTop, infoLeft) = this.ComputeLayout(
               cursorPos,
               screen,
               w,
               h,
               infoW,
               maxH,
               screenChanged,
               portrait);

            var totalW = w + this.shadowMarginL + this.shadowMarginR;
            var totalH = h + this.shadowMarginT + this.shadowMarginB;
            // Window top-left is inset by the shadow margins so content appears at lensLeft/lensTop.
            var winPos = new Point(lensLeft - this.shadowMarginL, lensTop - this.shadowMarginT);

            this.EnsureLayeredResources(w, h);
            this.EnsureFinalResources(totalW, totalH);

            var g = this.layeredGrp!;
            g.ResetTransform();
            g.ResetClip();

            // Zoom transform: cursor position maps to the center of the lens content.
            g.TranslateTransform(w / 2f, h / 2f);
            g.ScaleTransform(lens.Magnification, lens.Magnification);
            g.TranslateTransform(-cursorPos.X, -cursorPos.Y);

            g.Clear(Color.Black);

            (g.InterpolationMode, g.PixelOffsetMode) = lens.Scaling switch
               {
                  ScalingModeOption.NearestNeighbor => (InterpolationMode.NearestNeighbor,
                     PixelOffsetMode.Half),
                  ScalingModeOption.Bilinear => (InterpolationMode.Bilinear, PixelOffsetMode.Default),
                  ScalingModeOption.HighQualityBilinear => (InterpolationMode.HighQualityBilinear,
                     PixelOffsetMode.Default),
                  ScalingModeOption.Bicubic => (InterpolationMode.Bicubic, PixelOffsetMode.Default),
                  ScalingModeOption.HighQualityBicubic => (InterpolationMode.HighQualityBicubic,
                     PixelOffsetMode.Default),
                  _ => (InterpolationMode.NearestNeighbor, PixelOffsetMode.Half),
               };

            this.CopyScreen(cursorPos, w, h);
            // Sample the pixel at the cursor -- scrBmp is centered on cursorPos so the
            // center pixel maps exactly to the crosshair origin (the spec's sampled pixel).
            var sampledColor = this.scrBmp!.GetPixel(this.scrBmp.Width / 2, this.scrBmp.Height / 2);
            g.DrawImage(this.scrBmp, this.scrCaptureOrigin.X, this.scrCaptureOrigin.Y);

            // All overlay drawing is in device space.
            g.ResetTransform();
            g.ResetClip();

            g.Flush();
            this.ApplyDifferenceGrid(w, h);
            this.ApplyDifferenceCrosshair(w, h);

            DrawBorder(g, w, h, this.Focused, this.lineWidth);

            g.Flush();
            this.CompositeFinalFrame(w, h, totalW, totalH);
            this.CommitLayeredWindow(winPos, totalW, totalH);
            // Re-assert topmost every frame so popup menus, taskbar thumbnails, and tooltips
            // (which are also topmost but created after us) don't permanently cover the lens.
            SetWindowPos(this.Handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
            this.infoForm.UpdateAndPosition(
               cursorPos,
               sampledColor,
               new Rectangle(lensLeft, lensTop, w, h),
               infoLeft,
               precisionActive,
               Lens.Instance.PrecisionSpeed,
               dpiScale);
         }
         catch (Exception ex)
         {
            Debug.WriteLine("RenderFrame exception: " + ex);
         }
         finally
         {
            this.isRendering = false;
         }
      }
   }
}
