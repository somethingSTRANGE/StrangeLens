// -------------------------------------------------------------------------------------
// <copyright file="LensForm.cs" company="Strange Entertainment LLC">
//   Copyright 2004-2023 Strange Entertainment LLC. All rights reserved.
// </copyright>
// -------------------------------------------------------------------------------------

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Lens
{
   public partial class LensForm : Form
   {
      [SuppressMessage("ReSharper", "IdentifierTypo")] [SuppressMessage("ReSharper", "InconsistentNaming")]
      private const uint SPI_GETMOUSESPEED = 0x0070;

      [SuppressMessage("ReSharper", "IdentifierTypo")] [SuppressMessage("ReSharper", "InconsistentNaming")]
      private const uint SPI_SETMOUSESPEED = 0x0071;

      private const uint ULW_ALPHA = 0x00000002;

      // Drop shadow parameters.
      private const int   ShadowBlur     = 16;   // window margin on each side (px)
      private const float ShadowSigma    = 4.5f; // Gaussian standard deviation (px)
      private const int   ShadowOffsetX  = 0;
      private const int   ShadowOffsetY  = 6;    // shadow shifts this many px downward
      private const byte  ShadowMaxAlpha = 160;  // peak shadow opacity (0–255)
      private const int   ShadowMarginL  = ShadowBlur;
      private const int   ShadowMarginT  = ShadowBlur - ShadowOffsetY;  // 10
      private const int   ShadowMarginR  = ShadowBlur;
      private const int   ShadowMarginB  = ShadowBlur + ShadowOffsetY;  // 22

      // Crosshair blend mode — change this constant to switch, no Settings UI yet.
      //   Standard:   GDI+ line at full opacity drawn over content + grid.
      //   Difference: per-pixel |penColor − dst| written directly into the DIBSection;
      //               always contrasts against the background regardless of content.
      private enum CrosshairBlend { Standard, Difference }
      private const CrosshairBlend CrosshairMode = CrosshairBlend.Standard;

      private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

      [StructLayout(LayoutKind.Sequential, Pack = 1)]
      private struct BLENDFUNCTION
      {
         public byte BlendOp;
         public byte BlendFlags;
         public byte SourceConstantAlpha;
         public byte AlphaFormat;
      }

      [StructLayout(LayoutKind.Sequential)]
      private struct BITMAPINFOHEADER
      {
         public uint biSize;
         public int biWidth;
         public int biHeight;
         public ushort biPlanes;
         public ushort biBitCount;
         public uint biCompression; // BI_RGB = 0
         public uint biSizeImage;
         public int biXPelsPerMeter;
         public int biYPelsPerMeter;
         public uint biClrUsed;
         public uint biClrImportant;
      }

      [StructLayout(LayoutKind.Sequential)]
      private struct BITMAPINFO
      {
         public BITMAPINFOHEADER bmiHeader;
      }

      // Saved base speed — static so EmergencyRestoreMouseSpeed can reach it without a form instance.
      private static int savedBaseMouseSpeed;

      private readonly Timer timer;

      private int baseMouseSpeed;

      private InfoControl infoControl;

      private InfoForm infoForm;

      // Screen capture (small localized region around cursor).
      private Bitmap scrBmp;
      private Graphics scrGrp;
      private Point scrCaptureOrigin;
      private Bitmap checkerTile;
      private TextureBrush checkerBrush;

      private bool isRendering;

      // Mouse hook — field keeps the delegate alive so the GC doesn't collect it.
      private LowLevelMouseProc mouseHookProc;
      private IntPtr mouseHook = IntPtr.Zero;

      // Cached GDI resources for the layered window — allocated once, reused every frame.
      private IntPtr layeredMemDC = IntPtr.Zero;
      private IntPtr layeredBitmap = IntPtr.Zero;
      private IntPtr layeredBits = IntPtr.Zero; // raw pixel pointer into the DIBSection
      private Graphics layeredGrp;
      private int layeredW, layeredH;

      // Cached grid bitmap — rebuilt only when relevant settings change.
      private Bitmap gridBmp;
      private int cachedGridW, cachedGridH, cachedGridMag, cachedGridSize;
      private GridStyleOptions cachedGridStyle;
      private Color cachedGridColor;

      // Final (shadow-composited) DC submitted to UpdateLayeredWindow.
      private IntPtr finalMemDC = IntPtr.Zero;
      private IntPtr finalBitmap = IntPtr.Zero;
      private IntPtr finalBits = IntPtr.Zero;
      private int finalW, finalH;

      // Cached Gaussian shadow alpha map — rebuilt only when content size changes.
      private byte[] shadowAlpha;
      private int cachedShadowContentW = -1, cachedShadowContentH = -1;

      private Point targetLocation;
      // Tracks which side the info panel is currently on, for hysteresis.
      private bool infoOnLeft;
      // Tracks the last screen the cursor was on; used to re-initialize flip state on display change.
      private string lastScreenName;

      public LensForm()
      {
         this.InitializeComponent();

         this.FormBorderStyle = FormBorderStyle.None;
         this.ControlBox = false;
         this.ShowInTaskbar = false;
         this.TopMost = true;
         this.StartPosition = FormStartPosition.Manual;

         this.targetLocation = Cursor.Position;
         this.ApplyWidth();
         this.ApplyHeight();

         this.baseMouseSpeed = SystemInformation.MouseSpeed;
         savedBaseMouseSpeed = this.baseMouseSpeed;

         this.timer = new Timer { Interval = 16, Enabled = false };
         this.timer.Tick += (s, e) => this.RenderFrame();

         this.infoControl = new InfoControl();
         this.infoForm = new InfoForm(this.infoControl);
      }

      protected override CreateParams CreateParams
      {
         get
         {
            var cp = base.CreateParams;
            cp.ExStyle |= 0x00080000; // WS_EX_LAYERED
            return cp;
         }
      }

      [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
      public Point TargetLocation
      {
         get => this.targetLocation;
         set => this.targetLocation = value;
      }

      [DllImport("User32.dll")]
      private static extern bool SystemParametersInfo(uint uiAction, uint uiParam, uint pvParam, uint fWinIni);

      [DllImport("User32.dll", ExactSpelling = true, SetLastError = true)]
      private static extern bool UpdateLayeredWindow(IntPtr hwnd, IntPtr hdcDst, ref Point pptDst, ref Size psize,
         IntPtr hdcSrc, ref Point pptSrc, uint crKey, ref BLENDFUNCTION pblend, uint dwFlags);

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

      [DllImport("Gdi32.dll")] private static extern IntPtr CreatePen(int fnPenStyle, int nWidth, uint crColor);
      [DllImport("Gdi32.dll")] private static extern bool MoveToEx(IntPtr hdc, int x, int y, IntPtr lpPoint);
      [DllImport("Gdi32.dll")] private static extern bool LineTo(IntPtr hdc, int x, int y);

      [DllImport("user32.dll")] private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);
      [DllImport("user32.dll")] private static extern bool UnhookWindowsHookEx(IntPtr hhk);
      [DllImport("user32.dll")] private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
      [DllImport("kernel32.dll")] private static extern IntPtr GetModuleHandle(string lpModuleName);
      [DllImport("user32.dll", SetLastError = true)]
      private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);
      [DllImport("user32.dll")] private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
      [DllImport("user32.dll")] private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

      private const uint ModCtrlAltShift = 0x0007; // MOD_CONTROL | MOD_ALT | MOD_SHIFT
      private const int HotkeyHex   = 1;
      private const int HotkeyRgb   = 2;
      private const int HotkeyHsl   = 3;
      private const int HotkeyWeb   = 4;
      private const int Hotkey12BitA = 5;
      private const int Hotkey12BitB = 6;

      // Called by Program.cs on unhandled exceptions to ensure mouse speed is always restored.
      internal static void EmergencyRestoreMouseSpeed()
      {
         if (savedBaseMouseSpeed > 0)
            SystemParametersInfo(SPI_SETMOUSESPEED, 0, (uint)savedBaseMouseSpeed, 0);
      }


      protected override void WndProc(ref Message m)
      {
         const int WmHotkey = 0x0312;
         if (m.Msg == WmHotkey)
         {
            switch (m.WParam.ToInt32())
            {
               case HotkeyHex:    this.CopyToClipboardColorHex();   return;
               case HotkeyRgb:    this.CopyToClipboardColorRGB();   return;
               case HotkeyHsl:    this.CopyToClipboardColorHSL();   return;
               case HotkeyWeb:    this.CopyToClipboardColorWeb();   return;
               case Hotkey12BitA:
               case Hotkey12BitB: this.CopyToClipboardColor12Bit(); return;
            }
         }
         base.WndProc(ref m);
      }

      protected override void OnClosing(CancelEventArgs e)
      {
         this.timer.Stop();
         UnregisterHotKey(this.Handle, HotkeyHex);
         UnregisterHotKey(this.Handle, HotkeyRgb);
         UnregisterHotKey(this.Handle, HotkeyHsl);
         UnregisterHotKey(this.Handle, HotkeyWeb);
         UnregisterHotKey(this.Handle, Hotkey12BitA);
         UnregisterHotKey(this.Handle, Hotkey12BitB);
         if (this.mouseHook != IntPtr.Zero) { UnhookWindowsHookEx(this.mouseHook); this.mouseHook = IntPtr.Zero; }
         this.infoForm.Close();
         this.ResetMouseSpeed();
         this.FreeLayeredResources();
         this.FreeFinalResources();
         this.shadowAlpha = null;
         this.gridBmp?.Dispose();
         this.checkerBrush?.Dispose();
         this.checkerTile?.Dispose();
         this.scrGrp?.Dispose();
         this.scrBmp?.Dispose();
         base.OnClosing(e);
      }

      protected override void OnMouseMove(MouseEventArgs e)
      {
         base.OnMouseMove(e);
         this.TargetLocation = e.Location;
      }

      protected override void OnMouseWheel(MouseEventArgs e)
      {
         base.OnMouseWheel(e);

         switch (ModifierKeys)
         {
            case Keys.Control:
               if (e.Delta > 0) this.IncreaseSize();
               else this.DecreaseSize();
               break;
            case Keys.Alt:
               if (e.Delta > 0) this.IncreaseGridSize();
               else this.DecreaseGridSize();
               break;
         }
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
            default:
               Debug.WriteLine("1: " + e + ", " + e.Handled + ", " + e.GetType() + ", " + e.KeyCode + ", " +
                               e.KeyData + ", " + e.KeyValue);
               break;
         }
      }

      protected override void OnKeyPress(KeyPressEventArgs e)
      {
         base.OnKeyPress(e);
         Debug.WriteLine("2: " + e + ", " + e.Handled + ", " + e.GetType() + ", " + e.KeyChar + ", " +
                         e.ToString());
      }

      protected override void OnKeyUp(KeyEventArgs e)
      {
         base.OnKeyUp(e);

         switch (e.KeyCode)
         {
            case Keys.Escape:
               this.Close();
               break;
            case Keys.Oemplus:  if (e.Control) this.ChangeMagnification(1);  break;
            case Keys.OemMinus: if (e.Control) this.ChangeMagnification(-1); break;
            case Keys.OemOpenBrackets: this.ChangeWidth(-Lens.Defaults.SizeIncrement); break;
            case Keys.OemCloseBrackets: this.ChangeWidth(Lens.Defaults.SizeIncrement); break;
            case Keys.OemSemicolon: this.ChangeHeight(-Lens.Defaults.SizeIncrement); break;
            case Keys.OemQuotes: this.ChangeHeight(Lens.Defaults.SizeIncrement); break;
            default:
               Debug.WriteLine((ModifierKeys == Keys.Control ? "CTRL-" : string.Empty) +
                               (ModifierKeys == Keys.Alt ? "ALT-" : string.Empty) +
                               (ModifierKeys == Keys.Shift ? "SHIFT-" : string.Empty) + e.KeyCode + " (" +
                               ModifierKeys + ")");
               break;
         }
      }

      // Layered window content is managed entirely by UpdateLayeredWindow via RenderFrame.
      protected override void OnPaint(PaintEventArgs e) { }
      protected override void OnPaintBackground(PaintEventArgs e) { }

      private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
      {
         const int WmMousewheel = 0x020A;
         const Keys CtrlAltShift = Keys.Control | Keys.Alt | Keys.Shift;
         if (nCode >= 0 && wParam.ToInt32() == WmMousewheel && (Control.ModifierKeys & CtrlAltShift) == CtrlAltShift)
         {
            // MSLLHOOKSTRUCT.mouseData is at offset 8; high word is the signed wheel delta.
            var mouseData  = Marshal.ReadInt32(lParam, 8);
            var wheelDelta = (short)(mouseData >> 16);
            this.ChangeMagnification((short)(wheelDelta > 0 ? 1 : -1));
            return (IntPtr)1; // consume — don't forward to the app under the cursor
         }
         return CallNextHookEx(this.mouseHook, nCode, wParam, lParam);
      }

      protected override void OnShown(EventArgs e)
      {
         base.OnShown(e);

         this.SetMouseSpeed();

         // Rough initial position — first RenderFrame will correct it via UpdateLayeredWindow.
         var pos = Cursor.Position;
         this.Left = pos.X + 20;
         this.Top = pos.Y - this.Height / 2;

         // WDA_EXCLUDEFROMCAPTURE disabled — SetWindowDisplayAffinity deadlocks with DWM
         // on WS_EX_LAYERED windows. Revisit once basic rendering is stable.
         this.timer.Enabled = true;

         // Low-level mouse hook for Ctrl+Alt+Shift+scroll zoom — works even when the lens lacks focus.
         this.mouseHookProc = this.MouseHookCallback;
         this.mouseHook = SetWindowsHookEx(14 /*WH_MOUSE_LL*/, this.mouseHookProc, GetModuleHandle(null), 0);
         if (this.mouseHook == IntPtr.Zero)
            Debug.WriteLine($"SetWindowsHookEx failed: error {Marshal.GetLastWin32Error()}");

         // Global copy hotkeys — active only while the lens is open.
         RegisterHotKey(this.Handle, HotkeyHex,    ModCtrlAltShift, (uint)Keys.X);
         RegisterHotKey(this.Handle, HotkeyRgb,    ModCtrlAltShift, (uint)Keys.R);
         RegisterHotKey(this.Handle, HotkeyHsl,    ModCtrlAltShift, (uint)Keys.H);
         RegisterHotKey(this.Handle, HotkeyWeb,    ModCtrlAltShift, (uint)Keys.W);
         RegisterHotKey(this.Handle, Hotkey12BitA, ModCtrlAltShift, (uint)Keys.A);
         RegisterHotKey(this.Handle, Hotkey12BitB, ModCtrlAltShift, (uint)Keys.B);
      }

      private void RenderFrame()
      {
         if (this.isRendering) return;
         this.isRendering = true;
         try
         {
            var cursorPos = Cursor.Position;
            var lens = Lens.Instance;
            var w = lens.Width;
            var h = lens.Height;

            // Position the lens to the right of the cursor; flip left near screen edge.
            // The gap is fixed regardless of current magnification: it's based on the
            // widest possible capture region for this lens width (at minimum zoom = 2×).
            // This keeps the panel at a constant apparent distance as zoom changes.
            var screen = Screen.FromPoint(cursorPos);
            var gapX = (int)(Math.Ceiling(w / (float)Lens.Defaults.MinMagnification) / 2) + 10;
            // ── Flip orientation with hysteresis. ──────────────────────────────────────
            // Two independent thresholds on opposite screen edges prevent oscillation:
            //   default (cursor·lens·info) → inverse  when Info's RIGHT edge clips screen right.
            //   inverse (info·lens·cursor) → default  when Info's LEFT  edge clips screen left.
            // Because these are on opposite sides of the screen, there is a large dead zone in
            // the center where neither flip fires — no flickering at either edge.
            //
            // On display change: re-initialize to default, except start inverse if the cursor
            // is already within the right-edge flip zone of the new display.
            int infoW       = this.infoForm.HasVisibleContent ? this.infoForm.ContentW + InfoForm.PanelMargin : 0;
            bool rightClips = cursorPos.X + gapX + w + infoW > screen.Bounds.Right;
            bool leftClips  = cursorPos.X - gapX - w - infoW < screen.Bounds.Left;

            if (screen.DeviceName != this.lastScreenName)
            {
               this.lastScreenName = screen.DeviceName;
               this.infoOnLeft = rightClips;   // initialize for this display
            }
            else if (!this.infoOnLeft && rightClips)
               this.infoOnLeft = true;          // default → inverse
            else if (this.infoOnLeft && leftClips)
               this.infoOnLeft = false;         // inverse → default

            var lensLeft = this.infoOnLeft ? cursorPos.X - gapX - w : cursorPos.X + gapX;
            var lensTop = Math.Max(screen.Bounds.Top,
               Math.Min(cursorPos.Y - h / 2, screen.Bounds.Bottom - h));
            int totalW = w + ShadowMarginL + ShadowMarginR;
            int totalH = h + ShadowMarginT + ShadowMarginB;
            // Window top-left is inset by the shadow margins so content appears at lensLeft/lensTop.
            var winPos = new Point(lensLeft - ShadowMarginL, lensTop - ShadowMarginT);

            this.EnsureLayeredResources(w, h);
            this.EnsureFinalResources(totalW, totalH);

            var g = this.layeredGrp;
            g.ResetTransform();
            g.ResetClip();

            // Zoom transform: cursor position maps to the center of the lens content.
            g.TranslateTransform(w / 2f, h / 2f);
            g.ScaleTransform(lens.Magnification, lens.Magnification);
            g.TranslateTransform(-cursorPos.X, -cursorPos.Y);

            g.Clear(Color.Black);

            (g.InterpolationMode, g.PixelOffsetMode) = lens.Scaling switch
            {
               ScalingMode.NearestNeighbor     => (InterpolationMode.NearestNeighbor,     PixelOffsetMode.Half),
               ScalingMode.Bilinear            => (InterpolationMode.Bilinear,            PixelOffsetMode.Default),
               ScalingMode.HighQualityBilinear => (InterpolationMode.HighQualityBilinear, PixelOffsetMode.Default),
               ScalingMode.Bicubic             => (InterpolationMode.Bicubic,             PixelOffsetMode.Default),
               ScalingMode.HighQualityBicubic  => (InterpolationMode.HighQualityBicubic,  PixelOffsetMode.Default),
               _                               => (InterpolationMode.NearestNeighbor,     PixelOffsetMode.Half)
            };

            this.CopyScreen(cursorPos);
            // Sample the pixel at the cursor — scrBmp is centered on cursorPos so the
            // center pixel maps exactly to the crosshair origin (the spec's sampled pixel).
            var sampledColor = this.scrBmp.GetPixel(this.scrBmp.Width / 2, this.scrBmp.Height / 2);
            g.DrawImage(this.scrBmp, this.scrCaptureOrigin.X, this.scrCaptureOrigin.Y);

            // All overlay drawing is in device space.
            g.ResetTransform();
            g.ResetClip();
            this.DrawGrid(g, w, h);
            if (CrosshairMode == CrosshairBlend.Standard)
               this.DrawOrigin(g, w, h);
            DrawBorder(g, w, h, this.Focused);

            if (CrosshairMode == CrosshairBlend.Difference)
            {
               g.Flush();
               this.ApplyDifferenceCrosshair(w, h);
            }

            g.Flush();
            this.CompositeFinalFrame(w, h, totalW, totalH);
            this.CommitLayeredWindow(winPos, totalW, totalH);
            // Re-assert topmost every frame so popup menus, taskbar thumbnails, and tooltips
            // (which are also topmost but created after us) don't permanently cover the lens.
            const uint SWP_NOMOVE_NOSIZE_NOACTIVATE = 0x0013; // SWP_NOMOVE|SWP_NOSIZE|SWP_NOACTIVATE
            SetWindowPos(this.Handle, new IntPtr(-1), 0, 0, 0, 0, SWP_NOMOVE_NOSIZE_NOACTIVATE);
            this.infoForm.UpdateAndPosition(cursorPos, sampledColor, new Rectangle(lensLeft, lensTop, w, h));
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

      // Ensures the GDI memory DC and bitmap are allocated and match the requested size.
      private void EnsureLayeredResources(int w, int h)
      {
         if (this.layeredMemDC == IntPtr.Zero)
            this.layeredMemDC = CreateCompatibleDC(IntPtr.Zero);

         if (this.layeredBitmap == IntPtr.Zero || this.layeredW != w || this.layeredH != h)
         {
            this.layeredGrp?.Dispose();
            this.layeredGrp = null;
            if (this.layeredBitmap != IntPtr.Zero) DeleteObject(this.layeredBitmap);

            // UpdateLayeredWindow requires a 32-bit DIB section — a DDB is not reliable.
            var bmi = new BITMAPINFO
            {
               bmiHeader = new BITMAPINFOHEADER
               {
                  biSize = (uint)Marshal.SizeOf<BITMAPINFOHEADER>(),
                  biWidth = w,
                  biHeight = -h, // negative = top-down
                  biPlanes = 1,
                  biBitCount = 32,
                  biCompression = 0 // BI_RGB
               }
            };
            this.layeredBitmap = CreateDIBSection(IntPtr.Zero, ref bmi, 0, out this.layeredBits, IntPtr.Zero, 0);
            SelectObject(this.layeredMemDC, this.layeredBitmap);
            this.layeredGrp = Graphics.FromHdc(this.layeredMemDC);
            this.layeredW = w;
            this.layeredH = h;
         }
      }

      private void FreeLayeredResources()
      {
         this.layeredGrp?.Dispose();
         this.layeredGrp = null;
         if (this.layeredBitmap != IntPtr.Zero) { DeleteObject(this.layeredBitmap); this.layeredBitmap = IntPtr.Zero; this.layeredBits = IntPtr.Zero; }
         if (this.layeredMemDC != IntPtr.Zero) { DeleteDC(this.layeredMemDC); this.layeredMemDC = IntPtr.Zero; }
      }

      private void EnsureFinalResources(int w, int h)
      {
         if (this.finalMemDC == IntPtr.Zero)
            this.finalMemDC = CreateCompatibleDC(IntPtr.Zero);

         if (this.finalBitmap == IntPtr.Zero || this.finalW != w || this.finalH != h)
         {
            if (this.finalBitmap != IntPtr.Zero) { DeleteObject(this.finalBitmap); this.finalBits = IntPtr.Zero; }

            var bmi = new BITMAPINFO
            {
               bmiHeader = new BITMAPINFOHEADER
               {
                  biSize = (uint)Marshal.SizeOf<BITMAPINFOHEADER>(),
                  biWidth = w,
                  biHeight = -h,
                  biPlanes = 1,
                  biBitCount = 32,
                  biCompression = 0
               }
            };
            this.finalBitmap = CreateDIBSection(IntPtr.Zero, ref bmi, 0, out this.finalBits, IntPtr.Zero, 0);
            SelectObject(this.finalMemDC, this.finalBitmap);
            this.finalW = w;
            this.finalH = h;
         }
      }

      private void FreeFinalResources()
      {
         if (this.finalBitmap != IntPtr.Zero) { DeleteObject(this.finalBitmap); this.finalBitmap = IntPtr.Zero; this.finalBits = IntPtr.Zero; }
         if (this.finalMemDC != IntPtr.Zero) { DeleteDC(this.finalMemDC); this.finalMemDC = IntPtr.Zero; }
      }

      // Composites shadow + content into finalBits for UpdateLayeredWindow.
      // Content pixels are stamped alpha=255 (fully opaque). Shadow pixels carry their
      // Gaussian-blurred alpha. Regions outside both are transparent (alpha=0).
      private void CompositeFinalFrame(int cw, int ch, int tw, int th)
      {
         if (this.finalBits == IntPtr.Zero || this.layeredBits == IntPtr.Zero) return;

         this.EnsureShadow(cw, ch);

         // Clear final bitmap to transparent.
         int totalPixels = tw * th;
         for (int i = 0; i < totalPixels; i++)
            Marshal.WriteInt32(this.finalBits, i * 4, 0);

         // Write shadow. Pixel is pre-multiplied BGRA: black with alpha=a → (a<<24)|0.
         var alpha = this.shadowAlpha;
         for (int i = 0; i < totalPixels; i++)
         {
            byte a = alpha[i];
            if (a > 0)
               Marshal.WriteInt32(this.finalBits, i * 4, unchecked((int)((uint)a << 24)));
         }

         // Copy content pixels at (ShadowMarginL, ShadowMarginT), forcing alpha=255.
         // Content is fully opaque — GDI writes alpha=0, so we override it here.
         int contentStride = cw * 4;
         int finalStride   = tw * 4;
         for (int y = 0; y < ch; y++)
         {
            for (int x = 0; x < cw; x++)
            {
               int src = Marshal.ReadInt32(this.layeredBits, y * contentStride + x * 4);
               int dst = (src & 0x00FFFFFF) | unchecked((int)0xFF000000u);
               Marshal.WriteInt32(this.finalBits,
                  (y + ShadowMarginT) * finalStride + (x + ShadowMarginL) * 4, dst);
            }
         }
      }

      // Rebuilds the Gaussian shadow alpha map when content size changes.
      private void EnsureShadow(int contentW, int contentH)
      {
         if (this.shadowAlpha != null &&
             this.cachedShadowContentW == contentW &&
             this.cachedShadowContentH == contentH)
            return;

         this.cachedShadowContentW = contentW;
         this.cachedShadowContentH = contentH;

         int tw = contentW + ShadowMarginL + ShadowMarginR;
         int th = contentH + ShadowMarginT + ShadowMarginB;

         // Shadow source: the content rectangle offset by (ShadowOffsetX, ShadowOffsetY).
         int sx = ShadowMarginL + ShadowOffsetX;
         int sy = ShadowMarginT + ShadowOffsetY;
         var src = new float[tw * th];
         for (int y = sy; y < sy + contentH; y++)
            for (int x = sx; x < sx + contentW; x++)
               src[y * tw + x] = 1f;

         // Separable Gaussian blur (two passes, zero-padded boundary).
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

      private void CommitLayeredWindow(Point winPos, int w, int h)
      {
         var winSize = new Size(w, h);
         var srcPos = Point.Empty;
         // AlphaFormat=1 (AC_SRC_ALPHA): use per-pixel alpha from the DIBSection.
         // finalBits contains pre-multiplied BGRA — content pixels have alpha=255,
         // shadow pixels have alpha<255, transparent margin pixels have alpha=0.
         var blend = new BLENDFUNCTION { BlendOp = 0, BlendFlags = 0, SourceConstantAlpha = 255, AlphaFormat = 1 };
         var ok = UpdateLayeredWindow(this.Handle, IntPtr.Zero, ref winPos, ref winSize, this.finalMemDC,
            ref srcPos, 0, ref blend, ULW_ALPHA);
         if (!ok)
            Debug.WriteLine($"UpdateLayeredWindow failed: error {Marshal.GetLastWin32Error()}");
      }

      private static void DrawBorder(Graphics g, int w, int h, bool focused)
      {
         var color = focused ? SystemColors.Highlight : SystemColors.ControlDarkDark;
         var hdc = g.GetHdc();
         try
         {
            var pen = CreatePen(0 /*PS_SOLID*/, 1, ToColorRef(color));
            var oldPen = SelectObject(hdc, pen);
            // Outer 2px in the focus/unfocus color, inner 1px in black.
            // LineTo excludes its endpoint, so (0,0)→(w,0) covers columns 0..w-1 exactly.
            for (int i = 0; i < 2; i++)
            {
               MoveToEx(hdc, 0,       i,       IntPtr.Zero); LineTo(hdc, w,       i);       // top
               MoveToEx(hdc, 0,       h-1-i,   IntPtr.Zero); LineTo(hdc, w,       h-1-i);   // bottom
               MoveToEx(hdc, i,       0,       IntPtr.Zero); LineTo(hdc, i,       h);        // left
               MoveToEx(hdc, w-1-i,   0,       IntPtr.Zero); LineTo(hdc, w-1-i,   h);        // right
            }
            SelectObject(hdc, oldPen);
            DeleteObject(pen);

            // Inner black stroke at offset 2.
            var blackPen = CreatePen(0 /*PS_SOLID*/, 1, 0x00000000 /*black*/);
            SelectObject(hdc, blackPen);
            MoveToEx(hdc, 2,     2,     IntPtr.Zero); LineTo(hdc, w-2,   2);      // top
            MoveToEx(hdc, 2,     h-3,   IntPtr.Zero); LineTo(hdc, w-2,   h-3);    // bottom
            MoveToEx(hdc, 2,     2,     IntPtr.Zero); LineTo(hdc, 2,     h-2);    // left
            MoveToEx(hdc, w-3,   2,     IntPtr.Zero); LineTo(hdc, w-3,   h-2);    // right
            SelectObject(hdc, oldPen);
            DeleteObject(blackPen);
         }
         finally { g.ReleaseHdc(hdc); }
      }

      private void DrawOrigin(Graphics g, int w, int h)
      {
         var lens = Lens.Instance;
         int cx = w / 2, cy = h / 2, mag = lens.Magnification;
         using var pen = new Pen(lens.GridColor, 1f);
         g.DrawLine(pen, 0, cy, w, cy);
         g.DrawLine(pen, cx, 0, cx, h);
         g.DrawLine(pen, cx,       cy + mag, cx + mag, cy + mag);
         g.DrawLine(pen, cx + mag, cy,       cx + mag, cy + mag);
      }

      private void DrawGrid(Graphics g, int w, int h)
      {
         this.EnsureGridBitmap(w, h);
         if (this.gridBmp != null)
            g.DrawImage(this.gridBmp, 0, 0);
      }

      // Rebuilds the cached grid bitmap when any relevant setting has changed.
      private void EnsureGridBitmap(int w, int h)
      {
         var lens = Lens.Instance;
         var gridStyle = (GridStyleOptions)lens.GridStyle;
         if (this.gridBmp != null &&
             this.cachedGridW == w && this.cachedGridH == h &&
             this.cachedGridMag == lens.Magnification &&
             this.cachedGridSize == lens.GridSize &&
             this.cachedGridStyle == gridStyle &&
             this.cachedGridColor == lens.GridColor)
            return;

         this.gridBmp?.Dispose();
         this.gridBmp = null;
         this.cachedGridW = w;
         this.cachedGridH = h;
         this.cachedGridMag = lens.Magnification;
         this.cachedGridSize = lens.GridSize;
         this.cachedGridStyle = gridStyle;
         this.cachedGridColor = lens.GridColor;

         if (gridStyle == GridStyleOptions.None) return;

         this.gridBmp = new Bitmap(w, h, PixelFormat.Format32bppArgb);
         using var g = Graphics.FromImage(this.gridBmp);
         g.Clear(Color.Transparent);
         int cx = w / 2, cy = h / 2;
         int step = lens.GridSize * lens.Magnification;
         using var pen = new Pen(Color.FromArgb(0x33, lens.GridColor), 1f) { DashStyle = gridStyle.DashStyle() };
         for (int dy = step; cy - dy >= 0 || cy + dy < h; dy += step)
         {
            if (cy - dy >= 0) g.DrawLine(pen, 0, cy - dy, w, cy - dy);
            if (cy + dy < h)  g.DrawLine(pen, 0, cy + dy, w, cy + dy);
         }
         for (int dx = step; cx - dx >= 0 || cx + dx < w; dx += step)
         {
            if (cx - dx >= 0) g.DrawLine(pen, cx - dx, 0, cx - dx, h);
            if (cx + dx < w)  g.DrawLine(pen, cx + dx, 0, cx + dx, h);
         }
      }

      // Applies per-pixel difference blend to crosshair lines directly in the DIBSection.
      // Result = |penColor − dst| per channel, so the line always contrasts with content.
      private void ApplyDifferenceCrosshair(int w, int h)
      {
         if (this.layeredBits == IntPtr.Zero) return;
         var lens = Lens.Instance;
         int cx = w / 2, cy = h / 2, mag = lens.Magnification;
         int stride = w * 4;
         var c = lens.GridColor;
         for (int x = 0; x < w; x++) this.ApplyDiffPixel(cy * stride + x * 4, c);
         for (int y = 0; y < h; y++) this.ApplyDiffPixel(y * stride + cx * 4, c);
         for (int x = cx; x <= cx + mag; x++) this.ApplyDiffPixel((cy + mag) * stride + x * 4, c);
         for (int y = cy; y <= cy + mag; y++) this.ApplyDiffPixel(y * stride + (cx + mag) * 4, c);
      }

      private void ApplyDiffPixel(int offset, Color c)
      {
         int pixel = Marshal.ReadInt32(this.layeredBits, offset);
         byte b  = (byte)( pixel        & 0xFF);
         byte gr = (byte)((pixel >>  8) & 0xFF);
         byte r  = (byte)((pixel >> 16) & 0xFF);
         int result = unchecked((int)(
            (uint)Math.Abs(c.B - b) |
            ((uint)Math.Abs(c.G - gr) << 8) |
            ((uint)Math.Abs(c.R - r) << 16) |
            0xFF000000u));
         Marshal.WriteInt32(this.layeredBits, offset, result);
      }

      private static uint ToColorRef(Color c) => (uint)(c.R | (c.G << 8) | (c.B << 16));

      private void ApplyHeight()
      {
         this.Height = Lens.Instance.Height;
      }

      private void ApplyWidth()
      {
         this.Width = Lens.Instance.Width;
      }

      private void CopyScreen(Point cursorPos)
      {
         var lens = Lens.Instance;
         var captureW = (int)Math.Ceiling(lens.Width / (float)lens.Magnification) + 2;
         var captureH = (int)Math.Ceiling(lens.Height / (float)lens.Magnification) + 2;

         if (this.scrBmp == null || this.scrBmp.Width != captureW || this.scrBmp.Height != captureH)
         {
            this.checkerBrush?.Dispose();
            this.checkerTile?.Dispose();
            this.scrGrp?.Dispose();
            this.scrBmp?.Dispose();

            this.scrBmp = new Bitmap(captureW, captureH);
            this.scrGrp = Graphics.FromImage(this.scrBmp);

            // Build a 16×16 checkerboard tile — the standard "no content" indicator.
            this.checkerTile = new Bitmap(16, 16);
            using (var tg = Graphics.FromImage(this.checkerTile))
            using (var light = new SolidBrush(Color.FromArgb(200, 200, 200)))
            using (var dark = new SolidBrush(Color.FromArgb(120, 120, 120)))
            {
               tg.FillRectangle(light, 0, 0, 8, 8);
               tg.FillRectangle(dark,  8, 0, 8, 8);
               tg.FillRectangle(dark,  0, 8, 8, 8);
               tg.FillRectangle(light, 8, 8, 8, 8);
            }
            this.checkerBrush = new TextureBrush(this.checkerTile);
         }

         this.scrCaptureOrigin = new Point(cursorPos.X - captureW / 2, cursorPos.Y - captureH / 2);
         var captureRect = new Rectangle(this.scrCaptureOrigin, new Size(captureW, captureH));

         // Pre-fill with checkerboard. Anything not overwritten below is out of monitor bounds.
         this.scrGrp.FillRectangle(this.checkerBrush, 0, 0, captureW, captureH);

         // Copy only the portions that fall within actual monitor bounds.
         foreach (var screen in Screen.AllScreens)
         {
            var valid = Rectangle.Intersect(captureRect, screen.Bounds);
            if (valid.IsEmpty) continue;
            var dest = new Point(valid.X - captureRect.X, valid.Y - captureRect.Y);
            this.scrGrp.CopyFromScreen(valid.Location, dest, valid.Size);
         }
      }

      private void CopyToClipboardColorHex()
      {
         Clipboard.SetText(this.infoControl.ValueColorHex);
         this.infoControl.NotifyCopied("HEX");
      }

      private void CopyToClipboardColor12Bit()
      {
         Clipboard.SetText(this.infoControl.ValueColor12Bit);
         this.infoControl.NotifyCopied("12-Bit");
      }

      private void CopyToClipboardColorWeb()
      {
         Clipboard.SetText(this.infoControl.ValueColorWeb);
         this.infoControl.NotifyCopied("Web");
      }

      private void CopyToClipboardColorRGB()
      {
         Clipboard.SetText($"rgb({this.infoControl.ValueColorRGB})");
         this.infoControl.NotifyCopied("RGB");
      }

      private void CopyToClipboardColorHSL()
      {
         Clipboard.SetText($"hsl({this.infoControl.ValueColorHSL})");
         this.infoControl.NotifyCopied("HSL");
      }

      private void DecreaseGridSize()
      {
         Lens.Instance.GridSize--;
      }

      private void DecreaseSize()
      {
         Debug.WriteLine("DECREASE FORM SIZE KEEPING ASPECT RATIO");
      }

      private void IncreaseGridSize()
      {
         Lens.Instance.GridSize++;
      }

      private void IncreaseSize()
      {
         Debug.WriteLine("INCREASE FORM SIZE KEEPING ASPECT RATIO");
      }

      private void ChangeHeight(short amount)
      {
         Lens.Instance.Height += amount;
         this.ApplyHeight();
         this.RenderFrame();
      }

      private void ChangeMagnification(short amount)
      {
         Lens.Instance.Magnification = (byte)(Lens.Instance.Magnification + amount);
         this.SetMouseSpeed();
         this.RenderFrame();
      }

      private void ChangePosition(int x, int y)
      {
         var p = Cursor.Position;
         p.X += x;
         p.Y += y;
         Cursor.Position = p;
      }

      private void ChangeWidth(short amount)
      {
         Lens.Instance.Width += amount;
         this.ApplyWidth();
         this.RenderFrame();
      }

      private void ResetMouseSpeed()
      {
         SystemParametersInfo(SPI_SETMOUSESPEED, 0, (uint)this.baseMouseSpeed, 0);
      }

      private void SetMouseSpeed()
      {
         var factor = (float)(Lens.Instance.Magnification - Lens.Defaults.MinMagnification) /
                      (Lens.Defaults.MaxMagnification - Lens.Defaults.MinMagnification);
         var minSpeed = Math.Max(this.baseMouseSpeed / Lens.Instance.SpeedFactor,
            Lens.Defaults.MinMouseSpeed);
         var mouseSpeed = (uint)Math.Round((this.baseMouseSpeed - minSpeed) * (1 - factor) + minSpeed);

         // The pvParam (mouseSpeed) parameter must point to an integer that
         // receives a value which ranges between 1 (slowest) and 20 (fastest).
         // A value of 10 is the default.
         SystemParametersInfo(SPI_SETMOUSESPEED, 0, mouseSpeed, 0);
      }
   }
}
