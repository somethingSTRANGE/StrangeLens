// -------------------------------------------------------------------------------------
// <copyright file="SvgImageFactory.cs">
//   Copyright (c) 2026
//   Licensed under the MIT License. See LICENSE file in the project root.
// </copyright>
// -------------------------------------------------------------------------------------

namespace StrangeLens
{
   using System;
   using System.Collections.Generic;
   using System.Diagnostics;
   using System.Drawing.Drawing2D;
   using System.Globalization;

   /// <summary>Builds and caches <see cref="SvgImage"/> instances from embedded SVG path data.
   ///    One static method per icon; square icons accept a single <c>size</c> parameter,
   ///    non-square icons accept <c>width, height</c>. The factory owns all
   ///    <see cref="System.Drawing.Drawing2D.GraphicsPath"/> objects -- callers must not dispose
   ///    the returned <see cref="SvgImage"/> instances.</summary>
   internal static partial class SvgImageFactory
   {
      private static readonly Dictionary<(int, int), SvgImage> cachedAboutDonateBuyMeACoffee = new();

      private static readonly Dictionary<(int, int), SvgImage> cachedAboutDonateGitHub = new();

      private static readonly Dictionary<(int, int), SvgImage> cachedAboutDonateKoFi = new();

      private static readonly Dictionary<(int, int), SvgImage> cachedAboutDonatePayPal = new();

      private static readonly Dictionary<(int, int), SvgImage> cachedAboutLogo = new();

      private static readonly Dictionary<(int, int), SvgImage> cachedAboutResourceIssues = new();

      private static readonly Dictionary<(int, int), SvgImage> cachedAboutResourceSource = new();

      private static readonly Dictionary<(int, int), SvgImage> cachedInfoColorPalette = new();

      private static readonly Dictionary<(int, int), SvgImage> cachedInfoColorValues = new();

      private static readonly Dictionary<(int, int), SvgImage> cachedInfoLensSize = new();

      private static readonly Dictionary<(int, int), SvgImage> cachedInfoMagnification = new();

      private static readonly Dictionary<(int, int), SvgImage> cachedInfoMousePosition = new();

      public static SvgImage AboutDonateBuyMeACoffee(int size)
      {
         return Get(cachedAboutDonateBuyMeACoffee, Data.AboutDonateBuyMeACoffeeIcon, size, size);
      }

      public static SvgImage AboutDonateGitHub(int size)
      {
         return Get(cachedAboutDonateGitHub, Data.AboutDonateGitHubSponsorsIcon, size, size);
      }

      public static SvgImage AboutDonateKoFi(int size)
      {
         return Get(cachedAboutDonateKoFi, Data.AboutDonateKoFiIcon, size, size);
      }

      public static SvgImage AboutDonatePayPal(int size)
      {
         return Get(cachedAboutDonatePayPal, Data.AboutDonatePayPalIcon, size, size);
      }

      public static SvgImage AboutLogo(int width, int height)
      {
         return Get(cachedAboutLogo, Data.AboutLogoImage, width, height);
      }

      public static SvgImage AboutResourceIssues(int size)
      {
         return Get(cachedAboutResourceIssues, Data.AboutResourceIssuesIcon, size, size);
      }

      public static SvgImage AboutResourceSource(int size)
      {
         return Get(cachedAboutResourceSource, Data.AboutResourceSourceIcon, size, size);
      }

      public static SvgImage InfoColorPalette(int size)
      {
         return Get(cachedInfoColorPalette, Data.InfoColorPaletteIcon, size, size);
      }

      public static SvgImage InfoColorValues(int size)
      {
         return Get(cachedInfoColorValues, Data.InfoColorValuesIcon, size, size);
      }

      public static SvgImage InfoLensSize(int size)
      {
         return Get(cachedInfoLensSize, Data.InfoLensSizeIcon, size, size);
      }

      public static SvgImage InfoMagnification(int size)
      {
         return Get(cachedInfoMagnification, Data.InfoMagnificationIcon, size, size);
      }

      public static SvgImage InfoMousePosition(int size)
      {
         return Get(cachedInfoMousePosition, Data.InfoMouseCursorIcon, size, size);
      }

