using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Kyklos
{
    /// <summary>
    /// Eingebaute Symbole. "g:XXXX" ist ein Zeichen der Windows-Symbolschrift (Segoe Fluent Icons / MDL2 Assets),
    /// "v:name" ein Linien-Symbol aus <see cref="Vectors"/> (24er-Raster; Lucide, ISC-Lizenz, und Tabler, MIT-Lizenz,
    /// übernommen mit tools/import-icons.mjs; Organe ohne Vorlage im selben Raster nachgezeichnet).
    /// </summary>
    public static class Icons
    {
        public static readonly FontFamily Font = new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets");
        static readonly Typeface Face = new Typeface(Font, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);

        public static readonly string[][] Anatomy =
        {
            new[] { "v:lunge", "Lunge" }, new[] { "v:herz", "Herz" }, new[] { "v:gehirn", "Gehirn / Neurologie" }, new[] { "v:ohr", "Ohr" },
            new[] { "v:auge", "Auge" }, new[] { "v:rachen", "Rachen" }, new[] { "v:nebenhoehlen", "Nebenhöhlen" }, new[] { "v:schilddruese", "Schilddrüse" },
            new[] { "v:leber", "Leber / Galle" }, new[] { "v:magen", "Magen" }, new[] { "v:darm", "Darm" }, new[] { "v:niere", "Niere / Nierenlager" },
            new[] { "v:blase", "Blase / Harnwege" }, new[] { "v:uterus", "Gynäkologie" }, new[] { "v:abdomen", "Abdomen" }, new[] { "v:wirbelsaeule", "Wirbelsäule" },
            new[] { "v:knie", "Knie" }, new[] { "v:knochen", "Knochen / Fraktur" }, new[] { "v:zahn", "Zahn" }, new[] { "v:schaedel", "Schädel / Kopf" },
            new[] { "v:hand", "Hand" }, new[] { "v:fuss", "Fuß" }, new[] { "v:koerper", "Ganzkörper" }, new[] { "v:weiblich", "Weiblich" },
            new[] { "v:maennlich", "Männlich" }, new[] { "v:saeugling", "Säugling / Kind" }, new[] { "v:senior", "Senior / Geriatrie" }
        };

        public static readonly string[][] Diagnostics =
        {
            new[] { "v:stethoskop", "Stethoskop" }, new[] { "v:puls", "Puls" }, new[] { "v:herzpuls", "Herz mit Puls" }, new[] { "v:ekg", "EKG / Monitor" },
            new[] { "v:blutdruck", "Blutdruck" }, new[] { "v:thermometer", "Thermometer" }, new[] { "v:waage", "Gewicht" }, new[] { "v:messen", "Messen" },
            new[] { "v:brille", "Sehtest / Brille" }, new[] { "v:hoertest", "Hörtest" }, new[] { "v:lampe", "Lampe / Pupillen" }, new[] { "v:reflexhammer", "Reflexe" },
            new[] { "v:bildgebung", "Bildgebung / Sono" }, new[] { "v:befund", "Befund" }, new[] { "v:vorsorge", "Vorsorge / Check-up" }, new[] { "v:anamnese", "Anamnese" },
            new[] { "v:arztbrief", "Arztbrief" }
        };

        public static readonly string[][] Lab =
        {
            new[] { "v:labor", "Labor / Blutentnahme" }, new[] { "v:mikroskop", "Mikroskop" }, new[] { "v:dna", "Genetik" }, new[] { "v:tropfen", "Tropfen" },
            new[] { "v:virus", "Virus" }, new[] { "v:bakterien", "Bakterien" }, new[] { "v:insekt", "Insektenstich / Zecke" }, new[] { "v:maske", "Maske" },
            new[] { "v:desinfektion", "Desinfektion" }, new[] { "v:biogefahr", "Infektiös" }, new[] { "v:uebelkeit", "Übelkeit / krank" }
        };

        public static readonly string[][] Therapy =
        {
            new[] { "v:pille", "Medikament" }, new[] { "v:tabletten", "Tabletten" }, new[] { "v:medikamentendose", "Medikamentendose" }, new[] { "v:rezept", "Rezept" },
            new[] { "v:saft", "Saft / Sirup" }, new[] { "v:spritze", "Spritze" }, new[] { "v:impfung", "Impfung" }, new[] { "v:ampulle", "Ampulle" },
            new[] { "v:infusion", "Infusion" }, new[] { "v:pflaster", "Pflaster / Wunde" }, new[] { "v:verbandskasten", "Erste Hilfe" }, new[] { "v:kreuz", "Kreuz" },
            new[] { "v:kuehlen", "Kühlen" }, new[] { "v:waerme", "Wärme" }, new[] { "v:massage", "Massage" }, new[] { "v:physiotherapie", "Physiotherapie" },
            new[] { "v:kruecken", "Unterarmgehstützen" }, new[] { "v:gehstock", "Gehstock" }, new[] { "v:rollstuhl", "Rollstuhl" }, new[] { "v:bett", "Bettruhe" }
        };

        public static readonly string[][] Lifestyle =
        {
            new[] { "v:rauchen", "Rauchen" }, new[] { "v:rauchstopp", "Rauchstopp" }, new[] { "v:alkohol", "Alkohol" }, new[] { "v:trinken", "Trinkmenge" },
            new[] { "v:ernaehrung", "Ernährung" }, new[] { "v:bewegung", "Bewegung" }, new[] { "v:schlaf", "Schlaf" }, new[] { "v:stimmung", "Stimmung / Psyche" },
            new[] { "v:sturz", "Sturz" }
        };

        public static readonly string[][] Care =
        {
            new[] { "v:unfall", "Unfall / BG" }, new[] { "v:rettungsdienst", "Rettungsdienst" }, new[] { "v:krankenhaus", "Krankenhaus / Einweisung" },
            new[] { "v:hausbesuch", "Hausbesuch" }, new[] { "v:pflege", "Pflege" }, new[] { "v:trage", "Liegendtransport" }, new[] { "v:bescheinigung", "Bescheinigung / AU" }
        };

        public static readonly string[][] Medical = Concat(Anatomy, Diagnostics, Lab, Therapy, Lifestyle, Care);

        public static readonly string[][] Tools =
        {
            new[] { "v:kalender", "Kalender" }, new[] { "v:uhr", "Uhr" }, new[] { "v:person", "Person" }, new[] { "v:dokument", "Dokument" },
            new[] { "v:stift", "Schreiben" }, new[] { "v:kopieren", "Kopieren" }, new[] { "v:einfuegen", "Einfügen" }, new[] { "v:auswahl", "Auswahl" },
            new[] { "v:ausschnitt", "Ausschnitt" }, new[] { "v:suche", "Suchen" }, new[] { "v:haken", "Erledigt" }, new[] { "v:ordner", "Ordner" },
            new[] { "v:mail", "E-Mail" }, new[] { "v:globus", "Web" }, new[] { "v:rechner", "Rechner" }, new[] { "v:werkzeug", "Werkzeug" },
            new[] { "v:schloss", "Sperren" }, new[] { "v:blitz", "Makro" }, new[] { "v:wiedergabe", "Wiedergabe" }, new[] { "v:plus", "Plus" }
        };

        public static readonly string[][] General =
        {
            new[] { "g:E8A5", "Dokument" }, new[] { "g:E70B", "Notiz" }, new[] { "g:E70F", "Bearbeiten" }, new[] { "g:E8FD", "Liste" },
            new[] { "g:E9F9", "Bericht" }, new[] { "g:E787", "Kalender" }, new[] { "g:E823", "Uhr" }, new[] { "g:E916", "Stoppuhr" },
            new[] { "g:E77B", "Person" }, new[] { "g:E716", "Personen" }, new[] { "g:E715", "E-Mail" }, new[] { "g:E8BD", "Nachricht" },
            new[] { "g:E717", "Telefon" }, new[] { "g:E8C8", "Kopieren" }, new[] { "g:E77F", "Einfügen" }, new[] { "g:E8C6", "Ausschneiden" },
            new[] { "g:E74E", "Speichern" }, new[] { "g:E749", "Drucken" }, new[] { "g:E7A7", "Rückgängig" }, new[] { "g:E7A6", "Wiederholen" },
            new[] { "g:E721", "Suchen" }, new[] { "g:E72C", "Aktualisieren" }, new[] { "g:E74D", "Löschen" }, new[] { "g:E710", "Hinzufügen" },
            new[] { "g:E73E", "Haken" }, new[] { "g:E711", "Schließen" }, new[] { "g:E8B7", "Ordner" }, new[] { "g:E8E5", "Datei öffnen" },
            new[] { "g:E8F1", "Bibliothek" }, new[] { "g:E774", "Web" }, new[] { "g:E71B", "Link" }, new[] { "g:E8A7", "Neues Fenster" },
            new[] { "g:E80F", "Start" }, new[] { "g:E7F4", "Bildschirm" }, new[] { "g:E765", "Tastatur" }, new[] { "g:E962", "Maus" },
            new[] { "g:E756", "Konsole" }, new[] { "g:E943", "Code" }, new[] { "g:E713", "Einstellungen" }, new[] { "g:E90F", "Werkzeug" },
            new[] { "g:E8EF", "Rechner" }, new[] { "g:E768", "Wiedergabe" }, new[] { "g:E769", "Pause" }, new[] { "g:E71A", "Stopp" },
            new[] { "g:E893", "Weiter" }, new[] { "g:E892", "Zurück" }, new[] { "g:E767", "Lautstärke" }, new[] { "g:E74F", "Stumm" },
            new[] { "g:E8D6", "Musik" }, new[] { "g:E720", "Mikrofon" }, new[] { "g:E722", "Kamera" }, new[] { "g:E8B9", "Bild" },
            new[] { "g:E714", "Video" }, new[] { "g:E72E", "Sperren" }, new[] { "g:E785", "Entsperren" }, new[] { "g:E7E8", "Ein/Aus" },
            new[] { "g:E708", "Nacht" }, new[] { "g:E706", "Helligkeit" }, new[] { "g:E7BA", "Warnung" }, new[] { "g:E946", "Info" },
            new[] { "g:E897", "Hilfe" }, new[] { "g:E734", "Stern" }, new[] { "g:E8EC", "Etikett" }, new[] { "g:E7C1", "Flagge" },
            new[] { "g:E718", "Anheften" }, new[] { "g:E707", "Ort" }, new[] { "g:E753", "Cloud" }, new[] { "g:E896", "Herunterladen" },
            new[] { "g:E898", "Hochladen" }, new[] { "g:E72D", "Teilen" }, new[] { "g:E945", "Blitz" }, new[] { "g:EA80", "Idee" },
            new[] { "g:E8CB", "Sortieren" }, new[] { "g:E71C", "Filter" }, new[] { "g:E8E1", "Daumen hoch" }, new[] { "g:E76E", "Smiley" }
        };

        /// <summary>Abschnitte der Symbolauswahl in Anzeigereihenfolge.</summary>
        public static readonly KeyValuePair<string, string[][]>[] Sections =
        {
            new KeyValuePair<string, string[][]>("Anatomie", Anatomy),
            new KeyValuePair<string, string[][]>("Untersuchung", Diagnostics),
            new KeyValuePair<string, string[][]>("Labor und Infekt", Lab),
            new KeyValuePair<string, string[][]>("Therapie", Therapy),
            new KeyValuePair<string, string[][]>("Lebensstil", Lifestyle),
            new KeyValuePair<string, string[][]>("Versorgung", Care),
            new KeyValuePair<string, string[][]>("Werkzeuge", Tools),
            new KeyValuePair<string, string[][]>("Windows-Symbole", General)
        };

        static string[][] Concat(params string[][][] groups)
        {
            var all = new List<string[]>();
            foreach (var g in groups) all.AddRange(g);
            return all.ToArray();
        }

        static readonly Dictionary<string, string> Vectors = new Dictionary<string, string>
        {
            { "lunge", "M6.081 20 c1.612 0 2.919 -1.335 2.919 -2.98 v-9.763 c0 -0.694 -0.552 -1.257 -1.232 -1.257 c-0.205 0 -0.405 0.052 -0.584 0.15 l-0.13 0.083 c-1.46 1.059 -2.432 2.647 -3.404 5.824 c-0.42 1.37 -0.636 2.962 -0.648 4.775 c-0.012 1.675 1.261 3.054 2.877 3.161 l0.203 0.007 z " +
                       "M17.92 20 c-1.612 0 -2.92 -1.335 -2.92 -2.98 v-9.763 c0 -0.694 0.552 -1.257 1.233 -1.257 c0.204 0 0.405 0.052 0.584 0.15 l0.13 0.083 c1.46 1.059 2.432 2.647 3.405 5.824 c0.42 1.37 0.636 2.962 0.648 4.775 c0.012 1.675 -1.261 3.054 -2.878 3.161 l-0.202 0.007 z " +
                       "M9 12 a2.99 2.99 0 0 0 2.132 -0.89 M12 4 v9 M15 12 a2.99 2.99 0 0 1 -2.132 -0.89" },
            { "herz", "M19 14 c1.49 -1.46 3 -3.21 3 -5.5 A5.5 5.5 0 0 0 16.5 3 c-1.76 0 -3 0.5 -4.5 2 c-1.5 -1.5 -2.74 -2 -4.5 -2 A5.5 5.5 0 0 0 2 8.5 c0 2.3 1.5 4.05 3 5.5 l7 7 Z" },
            { "herzpuls", "M19 14 c1.49 -1.46 3 -3.21 3 -5.5 A5.5 5.5 0 0 0 16.5 3 c-1.76 0 -3 0.5 -4.5 2 c-1.5 -1.5 -2.74 -2 -4.5 -2 A5.5 5.5 0 0 0 2 8.5 c0 2.3 1.5 4.05 3 5.5 l7 7 Z M3.22 12 H9.5 l0.5 -1 l2 4.5 l2 -7 l1.5 3.5 h5.27" },
            { "puls", "M22 12 h-4 l-3 9 L9 3 l-3 9 H2" },
            { "stethoskop", "M5 2 H4 a2 2 0 0 0 -2 2 v5 a6 6 0 0 0 12 0 V4 a2 2 0 0 0 -2 -2 h-1 M8 15 v1 a6 6 0 0 0 12 0 v-4 M22 10 a2 2 0 1 1 -4 0 a2 2 0 1 1 4 0" },
            { "ohr", "M6 8.5 a6.5 6.5 0 1 1 13 0 c0 6 -6 6 -6 10 a3.5 3.5 0 1 1 -7 0 M15 8.5 a2.5 2.5 0 0 0 -5 0 v1 a2 2 0 1 1 0 4" },
            { "auge", "M2 12 C2 12 5 5 12 5 C19 5 22 12 22 12 C22 12 19 19 12 19 C5 19 2 12 2 12 Z M15 12 a3 3 0 1 1 -6 0 a3 3 0 1 1 6 0" },
            { "thermometer", "M14 4 v10.54 a4 4 0 1 1 -4 0 V4 a2 2 0 0 1 4 0 Z" },
            { "pille", "M10.5 20.5 l10 -10 a4.95 4.95 0 1 0 -7 -7 l-10 10 a4.95 4.95 0 1 0 7 7 Z M8.5 8.5 l7 7" },
            { "spritze", "M18 2 l4 4 M17 7 l3 -3 M19 9 L8.7 19.3 c-1 1 -2.5 1 -3.4 0 l-0.6 -0.6 c-1 -1 -1 -2.5 0 -3.4 L15 5 M9 11 l4 4 M5 19 l-3 3 M14 4 l6 6" },
            { "tropfen", "M12 22 a7 7 0 0 0 7 -7 c0 -2 -1 -3.9 -3 -5.5 c-2 -1.6 -3.5 -4 -4 -6.5 c-0.5 2.5 -2 4.9 -4 6.5 C6 11.1 5 13 5 15 a7 7 0 0 0 7 7 z" },
            { "kreuz", "M11 2 a2 2 0 0 0 -2 2 v5 H4 a2 2 0 0 0 -2 2 v2 c0 1.1 0.9 2 2 2 h5 v5 c0 1.1 0.9 2 2 2 h2 a2 2 0 0 0 2 -2 v-5 h5 a2 2 0 0 0 2 -2 v-2 a2 2 0 0 0 -2 -2 h-5 V4 a2 2 0 0 0 -2 -2 h-2 z" },
            { "befund", "M9 2 h6 a1 1 0 0 1 1 1 v2 a1 1 0 0 1 -1 1 H9 a1 1 0 0 1 -1 -1 V3 a1 1 0 0 1 1 -1 z M16 4 h2 a2 2 0 0 1 2 2 v14 a2 2 0 0 1 -2 2 H6 a2 2 0 0 1 -2 -2 V6 a2 2 0 0 1 2 -2 h2 M12 11 h4 M12 16 h4 M8 11 h0.01 M8 16 h0.01" },
            { "bett", "M2 4 v16 M2 8 h18 a2 2 0 0 1 2 2 v10 M2 17 h20 M6 8 v9" },
            { "gehirn", "M12 5 C12 3.5 10.5 3 9 3.5 C7.5 3 5.5 4 5.5 6 C3.5 6.5 3 9 4 10.5 C2.5 12 3 14.5 4.5 15.5 C4.5 18 6.5 19.5 8.5 19 C9.5 20.5 12 20.5 12 18.5 Z M12 5 C12 3.5 13.5 3 15 3.5 C16.5 3 18.5 4 18.5 6 C20.5 6.5 21 9 20 10.5 C21.5 12 21 14.5 19.5 15.5 C19.5 18 17.5 19.5 15.5 19 C14.5 20.5 12 20.5 12 18.5 Z M8.5 9 c1 0 2 0.5 3.5 1 M15.5 9 c-1 0 -2 0.5 -3.5 1 M7.5 14 c1.5 0 3 -0.5 4.5 -1 M16.5 14 c-1.5 0 -3 -0.5 -4.5 -1" },
            { "niere", "M13 3 C8.5 3 5.5 6.5 5.5 11 C5.5 15.5 8 20 11.5 21 C13.5 21.5 14.5 20 14.5 18.5 C14.5 16.5 12 16 12 13.5 C12 11 14.5 10.5 14.5 8 C14.5 5 15.5 3 13 3 z" },
            { "rachen", "M4 20 V13 a8 8 0 0 1 16 0 v7 M7.5 20 v-5 a4.5 4.5 0 0 1 9 0 v5 M12 10.5 v3.5 M13.2 15.2 a1.2 1.2 0 1 1 -2.4 0 a1.2 1.2 0 1 1 2.4 0" },
            { "nebenhoehlen", "M12 3 v11 M9.5 16 a2.5 2.5 0 0 0 5 0 M9.5 12 a3 3 0 1 1 -6 0 a3 3 0 1 1 6 0 M20.5 12 a3 3 0 1 1 -6 0 a3 3 0 1 1 6 0 M3.5 7 c1 -2 4 -2 5 0 M15.5 7 c1 -2 4 -2 5 0" },
            { "abdomen", "M21 12 a9 9 0 1 1 -18 0 a9 9 0 1 1 18 0 M12 3 v6.5 M12 14.5 v6.5 M3 12 h6.5 M14.5 12 h6.5 M13.2 12 a1.2 1.2 0 1 1 -2.4 0 a1.2 1.2 0 1 1 2.4 0" },
            { "wirbelsaeule", "M9 2 h6 a1.2 1.2 0 0 1 1.2 1.2 v0.8 a1.2 1.2 0 0 1 -1.2 1.2 h-6 a1.2 1.2 0 0 1 -1.2 -1.2 v-0.8 a1.2 1.2 0 0 1 1.2 -1.2 z M9 7.5 h6 a1.2 1.2 0 0 1 1.2 1.2 v0.8 a1.2 1.2 0 0 1 -1.2 1.2 h-6 a1.2 1.2 0 0 1 -1.2 -1.2 v-0.8 a1.2 1.2 0 0 1 1.2 -1.2 z M9 13 h6 a1.2 1.2 0 0 1 1.2 1.2 v0.8 a1.2 1.2 0 0 1 -1.2 1.2 h-6 a1.2 1.2 0 0 1 -1.2 -1.2 v-0.8 a1.2 1.2 0 0 1 1.2 -1.2 z M9 18.5 h6 a1.2 1.2 0 0 1 1.2 1.2 v0.8 a1.2 1.2 0 0 1 -1.2 1.2 h-6 a1.2 1.2 0 0 1 -1.2 -1.2 v-0.8 a1.2 1.2 0 0 1 1.2 -1.2 z" },
            { "knie", "M9 2 v4 c0 1 -2 1.5 -2 3.5 a2 2 0 0 0 2 2 h6 a2 2 0 0 0 2 -2 c0 -2 -2 -2.5 -2 -3.5 V2 M7 14.5 a1.5 1.5 0 0 1 1.5 -1.5 h7 a1.5 1.5 0 0 1 1.5 1.5 c0 1.5 -2 2 -2 3.5 v4 M9 22 v-4 c0 -1.5 -2 -2 -2 -3.5" },
            { "schilddruese", "M10 10.5 C10 7.5 9 5 7.5 5 C5.5 5 4 9 4 13.5 C4 16.5 5 19 7 19 C8.5 19 10 16.5 10 14 H14 C14 16.5 15.5 19 17 19 C19 19 20 16.5 20 13.5 C20 9 18.5 5 16.5 5 C15 5 14 7.5 14 10.5 Z M12 2 V10.5" },
            { "leber", "M3 8 C3 5.5 4.5 4 7.5 4 H17.5 C20 4 21.5 5.5 20.5 7.5 C19.5 9.5 17 11 14 13.5 C11 16 9 20 6 20 C4 20 3 18 3 15.5 Z M13.5 4 C13.5 6.5 12.5 8.5 10.5 10" },
            { "magen", "M13 2 V5 C14 4 15.5 3.5 17 3.5 C20 3.5 21.5 6.5 21 10.5 C20.5 15.5 16.5 20 11 20 C8.5 20 6.5 19 5.5 17.5 H3 M10 2 V6.5 C10 10 11.5 12.5 14 13 C12 14.5 9.5 14.5 6.5 14.5 H3" },
            { "darm", "M5 19.5 A1.5 1.5 0 0 1 5 16.5 A1.5 1.5 0 0 1 5 13.5 A1.5 1.5 0 0 1 5 10.5 A1.5 1.5 0 0 1 5 7.5 A2.5 2.5 0 0 1 7.5 5 A1.5 1.5 0 0 1 10.5 5 A1.5 1.5 0 0 1 13.5 5 A1.5 1.5 0 0 1 16.5 5 A2.5 2.5 0 0 1 19 7.5 A1.5 1.5 0 0 1 19 10.5 A1.5 1.5 0 0 1 19 13.5 A1.5 1.5 0 0 1 19 16.5 C19 18.5 17.5 19 15.5 19 C13.5 19 12.5 19.5 12.5 22 M9 9.5 H14.5 A1.75 1.75 0 0 1 14.5 13 H9.5 A1.75 1.75 0 0 0 9.5 16.5 H15" },
            { "blase", "M5.5 2.5 C7 2.5 8 4 8 6 C8 8 7 9.5 5.5 9.5 C4 9.5 3 8 3 6 C3 4 4 2.5 5.5 2.5 Z M18.5 2.5 C20 2.5 21 4 21 6 C21 8 20 9.5 18.5 9.5 C17 9.5 16 8 16 6 C16 4 17 2.5 18.5 2.5 Z M7.5 8 C9 10 9.5 12 9.8 14 M16.5 8 C15 10 14.5 12 14.2 14 M8.5 16 C8.5 14 10 13 12 13 C14 13 15.5 14 15.5 16 C15.5 18.5 14 19.5 12 19.5 C10 19.5 8.5 18.5 8.5 16 Z M12 19.5 V22" },
            { "uterus", "M9 6 C9 5 9.5 4.5 10.5 4.5 H13.5 C14.5 4.5 15 5 15 6 C15 10 14 13 13 14.5 V20 H11 V14.5 C10 13 9 10 9 6 Z M9 7.5 C7 5 4 5 3.5 7.5 V9.5 M15 7.5 C17 5 20 5 20.5 7.5 V9.5 M7.5 10.5 A1.5 1.5 0 1 1 4.5 10.5 A1.5 1.5 0 1 1 7.5 10.5 M19.5 10.5 A1.5 1.5 0 1 1 16.5 10.5 A1.5 1.5 0 1 1 19.5 10.5" },
            { "reflexhammer", "M14.36 3.24 L20.76 9.64 A2.2 2.2 0 0 1 17.64 12.76 L11.24 6.36 A2.2 2.2 0 0 1 14.36 3.24 Z M14.44 9.56 L4 20" },
            { "knochen", "M 15 3 a 3 3 0 0 1 3 3 a 3 3 0 1 1 -2.12 5.122 l -4.758 4.758 a 3 3 0 1 1 -5.117 2.297 l 0 -0.177 l -0.176 0 a 3 3 0 1 1 2.298 -5.115 l 4.758 -4.758 a 3 3 0 0 1 2.12 -5.122 l -0.005 -0.005" }, // tabler:bone
            { "zahn", "M 12 5.5 c -1.074 -0.586 -2.583 -1.5 -4 -1.5 c -2.1 0 -4 1.247 -4 5 c 0 4.899 1.056 8.41 2.671 10.537 c 0.573 0.756 1.97 0.521 2.567 -0.236 c 0.398 -0.505 0.819 -1.439 1.262 -2.801 c 0.292 -0.771 0.892 -1.504 1.5 -1.5 c 0.602 0 1.21 0.737 1.5 1.5 c 0.443 1.362 0.864 2.295 1.262 2.8 c 0.597 0.759 2 0.993 2.567 0.237 c 1.615 -2.127 2.671 -5.637 2.671 -10.537 c 0 -3.74 -1.908 -5 -4 -5 c -1.423 0 -2.92 0.911 -4 1.5 M 12 5.5 l 3 1.5" }, // tabler:dental
            { "schaedel", "M 12 4 c 4.418 0 8 3.358 8 7.5 c 0 1.901 -0.755 3.637 -2 4.96 l 0 2.54 a 1 1 0 0 1 -1 1 h -10 a 1 1 0 0 1 -1 -1 v -2.54 c -1.245 -1.322 -2 -3.058 -2 -4.96 c 0 -4.142 3.582 -7.5 8 -7.5 M 10 17 v 3 M 14 17 v 3 M 8 11 a 1 1 0 1 0 2 0 a 1 1 0 1 0 -2 0 M 14 11 a 1 1 0 1 0 2 0 a 1 1 0 1 0 -2 0" }, // tabler:skull
            { "hand", "M 8 13 v -7.5 a 1.5 1.5 0 0 1 3 0 v 6.5 M 11 5.5 v -2 a 1.5 1.5 0 1 1 3 0 v 8.5 M 14 5.5 a 1.5 1.5 0 0 1 3 0 v 6.5 M 17 7.5 a 1.5 1.5 0 0 1 3 0 v 8.5 a 6 6 0 0 1 -6 6 h -2 h 0.208 a 6 6 0 0 1 -5.012 -2.7 a 69.74 69.74 0 0 1 -0.196 -0.3 c -0.312 -0.479 -1.407 -2.388 -3.286 -5.728 a 1.5 1.5 0 0 1 0.536 -2.022 a 1.867 1.867 0 0 1 2.28 0.28 l 1.47 1.47" }, // tabler:hand-stop
            { "fuss", "M 4 16 v -2.38 C 4 11.5 2.97 10.5 3 8 c 0.03 -2.72 1.49 -6 4.5 -6 C 9.37 2 10 3.8 10 5.5 c 0 3.11 -2 5.66 -2 8.68 V 16 a 2 2 0 1 1 -4 0 Z M 20 20 v -2.38 c 0 -2.12 1.03 -3.12 1 -5.62 c -0.03 -2.72 -1.49 -6 -4.5 -6 C 14.63 6 14 7.8 14 9.5 c 0 3.11 2 5.66 2 8.68 V 20 a 2 2 0 1 0 4 0 Z M 16 17 h 4 M 4 13 h 4" }, // lucide:footprints
            { "koerper", "M 13 5 A 1 1 0 1 1 11 5 A 1 1 0 1 1 13 5 Z M 9 20 l 3 -6 l 3 6 M 6 8 l 6 2 l 6 -2 M 12 10 v 4" }, // lucide:person-standing
            { "weiblich", "M 7 9 a 5 5 0 1 0 10 0 a 5 5 0 1 0 -10 0 M 12 14 v 7 M 9 18 h 6" }, // tabler:gender-female
            { "maennlich", "M 5 14 a 5 5 0 1 0 10 0 a 5 5 0 1 0 -10 0 M 19 5 l -5.4 5.4 M 19 5 h -5 M 19 5 v 5" }, // tabler:gender-male
            { "saeugling", "M 10 16 c 0.5 0.3 1.2 0.5 2 0.5 s 1.5 -0.2 2 -0.5 M 15 12 h 0.01 M 19.38 6.813 A 9 9 0 0 1 20.8 10.2 a 2 2 0 0 1 0 3.6 a 9 9 0 0 1 -17.6 0 a 2 2 0 0 1 0 -3.6 A 9 9 0 0 1 12 3 c 2 0 3.5 1.1 3.5 2.5 s -0.9 2.5 -2 2.5 c -0.8 0 -1.5 -0.4 -1.5 -1 M 9 12 h 0.01" }, // lucide:baby
            { "senior", "M 11 21 l -1 -4 l -2 -3 v -6 M 5 14 l -1 -3 l 4 -3 l 3 2 l 3 0.5 M 7 4 a 1 1 0 1 0 2 0 a 1 1 0 1 0 -2 0 M 7 17 l -2 4 M 16 21 v -8.5 a 1.5 1.5 0 0 1 3 0 v 0.5" }, // tabler:old
            { "ekg", "M 3 5 a 1 1 0 0 1 1 -1 h 16 a 1 1 0 0 1 1 1 v 10 a 1 1 0 0 1 -1 1 h -16 a 1 1 0 0 1 -1 -1 l 0 -10 M 7 20 h 10 M 9 16 v 4 M 15 16 v 4 M 7 10 h 2 l 2 3 l 2 -6 l 1 3 h 3" }, // tabler:heart-rate-monitor
            { "blutdruck", "M 3 12 a 9 9 0 1 0 18 0 a 9 9 0 1 0 -18 0 M 11 12 a 1 1 0 1 0 2 0 a 1 1 0 1 0 -2 0 M 13.41 10.59 l 2.59 -2.59 M 7 12 a 5 5 0 0 1 5 -5" }, // tabler:gauge
            { "waage", "M 3 7 a 4 4 0 0 1 4 -4 h 10 a 4 4 0 0 1 4 4 v 10 a 4 4 0 0 1 -4 4 h -10 a 4 4 0 0 1 -4 -4 v -10 M 12 7 c 1.956 0 3.724 0.802 5 2.095 l -2.956 2.904 a 3 3 0 0 0 -2.038 -0.799 a 3 3 0 0 0 -2.038 0.798 l -2.956 -2.903 a 6.979 6.979 0 0 1 5 -2.095" }, // tabler:scale-outline
            { "messen", "M 19.875 12 c 0.621 0 1.125 0.512 1.125 1.143 v 5.714 c 0 0.631 -0.504 1.143 -1.125 1.143 h -15.875 a 1 1 0 0 1 -1 -1 v -5.857 c 0 -0.631 0.504 -1.143 1.125 -1.143 h 15.75 M 9 12 v 2 M 6 12 v 3 M 12 12 v 3 M 18 12 v 3 M 15 12 v 2 M 3 3 v 4 M 3 5 h 18 M 21 3 v 4" }, // tabler:ruler-measure
            { "brille", "M 8 4 h -2 l -3 10 M 16 4 h 2 l 3 10 M 10 16 l 4 0 M 21 16.5 a 3.5 3.5 0 0 1 -7 0 v -2.5 h 7 v 2.5 M 10 16.5 a 3.5 3.5 0 0 1 -7 0 v -2.5 h 7 v 2.5" }, // tabler:eyeglass
            { "hoertest", "M 15 15 a 2 2 0 0 1 -2 2 c -0.732 0 -1.555 -0.247 -1.72 -0.98 c -0.634 -2.8 -3.17 -2.628 -3.28 -5.02 v -0.5 a 3.5 3.5 0 0 1 6.671 -1.483 M 13 12 v 0.01 M 3 7 v -2 a 2 2 0 0 1 2 -2 h 2 M 3 17 v 2 a 2 2 0 0 0 2 2 h 2 M 17 3 h 2 a 2 2 0 0 1 2 2 v 2 M 17 21 h 2 a 2 2 0 0 0 2 -2 v -2" }, // tabler:ear-scan
            { "lampe", "M 12 13 v 1 M 17 2 a 1 1 0 0 1 1 1 v 4 a 3 3 0 0 1 -0.6 1.8 l -0.6 0.8 A 4 4 0 0 0 16 12 v 8 a 2 2 0 0 1 -2 2 H 10 a 2 2 0 0 1 -2 -2 v -8 a 4 4 0 0 0 -0.8 -2.4 l -0.6 -0.8 A 3 3 0 0 1 6 7 V 3 a 1 1 0 0 1 1 -1 z M 6 6 h 12" }, // lucide:flashlight
            { "vorsorge", "M 9 5 h -2 a 2 2 0 0 0 -2 2 v 12 a 2 2 0 0 0 2 2 h 10 a 2 2 0 0 0 2 -2 v -12 a 2 2 0 0 0 -2 -2 h -2 M 9 5 a 2 2 0 0 1 2 -2 h 2 a 2 2 0 0 1 2 2 a 2 2 0 0 1 -2 2 h -2 a 2 2 0 0 1 -2 -2 M 9 14 h 0.01 M 9 17 h 0.01 M 12 16 l 1 1 l 3 -3" }, // tabler:checkup-list
            { "arztbrief", "M 9 5 h -2 a 2 2 0 0 0 -2 2 v 12 a 2 2 0 0 0 2 2 h 10 a 2 2 0 0 0 2 -2 v -12 a 2 2 0 0 0 -2 -2 h -2 M 9 5 a 2 2 0 0 1 2 -2 h 2 a 2 2 0 0 1 2 2 a 2 2 0 0 1 -2 2 h -2 a 2 2 0 0 1 -2 -2 M 10 14 l 4 0 M 12 12 l 0 4" }, // tabler:report-medical
            { "anamnese", "M 9 2 H 15 A 1 1 0 0 1 16 3 V 5 A 1 1 0 0 1 15 6 H 9 A 1 1 0 0 1 8 5 V 3 A 1 1 0 0 1 9 2 Z M 16 4 h 2 a 2 2 0 0 1 2 2 v 14 a 2 2 0 0 1 -2 2 H 6 a 2 2 0 0 1 -2 -2 V 6 a 2 2 0 0 1 2 -2 h 2 M 9 14 h 6 M 12 17 v -6" }, // lucide:clipboard-plus
            { "bildgebung", "M 3 7 V 5 a 2 2 0 0 1 2 -2 h 2 M 17 3 h 2 a 2 2 0 0 1 2 2 v 2 M 21 17 v 2 a 2 2 0 0 1 -2 2 h -2 M 7 21 H 5 a 2 2 0 0 1 -2 -2 v -2 M 7 12 h 10" }, // lucide:scan-line
            { "mikroskop", "M 5 21 h 14 M 6 18 h 2 M 7 18 v 3 M 9 11 l 3 3 l 6 -6 l -3 -3 l -6 6 M 10.5 12.5 l -1.5 1.5 M 17 3 l 3 3 M 12 21 a 6 6 0 0 0 3.715 -10.712" }, // tabler:microscope
            { "labor", "M 9 2 v 17.5 A 2.5 2.5 0 0 1 6.5 22 A 2.5 2.5 0 0 1 4 19.5 V 2 M 20 2 v 17.5 a 2.5 2.5 0 0 1 -2.5 2.5 a 2.5 2.5 0 0 1 -2.5 -2.5 V 2 M 3 2 h 7 M 14 2 h 7 M 9 16 H 4 M 20 16 h -5" }, // lucide:test-tubes
            { "dna", "M 14.828 14.828 a 4 4 0 1 0 -5.656 -5.656 a 4 4 0 0 0 5.656 5.656 M 9.172 20.485 a 4 4 0 1 0 -5.657 -5.657 M 14.828 3.515 a 4 4 0 0 0 5.657 5.657" }, // tabler:dna
            { "tabletten", "M 3 8 a 5 5 0 1 0 10 0 a 5 5 0 1 0 -10 0 M 13 17 a 4 4 0 1 0 8 0 a 4 4 0 1 0 -8 0 M 4.5 4.5 l 7 7 M 19.5 14.5 l -5 5" }, // tabler:pills
            { "medikamentendose", "M 18 11 h -4 a 1 1 0 0 0 -1 1 v 5 a 1 1 0 0 0 1 1 h 4 M 6 7 v 13 a 2 2 0 0 0 2 2 h 8 a 2 2 0 0 0 2 -2 V 7 M 5 2 H 19 A 1 1 0 0 1 20 3 V 6 A 1 1 0 0 1 19 7 H 5 A 1 1 0 0 1 4 6 V 3 A 1 1 0 0 1 5 2 Z" }, // lucide:pill-bottle
            { "rezept", "M 6 19 v -16 h 4.5 a 4.5 4.5 0 1 1 0 9 h -4.5 M 19 21 l -9 -9 M 13 21 l 6 -6" }, // tabler:prescription
            { "saft", "M 8 21 h 8 a 1 1 0 0 0 1 -1 v -10 a 3 3 0 0 0 -3 -3 h -4 a 3 3 0 0 0 -3 3 v 10 a 1 1 0 0 0 1 1 M 10 14 h 4 M 12 12 v 4 M 10 7 v -3 a 1 1 0 0 1 1 -1 h 2 a 1 1 0 0 1 1 1 v 3" }, // tabler:medicine-syrup
            { "impfung", "M 17 3 l 4 4 M 19 5 l -4.5 4.5 M 11.5 6.5 l 6 6 M 16.5 11.5 l -6.5 6.5 h -4 v -4 l 6.5 -6.5 M 7.5 12.5 l 1.5 1.5 M 10.5 9.5 l 1.5 1.5 M 3 21 l 3 -3" }, // tabler:vaccine
            { "ampulle", "M 9 4 a 1 1 0 0 1 1 -1 h 4 a 1 1 0 0 1 1 1 v 1 a 1 1 0 0 1 -1 1 h -4 a 1 1 0 0 1 -1 -1 l 0 -1 M 10 6 v 0.98 c 0 0.877 -0.634 1.626 -1.5 1.77 c -0.866 0.144 -1.5 0.893 -1.5 1.77 v 8.48 a 2 2 0 0 0 2 2 h 6 a 2 2 0 0 0 2 -2 v -8.48 c 0 -0.877 -0.634 -1.626 -1.5 -1.77 a 1.795 1.795 0 0 1 -1.5 -1.77 v -0.98 M 7 12 h 10 M 7 18 h 10 M 11 15 h 2" }, // tabler:vaccine-bottle
            { "infusion", "M 12 18 v 2 a 2 2 0 0 0 2 2 h 6 M 6 11 c 0.72 0.5 1.44 1 3 1 c 3 0 3 -2 6 -2 c 1.56 0 2.28 0.5 3 1 M 9.293 3 c 0.453 0 0.887 -0.18 1.207 -0.5 s 0.754 -0.5 1.207 -0.5 h 0.586 c 0.453 0 0.887 0.18 1.207 0.5 s 0.754 0.5 1.207 0.5 H 16 a 2 2 0 0 1 2 2 v 11 a 2 2 0 0 1 -2 2 H 8 a 2 2 0 0 1 -2 -2 V 5 a 2 2 0 0 1 2 -2 z" }, // lucide:iv-bag
            { "pflaster", "M 14 12 l 0 0.01 M 10 12 l 0 0.01 M 12 10 l 0 0.01 M 12 14 l 0 0.01 M 4.5 12.5 l 8 -8 a 4.94 4.94 0 0 1 7 7 l -8 8 a 4.94 4.94 0 0 1 -7 -7" }, // tabler:bandage
            { "verbandskasten", "M 8 8 v -2 a 2 2 0 0 1 2 -2 h 4 a 2 2 0 0 1 2 2 v 2 M 4 10 a 2 2 0 0 1 2 -2 h 12 a 2 2 0 0 1 2 2 v 8 a 2 2 0 0 1 -2 2 h -12 a 2 2 0 0 1 -2 -2 l 0 -8 M 10 14 h 4 M 12 12 v 4" }, // tabler:first-aid-kit
            { "massage", "M 3 17 a 1 1 0 1 0 2 0 a 1 1 0 1 0 -2 0 M 8 5 a 1 1 0 1 0 2 0 a 1 1 0 1 0 -2 0 M 4 22 l 4 -2 v -3 h 12 M 11 20 h 9 M 8 14 l 3 -2 l 1 -4 c 3 1 3 4 3 6" }, // tabler:massage
            { "physiotherapie", "M 9 15 l -1 -3 l 4 -2 l 4 1 h 3.5 M 3 19 a 1 1 0 1 0 2 0 a 1 1 0 1 0 -2 0 M 11 6 a 1 1 0 1 0 2 0 a 1 1 0 1 0 -2 0 M 12 17 v -7 M 8 20 h 7 l 1 -4 l 4 -2 M 18 20 h 3" }, // tabler:physiotherapist
            { "kruecken", "M 8 5 a 2 2 0 0 1 2 -2 h 4 a 2 2 0 0 1 2 2 a 2 2 0 0 1 -2 2 h -4 a 2 2 0 0 1 -2 -2 M 11 21 h 2 M 12 21 v -4.092 a 3 3 0 0 1 0.504 -1.664 l 0.992 -1.488 a 3 3 0 0 0 0.504 -1.664 v -5.092 M 12 21 v -4.092 a 3 3 0 0 0 -0.504 -1.664 l -0.992 -1.488 a 3 3 0 0 1 -0.504 -1.664 v -5.092 M 10 11 h 4" }, // tabler:crutches
            { "rollstuhl", "M 3 16 a 5 5 0 1 0 10 0 a 5 5 0 1 0 -10 0 M 17 19 a 2 2 0 1 0 4 0 a 2 2 0 1 0 -4 0 M 19 17 a 3 3 0 0 0 -3 -3 h -3.4 M 3 3 h 1 a 2 2 0 0 1 2 2 v 6 M 6 8 h 11 M 15 8 v 6" }, // tabler:wheelchair
            { "gehstock", "M 9 21 l 6.324 -11.69 c 0.54 -0.974 1.756 -4.104 -1.499 -5.762 c -3.255 -1.657 -5.175 0.863 -5.825 2.032" }, // tabler:cane
            { "kuehlen", "M 10 20 l -1.25 -2.5 L 6 18 M 10 4 L 8.75 6.5 L 6 6 M 14 20 l 1.25 -2.5 L 18 18 M 14 4 l 1.25 2.5 L 18 6 M 17 21 l -3 -6 h -4 M 17 3 l -3 6 l 1.5 3 M 2 12 h 6.5 L 10 9 M 20 10 l -1.5 2 l 1.5 2 M 22 12 h -6.5 L 14 15 M 4 10 l 1.5 2 L 4 14 M 7 21 l 3 -6 l -1.5 -3 M 7 3 l 3 6 h 4" }, // lucide:snowflake
            { "waerme", "M 12 10.941 c 2.333 -3.308 0.167 -7.823 -1 -8.941 c 0 3.395 -2.235 5.299 -3.667 6.706 c -1.43 1.408 -2.333 3.294 -2.333 5.588 c 0 3.704 3.134 6.706 7 6.706 c 3.866 0 7 -3.002 7 -6.706 c 0 -1.712 -1.232 -4.403 -2.333 -5.588 c -2.084 3.353 -3.257 3.353 -4.667 2.235" }, // tabler:flame
            { "virus", "M 7 12 a 5 5 0 1 0 10 0 a 5 5 0 1 0 -10 0 M 12 7 v -4 M 11 3 h 2 M 15.536 8.464 l 2.828 -2.828 M 17.657 4.929 l 1.414 1.414 M 17 12 h 4 M 21 11 v 2 M 15.535 15.536 l 2.829 2.828 M 19.071 17.657 l -1.414 1.414 M 12 17 v 4 M 13 21 h -2 M 8.465 15.536 l -2.829 2.828 M 6.343 19.071 l -1.413 -1.414 M 7 12 h -4 M 3 13 v -2 M 8.464 8.464 l -2.828 -2.828 M 4.929 6.343 l 1.414 -1.413" }, // tabler:virus
            { "bakterien", "M 11 2 l 0.925 1.848 M 13 15 h 0.01 M 16 21 l -1 -2.472 M 19 2 l -1 1.804 M 2 19 l 2.746 -1.373 M 22 16 l -2.474 -2.13 M 22 5 l -1.804 1 M 3 10 l 2 2 M 9 16 h 0.01 M 9 20 v 2 M 9.33 7.035 c -0.51 1.478 -1.786 2.93 -3.09 3.794 A 5 5 0 0 0 9 20 a 12.1 12.1 0 0 0 11.902 -9.916 A 6 6 0 0 0 9.33 7.035 M 17 9 A 2 2 0 1 1 13 9 A 2 2 0 1 1 17 9 Z" }, // lucide:germ
            { "maske", "M 5 14.5 h -0.222 c -1.535 0 -2.778 -1.12 -2.778 -2.5 s 1.243 -2.5 2.778 -2.5 h 0.222 M 19 14.5 h 0.222 c 1.534 0 2.778 -1.12 2.778 -2.5 s -1.244 -2.5 -2.778 -2.5 h -0.222 M 9 10 h 6 M 9 14 h 6 M 12.55 18.843 l 5 -1.429 a 2 2 0 0 0 1.45 -1.923 v -6.981 a 2 2 0 0 0 -1.45 -1.923 l -5 -1.429 a 2 2 0 0 0 -1.1 0 l -5 1.429 a 2 2 0 0 0 -1.45 1.922 v 6.982 a 2 2 0 0 0 1.45 1.923 l 5 1.429 a 2 2 0 0 0 1.1 0" }, // tabler:face-mask
            { "desinfektion", "M 7 21 h 10 v -10 a 3 3 0 0 0 -3 -3 h -4 a 3 3 0 0 0 -3 3 v 10 M 15 3 h -6 a 2 2 0 0 0 -2 2 M 12 3 v 5 M 12 11 v 4 M 10 13 h 4" }, // tabler:hand-sanitizer
            { "biogefahr", "M 10 12 a 2 2 0 1 0 4 0 a 2 2 0 1 0 -4 0 M 11.939 14 c 0 0.173 0.048 0.351 0.056 0.533 l 0 0.217 a 4.75 4.75 0 0 1 -4.533 4.745 l -0.217 0 m -4.75 -4.75 a 4.75 4.75 0 0 1 7.737 -3.693 m 6.513 8.443 a 4.75 4.75 0 0 1 -4.69 -5.503 l -0.06 0 m 1.764 -2.944 a 4.75 4.75 0 0 1 7.731 3.477 l 0 0.217 m -11.195 -3.813 a 4.75 4.75 0 0 1 -1.828 -7.624 l 0.164 -0.172 m 6.718 0 a 4.75 4.75 0 0 1 -1.665 7.798" }, // tabler:biohazard
            { "insekt", "M 9 9 v -1 a 3 3 0 0 1 6 0 v 1 M 8 9 h 8 a 6 6 0 0 1 1 3 v 3 a 5 5 0 0 1 -10 0 v -3 a 6 6 0 0 1 1 -3 M 3 13 l 4 0 M 17 13 l 4 0 M 12 20 l 0 -6 M 4 19 l 3.35 -2 M 20 19 l -3.35 -2 M 4 7 l 3.75 2.4 M 20 7 l -3.75 2.4" }, // tabler:bug
            { "uebelkeit", "M 12 21 a 9 9 0 1 1 0 -18 a 9 9 0 0 1 0 18 M 9 10 h -0.01 M 15 10 h -0.01 M 8 16 l 1 -1 l 1.5 1 l 1.5 -1 l 1.5 1 l 1.5 -1 l 1 1" }, // tabler:mood-sick
            { "rauchen", "M 3 14 a 1 1 0 0 1 1 -1 h 16 a 1 1 0 0 1 1 1 v 2 a 1 1 0 0 1 -1 1 h -16 a 1 1 0 0 1 -1 -1 l 0 -2 M 8 13 l 0 4 M 16 5 v 0.5 a 2 2 0 0 0 2 2 a 2 2 0 0 1 2 2 v 0.5" }, // tabler:smoking
            { "rauchstopp", "M 8 13 l 0 4 M 16 5 v 0.5 a 2 2 0 0 0 2 2 a 2 2 0 0 1 2 2 v 0.5 M 3 3 l 18 18 M 17 13 h 3 a 1 1 0 0 1 1 1 v 2 c 0 0.28 -0.115 0.533 -0.3 0.714 m -3.7 0.286 h -13 a 1 1 0 0 1 -1 -1 v -2 a 1 1 0 0 1 1 -1 h 9" }, // tabler:smoking-no
            { "alkohol", "M 8 21 l 8 0 M 12 15 l 0 6 M 17 3 l 1 7 c 0 3.012 -2.686 5 -6 5 s -6 -1.988 -6 -5 l 1 -7 h 10 M 6 10 a 5 5 0 0 1 6 0 a 5 5 0 0 0 6 0" }, // tabler:glass-full
            { "trinken", "M 9 21 h 6 a 1 1 0 0 0 1 -1 v -3.625 c 0 -1.397 0.29 -2.775 0.845 -4.025 l 0.31 -0.7 c 0.556 -1.25 0.845 -2.253 0.845 -3.65 v -4 a 1 1 0 0 0 -1 -1 h -10 a 1 1 0 0 0 -1 1 v 4 c 0 1.397 0.29 2.4 0.845 3.65 l 0.31 0.7 a 9.931 9.931 0 0 1 0.845 4.025 v 3.625 a 1 1 0 0 0 1 1 M 6 8 h 12" }, // tabler:beer
            { "ernaehrung", "M 12 6.528 V 3 a 1 1 0 0 1 1 -1 h 0 M 18.237 21 A 15 15 0 0 0 22 11 a 6 6 0 0 0 -10 -4.472 A 6 6 0 0 0 2 11 a 15.1 15.1 0 0 0 3.763 10 a 3 3 0 0 0 3.648 0.648 a 5.5 5.5 0 0 1 5.178 0 A 3 3 0 0 0 18.237 21" }, // lucide:apple
            { "bewegung", "M 12 4 a 1 1 0 1 0 2 0 a 1 1 0 1 0 -2 0 M 7 21 l 3 -4 M 16 21 l -2 -4 l -3 -3 l 1 -6 M 6 12 l 2 -3 l 4 -1 l 3 3 l 3 1" }, // tabler:walk
            { "schlaf", "M 4 12 h 6 l -6 8 h 6 M 14 4 h 6 l -6 8 h 6" }, // tabler:zzz
            { "stimmung", "M 3 12 a 9 9 0 1 0 18 0 a 9 9 0 1 0 -18 0 M 9 10 l 0.01 0 M 15 10 l 0.01 0 M 9.5 15.25 a 3.5 3.5 0 0 1 5 0" }, // tabler:mood-sad
            { "sturz", "M 11 21 l 1 -5 l -1 -4 l -3 -4 h 4 l 3 -3 M 6 16 l -1 -4 l 3 -4 M 5 5 a 1 1 0 1 0 2 0 a 1 1 0 1 0 -2 0 M 13.5 12 h 2.5 l 4 2" }, // tabler:fall
            { "unfall", "M 8 17 a 2 2 0 1 0 4 0 a 2 2 0 1 0 -4 0 M 7 6 l 4 5 h 1 a 2 2 0 0 1 2 2 v 4 h -2 m -4 0 h -5 m 0 -6 h 8 m -6 0 v -5 m 2 0 h -4 M 14 8 v -2 M 19 12 h 2 M 17.5 15.5 l 1.5 1.5 M 17.5 8.5 l 1.5 -1.5" }, // tabler:car-crash
            { "rettungsdienst", "M 5 17 a 2 2 0 1 0 4 0 a 2 2 0 1 0 -4 0 M 15 17 a 2 2 0 1 0 4 0 a 2 2 0 1 0 -4 0 M 5 17 h -2 v -11 a 1 1 0 0 1 1 -1 h 9 v 12 m -4 0 h 6 m 4 0 h 2 v -6 h -8 m 0 -5 h 5 l 3 5 M 6 10 h 4 m -2 -2 v 4" }, // tabler:ambulance
            { "krankenhaus", "M 3 21 l 18 0 M 5 21 v -16 a 2 2 0 0 1 2 -2 h 10 a 2 2 0 0 1 2 2 v 16 M 9 21 v -4 a 2 2 0 0 1 2 -2 h 2 a 2 2 0 0 1 2 2 v 4 M 10 9 l 4 0 M 12 7 l 0 4" }, // tabler:building-hospital
            { "hausbesuch", "M 12.35 21 H 5 a 2 2 0 0 1 -2 -2 v -9 a 2 2 0 0 1 0.71 -1.53 l 7 -6 a 2 2 0 0 1 2.58 0 l 7 6 A 2 2 0 0 1 21 10 v 2.35 M 14.8 12.4 A 1 1 0 0 0 14 12 h -4 a 1 1 0 0 0 -1 1 v 8 M 15 18 h 6 M 18 15 v 6" }, // lucide:house-plus
            { "bescheinigung", "M 14 3 v 4 a 1 1 0 0 0 1 1 h 4 M 5 8 v -3 a 2 2 0 0 1 2 -2 h 7 l 5 5 v 11 a 2 2 0 0 1 -2 2 h -5 M 3 14 a 3 3 0 1 0 6 0 a 3 3 0 1 0 -6 0 M 4.5 17 l -1.5 5 l 3 -1.5 l 3 1.5 l -1.5 -5" }, // tabler:file-certificate
            { "pflege", "M 12 5 c 2.941 0 6.685 1.537 9 3 l -2 11 h -14 l -2 -11 c 2.394 -1.513 6.168 -3.005 9 -3 M 10 12 h 4 M 12 10 v 4" }, // tabler:nurse
            { "trage", "M 14 18 a 2 2 0 1 0 4 0 a 2 2 0 1 0 -4 0 M 6 18 a 2 2 0 1 0 4 0 a 2 2 0 1 0 -4 0 M 4 8 l 2.1 2.8 a 3 3 0 0 0 2.4 1.2 h 11.5 M 10 6 h 4 M 12 4 v 4 M 12 12 v 2 l -2.5 2.5 M 14.5 16.5 l -2.5 -2.5" }, // tabler:emergency-bed
            { "kalender", "M8 2 v4 M16 2 v4 M5 4 h14 a2 2 0 0 1 2 2 v14 a2 2 0 0 1 -2 2 H5 a2 2 0 0 1 -2 -2 V6 a2 2 0 0 1 2 -2 z M3 10 h18" },
            { "uhr", "M22 12 a10 10 0 1 1 -20 0 a10 10 0 1 1 20 0 M12 6 v6 l4 2" },
            { "person", "M19 21 v-2 a4 4 0 0 0 -4 -4 H9 a4 4 0 0 0 -4 4 v2 M16 7 a4 4 0 1 1 -8 0 a4 4 0 1 1 8 0" },
            { "dokument", "M15 2 H6 a2 2 0 0 0 -2 2 v16 a2 2 0 0 0 2 2 h12 a2 2 0 0 0 2 -2 V7 Z M14 2 v4 a2 2 0 0 0 2 2 h4 M8 13 h8 M8 17 h8" },
            { "stift", "M17 3 a2.85 2.83 0 1 1 4 4 L7.5 20.5 L2 22 l1.5 -5.5 Z M15 5 l4 4" },
            { "kopieren", "M10 8 h10 a2 2 0 0 1 2 2 v10 a2 2 0 0 1 -2 2 H10 a2 2 0 0 1 -2 -2 V10 a2 2 0 0 1 2 -2 z M4 16 c-1.1 0 -2 -0.9 -2 -2 V4 c0 -1.1 0.9 -2 2 -2 h10 c1.1 0 2 0.9 2 2" },
            { "einfuegen", "M9 2 h6 a1 1 0 0 1 1 1 v2 a1 1 0 0 1 -1 1 H9 a1 1 0 0 1 -1 -1 V3 a1 1 0 0 1 1 -1 z M16 4 h2 a2 2 0 0 1 2 2 v14 a2 2 0 0 1 -2 2 H6 a2 2 0 0 1 -2 -2 V6 a2 2 0 0 1 2 -2 h2" },
            { "auswahl", "M5 3 a2 2 0 0 0 -2 2 M19 3 a2 2 0 0 1 2 2 M21 19 a2 2 0 0 1 -2 2 M5 21 a2 2 0 0 1 -2 -2 M9 3 h1 M9 21 h1 M14 3 h1 M14 21 h1 M3 9 v1 M21 9 v1 M3 14 v1 M21 14 v1" },
            { "ausschnitt", "M6 2 v14 a2 2 0 0 0 2 2 h14 M18 22 V8 a2 2 0 0 0 -2 -2 H2" },
            { "suche", "M19 11 a8 8 0 1 1 -16 0 a8 8 0 1 1 16 0 M21 21 l-4.3 -4.3" },
            { "haken", "M20 6 L9 17 l-5 -5" },
            { "ordner", "M20 20 a2 2 0 0 0 2 -2 V8 a2 2 0 0 0 -2 -2 h-7.9 a2 2 0 0 1 -1.69 -0.9 L9.6 3.9 A2 2 0 0 0 7.93 3 H4 a2 2 0 0 0 -2 2 v13 a2 2 0 0 0 2 2 Z" },
            { "mail", "M4 4 h16 a2 2 0 0 1 2 2 v12 a2 2 0 0 1 -2 2 H4 a2 2 0 0 1 -2 -2 V6 a2 2 0 0 1 2 -2 z M22 7 l-8.97 5.7 a1.94 1.94 0 0 1 -2.06 0 L2 7" },
            { "globus", "M22 12 a10 10 0 1 1 -20 0 a10 10 0 1 1 20 0 M2 12 h20 M12 2 a15.3 15.3 0 0 1 4 10 a15.3 15.3 0 0 1 -4 10 a15.3 15.3 0 0 1 -4 -10 a15.3 15.3 0 0 1 4 -10 z" },
            { "rechner", "M6 2 h12 a2 2 0 0 1 2 2 v16 a2 2 0 0 1 -2 2 H6 a2 2 0 0 1 -2 -2 V4 a2 2 0 0 1 2 -2 z M8 6 h8 M16 14 v4 M16 10 h0.01 M12 10 h0.01 M8 10 h0.01 M12 14 h0.01 M8 14 h0.01 M12 18 h0.01 M8 18 h0.01" },
            { "werkzeug", "M14.7 6.3 a1 1 0 0 0 0 1.4 l1.6 1.6 a1 1 0 0 0 1.4 0 l3.77 -3.77 a6 6 0 0 1 -7.94 7.94 l-6.91 6.91 a2.12 2.12 0 0 1 -3 -3 l6.91 -6.91 a6 6 0 0 1 7.94 -7.94 l-3.76 3.76 z" },
            { "schloss", "M5 11 h14 a2 2 0 0 1 2 2 v7 a2 2 0 0 1 -2 2 H5 a2 2 0 0 1 -2 -2 v-7 a2 2 0 0 1 2 -2 z M7 11 V7 a5 5 0 0 1 10 0 v4" },
            { "blitz", "M13 2 L3 14 h9 l-1 8 l10 -12 h-9 l1 -8 z" },
            { "wiedergabe", "M6 3 l14 9 l-14 9 z" },
            { "plus", "M5 12 h14 M12 5 v14" }
        };

        static readonly Dictionary<string, Geometry> Cache = new Dictionary<string, Geometry>();

        static Geometry Vector(string name)
        {
            Geometry g;
            if (Cache.TryGetValue(name, out g)) return g;
            string data;
            if (Vectors.TryGetValue(name, out data))
            {
                try { g = Geometry.Parse(data); g.Freeze(); }
                catch (FormatException) { g = null; }
            }
            Cache[name] = g;
            return g;
        }

        public static string NameOf(string id)
        {
            foreach (var section in Sections)
                foreach (var e in section.Value) if (e[0] == id) return e[1];
            return "";
        }

        /// <summary>Zeichnet Bild oder Symbol mittig in <paramref name="box"/>. Bilder behalten ihre eigenen Farben.</summary>
        public static void Draw(DrawingContext dc, string icon, BitmapSource image, Rect box, Brush brush, double pixelsPerDip)
        {
            if (image != null)
            {
                double k = Math.Min(box.Width / image.PixelWidth, box.Height / image.PixelHeight);
                double w = image.PixelWidth * k, h = image.PixelHeight * k;
                var r = new Rect(box.X + (box.Width - w) / 2, box.Y + (box.Height - h) / 2, w, h);
                // Runde Ecken, damit auch ein rechteckiges Foto wie ein Tastenbild sitzt und nicht wie ein Aufkleber.
                double corner = Math.Min(w, h) * 0.2;
                dc.PushClip(new RectangleGeometry(r, corner, corner));
                dc.DrawImage(image, r);
                dc.Pop();
                return;
            }
            if (string.IsNullOrEmpty(icon) || icon.Length < 3) return;
            if (icon.StartsWith("v:", StringComparison.Ordinal))
            {
                var g = Vector(icon.Substring(2));
                if (g == null) return;
                double s = Math.Min(box.Width, box.Height) / 24.0;
                var pen = new Pen(brush, 1.75) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round, LineJoin = PenLineJoin.Round };
                dc.PushTransform(new MatrixTransform(s, 0, 0, s, box.X + (box.Width - 24 * s) / 2, box.Y + (box.Height - 24 * s) / 2));
                dc.DrawGeometry(null, pen, g);
                dc.Pop();
                return;
            }
            if (icon.StartsWith("g:", StringComparison.Ordinal))
            {
                int code;
                if (!int.TryParse(icon.Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out code)) return;
                // Die Symbolschrift füllt ihr Geviert nicht ganz; 0.92 gleicht sie optisch an die Linien-Symbole an.
                double size = Math.Min(box.Width, box.Height) * 0.92;
                var ft = new FormattedText(char.ConvertFromUtf32(code), CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                                           Face, size, brush, null, TextFormattingMode.Ideal, pixelsPerDip);
                dc.DrawText(ft, new Point(box.X + (box.Width - ft.Width) / 2, box.Y + (box.Height - ft.Height) / 2));
            }
        }
    }

    /// <summary>Zeigt ein einzelnes Symbol, z. B. in der Symbolauswahl.</summary>
    public sealed class IconView : FrameworkElement
    {
        string _icon = "";
        BitmapSource _image;
        Brush _brush = Brushes.Black;

        public IconView() { RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.HighQuality); }

        public void Set(string icon, BitmapSource image, Brush brush)
        {
            _icon = icon ?? "";
            _image = image;
            _brush = brush;
            InvalidateVisual();
        }

        protected override void OnRender(DrawingContext dc)
        {
            Icons.Draw(dc, _icon, _image, new Rect(0, 0, ActualWidth, ActualHeight), _brush, VisualTreeHelper.GetDpi(this).PixelsPerDip);
        }
    }
}
