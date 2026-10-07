using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Kyklos
{
    public enum ActionType { None, Text, Keys, Open, Url, Media, Macro, Folder, Delay, Click }
    public enum TextMode { Paste, Type }

    /// <summary>Taste oder Maustaste samt Zusatztasten – als Auslöser eines Rads oder als zu sendende Kombination.</summary>
    public sealed class Chord
    {
        public bool Mouse;          // true: Code ist eine Maustaste (1 = Mitte, 2 = Seitentaste 1, 3 = Seitentaste 2)
        public int Code;            // virtueller Tastencode oder Maustaste
        public bool Ctrl, Alt, Shift, Win;

        public bool IsEmpty { get { return Code == 0; } }

        public bool SameAs(Chord o)
        {
            return o != null && Mouse == o.Mouse && Code == o.Code && Ctrl == o.Ctrl && Alt == o.Alt && Shift == o.Shift && Win == o.Win;
        }

        public string Display()
        {
            if (IsEmpty) return "Nicht belegt";
            var p = new List<string>();
            if (Ctrl) p.Add("Strg");
            if (Alt) p.Add("Alt");
            if (Shift) p.Add("Umschalt");
            if (Win) p.Add("Win");
            string name = Mouse ? KeyNames.MouseName(Code) : KeyNames.VkName(Code);
            // Ein einzelnes Zeichen wie ^ liest sich allein wie ein Pfeil – deshalb beim Namen nennen.
            if (!Mouse && p.Count == 0 && name.Length == 1) name = "Taste " + name;
            p.Add(name);
            return string.Join(" + ", p);
        }

        public Chord Clone() { return (Chord)MemberwiseClone(); }

        public Dictionary<string, object> ToJson()
        {
            var d = new Dictionary<string, object>();
            if (Mouse) d["mouse"] = true;
            d["code"] = Code;
            if (Ctrl) d["ctrl"] = true;
            if (Alt) d["alt"] = true;
            if (Shift) d["shift"] = true;
            if (Win) d["win"] = true;
            return d;
        }

        public static Chord FromJson(Dictionary<string, object> d)
        {
            return new Chord
            {
                Mouse = Json.B(d, "mouse"), Code = Json.I(d, "code"),
                Ctrl = Json.B(d, "ctrl"), Alt = Json.B(d, "alt"), Shift = Json.B(d, "shift"), Win = Json.B(d, "win")
            };
        }

        public static Chord Key(int vk, bool ctrl = false, bool alt = false, bool shift = false, bool win = false)
        {
            return new Chord { Code = vk, Ctrl = ctrl, Alt = alt, Shift = shift, Win = win };
        }
    }

    public sealed class ActionDef
    {
        public ActionType Type = ActionType.None;
        public string Text = "";
        public bool Sample;         // mitgelieferter Beispieltext, vom Nutzer noch nicht bestätigt oder geändert
        public TextMode Mode = TextMode.Paste;
        public Chord Keys = new Chord();
        public string Path = "", Args = "";
        public string Url = "";
        public string Media = "playpause";
        public List<ActionDef> Steps = new List<ActionDef>();
        public int StepDelayMs = 60;
        public int DelayMs = 250;
        public int X, Y;                    // Bildschirmpunkt in physischen Pixeln (Mausklick)
        public string Button = "left";
        public List<Slot> Slots = new List<Slot>();

        public static readonly string[][] MediaNames =
        {
            new[] { "playpause", "Wiedergabe / Pause" }, new[] { "next", "Nächster Titel" }, new[] { "prev", "Vorheriger Titel" },
            new[] { "stop", "Wiedergabe stoppen" }, new[] { "volup", "Lauter" }, new[] { "voldown", "Leiser" }, new[] { "mute", "Ton aus / an" },
            new[] { "snip", "Bildschirmausschnitt" }, new[] { "desktop", "Desktop anzeigen" }, new[] { "lock", "PC sperren" }
        };

        public static readonly string[][] ButtonNames =
        {
            new[] { "left", "Linksklick" }, new[] { "double", "Doppelklick" }, new[] { "right", "Rechtsklick" }, new[] { "middle", "Mittelklick" }
        };

        public static string ButtonName(string id)
        {
            foreach (var b in ButtonNames) if (b[0] == id) return b[1];
            return "Linksklick";
        }

        public static string MediaName(string id)
        {
            foreach (var m in MediaNames) if (m[0] == id) return m[1];
            return id;
        }

        public static string TypeName(ActionType t)
        {
            switch (t)
            {
                case ActionType.Text: return "Text einfügen";
                case ActionType.Keys: return "Tastenkombination";
                case ActionType.Open: return "Programm, Datei oder Ordner öffnen";
                case ActionType.Url: return "Website öffnen";
                case ActionType.Media: return "Medien und System";
                case ActionType.Macro: return "Makro (mehrere Schritte)";
                case ActionType.Folder: return "Unterrad";
                case ActionType.Delay: return "Pause";
                case ActionType.Click: return "Mausklick";
                default: return "Keine Aktion";
            }
        }

        /// <summary>Einzeiler für das Display in der Radmitte.</summary>
        public string Summary()
        {
            switch (Type)
            {
                case ActionType.Text:
                    var variants = Variants.Of(Text);
                    string t = Regex.Replace(Variants.Outline(Gaps.Outline(variants[0])).Replace("{cursor}", ""), @"\s+", " ").Trim();
                    if (t.Length == 0) return "Noch kein Text hinterlegt";
                    if (variants.Count > 1) t = variants.Count + " Varianten · " + t;
                    if (Sample) t = "Beispieltext: " + t;
                    return t.Length > 120 ? t.Substring(0, 118).TrimEnd() + " …" : t;
                case ActionType.Keys: return Keys.Display();
                case ActionType.Open:
                    if (string.IsNullOrWhiteSpace(Path)) return "Noch kein Ziel gewählt";
                    try { string f = System.IO.Path.GetFileName(Path.TrimEnd('\\', '/')); return f.Length > 0 ? f : Path; }
                    catch (ArgumentException) { return Path; }
                case ActionType.Url:
                    return string.IsNullOrWhiteSpace(Url) ? "Noch keine Adresse" : Regex.Replace(Url.Trim(), @"^https?://(www\.)?", "");
                case ActionType.Media: return MediaName(Media);
                case ActionType.Macro: return Steps.Count == 1 ? "Makro · 1 Schritt" : "Makro · " + Steps.Count + " Schritte";
                case ActionType.Folder:
                    int n = Slots.Count(s => !s.IsEmpty);
                    return n == 1 ? "Unterrad · 1 Eintrag" : "Unterrad · " + n + " Einträge";
                case ActionType.Delay: return "Pause " + DelayMs + " ms";
                case ActionType.Click: return ButtonName(Button) + " bei " + X + ", " + Y;
                default: return "Keine Aktion";
            }
        }

        public Dictionary<string, object> ToJson()
        {
            var d = new Dictionary<string, object>();
            d["type"] = Type.ToString().ToLowerInvariant();
            switch (Type)
            {
                case ActionType.Text:
                    d["text"] = Text ?? "";
                    if (Mode == TextMode.Type) d["mode"] = "type";
                    if (Sample) d["sample"] = true;
                    break;
                case ActionType.Keys: d["keys"] = Keys.ToJson(); break;
                case ActionType.Open:
                    d["path"] = Path ?? "";
                    if (!string.IsNullOrEmpty(Args)) d["args"] = Args;
                    break;
                case ActionType.Url: d["url"] = Url ?? ""; break;
                case ActionType.Media: d["media"] = Media; break;
                case ActionType.Macro:
                    d["stepDelayMs"] = StepDelayMs;
                    d["steps"] = Steps.Select(s => (object)s.ToJson()).ToList();
                    break;
                case ActionType.Folder: d["slots"] = Slots.Select(s => (object)s.ToJson()).ToList(); break;
                case ActionType.Delay: d["delayMs"] = DelayMs; break;
                case ActionType.Click:
                    d["x"] = X;
                    d["y"] = Y;
                    d["button"] = Button;
                    break;
            }
            return d;
        }

        public static ActionDef FromJson(Dictionary<string, object> d)
        {
            var a = new ActionDef();
            if (d == null) return a;
            ActionType t;
            if (Enum.TryParse(Json.S(d, "type", "none"), true, out t)) a.Type = t;
            a.Text = Json.S(d, "text");
            a.Mode = Json.S(d, "mode") == "type" ? TextMode.Type : TextMode.Paste;
            a.Sample = Json.B(d, "sample");
            var k = Json.O(d, "keys");
            if (k != null) a.Keys = Chord.FromJson(k);
            a.Path = Json.S(d, "path");
            a.Args = Json.S(d, "args");
            a.Url = Json.S(d, "url");
            a.Media = Json.S(d, "media", "playpause");
            a.StepDelayMs = Json.I(d, "stepDelayMs", 60);
            a.DelayMs = Json.I(d, "delayMs", 250);
            a.X = Json.I(d, "x");
            a.Y = Json.I(d, "y");
            a.Button = Json.S(d, "button", "left");
            foreach (var s in Json.A(d, "steps")) a.Steps.Add(FromJson(s as Dictionary<string, object>));
            foreach (var s in Json.A(d, "slots")) a.Slots.Add(Slot.FromJson(s as Dictionary<string, object>));
            return a;
        }
    }

    public sealed class Slot
    {
        public string Label = "";
        public string Icon = "";      // eingebautes Symbol: "g:E8C8" (Symbolschrift) oder "v:lunge" (Vektor)
        public string Image = "";     // eigenes Bild als Base64-PNG
        public string Color = Palette.Default;
        public ActionDef Action = new ActionDef();

        BitmapSource _bmp; string _bmpOf;

        public bool HasIcon { get { return !string.IsNullOrEmpty(Icon) || !string.IsNullOrEmpty(Image); } }
        public bool IsEmpty { get { return Action.Type == ActionType.None && string.IsNullOrWhiteSpace(Label) && !HasIcon; } }
        public Color ColorValue { get { return Palette.Parse(Color); } }

        /// <summary>Beschriftung, notfalls aus der Aktion abgeleitet.</summary>
        public string DisplayLabel
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(Label)) return Label.Trim();
                switch (Action.Type)
                {
                    case ActionType.None: return "";
                    case ActionType.Macro: return "Makro";
                    case ActionType.Folder: return "Unterrad";
                    case ActionType.Open:
                        try { string f = System.IO.Path.GetFileNameWithoutExtension(Action.Path ?? ""); if (f.Length > 0) return f; }
                        catch (ArgumentException) { }
                        return "Öffnen";
                    default:
                        string s = Action.Summary();
                        return s.Length > 22 ? s.Substring(0, 20).TrimEnd() + " …" : s;
                }
            }
        }

        public BitmapSource Bitmap
        {
            get
            {
                if (string.IsNullOrEmpty(Image)) return null;
                if (!ReferenceEquals(_bmpOf, Image))
                {
                    _bmpOf = Image;
                    _bmp = null;
                    try
                    {
                        var bi = new BitmapImage();
                        bi.BeginInit();
                        bi.CacheOption = BitmapCacheOption.OnLoad;
                        bi.StreamSource = new MemoryStream(Convert.FromBase64String(Image));
                        bi.EndInit();
                        bi.Freeze();
                        _bmp = bi;
                    }
                    catch (Exception) { }   // beschädigtes Bild: Segment bleibt ohne Bild
                }
                return _bmp;
            }
        }

        public Dictionary<string, object> ToJson()
        {
            var d = new Dictionary<string, object>();
            d["label"] = Label ?? "";
            if (!string.IsNullOrEmpty(Icon)) d["icon"] = Icon;
            if (!string.IsNullOrEmpty(Image)) d["image"] = Image;
            d["color"] = Color;
            d["action"] = Action.ToJson();
            return d;
        }

        public static Slot FromJson(Dictionary<string, object> d)
        {
            var s = new Slot();
            if (d == null) return s;
            s.Label = Json.S(d, "label");
            s.Icon = Json.S(d, "icon");
            s.Image = Json.S(d, "image");
            s.Color = Json.S(d, "color", Palette.Default);
            s.Action = ActionDef.FromJson(Json.O(d, "action"));
            return s;
        }

        public Slot Clone() { return FromJson(ToJson()); }
    }

    public sealed class Wheel
    {
        public string Id = Guid.NewGuid().ToString("N").Substring(0, 8);
        public string Name = "Neues Rad";
        public Chord Trigger = new Chord();
        public Chord Trigger2 = new Chord();     // optionaler zweiter Auslöser für dasselbe Rad
        public List<Slot> Slots = new List<Slot>();

        /// <summary>Alle belegten Auslöser, der erste zuerst.</summary>
        public IEnumerable<Chord> Triggers
        {
            get
            {
                if (!Trigger.IsEmpty) yield return Trigger;
                if (!Trigger2.IsEmpty) yield return Trigger2;
            }
        }

        public bool HasTrigger { get { return !Trigger.IsEmpty || !Trigger2.IsEmpty; } }

        /// <summary>„Strg + Leertaste oder Taste ^" – für Seitenleiste und Bedienhinweis.</summary>
        public string TriggerDisplay() { return string.Join(" oder ", Triggers.Select(t => t.Display())); }

        public Dictionary<string, object> ToJson()
        {
            var d = new Dictionary<string, object>();
            d["id"] = Id;
            d["name"] = Name ?? "";
            d["trigger"] = Trigger.ToJson();
            if (!Trigger2.IsEmpty) d["trigger2"] = Trigger2.ToJson();
            d["slots"] = Slots.Select(s => (object)s.ToJson()).ToList();
            return d;
        }

        public static Wheel FromJson(Dictionary<string, object> d)
        {
            var w = new Wheel();
            w.Id = Json.S(d, "id", w.Id);
            w.Name = Json.S(d, "name", "Rad");
            var t = Json.O(d, "trigger");
            if (t != null) w.Trigger = Chord.FromJson(t);
            var t2 = Json.O(d, "trigger2");
            if (t2 != null) w.Trigger2 = Chord.FromJson(t2);
            // Ein zweiter Auslöser ohne ersten rückt nach vorn, damit die Oberfläche ihn zeigt.
            if (w.Trigger.IsEmpty && !w.Trigger2.IsEmpty) { w.Trigger = w.Trigger2; w.Trigger2 = new Chord(); }
            foreach (var s in Json.A(d, "slots")) w.Slots.Add(Slot.FromJson(s as Dictionary<string, object>));
            Normalize(w.Slots);
            return w;
        }

        /// <summary>Hält jede Segmentliste (auch in Unterrädern) im erlaubten Bereich von 2 bis 12.</summary>
        public static void Normalize(List<Slot> slots)
        {
            while (slots.Count < MinSlots) slots.Add(new Slot());
            if (slots.Count > MaxSlots) slots.RemoveRange(MaxSlots, slots.Count - MaxSlots);
            foreach (var s in slots)
                if (s.Action.Type == ActionType.Folder) Normalize(s.Action.Slots);
        }

        public const int MinSlots = 2, MaxSlots = 12;
    }

    public sealed class Settings
    {
        public double Scale = 1.0;
        public bool TapSticky = true;
        public bool RestoreCursor = true;
        public bool RestoreClipboard = true;
        public int PasteDelayMs = 350;
        public string Skin = Kyklos.Skin.DefaultId;

        public Dictionary<string, object> ToJson()
        {
            var d = new Dictionary<string, object>();
            d["scale"] = Scale;
            d["tapSticky"] = TapSticky;
            d["restoreCursor"] = RestoreCursor;
            d["restoreClipboard"] = RestoreClipboard;
            d["pasteDelayMs"] = PasteDelayMs;
            d["skin"] = Skin;
            return d;
        }

        public static Settings FromJson(Dictionary<string, object> d)
        {
            var s = new Settings();
            s.Scale = Math.Max(0.7, Math.Min(1.4, Json.N(d, "scale", 1.0)));
            s.TapSticky = Json.B(d, "tapSticky", true);
            s.RestoreCursor = Json.B(d, "restoreCursor", true);
            s.RestoreClipboard = Json.B(d, "restoreClipboard", true);
            s.PasteDelayMs = Math.Max(50, Math.Min(3000, Json.I(d, "pasteDelayMs", 350)));
            s.Skin = Json.S(d, "skin", Kyklos.Skin.DefaultId);
            if (!Kyklos.Skin.Exists(s.Skin)) s.Skin = Kyklos.Skin.DefaultId;
            return s;
        }
    }

    public sealed class Config
    {
        public Settings Settings = new Settings();
        public List<Wheel> Wheels = new List<Wheel>();

        public Dictionary<string, object> ToJson()
        {
            var d = new Dictionary<string, object>();
            d["version"] = 1;
            d["settings"] = Settings.ToJson();
            d["wheels"] = Wheels.Select(w => (object)w.ToJson()).ToList();
            return d;
        }

        public static Config FromJson(Dictionary<string, object> d)
        {
            var c = new Config();
            c.Settings = Settings.FromJson(Json.O(d, "settings"));
            foreach (var w in Json.A(d, "wheels"))
            {
                var wd = w as Dictionary<string, object>;
                if (wd != null) c.Wheels.Add(Wheel.FromJson(wd));
            }
            return c;
        }
    }

    /// <summary>Die acht Tastenfarben. Hell genug, dass Graphit-Schrift darauf mindestens 4,5:1 erreicht.</summary>
    public static class Palette
    {
        public const string Default = "#FF8A3D";

        public static readonly string[][] Colors =
        {
            new[] { "#FF8A3D", "Orange" }, new[] { "#F5C542", "Gelb" }, new[] { "#5DD28B", "Grün" }, new[] { "#45CFC6", "Türkis" },
            new[] { "#62ABFF", "Blau" }, new[] { "#AC94FF", "Violett" }, new[] { "#FF7B7B", "Rot" }, new[] { "#E8EAED", "Hell" }
        };

        public static Color Parse(string hex)
        {
            try
            {
                if (!string.IsNullOrEmpty(hex)) return (Color)ColorConverter.ConvertFromString(hex);
            }
            catch (FormatException) { }
            return (Color)ColorConverter.ConvertFromString(Default);
        }
    }

    public static class KeyNames
    {
        public static string MouseName(int b)
        {
            switch (b)
            {
                case 1: return "Mittlere Maustaste";
                case 2: return "Maus-Seitentaste 1";
                case 3: return "Maus-Seitentaste 2";
                default: return "Maustaste";
            }
        }

        public static bool IsModifier(int vk)
        {
            return vk == Native.VK_SHIFT || vk == Native.VK_CONTROL || vk == Native.VK_MENU
                || (vk >= Native.VK_LSHIFT && vk <= Native.VK_RMENU) || vk == Native.VK_LWIN || vk == Native.VK_RWIN;
        }

        public static string VkName(int vk)
        {
            switch (vk)
            {
                case 0x08: return "Rücktaste";
                case 0x09: return "Tab";
                case 0x0D: return "Eingabe";
                case 0x13: return "Pause";
                case 0x14: return "Feststelltaste";
                case 0x1B: return "Esc";
                case 0x20: return "Leertaste";
                case 0x21: return "Bild auf";
                case 0x22: return "Bild ab";
                case 0x23: return "Ende";
                case 0x24: return "Pos1";
                case 0x25: return "Pfeil links";
                case 0x26: return "Pfeil oben";
                case 0x27: return "Pfeil rechts";
                case 0x28: return "Pfeil unten";
                case 0x2C: return "Druck";
                case 0x2D: return "Einfg";
                case 0x2E: return "Entf";
                case 0x5B: case 0x5C: return "Win";
                case 0x5D: return "Menütaste";
                case 0x6A: return "Num *";
                case 0x6B: return "Num +";
                case 0x6D: return "Num -";
                case 0x6E: return "Num ,";
                case 0x6F: return "Num /";
                case 0x90: return "Num";
                case 0x91: return "Rollen";
                case 0xA0: case 0xA1: case 0x10: return "Umschalt";
                case 0xA2: case 0xA3: case 0x11: return "Strg";
                case 0xA4: case 0xA5: case 0x12: return "Alt";
                case 0xAD: return "Ton aus";
                case 0xAE: return "Leiser";
                case 0xAF: return "Lauter";
                case 0xB0: return "Nächster Titel";
                case 0xB1: return "Vorheriger Titel";
                case 0xB2: return "Stopp";
                case 0xB3: return "Wiedergabe / Pause";
            }
            if (vk >= 0x60 && vk <= 0x69) return "Num " + (vk - 0x60);
            if (vk >= 0x70 && vk <= 0x87) return "F" + (vk - 0x6F);
            uint ch = Native.MapVirtualKey((uint)vk, 2) & 0x7FFFFFFF;   // Bit 31 markiert Tottasten wie ^
            if (ch > 32) return char.ToUpperInvariant((char)ch).ToString();
            return "Taste " + vk;
        }
    }
}
