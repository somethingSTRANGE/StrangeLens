using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Globalization;

namespace Lens
{
   /// <summary>
   ///    Builds and caches <see cref="SvgImage" /> instances from embedded SVG path data.
   ///    One static method per icon; square icons accept a single <c>size</c> parameter,
   ///    non-square icons accept <c>width, height</c>. The factory owns all
   ///    <see cref="System.Drawing.Drawing2D.GraphicsPath" /> objects — callers must not
   ///    dispose the returned <see cref="SvgImage" /> instances.
   /// </summary>
   internal static partial class SvgImageFactory
   {
      // ── Caches ────────────────────────────────────────────────────────────────────────────

      private static readonly Dictionary<(int, int), SvgImage> c_colorPalette = new();
      private static readonly Dictionary<(int, int), SvgImage> c_colorValues = new();
      private static readonly Dictionary<(int, int), SvgImage> c_lensSize = new();
      private static readonly Dictionary<(int, int), SvgImage> c_magnification = new();
      private static readonly Dictionary<(int, int), SvgImage> c_mousePosition = new();
      private static readonly Dictionary<(int, int), SvgImage> c_copy = new();
      private static readonly Dictionary<(int, int), SvgImage> c_donateGitHub = new();
      private static readonly Dictionary<(int, int), SvgImage> c_donatePayPal = new();
      private static readonly Dictionary<(int, int), SvgImage> c_donateKoFi = new();
      private static readonly Dictionary<(int, int), SvgImage> c_donateBuyMeACoffee = new();
      private static readonly Dictionary<(int, int), SvgImage> c_issues = new();
      private static readonly Dictionary<(int, int), SvgImage> c_logo = new();
      private static readonly Dictionary<(int, int), SvgImage> c_logoCode = new();
      private static readonly Dictionary<(int, int), SvgImage> c_website = new();

      // ── Native size properties ────────────────────────────────────────────────────────────

      public static (int Width, int Height) LogoCodeNativeSize => ParseViewBox(Data.LogoCodeImage.ViewBox);

      // ── Public accessors — square icons ───────────────────────────────────────────────────

      public static SvgImage ColorPalette(int size)
      {
         return Get(c_colorPalette, Data.ColorPaletteIcon, size, size);
      }

      public static SvgImage ColorValues(int size)
      {
         return Get(c_colorValues, Data.ColorValuesIcon, size, size);
      }

      public static SvgImage LensSize(int size)
      {
         return Get(c_lensSize, Data.LensSizeIcon, size, size);
      }

      public static SvgImage Magnification(int size)
      {
         return Get(c_magnification, Data.MagnificationIcon, size, size);
      }

      public static SvgImage MousePosition(int size)
      {
         return Get(c_mousePosition, Data.MouseCursorIcon, size, size);
      }

      public static SvgImage Copy(int size)
      {
         return Get(c_copy, Data.CopyIcon, size, size);
      }

      public static SvgImage DonateGitHub(int size)
      {
         return Get(c_donateGitHub, Data.DonateGitHubIcon, size, size);
      }

      public static SvgImage DonateKoFi(int size)
      {
         return Get(c_donateKoFi, Data.DonateKoFiIcon, size, size);
      }

      public static SvgImage DonateBuyMeACoffee(int size)
      {
         return Get(c_donateBuyMeACoffee, Data.DonateBuyMeACoffeeIcon, size, size);
      }

      public static SvgImage DonatePayPal(int size)
      {
         return Get(c_donatePayPal, Data.DonatePayPalIcon, size, size);
      }

      public static SvgImage Issues(int size)
      {
         return Get(c_issues, Data.IssueIcon, size, size);
      }

      public static SvgImage Website(int size)
      {
         return Get(c_website, Data.WebsiteIcon, size, size);
      }

      // ── Public accessors — non-square icons ───────────────────────────────────────────────

      public static SvgImage Logo(int width, int height)
      {
         return Get(c_logo, Data.LogoImage, width, height);
      }

      public static SvgImage LogoCode(int width, int height)
      {
         return Get(c_logoCode, Data.LogoCodeImage, width, height);
      }

      // ── Private helpers ───────────────────────────────────────────────────────────────────

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

