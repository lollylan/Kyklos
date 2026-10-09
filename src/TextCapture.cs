using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using SD = System.Drawing;
using SDI = System.Drawing.Imaging;

namespace Kyklos
{
    /// <summary>
    /// „Text erkennen": Bereich auf dem Bildschirm aufziehen, der Text darin landet in der Zwischenablage. Gedacht für
    /// Arztbriefe, die als Fax oder Scan kommen – das Wichtige markieren und in die Karteikarte übernehmen.
    /// </summary>
    public static class TextCapture
    {
        /// <returns>false bei Abbruch oder ohne erkannten Text – ein Makro hält dann an, statt Altes einzufügen.</returns>
        public static async Task<bool> Run(Color hot, Skin skin)
        {
            IntPtr target = Native.GetForegroundWindow();
            // Das Rad blendet gerade aus; auf dem eingefrorenen Bild soll es nicht mehr zu sehen sein.
            await Task.Delay(200);

            var pick = await CaptureWindow.Pick(hot, skin);
            await FillWindow.GiveBack(target);
            if (pick == null) return false;

            var toast = new CaptureToast(hot, skin, pick.Release);
            string text;
            using (pick.Image)
            {
                var job = Ocr.Recognize(pick.Image);
                // Kurze Erkennungen ohne Zwischenstand, sonst flackert die Meldung.
                if (await Task.WhenAny(job, Task.Delay(300)) != job) toast.Busy();
                try { text = await job; }
                catch (Exception ex)
                {
                    Log.Write("Texterkennung fehlgeschlagen: " + ex);
                    toast.Fail("Texterkennung fehlgeschlagen", ex.Message);
                    return false;
                }
            }

            if (text == null)
            {
                toast.Fail("Texterkennung fehlt in Windows",
                           "Einstellungen › Zeit und Sprache › Sprache: bei Deutsch die Sprachoptionen öffnen und die optische Zeichenerkennung hinzufügen.");
                return false;
            }
            if (text.Length == 0)
            {
                toast.Fail("Kein Text erkannt", "Den Ausschnitt größer zoomen und noch einmal markieren.");
                return false;
            }
            // Wie bei den Bausteinen: Zwischenablage-Verlauf und Cloud-Abgleich werden gebeten, Patientendaten nicht zu behalten.
            if (!ClipboardText.Set(text.Replace("\n", "\r\n")))
            {
                toast.Fail("Zwischenablage belegt", "Ein anderes Programm hält sie gerade fest. Bitte noch einmal versuchen.");
                return false;
            }
            int lines = text.Split('\n').Count(l => l.Trim().Length > 0);
            toast.Done(text, lines == 1 ? "1 Zeile" : lines + " Zeilen");
            return true;
        }

        internal static SolidColorBrush Solid(Color c)
        {
            var b = new SolidColorBrush(c);
            b.Freeze();
            return b;
        }

        /// <summary>Gerätefarben wie im Lückentext: Milchglas wird deckend hell, damit Schrift auf jedem Hintergrund trägt.</summary>
        internal static Skin Plate(Skin skin)
        {
            return skin == null ? Skin.Graphit : skin.Decor == Decor.Frost ? Skin.Hell : skin;
        }

        internal static IEasingFunction EaseOut
        {
            get { return new ExponentialEase { EasingMode = EasingMode.EaseOut, Exponent = 5 }; }
        }
    }

    // ==================================================================== Texterkennung

    /// <summary>
    /// Die in Windows 10/11 eingebaute Texterkennung (Windows.Media.Ocr). Läuft vollständig auf dem PC – kein Netz,
    /// kein Dienst, keine mitgelieferten Daten. Angesprochen ohne Hilfsbibliothek, damit der Build kein Windows-SDK braucht.
    /// </summary>
    public static class Ocr
    {
        static Windows.Media.Ocr.OcrEngine _engine;
        static bool _tried;

        /// <summary>Deutsch, wenn installiert – Arztbriefe sind deutsch, auch wenn Windows auf Englisch läuft.</summary>
        static Windows.Media.Ocr.OcrEngine Engine()
        {
            if (_tried) return _engine;
            _tried = true;
            try
            {
                foreach (var lang in Windows.Media.Ocr.OcrEngine.AvailableRecognizerLanguages)
                    if (lang.LanguageTag.StartsWith("de", StringComparison.OrdinalIgnoreCase))
                    {
                        _engine = Windows.Media.Ocr.OcrEngine.TryCreateFromLanguage(lang);
                        if (_engine != null) return _engine;
                    }
                _engine = Windows.Media.Ocr.OcrEngine.TryCreateFromUserProfileLanguages();
            }
            catch (Exception ex) { Log.Write("Texterkennung nicht verfügbar: " + ex.Message); }
            return _engine;
        }

