using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace Kyklos
{
    /// <summary>
    /// Schmuck der Aussehen, ganz aus Code gezeichnet (bleibt bei jeder Skalierung scharf): das Auge für Halloween,
    /// Lichterkette, Schneehaube, Stechpalme und Schneekugel für Weihnachten. Ursprung ist die Radmitte.
    /// </summary>
    public static class SkinDecor
    {
        static Color C(string hex) { return (Color)ColorConverter.ConvertFromString(hex); }
        static Color A(Color c, double a) { return Color.FromArgb((byte)Math.Round(255 * Math.Max(0, Math.Min(1, a))), c.R, c.G, c.B); }
        static SolidColorBrush B(Color c) { var b = new SolidColorBrush(c); b.Freeze(); return b; }
        static Pen P(Color c, double w)
        {
            var p = new Pen(B(c), w) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round, LineJoin = PenLineJoin.Round };
            p.Freeze();
            return p;
        }
        static Point Polar(double r, double a) { return new Point(Math.Cos(a) * r, Math.Sin(a) * r); }

        static RadialGradientBrush Radial(params object[] stops)
        {
            var b = new RadialGradientBrush();
            for (int i = 0; i < stops.Length; i += 2) b.GradientStops.Add(new GradientStop((Color)stops[i + 1], (double)stops[i]));
            return b;
        }

        static LinearGradientBrush Vertical(Color top, Color bottom)
        {
            var b = new LinearGradientBrush(top, bottom, new Point(0, 0), new Point(0, 1));
            b.Freeze();
            return b;
        }

        // ================================================================ Halloween: das Auge

        public const double EyeRadius = WheelView.HubRadius - 4, IrisRadius = 31, MaxGaze = 38;
        public const double BlinkSeconds = 0.17, OpenEyeSeconds = 0.32, RestingLid = 0.14;

        static readonly Brush Sclera = Freeze(Radial(0.0, C("#FFF8EE"), 0.5, C("#F0E0CC"), 0.76, C("#DDAE98"), 0.9, C("#A9554A"), 1.0, C("#4A1517")),
                                              new Point(0.4, 0.33));
        static readonly Brush Iris = Freeze(Radial(0.0, C("#FFE37A"), 0.28, C("#F6B03E"), 0.6, C("#D2561A"), 0.85, C("#6E1A08"), 1.0, C("#240603")), null);
        static readonly Brush IrisShade = Freeze(Radial(0.0, A(Colors.Black, 0), 0.72, A(Colors.Black, 0), 0.86, A(Colors.Black, 0.42), 1.0, A(Colors.Black, 0)), null);
        static readonly Brush Socket = Freeze(Radial(0.0, A(Colors.Black, 0), 0.7, A(Colors.Black, 0), 1.0, A(Colors.Black, 0.62)), null);
        static readonly Brush Glint = Freeze(Radial(0.0, A(Colors.White, 0.95), 0.6, A(Colors.White, 0.85), 1.0, A(Colors.White, 0)), null);
        static readonly Brush PupilFill = B(C("#060102"));
        static readonly Brush LidTop = Vertical(C("#4C2331"), C("#2A1018")), LidBottom = Vertical(C("#2A1018"), C("#40202A"));
        static readonly Pen Limbus = P(C("#160403"), 2.2), LidEdge = P(C("#0B0306"), 2), LidShadow = P(A(Colors.Black, 0.28), 9);
        static readonly Pen[] VeinPens = { P(A(C("#A3121A"), 0.62), 1.5), P(A(C("#A3121A"), 0.5), 1.0), P(A(C("#B0202A"), 0.38), 0.6) };
        static readonly Pen StriaDark = P(A(C("#3A0E04"), 0.36), 0.9), StriaLight = P(A(C("#FFE9A8"), 0.24), 0.7);
        static readonly Geometry[] Veins = BuildVeins();
        static readonly Geometry StriaDarkGeo, StriaLightGeo;

        static Brush Freeze(RadialGradientBrush b, Point? origin)
        {
            if (origin.HasValue) b.GradientOrigin = origin.Value;
            b.Freeze();
            return b;
        }

        static SkinDecor()
        {
            var rnd = new Random(29);
            var dark = new StreamGeometry();
            var light = new StreamGeometry();
            using (StreamGeometryContext d = dark.Open(), l = light.Open())
            {
                const int n = 56;
                for (int j = 0; j < n; j++)
                {
                    double a = j * 2 * Math.PI / n + (rnd.NextDouble() - 0.5) * 0.05;
                    d.BeginFigure(Polar(IrisRadius * (0.3 + rnd.NextDouble() * 0.06), a), false, false);
                    d.LineTo(Polar(IrisRadius * (0.78 + rnd.NextDouble() * 0.18), a + (rnd.NextDouble() - 0.5) * 0.08), true, false);
                    double b = a + Math.PI / n;
                    l.BeginFigure(Polar(IrisRadius * 0.4, b), false, false);
                    l.LineTo(Polar(IrisRadius * (0.62 + rnd.NextDouble() * 0.14), b), true, false);
                }
            }
            dark.Freeze();
            light.Freeze();
            StriaDarkGeo = dark;
            StriaLightGeo = light;
        }

        /// <summary>Äderchen vom Rand nach innen, mit Verzweigungen – dick, mittel, fein.</summary>
        static Geometry[] BuildVeins()
        {
            var rnd = new Random(13);
            var segs = new List<Point>[] { new List<Point>(), new List<Point>(), new List<Point>() };
            Action<Point, double, int, double, bool> walk = null;
            walk = (p, dir, steps, len, main) =>
            {
                for (int s = 0; s < steps; s++)
                {
                    dir += (rnd.NextDouble() - 0.5) * 0.75;
                    var q = new Point(p.X + Math.Cos(dir) * len, p.Y + Math.Sin(dir) * len);
                    int cls = main ? (s < 2 ? 0 : s < 4 ? 1 : 2) : 2;
                    segs[cls].Add(p);
                    segs[cls].Add(q);
                    if (main && (s == 1 || s == 3) && rnd.NextDouble() < 0.75)
                        walk(q, dir + (rnd.NextDouble() < 0.5 ? -0.8 : 0.8), 3, len * 0.8, false);
                    p = q;
                    if (Math.Sqrt(q.X * q.X + q.Y * q.Y) < 34) break;
                }
            };
            for (int k = 0; k < 13; k++)
            {
                double a = k * 2 * Math.PI / 13 + (rnd.NextDouble() - 0.5) * 0.35;
                walk(Polar(EyeRadius + 2, a), a + Math.PI + (rnd.NextDouble() - 0.5) * 0.5, 6 + rnd.Next(3), 6.5, true);
            }
            var result = new Geometry[3];
            for (int c = 0; c < 3; c++)
            {
                var g = new StreamGeometry();
                using (var ctx = g.Open())
                    for (int i = 0; i + 1 < segs[c].Count; i += 2)
                    {
                        ctx.BeginFigure(segs[c][i], false, false);
                        ctx.LineTo(segs[c][i + 1], true, true);
                    }
                g.Freeze();
                result[c] = g;
            }
            return result;
        }

        /// <summary>
        /// Das Auge in der Nabe. <paramref name="gaze"/>: Zeiger relativ zur Mitte (DIP), <paramref name="closure"/>: 0 offen,
        /// 1 zu, <paramref name="dilate"/>: 0..1, wie weit sich die Schlitzpupille öffnet.
        /// </summary>
        public static void DrawEye(DrawingContext dc, Skin skin, Vector gaze, double closure, double dilate)
        {
            const double R = EyeRadius;
            dc.DrawEllipse(skin.BBezel, skin.PHubRim, new Point(0, 0), WheelView.HubRadius, WheelView.HubRadius);
            dc.PushClip(new EllipseGeometry(new Point(0, 0), R, R));

            dc.DrawEllipse(Sclera, null, new Point(0, 0), R, R);
            for (int c = 0; c < 3; c++) dc.DrawGeometry(null, VeinPens[c], Veins[c]);

            // Die Iris wandert zum Zeiger und wird zum Rand hin perspektivisch schmaler – so liest sich die Scheibe als Kugel.
            double d = gaze.Length;
            double shift = MaxGaze * (1 - Math.Exp(-d / 70));
            Vector o = d > 0.001 ? gaze / d * shift : new Vector(0, 0);
            double phi = Math.Atan2(o.Y, o.X) * 180 / Math.PI;
            double f = Math.Sqrt(Math.Max(0.2, 1 - Math.Pow(shift / (R * 1.08), 2)));
            var m = Matrix.Identity;
            m.Rotate(-phi);
            m.Scale(f, 1);
            m.Rotate(phi);
            m.Translate(o.X, o.Y);
            dc.PushTransform(new MatrixTransform(m));
            dc.DrawEllipse(IrisShade, null, new Point(0, 0), IrisRadius + 7, IrisRadius + 7);
            dc.DrawEllipse(Iris, null, new Point(0, 0), IrisRadius, IrisRadius);
            dc.DrawGeometry(null, StriaDark, StriaDarkGeo);
            dc.DrawGeometry(null, StriaLight, StriaLightGeo);
            dc.DrawEllipse(null, Limbus, new Point(0, 0), IrisRadius - 1, IrisRadius - 1);
            double slit = 4.5 + 9 * dilate;
            dc.DrawEllipse(PupilFill, null, new Point(0, 0), slit / 2, IrisRadius * 0.86);
            dc.Pop();

            // Glanzlicht auf der feuchten Hornhaut: steht fest, das Licht kommt von oben links.
            dc.PushTransform(new RotateTransform(-35, -25, -30));
            dc.DrawEllipse(Glint, null, new Point(-25, -30), 10.5, 7);
            dc.Pop();
            dc.DrawEllipse(Glint, null, new Point(-10, -42), 2.8, 2.8);
            dc.DrawEllipse(Socket, null, new Point(0, 0), R, R);

            DrawLids(dc, Math.Max(RestingLid, closure));
            dc.Pop();
        }

        static void DrawLids(DrawingContext dc, double c)
        {
            const double R = EyeRadius, W = R + 12;
            double yu = -R - 8 + c * (R + 20), yl = R + 8 - c * (R - 4), k = 20 * (1 - c);

            var edge = new StreamGeometry();
            using (var g = edge.Open())
            {
                g.BeginFigure(new Point(-W, yu - k), false, false);
                g.QuadraticBezierTo(new Point(0, yu + k), new Point(W, yu - k), true, true);
            }
            edge.Freeze();
            dc.DrawGeometry(null, LidShadow, edge);

            var top = new StreamGeometry();
            using (var g = top.Open())
            {
                g.BeginFigure(new Point(-W, -W), true, true);
                g.LineTo(new Point(W, -W), false, false);
                g.LineTo(new Point(W, yu - k), false, false);
                g.QuadraticBezierTo(new Point(0, yu + k), new Point(-W, yu - k), true, true);
            }
            top.Freeze();
            dc.DrawGeometry(LidTop, null, top);
            dc.DrawGeometry(null, LidEdge, edge);

            if (yl < R + 2)
            {
                var bottom = new StreamGeometry();
                using (var g = bottom.Open())
                {
                    g.BeginFigure(new Point(-W, W), true, true);
                    g.LineTo(new Point(W, W), false, false);
                    g.LineTo(new Point(W, yl + k), false, false);
                    g.QuadraticBezierTo(new Point(0, yl - k), new Point(-W, yl + k), true, true);
                }
                bottom.Freeze();
                dc.DrawGeometry(LidBottom, LidEdge, bottom);
            }
        }

        // ---------------------------------------------------------------- Halloween: Spinne am Faden

        static readonly Brush SpiderBody = B(C("#0B0710"));
        static readonly Pen SpiderLeg = P(C("#0B0710"), 1.4), Thread = P(A(C("#E8E0F0"), 0.55), 0.8);
        static readonly Brush SpiderEye = B(C("#FF4A2E"));

        /// <summary>Hängt rechts unten am Gehäuse und pendelt nach dem Öffnen aus.</summary>
        public static void DrawSpider(DrawingContext dc, double outer, double time, double sinceOpen, bool motion)
        {
            Point anchor = Polar(outer - 2, 62 * Math.PI / 180);
            double swing = motion ? 0.32 * Math.Exp(-sinceOpen * 1.4) * Math.Sin(sinceOpen * 5.2) + 0.05 * Math.Sin(time * 1.25) : 0.04;
            double len = 34 + (motion ? 1.5 * Math.Sin(time * 0.9) : 0);
            var body = new Point(anchor.X + Math.Sin(swing) * len, anchor.Y + Math.Cos(swing) * len);
            dc.DrawLine(Thread, anchor, body);

            dc.PushTransform(new RotateTransform(-swing * 180 / Math.PI, body.X, body.Y));
            for (int side = -1; side <= 1; side += 2)
                for (int i = 0; i < 4; i++)
                {
                    double ly = body.Y + 1 + i * 2.6 - 3.5;
                    var knee = new Point(body.X + side * (6.5 + i * 0.6), ly - 4.5 + i * 1.2);
                    var foot = new Point(body.X + side * (10 + i * 1.2), ly + 3 + i * 2.2);
                    dc.DrawLine(SpiderLeg, new Point(body.X + side * 2, ly), knee);
                    dc.DrawLine(SpiderLeg, knee, foot);
                }
            dc.DrawEllipse(SpiderBody, null, new Point(body.X, body.Y + 5), 5.6, 6.8);
            dc.DrawEllipse(SpiderBody, null, new Point(body.X, body.Y - 1.5), 3.6, 3.4);
            dc.DrawEllipse(SpiderEye, null, new Point(body.X - 1.3, body.Y - 1.2), 0.8, 0.8);
            dc.DrawEllipse(SpiderEye, null, new Point(body.X + 1.3, body.Y - 1.2), 0.8, 0.8);
            dc.Pop();
        }

        // ================================================================ Weihnachten

        static readonly Color[] BulbColors = { C("#FF5A4E"), C("#FFC94D"), C("#6FE07A"), C("#62B0FF"), C("#FF8FD0") };
        static readonly Brush[] BulbGlow = Array.ConvertAll(BulbColors, c => Freeze(Radial(0.0, A(c, 0.7), 0.35, A(c, 0.3), 1.0, A(c, 0)), null));
        static readonly Brush[] BulbCore = Array.ConvertAll(BulbColors, c => (Brush)B(c));
        static readonly Brush BulbHot = B(A(Colors.White, 0.85)), Socket2 = B(C("#2B3A31"));
        static readonly Pen Wire = P(C("#0A1711"), 1.1);
        static readonly Brush Snow = Vertical(C("#FFFFFF"), C("#D5E3EE")), SnowShadow = B(A(Colors.Black, 0.28));
        static readonly Brush Leaf = Vertical(C("#2A7D45"), C("#145230"));
        static readonly Pen LeafEdge = P(C("#0D3A20"), 0.8), LeafRib = P(C("#0D3A20"), 1);
        static readonly Brush Berry = Freeze(Radial(0.0, C("#FF6B7A"), 0.55, C("#D41F37"), 1.0, C("#8E0C1F")), new Point(0.35, 0.3));
        static readonly Brush Flake = B(Colors.White);
        static readonly Brush Drift = Vertical(A(Colors.White, 0.92), A(C("#CFE0EA"), 0.92));

        const double CapFrom = -150 * Math.PI / 180, CapTo = -30 * Math.PI / 180;

        /// <summary>Schneehaube oben, Lichterkette rundherum, Stechpalme unten – über den Tasten, unter der Nabe.</summary>
        public static void DrawXmas(DrawingContext dc, double outer, double time, bool motion)
        {
            DrawLights(dc, outer, time, motion);
            DrawSnowCap(dc, outer);
            DrawHolly(dc, new Point(0, outer + 1));
        }

        static void DrawLights(DrawingContext dc, double outer, double time, bool motion)
        {
            // Die Kette läuft vom rechten Ende der Schneehaube im Uhrzeigersinn bis zum linken.
            double from = CapTo + 0.05, to = CapFrom + 2 * Math.PI - 0.05;
            int n = (int)Math.Round((to - from) * outer / 27);
            double step = (to - from) / n;
            var wire = new StreamGeometry();
            using (var g = wire.Open())
            {
                g.BeginFigure(Polar(outer, from - step * 0.6), false, false);
                for (int i = 0; i <= n; i++)
                {
                    double a = from + i * step;
                    g.QuadraticBezierTo(Polar(outer + 6, a - step / 2), Polar(outer, a), true, true);
                }
                g.QuadraticBezierTo(Polar(outer + 6, to + step / 2), Polar(outer, to + step * 0.6), true, true);
            }
            wire.Freeze();
            dc.DrawGeometry(null, Wire, wire);

            for (int i = 0; i <= n; i++)
            {
                double a = from + i * step;
                int c = i % BulbColors.Length;
                double tw = motion ? 0.5 + 0.5 * Math.Sin(time * (1.1 + (i % 5) * 0.37) + i * 2.1) : (i % 3 == 0 ? 0.55 : 1);
                Point p = Polar(outer + 1.5, a);
                dc.PushOpacity(0.25 + 0.75 * tw);
                dc.DrawEllipse(BulbGlow[c], null, p, 14, 14);
                dc.Pop();
                dc.PushTransform(new RotateTransform(a * 180 / Math.PI + 90, p.X, p.Y));
                dc.DrawRectangle(Socket2, null, new Rect(p.X - 2.2, p.Y + 2.4, 4.4, 3));
                dc.DrawEllipse(BulbCore[c], null, new Point(p.X, p.Y - 1.2), 3.4, 4.6);
                dc.PushOpacity(0.35 + 0.65 * tw);
                dc.DrawEllipse(BulbHot, null, new Point(p.X - 0.9, p.Y - 2.8), 1.1, 1.5);
                dc.Pop();
                dc.Pop();
            }
        }

        static void DrawSnowCap(DrawingContext dc, double outer)
        {
            const int n = 90;
            var pts = new Point[2 * (n + 1)];
            for (int i = 0; i <= n; i++)
            {
                double t = i / (double)n, a = CapFrom + (CapTo - CapFrom) * t;
                double taper = Math.Pow(Math.Sin(Math.PI * t), 0.55);
                double up = taper * (7 + 2.8 * Math.Sin(a * 9 + 1) + 1.6 * Math.Sin(a * 23 + 0.4));
                double drip = Math.Pow(Math.Max(0, Math.Sin(a * 13 + 0.7)), 6) * 7;
                double down = taper * (7.5 + 1.5 * Math.Sin(a * 17) + drip);
                pts[i] = Polar(outer + up, a);
                pts[2 * (n + 1) - 1 - i] = Polar(outer - down, a);
            }
            var cap = new StreamGeometry();
            using (var g = cap.Open())
            {
                g.BeginFigure(pts[0], true, true);
                var rest = new Point[pts.Length - 1];
                Array.Copy(pts, 1, rest, 0, rest.Length);
                g.PolyLineTo(rest, true, true);
            }
            cap.Freeze();
            dc.PushTransform(new TranslateTransform(0, 1.8));
            dc.DrawGeometry(SnowShadow, null, cap);
            dc.Pop();
            dc.DrawGeometry(Snow, null, cap);
        }

        static Geometry HollyLeaf(double len, double width)
        {
            // Drei Stacheln je Seite, dazwischen nach innen gewölbte Bögen.
            double[] at = { 0.0, 0.27, 0.52, 0.77, 1.0 };
            Func<double, double> half = x => width * Math.Pow(Math.Sin(Math.PI * Math.Min(0.96, Math.Max(0.04, x))), 0.75);
            var g = new StreamGeometry();
            using (var c = g.Open())
            {
                c.BeginFigure(new Point(0, 0), true, true);
                for (int side = -1; side <= 1; side += 2)
                {
                    int[] order = side < 0 ? new[] { 1, 2, 3, 4 } : new[] { 3, 2, 1, 0 };
                    foreach (int k in order)
                    {
                        double x = at[k];
                        int prev = side < 0 ? k - 1 : k + 1;
                        double mid = (x + at[prev]) / 2;
                        Point tip = k == 4 || k == 0 ? new Point(x * len, 0) : new Point(x * len, side * (half(x) + 2.6));
                        Point ctrl = new Point(mid * len, side * half(mid) * 0.62);
                        c.QuadraticBezierTo(ctrl, tip, true, true);
                    }
                }
            }
            g.Freeze();
            return g;
        }

        static readonly Geometry LeafGeo = HollyLeaf(38, 12);

        static void DrawHolly(DrawingContext dc, Point at)
        {
            foreach (double rot in new[] { 196.0, -16.0, 92.0 })
            {
                dc.PushTransform(new MatrixTransform(RotateAt(rot, at)));
                double s = rot == 92.0 ? 0.72 : 1;
                dc.PushTransform(new ScaleTransform(s, s, at.X, at.Y));
                dc.PushTransform(new TranslateTransform(at.X + 2, at.Y));
                dc.DrawGeometry(Leaf, LeafEdge, LeafGeo);
                dc.DrawLine(LeafRib, new Point(1, 0), new Point(32, 0));
                dc.Pop();
                dc.Pop();
                dc.Pop();
            }
            foreach (var o in new[] { new Vector(-5.4, -2.4), new Vector(5.4, -2.8), new Vector(0, 4.4) })
            {
                Point p = at + o;
                dc.DrawEllipse(Berry, null, p, 5.6, 5.6);
                dc.DrawEllipse(BulbHot, null, new Point(p.X - 1.8, p.Y - 1.9), 1.3, 1.3);
            }
        }

        static Matrix RotateAt(double deg, Point c)
        {
            var m = Matrix.Identity;
            m.RotateAt(deg, c.X, c.Y);
            return m;
        }

        // ---------------------------------------------------------------- Weihnachten: Schneekugel in der Nabe

        struct Snowflake { public double X, Y, Speed, Phase, Size, Alpha; }
        static readonly Snowflake[] Flakes = MakeFlakes();

        static Snowflake[] MakeFlakes()
        {
            var rnd = new Random(5);
            var f = new Snowflake[26];
            for (int i = 0; i < f.Length; i++)
                f[i] = new Snowflake
                {
                    X = (rnd.NextDouble() * 2 - 1) * EyeRadius * 0.92, Y = rnd.NextDouble() * 2 * EyeRadius, Speed = 7 + rnd.NextDouble() * 9,
                    Phase = rnd.NextDouble() * 6.28, Size = 0.9 + rnd.NextDouble() * 1.4, Alpha = 0.3 + rnd.NextDouble() * 0.4
                };
            return f;
        }

        /// <summary>Leise rieselnder Schnee hinter der Schrift und eine kleine Schneewehe am Boden der Kugel.</summary>
        public static void DrawSnowGlobe(DrawingContext dc, double time, bool motion)
        {
            const double R = EyeRadius;
            dc.PushClip(new EllipseGeometry(new Point(0, 0), R, R));
            double span = 2 * R + 10;
            foreach (var f in Flakes)
            {
                double y = ((f.Y + (motion ? time * f.Speed : 0)) % span) - R - 5;
                double x = f.X + (motion ? 3 * Math.Sin(time * 0.9 + f.Phase) : 0);
                dc.PushOpacity(f.Alpha);
                dc.DrawEllipse(Flake, null, new Point(x, y), f.Size, f.Size);
                dc.Pop();
            }
            var drift = new StreamGeometry();
            using (var g = drift.Open())
            {
                // Bleibt unter der vierten Zeile des Displays, damit Schrift nie auf Schnee steht.
                g.BeginFigure(new Point(-R, R * 0.95), true, true);
                g.BezierTo(new Point(-R * 0.4, R * 0.8), new Point(R * 0.3, R * 0.9), new Point(R, R * 0.84), true, true);
                g.LineTo(new Point(R, R + 2), false, false);
                g.LineTo(new Point(-R, R + 2), false, false);
            }
            drift.Freeze();
            dc.DrawGeometry(Drift, null, drift);
            dc.Pop();
        }
    }
}