      private static (int Width, int Height) ParseViewBox(string viewBox)
      {
         var vb = viewBox.Trim().Split(' ');
         if (vb.Length == 2)
            return (int.Parse(vb[0], CultureInfo.InvariantCulture),
               int.Parse(vb[1], CultureInfo.InvariantCulture));
         if (vb.Length >= 4)
            return (int.Parse(vb[2], CultureInfo.InvariantCulture),
               int.Parse(vb[3], CultureInfo.InvariantCulture));
         return (640, 640);
      }

      private static IEnumerable<(char cmd, string args)> Tokenize(string segment)
      {
         const string Letters = "MmLlHhVvCcSsQqTtAaZz";
         var start = 0;
         var cmd = '\0';
         for (var i = 0; i < segment.Length; i++)
         {
            if (Letters.IndexOf(segment[i]) < 0) continue;
            if (cmd != '\0')
               yield return (cmd, segment.Substring(start, i - start).Trim());
            cmd = segment[i];
            start = i + 1;
         }

         if (cmd != '\0')
            yield return (cmd, segment.Substring(start).Trim());
      }

      // Parses SVG number lists, handling whitespace/comma separators AND the compact
      // format where a leading '-' or '+' acts as a separator (e.g. "0-1.5" → 0, -1.5).
      // Also handles scientific notation (e.g. "3.6e-4").
      private static float[] ParseArgs(string argStr)
      {
         if (argStr.Length == 0) return Array.Empty<float>();
         var nums = new List<float>();
         int i = 0, n = argStr.Length;
         while (i < n)
         {
            while (i < n && (argStr[i] == ' ' || argStr[i] == '\t' || argStr[i] == ',')) i++;
            if (i >= n) break;
            var start = i;
            if (argStr[i] == '-' || argStr[i] == '+') i++;
            var hasDot = false;
            while (i < n)
            {
               var c = argStr[i];
               if (c >= '0' && c <= '9')
               {
                  i++;
               }
               else if (c == '.' && !hasDot)
               {
                  hasDot = true;
                  i++;
               }
               else if (c == 'e' || c == 'E')
               {
                  i++;
                  if (i < n && (argStr[i] == '-' || argStr[i] == '+')) i++;
                  while (i < n && argStr[i] >= '0' && argStr[i] <= '9') i++;
                  break;
               }
               else
               {
                  break;
               }
            }

            if (i > start)
               nums.Add(float.Parse(argStr.Substring(start, i - start), CultureInfo.InvariantCulture));
            else
               i++; // unrecognized character — skip to avoid infinite loop
         }

         return nums.ToArray();
      }

      /// <summary>
      ///    Builds a <see cref="GraphicsPath" /> scaled so the viewBox fills exactly
      ///    <paramref name="width" /> × <paramref name="height" /> pixels (exact fill,
      ///    no letterboxing). Absolute coords use separate scaleX/scaleY; relative
      ///    coords use the same per-axis scale.
      /// </summary>
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

         var sx = width / viewW; // scaleX: SVG unit → screen pixel on X axis
         var sy = height / viewH; // scaleY: SVG unit → screen pixel on Y axis
         var ox = minX * sx; // scaled origin offset X
         var oy = minY * sy; // scaled origin offset Y

         var path = new GraphicsPath(FillMode.Winding);
         float cx = -ox, cy = -oy;
         float mx = cx, my = cy;

         char  prevCmd  = '\0';
         float prevCp2x = 0, prevCp2y = 0; // 2nd control point of last C/c/S/s (for S/s reflection)
         float prevQcpx = 0, prevQcpy = 0; // control point of last Q/q/T/t (for T/t reflection)

