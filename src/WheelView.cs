using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace Kyklos
{
    /// <summary>
    /// Zeichnet das Rad: Graphit-Scheibe, keilförmige Tasten mit gleich breiten Fugen, Display in der Nabe.
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

        static readonly Color CChassis = C("#15161A"), CKey = C("#26282E"), CKeyHot = C("#343841"), CKeyEmpty = C("#1B1C20"),
                              CText = C("#EDEEF0"), CText2 = C("#A4A9B1"), CDark = C("#15161A");
        static readonly Brush BChassis = B(CChassis), BHub = B(C("#0A0B0D")), BBezel = B(C("#1D1F24")), BSeat = B(C("#0C0D10")), BText = B(CText), BText2 = B(CText2),
                              BText3 = B(C("#7B808A")), BDark = B(CDark), BKeyEmpty = B(CKeyEmpty);
        static readonly Pen PRim = P(C("#2E3036"), 1), PHubRim = P(C("#2A2C32"), 1), PSelect = P(Colors.White, 2);
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

        public WheelView() { RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.HighQuality); }

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
            _contact = Falloff(_outer - 6, _outer + 10, 132, 1.6);
            _ambient = Falloff(_outer - 30, _outer + 60, 74, 2.0);
            Refresh();
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
            return moving;
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
            return Color.FromRgb((byte)(a.R + (b.R - a.R) * t), (byte)(a.G + (b.G - a.G) * t), (byte)(a.B + (b.B - a.B) * t));
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
            double e = EaseOut(Open);
            double alpha = Math.Max(0, Math.Min(1, e * Fade));
            if (alpha < 0.004) return;
            double s = Scale * (0.94 + 0.06 * e) * (1 + 0.02 * (1 - Fade));

            if (alpha < 0.996) dc.PushOpacity(alpha);
            dc.PushTransform(new MatrixTransform(s, 0, 0, s, ActualWidth / 2, ActualHeight / 2));

            // Zwei Schichten, beide nach unten versetzt: weiter Raumschatten und enger Kontaktschatten.
            dc.DrawEllipse(_ambient, null, new Point(0, 22), _outer + 60, _outer + 60);
            dc.DrawEllipse(_contact, null, new Point(0, 5), _outer + 10, _outer + 10);
            dc.DrawEllipse(BChassis, PRim, new Point(0, 0), _outer - 0.5, _outer - 0.5);

            double step = 2 * Math.PI / n;
            for (int i = 0; i < n; i++)
            {
                var slot = _slots[i];
                bool empty = slot.IsEmpty;
                double g = EditMode ? (i == Selected && !empty ? 1 : 0) : _glow[i];
                double mid = -Math.PI / 2 + i * step;
                Brush fill;
                if (empty) fill = EditMode && i == Hover ? B(CKey) : BKeyEmpty;
                else fill = B(Lerp(EditMode && i == Hover && i != Selected ? CKeyHot : CKey, slot.ColorValue, g));

                double lift = Motion ? LiftPx * g : 0;
                if (lift > 0.01)
                {
                    // Die gehobene Taste hinterlässt eine dunkle Mulde – so liest sich der Versatz als Hub, nicht als Verrutschen.
                    dc.DrawGeometry(BSeat, null, _geo[i]);
                    dc.PushTransform(new TranslateTransform(Math.Cos(mid) * lift, Math.Sin(mid) * lift));
                }
                dc.DrawGeometry(fill, EditMode && i == Selected ? PSelect : null, _geo[i]);
                DrawKeyContent(dc, i, slot, mid, g > 0.5);
                if (lift > 0.01) dc.Pop();
            }

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
                dc.DrawGeometry(null, P(onKey ? _slots[Hover].ColorValue : CText2, 4), sg);
            }

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
                if (EditMode) Icons.Draw(dc, "v:plus", null, new Rect(px - 10, py - 10, 20, 20), BText3, _ppd);
                return;
            }

            bool folder = slot.Action.Type == ActionType.Folder;
            bool hasIcon = slot.HasIcon;
            bool hasImage = slot.Bitmap != null;
            string label = slot.DisplayLabel;
            bool showLabel = label.Length > 0 && !(hasIcon && string.IsNullOrWhiteSpace(slot.Label));

            // Die Farbe der Taste trägt im Ruhezustand das oberste Element: das Symbol, sonst die Beschriftung.
            Brush tint = lit ? BDark : B(slot.ColorValue);
            double labelY = hasIcon && showLabel ? py + (hasImage ? 20.5 : 15.5) : py;
            double maxW = LabelWidth(i, px, labelY, mid, folder);

            if (hasIcon && showLabel)
            {
                var ft = Label(i, label, lit, lit ? BDark : BText, 12.5, maxW, 2);
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
                var pen = P(lit ? CDark : CText2, 1.75);
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
            // Blende und eingelassenes Display: der Ring macht aus der dunklen Scheibe eine Anzeige.
            dc.DrawEllipse(BBezel, PHubRim, new Point(0, 0), HubRadius, HubRadius);
            dc.DrawEllipse(BHub, null, new Point(0, 0), HubRadius - 4, HubRadius - 4);

            int key = Hover >= 0 && Hover < n ? Hover : -1;
            if (_hubFor != key)
            {
                _hubFor = key;
                string title, desc;
                if (key < 0) { title = _title; desc = _hint; }
                else if (_slots[key].IsEmpty) { title = "Frei"; desc = EditMode ? "Anklicken und belegen" : "Keine Aktion"; }
                else { title = _slots[key].DisplayLabel; desc = _slots[key].Action.Summary(); }
                if (title == desc) desc = "";
                _hubTitle = title.Length > 0 ? Text(title, Semibold, 15, BText, 128, 2) : null;
                int lines = _hubTitle != null && _hubTitle.Height > 26 ? 3 : 4;
                _hubDesc = desc.Length > 0 ? Text(desc, Regular, 13, BText2, 126, lines) : null;
            }

            double th = _hubTitle != null ? _hubTitle.Height : 0, dh = _hubDesc != null ? _hubDesc.Height : 0;
            double gap = th > 0 && dh > 0 ? 5 : 0;
            double y = -(th + gap + dh) / 2 - 1;
            if (_hubTitle != null) dc.DrawText(_hubTitle, new Point(-64, y));
            if (_hubDesc != null) dc.DrawText(_hubDesc, new Point(-63, y + th + gap));
        }
    }
}
