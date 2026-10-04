using System;
using System.Windows;
using System.Windows.Media;

namespace Kyklos
{
    /// <summary>Was ein Aussehen über die Grundfarben hinaus zeichnet.</summary>
    public enum Decor { None, Frost, Eye, Xmas }

    /// <summary>
    /// Ein Aussehen des Rads: Gerätefarben plus optionaler Schmuck. Die Tastenfarben (Palette) bleiben in jedem Aussehen
    /// dieselben; auf hellen Geräten werden Symbole und Beschriftungen in Ruhe so weit abgedunkelt, dass sie lesbar sind.
    /// </summary>
    public sealed class Skin
    {
        public const string DefaultId = "graphit";

        public string Id, Name, Note;
        public Decor Decor;
        public bool Light;                  // helles Gerät: Tastenfarben in Ruhe abdunkeln
        public Color Chassis, Rim, Key, KeyHot, KeyEmpty, Seat, Hub, Bezel, HubRim, Text, Text2, Text3, Ink, Select, FieldHover;
        public byte Ambient = 74, Contact = 132;

        public Brush BChassis, BKey, BKeyEmpty, BSeat, BHub, BBezel, BText, BText2, BText3, BInk;
        public Pen PRim, PHubRim, PSelect;

        /// <summary>Braucht laufende Bilder (Blinzeln, Lichter, Schnee) – dann zeichnet das Rad jedes Bild neu.</summary>
        public bool Animated { get { return Decor == Decor.Eye || Decor == Decor.Xmas; } }

        static Color C(string hex) { return (Color)ColorConverter.ConvertFromString(hex); }
        static SolidColorBrush B(Color c) { var b = new SolidColorBrush(c); b.Freeze(); return b; }
        static Pen P(Color c, double w) { var p = new Pen(B(c), w); p.Freeze(); return p; }

        Skin Build()
        {
            BChassis = B(Chassis); BKey = B(Key); BKeyEmpty = B(KeyEmpty); BSeat = B(Seat); BHub = B(Hub); BText = B(Text);
            BText2 = B(Text2); BText3 = B(Text3); BInk = B(Ink);
            PRim = P(Rim, 1); PHubRim = P(HubRim, 1); PSelect = P(Select, 2);
            if (Decor == Decor.Xmas)
            {
                // Messingblende um die Schneekugel.
                var g = new LinearGradientBrush { StartPoint = new Point(0.2, 0), EndPoint = new Point(0.8, 1) };
                g.GradientStops.Add(new GradientStop(C("#F1D488"), 0));
                g.GradientStops.Add(new GradientStop(C("#B98A35"), 0.45));
                g.GradientStops.Add(new GradientStop(C("#7A561C"), 0.7));
                g.GradientStops.Add(new GradientStop(C("#D7B260"), 1));
                g.Freeze();
                BBezel = g;
            }
            else BBezel = B(Bezel);
            return this;
        }

        public static readonly Skin Graphit = new Skin
        {
            Id = "graphit", Name = "Graphit", Note = "Matt und dunkel, wie ein Bedienteil. Die Voreinstellung.",
            Chassis = C("#15161A"), Rim = C("#2E3036"), Key = C("#26282E"), KeyHot = C("#343841"), KeyEmpty = C("#1B1C20"),
            Seat = C("#0C0D10"), Hub = C("#0A0B0D"), Bezel = C("#1D1F24"), HubRim = C("#2A2C32"),
            Text = C("#EDEEF0"), Text2 = C("#A4A9B1"), Text3 = C("#7B808A"), Ink = C("#15161A"), Select = Colors.White,
            FieldHover = C("#4A4E57")
        }.Build();

        public static readonly Skin Hell = new Skin
        {
            Id = "hell", Name = "Hell", Light = true,
            Note = "Helles Gehäuse, weiße Tasten. Die Tastenfarben werden in Ruhe abgedunkelt, damit sie auf Weiß lesbar bleiben.",
            Chassis = C("#DFE2E6"), Rim = C("#C3C8CF"), Key = C("#FBFBFC"), KeyHot = C("#EEF0F3"), KeyEmpty = C("#E9EBEE"),
            Seat = C("#B9BEC6"), Hub = C("#F6F7F8"), Bezel = C("#CDD1D7"), HubRim = C("#B4BAC2"),
            Text = C("#15161A"), Text2 = C("#4F545D"), Text3 = C("#7E838C"), Ink = C("#15161A"), Select = C("#15161A"),
            FieldHover = C("#9AA0A9"), Ambient = 46, Contact = 64
        }.Build();

