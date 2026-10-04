using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Kyklos
{
    /// <summary>
    /// Das Rad am Mauszeiger. Das Fenster nimmt nie den Fokus und lässt Klicks durch; Auswahl und Klicks kommen
    /// über die Hooks, die Zeigerposition wird pro Bild abgefragt.
    ///
    /// Es gibt zwei Fenster, die sich abwechseln: Ein durchsichtiges Fenster zeigt beim Verschieben oder Vergrößern
    /// noch einen Augenblick sein altes Bild an der neuen Stelle – das flackert. Deshalb wird ein sichtbares Fenster nie
    /// bewegt. Ein Unterrad erscheint im anderen Fenster, während das alte Rad in seinem stehen bleibt und zurücktritt.
    /// </summary>
    public sealed class Overlay
    {
        enum Mode { Closed, Held, Sticky, Closing }

        const double OpenSeconds = 0.13, SubOpenSeconds = 0.17, CloseSeconds = 0.11, TapSeconds = 0.28, DwellSeconds = 0.45;

        sealed class Layer
        {
            public readonly WheelView View = new WheelView();
            public readonly Window Win;
            public IntPtr Hwnd;
            public double RecedeAt;
            public int HideIn = -1;     // ab 0: wird versteckt, sobald ein leeres Bild auf dem Schirm war

            public Layer()
            {
                Win = new Window
                {
                    Title = "Kyklos-Rad",
                    WindowStyle = WindowStyle.None,
                    AllowsTransparency = true,
                    Background = Brushes.Transparent,
                    ResizeMode = ResizeMode.NoResize,
                    ShowInTaskbar = false,
                    ShowActivated = false,
                    Topmost = true,
                    Focusable = false,
                    IsHitTestVisible = false,
                    WindowStartupLocation = WindowStartupLocation.Manual,
                    Left = -32000, Top = -32000, Width = 64, Height = 64,
                    Content = View
                };
                Win.SourceInitialized += (s, e) =>
                {
                    Hwnd = new WindowInteropHelper(Win).Handle;
                    int ex = Native.GetWindowLong(Hwnd, Native.GWL_EXSTYLE);
                    Native.SetWindowLong(Hwnd, Native.GWL_EXSTYLE,
                                         ex | Native.WS_EX_NOACTIVATE | Native.WS_EX_TOOLWINDOW | Native.WS_EX_TRANSPARENT);
                };
                new WindowInteropHelper(Win).EnsureHandle();
            }

            /// <summary>Sichtbar und nicht schon auf dem Weg nach draußen.</summary>
            public bool Showing { get { return Win.IsVisible && HideIn < 0; } }

            /// <summary>
            /// Erst ein leeres Bild zeichnen, dann verstecken – sonst taucht beim nächsten Zeigen kurz das alte Bild auf.
            /// </summary>
            public void Retire()
            {
                if (!Win.IsVisible || HideIn >= 0) return;
                View.Fade = 0;
                View.InvalidateVisual();
                HideIn = 1;
            }
        }

        readonly AppHost _host;
        readonly Layer[] _layers = { new Layer(), new Layer() };
        Layer _cur, _old;                   // _old: das Rad, das gerade zurücktritt
        readonly Stopwatch _clock = Stopwatch.StartNew();
        Mode _mode = Mode.Closed;
        List<Slot> _slots = new List<Slot>();
        Native.POINT _origin, _center;
        double _dpi = 1, _scale = 1;
        double _last, _openAt, _openSeconds = OpenSeconds, _closeAt, _downAt, _dwellAt;
        int _dwell = -1;
        bool _frames, _motion = true;
        BitmapSource _backdrop;             // Milchglas: der Bildschirm, wie er vor dem Öffnen aussah
        Native.RECT _backdropOf, _backdropMon;

        public Overlay(AppHost host)
        {
            _host = host;
            _cur = _layers[0];
        }

        WheelView View { get { return _cur.View; } }

        Layer Other(Layer l) { return l == _layers[0] ? _layers[1] : _layers[0]; }

        public bool IsOpen { get { return _mode == Mode.Held || _mode == Mode.Sticky; } }

        double Now { get { return _clock.Elapsed.TotalSeconds; } }

        public void Open(Wheel wheel)
        {
            if (IsOpen) return;
            _scale = _host.Config.Settings.Scale;
            Native.GetCursorPos(out _origin);
            _downAt = Now;
            _backdrop = null;
            Show(wheel.Slots, wheel.Name, _origin, Mode.Held);
        }

        void Show(List<Slot> slots, string title, Native.POINT at, Mode mode)
        {
            // Ein Unterrad löst ein schon offenes Rad ab: Dann tritt das alte in seinem Fenster zurück, und das neue
            // erscheint im anderen. Auch sonst wird ein noch sichtbares Fenster nicht verschoben, sondern abgelöst.
            bool motion = SystemParameters.ClientAreaAnimation;
            bool sub = _cur.Showing && IsOpen;
            var skin = sub ? _cur.View.Skin : Skin.Get(_host.Config.Settings.Skin);
            if (_cur.Showing)
            {
                var prev = _cur;
                if (_old != null) { _old.Retire(); _old = null; }
                _cur = Other(prev);
                if (sub && motion)
                {
                    _old = prev;
                    _old.RecedeAt = Now;
                    _old.View.PointerOn = false;
                }
                else prev.Retire();
            }
            _cur.HideIn = -1;
            var view = View;
            view.Skin = skin;
            view.Recede = 0;
            _slots = slots;
            double outer = WheelView.OuterFor(slots.Count);

            IntPtr monitor = Native.MonitorFromPoint(at, 2);
            var mi = new Native.MONITORINFO { cbSize = Marshal.SizeOf(typeof(Native.MONITORINFO)) };
            Native.GetMonitorInfo(monitor, ref mi);
            _dpi = Native.DpiScale(monitor);
            // Milchglas: einmal pro Öffnen abgreifen, solange das Rad noch nicht auf dem Bildschirm liegt – ein Bereich um
            // den Zeiger, der auch Unterräder abdeckt. Unterräder nutzen dasselbe Bild; ein neuer Abgriff zeigte das Rad,
            // das gerade noch zu sehen ist.
            if (skin.Decor == Decor.Frost && (_backdrop == null || !Same(mi.rcMonitor, _backdropMon)))
            {
                int reach = (int)Math.Ceiling(2 * WheelView.HalfExtent * _scale * _dpi);
                var mr = mi.rcMonitor;
                _backdropOf = new Native.RECT
                {
                    left = Math.Max(mr.left, at.x - reach), top = Math.Max(mr.top, at.y - reach),
                    right = Math.Min(mr.right, at.x + reach), bottom = Math.Min(mr.bottom, at.y + reach)
                };
                _backdropMon = mr;
                _backdrop = Backdrop.Capture(_backdropOf);
            }

            // Die Scheibe bleibt ganz auf dem Bildschirm; rückt die Mitte, kommt der Zeiger mit.
            int half = (int)Math.Ceiling((outer + 10) * _scale * _dpi);
            Native.POINT c = at;
            var r = mi.rcMonitor;
            if (r.right - r.left > 2 * half) c.x = Math.Max(r.left + half, Math.Min(r.right - half, c.x));
            if (r.bottom - r.top > 2 * half) c.y = Math.Max(r.top + half, Math.Min(r.bottom - half, c.y));
            _center = c;
            if (c.x != at.x || c.y != at.y) Native.SetCursorPos(c.x, c.y);

            view.Scale = _scale;
            view.EditMode = false;
            view.Hover = -1;
            view.Flash = -1;
            view.PointerOn = false;
            // Windows-Einstellung „Animationen anzeigen" respektieren: ohne sie steht das Rad sofort da.
            _motion = motion;
            view.Motion = _motion;
            view.Open = _motion ? 0 : 1;
            view.Fade = 1;
            view.Deeper = sub;
            _openSeconds = sub ? SubOpenSeconds : OpenSeconds;
            view.SetContent(slots, title, "Mitte bricht ab");
            view.GazeTarget = new Vector((at.x - c.x) / (_dpi * _scale), (at.y - c.y) / (_dpi * _scale));
            view.ResetDecor();

            int px = (int)Math.Ceiling(WheelView.HalfExtent * _scale * _dpi);
            view.Backdrop = skin.Decor == Decor.Frost ? _backdrop : null;
            var br = _backdropOf;
            view.BackdropRect = new Rect((br.left - (c.x - px)) / _dpi, (br.top - (c.y - px)) / _dpi,
                                         (br.right - br.left) / _dpi, (br.bottom - br.top) / _dpi);
            if (!_cur.Win.IsVisible) _cur.Win.Show();
            // Zweimal: Wechselt das Fenster dabei auf einen Monitor mit anderer Skalierung, passt Windows die Größe
            // beim ersten Aufruf noch einmal an. HWND_TOPMOST legt es zugleich über ein zurücktretendes Rad.
            for (int n = 0; n < 2; n++)
                Native.SetWindowPos(_cur.Hwnd, Native.HWND_TOPMOST, c.x - px, c.y - px, 2 * px, 2 * px,
                                    Native.SWP_NOACTIVATE | Native.SWP_SHOWWINDOW);

            _mode = mode;
            _openAt = _last = Now;
            _dwell = -1;
            if (!_frames) { CompositionTarget.Rendering += OnFrame; _frames = true; }
        }

        static bool Same(Native.RECT a, Native.RECT b)
        {
            return a.left == b.left && a.top == b.top && a.right == b.right && a.bottom == b.bottom;
        }

        /// <summary>Segment unter dem Zeiger, frisch gemessen (-1 = Mitte).</summary>
        int Probe(out double dist, out double angle, out Native.POINT p)
        {
            Native.GetCursorPos(out p);
            double k = _dpi * _scale;
            double dx = (p.x - _center.x) / k, dy = (p.y - _center.y) / k;
            dist = Math.Sqrt(dx * dx + dy * dy);
            angle = Math.Atan2(dy, dx);
            if (dist < WheelView.DeadZone || _slots.Count == 0) return -1;
            return WheelView.IndexFromAngle(angle, _slots.Count);
        }

        void OnFrame(object sender, EventArgs e)
        {
            double now = Now, dt = Math.Min(0.05, now - _last);
            _last = now;

            // Fenster, die zuletzt ein leeres Bild gezeigt haben, jetzt verstecken.
            foreach (var l in _layers)
            {
                if (l.HideIn > 0) l.HideIn--;
                else if (l.HideIn == 0) { l.Win.Hide(); l.HideIn = -1; }
            }

            if (_old != null)
            {
                double t = (now - _old.RecedeAt) / WheelView.RecedeSeconds;
                if (t >= 1) { _old.Retire(); _old = null; }
                else
                {
                    _old.View.Recede = t;
                    _old.View.Tick(dt);
                    _old.View.InvalidateVisual();
                }
            }

            if (_mode == Mode.Closing)
            {
                double t = (now - _closeAt) / CloseSeconds;
                if (t >= 1) Finish();
                else
                {
                    View.Fade = 1 - t * t;
                    View.Tick(dt);
                    View.InvalidateVisual();
                }
            }
            else if (_mode != Mode.Closed) Steer(now, dt);

            if (_mode == Mode.Closed && _old == null && !_layers[0].Win.IsVisible && !_layers[1].Win.IsVisible)
            {
                CompositionTarget.Rendering -= OnFrame;
                _frames = false;
            }
        }

        void Steer(double now, double dt)
        {
            var view = View;
            double dist, angle;
            Native.POINT p;
            int hover = Probe(out dist, out angle, out p);
            bool dirty = hover != view.Hover || view.Open < 1 || (hover >= 0 && Math.Abs(angle - view.PointerAngle) > 0.004)
                         || view.PointerOn != (hover >= 0);
            view.Open = _motion ? Math.Min(1, (now - _openAt) / _openSeconds) : 1;
            view.Hover = hover;
            view.PointerOn = hover >= 0;
            view.PointerAngle = angle;
            view.GazeTarget = new Vector(Math.Cos(angle) * dist, Math.Sin(angle) * dist);

            // Unterrad: am Außenrand sofort, sonst nach kurzem Verweilen auf der Taste.
            if (hover >= 0 && _slots[hover].Action.Type == ActionType.Folder && _slots[hover].Action.Slots.Count >= Wheel.MinSlots)
            {
                if (_dwell != hover) { _dwell = hover; _dwellAt = now; }
                if (dist >= WheelView.OuterFor(_slots.Count) - 6 || now - _dwellAt > DwellSeconds)
                {
                    // Den Stand mit leuchtender Ordner-Taste noch zeichnen – so tritt das Rad zurück, wie es zuletzt aussah.
                    view.Tick(dt);
                    view.InvalidateVisual();
                    Show(_slots[hover].Action.Slots, _slots[hover].DisplayLabel, p, _mode);
                    return;
                }
            }
            else _dwell = -1;

            if (view.Tick(dt) || dirty) view.InvalidateVisual();
        }

        public void OnTriggerUp()
        {
            if (_mode != Mode.Held) return;
            double dist, angle;
            Native.POINT p;
            int hover = Probe(out dist, out angle, out p);
            if (hover >= 0) Commit(hover, p);
            else if (_host.Config.Settings.TapSticky && Now - _downAt < TapSeconds) _mode = Mode.Sticky;
            else Cancel();
        }

        public void OnClick()
        {
            if (!IsOpen) return;
            double dist, angle;
            Native.POINT p;
            int hover = Probe(out dist, out angle, out p);
            if (hover >= 0) Commit(hover, p); else Cancel();
        }

        public void OnRepress()
        {
            if (_mode == Mode.Sticky) Cancel();
        }

        public void Cancel()
        {
            if (!IsOpen) return;
            Close(-1);
        }

        void Commit(int index, Native.POINT cursor)
        {
            var slot = _slots[index];
            var action = slot.Action;
            if (action.Type == ActionType.None) { Cancel(); return; }
            if (action.Type == ActionType.Folder)
            {
                if (action.Slots.Count < Wheel.MinSlots) { Cancel(); return; }
                Show(action.Slots, slot.DisplayLabel, cursor, Mode.Sticky);
                return;
            }
            Close(index);
            _host.Run(action, slot);
        }

        void Close(int flash)
        {
            View.Flash = flash;
            View.Hover = -1;
            View.PointerOn = false;
            _mode = Mode.Closing;
            _closeAt = Now;
            InputHook.EndSession();
            if (_host.Config.Settings.RestoreCursor || _center.x != _origin.x || _center.y != _origin.y)
                Native.SetCursorPos(_origin.x, _origin.y);
            if (!_motion) Finish();
        }

        /// <summary>Das Rad ist zu. Die Bildschleife läuft weiter, bis beide Fenster ein leeres Bild gezeigt haben.</summary>
        void Finish()
        {
            _mode = Mode.Closed;
            _cur.Retire();
            if (_old != null) { _old.Retire(); _old = null; }
        }
    }
}
