using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Kyklos
{
    /// <summary>
    /// Entwicklerwerkzeuge, über versteckte Schalter erreichbar:
    ///   --dev-icon &lt;datei.ico&gt;   erzeugt das Programmsymbol
    ///   --dev-render &lt;ordner&gt;    rendert Rad, Symbolübersicht und Einstellungen als PNG zur Sichtprüfung
    ///   --dev-savetest &lt;ordner&gt;  prüft das Speichern gegen eine von außen offen gehaltene Datei (savetest.txt)
    /// </summary>
    public static class Dev
    {
        public static int Run(string[] args)
        {
            if (args.Length < 2) return 2;
            if (args[0] == "--dev-icon") { MakeIcon(args[1]); return 0; }
            if (args[0] == "--dev-render") { Render(args[1]); return 0; }
            if (args[0] == "--dev-savetest") return SaveTest(args[1]);
            return 2;
        }

        // ---------------------------------------------------------------- Speichern

        static string FirstName(string file)
        {
            var d = Json.Parse(File.ReadAllText(file)) as Dictionary<string, object>;
            return Json.S(Json.A(d, "wheels")[0] as Dictionary<string, object>, "name");
        }

        /// <summary>
        /// Spielt nach, was Virenscanner, Suchindex und Synchronisierung tun: die Konfiguration offen halten, während
        /// Kyklos speichern will. Ergebnis steht in savetest.txt; Rückgabe 0, wenn alles stimmt.
        /// </summary>
        static int SaveTest(string dir)
        {
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, "Kyklos.json"), tmp = path + ".tmp";
            File.Delete(path);
            File.Delete(tmp);
            var lines = new List<string>();
            bool all = true;
            Action<string, bool> check = (name, ok) => { lines.Add((ok ? "OK      " : "FEHLER  ") + name); all &= ok; };
            var cfg = ConfigStore.Default();

            ConfigStore.Save(cfg, path);
            check("Erstes Speichern legt die Datei an", File.Exists(path) && !File.Exists(tmp));

            cfg.Wheels[0].Name = "Zweiter Stand";
            ConfigStore.Save(cfg, path);
            check("Normales Ersetzen", FirstName(path) == "Zweiter Stand" && !File.Exists(tmp));

            // Der Fall aus dem Fehlerbericht: Ein anderes Programm hält die Datei offen und verbietet nur das Entfernen.
            cfg.Wheels[0].Name = "Dritter Stand";
            bool threw = false;
            using (new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                try { ConfigStore.Save(cfg, path); }
                catch (IOException ex) { threw = true; lines.Add("        " + ex.Message.Trim()); }
            }
            check("Speichern, waehrend ein anderes Programm die Datei offen haelt", !threw && FirstName(path) == "Dritter Stand");

            // Ganz gesperrt: Das Speichern muss den Fehler melden, und der neue Stand muss in der Zwischendatei warten.
            cfg.Wheels[0].Name = "Vierter Stand";
            bool failed = false;
            using (new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                try { ConfigStore.Save(cfg, path); }
                catch (IOException) { failed = true; }
                catch (UnauthorizedAccessException) { failed = true; }
            }
            check("Gesperrte Datei: Fehler wird gemeldet, Stand liegt in der Zwischendatei",
                  failed && File.Exists(tmp) && FirstName(tmp) == "Vierter Stand" && FirstName(path) == "Dritter Stand");

            bool created;
            string problem;
            var loaded = ConfigStore.Load(path, out created, out problem);
            check("Naechster Start uebernimmt den Stand aus der Zwischendatei", loaded.Wheels[0].Name == "Vierter Stand" && problem == null && !created);

            ConfigStore.Save(loaded, path);
            check("Naechstes Speichern raeumt die Zwischendatei weg", FirstName(path) == "Vierter Stand" && !File.Exists(tmp));

            File.WriteAllLines(Path.Combine(dir, "savetest.txt"), lines);
            return all ? 0 : 1;
        }

        // ---------------------------------------------------------------- Programmsymbol

        static void MakeIcon(string path)
        {
            int[] sizes = { 16, 20, 24, 32, 40, 48, 64, 256 };
            var images = new List<byte[]>();
            foreach (int size in sizes) images.Add(size >= 256 ? Png(Logo(size)) : Dib(Logo(size)));

            using (var bw = new BinaryWriter(File.Create(path)))
            {
                bw.Write((short)0); bw.Write((short)1); bw.Write((short)sizes.Length);
                int offset = 6 + 16 * sizes.Length;
                for (int i = 0; i < sizes.Length; i++)
                {
                    byte dim = (byte)(sizes[i] >= 256 ? 0 : sizes[i]);
                    bw.Write(dim); bw.Write(dim); bw.Write((byte)0); bw.Write((byte)0);
                    bw.Write((short)1); bw.Write((short)32);
                    bw.Write(images[i].Length); bw.Write(offset);
                    offset += images[i].Length;
                }
                foreach (var img in images) bw.Write(img);
            }
        }

        /// <summary>Das Rad in klein: Graphit-Scheibe, vier Tasten, die obere leuchtet orange.</summary>
        static BitmapSource Logo(int size)
        {
            bool tiny = size <= 24;
            double outer = 124, rim = tiny ? 12 : 9, inner = tiny ? 44 : 50, gap = tiny ? 22 : 13, corner = tiny ? 6 : 11;
            var dv = new DrawingVisual();
            using (var dc = dv.RenderOpen())
            {
                double s = size / 256.0;
                dc.PushTransform(new MatrixTransform(s, 0, 0, s, size / 2.0, size / 2.0));
                dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(0x15, 0x16, 0x1A)), null, new Point(0, 0), outer, outer);
                for (int i = 0; i < 4; i++)
                {
                    double mid = -Math.PI / 2 + i * Math.PI / 2;
                    var g = WheelView.BuildKey(inner, outer - rim, mid - Math.PI / 4, mid + Math.PI / 4, gap, corner);
                    dc.DrawGeometry(new SolidColorBrush(i == 0 ? Color.FromRgb(0xFF, 0x8A, 0x3D) : Color.FromRgb(0x3A, 0x3D, 0x45)), null, g);
                }
                dc.Pop();
            }
            var rtb = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(dv);
            return rtb;
        }

        /// <summary>Synthetisches, deckendes Querformat-Bild – steht für ein Foto, das jemand einem Segment gibt.</summary>
        static BitmapSource Photo()
        {
            var dv = new DrawingVisual();
            using (var dc = dv.RenderOpen())
            {
                var sky = new LinearGradientBrush(Color.FromRgb(0x2F, 0x6F, 0xB5), Color.FromRgb(0xBF, 0xDD, 0xF2), 90);
                dc.DrawRectangle(sky, null, new Rect(0, 0, 96, 64));
                dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(0xFF, 0xE0, 0x8A)), null, new Point(70, 20), 9, 9);
                dc.DrawGeometry(new SolidColorBrush(Color.FromRgb(0x2E, 0x7D, 0x4F)), null,
                                Geometry.Parse("M0 64 L0 44 L26 26 L48 46 L66 34 L96 52 L96 64 Z"));
            }
            var rtb = new RenderTargetBitmap(96, 64, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(dv);
            return rtb;
        }

        static byte[] Png(BitmapSource bmp)
        {
            var enc = new PngBitmapEncoder();
            enc.Frames.Add(BitmapFrame.Create(bmp));
            using (var ms = new MemoryStream())
            {
                enc.Save(ms);
                return ms.ToArray();
            }
        }

        /// <summary>Klassischer Symbol-Eintrag: BITMAPINFOHEADER, 32-Bit-Pixel von unten nach oben, leere Maske.</summary>
        static byte[] Dib(BitmapSource bmp)
        {
            var src = new FormatConvertedBitmap(bmp, PixelFormats.Bgra32, null, 0);
            int n = src.PixelWidth, stride = n * 4;
            var px = new byte[stride * n];
            src.CopyPixels(px, stride, 0);
            int maskStride = ((n + 31) / 32) * 4;
            using (var ms = new MemoryStream())
            using (var bw = new BinaryWriter(ms))
            {
                bw.Write(40); bw.Write(n); bw.Write(n * 2); bw.Write((short)1); bw.Write((short)32);
                bw.Write(0); bw.Write(stride * n + maskStride * n); bw.Write(0); bw.Write(0); bw.Write(0); bw.Write(0);
                for (int y = n - 1; y >= 0; y--) bw.Write(px, y * stride, stride);
                bw.Write(new byte[maskStride * n]);
                bw.Flush();
                return ms.ToArray();
            }
        }

        // ---------------------------------------------------------------- Sichtprüfung

        static void Save(Visual v, int w, int h, string path)
        {
            var rtb = new RenderTargetBitmap(w, h, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(v);
            File.WriteAllBytes(path, Png(rtb));
        }

        /// <summary>Rad über einer hellen, textdichten Fläche – so wie es über der Praxissoftware steht.</summary>
        static void RenderWheel(string path, List<Slot> slots, string title, int hover, double pointer, bool pointerOn,
                                Skin skin = null, bool desktop = false, Vector? gaze = null)
        {
            const int w = 760, h = 640;
            var view = new WheelView { Width = w, Height = h };
            view.Skin = skin ?? Skin.Graphit;
            view.SetContent(slots, title, "Mitte bricht ab");
            view.Hover = hover;
            view.PointerOn = pointerOn;
            view.PointerAngle = pointer;
            view.GazeTarget = gaze ?? (pointerOn ? new Vector(Math.Cos(pointer) * 160, Math.Sin(pointer) * 160) : new Vector(0, 0));
            for (int i = 0; i < 40; i++) view.Tick(0.05);

            var back = new DrawingVisual();
            using (var dc = back.RenderOpen())
            {
                if (desktop)
                {
                    // Bunter Schreibtisch mit Fenstern – darauf zeigt sich, ob das Milchglas trägt.
                    var sky = new LinearGradientBrush(Color.FromRgb(0x1D, 0x3B, 0x6E), Color.FromRgb(0xE0, 0x7A, 0x4F), 35);
                    dc.DrawRectangle(sky, null, new Rect(0, 0, w, h));
                    dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(0xFF, 0xD1, 0x66)), null, new Point(520, 190), 90, 90);
                    dc.DrawRectangle(Brushes.White, null, new Rect(40, 60, 330, 420));
                    dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(0x2B, 0x6C, 0xB0)), null, new Rect(40, 60, 330, 30));
                    var ink = new SolidColorBrush(Color.FromRgb(0x30, 0x34, 0x3B));
                    var rnd2 = new Random(3);
                    for (int y = 110; y < 470; y += 20) dc.DrawRectangle(ink, null, new Rect(56, y, 120 + rnd2.Next(170), 6));
                    dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(0x2E, 0x7D, 0x4F)), null, new Rect(430, 380, 300, 220));
                }
                else
                {
                    dc.DrawRectangle(Brushes.White, null, new Rect(0, 0, w, h));
                    var line = new SolidColorBrush(Color.FromRgb(0xC9, 0xCD, 0xD3));
                    var rnd = new Random(7);
                    for (int y = 28; y < h - 10; y += 22)
                        dc.DrawRectangle(line, null, new Rect(32, y, 320 + rnd.Next(380), 7));
                }
            }
            var ground = Capture(back, w, h);
            if (view.Skin.Decor == Decor.Frost)
            {
                view.Backdrop = Backdrop.FromImage(ground);
                view.BackdropRect = new Rect(0, 0, w, h);
            }
            var host = new Grid { Width = w, Height = h };
            host.Children.Add(new Image { Source = ground });
            host.Children.Add(view);
            host.Measure(new Size(w, h));
            host.Arrange(new Rect(0, 0, w, h));
            host.UpdateLayout();
            Save(host, w, h, path);
        }

        /// <summary>
        /// Wechsel ins Unterrad als Filmstreifen, so wie ihn das Overlay zeigt: zwei Ansichten (zwei Fenster), das alte Rad
        /// tritt an seinem Platz zurück, das neue wächst am Zeiger.
        /// </summary>
        static void RenderTransition(string path, List<Slot> slots, Skin skin)
        {
            const int w = 1120, h = 700, folder = 6;
            double shift = WheelView.OuterFor(slots.Count) - 10;
            var old = new WheelView { Width = w, Height = h, Skin = skin, RenderTransform = new TranslateTransform(shift, 0) };
            old.SetContent(slots, "Befunde", "Mitte bricht ab");
            old.Hover = folder;
            for (int i = 0; i < 10; i++) old.Tick(0.05);
            var sub = new WheelView { Width = w, Height = h, Skin = skin, Deeper = true, Open = 0 };
            sub.SetContent(slots[folder].Action.Slots, slots[folder].DisplayLabel, "Mitte bricht ab");
            sub.ResetDecor();
            var host = new Grid { Width = w, Height = h, Background = Brushes.White };
            host.Children.Add(old);
            host.Children.Add(sub);

            var frames = new List<BitmapSource>();
            double[] at = { 0.02, 0.05, 0.08, 0.11, 0.14, 0.17 };
            double t = 0;
            foreach (double target in at)
            {
                old.Tick(target - t);
                sub.Tick(target - t);
                t = target;
                old.Recede = Math.Min(1, t / WheelView.RecedeSeconds);
                sub.Open = Math.Min(1, t / 0.17);
                old.InvalidateVisual();
                sub.InvalidateVisual();
                host.Measure(new Size(w, h));
                host.Arrange(new Rect(0, 0, w, h));
                host.UpdateLayout();
                frames.Add(Capture(host, w, h));
            }
            var strip = new DrawingVisual();
            const double k = 0.42;
            using (var dc = strip.RenderOpen())
                for (int i = 0; i < frames.Count; i++) dc.DrawImage(frames[i], new Rect(i * w * k, 0, w * k, h * k));
            Save(strip, (int)(frames.Count * w * k), (int)(h * k), path);
        }

        static BitmapSource Capture(Visual v, int w, int h)
        {
            var rtb = new RenderTargetBitmap(w, h, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(v);
            return rtb;
        }

        static void IconSheet(string path)
        {
            var all = new List<string[]>();
            foreach (var section in Icons.Sections) all.AddRange(section.Value);
            // Kandidaten, die noch nicht im Katalog stehen – zum Prüfen der Zeichencodes.
            foreach (string c in new[] { "E80C", "E80D", "E7AD", "E76B", "E76C", "E70D", "E70E", "E72B", "E72A", "E8AB", "E8EE", "E7C3",
                                         "E8F8", "E82D", "E95E", "EB51", "E9D9", "E8D7", "E7FC", "E8C9", "E712", "E700" })
                all.Add(new[] { "g:" + c, c });

            const int cols = 10, cw = 104, ch = 82;
            int rows = (all.Count + cols - 1) / cols;
            var dv = new DrawingVisual();
            var face = new Typeface("Segoe UI");
            using (var dc = dv.RenderOpen())
            {
                dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(0x26, 0x28, 0x2E)), null, new Rect(0, 0, cols * cw, rows * ch));
                for (int i = 0; i < all.Count; i++)
                {
                    double x = (i % cols) * cw, y = (i / cols) * ch;
                    Icons.Draw(dc, all[i][0], null, new Rect(x + cw / 2 - 14, y + 12, 28, 28), Brushes.White, 1);
                    var ft = new FormattedText(all[i][1] + "\n" + all[i][0], CultureInfo.InvariantCulture, FlowDirection.LeftToRight, face, 10,
                                               Brushes.Silver, null, TextFormattingMode.Display, 1);
                    ft.TextAlignment = TextAlignment.Center;
                    ft.MaxTextWidth = cw;
                    dc.DrawText(ft, new Point(x, y + 46));
                }
            }
            Save(dv, cols * cw, rows * ch, path);
        }

        static void RenderSettings(string dir)
        {
            var host = new AppHost { Config = ConfigStore.Default(), ConfigPath = Path.Combine(dir, "Kyklos.json") };
            var sw = new SettingsWindow(host);
            var win = sw.Window;
            win.WindowStartupLocation = WindowStartupLocation.Manual;
            win.Left = -30000;
            win.Top = -30000;
            win.ShowInTaskbar = false;
            win.ShowActivated = false;
            win.Show();
            Shot(win, Path.Combine(dir, "settings-text.png"));

            sw.DevSelect(4);
            sw.DevScrollInspector();
            Shot(win, Path.Combine(dir, "settings-variants.png"));


            sw.DevSelect(6);
            Shot(win, Path.Combine(dir, "settings-folder.png"));
            sw.DevEnterFolder();
            sw.DevSelect(2);
            Shot(win, Path.Combine(dir, "settings-macro.png"));
            sw.ShowGeneral();
            Pump(400);      // Schalter-Animation auslaufen lassen
            Shot(win, Path.Combine(dir, "settings-general.png"));
            sw.DevScrollGeneral();
            Shot(win, Path.Combine(dir, "settings-general-end.png"));

            win.Width = 1080;
            win.Height = 680;
            sw.DevHome();
            Shot(win, Path.Combine(dir, "settings-min.png"));
            host.Config.Wheels[0].Trigger2 = Chord.Key(0x20, ctrl: true);     // Strg + Leertaste
            sw.DevHome();
            Shot(win, Path.Combine(dir, "settings-trigger2-min.png"));
            win.Width = 1220;
            win.Height = 800;
            sw.DevHome();
            Shot(win, Path.Combine(dir, "settings-trigger2.png"));
            win.Close();
        }

        static void Pump(int ms)
        {
            var frame = new DispatcherFrame();
            var t = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(ms) };
            t.Tick += (s, e) => { t.Stop(); frame.Continue = false; };
            t.Start();
            Dispatcher.PushFrame(frame);
        }

        static void Shot(Window win, string path)
        {
            win.Dispatcher.Invoke(DispatcherPriority.ApplicationIdle, new Action(() => { }));
            var root = (FrameworkElement)win.Content;
            int w = (int)Math.Ceiling(root.ActualWidth), h = (int)Math.Ceiling(root.ActualHeight);
            // Der Fensterhintergrund gehört nicht zum Inhaltsbaum – ohne ihn bliebe die Arbeitsfläche durchsichtig.
            var ground = new DrawingVisual();
            using (var dc = ground.RenderOpen()) dc.DrawRectangle(win.Background, null, new Rect(0, 0, w, h));
            var rtb = new RenderTargetBitmap(w, h, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(ground);
            rtb.Render(root);
            File.WriteAllBytes(path, Png(rtb));
        }

        static void Render(string dir)
        {
            Directory.CreateDirectory(dir);
            var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            Theme.Apply(app);

            var cfg = ConfigStore.Default();
            var main = cfg.Wheels[0];
            RenderWheel(Path.Combine(dir, "wheel-idle.png"), main.Slots, main.Name, -1, 0, false);
            RenderWheel(Path.Combine(dir, "wheel-hover.png"), main.Slots, main.Name, 0, -Math.PI / 2 + 0.12, true);
            RenderWheel(Path.Combine(dir, "wheel-hover-folder.png"), main.Slots, main.Name, 6, Math.PI, true);
            RenderWheel(Path.Combine(dir, "wheel-sub.png"), main.Slots[6].Action.Slots, "Werkzeuge", 2, Math.PI / 6, true);

            var many = new List<Slot>();
            string[] names = { "Lunge", "Herz", "Abdomen", "HNO", "Wirbelsäule", "Neurologie", "Haut", "Schilddrüse", "Lymphknoten", "Gefäße", "Psyche", "" };
            for (int i = 0; i < 12; i++)
                many.Add(new Slot
                {
                    Label = names[i], Color = Palette.Colors[i % 8][0], Icon = i % 3 == 0 ? Icons.Medical[i % Icons.Medical.Length][0] : "",
                    Action = new ActionDef { Type = names[i].Length > 0 ? ActionType.Text : ActionType.None, Text = "Beispieltext für " + names[i] }
                });
            RenderWheel(Path.Combine(dir, "wheel-12.png"), many, "Untersuchung", 4, Math.PI / 6, true);
            RenderWheel(Path.Combine(dir, "wheel-4.png"), many.GetRange(0, 4), "Kurz", 1, 0, true);

            // Alle Beschriftungsarten: Foto mit und ohne Text, freigestelltes Bild, nur Symbol, nur Text, Symbol mit Text.
            string photo = Convert.ToBase64String(Png(Photo())), logo = Convert.ToBase64String(Png(Logo(96)));
            var kinds = new List<Slot>
            {
                new Slot { Label = "Praxis", Image = photo, Color = Palette.Colors[4][0], Action = new ActionDef { Type = ActionType.Open, Path = @"C:\Praxis\Praxis.exe" } },
                new Slot { Image = photo, Color = Palette.Colors[1][0], Action = new ActionDef { Type = ActionType.Url, Url = "https://www.example.org/portal" } },
                new Slot { Image = logo, Color = Palette.Colors[5][0], Action = new ActionDef { Type = ActionType.Open, Path = @"C:\Tools\Kyklos.exe" } },
                new Slot { Icon = "v:herz", Color = Palette.Colors[6][0], Action = new ActionDef { Type = ActionType.Text, Text = "Cor: Herztöne rein und rhythmisch." } },
                new Slot { Label = "Nur Text", Color = Palette.Colors[2][0], Action = new ActionDef { Type = ActionType.Text, Text = "Ein Baustein ohne Symbol." } },
                new Slot { Label = "Stethoskop", Icon = "v:stethoskop", Color = Palette.Colors[0][0], Action = new ActionDef { Type = ActionType.Text, Text = "Auskultation unauffällig." } }
            };
            RenderWheel(Path.Combine(dir, "wheel-kinds-idle.png"), kinds, "Arten", -1, 0, false);
            RenderWheel(Path.Combine(dir, "wheel-kinds-image.png"), kinds, "Arten", 0, -Math.PI / 2, true);
            RenderWheel(Path.Combine(dir, "wheel-kinds-imageonly.png"), kinds, "Arten", 1, -Math.PI / 6, true);

            foreach (var sk in Skin.All)
            {
                if (sk == Skin.Graphit) continue;
                RenderWheel(Path.Combine(dir, "skin-" + sk.Id + "-idle.png"), main.Slots, main.Name, -1, 0, false, sk, false, new Vector(90, 40));
                RenderWheel(Path.Combine(dir, "skin-" + sk.Id + "-hover.png"), main.Slots, main.Name, 1, -Math.PI / 4 + 0.1, true, sk);
                RenderWheel(Path.Combine(dir, "skin-" + sk.Id + "-desk.png"), main.Slots, main.Name, 6, Math.PI, true, sk, true);
                FillShot(Path.Combine(dir, "skin-" + sk.Id + "-fill.png"), "Blutdruck {?Wert} mmHg, Puls {?Rhythmus: regelmäßig | unregelmäßig}.",
                         "Blutdruck", Palette.Colors[1][0], g => { g[0].Value = "130/85"; g[1].Picked[0] = true; }, 0, sk);
            }

            RenderTransition(Path.Combine(dir, "transition.png"), main.Slots, Skin.Graphit);
            RenderTransition(Path.Combine(dir, "transition-halloween.png"), main.Slots, Skin.Halloween);

            IconSheet(Path.Combine(dir, "icons.png"));
            RenderFill(dir);
            RenderSettings(dir);
        }

        /// <summary>Lückenabfrage über heller Fläche: leer, teils ausgefüllt, und mit sehr vielen Optionen.</summary>
        static void RenderFill(string dir)
        {
            const string cold = "Erkältungssymptome seit {?Tage} Tagen mit {?Symptome: Husten | Schnupfen | Heiserkeit | Kopfschmerzen | Gliederschmerzen | Fieber}. " +
                                "Kein Kontakt zu Erkrankten bekannt. Vorstellung am {datum}.";
            FillShot(Path.Combine(dir, "fill-empty.png"), cold, "Erkältung", Palette.Colors[3][0], null, 0);
            FillShot(Path.Combine(dir, "fill-filled.png"), cold, "Erkältung", Palette.Colors[3][0], g =>
            {
                g[0].Value = "3";
                g[1].Picked[0] = g[1].Picked[1] = g[1].Picked[3] = true;
            }, 1);
            FillShot(Path.Combine(dir, "fill-single.png"), "Blutdruck {?Wert} mmHg, Puls regelmäßig.", "", Palette.Default, null, 0);
            const string tick = "Zeckenstich vor {?Tage} Tagen. FSME-Impfung aktuell: {?FSME-Impfung: ja / nein / unbekannt}. " +
                                "Dosis {?Dosis: 1/2 / 1 / 2} Tabletten.";
            FillShot(Path.Combine(dir, "fill-pick.png"), tick, "Zeckenstich", Palette.Colors[5][0], g =>
            {
                g[0].Value = "2";
                g[1].Picked[2] = true;
            }, 1);
        }

        static void FillShot(string path, string text, string title, string color, Action<List<Gaps.Gap>> fill, int focus, Skin skin = null)
        {
            var win = FillWindow.DevBuild(text, title, Palette.Parse(color), fill, focus, skin);
            win.ShowActivated = false;
            win.Show();
            win.Dispatcher.Invoke(DispatcherPriority.ApplicationIdle, new Action(() => { }));
            var root = (FrameworkElement)win.Content;
            int w = (int)Math.Ceiling(root.ActualWidth + root.Margin.Left + root.Margin.Right),
                h = (int)Math.Ceiling(root.ActualHeight + root.Margin.Top + root.Margin.Bottom);
            var ground = new DrawingVisual();
            using (var dc = ground.RenderOpen())
            {
                dc.DrawRectangle(Brushes.White, null, new Rect(0, 0, w, h));
                var line = new SolidColorBrush(Color.FromRgb(0xC9, 0xCD, 0xD3));
                var rnd = new Random(3);
                for (int y = 14; y < h - 10; y += 22) dc.DrawRectangle(line, null, new Rect(16, y, 200 + rnd.Next(320), 7));
            }
            var rtb = new RenderTargetBitmap(w, h, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(ground);
            rtb.Render(root);
            File.WriteAllBytes(path, Png(rtb));
            win.Close();
        }
    }
}
