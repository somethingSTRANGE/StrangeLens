// -------------------------------------------------------------------------------------
// <copyright file="MeasureForm.cs">
//   Copyright (c) 2026
//   Licensed under the MIT License. See LICENSE file in the project root.
// </copyright>
// -------------------------------------------------------------------------------------

namespace StrangeLens
{
   using System;
   using System.Diagnostics.CodeAnalysis;
   using System.Drawing;
   using System.Runtime.InteropServices;
   using System.Windows.Forms;

   using static NativeMethods;

   /// <summary>Click-through topmost overlay that draws a 1-pixel animated border rect between
   ///    the measurement anchor and the current cursor position. Rendered via
   ///    <c>UpdateLayeredWindow</c> so DWM composites it into the desktop and LensForm's
   ///    CopyFromScreen captures and magnifies it naturally.</summary>
   internal class MeasureForm : Form
   {
      private IntPtr bitmap = IntPtr.Zero;

      private IntPtr bits = IntPtr.Zero;

      private int bmpH = -1;

      private int bmpW = -1;

      private IntPtr memDC = IntPtr.Zero;

      public MeasureForm()
      {
         this.FormBorderStyle = FormBorderStyle.None;
         this.ShowInTaskbar = false;
         this.StartPosition = FormStartPosition.Manual;
      }

      protected override CreateParams CreateParams
      {
         get
         {
            var cp = base.CreateParams;
            cp.ExStyle |= WS_EX_LAYERED | WS_EX_TOPMOST | WS_EX_NOACTIVATE;
            return cp;
         }
      }

      protected override bool ShowWithoutActivation => true;

      internal void Dismiss()
      {
         SetWindowPos(
            this.Handle,
            IntPtr.Zero,
            0,
            0,
            0,
            0,
            SWP_HIDEWINDOW | SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_NOZORDER);
         this.FreeResources();
      }

      /// <summary>Redraws and repositions the border rect for the current frame. The rect spans
      ///    the region between the anchor crosshair intersection and the live crosshair
      ///    intersection, so the displayed dimensions equal the cursor movement delta rather than
      ///    an inclusive pixel count. Equal points produce an invisible 0×0 value with a 1×1 dot
      ///    on the screen; moving right 100px produces 100×0. The pixel at <paramref name="live"/>
      ///    is left transparent so that the Lens samples the real desktop content at the cursor
      ///    position rather than the border.</summary>
      /// <param name="anchor">Fixed corner dropped when measurement mode was activated.</param>
      /// <param name="live">Current cursor position; defines the opposite corner of the rect.</param>
      /// <param name="animPhase">Ping-pong value in [0, 1]; 0 = fully transparent, 1 = opaque
      ///    white.</param>
      internal void Update(Point anchor, Point live, float animPhase)
      {
         var left = Math.Min(anchor.X, live.X);
         var top = Math.Min(anchor.Y, live.Y);
         var w = Math.Abs(live.X - anchor.X);
         var h = Math.Abs(live.Y - anchor.Y);

         // Form must be at least 1×1 to be renderable; InfoPanel displays the real delta.
         var formW = Math.Max(1, w);
         var formH = Math.Max(1, h);

         // The sampled pixel lands on the form's top-left corner when live is at or
         // above-left of anchor. Leave that pixel transparent so color sampling reads
         // real desktop content. In all other quadrants the sampled pixel is outside
         // the form's extent, so out-of-bounds sentinels (formW / formH) disable the skip.
         var skipX = live.X <= anchor.X ? 0 : formW;
         var skipY = live.Y <= anchor.Y ? 0 : formH;

         this.EnsureResources(formW, formH);
         this.RenderBorder(formW, formH, animPhase, skipX, skipY);
         this.Commit(new Point(left, top), formW, formH);
      }

      protected override void OnFormClosing(FormClosingEventArgs e)
      {
         this.FreeResources();
         base.OnFormClosing(e);
      }

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

      private void Commit(Point winPos, int w, int h)
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
            this.memDC,
            ref srcPos,
            0,
            ref blend,
            ULW_ALPHA);

