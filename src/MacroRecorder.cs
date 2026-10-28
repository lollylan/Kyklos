using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace Kyklos
{
    /// <summary>Eine Taste, wie der Hook sie während einer Makro-Aufnahme in einem fremden Programm gesehen hat.</summary>
    public sealed class RecordedKey
    {
        public int Vk, Scan, Time;              // Time: Millisekunden-Zeitstempel des Systems
        public bool Ctrl, Alt, Shift, Win, AltGr, Caps;
        public IntPtr Layout;                   // Tastaturlayout des Programms vorn – daraus folgt das Zeichen
    }

    /// <summary>
    /// Macht aus aufgenommenen Tasten Makro-Schritte: Jede Taste wird ein Tasten-Schritt, längeres Warten dazwischen eine
    /// Pause. Nur mit <see cref="LettersAsText"/> wird Getipptes zu einem Text-Schritt; Pausen darin zählen dann nicht,
    /// und die Rücktaste korrigiert den gerade getippten Text, statt einen eigenen Schritt zu bekommen.
    /// Standard sind Einzeltasten: Viele Programme nutzen einzelne Buchstaben als Kürzel, und eingefügter Text löst die nicht aus.
    /// </summary>
    public sealed class MacroRecorder
    {
        public const int MinPauseMs = 300;      // kürzeres Warten deckt die Pause zwischen den Schritten ab

        public readonly List<ActionDef> Steps = new List<ActionDef>();
        public bool LettersAsText;              // lässt sich während der Aufnahme umschalten
        int _last;
        bool _started;
        char _dead;                             // gedrückte Tottaste (^ ´ `), wartet auf ihren Buchstaben
        int _deadGap;                           // Warten vor der Tottaste – zählt für das fertige Zeichen

        public void Add(RecordedKey k)
        {
            bool dead;
            string ch = CharOf(k, out dead);
            int gap = _started ? unchecked(k.Time - _last) : 0;
            _started = true;
            _last = k.Time;

            if (!LettersAsText)
            {
                FlushDead();
                AddKey(k, gap);
                return;
            }
            if (dead)
            {
                FlushDead();
                _dead = ch[0];
                _deadGap = gap;
                return;
            }
            if (_dead != 0)
            {
                if (ch != null) { ch = Compose(_dead, ch); _dead = default(char); gap = _deadGap; }
                else FlushDead();
            }

            if (ch != null) { AddText(ch, gap); return; }

            var text = LastText();
            if (k.Vk == Native.VK_BACK && !k.Ctrl && !k.Alt && !k.Win && !k.Shift && text != null)
            {
                text.Text = text.Text.Substring(0, text.Text.Length - 1);
                if (text.Text.Length == 0)
                {
                    Steps.RemoveAt(Steps.Count - 1);
                    if (Steps.Count > 0 && Steps[Steps.Count - 1].Type == ActionType.Delay) Steps.RemoveAt(Steps.Count - 1);
                }
                return;
            }
            AddKey(k, gap);
        }

        void AddKey(RecordedKey k, int gap)
        {
            Pause(gap);
            Steps.Add(new ActionDef { Type = ActionType.Keys, Keys = Chord.Key(k.Vk, k.Ctrl, k.Alt, k.Shift, k.Win) });
        }

        /// <summary>
        /// Zerlegt einen Text in Tasten-Schritte, wie das Tastaturlayout sie tippen würde (Großbuchstaben mit Umschalt,
        /// @ mit Strg+Alt). Zeichen ohne eigene Taste – etwa ê, das eine Tottaste braucht – bleiben als Text dazwischen stehen.
        /// </summary>
        public static List<ActionDef> SplitText(string text, IntPtr layout)
        {
            var steps = new List<ActionDef>();
            var rest = new StringBuilder();
            Action flush = () =>
            {
                if (rest.Length == 0) return;
                steps.Add(new ActionDef { Type = ActionType.Text, Text = rest.ToString() });
                rest.Clear();
            };
            foreach (char c in text.Replace("\r\n", "\n"))
            {
                Chord key = null;
                if (c == '\n') key = Chord.Key(Native.VK_RETURN);
                else if (c == '\t') key = Chord.Key(Native.VK_TAB);
                else
                {
                    short scan = Native.VkKeyScanEx(c, layout);
                    int vk = scan & 0xFF, mods = (scan >> 8) & 0xFF;
                    if (scan != -1 && vk != 0xFF && (mods & ~7) == 0)
                        key = Chord.Key(vk, ctrl: (mods & 2) != 0, alt: (mods & 4) != 0, shift: (mods & 1) != 0);
                }
                if (key == null) { rest.Append(c); continue; }
                flush();
                steps.Add(new ActionDef { Type = ActionType.Keys, Keys = key });
            }
            flush();
            return steps;
        }

        /// <summary>Schließt die Aufnahme ab: eine zuletzt allein gedrückte Tottaste kommt als Zeichen dazu.</summary>
        public List<ActionDef> Finish()
        {
            FlushDead();
            return Steps;
        }

        ActionDef LastText()
        {
            if (Steps.Count == 0) return null;
            var s = Steps[Steps.Count - 1];
            return s.Type == ActionType.Text ? s : null;
        }

        void AddText(string ch, int gap)
        {
            var text = LastText();
            if (text != null) { text.Text += ch; return; }
            Pause(gap);
            Steps.Add(new ActionDef { Type = ActionType.Text, Text = ch });
        }

        void FlushDead()
        {
            if (_dead == 0) return;
            string ch = _dead.ToString();
            _dead = default(char);
            AddText(ch, _deadGap);
        }

        void Pause(int gap)
        {
            if (Steps.Count == 0 || gap < MinPauseMs) return;
            Steps.Add(new ActionDef { Type = ActionType.Delay, DelayMs = Math.Min(60000, (gap + 25) / 50 * 50) });
        }

        /// <summary>Tottaste plus Buchstabe zum fertigen Zeichen (^ + e = ê); mit Leertaste bleibt das Zeichen selbst.</summary>
        static string Compose(char dead, string ch)
        {
            if (ch == " ") return dead.ToString();
            char mark;
            switch (dead)
            {
                case '^': mark = '̂'; break;
                case '´': mark = '́'; break;
                case '`': mark = '̀'; break;
                case '~': mark = '̃'; break;
                case '¨': mark = '̈'; break;
                case '°': mark = '̊'; break;
                default: return dead + ch;
            }
            string c = (ch + mark).Normalize(NormalizationForm.FormC);
            return c.Length == ch.Length ? c : dead + ch;
        }

        /// <summary>
        /// Das Zeichen, das die Taste im Layout des Programms erzeugt, oder null für Kürzel und Steuertasten.
        /// Flag 4 lässt den Tottasten-Zustand des Systems unberührt – sonst verlöre das Programm sein ^ oder ´.
        /// </summary>
        static string CharOf(RecordedKey k, out bool dead)
        {
            dead = false;
            if (k.Win || ((k.Ctrl || k.Alt) && !k.AltGr)) return null;
            if (k.Vk == Native.VK_BACK || k.Vk == Native.VK_TAB || k.Vk == Native.VK_RETURN || k.Vk == Native.VK_ESCAPE) return null;
            var state = new byte[256];
            if (k.Shift) state[Native.VK_SHIFT] = 0x80;
            if (k.AltGr) { state[Native.VK_CONTROL] = 0x80; state[Native.VK_MENU] = 0x80; }
            if (k.Caps) state[0x14] = 0x01;
            var buf = new StringBuilder(8);
            int n = Native.ToUnicodeEx((uint)k.Vk, (uint)k.Scan, state, buf, buf.Capacity, 4, k.Layout);
            if (n < 0)
            {
                if (buf.Length == 0 || buf[0] < 0x20) return null;
                dead = true;
                return buf[0].ToString();
            }
            if (n == 0) return null;
            string s = buf.ToString(0, Math.Min(n, buf.Length));
            foreach (char c in s) if (c < 0x20 || c == 0x7F) return null;
            return s;
        }
    }

    /// <summary>
    /// Leiste unten am Bildschirm, solange ein Makro aufgenommen wird. Sie nimmt nie den Fokus, damit die Tasten im
    /// Programm landen, in dem aufgenommen wird; ein Klick auf Stopp holt die Einstellungen zurück.
    /// </summary>
    public sealed class RecordBar
    {
        public event Action<bool> Done;         // true: übernehmen, false: verwerfen
        public event Action<bool> LettersAsTextChanged;

        readonly Window _win;
        readonly TextBlock _count;

        public RecordBar()
        {
            Func<string, Brush> res = key => (Brush)Application.Current.FindResource(key);
            var dot = new Ellipse { Width = 10, Height = 10, Fill = res("Danger"), VerticalAlignment = VerticalAlignment.Center };
            dot.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(1, 0.35, TimeSpan.FromSeconds(0.9))
            {
                AutoReverse = true, RepeatBehavior = RepeatBehavior.Forever, EasingFunction = new SineEase()
            });
            _count = new TextBlock { Foreground = res("Ink2"), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(6, 0, 20, 0) };
            SetCount(0);
            var discard = new Button
            {
                Style = (Style)Application.Current.FindResource("Btn"), Content = "Verwerfen", Margin = new Thickness(0, 0, 8, 0), Focusable = false
            };
            var stop = new Button { Style = (Style)Application.Current.FindResource("BtnPrimary"), Content = "Stopp", Focusable = false };
            var asText = new CheckBox
            {
                Style = (Style)Application.Current.FindResource("Switch"), Content = "Buchstaben als Text", Focusable = false,
                VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 20, 0),
                ToolTip = "Aus: Jeder Buchstabe wird ein echter Tastendruck – für Programme, die Buchstaben als Kürzel nutzen.\n" +
                          "An: Getipptes wird ein Text-Schritt und beim Abspielen eingefügt."
            };
            asText.Click += (s, e) =>
            {
                var changed = LettersAsTextChanged;
                if (changed != null) changed(asText.IsChecked == true);
            };
            discard.Click += (s, e) => Finish(false);
            stop.Click += (s, e) => Finish(true);

            var row = new StackPanel { Orientation = Orientation.Horizontal };
            row.Children.Add(dot);
            row.Children.Add(new TextBlock
            {
                Text = "Aufnahme läuft", FontWeight = FontWeights.SemiBold, Foreground = res("Ink"), VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10, 0, 0, 0)
            });
            row.Children.Add(_count);
            row.Children.Add(asText);
            row.Children.Add(discard);
            row.Children.Add(stop);

            _win = new Window
            {
                Title = "Kyklos – Makro-Aufnahme",
                WindowStyle = WindowStyle.None, ResizeMode = ResizeMode.NoResize, SizeToContent = SizeToContent.WidthAndHeight,
                ShowInTaskbar = false, ShowActivated = false, Topmost = true,
                WindowStartupLocation = WindowStartupLocation.Manual, Left = -32000, Top = -32000,
                Background = res("Surface"), BorderBrush = res("Line"), BorderThickness = new Thickness(1),
                FontFamily = new FontFamily("Segoe UI"), FontSize = 13,
                Content = new Border { Padding = new Thickness(16, 10, 10, 10), Child = row }
            };
            _win.SourceInitialized += (s, e) =>
            {
                var hwnd = new WindowInteropHelper(_win).Handle;
                int ex = Native.GetWindowLong(hwnd, Native.GWL_EXSTYLE);
                Native.SetWindowLong(hwnd, Native.GWL_EXSTYLE, ex | Native.WS_EX_NOACTIVATE | Native.WS_EX_TOOLWINDOW);
            };
            _win.Loaded += (s, e) =>
            {
                var wa = SystemParameters.WorkArea;
                _win.Left = wa.Left + (wa.Width - _win.ActualWidth) / 2;
                _win.Top = wa.Bottom - _win.ActualHeight - 24;
            };
        }

        public void Show() { _win.Show(); }

        public void SetCount(int steps)
        {
            _count.Text = steps == 0 ? "· tippe in einem anderen Programm" : steps == 1 ? "· 1 Schritt" : "· " + steps + " Schritte";
        }

        public void Finish(bool keep)
        {
            if (!_win.IsVisible) return;
            _win.Close();
            var done = Done;
            if (done != null) done(keep);
        }
    }
}
