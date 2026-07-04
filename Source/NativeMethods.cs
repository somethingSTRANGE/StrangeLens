// -------------------------------------------------------------------------------------
// <copyright file="NativeMethods.cs">
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

   /// <summary>P/Invoke declarations and Win32 constants shared across the application. Import
   ///    with <c>using static StrangeLens.NativeMethods;</c> for unqualified access.</summary>
   [SuppressMessage("ReSharper", "InconsistentNaming")]
   [SuppressMessage("ReSharper", "IdentifierTypo")]
   [SuppressMessage("ReSharper", "CommentTypo")]
   internal static class NativeMethods
   {
      // ── Window extended styles (WS_EX_*) ────────────────────────────────────────────────

      /// <summary>Paints all descendants of a window in bottom-to-top painting order using
      ///    double-buffering. Bottom-to-top painting order allows a descendent window to have
      ///    translucency (alpha) and transparency (color-key) effects, but only if the descendent
      ///    window also has the WS_EX_TRANSPARENT bit set. Double-buffering allows the window and
      ///    its descendents to be painted without a flicker.</summary>
      internal const int WS_EX_COMPOSITED = 0x02000000;

      /// <summary>The window should not be brought to the foreground when the user clicks it. When
      ///    the user minimizes or closes the foreground window, the system will not bring this
      ///    window to the foreground.</summary>
      internal const int WS_EX_NOACTIVATE = 0x08000000;

      /// <summary>Specifies a window that is a layered window. This style cannot be used if the
      ///    window has a class style of either CS_OWNDC or CS_CLASSDC. Required for
      ///    UpdateLayeredWindow.</summary>
      internal const int WS_EX_LAYERED = 0x00080000;

      /// <summary>Forces a top-level window onto the taskbar when the window is visible.</summary>
      internal const int WS_EX_TOPMOST = 0x00000008;

      // ── DWM ─────────────────────────────────────────────────────────────────────────────

      /// <summary>
      ///    <para>Desktop Window Manager (DWM) attribute applied to a window.</para>
      ///    <para>Use with DwmSetWindowAttribute. Allows the window frame for this window to be
      ///       drawn in dark mode colors when the dark mode system setting is enabled. For
      ///       compatibility reasons, all windows default to light mode regardless of the system
      ///       setting. The pvAttribute parameter points to a value of type BOOL. TRUE to honor
      ///       dark mode for the window, FALSE to always use light mode.</para>
      ///    <para>This value is supported starting with Windows 11 Build 22000.</para>
      /// </summary>
      internal const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

      // ── UpdateLayeredWindow flags (ULW_*) ────────────────────────────────────────────────

      /// <summary>Use the <c>pblend</c> blend function. If the display mode is 256 colors, the
      ///    effect of this value is the same as the effect of ULW_OPAQUE.</summary>
      internal const uint ULW_ALPHA = 0x00000002;

      // ── SetWindowPos flags (SWP_*) ───────────────────────────────────────────────────────

      /// <summary>Retains the current size (ignores the cx and cy parameters).</summary>
      internal const uint SWP_NOSIZE = 0x0001;

      /// <summary>Retains the current position (ignores the X and Y parameters).</summary>
      internal const uint SWP_NOMOVE = 0x0002;

      /// <summary>Does not activate the window.</summary>
      internal const uint SWP_NOACTIVATE = 0x0010;

      /// <summary>Retains the current Z-order (ignores the hWndInsertAfter parameter).</summary>
      internal const uint SWP_NOZORDER = 0x0004;

      /// <summary>Displays the window.</summary>
      internal const uint SWP_SHOWWINDOW = 0x0040;

      /// <summary>Hides the window.</summary>
      internal const uint SWP_HIDEWINDOW = 0x0080;

      /// <summary>Places the window above all non-topmost windows. The window maintains its
      ///    topmost position even when it is deactivated.</summary>
      internal static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);

      // ── Hotkey modifiers (MOD_*) ─────────────────────────────────────────────────────────

      /// <summary>Either ALT key must be held down.</summary>
      internal const uint MOD_ALT = 0x0001;

      /// <summary>Either CTRL key must be held down.</summary>
      internal const uint MOD_CONTROL = 0x0002;

      /// <summary>Either SHIFT key must be held down.</summary>
      internal const uint MOD_SHIFT = 0x0004;

      // ── Window messages (WM_*) ───────────────────────────────────────────────────────────

      /// <summary>Sent when the cursor is in an inactive window and the user presses a mouse
      ///    button. The parent window receives this message only if the child window passes it to
      ///    the DefWindowProc function.</summary>
      internal const int WM_MOUSEACTIVATE = 0x0021;

      /// <summary>Sent to a window to determine what part of the window corresponds to a
      ///    particular screen coordinate. A window receives this message through its WindowProc
      ///    function.</summary>
      internal const int WM_NCHITTEST = 0x0084;

      /// <summary>Sent to a window when its nonclient area needs to be changed to indicate an
      ///    active or inactive state.</summary>
      internal const int WM_NCACTIVATE = 0x0086;

      /// <summary>Posted to the thread's message queue when a hotkey registered by RegisterHotKey
      ///    is pressed.</summary>
      internal const int WM_HOTKEY = 0x0312;

      /// <summary>Posted to a window when the cursor moves.</summary>
      internal const int WM_MOUSEMOVE = 0x0200;

      /// <summary>Posted when the user rotates the mouse wheel while the cursor is in the hot spot
      ///    of a window, or when the cursor is in a window and the user rotates the mouse wheel.</summary>
      internal const int WM_MOUSEWHEEL = 0x020A;

      // ── WM_MOUSEACTIVATE return values ───────────────────────────────────────────────────

      /// <summary>Does not activate the window and discards the mouse message.</summary>
      internal const int MA_NOACTIVATE = 3;

      // ── WM_NCHITTEST return values ───────────────────────────────────────────────────────

      /// <summary>In a window currently covered by another window in the same thread (the message
      ///    will be sent to underlying windows in the same thread until one of them returns a code
      ///    that is not HTTRANSPARENT).</summary>
      internal const int HTTRANSPARENT = -1;

      // ── Windows hook types (WH_*) ────────────────────────────────────────────────────────

      /// <summary>Low-level mouse input events hook identifier for SetWindowsHookEx.</summary>
      internal const int WH_MOUSE_LL = 14;

      // ── GetAsyncKeyState ─────────────────────────────────────────────────────────────────

      /// <summary>Bit set in the GetAsyncKeyState return value when the key is currently pressed.</summary>
      internal const int KEY_PRESSED = 0x8000;

      // ── Structs ──────────────────────────────────────────────────────────────────────────

      /// <summary>Callback type for a low-level mouse hook installed via SetWindowsHookEx with
      ///    WH_MOUSE_LL. The returned value determines whether the input is forwarded
      ///    (CallNextHookEx) or consumed (any non-zero value).</summary>
      internal delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

      [StructLayout(LayoutKind.Sequential)]
      internal struct BITMAPINFO
      {
         public BITMAPINFOHEADER bmiHeader;
      }

      [StructLayout(LayoutKind.Sequential)]
      internal struct BITMAPINFOHEADER
      {
         public uint biSize;

         public int biWidth, biHeight;

         public ushort biPlanes, biBitCount;

         public uint biCompression; // BI_RGB = 0

         public uint biSizeImage;

         public int biXPelsPerMeter, biYPelsPerMeter;

         public uint biClrUsed, biClrImportant;
      }

      [StructLayout(LayoutKind.Sequential, Pack = 1)]
      internal struct BLENDFUNCTION
      {
         public byte BlendOp, BlendFlags, SourceConstantAlpha, AlphaFormat;
      }

      // ── DWM functions ────────────────────────────────────────────────────────────────────

      [DllImport("dwmapi.dll")]
      internal static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

      // ── GDI32 functions ──────────────────────────────────────────────────────────────────

      [DllImport("Gdi32.dll", ExactSpelling = true, SetLastError = true)]
      internal static extern IntPtr CreateCompatibleDC(IntPtr hdc);

      [DllImport("Gdi32.dll", ExactSpelling = true, SetLastError = true)]
      internal static extern IntPtr CreateDIBSection(
         IntPtr hdc,
         ref BITMAPINFO pbmi,
         uint usage,
         out IntPtr ppvBits,
         IntPtr hSection,
         uint offset);

      [DllImport("Gdi32.dll")]
      internal static extern IntPtr CreatePen(int fnPenStyle, int nWidth, uint crColor);

      [DllImport("Gdi32.dll", ExactSpelling = true, SetLastError = true)]
      internal static extern bool DeleteDC(IntPtr hdc);

      [DllImport("Gdi32.dll", ExactSpelling = true, SetLastError = true)]
      internal static extern bool DeleteObject(IntPtr hobj);

      [DllImport("Gdi32.dll")]
      internal static extern bool LineTo(IntPtr hdc, int x, int y);

      [DllImport("Gdi32.dll")]
      internal static extern bool MoveToEx(IntPtr hdc, int x, int y, IntPtr lpPoint);

      [DllImport("Gdi32.dll", ExactSpelling = true)]
      internal static extern IntPtr SelectObject(IntPtr hdc, IntPtr h);

      // ── User32 functions ─────────────────────────────────────────────────────────────────

      [DllImport("user32.dll")]
      internal static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

      [DllImport("User32.dll")]
      internal static extern short GetAsyncKeyState(int vKey);

      [DllImport("user32.dll", SetLastError = true)]
      internal static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

      [DllImport("User32.dll")]
      internal static extern bool SetCursorPos(int x, int y);

      [DllImport("user32.dll", SetLastError = true)]
      internal static extern bool SetWindowPos(
         IntPtr hWnd,
         IntPtr hWndInsertAfter,
         int x,
         int y,
         int cx,
         int cy,
         uint uFlags);

      [DllImport("user32.dll")]
      internal static extern IntPtr SetWindowsHookEx(
         int idHook,
         LowLevelMouseProc lpfn,
         IntPtr hMod,
         uint dwThreadId);

      [DllImport("user32.dll")]
      internal static extern bool UnhookWindowsHookEx(IntPtr hhk);

      [DllImport("user32.dll")]
      internal static extern bool UnregisterHotKey(IntPtr hWnd, int id);

      [DllImport("User32.dll", ExactSpelling = true, SetLastError = true)]
      internal static extern bool UpdateLayeredWindow(
         IntPtr hwnd,
         IntPtr hdcDst,
         ref Point pptDst,
         ref Size psize,
         IntPtr hdcSrc,
         ref Point pptSrc,
         uint crKey,
         ref BLENDFUNCTION pblend,
         uint dwFlags);

      // ── Kernel32 functions ───────────────────────────────────────────────────────────────

      [DllImport("kernel32.dll")]
      internal static extern IntPtr GetModuleHandle(string? lpModuleName);

      [DllImport("kernel32.dll")]
      internal static extern void RtlZeroMemory(IntPtr dest, IntPtr size);
   }
}