        /// <returns>Erkannter Text mit Zeilenumbrüchen (\n), "" ohne Text, null ohne Erkennung in Windows.</returns>
        public static async Task<string> Recognize(SD.Bitmap region)
        {
            var engine = Engine();
            if (engine == null) return null;
            int max = (int)Windows.Media.Ocr.OcrEngine.MaxImageDimension;
            var img = await Task.Run(() => Prepare(region, max));
            var bmp = Windows.Graphics.Imaging.SoftwareBitmap.CreateCopyFromBuffer(
                Windows.Security.Cryptography.CryptographicBuffer.CreateFromByteArray(img.Pixels),
                Windows.Graphics.Imaging.BitmapPixelFormat.Bgra8, img.Width, img.Height, Windows.Graphics.Imaging.BitmapAlphaMode.Premultiplied);
            var result = await Await(engine.RecognizeAsync(bmp));
            return Format(result);
        }

        static Task<T> Await<T>(Windows.Foundation.IAsyncOperation<T> op)
        {
            var tcs = new TaskCompletionSource<T>();
            op.Completed = (o, status) =>
            {
                if (status == Windows.Foundation.AsyncStatus.Completed) tcs.TrySetResult(o.GetResults());
                else if (status == Windows.Foundation.AsyncStatus.Error) tcs.TrySetException(o.ErrorCode);
                else tcs.TrySetCanceled();
            };
            return tcs.Task;
        }

        /// <summary>
        /// Zeilen in Lesereihenfolge. Ein deutlich größerer Abstand als sonst zwischen den Zeilen wird zur Leerzeile,
        /// damit Absätze und Abschnitte des Briefs erhalten bleiben.
        /// </summary>
        static string Format(Windows.Media.Ocr.OcrResult result)
        {
            var lines = new List<string>();
            var top = new List<double>();
            var bottom = new List<double>();
            foreach (var line in result.Lines)
            {
                string t = line.Text.Trim();
                if (t.Length == 0) continue;
                double y0 = double.MaxValue, y1 = double.MinValue;
                foreach (var w in line.Words)
                {
                    var r = w.BoundingRect;
                    y0 = Math.Min(y0, r.Y);
                    y1 = Math.Max(y1, r.Y + r.Height);
                }
                lines.Add(t);
                top.Add(y0);
                bottom.Add(y1);
            }
            if (lines.Count == 0) return "";

            double height = Median(Enumerable.Range(0, lines.Count).Select(i => bottom[i] - top[i]));
            var gaps = Enumerable.Range(1, lines.Count - 1).Select(i => top[i] - bottom[i - 1]).ToList();
            // Gemessen am üblichen Zeilenabstand, höchstens aber an einem großzügigen – sonst gäbe es bei nur zwei
            // Zeilen keinen Vergleich, und ihr Abstand wäre immer der übliche.
            double usual = gaps.Count > 0 ? Math.Min(Median(gaps), 0.8 * height) : 0;
            var sb = new StringBuilder(lines[0]);
            for (int i = 1; i < lines.Count; i++)
            {
                sb.Append('\n');
                if (gaps[i - 1] > usual + 0.6 * height) sb.Append('\n');
                sb.Append(lines[i]);
            }
            return sb.ToString();
        }

        static double Median(IEnumerable<double> values)
        {
            var v = values.OrderBy(x => x).ToList();
            return v.Count == 0 ? 0 : v[v.Count / 2];
        }

        public sealed class Prepared
        {
            public byte[] Pixels;
            public int Width, Height;
        }