         foreach (var seg in segments)
         foreach (var (cmd, argStr) in Tokenize(seg))
         {
            var raw      = ParseArgs(argStr);
            int ri       = 0;
            var repeatAs = cmd; // M→L and m→l after first pair; all others stay constant

            do
            {
               // Validate enough args remain before consuming.
               static bool Need(int n, int have, char c)
               {
                  if (have >= n) return true;
                  Debug.WriteLine($"[SvgImageFactory] '{c}': need {n} args, have {have}");
                  return false;
               }

               float tx, ty, x1, y1, x2, y2, x3, y3, dx, dy, qcpx, qcpy;
               int   avail = raw.Length - ri;

               switch (repeatAs)
               {
                  case 'M':
                     if (!Need(2, avail, repeatAs)) { ri = raw.Length; break; }
                     cx = mx = raw[ri]*sx-ox; cy = my = raw[ri+1]*sy-oy; ri+=2;
                     path.StartFigure();
                     repeatAs = 'L';
                     break;
                  case 'm':
                     if (!Need(2, avail, repeatAs)) { ri = raw.Length; break; }
                     cx = mx = cx+raw[ri]*sx; cy = my = cy+raw[ri+1]*sy; ri+=2;
                     path.StartFigure();
                     repeatAs = 'l';
                     break;

                  case 'L':
                     if (!Need(2, avail, repeatAs)) { ri = raw.Length; break; }
                     tx = raw[ri]*sx-ox; ty = raw[ri+1]*sy-oy; ri+=2;
                     path.AddLine(cx, cy, tx, ty);
                     cx = tx; cy = ty;
                     break;
                  case 'l':
                     if (!Need(2, avail, repeatAs)) { ri = raw.Length; break; }
                     dx = raw[ri]*sx; dy = raw[ri+1]*sy; ri+=2;
                     path.AddLine(cx, cy, cx+dx, cy+dy);
                     cx+=dx; cy+=dy;
                     break;

                  case 'H':
                     if (!Need(1, avail, repeatAs)) { ri = raw.Length; break; }
                     tx = raw[ri]*sx-ox; ri++;
                     path.AddLine(cx, cy, tx, cy);
                     cx = tx;
                     break;
                  case 'h':
                     if (!Need(1, avail, repeatAs)) { ri = raw.Length; break; }
                     dx = raw[ri]*sx; ri++;
                     path.AddLine(cx, cy, cx+dx, cy);
                     cx+=dx;
                     break;

                  case 'V':
                     if (!Need(1, avail, repeatAs)) { ri = raw.Length; break; }
                     ty = raw[ri]*sy-oy; ri++;
                     path.AddLine(cx, cy, cx, ty);
                     cy = ty;
                     break;
                  case 'v':
                     if (!Need(1, avail, repeatAs)) { ri = raw.Length; break; }
                     dy = raw[ri]*sy; ri++;
                     path.AddLine(cx, cy, cx, cy+dy);
                     cy+=dy;
                     break;

                  case 'C':
                     if (!Need(6, avail, repeatAs)) { ri = raw.Length; break; }
                     x1=raw[ri]*sx-ox;   y1=raw[ri+1]*sy-oy;
                     x2=raw[ri+2]*sx-ox; y2=raw[ri+3]*sy-oy;
                     x3=raw[ri+4]*sx-ox; y3=raw[ri+5]*sy-oy; ri+=6;
                     path.AddBezier(cx, cy, x1, y1, x2, y2, x3, y3);
                     prevCp2x=x2; prevCp2y=y2; cx=x3; cy=y3;
                     break;
                  case 'c':
                     if (!Need(6, avail, repeatAs)) { ri = raw.Length; break; }
                     x1=cx+raw[ri]*sx;   y1=cy+raw[ri+1]*sy;
                     x2=cx+raw[ri+2]*sx; y2=cy+raw[ri+3]*sy;
                     x3=cx+raw[ri+4]*sx; y3=cy+raw[ri+5]*sy; ri+=6;
                     path.AddBezier(cx, cy, x1, y1, x2, y2, x3, y3);
                     prevCp2x=x2; prevCp2y=y2; cx=x3; cy=y3;
                     break;

                  case 'S':
                  {
                     if (!Need(4, avail, repeatAs)) { ri = raw.Length; break; }
                     bool wasCubic = prevCmd=='C'||prevCmd=='c'||prevCmd=='S'||prevCmd=='s';
                     x1 = wasCubic ? 2*cx-prevCp2x : cx;
                     y1 = wasCubic ? 2*cy-prevCp2y : cy;
                     x2=raw[ri]*sx-ox;   y2=raw[ri+1]*sy-oy;
                     x3=raw[ri+2]*sx-ox; y3=raw[ri+3]*sy-oy; ri+=4;
                     path.AddBezier(cx, cy, x1, y1, x2, y2, x3, y3);
                     prevCp2x=x2; prevCp2y=y2; cx=x3; cy=y3;
                  } break;
                  case 's':
                  {
                     if (!Need(4, avail, repeatAs)) { ri = raw.Length; break; }
                     bool wasCubic = prevCmd=='C'||prevCmd=='c'||prevCmd=='S'||prevCmd=='s';
                     x1 = wasCubic ? 2*cx-prevCp2x : cx;
                     y1 = wasCubic ? 2*cy-prevCp2y : cy;
                     x2=cx+raw[ri]*sx;   y2=cy+raw[ri+1]*sy;
                     x3=cx+raw[ri+2]*sx; y3=cy+raw[ri+3]*sy; ri+=4;
                     path.AddBezier(cx, cy, x1, y1, x2, y2, x3, y3);
                     prevCp2x=x2; prevCp2y=y2; cx=x3; cy=y3;
                  } break;

                  case 'Q':
                     if (!Need(4, avail, repeatAs)) { ri = raw.Length; break; }
                     qcpx=raw[ri]*sx-ox;   qcpy=raw[ri+1]*sy-oy;
                     x3=raw[ri+2]*sx-ox;   y3=raw[ri+3]*sy-oy; ri+=4;
                     x1=cx+2f/3*(qcpx-cx); y1=cy+2f/3*(qcpy-cy);
                     x2=x3+2f/3*(qcpx-x3); y2=y3+2f/3*(qcpy-y3);
                     path.AddBezier(cx, cy, x1, y1, x2, y2, x3, y3);
                     prevQcpx=qcpx; prevQcpy=qcpy; cx=x3; cy=y3;
                     break;
                  case 'q':
                     if (!Need(4, avail, repeatAs)) { ri = raw.Length; break; }
                     qcpx=cx+raw[ri]*sx;   qcpy=cy+raw[ri+1]*sy;
                     x3=cx+raw[ri+2]*sx;   y3=cy+raw[ri+3]*sy; ri+=4;
                     x1=cx+2f/3*(qcpx-cx); y1=cy+2f/3*(qcpy-cy);
                     x2=x3+2f/3*(qcpx-x3); y2=y3+2f/3*(qcpy-y3);
                     path.AddBezier(cx, cy, x1, y1, x2, y2, x3, y3);
                     prevQcpx=qcpx; prevQcpy=qcpy; cx=x3; cy=y3;
                     break;

                  case 'T':
                  {
                     if (!Need(2, avail, repeatAs)) { ri = raw.Length; break; }
                     bool wasQuad = prevCmd=='Q'||prevCmd=='q'||prevCmd=='T'||prevCmd=='t';
                     qcpx = wasQuad ? 2*cx-prevQcpx : cx;
                     qcpy = wasQuad ? 2*cy-prevQcpy : cy;
                     x3=raw[ri]*sx-ox; y3=raw[ri+1]*sy-oy; ri+=2;
                     x1=cx+2f/3*(qcpx-cx); y1=cy+2f/3*(qcpy-cy);
                     x2=x3+2f/3*(qcpx-x3); y2=y3+2f/3*(qcpy-y3);
                     path.AddBezier(cx, cy, x1, y1, x2, y2, x3, y3);
                     prevQcpx=qcpx; prevQcpy=qcpy; cx=x3; cy=y3;
                  } break;
                  case 't':
                  {
                     if (!Need(2, avail, repeatAs)) { ri = raw.Length; break; }
                     bool wasQuad = prevCmd=='Q'||prevCmd=='q'||prevCmd=='T'||prevCmd=='t';
                     qcpx = wasQuad ? 2*cx-prevQcpx : cx;
                     qcpy = wasQuad ? 2*cy-prevQcpy : cy;
                     x3=cx+raw[ri]*sx; y3=cy+raw[ri+1]*sy; ri+=2;
                     x1=cx+2f/3*(qcpx-cx); y1=cy+2f/3*(qcpy-cy);
                     x2=x3+2f/3*(qcpx-x3); y2=y3+2f/3*(qcpy-y3);
                     path.AddBezier(cx, cy, x1, y1, x2, y2, x3, y3);
                     prevQcpx=qcpx; prevQcpy=qcpy; cx=x3; cy=y3;
                  } break;

                  case 'A':
                  {
                     if (!Need(7, avail, repeatAs)) { ri = raw.Length; break; }
                     float arx = Math.Abs(raw[ri]*sx), ary = Math.Abs(raw[ri+1]*sy);
                     float phi = raw[ri+2];
                     bool fa = raw[ri+3] != 0, fs = raw[ri+4] != 0;
                     x2 = raw[ri+5]*sx-ox; y2 = raw[ri+6]*sy-oy; ri+=7;
                     AddSvgArc(path, cx, cy, arx, ary, phi, fa, fs, x2, y2);
                     cx = x2; cy = y2;
                  } break;
                  case 'a':
                  {
                     if (!Need(7, avail, repeatAs)) { ri = raw.Length; break; }
                     float arx = Math.Abs(raw[ri]*sx), ary = Math.Abs(raw[ri+1]*sy);
                     float phi = raw[ri+2];
                     bool fa = raw[ri+3] != 0, fs = raw[ri+4] != 0;
                     x2 = cx+raw[ri+5]*sx; y2 = cy+raw[ri+6]*sy; ri+=7;
                     AddSvgArc(path, cx, cy, arx, ary, phi, fa, fs, x2, y2);
                     cx = x2; cy = y2;
                  } break;

                  case 'Z':
                  case 'z':
                     path.CloseFigure();
                     cx = mx; cy = my;
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

      // Converts SVG endpoint arc parameterisation to a GDI+ AddArc call.
      // Implements the algorithm from SVG 1.1 spec appendix B (F.6).
      private static void AddSvgArc(GraphicsPath path,
         float x1, float y1, float rx, float ry, float phiDeg,
         bool fa, bool fs, float x2, float y2)
      {
         if (x1 == x2 && y1 == y2) return;
         if (rx == 0 || ry == 0) { path.AddLine(x1, y1, x2, y2); return; }

         double phi = phiDeg * Math.PI / 180;
         double cosP = Math.Cos(phi), sinP = Math.Sin(phi);

         double dx = (x1 - x2) / 2.0, dy = (y1 - y2) / 2.0;
         double x1p =  cosP*dx + sinP*dy;
         double y1p = -sinP*dx + cosP*dy;

         // Ensure radii are large enough.
         double lambda = x1p*x1p/(rx*(double)rx) + y1p*y1p/(ry*(double)ry);
         if (lambda > 1) { double s = Math.Sqrt(lambda); rx = (float)(s*rx); ry = (float)(s*ry); }

         double rxq = (double)rx*rx, ryq = (double)ry*ry;
         double x1pq = x1p*x1p, y1pq = y1p*y1p;
         double num = Math.Max(0, rxq*ryq - rxq*y1pq - ryq*x1pq);
         double den = rxq*y1pq + ryq*x1pq;
         double sq  = (fa == fs ? -1 : 1) * Math.Sqrt(den == 0 ? 0 : num / den);
         double cxp =  sq * rx * y1p / ry;
         double cyp = -sq * ry * x1p / rx;

         double cx = cosP*cxp - sinP*cyp + (x1 + x2) / 2.0;
         double cy = sinP*cxp + cosP*cyp + (y1 + y2) / 2.0;

         double ux = (x1p - cxp) / rx, uy = (y1p - cyp) / ry;
         double vx = (-x1p - cxp) / rx, vy = (-y1p - cyp) / ry;

         double startAngle = SvgAngle(1, 0, ux, uy);
         double sweepAngle = SvgAngle(ux, uy, vx, vy);
         if (!fs && sweepAngle > 0) sweepAngle -= 360;
         if ( fs && sweepAngle < 0) sweepAngle += 360;

         if (phiDeg != 0)
         {
            // Rotated ellipse: apply transform around centre, draw, restore.
            var state = path.GetLastPoint(); // dummy — handled via Graphics transform at render time
            Debug.WriteLine($"[SvgImageFactory] rotated arc (phi={phiDeg}) not fully supported");
         }

         path.AddArc((float)(cx-rx), (float)(cy-ry), rx*2, ry*2,
                     (float)startAngle, (float)sweepAngle);
      }

      private static double SvgAngle(double ux, double uy, double vx, double vy)
      {
         double dot = ux*vx + uy*vy;
         double len = Math.Sqrt(ux*ux + uy*uy) * Math.Sqrt(vx*vx + vy*vy);
         double a   = Math.Acos(Math.Max(-1, Math.Min(1, len == 0 ? 0 : dot / len))) * 180 / Math.PI;
         return ux*vy - uy*vx < 0 ? -a : a;
      }
   }
}