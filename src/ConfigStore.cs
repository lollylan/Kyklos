using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace Kyklos
{
    /// <summary>Lädt und speichert die Konfiguration – neben der Exe (portabel), ersatzweise unter %APPDATA%.</summary>
    public static class ConfigStore
    {
        public const string FileName = "Kyklos.json";

        public static string ExeDir
        {
            get { return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location); }
        }

        /// <summary>Neben der Exe, solange der Ordner beschreibbar ist (USB-Stick, eigener Ordner); sonst im Benutzerprofil.</summary>
        public static string ResolvePath()
        {
            string local = Path.Combine(ExeDir, FileName);
            AdoptLegacy(ExeDir);
            if (File.Exists(local) || CanWrite(ExeDir)) return local;
            string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Kyklos");
            Directory.CreateDirectory(dir);
            AdoptLegacy(dir, Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Shortcut"));
            return Path.Combine(dir, FileName);
        }

        /// <summary>
        /// Übernimmt die Konfiguration aus der Zeit, als das Programm noch „Shortcut" hieß. Kopiert statt verschiebt,
        /// damit die alte Datei als Sicherung liegen bleibt.
        /// </summary>
        static void AdoptLegacy(string dir, string legacyDir = null)
        {
            string target = Path.Combine(dir, FileName), legacy = Path.Combine(legacyDir ?? dir, "Shortcut.json");
            try
            {
                if (File.Exists(target) || !File.Exists(legacy)) return;
                File.Copy(legacy, target);
                if (File.Exists(legacy + TmpSuffix)) File.Copy(legacy + TmpSuffix, target + TmpSuffix, true);
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }

        static bool CanWrite(string dir)
        {
            try
            {
                string probe = Path.Combine(dir, ".kyklos-" + Guid.NewGuid().ToString("N") + ".tmp");
                File.WriteAllText(probe, "");
                File.Delete(probe);
                return true;
            }
            catch (IOException) { return false; }
            catch (UnauthorizedAccessException) { return false; }
        }

        /// <summary>Liest die Datei. <paramref name="problem"/> nennt, was schiefging, falls auf die Vorgabe ausgewichen wurde.</summary>
        public static Config Load(string path, out bool created, out string problem)
        {
            created = false;
            problem = null;

            // Liegt eine neuere Zwischendatei, war das Ersetzen beim letzten Mal bis zum Beenden blockiert:
            // Sie enthält den jüngsten Stand.
            string tmp = path + TmpSuffix;
            try
            {
                if (File.Exists(tmp) && (!File.Exists(path) || File.GetLastWriteTimeUtc(tmp) > File.GetLastWriteTimeUtc(path)))
                {
                    var pending = Read(tmp);
                    if (pending.Wheels.Count > 0) return pending;
                }
            }
            catch (Exception) { }   // unvollständige Zwischendatei: die eigentliche Datei gilt

            if (!File.Exists(path))
            {
                created = true;
                return Default();
            }
            try
            {
                var cfg = Read(path);
                if (cfg.Wheels.Count == 0) cfg.Wheels.Add(Default().Wheels[0]);
                return cfg;
            }
            catch (Exception ex)
            {
                string backup = path + ".defekt-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
                try { File.Copy(path, backup, true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
                problem = "Die Konfiguration konnte nicht gelesen werden (" + ex.Message + ").\n\nSie wurde gesichert unter\n" + backup +
                          "\n\nKyklos startet mit den Beispiel-Rädern.";
                return Default();
            }
        }

        const string TmpSuffix = ".tmp";

        static Config Read(string file)
        {
            return Config.FromJson(Json.Parse(File.ReadAllText(file, Encoding.UTF8)) as Dictionary<string, object>);
        }

        /// <summary>
        /// Schreibt erst eine Zwischendatei und tauscht sie dann gegen die Konfiguration. Wirft, wenn beides scheitert –
        /// die Zwischendatei bleibt dann mit dem neuen Stand liegen und wird beim nächsten Laden übernommen.
        /// </summary>
        public static void Save(Config cfg, string path)
        {
            string json = Json.Write(cfg.ToJson());
            var utf8 = new UTF8Encoding(false);
            string tmp = path + TmpSuffix;
            File.WriteAllText(tmp, json, utf8);
            if (!File.Exists(path))
            {
                File.Move(tmp, path);
                return;
            }
            try
            {
                File.Replace(tmp, path, null);
                return;
            }
            catch (IOException) { }
            // Ersetzen verlangt, die alte Datei zu entfernen. Das verweigert Windows, solange ein anderes Programm sie
            // geöffnet hält – Virenscanner, Suchindex und Synchronisierung tun das kurz nach jedem Schreiben.
            // Hineinschreiben lassen diese Programme in aller Regel zu.
            File.WriteAllText(path, json, utf8);
            try { File.Delete(tmp); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }

        static Slot TextSlot(string label, string icon, string color, string text, bool sample = false)
        {
            return new Slot
            {
                Label = label, Icon = icon, Color = color,
                Action = new ActionDef { Type = ActionType.Text, Text = text, Sample = sample }
            };
        }

        static Slot KeySlot(string label, string icon, string color, Chord keys)
        {
            return new Slot { Label = label, Icon = icon, Color = color, Action = new ActionDef { Type = ActionType.Keys, Keys = keys } };
        }

        /// <summary>
        /// Beispiel-Räder für den ersten Start. Die Befundtexte sind als Beispiel markiert (sample): Sie werden mit
        /// Vermerk eingefügt, bis der Nutzer sie ändert oder ausdrücklich übernimmt.
        /// </summary>
        public static Config Default()
        {
            const string orange = "#FF8A3D", gelb = "#F5C542", gruen = "#5DD28B", tuerkis = "#45CFC6",
                         blau = "#62ABFF", violett = "#AC94FF", rot = "#FF7B7B", hell = "#E8EAED";

            var tools = new ActionDef { Type = ActionType.Folder };
            tools.Slots.Add(KeySlot("Kopieren", "v:kopieren", blau, Chord.Key('C', ctrl: true)));
            tools.Slots.Add(KeySlot("Einfügen", "v:einfuegen", blau, Chord.Key('V', ctrl: true)));
            tools.Slots.Add(new Slot
            {
                Label = "Alles kopieren", Icon = "v:auswahl", Color = violett,
                Action = new ActionDef
                {
                    Type = ActionType.Macro, StepDelayMs = 80,
                    Steps =
                    {
                        new ActionDef { Type = ActionType.Keys, Keys = Chord.Key('A', ctrl: true) },
                        new ActionDef { Type = ActionType.Keys, Keys = Chord.Key('C', ctrl: true) }
                    }
                }
            });
            tools.Slots.Add(new Slot { Label = "Ausschnitt", Icon = "v:ausschnitt", Color = gruen, Action = new ActionDef { Type = ActionType.Media, Media = "snip" } });
            tools.Slots.Add(new Slot { Label = "Rechner", Icon = "v:rechner", Color = gelb, Action = new ActionDef { Type = ActionType.Open, Path = "calc.exe" } });
            tools.Slots.Add(new Slot { Label = "PC sperren", Icon = "v:schloss", Color = rot, Action = new ActionDef { Type = ActionType.Media, Media = "lock" } });

            var wheel = new Wheel { Name = "Befunde", Trigger = Chord.Key(0xDC) };  // 0xDC: die ^-Taste auf deutscher Tastatur
            wheel.Slots.Add(TextSlot("Lunge", "v:lunge", blau,
                "Pulmo: Thorax symmetrisch, seitengleich belüftet. Vesikuläres Atemgeräusch beidseits, keine Rasselgeräusche, kein Giemen oder Brummen. Sonorer Klopfschall, Lungengrenzen gut atemverschieblich.", true));
            wheel.Slots.Add(TextSlot("Herz", "v:herz", rot,
                "Cor: Herztöne rein und rhythmisch, keine pathologischen Herzgeräusche, normofrequent. Keine peripheren Ödeme, Halsvenen nicht gestaut.", true));
            wheel.Slots.Add(TextSlot("Abdomen", "", gelb,
                "Abdomen: Bauchdecke weich, kein Druckschmerz, keine Abwehrspannung, keine Resistenzen. Darmgeräusche regelrecht über allen vier Quadranten. Leber und Milz nicht vergrößert tastbar, Nierenlager beidseits frei.", true));
            wheel.Slots.Add(TextSlot("HNO", "v:ohr", gruen,
                "HNO: Rachen reizlos, Tonsillen nicht vergrößert, keine Beläge. Trommelfelle beidseits spiegelnd und intakt. Keine zervikalen Lymphknotenschwellungen.", true));
            wheel.Slots.Add(TextSlot("Allgemein", "v:person", hell,
                "Patient in gutem Allgemein- und normalem Ernährungszustand, wach, zu allen Qualitäten orientiert, afebril.\n{oder}\n" +
                "Guter AZ, normaler EZ. Patient wach und allseits orientiert, {~kein Fieber | afebril | Temperatur im Normbereich}.\n{oder}\n" +
                "Patient wach, zeitlich, örtlich und zur Person orientiert, in gutem Allgemein- und normalem Ernährungszustand, fieberfrei.", true));
            wheel.Slots.Add(TextSlot("Datum", "v:kalender", tuerkis, "{datum}: "));
            wheel.Slots.Add(new Slot { Label = "Werkzeuge", Icon = "v:werkzeug", Color = orange, Action = tools });
            wheel.Slots.Add(new Slot());

            var cfg = new Config();
            cfg.Wheels.Add(wheel);
            return cfg;
        }
    }
}