        /// <summary>
        /// Bereitet den Ausschnitt für die Erkennung vor. Bildschirmschrift ist für die Engine klein: doppelt so groß
        /// erkennt sie deutlich besser. Faxe sind oft mit Störpunkten übersät; die werden entfernt – aber nur dann,
        /// denn bei sauberer kleiner Schrift sind einzelne Punkte i-Punkte, Kommas und Doppelpunkte.
        /// </summary>
        public static Prepared Prepare(SD.Bitmap src, int maxDimension)
        {
            int w = src.Width, h = src.Height;
            byte[] px = Pixels(src);

            var dark = new bool[w * h];
            int darkCount = 0;
            for (int i = 0; i < dark.Length; i++)
            {
                int o = i * 4;
                if (px[o] + px[o + 1] + px[o + 2] < 384) { dark[i] = true; darkCount++; }
            }
            var lone = new List<int>();
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    int i = y * w + x;
                    if (!dark[i]) continue;
                    bool alone = true;
                    for (int dy = -1; dy <= 1 && alone; dy++)
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            int xx = x + dx, yy = y + dy;
                            if ((dx != 0 || dy != 0) && xx >= 0 && yy >= 0 && xx < w && yy < h && dark[yy * w + xx]) { alone = false; break; }
                        }
                    if (alone) lone.Add(i);
                }
            // Gemessen: saubere Schrift hat unter 1 % einzelne Punkte, ein verrauschtes Fax über 10 %.
            if (darkCount > 0 && lone.Count > 0.03 * darkCount)
                foreach (int i in lone) { int o = i * 4; px[o] = px[o + 1] = px[o + 2] = 255; }

