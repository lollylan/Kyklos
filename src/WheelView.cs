using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Kyklos
{
    /// <summary>
    /// Zeichnet das Rad: Scheibe, keilförmige Tasten mit gleich breiten Fugen, Display in der Nabe – in den Farben und mit
    /// dem Schmuck des gewählten Aussehens (<see cref="Skin"/>).
    /// Dieselbe Ansicht dient als Overlay und als anklickbare Vorschau in den Einstellungen.
    /// Alle Maße sind geräteunabhängige Pixel bei Skalierung 1, Ursprung ist die Radmitte.
    /// </summary>
    public sealed class WheelView : FrameworkElement
    {
        public const double HubRadius = 84, InnerRadius = 93, RimWidth = 6, KeyGap = 5, KeyCorner = 8, LiftPx = 4;
        // ShadowPad: Platz für den Schatten im Overlay-Fenster. PreviewPad: Rand der Vorschau in den Einstellungen –
        // dort darf der Schatten über den Rand der Ansicht hinauslaufen.
        public const double ShadowPad = 84, PreviewPad = 46, MaxOuter = 258, HalfExtent = MaxOuter + ShadowPad;
        public const double DeadZone = 34;

        public static double OuterFor(int n)
        {
            return n <= 4 ? 200 : n <= 6 ? 208 : n <= 8 ? 222 : n <= 10 ? 240 : MaxOuter;
        }

        static Color C(string hex) { return (Color)ColorConverter.ConvertFromString(hex); }
        static SolidColorBrush B(Color c) { var b = new SolidColorBrush(c); b.Freeze(); return b; }
        static Pen P(Color c, double w)
        {
            var p = new Pen(B(c), w) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round, LineJoin = PenLineJoin.Round };
            p.Freeze();
            return p;
        }

        static readonly Pen PFrostEdge = P(Color.FromArgb(0x33, 0, 0, 0), 1);
        static readonly FontFamily Ui = new FontFamily("Segoe UI");
        static readonly Typeface Regular = new Typeface(Ui, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
        static readonly Typeface Semibold = new Typeface(Ui, FontStyles.Normal, FontWeights.SemiBold, FontStretches.Normal);

        List<Slot> _slots = new List<Slot>();
        string _title = "", _hint = "";
        Geometry[] _geo = new Geometry[0];
        double[] _glow = new double[0];
        FormattedText[,] _labels = new FormattedText[0, 2];
        double[] _labelW = new double[0];
        FormattedText _hubTitle, _hubDesc;
        int _hubFor = int.MinValue;
        double _outer = 222, _ppd = 1;
        Brush _contact, _ambient;
        Pen _sheen;
        Skin _skin = Skin.Graphit;

        // Schmuck: Uhr, Blick des Auges, Lider, Schleier über dem Auge, solange das Display Text zeigt.
        static readonly Random Rnd = new Random();
        double _time, _sinceOpen = 10, _veil, _blinkAt = -1, _nextBlink = 3;
        Vector _gaze;

        public int Hover = -1;          // Segment unter dem Zeiger
        public int Selected = -1;       // gewähltes Segment (nur Einstellungen)
        public int Flash = -1;          // ausgelöstes Segment, bleibt beim Ausblenden erleuchtet
        public bool EditMode;
        public bool Motion = true;      // false, wenn Windows-Animationen abgeschaltet sind: kein Hub, kein Einblenden
        public bool PointerOn;
        public double PointerAngle;
        public double Scale = 1;
        public double Open = 1;         // 0..1, Einblendung
        public double Fade = 1;         // 1..0, Ausblendung
        public Vector GazeTarget;       // Zeiger relativ zur Radmitte (DIP bei Skalierung 1) – dorthin schaut das Auge
        public BitmapSource Backdrop;   // Milchglas: weichgezeichneter Bildschirm hinter dem Fenster
        public Rect BackdropRect;       // wo dieses Bild in der Ansicht liegt
        public bool Deeper;             // Unterrad: wächst stärker aus der Ordner-Taste heraus
        public double Recede;           // 0..1: Wechsel ins Unterrad – dieses Rad tritt zurück und blendet aus

        public const double RecedeSeconds = 0.16;

        public WheelView() { RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.HighQuality); }

        public Skin Skin
        {
            get { return _skin; }
            set
            {
                _skin = value ?? Skin.Graphit;
                Shadows();
                Refresh();
            }
        }

        public int Count { get { return _slots.Count; } }
        public double Outer { get { return _outer; } }

        public void SetContent(List<Slot> slots, string title, string hint)
        {
            _slots = slots ?? new List<Slot>();
            _title = title ?? "";
            _hint = hint ?? "";
            int n = _slots.Count;
            _outer = OuterFor(n);
            _geo = new Geometry[n];
            _glow = new double[n];
            double step = 2 * Math.PI / Math.Max(1, n);
            for (int i = 0; i < n; i++)
            {
                double mid = -Math.PI / 2 + i * step;
                _geo[i] = BuildKey(InnerRadius, _outer - RimWidth, mid - step / 2, mid + step / 2, KeyGap, KeyCorner);
            }
            Shadows();
            Refresh();
        }

        void Shadows()
        {
            _contact = Falloff(_outer - 6, _outer + 10, _skin.Contact, 1.6);
            _ambient = Falloff(_outer - 30, _outer + 60, _skin.Ambient, 2.0);
            if (_skin.Decor == Decor.Frost && _sheen == null)
            {
                // Lichtkante des Glases: oben hell, zu den Seiten auslaufend.
                var g = new LinearGradientBrush { StartPoint = new Point(0.5, 0), EndPoint = new Point(0.5, 1) };
                g.GradientStops.Add(new GradientStop(Color.FromArgb(0xF0, 255, 255, 255), 0));
                g.GradientStops.Add(new GradientStop(Color.FromArgb(0x00, 255, 255, 255), 0.55));
                g.Freeze();
                _sheen = new Pen(g, 1.5);
                _sheen.Freeze();
            }
        }

        /// <summary>Beim Erscheinen des Rads: Das Auge schlägt sich auf, die Spinne pendelt neu aus.</summary>
        public void ResetDecor()
        {
            _sinceOpen = Motion ? 0 : 10;
            _gaze = GazeTarget;
            _veil = 0;
            _blinkAt = -1;
            _nextBlink = _time + 2.5 + Rnd.NextDouble() * 3;
        }

        /// <summary>Nach Änderungen an Beschriftung, Symbol oder Aktion: Textlayouts verwerfen und neu zeichnen.</summary>
        public void Refresh()
        {
            _labels = new FormattedText[_slots.Count, 2];
            _labelW = new double[_slots.Count];
            for (int i = 0; i < _labelW.Length; i++) _labelW[i] = double.NaN;
            _hubFor = int.MinValue;
            InvalidateVisual();
        }

        /// <summary>Führt die Leucht-Übergänge nach; liefert true, solange sich noch etwas bewegt.</summary>
        public bool Tick(double dt)
        {
            bool moving = false;
            for (int i = 0; i < _glow.Length; i++)
            {
                double target = (i == Hover && !_slots[i].IsEmpty) || i == Flash ? 1 : 0;
                double g = _glow[i];
                if (g == target) continue;
                g += (target - g) * (1 - Math.Exp(-dt / (target > g ? 0.03 : 0.085)));
                if (Math.Abs(target - g) < 0.012) g = target;
                _glow[i] = g;
                moving = true;
            }
            if (_skin.Animated)
            {
                _time += dt;
                _sinceOpen += dt;
                double veil = Hover >= 0 || Flash >= 0 ? 1 : 0;
                if (Motion)
                {
                    _gaze += (GazeTarget - _gaze) * (1 - Math.Exp(-dt / 0.06));
                    _veil += (veil - _veil) * (1 - Math.Exp(-dt / 0.05));
                    if (_blinkAt < 0 && _time > _nextBlink && _veil < 0.5) _blinkAt = _time;
                    if (_blinkAt >= 0 && _time - _blinkAt > SkinDecor.BlinkSeconds)
                    {
                        _blinkAt = -1;
                        _nextBlink = _time + 2.5 + Rnd.NextDouble() * 4.5;
                    }
                    moving = true;
                }
                else
                {
                    moving |= _gaze != GazeTarget || _veil != veil;
                    _gaze = GazeTarget;
                    _veil = veil;
                }
            }
            return moving;
        }

        double Closure()
        {
            if (!Motion) return 0;
            double c = 1 - EaseOut(Math.Min(1, _sinceOpen / SkinDecor.OpenEyeSeconds));
            if (_blinkAt >= 0) c = Math.Max(c, Math.Sin(Math.PI * Math.Min(1, (_time - _blinkAt) / SkinDecor.BlinkSeconds)));
            return c;
        }

        public static int IndexFromAngle(double angle, int n)
        {
            double step = 2 * Math.PI / n;
            int i = (int)Math.Round((angle + Math.PI / 2) / step);
            return ((i % n) + n) % n;
        }

        /// <summary>Segment an einem Punkt der Ansicht; -1 = Nabe, -2 = außerhalb.</summary>
        public int HitTest(Point p)
        {
            int n = _slots.Count;
            if (n == 0) return -2;
            double dx = (p.X - ActualWidth / 2) / Scale, dy = (p.Y - ActualHeight / 2) / Scale;
            double d = Math.Sqrt(dx * dx + dy * dy);
            if (d < InnerRadius - 4) return -1;
            if (d > _outer + LiftPx) return -2;
            return IndexFromAngle(Math.Atan2(dy, dx), n);
        }

        static Point Polar(double r, double a) { return new Point(Math.Cos(a) * r, Math.Sin(a) * r); }

        /// <summary>
        /// Ringsegment mit runden Ecken. Die Seitenkanten laufen parallel zur Trennlinie, dadurch ist die Fuge
        /// innen wie außen gleich breit. Rundung: erst um den Radius verkleinern, dann mit rundem Stift aufweiten.
        /// </summary>
        public static Geometry BuildKey(double rIn, double rOut, double a0, double a1, double gap, double corner)
        {
            double r1 = rIn + corner, r2 = rOut - corner, h = gap / 2 + corner;
            double o1 = Math.Asin(h / r1), o2 = Math.Asin(h / r2);
            Point p1 = Polar(r2, a0 + o2), p2 = Polar(r2, a1 - o2), p3 = Polar(r1, a1 - o1), p4 = Polar(r1, a0 + o1);
            var sg = new StreamGeometry();
            using (var c = sg.Open())
            {
                c.BeginFigure(p1, true, true);
                c.ArcTo(p2, new Size(r2, r2), 0, (a1 - o2) - (a0 + o2) > Math.PI, SweepDirection.Clockwise, true, true);
                c.LineTo(p3, true, true);
                c.ArcTo(p4, new Size(r1, r1), 0, (a1 - o1) - (a0 + o1) > Math.PI, SweepDirection.Counterclockwise, true, true);
            }
            sg.Freeze();
            var pen = new Pen(Brushes.Black, corner * 2) { LineJoin = PenLineJoin.Round };
            var g = Geometry.Combine(sg, sg.GetWidenedPathGeometry(pen), GeometryCombineMode.Union, null);
            g.Freeze();
            return g;
        }

        /// <summary>
        /// Schattenschicht: bis <paramref name="inner"/> volle Deckkraft, dann in acht Stufen weich auslaufend
        /// (Potenzkurve statt linearer Rampe – eine lineare Rampe endet mit sichtbarer Kante).
        /// </summary>
        static Brush Falloff(double inner, double radius, byte alpha, double power)
        {
            var b = new RadialGradientBrush();
            b.GradientStops.Add(new GradientStop(Color.FromArgb(alpha, 0, 0, 0), 0));
            for (int i = 0; i <= 8; i++)
            {
                double t = i / 8.0;
                byte a = (byte)Math.Round(alpha * Math.Pow(1 - t, power));
                b.GradientStops.Add(new GradientStop(Color.FromArgb(a, 0, 0, 0), (inner + (radius - inner) * t) / radius));
            }
            b.Freeze();
            return b;
        }

        static Color Lerp(Color a, Color b, double t)
        {
            return Color.FromArgb((byte)(a.A + (b.A - a.A) * t), (byte)(a.R + (b.R - a.R) * t), (byte)(a.G + (b.G - a.G) * t),
                                  (byte)(a.B + (b.B - a.B) * t));
        }

        static double EaseOut(double t) { return t >= 1 ? 1 : 1 - Math.Pow(2, -10 * t); }

        FormattedText Text(string s, Typeface face, double size, Brush brush, double maxWidth, int maxLines)
        {
            var ft = new FormattedText(s, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, face, size, brush, null,
                                       TextFormattingMode.Ideal, _ppd);
            ft.MaxTextWidth = maxWidth;
            ft.MaxLineCount = maxLines;
            ft.Trimming = TextTrimming.CharacterEllipsis;
            ft.TextAlignment = TextAlignment.Center;
            return ft;
        }

        protected override void OnRender(DrawingContext dc)
        {
            int n = _slots.Count;
            if (n == 0) return;
            _ppd = VisualTreeHelper.GetDpi(this).PixelsPerDip;
            // Das Unterrad wächst gleichmäßiger heran (kubisch) als das erste Rad, das sofort dastehen soll.
            double e = Deeper ? 1 - Math.Pow(1 - Math.Min(1, Open), 3) : EaseOut(Open);
            // Das Unterrad ist schon nach einem Fünftel des Wachsens deckend und legt sich als neue Ebene über das
            // zurücktretende alte – zwei halb durchsichtige Räder übereinander wären unruhig.
            double show = Deeper ? Math.Min(1, Open / 0.2) : e;
            // Zurücktreten: Größe bremst weich ab (kubisch), die Deckkraft hält erst und fällt dann (Smoothstep) – so bleibt
            // das alte Rad lange genug sichtbar, dass das Auge dem Wechsel folgen kann.
            double rec = Math.Max(0, Math.Min(1, Recede));
            double alpha = Math.Max(0, Math.Min(1, show * Fade * (1 - rec * rec * (3 - 2 * rec))));
            if (alpha < 0.004) return;
            double grow = Deeper ? 0.24 : 0.06;
            double s = Scale * (1 - grow + grow * e) * (1 + 0.02 * (1 - Fade)) * (1 - 0.08 * (1 - Math.Pow(1 - rec, 3)));

            if (alpha < 0.996) dc.PushOpacity(alpha);
            var sk = _skin;
            bool frost = sk.Decor == Decor.Frost;
            var center = new Point(ActualWidth / 2, ActualHeight / 2);
            dc.PushTransform(new MatrixTransform(s, 0, 0, s, center.X, center.Y));

            // Zwei Schichten, beide nach unten versetzt: weiter Raumschatten und enger Kontaktschatten.
            dc.DrawEllipse(_ambient, null, new Point(0, 22), _outer + 60, _outer + 60);
            dc.DrawEllipse(_contact, null, new Point(0, 5), _outer + 10, _outer + 10);

            // Milchglas: unter der Scheibe der weichgezeichnete Bildschirm. Er steht beim Einblenden still, nur die
            // Scheibe wächst – wie eine Glasplatte, die über das Bild gelegt wird.
            if (frost && Backdrop != null)
            {
                dc.Pop();
                dc.PushClip(new EllipseGeometry(center, _outer * s, _outer * s));
                dc.DrawImage(Backdrop, BackdropRect);
                dc.Pop();
                dc.PushTransform(new MatrixTransform(s, 0, 0, s, center.X, center.Y));
            }
            dc.DrawEllipse(sk.BChassis, sk.PRim, new Point(0, 0), _outer - 0.5, _outer - 0.5);
            if (frost) dc.DrawEllipse(null, PFrostEdge, new Point(0, 0), _outer + 0.5, _outer + 0.5);

            double step = 2 * Math.PI / n;
            for (int i = 0; i < n; i++)
            {
                var slot = _slots[i];
                bool empty = slot.IsEmpty;
                double g = EditMode ? (i == Selected && !empty ? 1 : 0) : _glow[i];
                double mid = -Math.PI / 2 + i * step;
                Brush fill;
                if (empty) fill = EditMode && i == Hover ? sk.BKey : sk.BKeyEmpty;
                else fill = B(Lerp(EditMode && i == Hover && i != Selected ? sk.KeyHot : sk.Key, slot.ColorValue, g));

                double lift = Motion ? LiftPx * g : 0;
                if (lift > 0.01)
                {
                    // Die gehobene Taste hinterlässt eine dunkle Mulde – so liest sich der Versatz als Hub, nicht als Verrutschen.
                    dc.DrawGeometry(sk.BSeat, null, _geo[i]);
                    dc.PushTransform(new TranslateTransform(Math.Cos(mid) * lift, Math.Sin(mid) * lift));
                }
                dc.DrawGeometry(fill, EditMode && i == Selected ? sk.PSelect : null, _geo[i]);
                DrawKeyContent(dc, i, slot, mid, g > 0.5);
                if (lift > 0.01) dc.Pop();
            }

            if (sk.Decor == Decor.Xmas) SkinDecor.DrawXmas(dc, _outer, _time, Motion);
            if (frost) dc.DrawEllipse(null, _sheen, new Point(0, 0), _outer - 1.25, _outer - 1.25);

            DrawHub(dc, n);

            if (PointerOn && !EditMode)
            {
                double r = (HubRadius + InnerRadius) / 2, d = 0.17;
                var sg = new StreamGeometry();
                using (var c = sg.Open())
                {
                    c.BeginFigure(Polar(r, PointerAngle - d), false, false);
                    c.ArcTo(Polar(r, PointerAngle + d), new Size(r, r), 0, false, SweepDirection.Clockwise, true, true);
                }
                bool onKey = Hover >= 0 && Hover < n && !_slots[Hover].IsEmpty;
                dc.DrawGeometry(null, P(onKey ? sk.Tint(_slots[Hover].ColorValue) : sk.Text2, 4), sg);
            }

            if (sk.Decor == Decor.Eye) SkinDecor.DrawSpider(dc, _outer, _time, _sinceOpen, Motion);

            dc.Pop();
            if (alpha < 0.996) dc.Pop();
        }

        void DrawKeyContent(DrawingContext dc, int i, Slot slot, double mid, bool lit)
        {
            double rOut = _outer - RimWidth;
            double rc = (InnerRadius + rOut) / 2 + 1;
            double px = Math.Cos(mid) * rc, py = Math.Sin(mid) * rc;

            if (slot.IsEmpty)
            {
                if (EditMode) Icons.Draw(dc, "v:plus", null, new Rect(px - 10, py - 10, 20, 20), _skin.BText3, _ppd);
                return;
            }

            bool folder = slot.Action.Type == ActionType.Folder;
            bool hasIcon = slot.HasIcon;
            bool hasImage = slot.Bitmap != null;
            string label = slot.DisplayLabel;
            bool showLabel = label.Length > 0 && !(hasIcon && string.IsNullOrWhiteSpace(slot.Label));

            // Die Farbe der Taste trägt im Ruhezustand das oberste Element: das Symbol, sonst die Beschriftung.
            Brush tint = lit ? _skin.BInk : B(_skin.Tint(slot.ColorValue));
            double labelY = hasIcon && showLabel ? py + (hasImage ? 20.5 : 15.5) : py;
            double maxW = LabelWidth(i, px, labelY, mid, folder);

            if (hasIcon && showLabel)
            {
                var ft = Label(i, label, lit, lit ? _skin.BInk : _skin.BText, 12.5, maxW, 2);
                double icon = hasImage ? 36 : 26, gap = 5, top = py - (icon + gap + ft.Height) / 2;
                Icons.Draw(dc, slot.Icon, slot.Bitmap, new Rect(px - icon / 2, top, icon, icon), tint, _ppd);
                dc.DrawText(ft, new Point(px - maxW / 2, top + icon + gap));
            }
            else if (hasIcon)
            {
                double icon = hasImage ? 60 : 36;
                Icons.Draw(dc, slot.Icon, slot.Bitmap, new Rect(px - icon / 2, py - icon / 2, icon, icon), tint, _ppd);
            }
            else
            {
                var ft = Label(i, label, lit, tint, 13.5, maxW, 3);
                dc.DrawText(ft, new Point(px - maxW / 2, py - ft.Height / 2));
            }

            if (folder)
            {
                // Winkel am Außenrand: hier geht es eine Ebene tiefer.
                double c = Math.Cos(mid), s = Math.Sin(mid);
                Point tip = new Point(c * (rOut - 8), s * (rOut - 8));
                Point back = new Point(c * (rOut - 12.5), s * (rOut - 12.5));
                var pen = P(lit ? _skin.Ink : _skin.Text2, 1.75);
                dc.DrawLine(pen, new Point(back.X - s * 5.5, back.Y + c * 5.5), tip);
                dc.DrawLine(pen, new Point(back.X + s * 5.5, back.Y - c * 5.5), tip);
            }
        }

        /// <summary>
        /// Breite, die eine waagerechte Beschriftung um (px, y) in dieser Taste hat. Seitliche Tasten bieten viel mehr
        /// Platz als die obere und untere – deshalb wird abgetastet statt pauschal über die Sehne gerechnet.
        /// </summary>
        double LabelWidth(int i, double px, double y, double mid, bool folder)
        {
            if (!double.IsNaN(_labelW[i])) return _labelW[i];
            double half = Math.PI / _slots.Count, rIn = InnerRadius + 5, rOut = _outer - RimWidth - (folder ? 17 : 5);
            Func<double, double, bool> inside = (x, yy) =>
            {
                double r = Math.Sqrt(x * x + yy * yy);
                if (r < rIn || r > rOut) return false;
                double a = Math.Atan2(yy, x) - mid;
                double edge = half - Math.Abs(Math.Atan2(Math.Sin(a), Math.Cos(a)));
                return edge > 0 && r * Math.Sin(Math.Min(edge, Math.PI / 2)) >= KeyGap / 2 + 5;
            };
            double room = 0;
            for (double d = 2; d <= 70; d += 2)
            {
                if (!inside(px - d, y - 9) || !inside(px + d, y - 9) || !inside(px - d, y + 9) || !inside(px + d, y + 9)) break;
                room = d;
            }
            return _labelW[i] = Math.Max(44, 2 * room);
        }

        FormattedText Label(int i, string text, bool lit, Brush brush, double size, double maxW, int lines)
        {
            int k = lit ? 1 : 0;
            if (_labels[i, k] == null) _labels[i, k] = Text(text, Semibold, size, brush, maxW, lines);
            return _labels[i, k];
        }

        void DrawHub(DrawingContext dc, int n)
        {
            var sk = _skin;
            int key = Hover >= 0 && Hover < n ? Hover : -1;
            bool eye = sk.Decor == Decor.Eye;
            if (eye)
            {
                // Halloween: In der Nabe sitzt ein Auge. Zeigt das Display Text, legt sich ein Schatten darüber.
                SkinDecor.DrawEye(dc, sk, _gaze, Closure(), _veil);
                if (_veil > 0.004)
                    dc.DrawEllipse(new SolidColorBrush(Color.FromArgb((byte)(176 * _veil), 8, 3, 6)), null, new Point(0, 0),
                                   HubRadius - 4, HubRadius - 4);
                if (key < 0 || _veil < 0.004) return;
            }
            else
            {
                // Blende und eingelassenes Display: der Ring macht aus der Scheibe eine Anzeige.
                dc.DrawEllipse(sk.BBezel, sk.PHubRim, new Point(0, 0), HubRadius, HubRadius);
                dc.DrawEllipse(sk.BHub, null, new Point(0, 0), HubRadius - 4, HubRadius - 4);
                if (sk.Decor == Decor.Xmas) SkinDecor.DrawSnowGlobe(dc, _time, Motion);
            }

            if (_hubFor != key)
            {
                _hubFor = key;
                string title, desc;
                if (key < 0) { title = _title; desc = _hint; }
                else if (_slots[key].IsEmpty) { title = "Frei"; desc = EditMode ? "Anklicken und belegen" : "Keine Aktion"; }
                else { title = _slots[key].DisplayLabel; desc = _slots[key].Action.Summary(); }
                if (title == desc) desc = "";
                _hubTitle = title.Length > 0 ? Text(title, Semibold, 15, sk.BText, 128, 2) : null;
                int lines = _hubTitle != null && _hubTitle.Height > 26 ? 3 : 4;
                _hubDesc = desc.Length > 0 ? Text(desc, Regular, 13, sk.BText2, 126, lines) : null;
            }

            double th = _hubTitle != null ? _hubTitle.Height : 0, dh = _hubDesc != null ? _hubDesc.Height : 0;
            double gap = th > 0 && dh > 0 ? 5 : 0;
            double y = -(th + gap + dh) / 2 - 1;
            if (eye) dc.PushOpacity(_veil);
            if (_hubTitle != null) dc.DrawText(_hubTitle, new Point(-64, y));
            if (_hubDesc != null) dc.DrawText(_hubDesc, new Point(-63, y + th + gap));
            if (eye) dc.Pop();
        }
    }
}
