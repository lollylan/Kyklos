using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace Kyklos
{
    /// <summary>
    /// Das Rad am Mauszeiger. Das Fenster nimmt nie den Fokus und lässt Klicks durch; Auswahl und Klicks kommen
    /// über die Hooks, die Zeigerposition wird pro Bild abgefragt.
    /// </summary>
    public sealed class Overlay
    {
        enum Mode { Closed, Held, Sticky, Closing }

        const double OpenSeconds = 0.13, CloseSeconds = 0.11, TapSeconds = 0.28, DwellSeconds = 0.45;

        readonly AppHost _host;
        readonly Window _win;
        readonly WheelView _view = new WheelView();
        readonly Stopwatch _clock = Stopwatch.StartNew();
        IntPtr _hwnd;
        Mode _mode = Mode.Closed;
        List<Slot> _slots = new List<Slot>();
        Native.POINT _origin, _center;
        double _dpi = 1, _scale = 1;
        double _last, _openAt, _closeAt, _downAt, _dwellAt;
        int _dwell = -1;
        bool _frames, _motion = true;

        public Overlay(AppHost host)
        {
            _host = host;
            _win = new Window
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
                Content = _view
            };
            _win.SourceInitialized += (s, e) =>
            {
                _hwnd = new WindowInteropHelper(_win).Handle;
                int ex = Native.GetWindowLong(_hwnd, Native.GWL_EXSTYLE);
                Native.SetWindowLong(_hwnd, Native.GWL_EXSTYLE,
                                     ex | Native.WS_EX_NOACTIVATE | Native.WS_EX_TOOLWINDOW | Native.WS_EX_TRANSPARENT);
            };
            new WindowInteropHelper(_win).EnsureHandle();
        }

        public bool IsOpen { get { return _mode == Mode.Held || _mode == Mode.Sticky; } }

        double Now { get { return _clock.Elapsed.TotalSeconds; } }

        public void Open(Wheel wheel)
        {
            if (IsOpen) return;
            _scale = _host.Config.Settings.Scale;
            Native.GetCursorPos(out _origin);
            _downAt = Now;
            Show(wheel.Slots, wheel.Name, _origin, Mode.Held);
        }

        void Show(List<Slot> slots, string title, Native.POINT at, Mode mode)
        {
            _slots = slots;
            double outer = WheelView.OuterFor(slots.Count);

            IntPtr monitor = Native.MonitorFromPoint(at, 2);
            var mi = new Native.MONITORINFO { cbSize = Marshal.SizeOf(typeof(Native.MONITORINFO)) };
            Native.GetMonitorInfo(monitor, ref mi);
            _dpi = Native.DpiScale(monitor);

            // Die Scheibe bleibt ganz auf dem Bildschirm; rückt die Mitte, kommt der Zeiger mit.
            int half = (int)Math.Ceiling((outer + 10) * _scale * _dpi);
            Native.POINT c = at;
            var r = mi.rcMonitor;
            if (r.right - r.left > 2 * half) c.x = Math.Max(r.left + half, Math.Min(r.right - half, c.x));
            if (r.bottom - r.top > 2 * half) c.y = Math.Max(r.top + half, Math.Min(r.bottom - half, c.y));
            _center = c;
            if (c.x != at.x || c.y != at.y) Native.SetCursorPos(c.x, c.y);

            _view.Scale = _scale;
            _view.EditMode = false;
            _view.Hover = -1;
            _view.Flash = -1;
            _view.PointerOn = false;
            // Windows-Einstellung „Animationen anzeigen" respektieren: ohne sie steht das Rad sofort da.
            _motion = SystemParameters.ClientAreaAnimation;
            _view.Motion = _motion;
            _view.Open = _motion ? 0 : 1;
            _view.Fade = 1;
            _view.SetContent(slots, title, "Mitte bricht ab");

            int px = (int)Math.Ceiling(WheelView.HalfExtent * _scale * _dpi);
            if (!_win.IsVisible) _win.Show();
            // Zweimal: Wechselt das Fenster dabei auf einen Monitor mit anderer Skalierung, passt Windows die Größe
            // beim ersten Aufruf noch einmal an.
            for (int n = 0; n < 2; n++)
                Native.SetWindowPos(_hwnd, Native.HWND_TOPMOST, c.x - px, c.y - px, 2 * px, 2 * px,
                                    Native.SWP_NOACTIVATE | Native.SWP_SHOWWINDOW);

            _mode = mode;
            _openAt = _last = Now;
            _dwell = -1;
            if (!_frames) { CompositionTarget.Rendering += OnFrame; _frames = true; }
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

            if (_mode == Mode.Closing)
            {
                double t = (now - _closeAt) / CloseSeconds;
                if (t >= 1) { Finish(); return; }
                _view.Fade = 1 - t * t;
                _view.Tick(dt);
                _view.InvalidateVisual();
                return;
            }
            if (_mode == Mode.Closed) return;

            double dist, angle;
            Native.POINT p;
            int hover = Probe(out dist, out angle, out p);
            bool dirty = hover != _view.Hover || _view.Open < 1 || (hover >= 0 && Math.Abs(angle - _view.PointerAngle) > 0.004)
                         || _view.PointerOn != (hover >= 0);
            _view.Open = _motion ? Math.Min(1, (now - _openAt) / OpenSeconds) : 1;
            _view.Hover = hover;
            _view.PointerOn = hover >= 0;
            _view.PointerAngle = angle;

            // Unterrad: am Außenrand sofort, sonst nach kurzem Verweilen auf der Taste.
            if (hover >= 0 && _slots[hover].Action.Type == ActionType.Folder && _slots[hover].Action.Slots.Count >= Wheel.MinSlots)
            {
                if (_dwell != hover) { _dwell = hover; _dwellAt = now; }
                if (dist >= WheelView.OuterFor(_slots.Count) - 6 || now - _dwellAt > DwellSeconds)
                {
                    Show(_slots[hover].Action.Slots, _slots[hover].DisplayLabel, p, _mode);
                    return;
                }
            }
            else _dwell = -1;

            if (_view.Tick(dt) || dirty) _view.InvalidateVisual();
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
            _view.Flash = flash;
            _view.Hover = -1;
            _view.PointerOn = false;
            _mode = Mode.Closing;
            _closeAt = Now;
            InputHook.EndSession();
            if (_host.Config.Settings.RestoreCursor || _center.x != _origin.x || _center.y != _origin.y)
                Native.SetCursorPos(_origin.x, _origin.y);
            if (!_motion) Finish();
        }

        void Finish()
        {
            _mode = Mode.Closed;
            _win.Hide();
            if (_frames) { CompositionTarget.Rendering -= OnFrame; _frames = false; }
        }
    }
}
