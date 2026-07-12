// -------------------------------------------------------------------------------------
// <copyright file="VectorImageFactory.cs">
//   Copyright (c) 2026
//   Licensed under the MIT License. See LICENSE file in the project root.
// </copyright>
// -------------------------------------------------------------------------------------

namespace StrangeLens
{
   using System;
   using System.Collections.Generic;
   using System.Diagnostics;
   using System.Diagnostics.CodeAnalysis;
   using System.Drawing.Drawing2D;
   using System.Globalization;

   /// <summary>Builds and caches <see cref="VectorImage"/> instances from embedded SVG path
   ///    data. One static method per icon; square icons accept a single <c>size</c> parameter,
   ///    non-square icons accept <c>width, height</c>. The factory owns all
   ///    <see cref="System.Drawing.Drawing2D.GraphicsPath"/> objects -- callers must not
   ///    dispose the returned <see cref="VectorImage"/> instances.</summary>
   internal static partial class VectorImageFactory
   {
      private static readonly Dictionary<(int, int), VectorImage> cachedAboutDonateBuyMeACoffee = new();

      private static readonly Dictionary<(int, int), VectorImage> cachedAboutDonateGitHub = new();

      private static readonly Dictionary<(int, int), VectorImage> cachedAboutDonateKoFi = new();

      private static readonly Dictionary<(int, int), VectorImage> cachedAboutDonatePayPal = new();

      private static readonly Dictionary<(int, int), VectorImage> cachedAboutLogo = new();

      private static readonly Dictionary<(int, int), VectorImage> cachedAboutResourceIssues = new();

      private static readonly Dictionary<(int, int), VectorImage> cachedAboutResourceSource = new();

      private static readonly Dictionary<(int, int), VectorImage> cachedInfoColorPalette = new();

      private static readonly Dictionary<(int, int), VectorImage> cachedInfoColorValues = new();

      private static readonly Dictionary<(int, int), VectorImage> cachedInfoLensSize = new();

      private static readonly Dictionary<(int, int), VectorImage> cachedInfoMagnification = new();

      private static readonly Dictionary<(int, int), VectorImage> cachedInfoMousePosition = new();

      private static readonly Dictionary<(int, int), VectorImage> cachedInfoRuler = new();

      public static VectorImage AboutDonateBuyMeACoffee(int size)
      {
         return Get(cachedAboutDonateBuyMeACoffee, Data.AboutDonateBuyMeACoffeeIcon, size, size);
      }

      public static VectorImage AboutDonateGitHub(int size)
      {
         return Get(cachedAboutDonateGitHub, Data.AboutDonateGitHubSponsorsIcon, size, size);
      }

      public static VectorImage AboutDonateKoFi(int size)
      {
         return Get(cachedAboutDonateKoFi, Data.AboutDonateKoFiIcon, size, size);
      }

      public static VectorImage AboutDonatePayPal(int size)
      {
         return Get(cachedAboutDonatePayPal, Data.AboutDonatePayPalIcon, size, size);
      }

      public static VectorImage AboutLogo(int width, int height)
      {
         return Get(cachedAboutLogo, Data.AboutLogoImage, width, height);
      }

      public static VectorImage AboutResourceIssues(int size)
      {
         return Get(cachedAboutResourceIssues, Data.AboutResourceIssuesIcon, size, size);
      }

      public static VectorImage AboutResourceSource(int size)
      {
         return Get(cachedAboutResourceSource, Data.AboutResourceSourceIcon, size, size);
      }

      public static VectorImage InfoColorPalette(int size)
      {
         return Get(cachedInfoColorPalette, Data.InfoColorPaletteIcon, size, size);
      }

      public static VectorImage InfoColorValues(int size)
      {
         return Get(cachedInfoColorValues, Data.InfoColorValuesIcon, size, size);
      }

      public static VectorImage InfoLensSize(int size)
      {
         return Get(cachedInfoLensSize, Data.InfoLensSizeIcon, size, size);
      }

      public static VectorImage InfoMagnification(int size)
      {
         return Get(cachedInfoMagnification, Data.InfoMagnificationIcon, size, size);
      }