        public static readonly Skin Milchglas = new Skin
        {
            Id = "milchglas", Name = "Milchglas", Light = true, Decor = Decor.Frost,
            Note = "Das Rad liegt wie eine Mattglasscheibe über dem Bildschirm, was dahinter ist, schimmert weichgezeichnet durch.",
            Chassis = C("#5CF2F4F8"), Rim = C("#C8FFFFFF"), Key = C("#9EFFFFFF"), KeyHot = C("#C4FFFFFF"), KeyEmpty = C("#3DFFFFFF"),
            Seat = C("#24000000"), Hub = C("#B0FFFFFF"), Bezel = C("#5CFFFFFF"), HubRim = C("#D9FFFFFF"),
            Text = C("#111216"), Text2 = C("#3A3E46"), Text3 = C("#5E636C"), Ink = C("#15161A"), Select = C("#15161A"),
            FieldHover = C("#9AA0A9"), Ambient = 40, Contact = 52
        }.Build();

        public static readonly Skin Halloween = new Skin
        {
            Id = "halloween", Name = "Halloween", Decor = Decor.Eye,
            Note = "In der Mitte sitzt ein Auge, das deinem Mauszeiger folgt und ab und zu blinzelt. Zeigt die Mitte einen Text, legt sich ein Schatten darüber.",
            Chassis = C("#120D17"), Rim = C("#2C2035"), Key = C("#221A2A"), KeyHot = C("#30253A"), KeyEmpty = C("#18121D"),
            Seat = C("#08050B"), Hub = C("#0A060C"), Bezel = C("#2A1119"), HubRim = C("#3D1A24"),
            Text = C("#F2EADC"), Text2 = C("#BBAE9F"), Text3 = C("#7E7186"), Ink = C("#120D17"), Select = C("#F2EADC"),
            FieldHover = C("#4A3B55")
        }.Build();

        public static readonly Skin Weihnachten = new Skin
        {
            Id = "weihnachten", Name = "Weihnachten", Decor = Decor.Xmas,
            Note = "Tannengrün mit Lichterkette, Schneehaube und Stechpalme, in der Mitte rieselt Schnee wie in einer Schneekugel.",
            Chassis = C("#0F2A20"), Rim = C("#24503D"), Key = C("#1A3C2E"), KeyHot = C("#244B3B"), KeyEmpty = C("#133225"),
            Seat = C("#061510"), Hub = C("#0A1D15"), Bezel = C("#B98A35"), HubRim = C("#5E4718"),
            Text = C("#F7F2E8"), Text2 = C("#B9CCC0"), Text3 = C("#71917F"), Ink = C("#0F2A20"), Select = C("#F7F2E8"),
            FieldHover = C("#3C6B56")
        }.Build();

        public static readonly Skin[] All = { Graphit, Hell, Milchglas, Halloween, Weihnachten };

        public static Skin Get(string id)
        {
            foreach (var s in All) if (s.Id == id) return s;
            return Graphit;
        }

        public static bool Exists(string id)
        {
            foreach (var s in All) if (s.Id == id) return true;
            return false;
        }

        // ---------------------------------------------------------------- Lesbarkeit auf hellen Geräten

        static double Lum(Color c)
        {
            Func<byte, double> ch = v => { double x = v / 255.0; return x <= 0.03928 ? x / 12.92 : Math.Pow((x + 0.055) / 1.055, 2.4); };
            return 0.2126 * ch(c.R) + 0.7152 * ch(c.G) + 0.0722 * ch(c.B);
        }

        static double Contrast(Color a, Color b)
        {
            double la = Lum(a), lb = Lum(b);
            return (Math.Max(la, lb) + 0.05) / (Math.Min(la, lb) + 0.05);
        }