         // Show without changing Z-order. WS_EX_TOPMOST (set in CreateParams) keeps the window
         // above regular windows. LensForm and InfoForm re-assert HWND_TOPMOST every frame, so
         // they naturally remain above MeasureForm without any explicit ordering here.
         SetWindowPos(
            this.Handle,
            IntPtr.Zero,
            0,
            0,
            0,
            0,
            SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_NOZORDER | SWP_SHOWWINDOW);
      }

      private void EnsureResources(int w, int h)
      {
         if (this.memDC == IntPtr.Zero)
         {
            this.memDC = CreateCompatibleDC(IntPtr.Zero);
         }

         if ((this.bmpW == w) && (this.bmpH == h))
         {
            return;
         }

         if (this.bitmap != IntPtr.Zero)
         {
            DeleteObject(this.bitmap);
            this.bitmap = IntPtr.Zero;
            this.bits = IntPtr.Zero;
         }

         var bmi = MakeBmi(w, h);
         this.bitmap = CreateDIBSection(IntPtr.Zero, ref bmi, 0, out this.bits, IntPtr.Zero, 0);
         SelectObject(this.memDC, this.bitmap);
         this.bmpW = w;
         this.bmpH = h;
      }

      private void FreeResources()
      {
         if (this.bitmap != IntPtr.Zero)
         {
            DeleteObject(this.bitmap);
            this.bitmap = IntPtr.Zero;
            this.bits = IntPtr.Zero;
         }

         if (this.memDC != IntPtr.Zero)
         {
            DeleteDC(this.memDC);
            this.memDC = IntPtr.Zero;
         }

         this.bmpW = -1;
         this.bmpH = -1;
      }

      [SuppressMessage("ReSharper", "CognitiveComplexity")]
      private void RenderBorder(int w, int h, float animPhase, int skipX, int skipY)
      {
         if (this.bits == IntPtr.Zero)
         {
            return;
         }

         // Single kernel call to zero the buffer — O(1) managed overhead regardless of rect size.
         RtlZeroMemory(this.bits, new IntPtr(w * h * 4));

         // Squared phase: spends most of the cycle near transparent, then a brief bright flash.
         // Capped at 75% so the border never fully occludes what's underneath.
         var a = (int)(animPhase * animPhase * 0.75f * 255);

         // 2-pixel black/white dashes via ((x/2)+(y/2))%2 — 2×2 tile checkerboard.
         // Along any straight edge this produces runs of 2 same-color pixels, which
         // read as a dash rather than a fine gray blur, while still guaranteeing one
         // color always contrasts against any background (light or dark).
         // Pre-multiplied BGRA: white = (a,a,a,a); black = RGB=0, alpha=a → a<<24.
         var whitePx = unchecked((int)((uint)a * 0x01010101u));
         var blackPx = unchecked((int)((uint)a << 24));

         for (var x = 0; x < w; x++)
         {
            if (!((x == skipX) && (0 == skipY)))
            {
               // top row (y=0)
               Marshal.WriteInt32(this.bits, x * 4, (x / 2) % 2 == 0 ? whitePx : blackPx);
            }

            if (!((x == skipX) && (h - 1 == skipY)))
            {
               // bottom row
               Marshal.WriteInt32(
                  this.bits,
                  (((h - 1) * w) + x) * 4,
                  ((x / 2) + ((h - 1) / 2)) % 2 == 0 ? whitePx : blackPx);
            }
         }

         for (var y = 1; y < h - 1; y++)
         {
            if (!((0 == skipX) && (y == skipY)))
            {
               // left col (x=0)
               Marshal.WriteInt32(this.bits, y * w * 4, (y / 2) % 2 == 0 ? whitePx : blackPx);
            }

            if (!((w - 1 == skipX) && (y == skipY)))
            {
               // right col
               Marshal.WriteInt32(
                  this.bits,
                  (((y * w) + w) - 1) * 4,
                  (((w - 1) / 2) + (y / 2)) % 2 == 0 ? whitePx : blackPx);
            }
         }
      }
   }
}
