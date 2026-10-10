using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Kyklos
{
    /// <summary>
    /// Findet das Fenster eines Programms und holt es nach vorn – für den Makro-Schritt „Fenster nach vorn holen“.
    /// Anders als ein Mausklick klappt das auch, wenn das Fenster verdeckt, verschoben oder minimiert ist.
    /// </summary>
    public static class WindowFocus
    {
        public sealed class Info
        {
            public IntPtr Handle;
            public string Title = "", ExePath = "";
            public string ExeName { get { return Path.GetFileName(ExePath); } }
        }

        /// <summary>Universal-Apps laufen in diesem Rahmenprozess; an ihm lässt sich das Programm nicht erkennen.</summary>
        public const string AppFrameHost = "ApplicationFrameHost.exe";

        /// <summary>Alle Fenster, die in der Taskleiste stehen würden – das zuletzt benutzte zuerst.</summary>
        public static List<Info> List()
        {
            var list = new List<Info>();
            uint self = (uint)System.Diagnostics.Process.GetCurrentProcess().Id;
            Native.EnumWindows((h, l) =>
            {
                if (!Native.IsWindowVisible(h) || Native.GetWindow(h, Native.GW_OWNER) != IntPtr.Zero) return true;
                int ex = Native.GetWindowLong(h, Native.GWL_EXSTYLE);
                if ((ex & Native.WS_EX_TOOLWINDOW) != 0) return true;
                int cloaked;
                if (Native.DwmGetWindowAttribute(h, Native.DWMWA_CLOAKED, out cloaked, 4) == 0 && cloaked != 0) return true;
                // Ohne Titel ist meist ein Hilfsfenster – außer es meldet sich ausdrücklich für die Taskleiste an (z. B. Heidi).
                int len = Native.GetWindowTextLength(h);
                if (len == 0 && (ex & Native.WS_EX_APPWINDOW) == 0) return true;
                uint pid;
                Native.GetWindowThreadProcessId(h, out pid);
                if (pid == self) return true;
                var title = new StringBuilder(len + 1);
                Native.GetWindowText(h, title, title.Capacity);
                list.Add(new Info { Handle = h, Title = title.ToString(), ExePath = ProcessPath(pid) });
                return true;
            }, IntPtr.Zero);
            return list;
        }

        /// <summary>Das Programm, das gerade vorn ist: Exe-Name ohne Endung, Fenstertitel und Titel des Hauptfensters.</summary>
        public sealed class Front { public string ExeKey = "", Title = "", OwnerTitle = ""; }

        static uint _lastPid;
        static IntPtr _lastWnd;
        static string _lastKey = "";

        /// <summary>
        /// Läuft im Tastatur-Hook beim Druck auf einen Auslöser, deshalb knapp gehalten: Der Programmname wird zum Fenster
        /// gemerkt, meist bleibt man ja im selben. Fenster und Prozessnummer zusammen werden nicht so bald neu vergeben.
        /// </summary>
        public static Front Foreground()
        {
            var f = new Front();
            IntPtr h = Native.GetForegroundWindow();
            if (h == IntPtr.Zero) return f;
            uint pid;
            Native.GetWindowThreadProcessId(h, out pid);
            if (pid != _lastPid || h != _lastWnd)
            {
                _lastKey = AppScope.ProgramKey(ProcessPath(pid));
                _lastPid = pid;
                _lastWnd = h;
            }
            f.ExeKey = _lastKey;
            f.Title = TitleOf(h);
            IntPtr root = Native.GetAncestor(h, Native.GA_ROOTOWNER);
            f.OwnerTitle = root != IntPtr.Zero && root != h ? TitleOf(root) : "";
            return f;
        }

        static string TitleOf(IntPtr h)
        {
            int len = Native.GetWindowTextLength(h);
            if (len <= 0) return "";
            var sb = new StringBuilder(len + 1);
            Native.GetWindowText(h, sb, sb.Capacity);
            return sb.ToString();
        }

        public static string ProcessPath(uint pid)
        {
            IntPtr p = Native.OpenProcess(Native.PROCESS_QUERY_LIMITED_INFORMATION, false, pid);
            if (p == IntPtr.Zero) return "";
            try
            {
                var sb = new StringBuilder(1024);
                int size = sb.Capacity;
                return Native.QueryFullProcessImageName(p, 0, sb, ref size) ? sb.ToString(0, size) : "";
            }
            finally { Native.CloseHandle(p); }
        }

        /// <summary>„C:\…\Transkript.exe“, „Transkript.exe“ und „Transkript“ meinen dasselbe Programm.</summary>
        static string ProgramKey(string path) { return AppScope.ProgramKey(path); }

        /// <summary>Das zuletzt benutzte passende Fenster, oder null.</summary>
        public static Info Find(ActionDef a)
        {
            string want = ProgramKey(a.Path), title = (a.Title ?? "").Trim();
            if (want.Length == 0 && title.Length == 0) return null;
            foreach (var w in List())
            {
                if (want.Length > 0 && !string.Equals(ProgramKey(w.ExePath), want, StringComparison.OrdinalIgnoreCase)) continue;
                if (title.Length > 0 && w.Title.IndexOf(title, StringComparison.OrdinalIgnoreCase) < 0) continue;
                return w;
            }
            return null;
        }

        /// <summary>
        /// Holt das Fenster nach vorn, startet das Programm notfalls und meldet, ob es geklappt hat. Ohne Erfolg soll das Makro
        /// abbrechen, damit die folgenden Tasten nicht im falschen Programm landen.
        /// </summary>
        public static async Task<bool> Bring(ActionDef a)
        {
            var w = Find(a);
            if (w == null && a.Launch && !string.IsNullOrWhiteSpace(a.Path)
                && ActionRunner.Start(Environment.ExpandEnvironmentVariables(a.Path.Trim().Trim('"')), null))
            {
                for (int n = 0; n < 60 && (w = Find(a)) == null; n++) await Task.Delay(250);
                if (w != null) await Task.Delay(400);   // frisch gestartet: Das Fenster steht, der Inhalt lädt oft noch
            }
            if (w == null)
            {
                Log.Write("Fenster nicht gefunden: " + a.Summary());
                return false;
            }
            return await Activate(w.Handle);
        }

        /// <summary>
        /// Windows gibt den Vordergrund nur ungern an ein Programm, das gerade nicht vorn ist. Erst mit der Eingabe des vorderen
        /// Programms verbunden versuchen; reicht das nicht, zweimal Alt tippen – das löst die Sperre, und der zweite Druck
        /// schließt die Menüleiste wieder, die der erste im vorderen Programm geöffnet hat.
        /// </summary>
        public static async Task<bool> Activate(IntPtr hwnd)
        {
            if (Native.IsIconic(hwnd)) Native.ShowWindow(hwnd, Native.SW_RESTORE);
            for (int attempt = 0; attempt < 3; attempt++)
            {
                if (attempt > 0) KeySender.Tap(Native.VK_MENU, 2);
                Force(hwnd);
                for (int n = 0; n < 15; n++)
                {
                    if (Native.GetForegroundWindow() == hwnd)
                    {
                        await Task.Delay(80);   // erst weiter, wenn das Fenster seinen Tastaturfokus zurückhat
                        return true;
                    }
                    await Task.Delay(20);
                }
                if (!Native.IsWindow(hwnd)) return false;
            }
            Log.Write("Fenster ließ sich nicht nach vorn holen.");
            return false;
        }

        static void Force(IntPtr hwnd)
        {
            IntPtr fg = Native.GetForegroundWindow();
            uint pid;
            uint other = Native.GetWindowThreadProcessId(fg, out pid), me = Native.GetCurrentThreadId();
            bool attached = other != 0 && other != me && Native.AttachThreadInput(me, other, true);
            try
            {
                Native.BringWindowToTop(hwnd);
                Native.SetForegroundWindow(hwnd);
            }
            finally
            {
                if (attached) Native.AttachThreadInput(me, other, false);
            }
        }
    }
}
