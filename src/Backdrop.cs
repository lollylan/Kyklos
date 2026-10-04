using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Kyklos
{
    /// <summary>
    /// Hintergrund für das Milchglas: Der Bildschirm unter dem Rad wird beim Öffnen einmal abgegriffen, verkleinert und
    /// weichgezeichnet. Verkleinern erledigt die Grafikkarte beim Kopieren (HALFTONE mittelt), der Rest ist ein
    /// dreifacher Kastenfilter – zusammen ein paar Millisekunden, und das Rad liegt wie hinter Mattglas auf dem Bild.
    /// </summary>
    public static class Backdrop
    {
        public const int Down = 6;          // Verkleinerung beim Abgreifen
        const int Radius = 3, Passes = 3;   // ergibt zusammen mit Down eine Weichzeichnung von gut 30 Bildpunkten

        [StructLayout(LayoutKind.Sequential)]
        struct BITMAPINFOHEADER
        {
            public int biSize, biWidth, biHeight;
            public short biPlanes, biBitCount;
            public int biCompression, biSizeImage, biXPelsPerMeter, biYPelsPerMeter, biClrUsed, biClrImportant;
        }

        [DllImport("user32.dll")] static extern IntPtr GetDC(IntPtr hWnd);
        [DllImport("user32.dll")] static extern int ReleaseDC(IntPtr hWnd, IntPtr hdc);
        [DllImport("gdi32.dll")] static extern IntPtr CreateCompatibleDC(IntPtr hdc);
        [DllImport("gdi32.dll")] static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int w, int h);
        [DllImport("gdi32.dll")] static extern IntPtr SelectObject(IntPtr hdc, IntPtr obj);
        [DllImport("gdi32.dll")] static extern bool DeleteObject(IntPtr obj);
        [DllImport("gdi32.dll")] static extern bool DeleteDC(IntPtr hdc);
        [DllImport("gdi32.dll")] static extern int SetStretchBltMode(IntPtr hdc, int mode);
        [DllImport("gdi32.dll")] static extern bool SetBrushOrgEx(IntPtr hdc, int x, int y, IntPtr old);
        [DllImport("gdi32.dll")]
        static extern bool StretchBlt(IntPtr dst, int x, int y, int w, int h, IntPtr src, int sx, int sy, int sw, int sh, int rop);
        [DllImport("gdi32.dll")]
        static extern int GetDIBits(IntPtr hdc, IntPtr bmp, int start, int lines, byte[] bits, ref BITMAPINFOHEADER bi, int usage);

        const int HALFTONE = 4, SRCCOPY = 0x00CC0020;

        /// <summary>Greift ein Bildschirmrechteck (physische Pixel) ab; null, wenn Windows das nicht zulässt.</summary>
        public static BitmapSource Capture(Native.RECT r)
        {
            int w = r.right - r.left, h = r.bottom - r.top;
            if (w <= 0 || h <= 0) return null;
            int sw = (w + Down - 1) / Down, sh = (h + Down - 1) / Down;
            IntPtr screen = GetDC(IntPtr.Zero), mem = IntPtr.Zero, bmp = IntPtr.Zero, old = IntPtr.Zero;
            try
            {
                mem = CreateCompatibleDC(screen);
                bmp = CreateCompatibleBitmap(screen, sw, sh);
                if (mem == IntPtr.Zero || bmp == IntPtr.Zero) return null;
                old = SelectObject(mem, bmp);
                SetStretchBltMode(mem, HALFTONE);
                SetBrushOrgEx(mem, 0, 0, IntPtr.Zero);
                if (!StretchBlt(mem, 0, 0, sw, sh, screen, r.left, r.top, w, h, SRCCOPY)) return null;
                SelectObject(mem, old);
                old = IntPtr.Zero;
                var bi = new BITMAPINFOHEADER { biSize = Marshal.SizeOf(typeof(BITMAPINFOHEADER)), biWidth = sw, biHeight = -sh, biPlanes = 1, biBitCount = 32 };
                var px = new byte[sw * sh * 4];
                if (GetDIBits(mem, bmp, 0, sh, px, ref bi, 0) == 0) return null;
                for (int i = 3; i < px.Length; i += 4) px[i] = 255;
                return Finish(px, sw, sh);
            }
            finally
            {
                if (old != IntPtr.Zero) SelectObject(mem, old);
                if (bmp != IntPtr.Zero) DeleteObject(bmp);
                if (mem != IntPtr.Zero) DeleteDC(mem);
                ReleaseDC(IntPtr.Zero, screen);
            }
        }

        /// <summary>Dasselbe aus einem fertigen Bild – für die Sichtprüfung ohne echten Bildschirm.</summary>
        public static BitmapSource FromImage(BitmapSource src)
        {
            var small = new TransformedBitmap(src, new ScaleTransform(1.0 / Down, 1.0 / Down));
            var conv = new FormatConvertedBitmap(small, PixelFormats.Bgra32, null, 0);
            int w = conv.PixelWidth, h = conv.PixelHeight;
            var px = new byte[w * h * 4];
            conv.CopyPixels(px, w * 4, 0);
            return Finish(px, w, h);
        }

        static BitmapSource Finish(byte[] px, int w, int h)
        {
            var tmp = new byte[px.Length];
            for (int p = 0; p < Passes; p++)
            {
                Blur(px, tmp, w, h, 4, w * 4);   // waagerecht
                Blur(tmp, px, h, w, w * 4, 4);   // senkrecht
            }
            var bmp = BitmapSource.Create(w, h, 96, 96, PixelFormats.Bgra32, null, px, w * 4);
            bmp.Freeze();
            return bmp;
        }

        /// <summary>Gleitender Mittelwert über 2·Radius+1 Pixel entlang einer Achse, Ränder werden wiederholt.</summary>
        static void Blur(byte[] src, byte[] dst, int len, int lines, int step, int lineStep)
        {
            int win = 2 * Radius + 1;
            for (int l = 0; l < lines; l++)
            {
                int line = l * lineStep;
                for (int c = 0; c < 3; c++)
                {
                    int sum = 0;
                    for (int k = -Radius; k <= Radius; k++) sum += src[line + Math.Max(0, Math.Min(len - 1, k)) * step + c];
                    for (int i = 0; i < len; i++)
                    {
                        dst[line + i * step + c] = (byte)(sum / win);
                        int add = Math.Min(len - 1, i + Radius + 1), sub = Math.Max(0, i - Radius);
                        sum += src[line + add * step + c] - src[line + sub * step + c];
                    }
                }
                for (int i = 0; i < len; i++) dst[line + i * step + 3] = 255;
            }
        }
    }
}
