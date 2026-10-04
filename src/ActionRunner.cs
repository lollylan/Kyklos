using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Kyklos
{
    /// <summary>Erzeugt Tastatureingaben über SendInput. Alle Ereignisse tragen unsere Signatur.</summary>
    public static class KeySender
    {
        static readonly int Size = Marshal.SizeOf(typeof(Native.INPUT));

        static bool Extended(int vk)
        {
            return (vk >= 0x21 && vk <= 0x28) || vk == 0x2D || vk == 0x2E || vk == 0x5B || vk == 0x5C || vk == 0x5D
                || vk == 0x6F || vk == 0x90 || vk == 0x2C || vk == Native.VK_RCONTROL || vk == Native.VK_RMENU || (vk >= 0xA6 && vk <= 0xB7);
        }

        static Native.INPUT Key(int vk, bool up)
        {
            var i = new Native.INPUT { type = Native.INPUT_KEYBOARD };
            i.u.ki.wVk = (ushort)vk;
            i.u.ki.wScan = (ushort)Native.MapVirtualKey((uint)vk, 0);
            i.u.ki.dwFlags = (up ? Native.KEYEVENTF_KEYUP : 0) | (Extended(vk) ? Native.KEYEVENTF_EXTENDEDKEY : 0);
            i.u.ki.dwExtraInfo = Native.Signature;
            return i;
        }

        static Native.INPUT Unicode(char c, bool up)
        {
            var i = new Native.INPUT { type = Native.INPUT_KEYBOARD };
            i.u.ki.wScan = c;
            i.u.ki.dwFlags = Native.KEYEVENTF_UNICODE | (up ? Native.KEYEVENTF_KEYUP : 0);
            i.u.ki.dwExtraInfo = Native.Signature;
            return i;
        }

        static void Send(List<Native.INPUT> list)
        {
            if (list.Count > 0) Native.SendInput((uint)list.Count, list.ToArray(), Size);
        }

        public static void Tap(int vk, int times = 1)
        {
            var l = new List<Native.INPUT>();
            for (int n = 0; n < times; n++) { l.Add(Key(vk, false)); l.Add(Key(vk, true)); }
            Send(l);
        }

        public static void Combo(Chord c)
        {
            if (c == null || c.IsEmpty || c.Mouse) return;
            var mods = new List<int>();
            if (c.Ctrl) mods.Add(Native.VK_CONTROL);
            if (c.Alt) mods.Add(Native.VK_MENU);
            if (c.Shift) mods.Add(Native.VK_SHIFT);
            if (c.Win) mods.Add(Native.VK_LWIN);
            var l = new List<Native.INPUT>();
            foreach (int m in mods) l.Add(Key(m, false));
            l.Add(Key(c.Code, false));
            l.Add(Key(c.Code, true));
            for (int n = mods.Count - 1; n >= 0; n--) l.Add(Key(mods[n], true));
            Send(l);
        }

        /// <summary>
        /// Hält der Nutzer vom Auslöser noch Strg, Alt, Umschalt oder Win, würden sie sich in unsere Eingaben mischen
        /// (aus Strg+V würde Strg+Alt+V). Deshalb lassen wir sie für das Zielprogramm los.
        /// </summary>
        public static void ReleaseModifiers()
        {
            var l = new List<Native.INPUT>();
            foreach (int vk in new[] { Native.VK_LSHIFT, Native.VK_RSHIFT, Native.VK_LCONTROL, Native.VK_RCONTROL,
                                       Native.VK_LMENU, Native.VK_RMENU, Native.VK_LWIN, Native.VK_RWIN })
                if (Native.IsDown(vk)) l.Add(Key(vk, true));
            Send(l);
        }

        /// <summary>Tippt Text Zeichen für Zeichen; funktioniert auch dort, wo Einfügen gesperrt ist.</summary>
        public static async Task TypeText(string text)
        {
            var l = new List<Native.INPUT>();
            int inBatch = 0;
            foreach (char ch in text)
            {
                if (ch == '\r') continue;
                if (ch == '\n') { l.Add(Key(Native.VK_RETURN, false)); l.Add(Key(Native.VK_RETURN, true)); }
                else if (ch == '\t') { l.Add(Key(Native.VK_TAB, false)); l.Add(Key(Native.VK_TAB, true)); }
                else { l.Add(Unicode(ch, false)); l.Add(Unicode(ch, true)); }
                if (++inBatch >= 24)
                {
                    Send(l);
                    l.Clear();
                    inBatch = 0;
                    await Task.Delay(8);    // langsame Programme verlieren sonst Zeichen
                }
            }
            Send(l);
        }
    }

    /// <summary>Sichert den Inhalt der Zwischenablage, damit er nach dem Einfügen wieder da ist.</summary>
    public sealed class ClipboardSnapshot
    {
        readonly List<KeyValuePair<string, object>> _data = new List<KeyValuePair<string, object>>();

        public static ClipboardSnapshot Take()
        {
            var snap = new ClipboardSnapshot();
            try
            {
                IDataObject d = Clipboard.GetDataObject();
                if (d == null) return snap;
                foreach (string f in d.GetFormats(false))
                {
                    try
                    {
                        object o = d.GetData(f, false);
                        var ms = o as MemoryStream;
                        if (ms != null) o = new MemoryStream(ms.ToArray());
                        else if (!(o is string) && !(o is string[])) continue;      // Bilder usw. hängen an fremden Handles
                        snap._data.Add(new KeyValuePair<string, object>(f, o));
                    }
                    catch (Exception) { }   // einzelne Formate dürfen fehlen
                }
            }
            catch (Exception) { }
            return snap;
        }

        public void Restore()
        {
            ClipboardText.Retry(() =>
            {
                if (_data.Count == 0) { Clipboard.Clear(); return; }
                var d = new DataObject();
                foreach (var kv in _data) d.SetData(kv.Key, kv.Value, false);
                Clipboard.SetDataObject(d, true);
            });
        }
    }

    public static class ClipboardText
    {
        public static bool Retry(Action a)
        {
            for (int n = 0; n < 12; n++)
            {
                try { a(); return true; }
                catch (COMException) { Thread.Sleep(25); }       // Zwischenablage gerade von einem anderen Programm belegt
                catch (ExternalException) { Thread.Sleep(25); }
            }
            return false;
        }

        public static bool Set(string text)
        {
            return Retry(() =>
            {
                var d = new DataObject();
                d.SetData(DataFormats.UnicodeText, text, true);
                // Bittet Zwischenablage-Verlauf und Cloud-Synchronisierung, den Baustein nicht aufzuzeichnen.
                d.SetData("ExcludeClipboardContentFromMonitorProcessing", new MemoryStream(new byte[4]), false);
                d.SetData("CanIncludeInClipboardHistory", new MemoryStream(new byte[4]), false);
                d.SetData("CanUploadToCloudClipboard", new MemoryStream(new byte[4]), false);
                Clipboard.SetDataObject(d, true);
            });
        }

        public static string Get()
        {
            string s = "";
            Retry(() => { s = Clipboard.ContainsText() ? Clipboard.GetText() : ""; });
            return s;
        }
    }

    /// <summary>Führt Aktionen nacheinander auf dem UI-Thread aus.</summary>
    public static class ActionRunner
    {
        static readonly SemaphoreSlim Gate = new SemaphoreSlim(1, 1);

        /// <param name="origin">Segment, das die Aktion ausgelöst hat – liefert Titel und Farbe für die Lückenabfrage.</param>
        public static async void Enqueue(ActionDef action, Settings settings, Slot origin = null)
        {
            await Gate.WaitAsync();
            try
            {
                KeySender.ReleaseModifiers();
                await Run(action, settings, origin, 0);
            }
            catch (Exception ex) { Log.Write("Aktion fehlgeschlagen: " + ex); }
            finally { Gate.Release(); }
        }

        static async Task Run(ActionDef a, Settings st, Slot origin, int depth)
        {
            switch (a.Type)
            {
                case ActionType.Text:
                    await InsertText(a, st, origin);
                    break;
                case ActionType.Keys:
                    KeySender.Combo(a.Keys);
                    break;
                case ActionType.Open:
                    if (!string.IsNullOrWhiteSpace(a.Path)) Start(Environment.ExpandEnvironmentVariables(a.Path.Trim()), a.Args);
                    break;
                case ActionType.Url:
                    if (!string.IsNullOrWhiteSpace(a.Url))
                    {
                        string u = a.Url.Trim();
                        if (u.IndexOf("://", StringComparison.Ordinal) < 0) u = "https://" + u;
                        Start(u, null);
                    }
                    break;
                case ActionType.Media:
                    Media(a.Media);
                    break;
                case ActionType.Delay:
                    await Task.Delay(Math.Max(0, Math.Min(60000, a.DelayMs)));
                    break;
                case ActionType.Macro:
                    if (depth > 4) break;
                    for (int i = 0; i < a.Steps.Count; i++)
                    {
                        await Run(a.Steps[i], st, origin, depth + 1);
                        if (i + 1 < a.Steps.Count) await Task.Delay(Math.Max(0, Math.Min(5000, a.StepDelayMs)));
                    }
                    break;
            }
        }

        static void Start(string target, string args)
        {
            try
            {
                var psi = new ProcessStartInfo(target) { UseShellExecute = true };
                if (!string.IsNullOrWhiteSpace(args)) psi.Arguments = args;
                try
                {
                    string dir = Path.GetDirectoryName(target);
                    if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir)) psi.WorkingDirectory = dir;
                }
                catch (ArgumentException) { }
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                Log.Write("Öffnen fehlgeschlagen (" + target + "): " + ex.Message);
                MessageBox.Show("„" + target + "“ konnte nicht geöffnet werden.\n\n" + ex.Message, "Kyklos",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        static void Media(string id)
        {
            switch (id)
            {
                case "playpause": KeySender.Tap(0xB3); break;
                case "next": KeySender.Tap(0xB0); break;
                case "prev": KeySender.Tap(0xB1); break;
                case "stop": KeySender.Tap(0xB2); break;
                case "volup": KeySender.Tap(0xAF, 2); break;
                case "voldown": KeySender.Tap(0xAE, 2); break;
                case "mute": KeySender.Tap(0xAD); break;
                case "snip": KeySender.Combo(Chord.Key('S', shift: true, win: true)); break;
                case "desktop": KeySender.Combo(Chord.Key('D', win: true)); break;
                case "lock": Native.LockWorkStation(); break;
            }
        }

        public const string SampleMark = "[Beispieltext] ";

        static async Task InsertText(ActionDef a, Settings st, Slot origin)
        {
            string raw = Variants.Resolve(a.Text);
            if (Gaps.Has(raw))
            {
                raw = await FillWindow.Ask(raw, origin == null ? "" : origin.DisplayLabel,
                                           origin == null ? Palette.Parse(Palette.Default) : origin.ColorValue, Skin.Get(st.Skin));
                if (raw == null) return;    // abgebrochen
            }
            int back;
            string text = Expand(raw, out back);
            if (text.Length == 0) return;
            // Mitgelieferte Beispieltexte dürfen nicht unbemerkt als eigener Befund in einer Karteikarte landen.
            if (a.Sample) text = SampleMark + text;

            if (a.Mode == TextMode.Type)
            {
                await KeySender.TypeText(text);
            }
            else
            {
                ClipboardSnapshot snap = st.RestoreClipboard ? ClipboardSnapshot.Take() : null;
                if (!ClipboardText.Set(text.Replace("\n", "\r\n")))
                {
                    await KeySender.TypeText(text);     // Zwischenablage dauerhaft belegt: lieber tippen als nichts tun
                }
                else
                {
                    await Task.Delay(40);
                    KeySender.Combo(Chord.Key('V', ctrl: true));
                    if (snap != null)
                    {
                        // Das Zielprogramm liest die Zwischenablage erst, wenn es Strg+V verarbeitet.
                        await Task.Delay(st.PasteDelayMs);
                        snap.Restore();
                    }
                }
            }

            if (back > 0)
            {
                await Task.Delay(a.Mode == TextMode.Type ? 30 : 90);
                KeySender.Tap(Native.VK_LEFT, back);
            }
        }

        /// <summary>
        /// Ersetzt die Platzhalter. {cursor} wird entfernt; <paramref name="back"/> sagt, wie viele Zeichen die
        /// Schreibmarke danach nach links wandern muss, um an dieser Stelle zu stehen.
        /// </summary>
        public static string Expand(string text, out int back)
        {
            string s = ExpandDates(text);
            if (s.Contains("{zwischenablage}")) s = s.Replace("{zwischenablage}", ClipboardText.Get().Replace("\r\n", "\n"));
            back = 0;
            int at = s.IndexOf("{cursor}", StringComparison.Ordinal);
            if (at >= 0)
            {
                s = s.Remove(at, "{cursor}".Length).Replace("{cursor}", "");
                back = s.Length - at;
            }
            return s;
        }

        static string ExpandDates(string text)
        {
            DateTime now = DateTime.Now;
            var sb = new StringBuilder(text.Replace("\r\n", "\n").Replace('\r', '\n'));
            sb.Replace("{datum}", now.ToString("dd.MM.yyyy"));
            sb.Replace("{uhrzeit}", now.ToString("HH:mm"));
            sb.Replace("{wochentag}", now.ToString("dddd", new System.Globalization.CultureInfo("de-DE")));
            return sb.ToString();
        }

        /// <summary>Für die Vorschau beim Ausfüllen: Datum und Zeit eingesetzt, ohne die Zwischenablage zu lesen.</summary>
        public static string ExpandForPreview(string text)
        {
            return ExpandDates(text).Replace("{zwischenablage}", "[Zwischenablage]").Replace("{cursor}", "");
        }
    }

    public static class Log
    {
        public static string File = "";

        public static void Write(string line)
        {
            if (string.IsNullOrEmpty(File)) return;
            try { System.IO.File.AppendAllText(File, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "  " + line + "\r\n"); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }
}
