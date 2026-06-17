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

      public static (int Width, int Height) LogoCodeNativeSize => ParseViewBox(Data.LogoCodeImage);

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
         return Get(c_mousePosition, Data.MousePositionIcon, size, size);
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

      private static SvgImage Get(Dictionary<(int, int), SvgImage> cache, string[] data, int w, int h)
      {
         var key = (w, h);
         if (!cache.TryGetValue(key, out var img))
         {
            img = new SvgImage(Build(data, w, h), w, h);
            cache[key] = img;
         }

         return img;
      }

      private static (int Width, int Height) ParseViewBox(string[] data)
      {
         if (data.Length > 0 && !HasCommand(data[0]))
         {
            var vb = data[0].Trim().Split(' ');
            if (vb.Length == 2)
               return (int.Parse(vb[0], CultureInfo.InvariantCulture),
                  int.Parse(vb[1], CultureInfo.InvariantCulture));
            if (vb.Length >= 4)
               return (int.Parse(vb[2], CultureInfo.InvariantCulture),
                  int.Parse(vb[3], CultureInfo.InvariantCulture));
         }

         return (640, 640);
      }

      private static bool HasCommand(string s)
      {
         foreach (var c in s)
            if ("MmLlHhVvCcSsQqTtZz".IndexOf(c) >= 0)
               return true;
         return false;
      }

      private static IEnumerable<(char cmd, string args)> Tokenize(string segment)
      {
         const string Letters = "MmLlHhVvCcSsQqTtZz";
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
         }

         return nums.ToArray();
      }

      /// <summary>
      ///    Builds a <see cref="GraphicsPath" /> scaled so the viewBox fills exactly
      ///    <paramref name="width" /> × <paramref name="height" /> pixels (exact fill,
      ///    no letterboxing). Absolute coords use separate scaleX/scaleY; relative
      ///    coords use the same per-axis scale.
      /// </summary>
      private static GraphicsPath Build(string[] data, int width, int height)
      {
         float minX = 0, minY = 0, viewW = 640, viewH = 640;
         if (data.Length > 0 && !HasCommand(data[0]))
         {
            var vb = data[0].Trim().Split(' ');
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

         foreach (var seg in data)
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
   }
}