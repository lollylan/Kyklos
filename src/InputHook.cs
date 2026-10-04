using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Threading;

namespace Kyklos
{
    /// <summary>
    /// Globale Tastatur- und Maus-Hooks. Sie laufen auf einem eigenen Thread, damit die Eingabe des ganzen Systems
    /// nie auf die Oberfläche warten muss; die Rückrufe entscheiden nur „schlucken oder durchlassen" und reichen
    /// alles Weitere an den UI-Thread.
    /// </summary>
    public static class InputHook
    {
        public sealed class Binding { public Chord Trigger; public string WheelId; }

        public static event Action<Binding> TriggerDown;  // Auslöser gedrückt (welcher, für welches Rad)
        public static event Action TriggerUp;             // Auslöser losgelassen
        public static event Action Repress;               // Auslöser erneut gedrückt, während das Rad offen steht
        public static event Action Click;                 // Linksklick bei offenem Rad
        public static event Action Cancel;                // Esc oder Rechtsklick bei offenem Rad

        /// <summary>Ist gesetzt, solange ein Feld in den Einstellungen auf eine Taste wartet. null als Ergebnis = abgebrochen.</summary>
        public static volatile Action<Chord> RecordCallback;
        public static volatile bool RecordAllowMouse;
        public static volatile bool Paused;

        static volatile Binding[] _bindings = new Binding[0];
        static volatile Binding _active;        // Rad ist offen
        static volatile bool _released;         // Auslöser schon losgelassen (Rad steht per Antippen offen)

        // Nur auf dem Hook-Thread benutzt: Tasten, deren Drücken geschluckt wurde – ihr Loslassen muss auch weg.
        // Wert ist der Zeitpunkt des letzten geschluckten Drückens (Environment.TickCount).
        static readonly Dictionary<int, int> _eatUp = new Dictionary<int, int>();

        static void Eat(int key) { _eatUp[key] = Environment.TickCount; }

        /// <summary>
        /// true, solange eine geschluckte Taste noch gehalten wird: Ihre Tastenwiederholungen dürfen weder beim Programm
        /// ankommen noch das Rad neu öffnen (etwa nach Esc bei weiter gehaltenem Auslöser). Kommt länger als die
        /// langsamste Wiederholrate nichts, ging das Loslassen verloren – dann zählt der nächste Druck wieder normal.
        /// </summary>
        static bool StillHeld(int key, bool repeats)
        {
            int at;
            if (!_eatUp.TryGetValue(key, out at)) return false;
            if (repeats && unchecked(Environment.TickCount - at) < 1200)
            {
                _eatUp[key] = Environment.TickCount;
                return true;
            }
            _eatUp.Remove(key);
            return false;
        }

        static Dispatcher _ui, _hookDispatcher;
        static IntPtr _kbHook, _mouseHook;
        static Native.HookProc _kbProc, _mouseProc;     // Referenzen halten, sonst räumt die GC die Rückrufe ab

        public static void Start(Dispatcher ui)
        {
            _ui = ui;
            var ready = new ManualResetEventSlim();
            var t = new Thread(() =>
            {
                _hookDispatcher = Dispatcher.CurrentDispatcher;
                _kbProc = KeyboardProc;
                _mouseProc = MouseProc;
                IntPtr mod = Native.GetModuleHandle(null);
                _kbHook = Native.SetWindowsHookEx(Native.WH_KEYBOARD_LL, _kbProc, mod, 0);
                _mouseHook = Native.SetWindowsHookEx(Native.WH_MOUSE_LL, _mouseProc, mod, 0);
                ready.Set();
                Dispatcher.Run();
                if (_kbHook != IntPtr.Zero) Native.UnhookWindowsHookEx(_kbHook);
                if (_mouseHook != IntPtr.Zero) Native.UnhookWindowsHookEx(_mouseHook);
            });
            t.IsBackground = true;
            t.Name = "Kyklos-Hooks";
            t.SetApartmentState(ApartmentState.STA);
            t.Start();
            ready.Wait(3000);
        }

        public static bool Installed { get { return _kbHook != IntPtr.Zero && _mouseHook != IntPtr.Zero; } }

        public static void Stop()
        {
            if (_hookDispatcher != null) _hookDispatcher.BeginInvokeShutdown(DispatcherPriority.Send);
        }

        public static void SetBindings(IEnumerable<Binding> bindings)
        {
            _bindings = new List<Binding>(bindings).ToArray();
        }

        /// <summary>Das Rad ist zu: Eingaben gehen wieder ungefiltert an die Programme.</summary>
        public static void EndSession()
        {
            _active = null;
            _released = false;
        }

        static void Post(Action a)
        {
            if (a != null) _ui.BeginInvoke(DispatcherPriority.Send, a);
        }

        static Binding Match(bool mouse, int code)
        {
            bool ctrl = Native.IsDown(Native.VK_CONTROL), alt = Native.IsDown(Native.VK_MENU), shift = Native.IsDown(Native.VK_SHIFT),
                 win = Native.IsDown(Native.VK_LWIN) || Native.IsDown(Native.VK_RWIN);
            foreach (var b in _bindings)
            {
                var t = b.Trigger;
                if (t.Mouse == mouse && t.Code == code && t.Ctrl == ctrl && t.Alt == alt && t.Shift == shift && t.Win == win) return b;
            }
            return null;
        }