      /// <summary>Converts SVG endpoint arc parameterisation to a GDI+ AddArc call. Implements the
      ///    algorithm from SVG 1.1 spec appendix B (F.6).</summary>
      private static void AddSvgArc(
         GraphicsPath path,
         float x1,
         float y1,
         float rx,
         float ry,
         float phiDeg,
         bool fa,
         bool fs,
         float x2,
         float y2)
      {
         if ((x1 == x2) && (y1 == y2))
         {
            return;
         }

         if ((rx == 0) || (ry == 0))
         {
            path.AddLine(x1, y1, x2, y2);
            return;
         }

         var phi = (phiDeg * Math.PI) / 180;
         double cosP = Math.Cos(phi), sinP = Math.Sin(phi);

         double dx = (x1 - x2) / 2.0, dy = (y1 - y2) / 2.0;
         var x1p = (cosP * dx) + (sinP * dy);
         var y1p = (-sinP * dx) + (cosP * dy);

         // Ensure radii are large enough.
         var lambda = ((x1p * x1p) / (rx * (double)rx)) + ((y1p * y1p) / (ry * (double)ry));
         if (lambda > 1)
         {
            var s = Math.Sqrt(lambda);
            rx = (float)(s * rx);
            ry = (float)(s * ry);
         }

         double rxq = (double)rx * rx, ryq = (double)ry * ry;
         double x1pq = x1p * x1p, y1pq = y1p * y1p;
         var num = Math.Max(0, (rxq * ryq) - (rxq * y1pq) - (ryq * x1pq));
         var den = (rxq * y1pq) + (ryq * x1pq);
         var sq = (fa == fs ? -1 : 1) * Math.Sqrt(den == 0 ? 0 : num / den);
         var cxp = (sq * rx * y1p) / ry;
         var cyp = (-sq * ry * x1p) / rx;

         var cx = ((cosP * cxp) - (sinP * cyp)) + ((x1 + x2) / 2.0);
         var cy = (sinP * cxp) + (cosP * cyp) + ((y1 + y2) / 2.0);

         double ux = (x1p - cxp) / rx, uy = (y1p - cyp) / ry;
         double vx = (-x1p - cxp) / rx, vy = (-y1p - cyp) / ry;

         var startAngle = SvgAngle(1, 0, ux, uy);
         var sweepAngle = SvgAngle(ux, uy, vx, vy);
         if (!fs && (sweepAngle > 0))
         {
            sweepAngle -= 360;
         }

         if (fs && (sweepAngle < 0))
         {
            sweepAngle += 360;
         }

         if (phiDeg != 0)
         {
            // Rotated ellipse: apply transform around centre, draw, restore.
            var state = path.GetLastPoint(); // dummy -- handled via Graphics transform at render time
            Debug.WriteLine($"[SvgImageFactory] rotated arc (phi={phiDeg}) not fully supported");
         }

         path.AddArc(
            (float)(cx - rx),
            (float)(cy - ry),
            rx * 2,
            ry * 2,
            (float)startAngle,
            (float)sweepAngle);
      }