            // Rand in der Hintergrundfarbe: Schrift, die bis an die Kante reicht, erkennt die Engine schlechter.
            const int pad = 12;
            double k = Math.Min(2.0, (maxDimension - 2 * pad) / (double)Math.Max(w, h));
            int sw = Math.Max(1, (int)Math.Round(w * k)), sh = Math.Max(1, (int)Math.Round(h * k));
            int ow = sw + 2 * pad, oh = sh + 2 * pad;
            using (var clean = FromPixels(px, w, h))
            using (var big = new SD.Bitmap(ow, oh, SDI.PixelFormat.Format32bppArgb))
            {
                using (var g = SD.Graphics.FromImage(big))
                {
                    g.Clear(EdgeColor(px, w, h));
                    g.InterpolationMode = SD.Drawing2D.InterpolationMode.HighQualityBicubic;
                    g.PixelOffsetMode = SD.Drawing2D.PixelOffsetMode.HighQuality;
                    using (var attr = new SDI.ImageAttributes())
                    {
                        attr.SetWrapMode(SD.Drawing2D.WrapMode.TileFlipXY);   // sonst franst der Bildrand beim Vergrößern aus
                        g.DrawImage(clean, new SD.Rectangle(pad, pad, sw, sh), 0, 0, w, h, SD.GraphicsUnit.Pixel, attr);
                    }
                }
                return new Prepared { Pixels = Pixels(big), Width = ow, Height = oh };
            }
        }

        /// <summary>Mittlere Farbe des Bildrands – fast immer das Papier bzw. der Fensterhintergrund.</summary>
        static SD.Color EdgeColor(byte[] px, int w, int h)
        {
            long b = 0, g = 0, r = 0, n = 0;
            Action<int> add = i => { b += px[i * 4]; g += px[i * 4 + 1]; r += px[i * 4 + 2]; n++; };
            for (int x = 0; x < w; x++) { add(x); add((h - 1) * w + x); }
            for (int y = 0; y < h; y++) { add(y * w); add(y * w + w - 1); }
            return SD.Color.FromArgb(255, (int)(r / n), (int)(g / n), (int)(b / n));
        }

        /// <summary>BGRA, Zeile an Zeile, deckend.</summary>
        static byte[] Pixels(SD.Bitmap bmp)
        {
            int w = bmp.Width, h = bmp.Height;
            var data = bmp.LockBits(new SD.Rectangle(0, 0, w, h), SDI.ImageLockMode.ReadOnly, SDI.PixelFormat.Format32bppArgb);
            try
            {
                var px = new byte[w * h * 4];
                for (int y = 0; y < h; y++)
                    System.Runtime.InteropServices.Marshal.Copy(data.Scan0 + y * data.Stride, px, y * w * 4, w * 4);
                for (int i = 3; i < px.Length; i += 4) px[i] = 255;
                return px;
            }
            finally { bmp.UnlockBits(data); }
        }

        static SD.Bitmap FromPixels(byte[] px, int w, int h)
        {
            var bmp = new SD.Bitmap(w, h, SDI.PixelFormat.Format32bppArgb);
            var data = bmp.LockBits(new SD.Rectangle(0, 0, w, h), SDI.ImageLockMode.WriteOnly, SDI.PixelFormat.Format32bppArgb);
            try
            {
                for (int y = 0; y < h; y++)
                    System.Runtime.InteropServices.Marshal.Copy(px, y * w * 4, data.Scan0 + y * data.Stride, w * 4);
            }
            finally { bmp.UnlockBits(data); }
            return bmp;
        }

        /// <summary>Für die Sichtprüfung und das Fenster: GDI-Bild als WPF-Bild.</summary>
        internal static BitmapSource ToSource(SD.Bitmap bmp)
        {
            var src = BitmapSource.Create(bmp.Width, bmp.Height, 96, 96, PixelFormats.Bgra32, null, Pixels(bmp), bmp.Width * 4);
            src.Freeze();
            return src;
        }
    }

    // ==================================================================== Bereich aufziehen

    /// <summary>
    /// Der Bildschirm unter dem Zeiger wird eingefroren und abgedunkelt; im aufgezogenen Rahmen steht er wieder hell.
    /// Erkannt wird auf dem eingefrorenen Bild – was sich währenddessen auf dem Bildschirm bewegt, stört nicht.
    /// </summary>
    sealed class CaptureWindow
    {
        public sealed class Result
        {
            public SD.Bitmap Image;
            public Native.POINT Release;    // physische Pixel – dort erscheint die Rückmeldung
        }

        const double VeilOpacity = 0.55;
        const int MinPixels = 6;

        readonly Window _win;
        readonly Grid _root;
        readonly SD.Bitmap _shot;
        readonly Native.RECT _mon, _work;
        readonly RectangleGeometry _all = new RectangleGeometry(), _hole = new RectangleGeometry();
        readonly Rectangle _frame, _edge;
        readonly Border _hint;
        readonly TaskCompletionSource<Result> _done = new TaskCompletionSource<Result>();
        readonly bool _motion = SystemParameters.ClientAreaAnimation;
        Point? _start;
        Rect _sel;
        bool _hintShown = true, _finished;

        public static Task<Result> Pick(Color hot, Skin skin)
        {
            Native.POINT p;
            Native.GetCursorPos(out p);
            var mi = new Native.MONITORINFO { cbSize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(Native.MONITORINFO)) };
            Native.GetMonitorInfo(Native.MonitorFromPoint(p, 2), ref mi);
            SD.Bitmap shot = Grab(mi.rcMonitor);
            if (shot == null) return Task.FromResult<Result>(null);
            return new CaptureWindow(shot, mi.rcMonitor, mi.rcWork, hot, skin).Run();
        }

        static SD.Bitmap Grab(Native.RECT r)
        {
            int w = r.right - r.left, h = r.bottom - r.top;
            if (w <= 0 || h <= 0) return null;
            var bmp = new SD.Bitmap(w, h, SDI.PixelFormat.Format32bppArgb);
            try
            {
                using (var g = SD.Graphics.FromImage(bmp))
                    g.CopyFromScreen(r.left, r.top, 0, 0, new SD.Size(w, h), SD.CopyPixelOperation.SourceCopy);
                return bmp;
            }
            catch (Exception ex)    // z. B. gesperrter Bildschirm oder sichere Anmeldeoberfläche
            {
                Log.Write("Bildschirm konnte nicht abgegriffen werden: " + ex.Message);
                bmp.Dispose();
                return null;
            }
        }

        CaptureWindow(SD.Bitmap shot, Native.RECT mon, Native.RECT work, Color hot, Skin skin)
        {
            _shot = shot;
            _mon = mon;
            _work = work;
            var sk = TextCapture.Plate(skin);

            _root = new Grid { ClipToBounds = true, Background = Brushes.Black };
            _root.Children.Add(new Image { Source = Ocr.ToSource(shot), Stretch = Stretch.Fill });
            RenderOptions.SetBitmapScalingMode(_root.Children[0], BitmapScalingMode.NearestNeighbor);

            // Der Schleier ist das Gerät selbst: Graphit über dem Bildschirm, ausgespart, wo der Rahmen liegt.
            var veil = new Path
            {
                Fill = TextCapture.Solid(Color.FromArgb((byte)(255 * VeilOpacity), 0x15, 0x16, 0x1A)),
                Data = new CombinedGeometry(GeometryCombineMode.Exclude, _all, _hole)
            };
            _root.Children.Add(veil);

            // Rahmen in der Segmentfarbe, außen eine Graphit-Haarlinie – so bleibt er auf weißem Papier wie auf dunklen
            // Fenstern sichtbar.
            var canvas = new Canvas { IsHitTestVisible = false };
            _edge = new Rectangle { Stroke = TextCapture.Solid(Color.FromRgb(0x15, 0x16, 0x1A)), StrokeThickness = 1, SnapsToDevicePixels = true, Visibility = Visibility.Collapsed };
            _frame = new Rectangle { Stroke = TextCapture.Solid(hot), StrokeThickness = 2, SnapsToDevicePixels = true, Visibility = Visibility.Collapsed };
            canvas.Children.Add(_edge);
            canvas.Children.Add(_frame);
            _root.Children.Add(canvas);

            _hint = BuildHint(hot, sk);
            _root.Children.Add(_hint);

            _win = new Window
            {
                Title = "Kyklos – Text erkennen",
                WindowStyle = WindowStyle.None, ResizeMode = ResizeMode.NoResize, ShowInTaskbar = false, Topmost = true,
                WindowStartupLocation = WindowStartupLocation.Manual, Left = -32000, Top = -32000, Width = 200, Height = 200,
                FontFamily = new FontFamily("Segoe UI"), Cursor = Cursors.Cross, Content = _root
            };
            TextOptions.SetTextFormattingMode(_win, TextFormattingMode.Display);
            _root.SizeChanged += (s, e) =>
            {
                _all.Rect = new Rect(0, 0, _root.ActualWidth, _root.ActualHeight);
                PlaceHint();
            };
            _win.PreviewKeyDown += (s, e) => { if (e.Key == Key.Escape) { e.Handled = true; Finish(null); } };
            _win.MouseLeftButtonDown += OnDown;
            _win.MouseMove += OnMove;
            _win.MouseLeftButtonUp += OnUp;
            _win.MouseRightButtonDown += (s, e) => { e.Handled = true; Finish(null); };
            _win.Deactivated += (s, e) => Finish(null);     // Alt+Tab oder ein anderes Fenster drängt sich vor
            _win.Closed += (s, e) => Finish(null);
        }

        static Border BuildHint(Color hot, Skin sk)
        {
            var row = new StackPanel { Orientation = Orientation.Horizontal };
            var icon = new IconView { Width = 18, Height = 18, VerticalAlignment = VerticalAlignment.Center };
            icon.Set("v:texterkennung", null, TextCapture.Solid(sk.Tint(hot)));
            row.Children.Add(icon);
            row.Children.Add(new TextBlock
            {
                Text = "Rahmen um den Text aufziehen", FontSize = 13, FontWeight = FontWeights.SemiBold, Foreground = sk.BText,
                Margin = new Thickness(10, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center
            });
            row.Children.Add(new Border
            {
                Background = TextCapture.Solid(sk.Key), BorderBrush = TextCapture.Solid(sk.Rim), BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4), Padding = new Thickness(6, 1, 6, 2), Margin = new Thickness(20, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Child = new TextBlock { Text = "Esc", FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = sk.BText }
            });
            row.Children.Add(new TextBlock
            {
                Text = "abbrechen", FontSize = 12, Foreground = sk.BText2, Margin = new Thickness(6, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center
            });
            return new Border
            {
                Background = sk.BChassis, BorderBrush = sk.PRim.Brush, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(10),
                Padding = new Thickness(16, 10, 14, 10), HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top,
                IsHitTestVisible = false, Child = row,
                Effect = new System.Windows.Media.Effects.DropShadowEffect { Color = Colors.Black, Direction = 270, ShadowDepth = 6, BlurRadius = 20, Opacity = 0.45 }
            };
        }

        /// <summary>Oben mittig im Arbeitsbereich des Monitors, also nicht unter einer oben angedockten Taskleiste.</summary>
        void PlaceHint()
        {
            if (_root.ActualWidth <= 0) return;
            double k = _root.ActualWidth / Math.Max(1, _mon.right - _mon.left);     // DIP je Pixel
            _hint.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            double cx = ((_work.left + _work.right) / 2.0 - _mon.left) * k;
            _hint.Margin = new Thickness(Math.Max(0, cx - _hint.DesiredSize.Width / 2), (_work.top - _mon.top) * k + 20, 0, 0);
        }

        Task<Result> Run()
        {
            _win.Show();
            IntPtr hwnd = new WindowInteropHelper(_win).Handle;
            // Zweimal: Wechselt das Fenster auf einen Monitor mit anderer Skalierung, passt Windows die Größe beim ersten Mal an.
            for (int n = 0; n < 2; n++)
                Native.SetWindowPos(hwnd, Native.HWND_TOPMOST, _mon.left, _mon.top, _mon.right - _mon.left, _mon.bottom - _mon.top, Native.SWP_SHOWWINDOW);
            if (_motion)
            {
                // Erst steht der Bildschirm unverändert da, dann legt sich der Schleier darüber – kein Sprung beim Öffnen.
                var veil = _root.Children[1];
                veil.Opacity = 0;
                veil.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(140)) { EasingFunction = TextCapture.EaseOut });
                _hint.Opacity = 0;
                _hint.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(180)) { EasingFunction = TextCapture.EaseOut });
            }
            FillWindow.TakeFocus(_win);
            return _done.Task;
        }

        void OnDown(object sender, MouseButtonEventArgs e)
        {
            _start = e.GetPosition(_root);
            _win.CaptureMouse();
            Update(_start.Value);
        }

        void OnMove(object sender, MouseEventArgs e)
        {
            Point p = e.GetPosition(_root);
            if (_start != null) Update(p);
            // Der Hinweis weicht, sobald er im Weg ist.
            Rect hint = new Rect(_hint.Margin.Left, _hint.Margin.Top, _hint.ActualWidth, _hint.ActualHeight);
            hint.Inflate(32, 32);
            ShowHint(!hint.Contains(p) && !(_start != null && _sel.IntersectsWith(hint)));
        }

        void OnUp(object sender, MouseButtonEventArgs e)
        {
            if (_start == null) return;
            _win.ReleaseMouseCapture();
            Update(e.GetPosition(_root));
            _start = null;

            double k = (_mon.right - _mon.left) / Math.Max(1, _root.ActualWidth);   // Pixel je DIP
            int x0 = Clamp((int)Math.Floor(_sel.Left * k), _shot.Width), y0 = Clamp((int)Math.Floor(_sel.Top * k), _shot.Height);
            int x1 = Clamp((int)Math.Ceiling(_sel.Right * k), _shot.Width), y1 = Clamp((int)Math.Ceiling(_sel.Bottom * k), _shot.Height);
            if (x1 - x0 < MinPixels || y1 - y0 < MinPixels)
            {
                // Nur geklickt: Rahmen verwerfen, weiter warten.
                _hole.Rect = new Rect();
                _frame.Visibility = _edge.Visibility = Visibility.Collapsed;
                return;
            }
            var crop = _shot.Clone(new SD.Rectangle(x0, y0, x1 - x0, y1 - y0), SDI.PixelFormat.Format32bppArgb);
            Native.POINT at;
            Native.GetCursorPos(out at);
            Finish(new Result { Image = crop, Release = at });
        }

        static int Clamp(int v, int max) { return Math.Max(0, Math.Min(max, v)); }

        void Update(Point p)
        {
            var a = _start.Value;
            _sel = new Rect(a, p);
            _hole.Rect = _sel;
            foreach (var r in new[] { _frame, _edge })
            {
                double grow = r == _frame ? 1 : 2.5;    // Farbrahmen außen um den Ausschnitt, Haarlinie darum
                Canvas.SetLeft(r, _sel.Left - grow);
                Canvas.SetTop(r, _sel.Top - grow);
                r.Width = _sel.Width + 2 * grow;
                r.Height = _sel.Height + 2 * grow;
                r.Visibility = Visibility.Visible;
            }
        }

        void ShowHint(bool show)
        {
            if (show == _hintShown) return;
            _hintShown = show;
            double to = show ? 1 : 0;
            if (_motion) _hint.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(to, TimeSpan.FromMilliseconds(120)) { EasingFunction = TextCapture.EaseOut });
            else { _hint.BeginAnimation(UIElement.OpacityProperty, null); _hint.Opacity = to; }
        }

        void Finish(Result r)
        {
            if (_finished) { if (r != null) r.Image.Dispose(); return; }
            _finished = true;
            if (_win.IsVisible) _win.Close();
            _shot.Dispose();
            _done.TrySetResult(r);
        }

        // ---------------------------------------------------------------- Sichtprüfung (--dev-render)

        /// <summary>Baut das Fenster über einem Bild auf, ohne es zu zeigen; mit Rahmen, wenn <paramref name="sel"/> gesetzt ist.</summary>
        internal static Grid DevBuild(SD.Bitmap shot, Color hot, Skin skin, Rect? sel)
        {
            var r = new Native.RECT { right = shot.Width, bottom = shot.Height };
            var c = new CaptureWindow(shot, r, r, hot, skin);
            c._win.Content = null;
            c._root.Width = shot.Width;
            c._root.Height = shot.Height;
            c._all.Rect = new Rect(0, 0, shot.Width, shot.Height);
            if (sel != null)
            {
                c._start = sel.Value.TopLeft;
                c.Update(sel.Value.BottomRight);
                c._hint.Opacity = c._sel.IntersectsWith(new Rect(0, 0, shot.Width, 80)) ? 0 : 1;
            }
            c._root.Measure(new Size(shot.Width, shot.Height));
            c._root.Arrange(new Rect(0, 0, shot.Width, shot.Height));
            c.PlaceHint();
            c._root.UpdateLayout();
            return c._root;
        }
    }

    // ==================================================================== Rückmeldung

    /// <summary>
    /// Kleine Tafel am Zeiger: was in der Zwischenablage liegt, oder warum nichts. Nimmt keinen Fokus, lässt Klicks
    /// durch und verschwindet von selbst – das Zielprogramm bleibt bedienbar.
    /// </summary>
    sealed class CaptureToast
    {
        static CaptureToast _current;

        readonly Window _win;
        readonly Border _plate;
        readonly IconView _icon;
        readonly TextBlock _title, _meta, _body;
        readonly Skin _sk;
        readonly Color _hot;
        readonly Native.POINT _at;
        readonly System.Windows.Threading.DispatcherTimer _timer = new System.Windows.Threading.DispatcherTimer();
        readonly bool _motion = SystemParameters.ClientAreaAnimation;
        bool _shown;

        public CaptureToast(Color hot, Skin skin, Native.POINT at)
        {
            _hot = hot;
            _sk = TextCapture.Plate(skin);
            _at = at;

            _icon = new IconView { Width = 18, Height = 18, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(0, 1, 12, 0) };
            _title = new TextBlock { FontSize = 13, FontWeight = FontWeights.SemiBold, Foreground = _sk.BText, TextTrimming = TextTrimming.CharacterEllipsis };
            _meta = new TextBlock { FontSize = 12, Foreground = _sk.BText2, Margin = new Thickness(16, 1, 0, 0) };
            _body = new TextBlock
            {
                FontSize = 12, LineHeight = 17, LineStackingStrategy = LineStackingStrategy.BlockLineHeight, Foreground = _sk.BText2,
                TextWrapping = TextWrapping.Wrap, TextTrimming = TextTrimming.CharacterEllipsis, Margin = new Thickness(0, 4, 0, 0)
            };
            var head = new DockPanel();
            DockPanel.SetDock(_meta, Dock.Right);
            head.Children.Add(_meta);
            head.Children.Add(_title);
            var text = new StackPanel { Width = 320 };
            text.Children.Add(head);
            text.Children.Add(_body);
            var row = new DockPanel();
            DockPanel.SetDock(_icon, Dock.Left);
            row.Children.Add(_icon);
            row.Children.Add(text);

            _plate = new Border
            {
                Background = _sk.BChassis, BorderBrush = _sk.PRim.Brush, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(10),
                Padding = new Thickness(14, 12, 16, 13), Margin = new Thickness(24, 12, 24, 32), Child = row,
                Effect = new System.Windows.Media.Effects.DropShadowEffect { Color = Colors.Black, Direction = 270, ShadowDepth = 8, BlurRadius = 24, Opacity = 0.45 }
            };
            _win = new Window
            {
                Title = "Kyklos – Texterkennung", WindowStyle = WindowStyle.None, AllowsTransparency = true, Background = Brushes.Transparent,
                ResizeMode = ResizeMode.NoResize, ShowInTaskbar = false, ShowActivated = false, Topmost = true, Focusable = false,
                SizeToContent = SizeToContent.WidthAndHeight, WindowStartupLocation = WindowStartupLocation.Manual, Left = -32000, Top = -32000,
                FontFamily = new FontFamily("Segoe UI"), Content = _plate
            };
            TextOptions.SetTextFormattingMode(_win, TextFormattingMode.Display);
            _win.SourceInitialized += (s, e) =>
            {
                IntPtr h = new WindowInteropHelper(_win).Handle;
                int ex = Native.GetWindowLong(h, Native.GWL_EXSTYLE);
                Native.SetWindowLong(h, Native.GWL_EXSTYLE, ex | Native.WS_EX_NOACTIVATE | Native.WS_EX_TOOLWINDOW | Native.WS_EX_TRANSPARENT);
            };
            _timer.Tick += (s, e) => { _timer.Stop(); Hide(); };
        }

        public void Busy() { Set("v:texterkennung", _sk.BText2, "Text wird erkannt …", "", null, 0); }

        /// <summary>Die ersten Zeilen, wie sie im Brief stehen – zum Gegenlesen, bevor eingefügt wird.</summary>
        public void Done(string text, string meta)
        {
            var lines = text.Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0).ToList();
            string preview = string.Join("\n", lines.Take(3)) + (lines.Count > 3 ? " …" : "");
            Set("v:haken", TextCapture.Solid(_sk.Tint(_hot)), "Text kopiert", meta, preview, 2600, false);
        }

        public void Fail(string title, string help) { Set("v:texterkennung", _sk.BText3, title, "", help, 4500); }

        void Set(string icon, Brush iconBrush, string title, string meta, string body, int ms, bool wrap = true)
        {
            _icon.Set(icon, null, iconBrush);
            _title.Text = title;
            _meta.Text = meta;
            _body.Text = body ?? "";
            _body.TextWrapping = wrap ? TextWrapping.Wrap : TextWrapping.NoWrap;    // Vorschau: eine Briefzeile je Zeile
            _body.Visibility = string.IsNullOrEmpty(body) ? Visibility.Collapsed : Visibility.Visible;
            if (!_shown) Show(); else Place();
            _timer.Stop();
            if (ms > 0) { _timer.Interval = TimeSpan.FromMilliseconds(ms); _timer.Start(); }
        }

        void Show()
        {
            if (_current != null && _current != this) _current.Close();
            _current = this;
            _shown = true;
            _win.Show();
            Place();
            Place();    // ein zweites Mal: auf einem Monitor mit anderer Skalierung ändert sich die Größe
            if (_motion)
                _plate.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(130)) { EasingFunction = TextCapture.EaseOut });
        }

        /// <summary>Rechts unter dem Zeiger, ganz im Arbeitsbereich; wo kein Platz ist, auf der anderen Seite.</summary>
        void Place()
        {
            _win.UpdateLayout();
            IntPtr h = new WindowInteropHelper(_win).Handle;
            if (h == IntPtr.Zero) return;
            var mi = new Native.MONITORINFO { cbSize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(Native.MONITORINFO)) };
            Native.GetMonitorInfo(Native.MonitorFromPoint(_at, 2), ref mi);
            var wa = mi.rcWork;
            Native.RECT r;
            Native.GetWindowRect(h, out r);
            int w = r.right - r.left, ht = r.bottom - r.top;
            double dpi = Native.DpiScale(Native.MonitorFromPoint(_at, 2));
            int gap = (int)(8 * dpi);
            int x = _at.x + gap - (int)(24 * dpi), y = _at.y + gap;     // der Schattenrand des Fensters zählt nicht
            if (x + w > wa.right) x = _at.x - w + (int)(24 * dpi);
            if (y + ht > wa.bottom) y = _at.y - ht - gap + (int)(32 * dpi);
            x = Math.Max(wa.left, Math.Min(wa.right - w, x));
            y = Math.Max(wa.top, Math.Min(wa.bottom - ht, y));
            Native.SetWindowPos(h, Native.HWND_TOPMOST, x, y, 0, 0, Native.SWP_NOSIZE | Native.SWP_NOACTIVATE | Native.SWP_SHOWWINDOW);
        }

        void Hide()
        {
            if (!_motion) { Close(); return; }
            var fade = new DoubleAnimation(0, TimeSpan.FromMilliseconds(220)) { EasingFunction = new ExponentialEase { EasingMode = EasingMode.EaseIn, Exponent = 3 } };
            fade.Completed += (s, e) => Close();
            _plate.BeginAnimation(UIElement.OpacityProperty, fade);
        }

        void Close()
        {
            _timer.Stop();
            if (_current == this) _current = null;
            if (_win.IsVisible) _win.Close();
        }

        // ---------------------------------------------------------------- Sichtprüfung (--dev-render)

        internal static Border DevBuild(Color hot, Skin skin, int state, string text)
        {
            var t = new CaptureToast(hot, skin, new Native.POINT());
            t._shown = true;    // nicht zeigen, nur aufbauen
            if (state == 0) t.Done(text, "4 Zeilen");
            else if (state == 1) t.Fail("Kein Text erkannt", "Den Ausschnitt größer zoomen und noch einmal markieren.");
            else t.Busy();
            t._timer.Stop();
            t._win.Content = null;
            return t._plate;
        }
    }
}