      public static VectorImage InfoMousePosition(int size)
      {
         return Get(cachedInfoMousePosition, Data.InfoMouseCursorIcon, size, size);
      }

      public static VectorImage InfoRuler(int size)
      {
         return Get(cachedInfoRuler, Data.InfoRulerIcon, size, size);
      }

      private static void AddQuadBezier(
         GraphicsPath path,
         float cx,
         float cy,
         float qcpX,
         float qcpY,
         float x3,
         float y3)
      {
         path.AddBezier(
            cx,
            cy,
            cx + ((2f / 3) * (qcpX - cx)),
            cy + ((2f / 3) * (qcpY - cy)),
            x3 + ((2f / 3) * (qcpX - x3)),
            y3 + ((2f / 3) * (qcpY - y3)),
            x3,
            y3);
      }

      /// <summary>Converts SVG endpoint arc parameterization to a GDI+ AddArc call. Implements the
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
         if (x1.IsNearlyEqual(x2) && y1.IsNearlyEqual(y2))
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
         var x1P = (cosP * dx) + (sinP * dy);
         var y1P = (-sinP * dx) + (cosP * dy);

         // Ensure radii are large enough.
         var lambda = ((x1P * x1P) / (rx * (double)rx)) + ((y1P * y1P) / (ry * (double)ry));
         if (lambda > 1)
         {
            var s = Math.Sqrt(lambda);
            rx = (float)(s * rx);
            ry = (float)(s * ry);
         }

         double rxq = (double)rx * rx, ryq = (double)ry * ry;
         double x1Pq = x1P * x1P, y1Pq = y1P * y1P;
         var num = Math.Max(0, (rxq * ryq) - (rxq * y1Pq) - (ryq * x1Pq));
         var den = (rxq * y1Pq) + (ryq * x1Pq);
         var sq = (fa == fs ? -1 : 1) * Math.Sqrt(den == 0 ? 0 : num / den);
         var cxp = (sq * rx * y1P) / ry;
         var cyp = (-sq * ry * x1P) / rx;

         var cx = ((cosP * cxp) - (sinP * cyp)) + ((x1 + x2) / 2.0);
         var cy = (sinP * cxp) + (cosP * cyp) + ((y1 + y2) / 2.0);

         double ux = (x1P - cxp) / rx, uy = (y1P - cyp) / ry;
         double vx = (-x1P - cxp) / rx, vy = (-y1P - cyp) / ry;

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
            Debug.WriteLine($"[VectorImageFactory] rotated arc (phi={phiDeg}) not fully supported");
         }