        /// <summary>
        /// Farbe von Symbol und Beschriftung einer Taste in Ruhe. Auf dunklen Geräten die Tastenfarbe selbst; auf hellen
        /// so weit zu Schwarz gemischt, bis sie auf der Tastenkappe 4,5:1 erreicht – der Farbton bleibt erkennbar.
        /// </summary>
        public Color Tint(Color c)
        {
            if (!Light) return c;
            // Milchglas: die Kappe ist durchscheinend, gerechnet wird gegen ein typisches Grau unter dem Glas.
            Color against = Decor == Decor.Frost ? C("#E4E6EA") : Key;
            for (int i = 0; i < 20 && Contrast(c, against) < 4.5; i++)
                c = Color.FromRgb((byte)(c.R * 0.9), (byte)(c.G * 0.9), (byte)(c.B * 0.9));
            return c;
        }
    }

    /// <summary>Das Rad in klein für die Auswahl in den Einstellungen: vier Tasten, die obere leuchtet, samt Schmuck.</summary>
    public sealed class SkinChip : FrameworkElement
    {
        const double Outer = 124, Hub = 46;
        static readonly Geometry[] Keys = BuildKeys();
        static readonly Color Lit = (Color)ColorConverter.ConvertFromString(Palette.Default);

        readonly Skin _skin;

        public SkinChip(Skin skin) { _skin = skin; }

        static Geometry[] BuildKeys()
        {
            var k = new Geometry[4];
            for (int i = 0; i < 4; i++)
            {
                double mid = -Math.PI / 2 + i * Math.PI / 2;
                k[i] = WheelView.BuildKey(Hub + 7, Outer - 9, mid - Math.PI / 4, mid + Math.PI / 4, 11, 10);
            }
            return k;
        }

        static Brush Blob(string hex)
        {
            var c = (Color)ColorConverter.ConvertFromString(hex);
            var b = new RadialGradientBrush(c, Color.FromArgb(0, c.R, c.G, c.B));
            b.Freeze();
            return b;
        }

        static readonly Brush[] Blobs = { Blob("#FF8A3D"), Blob("#62ABFF"), Blob("#5DD28B") };
        static readonly Point[] BlobAt = { new Point(-70, -60), new Point(80, -10), new Point(-20, 85) };

        protected override void OnRender(DrawingContext dc)
        {
            var sk = _skin;
            double s = Math.Min(ActualWidth, ActualHeight) / (2 * (Outer + 12));
            dc.PushTransform(new MatrixTransform(s, 0, 0, s, ActualWidth / 2, ActualHeight / 2));

            if (sk.Decor == Decor.Frost)
            {
                // Etwas Farbe hinter dem Glas, außen scharf, unter der Scheibe weich – sonst wäre Milchglas auf Weiß unsichtbar.
                for (int i = 0; i < 3; i++) dc.DrawEllipse(Blobs[i], null, BlobAt[i], 62, 62);
                dc.PushClip(new EllipseGeometry(new Point(0, 0), Outer, Outer));
                dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(0xF2, 0xF3, 0xF5)), null, new Rect(-Outer, -Outer, 2 * Outer, 2 * Outer));
                for (int i = 0; i < 3; i++) dc.DrawEllipse(Blobs[i], null, BlobAt[i], 92, 92);
                dc.Pop();
            }

            var rim = new Pen(sk.PRim.Brush, 1 / s);
            dc.DrawEllipse(sk.BChassis, rim, new Point(0, 0), Outer, Outer);
            for (int i = 0; i < 4; i++) dc.DrawGeometry(i == 0 ? new SolidColorBrush(Lit) : sk.BKey, null, Keys[i]);

            double h = Hub / WheelView.HubRadius;
            dc.PushTransform(new ScaleTransform(h, h));
            if (sk.Decor == Decor.Eye) SkinDecor.DrawEye(dc, sk, new Vector(40, 18), 0, 0.2);
            else
            {
                dc.DrawEllipse(sk.BBezel, null, new Point(0, 0), WheelView.HubRadius, WheelView.HubRadius);
                dc.DrawEllipse(sk.BHub, null, new Point(0, 0), WheelView.HubRadius - 6, WheelView.HubRadius - 6);
                if (sk.Decor == Decor.Xmas) SkinDecor.DrawSnowGlobe(dc, 0, false);
            }
            dc.Pop();
            if (sk.Decor == Decor.Xmas) SkinDecor.DrawXmas(dc, Outer, 0, false);
            dc.Pop();
        }
    }
}