      /// <summary>Builds a <see cref="GraphicsPath"/> scaled so the viewBox fills exactly
      ///    <paramref name="width"/> x <paramref name="height"/> pixels (exact fill, no
      ///    letterboxing). Absolute coords use separate scaleX/scaleY; relative coords use the
      ///    same per-axis scale.</summary>
      private static GraphicsPath BuildPath(string viewBox, string[] segments, int width, int height)
      {
         float minX = 0, minY = 0, viewW = 640, viewH = 640;
         var vb = viewBox.Trim().Split(' ');
         if (vb.Length == 2)
         {
            viewW = float.Parse(vb[0], CultureInfo.InvariantCulture);
            viewH = float.Parse(vb[1], CultureInfo.InvariantCulture);
         }
         else if (vb.Length >= 4)
         {
            minX = float.Parse(vb[0], CultureInfo.InvariantCulture);
            minY = float.Parse(vb[1], CultureInfo.InvariantCulture);
            viewW = float.Parse(vb[2], CultureInfo.InvariantCulture);
            viewH = float.Parse(vb[3], CultureInfo.InvariantCulture);
         }

         var sx = width / viewW; // scaleX: SVG unit -> screen pixel on X axis
         var sy = height / viewH; // scaleY: SVG unit -> screen pixel on Y axis
         var ox = minX * sx; // scaled origin offset X
         var oy = minY * sy; // scaled origin offset Y

         var path = new GraphicsPath(FillMode.Winding);
         float cx = -ox, cy = -oy;
         float mx = cx, my = cy;

         var prevCmd = '\0';
         float prevCp2x = 0, prevCp2y = 0; // 2nd control point of last C/c/S/s (for S/s reflection)
         float prevQcpx = 0, prevQcpy = 0; // control point of last Q/q/T/t (for T/t reflection)

         foreach (var seg in segments)
         foreach (var (cmd, argStr) in Tokenize(seg))
         {
            var raw = ParseArgs(argStr);
            var ri = 0;
            var repeatAs = cmd; // M->L and m->l after first pair; all others stay constant

            do
            {
               // Validate enough args remain before consuming.
               static bool Need(int n, int have, char c)
               {
                  if (have >= n)
                  {
                     return true;
                  }

                  Debug.WriteLine($"[SvgImageFactory] '{c}': need {n} args, have {have}");
                  return false;
               }

               float tx, ty, x1, y1, x2, y2, x3, y3, dx, dy, qcpx, qcpy;
               var avail = raw.Length - ri;

               switch (repeatAs)
               {
                  case 'M':
                     if (!Need(2, avail, repeatAs))
                     {
                        ri = raw.Length;
                        break;
                     }

                     cx = mx = (raw[ri] * sx) - ox;
                     cy = my = (raw[ri + 1] * sy) - oy;
                     ri += 2;
                     path.StartFigure();
                     repeatAs = 'L';
                     break;
                  case 'm':
                     if (!Need(2, avail, repeatAs))
                     {
                        ri = raw.Length;
                        break;
                     }

                     cx = mx = cx + (raw[ri] * sx);
                     cy = my = cy + (raw[ri + 1] * sy);
                     ri += 2;
                     path.StartFigure();
                     repeatAs = 'l';
                     break;

                  case 'L':
                     if (!Need(2, avail, repeatAs))
                     {
                        ri = raw.Length;
                        break;
                     }

                     tx = (raw[ri] * sx) - ox;
                     ty = (raw[ri + 1] * sy) - oy;
                     ri += 2;
                     path.AddLine(cx, cy, tx, ty);
                     cx = tx;
                     cy = ty;
                     break;
                  case 'l':
                     if (!Need(2, avail, repeatAs))
                     {
                        ri = raw.Length;
                        break;
                     }

                     dx = raw[ri] * sx;
                     dy = raw[ri + 1] * sy;
                     ri += 2;
                     path.AddLine(cx, cy, cx + dx, cy + dy);
                     cx += dx;
                     cy += dy;
                     break;

                  case 'H':
                     if (!Need(1, avail, repeatAs))
                     {
                        ri = raw.Length;
                        break;
                     }

                     tx = (raw[ri] * sx) - ox;
                     ri++;
                     path.AddLine(cx, cy, tx, cy);
                     cx = tx;
                     break;
                  case 'h':
                     if (!Need(1, avail, repeatAs))
                     {
                        ri = raw.Length;
                        break;
                     }

                     dx = raw[ri] * sx;
                     ri++;
                     path.AddLine(cx, cy, cx + dx, cy);
                     cx += dx;
                     break;

                  case 'V':
                     if (!Need(1, avail, repeatAs))
                     {
                        ri = raw.Length;
                        break;
                     }

                     ty = (raw[ri] * sy) - oy;
                     ri++;
                     path.AddLine(cx, cy, cx, ty);
                     cy = ty;
                     break;
                  case 'v':
                     if (!Need(1, avail, repeatAs))
                     {
                        ri = raw.Length;
                        break;
                     }

                     dy = raw[ri] * sy;
                     ri++;
                     path.AddLine(cx, cy, cx, cy + dy);
                     cy += dy;
                     break;

                  case 'C':
                     if (!Need(6, avail, repeatAs))
                     {
                        ri = raw.Length;
                        break;
                     }

                     x1 = (raw[ri] * sx) - ox;
                     y1 = (raw[ri + 1] * sy) - oy;
                     x2 = (raw[ri + 2] * sx) - ox;
                     y2 = (raw[ri + 3] * sy) - oy;
                     x3 = (raw[ri + 4] * sx) - ox;
                     y3 = (raw[ri + 5] * sy) - oy;
                     ri += 6;
                     path.AddBezier(cx, cy, x1, y1, x2, y2, x3, y3);
                     prevCp2x = x2;
                     prevCp2y = y2;
                     cx = x3;
                     cy = y3;
                     break;
                  case 'c':
                     if (!Need(6, avail, repeatAs))
                     {
                        ri = raw.Length;
                        break;
                     }

                     x1 = cx + (raw[ri] * sx);
                     y1 = cy + (raw[ri + 1] * sy);
                     x2 = cx + (raw[ri + 2] * sx);
                     y2 = cy + (raw[ri + 3] * sy);
                     x3 = cx + (raw[ri + 4] * sx);
                     y3 = cy + (raw[ri + 5] * sy);
                     ri += 6;
                     path.AddBezier(cx, cy, x1, y1, x2, y2, x3, y3);
                     prevCp2x = x2;
                     prevCp2y = y2;
                     cx = x3;
                     cy = y3;
                     break;

                  case 'S':
                     {
                        if (!Need(4, avail, repeatAs))
                        {
                           ri = raw.Length;
                           break;
                        }

                        var wasCubic = (prevCmd == 'C') || (prevCmd == 'c') || (prevCmd == 'S')
                                       || (prevCmd == 's');
                        x1 = wasCubic ? (2 * cx) - prevCp2x : cx;
                        y1 = wasCubic ? (2 * cy) - prevCp2y : cy;
                        x2 = (raw[ri] * sx) - ox;
                        y2 = (raw[ri + 1] * sy) - oy;
                        x3 = (raw[ri + 2] * sx) - ox;
                        y3 = (raw[ri + 3] * sy) - oy;
                        ri += 4;
                        path.AddBezier(cx, cy, x1, y1, x2, y2, x3, y3);
                        prevCp2x = x2;
                        prevCp2y = y2;
                        cx = x3;
                        cy = y3;
                     }
                     break;
                  case 's':
                     {
                        if (!Need(4, avail, repeatAs))
                        {
                           ri = raw.Length;
                           break;
                        }

                        var wasCubic = (prevCmd == 'C') || (prevCmd == 'c') || (prevCmd == 'S')
                                       || (prevCmd == 's');
                        x1 = wasCubic ? (2 * cx) - prevCp2x : cx;
                        y1 = wasCubic ? (2 * cy) - prevCp2y : cy;
                        x2 = cx + (raw[ri] * sx);
                        y2 = cy + (raw[ri + 1] * sy);
                        x3 = cx + (raw[ri + 2] * sx);
                        y3 = cy + (raw[ri + 3] * sy);
                        ri += 4;
                        path.AddBezier(cx, cy, x1, y1, x2, y2, x3, y3);
                        prevCp2x = x2;
                        prevCp2y = y2;
                        cx = x3;
                        cy = y3;
                     }
                     break;

                  case 'Q':
                     if (!Need(4, avail, repeatAs))
                     {
                        ri = raw.Length;
                        break;
                     }

                     qcpx = (raw[ri] * sx) - ox;
                     qcpy = (raw[ri + 1] * sy) - oy;
                     x3 = (raw[ri + 2] * sx) - ox;
                     y3 = (raw[ri + 3] * sy) - oy;
                     ri += 4;
                     x1 = cx + ((2f / 3) * (qcpx - cx));
                     y1 = cy + ((2f / 3) * (qcpy - cy));
                     x2 = x3 + ((2f / 3) * (qcpx - x3));
                     y2 = y3 + ((2f / 3) * (qcpy - y3));
                     path.AddBezier(cx, cy, x1, y1, x2, y2, x3, y3);
                     prevQcpx = qcpx;
                     prevQcpy = qcpy;
                     cx = x3;
                     cy = y3;
                     break;
                  case 'q':
                     if (!Need(4, avail, repeatAs))
                     {
                        ri = raw.Length;
                        break;
                     }

                     qcpx = cx + (raw[ri] * sx);
                     qcpy = cy + (raw[ri + 1] * sy);
                     x3 = cx + (raw[ri + 2] * sx);
                     y3 = cy + (raw[ri + 3] * sy);
                     ri += 4;
                     x1 = cx + ((2f / 3) * (qcpx - cx));
                     y1 = cy + ((2f / 3) * (qcpy - cy));
                     x2 = x3 + ((2f / 3) * (qcpx - x3));
                     y2 = y3 + ((2f / 3) * (qcpy - y3));
                     path.AddBezier(cx, cy, x1, y1, x2, y2, x3, y3);
                     prevQcpx = qcpx;
                     prevQcpy = qcpy;
                     cx = x3;
                     cy = y3;
                     break;

                  case 'T':
                     {
                        if (!Need(2, avail, repeatAs))
                        {
                           ri = raw.Length;
                           break;
                        }

                        var wasQuad = (prevCmd == 'Q') || (prevCmd == 'q') || (prevCmd == 'T')
                                      || (prevCmd == 't');
                        qcpx = wasQuad ? (2 * cx) - prevQcpx : cx;
                        qcpy = wasQuad ? (2 * cy) - prevQcpy : cy;
                        x3 = (raw[ri] * sx) - ox;
                        y3 = (raw[ri + 1] * sy) - oy;
                        ri += 2;
                        x1 = cx + ((2f / 3) * (qcpx - cx));
                        y1 = cy + ((2f / 3) * (qcpy - cy));
                        x2 = x3 + ((2f / 3) * (qcpx - x3));
                        y2 = y3 + ((2f / 3) * (qcpy - y3));
                        path.AddBezier(cx, cy, x1, y1, x2, y2, x3, y3);
                        prevQcpx = qcpx;
                        prevQcpy = qcpy;
                        cx = x3;
                        cy = y3;
                     }
                     break;
                  case 't':
                     {
                        if (!Need(2, avail, repeatAs))
                        {
                           ri = raw.Length;
                           break;
                        }

                        var wasQuad = (prevCmd == 'Q') || (prevCmd == 'q') || (prevCmd == 'T')
                                      || (prevCmd == 't');
                        qcpx = wasQuad ? (2 * cx) - prevQcpx : cx;
                        qcpy = wasQuad ? (2 * cy) - prevQcpy : cy;
                        x3 = cx + (raw[ri] * sx);
                        y3 = cy + (raw[ri + 1] * sy);
                        ri += 2;
                        x1 = cx + ((2f / 3) * (qcpx - cx));
                        y1 = cy + ((2f / 3) * (qcpy - cy));
                        x2 = x3 + ((2f / 3) * (qcpx - x3));
                        y2 = y3 + ((2f / 3) * (qcpy - y3));
                        path.AddBezier(cx, cy, x1, y1, x2, y2, x3, y3);
                        prevQcpx = qcpx;
                        prevQcpy = qcpy;
                        cx = x3;
                        cy = y3;
                     }
                     break;

                  case 'A':
                     {
                        if (!Need(7, avail, repeatAs))
                        {
                           ri = raw.Length;
                           break;
                        }

                        float arx = Math.Abs(raw[ri] * sx), ary = Math.Abs(raw[ri + 1] * sy);
                        var phi = raw[ri + 2];
                        bool fa = raw[ri + 3] != 0, fs = raw[ri + 4] != 0;
                        x2 = (raw[ri + 5] * sx) - ox;
                        y2 = (raw[ri + 6] * sy) - oy;
                        ri += 7;
                        AddSvgArc(path, cx, cy, arx, ary, phi, fa, fs, x2, y2);
                        cx = x2;
                        cy = y2;
                     }
                     break;
                  case 'a':
                     {
                        if (!Need(7, avail, repeatAs))
                        {
                           ri = raw.Length;
                           break;
                        }

                        float arx = Math.Abs(raw[ri] * sx), ary = Math.Abs(raw[ri + 1] * sy);
                        var phi = raw[ri + 2];
                        bool fa = raw[ri + 3] != 0, fs = raw[ri + 4] != 0;
                        x2 = cx + (raw[ri + 5] * sx);
                        y2 = cy + (raw[ri + 6] * sy);
                        ri += 7;
                        AddSvgArc(path, cx, cy, arx, ary, phi, fa, fs, x2, y2);
                        cx = x2;
                        cy = y2;
                     }
                     break;

                  case 'Z':
                  case 'z':
                     path.CloseFigure();
                     cx = mx;
                     cy = my;
                     ri = raw.Length; // Z takes no params; exit loop
                     break;

                  default:
                     Debug.WriteLine($"[SvgImageFactory] unhandled command '{repeatAs}' (args: {argStr})");
                     ri = raw.Length;
                     break;
               }

               prevCmd = repeatAs;
            }
            while (ri < raw.Length);
         }

         return path;
      }