         path.AddArc(
            (float)(cx - rx),
            (float)(cy - ry),
            rx * 2,
            ry * 2,
            (float)startAngle,
            (float)sweepAngle);
      }

      [SuppressMessage("ReSharper", "CognitiveComplexity")]
      private static char ApplyCommand(
         ref PathCursor cur,
         char cmd,
         float[] raw,
         ref int ri,
         GraphicsPath path,
         float sx,
         float sy,
         float ox,
         float oy)
      {
         static bool Need(int n, int have, char c)
         {
            if (have >= n)
            {
               return true;
            }

            Debug.WriteLine($"[VectorImageFactory] '{c}': need {n} args, have {have}");
            return false;
         }

         float tx, ty, x1, y1, x2, y2, x3, y3, dx, dy, quadCpX, quadCpY;
         var avail = raw.Length - ri;

         switch (cmd)
         {
            case 'M':
               if (!Need(2, avail, cmd))
               {
                  ri = raw.Length;
                  break;
               }

               cur.Cx = cur.Mx = (raw[ri] * sx) - ox;
               cur.Cy = cur.My = (raw[ri + 1] * sy) - oy;
               ri += 2;
               path.StartFigure();
               return 'L';
            case 'm':
               if (!Need(2, avail, cmd))
               {
                  ri = raw.Length;
                  break;
               }

               cur.Cx = cur.Mx = cur.Cx + (raw[ri] * sx);
               cur.Cy = cur.My = cur.Cy + (raw[ri + 1] * sy);
               ri += 2;
               path.StartFigure();
               return 'l';

            case 'L':
               if (!Need(2, avail, cmd))
               {
                  ri = raw.Length;
                  break;
               }

               tx = (raw[ri] * sx) - ox;
               ty = (raw[ri + 1] * sy) - oy;
               ri += 2;
               path.AddLine(cur.Cx, cur.Cy, tx, ty);
               cur.Cx = tx;
               cur.Cy = ty;
               break;
            case 'l':
               if (!Need(2, avail, cmd))
               {
                  ri = raw.Length;
                  break;
               }

               dx = raw[ri] * sx;
               dy = raw[ri + 1] * sy;
               ri += 2;
               path.AddLine(cur.Cx, cur.Cy, cur.Cx + dx, cur.Cy + dy);
               cur.Cx += dx;
               cur.Cy += dy;
               break;

            case 'H':
               if (!Need(1, avail, cmd))
               {
                  ri = raw.Length;
                  break;
               }

               tx = (raw[ri] * sx) - ox;
               ri++;
               path.AddLine(cur.Cx, cur.Cy, tx, cur.Cy);
               cur.Cx = tx;
               break;
            case 'h':
               if (!Need(1, avail, cmd))
               {
                  ri = raw.Length;
                  break;
               }

               dx = raw[ri] * sx;
               ri++;
               path.AddLine(cur.Cx, cur.Cy, cur.Cx + dx, cur.Cy);
               cur.Cx += dx;
               break;

            case 'V':
               if (!Need(1, avail, cmd))
               {
                  ri = raw.Length;
                  break;
               }

               ty = (raw[ri] * sy) - oy;
               ri++;
               path.AddLine(cur.Cx, cur.Cy, cur.Cx, ty);
               cur.Cy = ty;
               break;
            case 'v':
               if (!Need(1, avail, cmd))
               {
                  ri = raw.Length;
                  break;
               }

               dy = raw[ri] * sy;
               ri++;
               path.AddLine(cur.Cx, cur.Cy, cur.Cx, cur.Cy + dy);
               cur.Cy += dy;
               break;

            case 'C':
               if (!Need(6, avail, cmd))
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
               path.AddBezier(cur.Cx, cur.Cy, x1, y1, x2, y2, x3, y3);
               cur.PrevCp2X = x2;
               cur.PrevCp2Y = y2;
               cur.Cx = x3;
               cur.Cy = y3;
               break;
            case 'c':
               if (!Need(6, avail, cmd))
               {
                  ri = raw.Length;
                  break;
               }

               x1 = cur.Cx + (raw[ri] * sx);
               y1 = cur.Cy + (raw[ri + 1] * sy);
               x2 = cur.Cx + (raw[ri + 2] * sx);
               y2 = cur.Cy + (raw[ri + 3] * sy);
               x3 = cur.Cx + (raw[ri + 4] * sx);
               y3 = cur.Cy + (raw[ri + 5] * sy);
               ri += 6;
               path.AddBezier(cur.Cx, cur.Cy, x1, y1, x2, y2, x3, y3);
               cur.PrevCp2X = x2;
               cur.PrevCp2Y = y2;
               cur.Cx = x3;
               cur.Cy = y3;
               break;

            case 'S':
               {
                  if (!Need(4, avail, cmd))
                  {
                     ri = raw.Length;
                     break;
                  }

                  var wasCubic = cur.PrevCmd is 'C' or 'c' or 'S' or 's';
                  x1 = wasCubic ? (2 * cur.Cx) - cur.PrevCp2X : cur.Cx;
                  y1 = wasCubic ? (2 * cur.Cy) - cur.PrevCp2Y : cur.Cy;
                  x2 = (raw[ri] * sx) - ox;
                  y2 = (raw[ri + 1] * sy) - oy;
                  x3 = (raw[ri + 2] * sx) - ox;
                  y3 = (raw[ri + 3] * sy) - oy;
                  ri += 4;
                  path.AddBezier(cur.Cx, cur.Cy, x1, y1, x2, y2, x3, y3);
                  cur.PrevCp2X = x2;
                  cur.PrevCp2Y = y2;
                  cur.Cx = x3;
                  cur.Cy = y3;
               }
               break;
            case 's':
               {
                  if (!Need(4, avail, cmd))
                  {
                     ri = raw.Length;
                     break;
                  }

                  var wasCubic = cur.PrevCmd is 'C' or 'c' or 'S' or 's';
                  x1 = wasCubic ? (2 * cur.Cx) - cur.PrevCp2X : cur.Cx;
                  y1 = wasCubic ? (2 * cur.Cy) - cur.PrevCp2Y : cur.Cy;
                  x2 = cur.Cx + (raw[ri] * sx);
                  y2 = cur.Cy + (raw[ri + 1] * sy);
                  x3 = cur.Cx + (raw[ri + 2] * sx);
                  y3 = cur.Cy + (raw[ri + 3] * sy);
                  ri += 4;
                  path.AddBezier(cur.Cx, cur.Cy, x1, y1, x2, y2, x3, y3);
                  cur.PrevCp2X = x2;
                  cur.PrevCp2Y = y2;
                  cur.Cx = x3;
                  cur.Cy = y3;
               }
               break;

            case 'Q':
               if (!Need(4, avail, cmd))
               {
                  ri = raw.Length;
                  break;
               }

               quadCpX = (raw[ri] * sx) - ox;
               quadCpY = (raw[ri + 1] * sy) - oy;
               x3 = (raw[ri + 2] * sx) - ox;
               y3 = (raw[ri + 3] * sy) - oy;
               ri += 4;
               AddQuadBezier(path, cur.Cx, cur.Cy, quadCpX, quadCpY, x3, y3);
               cur.PrevQuadCpX = quadCpX;
               cur.PrevQuadCpY = quadCpY;
               cur.Cx = x3;
               cur.Cy = y3;
               break;
            case 'q':
               if (!Need(4, avail, cmd))
               {
                  ri = raw.Length;
                  break;
               }

               quadCpX = cur.Cx + (raw[ri] * sx);
               quadCpY = cur.Cy + (raw[ri + 1] * sy);
               x3 = cur.Cx + (raw[ri + 2] * sx);
               y3 = cur.Cy + (raw[ri + 3] * sy);
               ri += 4;
               AddQuadBezier(path, cur.Cx, cur.Cy, quadCpX, quadCpY, x3, y3);
               cur.PrevQuadCpX = quadCpX;
               cur.PrevQuadCpY = quadCpY;
               cur.Cx = x3;
               cur.Cy = y3;
               break;

            case 'T':
               {
                  if (!Need(2, avail, cmd))
                  {
                     ri = raw.Length;
                     break;
                  }

                  var wasQuad = cur.PrevCmd is 'Q' or 'q' or 'T' or 't';
                  quadCpX = wasQuad ? (2 * cur.Cx) - cur.PrevQuadCpX : cur.Cx;
                  quadCpY = wasQuad ? (2 * cur.Cy) - cur.PrevQuadCpY : cur.Cy;
                  x3 = (raw[ri] * sx) - ox;
                  y3 = (raw[ri + 1] * sy) - oy;
                  ri += 2;
                  AddQuadBezier(path, cur.Cx, cur.Cy, quadCpX, quadCpY, x3, y3);
                  cur.PrevQuadCpX = quadCpX;
                  cur.PrevQuadCpY = quadCpY;
                  cur.Cx = x3;
                  cur.Cy = y3;
               }
               break;
            case 't':
               {
                  if (!Need(2, avail, cmd))
                  {
                     ri = raw.Length;
                     break;
                  }

                  var wasQuad = cur.PrevCmd is 'Q' or 'q' or 'T' or 't';
                  quadCpX = wasQuad ? (2 * cur.Cx) - cur.PrevQuadCpX : cur.Cx;
                  quadCpY = wasQuad ? (2 * cur.Cy) - cur.PrevQuadCpY : cur.Cy;
                  x3 = cur.Cx + (raw[ri] * sx);
                  y3 = cur.Cy + (raw[ri + 1] * sy);
                  ri += 2;
                  AddQuadBezier(path, cur.Cx, cur.Cy, quadCpX, quadCpY, x3, y3);
                  cur.PrevQuadCpX = quadCpX;
                  cur.PrevQuadCpY = quadCpY;
                  cur.Cx = x3;
                  cur.Cy = y3;
               }
               break;

            case 'A':
               {
                  if (!Need(7, avail, cmd))
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
                  AddSvgArc(path, cur.Cx, cur.Cy, arx, ary, phi, fa, fs, x2, y2);
                  cur.Cx = x2;
                  cur.Cy = y2;
               }
               break;
            case 'a':
               {
                  if (!Need(7, avail, cmd))
                  {
                     ri = raw.Length;
                     break;
                  }

                  float arx = Math.Abs(raw[ri] * sx), ary = Math.Abs(raw[ri + 1] * sy);
                  var phi = raw[ri + 2];
                  bool fa = raw[ri + 3] != 0, fs = raw[ri + 4] != 0;
                  x2 = cur.Cx + (raw[ri + 5] * sx);
                  y2 = cur.Cy + (raw[ri + 6] * sy);
                  ri += 7;
                  AddSvgArc(path, cur.Cx, cur.Cy, arx, ary, phi, fa, fs, x2, y2);
                  cur.Cx = x2;
                  cur.Cy = y2;
               }
               break;

            case 'Z':
            case 'z':
               path.CloseFigure();
               cur.Cx = cur.Mx;
               cur.Cy = cur.My;
               ri = raw.Length;
               break;

            default:
               Debug.WriteLine($"[VectorImageFactory] unhandled command '{cmd}'");
               ri = raw.Length;
               break;
         }

         return cmd;
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
         var cursor = new PathCursor(-ox, -oy);

         foreach (var seg in segments)
         foreach (var (cmd, argStr) in Tokenize(seg))
         {
            var raw = ParseArgs(argStr);
            var ri = 0;
            var repeatAs = cmd;
            do
            {
               repeatAs = ApplyCommand(ref cursor, repeatAs, raw, ref ri, path, sx, sy, ox, oy);
               cursor.PrevCmd = repeatAs;
            }
            while (ri < raw.Length);
         }

         return path;
      }

      private static VectorImage Get(Dictionary<(int, int), VectorImage> cache, VectorData data, int w, int h)
      {
         var key = (w, h);
         if (!cache.TryGetValue(key, out var img))
         {
            var path1 = BuildPath(data.ViewBox, data.Primary, w, h);
            var path2 = data.Secondary != null ? BuildPath(data.ViewBox, data.Secondary, w, h) : null;
            img = new VectorImage(path1, path2, w, h);
            cache[key] = img;
         }

         return img;
      }

      /// <summary>Parses SVG number lists, handling whitespace/comma separators AND the compact
      ///    format where a leading '-' or '+' acts as a separator (e.g. "0-1.5" -> 0, -1.5). Also
      ///    handles scientific notation (e.g. "3.6e-4").</summary>
      [SuppressMessage("ReSharper", "CognitiveComplexity")]
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
            while ((i < n) && argStr[i] is ' ' or '\t' or ',')
            {
               i++;
            }

            if (i >= n)
            {
               break;
            }

            var len = ScanNumber(argStr, i, n);
            if (len > 0)
            {
               nums.Add(float.Parse(argStr.Substring(i, len), CultureInfo.InvariantCulture));
               i += len;
            }
            else
            {
               i++; // unrecognized character -- skip to avoid infinite loop
            }
         }

         return nums.ToArray();
      }

      [SuppressMessage("ReSharper", "CognitiveComplexity")]
      private static int ScanNumber(string s, int start, int n)
      {
         var i = start;
         if (s[i] is '-' or '+')
         {
            i++;
         }

         var hasDot = false;
         while (i < n)
         {
            var c = s[i];
            if (c is >= '0' and <= '9')
            {
               i++;
            }
            else if ((c == '.') && !hasDot)
            {
               hasDot = true;
               i++;
            }
            else if (c is 'e' or 'E')
            {
               i++;
               if ((i < n) && s[i] is '-' or '+')
               {
                  i++;
               }

               while ((i < n) && s[i] is >= '0' and <= '9')
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

         return i - start;
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