        static Chord Capture(bool mouse, int code)
        {
            return new Chord
            {
                Mouse = mouse, Code = code,
                Ctrl = Native.IsDown(Native.VK_CONTROL), Alt = Native.IsDown(Native.VK_MENU), Shift = Native.IsDown(Native.VK_SHIFT),
                Win = Native.IsDown(Native.VK_LWIN) || Native.IsDown(Native.VK_RWIN)
            };
        }

        static IntPtr KeyboardProc(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                var k = Marshal.PtrToStructure<Native.KBDLLHOOKSTRUCT>(lParam);
                if (k.dwExtraInfo != Native.Signature)
                {
                    int msg = (int)wParam;
                    bool down = msg == Native.WM_KEYDOWN || msg == Native.WM_SYSKEYDOWN;
                    if (HandleKey((int)k.vkCode, down)) return (IntPtr)1;
                }
            }
            return Native.CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
        }

        static bool HandleKey(int vk, bool down)
        {
            if (!down)
            {
                if (!_eatUp.Remove(vk)) return false;
                var a = _active;
                if (a != null && !a.Trigger.Mouse && a.Trigger.Code == vk && !_released)
                {
                    _released = true;
                    Post(TriggerUp);
                }
                return true;
            }

            var rec = RecordCallback;
            if (rec != null)
            {
                if (KeyNames.IsModifier(vk)) return false;
                RecordCallback = null;
                Eat(vk);
                Chord c = vk == Native.VK_ESCAPE ? null : Capture(false, vk);
                Post(() => rec(c));
                return true;
            }

            var act = _active;
            if (act != null)
            {
                if (!act.Trigger.Mouse && act.Trigger.Code == vk)
                {
                    Eat(vk);                 // Tastenwiederholung beim Halten oder erneutes Drücken
                    if (_released) Post(Repress);
                    return true;
                }
                if (vk == Native.VK_ESCAPE)
                {
                    Eat(vk);
                    Post(Cancel);
                    return true;
                }
                return false;
            }

            if (StillHeld(vk, true)) return true;
            if (Paused) return false;
            var b = Match(false, vk);
            if (b == null) return false;
            _active = b;
            _released = false;
            Eat(vk);
            var down1 = TriggerDown;
            if (down1 != null) Post(() => down1(b));
            return true;
        }

        static IntPtr MouseProc(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int msg = (int)wParam;
                if (msg != Native.WM_MOUSEMOVE && msg != Native.WM_MOUSEWHEEL && msg != Native.WM_MOUSEHWHEEL)
                {
                    var m = Marshal.PtrToStructure<Native.MSLLHOOKSTRUCT>(lParam);
                    if (m.dwExtraInfo != Native.Signature && HandleMouse(msg, m.mouseData)) return (IntPtr)1;
                }
            }
            return Native.CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
        }

        const int Left = -1, Right = -2;    // intern; Auslöser-Tasten sind 1 (Mitte), 2 und 3 (Seitentasten)

        static bool HandleMouse(int msg, uint mouseData)
        {
            int btn; bool down;
            switch (msg)
            {
                case Native.WM_LBUTTONDOWN: btn = Left; down = true; break;
                case Native.WM_LBUTTONUP: btn = Left; down = false; break;
                case Native.WM_RBUTTONDOWN: btn = Right; down = true; break;
                case Native.WM_RBUTTONUP: btn = Right; down = false; break;
                case Native.WM_MBUTTONDOWN: btn = 1; down = true; break;
                case Native.WM_MBUTTONUP: btn = 1; down = false; break;
                case Native.WM_XBUTTONDOWN: btn = (mouseData >> 16) == 1 ? 2 : 3; down = true; break;
                case Native.WM_XBUTTONUP: btn = (mouseData >> 16) == 1 ? 2 : 3; down = false; break;
                default: return false;
            }
            int key = 0x10000 + btn;    // eigener Nummernkreis neben den Tastencodes

            if (!down)
            {
                if (!_eatUp.Remove(key)) return false;
                var a = _active;
                if (a != null && a.Trigger.Mouse && a.Trigger.Code == btn && !_released)
                {
                    _released = true;
                    Post(TriggerUp);
                }
                return true;
            }

            var rec = RecordCallback;
            if (rec != null)
            {
                if (btn <= 0 || !RecordAllowMouse) return false;
                RecordCallback = null;
                Eat(key);
                Chord c = Capture(true, btn);
                Post(() => rec(c));
                return true;
            }

            var act = _active;
            if (act != null)
            {
                if (act.Trigger.Mouse && act.Trigger.Code == btn)
                {
                    Eat(key);
                    if (_released) Post(Repress);
                    return true;
                }
                if (btn == Left) { Eat(key); Post(Click); return true; }
                if (btn == Right) { Eat(key); Post(Cancel); return true; }
                return false;
            }

            StillHeld(key, false);      // Maustasten wiederholen nicht: ein alter Eintrag ist immer ein verlorenes Loslassen
            if (Paused || btn <= 0) return false;
            var b = Match(true, btn);
            if (b == null) return false;
            _active = b;
            _released = false;
            Eat(key);
            var down1 = TriggerDown;
            if (down1 != null) Post(() => down1(b));
            return true;
        }
    }
}
