using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Kyklos
{
    /// <summary>
    /// Galerie: zeigt die Räder aus dem Ordner galerie/ des Kyklos-Repos. Die Liste (index.json) und jedes Rad werden
    /// beim Öffnen geladen; die Vorschau zeichnet die App selbst aus den Rad-Daten. Was ein fremdes Rad tut (Programme
    /// starten, Websites öffnen, Makros), ermittelt die App aus dem Rad und nicht aus der Liste – vor dem Hinzufügen
    /// steht es im Klartext da.
    /// </summary>
    public sealed class Gallery
    {
        // Zum Ausprobieren vor dem Veröffentlichen: Umgebungsvariable KYKLOS_GALERIE auf einen Ordner oder eine Adresse setzen.
        const string DefaultBase = "https://raw.githubusercontent.com/lollylan/Kyklos/main/galerie/";
        const int MaxBytes = 256 * 1024, MaxEntries = 200, TimeoutMs = 20000;

        sealed class Entry
        {
            public string File = "", Name = "", Author = "", Description = "", Program = "";
            public string Raw;
            public Wheel First;
            public List<string> Details = new List<string>();
            public string Summary = "";
            public FrameworkElement Card;
            public Image Pic;
            public Button AddBtn;
            public TextBlock Note;
        }

        readonly Window _w;
        readonly Func<List<Wheel>, int> _add;
        readonly Skin _skin;
        readonly string _base;
        readonly List<Entry> _entries = new List<Entry>();
        WrapPanel _grid;
        TextBlock _status;
        TextBox _search;
        bool _closed;
        readonly string _addText, _doneText;
        readonly bool _closeAfter;

        static Gallery _open;

        /// <summary>Öffnet die Galerie – oder holt die schon offene nach vorn. <paramref name="add"/> hängt Räder an und liefert, wie viele.</summary>
        public static Gallery Show(Window owner, Skin skin, Func<List<Wheel>, int> add,
                                   string addText = "Hinzufügen", string doneText = "Hinzugefügt", bool closeAfter = false)
        {
            if (_open != null)
            {
                if (_open._addText == addText) { _open._w.Activate(); return _open; }
                _open._w.Close();   // andere Aufgabe (hinzufügen oder einsetzen): neu öffnen
            }
            _open = new Gallery(owner, skin, add, addText, doneText, closeAfter);
            _open._w.Show();
            _open.Load();
            return _open;
        }

        public Window Window { get { return _w; } }
        public int Loaded { get { return _entries.Count(x => x.Card != null); } }
        public string Status { get { return _status.Text; } }

        Gallery(Window owner, Skin skin, Func<List<Wheel>, int> add, string addText, string doneText, bool closeAfter)
        {
            _add = add;
            _addText = addText; _doneText = doneText; _closeAfter = closeAfter;
            _skin = skin != null && skin.Decor != Decor.Frost ? skin : Skin.Graphit;   // Milchglas braucht den Bildschirm dahinter
            string b = Environment.GetEnvironmentVariable("KYKLOS_GALERIE");
            if (string.IsNullOrWhiteSpace(b)) b = DefaultBase;
            _base = b.EndsWith("/") || b.EndsWith("\\") ? b : b + "/";

            _w = new Window
            {
                Title = "Räder aus dem Internet",
                Owner = owner, WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Width = 920, Height = 700, MinWidth = 520, MinHeight = 420,
                Background = Brush("Bg"), FontFamily = new FontFamily("Segoe UI"), UseLayoutRounding = true,
                Icon = owner == null ? null : owner.Icon
            };
            TextOptions.SetTextFormattingMode(_w, TextFormattingMode.Display);
            _w.Closed += (s, e) => { _closed = true; if (_open == this) _open = null; };

            var root = new Grid();
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var head = new Grid { Margin = new Thickness(0, 0, 0, 14) };
            head.ColumnDefinitions.Add(new ColumnDefinition());
            head.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var titles = new StackPanel();
            titles.Children.Add(new TextBlock { Text = "Räder aus dem Internet", FontSize = 20, FontWeight = FontWeights.SemiBold, Foreground = Brush("Ink") });
            titles.Children.Add(new TextBlock
            {
                Text = "Räder anderer Anwender. Vor dem Hinzufügen zeigt Kyklos, was ein Rad ausführt.",
                Style = (Style)Application.Current.FindResource("Help"), Margin = new Thickness(0, 4, 0, 0), TextWrapping = TextWrapping.Wrap
            });
            head.Children.Add(titles);
            _search = new TextBox { Width = 220, ToolTip = "Suchen nach Name, Autor, Programm" };
            _search.TextChanged += (s, e) => Filter();
            var find = new StackPanel { Margin = new Thickness(16, 0, 0, 0), VerticalAlignment = VerticalAlignment.Bottom };
            find.Children.Add(new TextBlock { Text = "Suchen", Style = (Style)Application.Current.FindResource("Help"), Margin = new Thickness(1, 0, 0, 4) });
            find.Children.Add(_search);
            Grid.SetColumn(find, 1);
            head.Children.Add(find);
            root.Children.Add(head);

            _grid = new WrapPanel { Orientation = Orientation.Horizontal };
            var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Content = _grid, Focusable = false };
            Grid.SetRow(scroll, 1);
            root.Children.Add(scroll);

            _status = new TextBlock { Style = (Style)Application.Current.FindResource("Help"), Margin = new Thickness(0, 10, 0, 0), Text = "Lade die Liste …" };
            Grid.SetRow(_status, 2);
            root.Children.Add(_status);

            _w.Content = new Border { Padding = new Thickness(24, 20, 24, 16), Child = root };
        }

        static Brush Brush(string key) { return (Brush)Application.Current.FindResource(key); }

        async void Load()
        {
            try
            {
                string text = await Fetch(_base + "index.json");
                var d = Json.Parse(text) as Dictionary<string, object>;
                if (d == null) throw new InvalidDataException("index.json hat kein gültiges Format.");
                foreach (var o in Json.A(d, "raeder"))
                {
                    var e = o as Dictionary<string, object>;
                    if (e == null || _entries.Count >= MaxEntries) continue;
                    string file = Json.S(e, "datei");
                    if (!SafeName(file)) continue;
                    _entries.Add(new Entry
                    {
                        File = file, Name = Json.S(e, "name", file), Author = Json.S(e, "autor"),
                        Description = Json.S(e, "beschreibung"), Program = Json.S(e, "programm")
                    });
                }
            }
            catch (Exception ex)
            {
                if (_closed) return;
                _status.Text = Offline(ex);
                return;
            }
            if (_closed) return;
            if (_entries.Count == 0) { _status.Text = "Die Galerie ist noch leer."; return; }
            _status.Text = _entries.Count == 1 ? "1 Rad wird geladen …" : _entries.Count + " Räder werden geladen …";

            int failed = 0;
            foreach (var en in _entries)
            {
                if (_closed) return;
                try
                {
                    en.Raw = await Fetch(_base + "raeder/" + Uri.EscapeDataString(en.File));
                    var wheels = Parse(en.Raw);
                    en.First = wheels[0];
                    Describe(en, wheels);
                    AddCard(en);
                }
                catch (Exception) { failed++; }
            }
            if (_closed) return;
            Filter();
            _status.Text = failed == 0 ? "" : failed + (failed == 1 ? " Rad" : " Räder") + " konnte(n) nicht geladen werden.";
        }

        static string Offline(Exception ex)
        {
            if (ex is WebException || ex is TimeoutException)
                return "Die Galerie ist gerade nicht erreichbar. Bitte die Internetverbindung prüfen und später noch einmal öffnen.";
            return "Die Galerie konnte nicht gelesen werden: " + ex.Message;
        }

        /// <summary>Nur ein einfacher Dateiname – nichts, was aus dem Galerie-Ordner hinausführt.</summary>
        static bool SafeName(string f)
        {
            return !string.IsNullOrEmpty(f) && f.EndsWith(".json", StringComparison.OrdinalIgnoreCase) && f.Length < 120 &&
                   f.IndexOfAny(new[] { '/', '\\', ':', '?', '#', '%' }) < 0 && !f.Contains("..");
        }

        static async Task<string> Fetch(string url)
        {
            try { System.Net.ServicePointManager.SecurityProtocol |= (SecurityProtocolType)3072; }   // TLS 1.2
            catch (Exception) { }
            using (var wc = new WebClient { Encoding = Encoding.UTF8 })
            {
                wc.Headers[HttpRequestHeader.UserAgent] = "Kyklos";
                wc.CachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.BypassCache);
                var t = wc.DownloadStringTaskAsync(new Uri(url));
                if (await Task.WhenAny(t, Task.Delay(TimeoutMs)) != t)
                {
                    wc.CancelAsync();
                    var ignore = t.ContinueWith(x => { var _ = x.Exception; });
                    throw new TimeoutException();
                }
                string s = await t;
                if (s.Length > MaxBytes) throw new InvalidDataException("Die Datei ist zu groß.");
                return s;
            }
        }

        static List<Wheel> Parse(string raw)
        {
            var wheels = Config.FromJson(Json.Parse(raw) as Dictionary<string, object>).Wheels;
            if (wheels.Count == 0) throw new FormatException("Die Datei enthält keine Räder.");
            return wheels;
        }

        // Was ein Rad tut, steht in seinen Aktionen. Die Liste in index.json ist nur Beschriftung und sagt dazu nichts.
        static void Describe(Entry en, List<Wheel> wheels)
        {
            foreach (var w in wheels) Scan(w.Slots, en.Details);
            en.Details = en.Details.Distinct().ToList();
            bool prog = false, web = false, macro = false;
            foreach (var w in wheels) Flags(w.Slots, ref prog, ref web, ref macro);
            var parts = new List<string>();
            if (prog) parts.Add("Startet Programme");
            if (web) parts.Add("Öffnet Websites");
            if (macro) parts.Add("Enthält Makros");
            en.Summary = parts.Count == 0 ? "Nur Text, Tasten und Medien" : string.Join(", ", parts);
        }

        static void Flags(IEnumerable<Slot> slots, ref bool prog, ref bool web, ref bool macro)
        {
            foreach (var s in slots) Flags(s.Action, ref prog, ref web, ref macro);
        }

        static void Flags(ActionDef a, ref bool prog, ref bool web, ref bool macro)
        {
            switch (a.Type)
            {
                case ActionType.Open: prog = true; break;
                case ActionType.Focus: if (a.Launch) prog = true; break;
                case ActionType.Url: web = true; break;
                case ActionType.Click: macro = true; break;
                case ActionType.Macro:
                    macro = true;
                    foreach (var st in a.Steps) Flags(st, ref prog, ref web, ref macro);
                    break;
                case ActionType.Folder: Flags(a.Slots, ref prog, ref web, ref macro); break;
            }
        }

        static void Scan(IEnumerable<Slot> slots, List<string> found)
        {
            foreach (var s in slots) Scan(s.Action, found);
        }

        static void Scan(ActionDef a, List<string> found)
        {
            switch (a.Type)
            {
                case ActionType.Open:
                    found.Add("startet „" + (a.Path ?? "").Trim() + (string.IsNullOrWhiteSpace(a.Args) ? "" : " " + a.Args.Trim()) + "“");
                    break;
                case ActionType.Focus:
                    if (a.Launch) found.Add("startet bei Bedarf „" + (a.Path ?? "").Trim() + "“");
                    break;
                case ActionType.Url: found.Add("öffnet die Website " + (a.Url ?? "").Trim()); break;
                case ActionType.Click: found.Add("klickt mit der Maus auf eine feste Bildschirmposition"); break;
                case ActionType.Macro:
                    found.Add("führt ein Makro mit " + a.Steps.Count + (a.Steps.Count == 1 ? " Schritt" : " Schritten") + " aus");
                    foreach (var st in a.Steps) Scan(st, found);
                    break;
                case ActionType.Folder: Scan(a.Slots, found); break;
            }
        }

        BitmapSource Preview(Wheel w)
        {
            double size = 2 * (WheelView.OuterFor(w.Slots.Count) + WheelView.PreviewPad + 24);   // Luft, damit der Schatten nicht abgeschnitten wird
            int px = (int)Math.Ceiling(size);
            var view = new WheelView { Width = px, Height = px, Motion = false };
            view.Skin = _skin;
            view.SetContent(w.Slots, w.Name, "");
            for (int i = 0; i < 10; i++) view.Tick(0.05);
            view.Measure(new Size(px, px));
            view.Arrange(new Rect(0, 0, px, px));
            view.UpdateLayout();
            var rtb = new RenderTargetBitmap(px, px, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(view);
            rtb.Freeze();
            return rtb;
        }

        void AddCard(Entry en)
        {
            var style = (Style)Application.Current.FindResource("Help");
            var col = new StackPanel();
            var pic = new Image { Width = 216, Height = 216, Stretch = Stretch.Uniform, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 0, 0, 8) };
            try { pic.Source = Preview(en.First); } catch (Exception) { }   // ohne Vorschau geht es auch
            RenderOptions.SetBitmapScalingMode(pic, BitmapScalingMode.HighQuality);
            col.Children.Add(pic);
            col.Children.Add(new TextBlock { Text = en.Name, FontSize = 14, FontWeight = FontWeights.SemiBold, Foreground = Brush("Ink"), TextTrimming = TextTrimming.CharacterEllipsis });
            string by = string.Join(" · ", new[] { en.Author, en.Program }.Where(x => !string.IsNullOrWhiteSpace(x)));
            if (by.Length > 0) col.Children.Add(new TextBlock { Text = by, Style = style, TextTrimming = TextTrimming.CharacterEllipsis, Margin = new Thickness(0, 2, 0, 0) });
            // Feste Höhe für drei Zeilen, damit die Schaltflächen aller Karten einer Reihe auf einer Linie stehen.
            col.Children.Add(new TextBlock { Text = en.Description, Style = style, TextWrapping = TextWrapping.Wrap, TextTrimming = TextTrimming.CharacterEllipsis, Height = 56, Margin = new Thickness(0, 6, 0, 0), ToolTip = en.Description });
            var note = new TextBlock { Text = en.Summary, Style = style, Margin = new Thickness(0, 8, 0, 0), TextTrimming = TextTrimming.CharacterEllipsis };
            col.Children.Add(note);
            var btn = new Button { Content = _addText, Style = (Style)Application.Current.FindResource("BtnPrimary"), Margin = new Thickness(0, 10, 0, 0), HorizontalAlignment = HorizontalAlignment.Stretch };
            btn.Click += (s, e) => Add(en);
            col.Children.Add(btn);

            var card = new Border
            {
                Width = 248, Margin = new Thickness(0, 0, 14, 14), Padding = new Thickness(14),
                Background = Brush("Surface"), BorderBrush = Brush("Line"), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(8),
                Child = col
            };
            en.Card = card; en.Pic = pic; en.AddBtn = btn; en.Note = note;
            _grid.Children.Add(card);
        }

        void Add(Entry en)
        {
            List<Wheel> wheels;
            try { wheels = Parse(en.Raw); }   // jedes Mal frisch, damit zwei Hinzufügungen nicht dasselbe Objekt teilen
            catch (Exception ex)
            {
                MessageBox.Show(_w, "Das Rad ist beschädigt:\n\n" + ex.Message, "Kyklos", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (en.Details.Count > 0)
            {
                var sb = new StringBuilder();
                sb.Append("Das Rad „").Append(en.Name).Append("“ stammt von ").Append(string.IsNullOrWhiteSpace(en.Author) ? "einem anderen Anwender" : en.Author)
                  .Append(" und führt aus:\n\n");
                foreach (var d in en.Details.Take(8)) sb.Append("•  ").Append(d).Append('\n');
                if (en.Details.Count > 8) sb.Append("•  … und ").Append(en.Details.Count - 8).Append(" weitere\n");
                sb.Append("\nNur hinzufügen, wenn du das nachvollziehen kannst und der Quelle traust.");
                if (MessageBox.Show(_w, sb.ToString(), "Rad hinzufügen", MessageBoxButton.OKCancel, MessageBoxImage.Warning, MessageBoxResult.Cancel) != MessageBoxResult.OK) return;
            }
            if (_add(wheels) <= 0) return;   // abgebrochen oder abgelehnt
            en.AddBtn.Content = _doneText;
            en.AddBtn.IsEnabled = false;
            if (_closeAfter) _w.Close();
        }

        void Filter()
        {
            string q = (_search.Text ?? "").Trim();
            int shown = 0;
            foreach (var en in _entries)
            {
                if (en.Card == null) continue;
                bool hit = q.Length == 0 || new[] { en.Name, en.Author, en.Program, en.Description }
                    .Any(x => (x ?? "").IndexOf(q, StringComparison.CurrentCultureIgnoreCase) >= 0);
                en.Card.Visibility = hit ? Visibility.Visible : Visibility.Collapsed;
                if (hit) shown++;
            }
            if (q.Length > 0 && shown == 0 && _entries.Any(x => x.Card != null)) _status.Text = "Kein Rad passt zu „" + q + "“.";
            else if (_status.Text.StartsWith("Kein Rad passt")) _status.Text = "";
        }
    }
}