      private static SvgImage Get(Dictionary<(int, int), SvgImage> cache, SvgData data, int w, int h)
      {
         var key = (w, h);
         if (!cache.TryGetValue(key, out var img))
         {
            var path1 = BuildPath(data.ViewBox, data.Primary, w, h);
            var path2 = data.Secondary != null ? BuildPath(data.ViewBox, data.Secondary, w, h) : null;
            img = new SvgImage(path1, path2, w, h);
            cache[key] = img;
         }

         return img;
      }

      /// <summary>Parses SVG number lists, handling whitespace/comma separators AND the compact
      ///    format where a leading '-' or '+' acts as a separator (e.g. "0-1.5" -> 0, -1.5). Also
      ///    handles scientific notation (e.g. "3.6e-4").</summary>
      private static float[] ParseArgs(string argStr)
      {
         if (argStr.Length == 0)
         {
            return Array.Empty<float>();
         }

         var nums = new List<float>();
         int i = 0, n = argStr.Length;
         while (i < n)
         {
            while ((i < n) && ((argStr[i] == ' ') || (argStr[i] == '\t') || (argStr[i] == ',')))
            {
               i++;
            }

            if (i >= n)
            {
               break;
            }

            var start = i;
            if ((argStr[i] == '-') || (argStr[i] == '+'))
            {
               i++;
            }

            var hasDot = false;
            while (i < n)
            {
               var c = argStr[i];
               if ((c >= '0') && (c <= '9'))
               {
                  i++;
               }
               else if ((c == '.') && !hasDot)
               {
                  hasDot = true;
                  i++;
               }
               else if ((c == 'e') || (c == 'E'))
               {
                  i++;
                  if ((i < n) && ((argStr[i] == '-') || (argStr[i] == '+')))
                  {
                     i++;
                  }

                  while ((i < n) && (argStr[i] >= '0') && (argStr[i] <= '9'))
                  {
                     i++;
                  }

                  break;
               }
               else
               {
                  break;
               }
            }

            if (i > start)
            {
               nums.Add(float.Parse(argStr.Substring(start, i - start), CultureInfo.InvariantCulture));
            }
            else
            {
               i++; // unrecognized character -- skip to avoid infinite loop
            }
         }

         return nums.ToArray();
      }

      private static double SvgAngle(double ux, double uy, double vx, double vy)
      {
         var dot = (ux * vx) + (uy * vy);
         var len = Math.Sqrt((ux * ux) + (uy * uy)) * Math.Sqrt((vx * vx) + (vy * vy));
         var a = (Math.Acos(Math.Max(-1, Math.Min(1, len == 0 ? 0 : dot / len))) * 180) / Math.PI;
         return (ux * vy) - (uy * vx) < 0 ? -a : a;
      }

      private static IEnumerable<(char cmd, string args)> Tokenize(string segment)
      {
         const string Letters = "MmLlHhVvCcSsQqTtAaZz";
         var start = 0;
         var cmd = '\0';
         for (var i = 0; i < segment.Length; i++)
         {
            if (Letters.IndexOf(segment[i]) < 0)
            {
               continue;
            }

            if (cmd != '\0')
            {
               yield return (cmd, segment.Substring(start, i - start).Trim());
            }

            cmd = segment[i];
            start = i + 1;
         }

         if (cmd != '\0')
         {
            yield return (cmd, segment.Substring(start).Trim());
         }
      }
   }
}
