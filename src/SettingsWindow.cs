using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Win32;

namespace Kyklos
{
    /// <summary>
    /// Einstellungen: links die Räder, in der Mitte das Rad zum Anklicken, rechts der Inspektor des gewählten Segments.
    /// Das Layout kommt aus SettingsWindow.xaml; jede Änderung wirkt sofort und wird automatisch gespeichert.
    /// </summary>
    public sealed class SettingsWindow
    {
        readonly AppHost _host;
        readonly Window _w;
        public event Action Closed;

        Wheel _wheel;
        readonly List<Slot> _trail = new List<Slot>();   // geöffnete Unterräder, vom Hauptrad aus
        int _sel;
        bool _loading;
        int _dragFrom = -1;
        Button _recBtn;
        object _recOld;

        readonly ListBox WheelList;
        readonly ToggleButton NavGeneral;
        readonly TextBlock Status, CountText, TriggerNote, SlotTitle, SlotPos, ScaleText, ConfigPathText, VersionText, UsageHint;
        readonly Border SampleNote;
        readonly Button SampleKeep;
        readonly Grid EditorPanel;
        readonly ScrollViewer GeneralPanel;
        readonly Button AddWheel, CloneWheel, DeleteWheel, TriggerBtn, AddTrigger2, Trigger2Btn, Trigger2Clear, AddSlot, TestBtn, RemoveSlot, MoveCcw, MoveCw, IconBtn, ImageBtn, IconClear,
                        KeysBtn, BrowseBtn, OpenFolderBtn, OpenConfigDir, ExportBtn, ImportBtn, QuitBtn, GapFieldBtn, GapChoiceBtn, GapPickBtn,
                        VariantBtn, AlternBtn;
        readonly TextBox WheelName, LabelBox, TextBody, PathBox, ArgsBox, UrlBox, StepDelayBox, DelayBox;
        readonly ComboBox TypeBox, TextModeBox, MediaBox, AddStepBox;
        readonly StackPanel TriggerGroup, Trigger2Group, Crumbs, Swatches, PText, PKeys, POpen, PUrl, PMedia, PMacro, PFolder, StepsHost, IconHost;
        readonly CheckBox TapSwitch, CursorSwitch, ClipSwitch, AutostartSwitch;
        readonly Slider ScaleSlider;
        readonly UniformGrid SkinTiles;
        readonly TextBlock SkinHelp;
        readonly WheelView Preview;
        readonly Stopwatch _clock = Stopwatch.StartNew();
        double _lastFrame;
        bool _ticking;
        readonly IconView IconPreview;
        readonly Popup IconPopup;

        Config Cfg { get { return _host.Config; } }
        List<Slot> CurrentSlots { get { return _trail.Count == 0 ? _wheel.Slots : _trail[_trail.Count - 1].Action.Slots; } }
        Slot Cur { get { return CurrentSlots[_sel]; } }
        Brush Res(string key) { return (Brush)_w.FindResource(key); }
        T F<T>(string name) where T : class
        {
            var o = _w.FindName(name) as T;
            if (o == null) throw new InvalidOperationException("Element fehlt im Layout: " + name);
            return o;
        }

        public Window Window { get { return _w; } }

        public SettingsWindow(AppHost host)
        {
            _host = host;
            _w = (Window)Theme.LoadXaml("SettingsWindow.xaml");
            try
            {
                using (Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream("Kyklos.app.ico"))
                    if (s != null) _w.Icon = BitmapFrame.Create(s, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
            }
            catch (Exception) { }   // ohne Fenstersymbol geht es auch

            WheelList = F<ListBox>("WheelList"); NavGeneral = F<ToggleButton>("NavGeneral");
            Status = F<TextBlock>("Status"); CountText = F<TextBlock>("CountText"); TriggerNote = F<TextBlock>("TriggerNote");
            SlotTitle = F<TextBlock>("SlotTitle"); SlotPos = F<TextBlock>("SlotPos"); ScaleText = F<TextBlock>("ScaleText");
            ConfigPathText = F<TextBlock>("ConfigPathText"); VersionText = F<TextBlock>("VersionText");
            EditorPanel = F<Grid>("EditorPanel"); GeneralPanel = F<ScrollViewer>("GeneralPanel");
            AddWheel = F<Button>("AddWheel"); CloneWheel = F<Button>("CloneWheel"); DeleteWheel = F<Button>("DeleteWheel"); TriggerBtn = F<Button>("TriggerBtn");
            AddTrigger2 = F<Button>("AddTrigger2"); Trigger2Btn = F<Button>("Trigger2Btn"); Trigger2Clear = F<Button>("Trigger2Clear");
            TriggerGroup = F<StackPanel>("TriggerGroup"); Trigger2Group = F<StackPanel>("Trigger2Group");
            AddSlot = F<Button>("AddSlot"); TestBtn = F<Button>("TestBtn"); RemoveSlot = F<Button>("RemoveSlot");
            MoveCcw = F<Button>("MoveCcw"); MoveCw = F<Button>("MoveCw"); IconBtn = F<Button>("IconBtn"); ImageBtn = F<Button>("ImageBtn");
            IconClear = F<Button>("IconClear"); KeysBtn = F<Button>("KeysBtn"); BrowseBtn = F<Button>("BrowseBtn");
            OpenFolderBtn = F<Button>("OpenFolderBtn"); OpenConfigDir = F<Button>("OpenConfigDir"); ExportBtn = F<Button>("ExportBtn");
            ImportBtn = F<Button>("ImportBtn"); QuitBtn = F<Button>("QuitBtn");
            GapFieldBtn = F<Button>("GapFieldBtn"); GapChoiceBtn = F<Button>("GapChoiceBtn"); GapPickBtn = F<Button>("GapPickBtn");
            VariantBtn = F<Button>("VariantBtn"); AlternBtn = F<Button>("AlternBtn");
            WheelName = F<TextBox>("WheelName"); LabelBox = F<TextBox>("LabelBox"); TextBody = F<TextBox>("TextBody");
            PathBox = F<TextBox>("PathBox"); ArgsBox = F<TextBox>("ArgsBox"); UrlBox = F<TextBox>("UrlBox");
            StepDelayBox = F<TextBox>("StepDelayBox"); DelayBox = F<TextBox>("DelayBox");
            TypeBox = F<ComboBox>("TypeBox"); TextModeBox = F<ComboBox>("TextModeBox"); MediaBox = F<ComboBox>("MediaBox");
            AddStepBox = F<ComboBox>("AddStepBox");
            Crumbs = F<StackPanel>("Crumbs"); Swatches = F<StackPanel>("Swatches"); PText = F<StackPanel>("PText");
            PKeys = F<StackPanel>("PKeys"); POpen = F<StackPanel>("POpen"); PUrl = F<StackPanel>("PUrl"); PMedia = F<StackPanel>("PMedia");
            PMacro = F<StackPanel>("PMacro"); PFolder = F<StackPanel>("PFolder"); StepsHost = F<StackPanel>("StepsHost");
            IconHost = F<StackPanel>("IconHost");
            TapSwitch = F<CheckBox>("TapSwitch"); CursorSwitch = F<CheckBox>("CursorSwitch"); ClipSwitch = F<CheckBox>("ClipSwitch");
            AutostartSwitch = F<CheckBox>("AutostartSwitch");
            ScaleSlider = F<Slider>("ScaleSlider"); Preview = F<WheelView>("Preview"); IconPreview = F<IconView>("IconPreview");
            IconPopup = F<Popup>("IconPopup");
            UsageHint = F<TextBlock>("UsageHint"); SampleNote = F<Border>("SampleNote"); SampleKeep = F<Button>("SampleKeep");

            SkinTiles = F<UniformGrid>("SkinTiles"); SkinHelp = F<TextBlock>("SkinHelp");

            Preview.EditMode = true;
            Preview.Motion = SystemParameters.ClientAreaAnimation;
            Preview.Skin = Skin.Get(Cfg.Settings.Skin);
            BuildChoices();
            Wire();

            RebuildWheelList();
            SelectWheel(Cfg.Wheels[0]);
            LoadGeneral();

            _host.Saved += OnSaved;
            _host.SaveFailed += OnSaveFailed;
            _w.Deactivated += (s, e) => CancelRecord();
            _w.IsVisibleChanged += (s, e) => UpdateTicker();
            _w.Closed += (s, e) =>
            {
                CancelRecord();
                if (_ticking) { CompositionTarget.Rendering -= OnFrame; _ticking = false; }
                _host.Saved -= OnSaved;
                _host.SaveFailed -= OnSaveFailed;
                var closed = Closed;
                if (closed != null) closed();
            };
        }

        public void Show()
        {
            _w.Show();
            if (_w.WindowState == WindowState.Minimized) _w.WindowState = WindowState.Normal;
            _w.Activate();
        }

        // Für die Sichtprüfung über --dev-render.
        public void DevSelect(int i) { SelectSlot(i); }
        public void DevEnterFolder() { EnterFolder(Cur); }
        public void DevCloneWheel() { CloneWheel.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent)); }
        public void DevHome() { ShowEditor(); SelectWheel(Cfg.Wheels[0]); }
        public void DevScrollGeneral() { GeneralPanel.UpdateLayout(); GeneralPanel.ScrollToEnd(); }
        public void DevScrollInspector()
        {
            DependencyObject d = PText;
            while (d != null && !(d is ScrollViewer)) d = VisualTreeHelper.GetParent(d);
            var sv = d as ScrollViewer;
            if (sv != null) { sv.UpdateLayout(); sv.ScrollToEnd(); }
        }

        void Touch() { _host.ConfigChanged(); }

        void OnSaved() { Status.Text = "Gespeichert um " + DateTime.Now.ToString("HH:mm:ss") + " Uhr."; }

        void OnSaveFailed() { Status.Text = "Speichern gerade nicht möglich – Kyklos versucht es weiter."; }

        // ---------------------------------------------------------------- Aufbau

        static ComboBoxItem Item(string text, object tag) { return new ComboBoxItem { Content = text, Tag = tag }; }

        static void SelectTag(ComboBox box, object tag)
        {
            foreach (ComboBoxItem i in box.Items)
                if (Equals(i.Tag, tag)) { box.SelectedItem = i; return; }
            box.SelectedIndex = -1;
        }

        static object TagOf(ComboBox box)
        {
            var i = box.SelectedItem as ComboBoxItem;
            return i == null ? null : i.Tag;
        }

        void BuildChoices()
        {
            foreach (var t in new[] { ActionType.None, ActionType.Text, ActionType.Keys, ActionType.Open, ActionType.Url,
                                      ActionType.Media, ActionType.Macro, ActionType.Folder })
                TypeBox.Items.Add(Item(ActionDef.TypeName(t), t));

            TextModeBox.Items.Add(Item("Zwischenablage – schnell, auch für lange Texte", TextMode.Paste));
            TextModeBox.Items.Add(Item("Tippen – Zeichen für Zeichen", TextMode.Type));

            foreach (var m in ActionDef.MediaNames) MediaBox.Items.Add(Item(m[1], m[0]));

            foreach (var t in new[] { ActionType.Text, ActionType.Keys, ActionType.Open, ActionType.Url, ActionType.Media, ActionType.Delay, ActionType.Click })
                AddStepBox.Items.Add(Item(ActionDef.TypeName(t), t));

            foreach (var c in Palette.Colors)
            {
                var rb = new RadioButton
                {
                    Style = (Style)_w.FindResource("Swatch"), GroupName = "Tastenfarbe", Tag = c[0], ToolTip = c[1],
                    Background = new SolidColorBrush(Palette.Parse(c[0]))
                };
                rb.Checked += (s, e) =>
                {
                    if (_loading) return;
                    Cur.Color = (string)((RadioButton)s).Tag;
                    Preview.Refresh();
                    Touch();
                };
                Swatches.Children.Add(rb);
            }

            foreach (var section in Icons.Sections) AddIconSection(section.Key, section.Value);

            foreach (var sk in Skin.All)
            {
                var face = new StackPanel();
                face.Children.Add(new SkinChip(sk) { Width = 76, Height = 76, HorizontalAlignment = HorizontalAlignment.Center });
                face.Children.Add(new TextBlock { Text = sk.Name, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 7, 0, 0) });
                var rb = new RadioButton { Style = (Style)_w.FindResource("SkinTile"), GroupName = "Aussehen", Tag = sk.Id, Content = face };
                rb.Checked += (s, e) =>
                {
                    var chosen = Skin.Get((string)((RadioButton)s).Tag);
                    SkinHelp.Text = chosen.Note;
                    if (_loading) return;
                    Cfg.Settings.Skin = chosen.Id;
                    Preview.Skin = chosen;
                    UpdateTicker();
                    Touch();
                };
                SkinTiles.Children.Add(rb);
            }
        }

        /// <summary>
        /// Aussehen mit Bewegung (Auge, Lichter, Schnee) brauchen auch in der Vorschau laufende Bilder – nur solange das
        /// Fenster zu sehen ist.
        /// </summary>
        void UpdateTicker()
        {
            bool want = Preview.Skin.Animated && _w.IsVisible;
            if (want == _ticking) return;
            _ticking = want;
            _lastFrame = _clock.Elapsed.TotalSeconds;
            if (want) CompositionTarget.Rendering += OnFrame; else CompositionTarget.Rendering -= OnFrame;
        }

        void OnFrame(object sender, EventArgs e)
        {
            double now = _clock.Elapsed.TotalSeconds, dt = Math.Min(0.05, now - _lastFrame);
            _lastFrame = now;
            if (_w.WindowState == WindowState.Minimized || !Preview.IsVisible) return;
            if (Preview.Tick(dt)) Preview.InvalidateVisual();
        }

        void AddIconSection(string title, string[][] icons)
        {
            IconHost.Children.Add(new TextBlock
            {
                Text = title, Style = (Style)_w.FindResource("GroupLabel"),
                Margin = new Thickness(4, IconHost.Children.Count == 0 ? 2 : 14, 0, 4)
            });
            var wrap = new WrapPanel();
            foreach (var e in icons)
            {
                string id = e[0];
                var view = new IconView { Width = 22, Height = 22 };
                view.Set(id, null, Res("Ink"));
                var b = new Button { Style = (Style)_w.FindResource("IconCell"), Content = view, ToolTip = e[1] };
                b.Click += (s, a) =>
                {
                    Cur.Icon = id;
                    Cur.Image = "";
                    IconPopup.IsOpen = false;
                    SlotChanged();
                };
                wrap.Children.Add(b);
            }
            IconHost.Children.Add(wrap);
        }

        void Wire()
        {
            WheelList.SelectionChanged += (s, e) =>
            {
                if (_loading || WheelList.SelectedIndex < 0) return;
                ShowEditor();
                SelectWheel(Cfg.Wheels[WheelList.SelectedIndex]);
            };
            AddWheel.Click += (s, e) =>
            {
                var w = new Wheel { Name = "Neues Rad" };
                for (int i = 0; i < 6; i++) w.Slots.Add(new Slot());
                Cfg.Wheels.Add(w);
                RebuildWheelList();
                ShowEditor();
                SelectWheel(w);
                Touch();
                WheelName.Focus();
                WheelName.SelectAll();
            };
            NavGeneral.Click += (s, e) => ShowGeneral();

            WheelName.TextChanged += (s, e) =>
            {
                if (_loading) return;
                _wheel.Name = WheelName.Text;
                RebuildWheelList();
                if (_trail.Count == 0) LoadPreview();
                BuildCrumbs();
                Touch();
            };
            TriggerBtn.Click += (s, e) => Record(TriggerBtn, true, c => SetTrigger(c, false));
            AddTrigger2.Click += (s, e) => Record(AddTrigger2, true, c => SetTrigger(c, true));
            Trigger2Btn.Click += (s, e) => Record(Trigger2Btn, true, c => SetTrigger(c, true));
            Trigger2Clear.Click += (s, e) =>
            {
                CancelRecord();
                SetTrigger(new Chord(), true);
            };
            AddSlot.Click += (s, e) =>
            {
                if (CurrentSlots.Count >= Wheel.MaxSlots) return;
                CurrentSlots.Add(new Slot());
                _sel = CurrentSlots.Count - 1;
                LoadStage();
                LoadInspector();
                Touch();
            };
            CloneWheel.Click += (s, e) =>
            {
                CancelRecord();
                var copy = Wheel.FromJson(_wheel.ToJson());
                copy.Id = new Wheel().Id;
                string name = (_wheel.Name ?? "").Trim();
                copy.Name = (name.Length > 32 ? name.Substring(0, 32).TrimEnd() : name) + " (Kopie)";
                // Dieselbe Taste würde zwei Räder öffnen wollen – die Kopie bekommt ihre eigene.
                copy.Trigger = new Chord();
                copy.Trigger2 = new Chord();
                Cfg.Wheels.Insert(Cfg.Wheels.IndexOf(_wheel) + 1, copy);
                RebuildWheelList();
                ShowEditor();
                SelectWheel(copy);
                Touch();
                WheelName.Focus();
                WheelName.SelectAll();
            };
            DeleteWheel.Click += (s, e) => Confirm(DeleteWheel, "Wirklich löschen?", () =>
            {
                int i = Cfg.Wheels.IndexOf(_wheel);
                Cfg.Wheels.Remove(_wheel);
                RebuildWheelList();
                SelectWheel(Cfg.Wheels[Math.Max(0, Math.Min(i, Cfg.Wheels.Count - 1))]);
                Touch();
            });

            Preview.MouseMove += (s, e) =>
            {
                int i = Preview.HitTest(e.GetPosition(Preview));
                Preview.Cursor = i >= 0 ? Cursors.Hand : null;
                if (Preview.Hover == Math.Max(-1, i)) return;
                Preview.Hover = Math.Max(-1, i);
                Preview.InvalidateVisual();
            };
            Preview.MouseLeave += (s, e) => { Preview.Hover = -1; Preview.InvalidateVisual(); };
            // Das Auge im Halloween-Aussehen folgt dem Zeiger überall im Fenster.
            _w.PreviewMouseMove += (s, e) =>
            {
                var p = e.GetPosition(Preview);
                Preview.GazeTarget = new Vector(p.X - Preview.ActualWidth / 2, p.Y - Preview.ActualHeight / 2);
            };
            _w.MouseLeave += (s, e) => Preview.GazeTarget = new Vector(0, 0);
            Preview.MouseLeftButtonDown += (s, e) =>
            {
                int i = Preview.HitTest(e.GetPosition(Preview));
                if (i < 0) return;
                SelectSlot(i);
                if (e.ClickCount == 2 && Cur.Action.Type == ActionType.Folder) { EnterFolder(Cur); return; }
                _dragFrom = i;
                Preview.CaptureMouse();
            };
            Preview.MouseLeftButtonUp += (s, e) =>
            {
                Preview.ReleaseMouseCapture();
                int from = _dragFrom, to = Preview.HitTest(e.GetPosition(Preview));
                _dragFrom = -1;
                if (from < 0 || to < 0 || to == from) return;
                var slots = CurrentSlots;
                var tmp = slots[from]; slots[from] = slots[to]; slots[to] = tmp;
                _sel = to;
                LoadStage();
                LoadInspector();
                Touch();
            };

            MoveCcw.Click += (s, e) => MoveSlot(-1);
            MoveCw.Click += (s, e) => MoveSlot(1);

            LabelBox.TextChanged += (s, e) => { if (_loading) return; Cur.Label = LabelBox.Text; SlotChanged(false); };
            IconBtn.Click += (s, e) => { IconPopup.PlacementTarget = IconBtn; IconPopup.IsOpen = true; };
            ImageBtn.Click += (s, e) => PickImage();
            IconClear.Click += (s, e) => { Cur.Icon = ""; Cur.Image = ""; SlotChanged(); };

            TypeBox.SelectionChanged += (s, e) =>
            {
                if (_loading || TagOf(TypeBox) == null) return;
                var a = Cur.Action;
                a.Type = (ActionType)TagOf(TypeBox);
                if (a.Type == ActionType.Folder && a.Slots.Count < Wheel.MinSlots)
                    while (a.Slots.Count < 6) a.Slots.Add(new Slot());
                SlotChanged();
            };
            TextBody.TextChanged += (s, e) =>
            {
                if (_loading) return;
                Cur.Action.Text = TextBody.Text;
                Cur.Action.Sample = false;      // geändert heißt: jetzt ist es sein Text
                SampleNote.Visibility = Visibility.Collapsed;
                TextBody.Height = 196;
                SlotChanged(false);
            };
            SampleKeep.Click += (s, e) => { Cur.Action.Sample = false; SlotChanged(); };
            GapFieldBtn.Click += (s, e) => InsertGap("{?", "}", "Bezeichnung");
            GapChoiceBtn.Click += (s, e) => InsertGap("{?", ": Option 1 | Option 2 | Option 3}", "Bezeichnung");
            GapPickBtn.Click += (s, e) => InsertGap("{?", ": ja / nein / unbekannt}", "Bezeichnung");
            VariantBtn.Click += (s, e) => AddVariant();
            AlternBtn.Click += (s, e) => InsertAlternation();
            TextModeBox.SelectionChanged += (s, e) =>
            {
                if (_loading || TagOf(TextModeBox) == null) return;
                Cur.Action.Mode = (TextMode)TagOf(TextModeBox);
                Touch();
            };
            KeysBtn.Click += (s, e) => Record(KeysBtn, false, c => { Cur.Action.Keys = c; SlotChanged(); });
            PathBox.TextChanged += (s, e) => { if (_loading) return; Cur.Action.Path = PathBox.Text; SlotChanged(false); };
            ArgsBox.TextChanged += (s, e) => { if (_loading) return; Cur.Action.Args = ArgsBox.Text; Touch(); };
            BrowseBtn.Click += (s, e) => PickTarget();
            UrlBox.TextChanged += (s, e) => { if (_loading) return; Cur.Action.Url = UrlBox.Text; SlotChanged(false); };
            MediaBox.SelectionChanged += (s, e) =>
            {
                if (_loading || TagOf(MediaBox) == null) return;
                Cur.Action.Media = (string)TagOf(MediaBox);
                SlotChanged(false);
            };
            AddStepBox.SelectionChanged += (s, e) =>
            {
                if (_loading || TagOf(AddStepBox) == null) return;
                Cur.Action.Steps.Add(new ActionDef { Type = (ActionType)TagOf(AddStepBox) });
                _loading = true; AddStepBox.SelectedIndex = -1; _loading = false;
                BuildSteps();
                SlotChanged(false);
            };
            StepDelayBox.TextChanged += (s, e) =>
            {
                int v;
                if (_loading || !int.TryParse(StepDelayBox.Text, out v)) return;
                Cur.Action.StepDelayMs = Math.Max(0, Math.Min(5000, v));
                Touch();
            };
            OpenFolderBtn.Click += (s, e) => EnterFolder(Cur);

            TestBtn.Click += async (s, e) =>
            {
                var slot = Cur;
                var action = slot.Action;
                TestBtn.IsEnabled = false;
                for (int n = 3; n >= 1; n--)
                {
                    TestBtn.Content = "Startet in " + n + " …";
                    await Task.Delay(1000);
                }
                TestBtn.Content = "Aktion testen";
                TestBtn.IsEnabled = true;
                _host.Run(action, slot);
            };
            RemoveSlot.Click += (s, e) =>
            {
                if (CurrentSlots.Count <= Wheel.MinSlots) return;
                Action remove = () =>
                {
                    CurrentSlots.RemoveAt(_sel);
                    _sel = Math.Min(_sel, CurrentSlots.Count - 1);
                    LoadStage();
                    LoadInspector();
                    Touch();
                };
                if (Cur.IsEmpty) remove(); else Confirm(RemoveSlot, "Wirklich entfernen?", remove);
            };

            // Allgemein
            RoutedEventHandler toggles = (s, e) =>
            {
                if (_loading) return;
                var st = Cfg.Settings;
                st.TapSticky = TapSwitch.IsChecked == true;
                st.RestoreCursor = CursorSwitch.IsChecked == true;
                st.RestoreClipboard = ClipSwitch.IsChecked == true;
                DelayBox.IsEnabled = st.RestoreClipboard;
                Touch();
            };
            foreach (var cb in new[] { TapSwitch, CursorSwitch, ClipSwitch }) { cb.Checked += toggles; cb.Unchecked += toggles; }
            ScaleSlider.ValueChanged += (s, e) =>
            {
                ScaleText.Text = ((int)ScaleSlider.Value) + " %";
                if (_loading) return;
                Cfg.Settings.Scale = ScaleSlider.Value / 100.0;
                Touch();
            };
            DelayBox.TextChanged += (s, e) =>
            {
                int v;
                if (_loading || !int.TryParse(DelayBox.Text, out v)) return;
                Cfg.Settings.PasteDelayMs = Math.Max(50, Math.Min(3000, v));
                Touch();
            };
            RoutedEventHandler autostart = (s, e) =>
            {
                if (_loading) return;
                try { SetAutostart(AutostartSwitch.IsChecked == true); }
                catch (Exception ex)
                {
                    MessageBox.Show(_w, "Der Autostart-Eintrag konnte nicht geändert werden:\n\n" + ex.Message, "Kyklos",
                                    MessageBoxButton.OK, MessageBoxImage.Warning);
                    _loading = true; AutostartSwitch.IsChecked = AutostartOn(); _loading = false;
                }
            };
            AutostartSwitch.Checked += autostart;
            AutostartSwitch.Unchecked += autostart;
            var asklaion = F<System.Windows.Documents.Hyperlink>("AsklaionLink");
            asklaion.RequestNavigate += (s, e) =>
            {
                try { Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true }); }
                catch (Exception) { }   // kein Standardbrowser: dann eben nicht
                e.Handled = true;
            };
            OpenConfigDir.Click += (s, e) => Process.Start("explorer.exe", "/select,\"" + _host.ConfigPath + "\"");
            ExportBtn.Click += (s, e) => Export();
            ImportBtn.Click += (s, e) => Import();
            QuitBtn.Click += (s, e) => _host.Quit();
        }

        // ---------------------------------------------------------------- Räder

        void RebuildWheelList()
        {
            bool was = _loading;
            _loading = true;
            int keep = _wheel == null ? -1 : Cfg.Wheels.IndexOf(_wheel);
            WheelList.Items.Clear();
            foreach (var w in Cfg.Wheels)
            {
                var sp = new StackPanel();
                sp.Children.Add(new TextBlock
                {
                    Text = string.IsNullOrWhiteSpace(w.Name) ? "Ohne Namen" : w.Name, FontWeight = FontWeights.SemiBold,
                    TextTrimming = TextTrimming.CharacterEllipsis
                });
                sp.Children.Add(new TextBlock
                {
                    Text = w.HasTrigger ? w.TriggerDisplay() + " halten" : "Kein Auslöser", FontSize = 12, Foreground = Res("Ink2"),
                    Margin = new Thickness(0, 2, 0, 0), TextTrimming = TextTrimming.CharacterEllipsis
                });
                WheelList.Items.Add(new ListBoxItem { Content = sp });
            }
            if (EditorPanel.Visibility == Visibility.Visible) WheelList.SelectedIndex = keep;
            _loading = was;
        }

        void SelectWheel(Wheel w)
        {
            _wheel = w;
            _trail.Clear();
            _sel = 0;
            bool was = _loading;
            _loading = true;
            WheelList.SelectedIndex = Cfg.Wheels.IndexOf(w);
            _loading = was;
            LoadStage();
            LoadInspector();
        }

        void ShowEditor()
        {
            NavGeneral.IsChecked = false;
            GeneralPanel.Visibility = Visibility.Collapsed;
            EditorPanel.Visibility = Visibility.Visible;
        }

        public void ShowGeneral()
        {
            CancelRecord();
            _loading = true;
            WheelList.SelectedIndex = -1;
            _loading = false;
            NavGeneral.IsChecked = true;
            EditorPanel.Visibility = Visibility.Collapsed;
            GeneralPanel.Visibility = Visibility.Visible;
        }

        void LoadPreview()
        {
            var slots = CurrentSlots;
            double size = 2 * (WheelView.OuterFor(slots.Count) + WheelView.PreviewPad);
            Preview.Width = size;
            Preview.Height = size;
            Preview.Selected = _sel;
            string title = _trail.Count == 0 ? _wheel.Name : _trail[_trail.Count - 1].DisplayLabel;
            Preview.SetContent(slots, title, "Segment anklicken");
        }

        void LoadStage()
        {
            bool was = _loading;
            _loading = true;
            if (WheelName.Text != _wheel.Name) WheelName.Text = _wheel.Name;
            TriggerBtn.Content = _wheel.Trigger.IsEmpty ? "Taste festlegen" : _wheel.Trigger.Display();
            bool second = !_wheel.Trigger2.IsEmpty;
            Trigger2Btn.Content = _wheel.Trigger2.Display();
            Trigger2Group.Visibility = second ? Visibility.Visible : Visibility.Collapsed;
            // Erst wenn der erste Auslöser steht, gibt es einen zweiten.
            AddTrigger2.Visibility = !second && !_wheel.Trigger.IsEmpty ? Visibility.Visible : Visibility.Collapsed;
            // Ohne Folge-Element hält die Gruppe selbst den Abstand zur Segmentzahl.
            TriggerGroup.Margin = new Thickness(0, 0, _wheel.Trigger.IsEmpty ? 28 : 0, 8);
            int n = CurrentSlots.Count;
            CountText.Text = n + " Segmente";
            AddSlot.IsEnabled = n < Wheel.MaxSlots;
            DeleteWheel.IsEnabled = Cfg.Wheels.Count > 1;
            DeleteWheel.Content = "Rad löschen";
            DeleteWheel.Tag = null;

            TriggerNote.Text = TriggerProblem();
            TriggerNote.Visibility = TriggerNote.Text.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
            UsageHint.Text = !_wheel.HasTrigger
                ? "Lege oben einen Auslöser fest, um dieses Rad zu öffnen."
                : "So öffnest du das Rad: " + _wheel.TriggerDisplay() + " halten, Maus in eine Richtung bewegen, loslassen.";

            LoadPreview();
            BuildCrumbs();
            _loading = was;
        }

        void SetTrigger(Chord c, bool second)
        {
            if (second) _wheel.Trigger2 = c;
            else _wheel.Trigger = c;
            RebuildWheelList();
            LoadStage();
            Touch();
        }

        /// <summary>Warnung unter den Auslösern: fehlt einer, sind beide gleich, oder nutzt ein anderes Rad denselben?</summary>
        string TriggerProblem()
        {
            if (_wheel.Trigger.IsEmpty) return "Ohne Auslöser lässt sich dieses Rad nicht öffnen.";
            if (_wheel.Trigger2.SameAs(_wheel.Trigger)) return "Beide Auslöser sind gleich. Lege als zweiten eine andere Taste fest.";
            bool two = !_wheel.Trigger2.IsEmpty;
            foreach (var t in _wheel.Triggers)
            {
                var clash = Cfg.Wheels.FirstOrDefault(o => o != _wheel && o.Triggers.Any(x => x.SameAs(t)));
                if (clash == null) continue;
                return (two ? t.Display() + " nutzt" : "Diesen Auslöser nutzt") + " auch das Rad „" + clash.Name +
                       "“. Es öffnet sich das Rad, das in der Liste weiter oben steht.";
            }
            return "";
        }

        void BuildCrumbs()
        {
            Crumbs.Children.Clear();
            if (_trail.Count == 0) return;
            for (int level = 0; level <= _trail.Count; level++)
            {
                string name = level == 0 ? _wheel.Name : _trail[level - 1].DisplayLabel;
                if (level > 0)
                    Crumbs.Children.Add(new TextBlock
                    {
                        Text = "", Style = (Style)_w.FindResource("Glyph"), FontSize = 10, Foreground = Res("Ink2"),
                        Margin = new Thickness(2, 1, 2, 0)
                    });
                if (level == _trail.Count)
                {
                    Crumbs.Children.Add(new TextBlock
                    {
                        Text = name, FontWeight = FontWeights.SemiBold, VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(10, 0, 0, 0)
                    });
                }
                else
                {
                    int target = level;
                    var b = new Button { Style = (Style)_w.FindResource("BtnGhost"), Content = name };
                    b.Click += (s, e) => LeaveTo(target);
                    Crumbs.Children.Add(b);
                }
            }
        }

        void EnterFolder(Slot folder)
        {
            if (folder.Action.Type != ActionType.Folder) return;
            Wheel.Normalize(folder.Action.Slots);
            _trail.Add(folder);
            _sel = 0;
            LoadStage();
            LoadInspector();
        }

        void LeaveTo(int level)
        {
            Slot cameFrom = _trail[level];
            _trail.RemoveRange(level, _trail.Count - level);
            _sel = Math.Max(0, CurrentSlots.IndexOf(cameFrom));
            LoadStage();
            LoadInspector();
        }

        // ---------------------------------------------------------------- Segment

        void SelectSlot(int i)
        {
            if (i == _sel) return;
            CancelRecord();
            _sel = i;
            Preview.Selected = i;
            Preview.InvalidateVisual();
            LoadInspector();
        }

        void MoveSlot(int dir)
        {
            var slots = CurrentSlots;
            int to = ((_sel + dir) % slots.Count + slots.Count) % slots.Count;
            var tmp = slots[_sel]; slots[_sel] = slots[to]; slots[to] = tmp;
            _sel = to;
            LoadStage();
            LoadInspector();
            Touch();
        }

        static string PosName(int i, int n)
        {
            string[] names = { "Oben", "Oben rechts", "Rechts", "Unten rechts", "Unten", "Unten links", "Links", "Oben links" };
            double k = i * 8.0 / n;
            return Math.Abs(k - Math.Round(k)) < 0.01 ? names[(int)Math.Round(k) % 8] : "Bei " + Math.Round(i * 360.0 / n) + "°";
        }

        /// <summary>Nach einer Änderung am gewählten Segment. full = auch den Inspektor neu aufbauen.</summary>
        void SlotChanged(bool full = true)
        {
            SlotTitle.Text = Cur.IsEmpty ? "Freies Segment" : Cur.DisplayLabel;
            Preview.Refresh();
            if (full) LoadInspector();
            else TestBtn.IsEnabled = Cur.Action.Type != ActionType.None && Cur.Action.Type != ActionType.Folder;
            Touch();
        }

        void LoadInspector()
        {
            bool was = _loading;
            _loading = true;
            var slot = Cur;
            var a = slot.Action;
            int n = CurrentSlots.Count;

            SlotTitle.Text = slot.IsEmpty ? "Freies Segment" : slot.DisplayLabel;
            SlotPos.Text = PosName(_sel, n) + " · Segment " + (_sel + 1) + " von " + n;
            if (LabelBox.Text != slot.Label) LabelBox.Text = slot.Label;
            IconPreview.Set(slot.Icon, slot.Bitmap, Res("Ink"));
            IconClear.IsEnabled = slot.HasIcon;
            foreach (RadioButton rb in Swatches.Children)
                rb.IsChecked = string.Equals((string)rb.Tag, slot.Color, StringComparison.OrdinalIgnoreCase);

            SelectTag(TypeBox, a.Type);
            if (TextBody.Text != a.Text) TextBody.Text = a.Text;
            SelectTag(TextModeBox, a.Mode);
            KeysBtn.Content = a.Keys.IsEmpty ? "Kombination festlegen" : a.Keys.Display();
            if (PathBox.Text != a.Path) PathBox.Text = a.Path;
            if (ArgsBox.Text != a.Args) ArgsBox.Text = a.Args;
            if (UrlBox.Text != a.Url) UrlBox.Text = a.Url;
            SelectTag(MediaBox, a.Media);
            StepDelayBox.Text = a.StepDelayMs.ToString();
            if (a.Type == ActionType.Macro) BuildSteps();

            PText.Visibility = Vis(a.Type == ActionType.Text);
            SampleNote.Visibility = Vis(a.Type == ActionType.Text && a.Sample);
            // Mit dem Hinweis darunter wird das Textfeld niedriger, damit „Einfügen per" sichtbar bleibt.
            TextBody.Height = a.Sample ? 124 : 196;
            PKeys.Visibility = Vis(a.Type == ActionType.Keys);
            POpen.Visibility = Vis(a.Type == ActionType.Open);
            PUrl.Visibility = Vis(a.Type == ActionType.Url);
            PMedia.Visibility = Vis(a.Type == ActionType.Media);
            PMacro.Visibility = Vis(a.Type == ActionType.Macro);
            PFolder.Visibility = Vis(a.Type == ActionType.Folder);

            TestBtn.IsEnabled = a.Type != ActionType.None && a.Type != ActionType.Folder;
            RemoveSlot.IsEnabled = n > Wheel.MinSlots;
            RemoveSlot.Content = "Segment entfernen";
            RemoveSlot.Tag = null;
            _loading = was;
        }

        static Visibility Vis(bool on) { return on ? Visibility.Visible : Visibility.Collapsed; }

        // ---------------------------------------------------------------- Makro-Schritte

        void BuildSteps()
        {
            StepsHost.Children.Clear();
            var steps = Cur.Action.Steps;
            if (steps.Count == 0)
            {
                StepsHost.Children.Add(new TextBlock
                {
                    Style = (Style)_w.FindResource("Help"), Margin = new Thickness(0, 8, 0, 0),
                    Text = "Noch keine Schritte. Ein Makro führt mehrere Aktionen nacheinander aus – zum Beispiel einen Text einfügen und danach Tab drücken."
                });
                return;
            }
            for (int i = 0; i < steps.Count; i++)
            {
                int index = i;
                var step = steps[i];
                var head = new DockPanel();
                var tools = new StackPanel { Orientation = Orientation.Horizontal };
                DockPanel.SetDock(tools, Dock.Right);
                tools.Children.Add(StepTool("", "Nach oben", index > 0, () => SwapSteps(index, index - 1)));
                tools.Children.Add(StepTool("", "Nach unten", index < steps.Count - 1, () => SwapSteps(index, index + 1)));
                tools.Children.Add(StepTool("", "Schritt entfernen", true, () =>
                {
                    Cur.Action.Steps.RemoveAt(index);
                    BuildSteps();
                    SlotChanged(false);
                }));
                head.Children.Add(tools);
                head.Children.Add(new TextBlock
                {
                    Text = (i + 1) + ". " + ActionDef.TypeName(step.Type), FontWeight = FontWeights.SemiBold,
                    VerticalAlignment = VerticalAlignment.Center
                });

                var body = new StackPanel();
                body.Children.Add(head);
                var editor = StepEditor(step);
                editor.Margin = new Thickness(0, 6, 0, 0);
                body.Children.Add(editor);
                StepsHost.Children.Add(new Border
                {
                    BorderBrush = Res("Line"), BorderThickness = new Thickness(0, 0, 0, 1), Padding = new Thickness(0, 8, 0, 14), Child = body
                });
            }
        }

        Button StepTool(string glyph, string tip, bool enabled, Action act)
        {
            var b = new Button
            {
                Style = (Style)_w.FindResource("BtnIcon"), ToolTip = tip, IsEnabled = enabled,
                Content = new TextBlock { Text = glyph, Style = (Style)_w.FindResource("Glyph"), FontSize = 11 }
            };
            b.Click += (s, e) => act();
            return b;
        }

        void SwapSteps(int a, int b)
        {
            var steps = Cur.Action.Steps;
            var tmp = steps[a]; steps[a] = steps[b]; steps[b] = tmp;
            BuildSteps();
            Touch();
        }

        FrameworkElement StepEditor(ActionDef step)
        {
            switch (step.Type)
            {
                case ActionType.Keys:
                    var kb = new Button
                    {
                        Style = (Style)_w.FindResource("Btn"), HorizontalAlignment = HorizontalAlignment.Stretch,
                        Content = step.Keys.IsEmpty ? "Kombination festlegen" : step.Keys.Display()
                    };
                    kb.Click += (s, e) => Record(kb, false, c => { step.Keys = c; kb.Content = c.Display(); Touch(); });
                    return kb;
                case ActionType.Media:
                    var mb = new ComboBox();
                    foreach (var m in ActionDef.MediaNames) mb.Items.Add(Item(m[1], m[0]));
                    SelectTag(mb, step.Media);
                    mb.SelectionChanged += (s, e) => { if (TagOf(mb) != null) { step.Media = (string)TagOf(mb); Touch(); } };
                    return mb;
                case ActionType.Delay:
                    var dp = new DockPanel { LastChildFill = false };
                    var db = new TextBox { Width = 72, TextAlignment = TextAlignment.Right, MaxLength = 5, Text = step.DelayMs.ToString() };
                    db.TextChanged += (s, e) =>
                    {
                        int v;
                        if (int.TryParse(db.Text, out v)) { step.DelayMs = Math.Max(0, Math.Min(60000, v)); Touch(); }
                    };
                    dp.Children.Add(db);
                    dp.Children.Add(new TextBlock
                    {
                        Text = "ms warten", Foreground = Res("Ink2"), VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(8, 0, 0, 0)
                    });
                    return dp;
                case ActionType.Click:
                    return ClickEditor(step);
                case ActionType.Open:
                    var ob = new TextBox { Text = step.Path, Tag = "Programm, Datei oder Ordner" };
                    ob.TextChanged += (s, e) => { step.Path = ob.Text; Touch(); };
                    return ob;
                case ActionType.Url:
                    var ub = new TextBox { Text = step.Url, Tag = "https://…" };
                    ub.TextChanged += (s, e) => { step.Url = ub.Text; Touch(); };
                    return ub;
                default:
                    var tb = new TextBox
                    {
                        Style = (Style)_w.FindResource("TextArea"), Text = step.Text, MinHeight = 34, MaxHeight = 120,
                        Tag = "Text, der eingefügt wird"
                    };
                    tb.TextChanged += (s, e) => { step.Text = tb.Text; Touch(); };
                    return tb;
            }
        }

        FrameworkElement ClickEditor(ActionDef step)
        {
            var panel = new StackPanel();
            var kind = new ComboBox();
            foreach (var b in ActionDef.ButtonNames) kind.Items.Add(Item(b[1], b[0]));
            SelectTag(kind, step.Button);
            kind.SelectionChanged += (s, e) => { if (TagOf(kind) != null) { step.Button = (string)TagOf(kind); Touch(); } };
            panel.Children.Add(kind);

            var row = new DockPanel { LastChildFill = false, Margin = new Thickness(0, 8, 0, 0) };
            Func<string, TextBox> coord = name =>
            {
                row.Children.Add(new TextBlock
                {
                    Text = name, Foreground = Res("Ink2"), VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(row.Children.Count == 0 ? 0 : 12, 0, 6, 0)
                });
                var box = new TextBox { Width = 64, TextAlignment = TextAlignment.Right, MaxLength = 6 };
                row.Children.Add(box);
                return box;
            };
            var xb = coord("X");
            var yb = coord("Y");
            xb.Text = step.X.ToString();
            yb.Text = step.Y.ToString();
            xb.TextChanged += (s, e) => { int v; if (int.TryParse(xb.Text, out v)) { step.X = v; Touch(); } };
            yb.TextChanged += (s, e) => { int v; if (int.TryParse(yb.Text, out v)) { step.Y = v; Touch(); } };

            var pick = new Button { Style = (Style)_w.FindResource("Btn"), Content = "Position aufnehmen" };
            DockPanel.SetDock(pick, Dock.Right);
            pick.Click += async (s, e) =>
            {
                pick.IsEnabled = false;
                Native.POINT p;
                for (int ticks = 30; ticks > 0; ticks--)
                {
                    Native.GetCursorPos(out p);
                    pick.Content = (ticks + 9) / 10 + " s · " + p.x + ", " + p.y;
                    await Task.Delay(100);
                }
                Native.GetCursorPos(out p);
                xb.Text = p.x.ToString();
                yb.Text = p.y.ToString();
                pick.Content = "Position aufnehmen";
                pick.IsEnabled = true;
            };
            row.Children.Add(pick);
            panel.Children.Add(row);

            panel.Children.Add(new TextBlock
            {
                Style = (Style)_w.FindResource("Help"), Margin = new Thickness(0, 6, 0, 0),
                Text = "Nach „Position aufnehmen“ hast du 3 Sekunden, den Zeiger auf die Stelle zu bewegen. Der Klick trifft immer " +
                       "denselben Bildschirmpunkt – verschiebt sich das Fenster, geht er daneben. Danach steht der Zeiger wieder, wo er war."
            });
            return panel;
        }

        // ---------------------------------------------------------------- Tasten aufnehmen, Rückfragen

        /// <summary>Wartet auf die nächste Taste (über den globalen Hook, damit auch ^ oder Maus-Seitentasten ankommen).</summary>
        void Record(Button b, bool allowMouse, Action<Chord> done)
        {
            bool same = _recBtn == b;
            CancelRecord();
            if (same) return;      // zweiter Klick bricht ab
            _recBtn = b;
            _recOld = b.Content;
            b.Content = allowMouse ? "Taste oder Maus-Seitentaste drücken …" : "Kombination drücken …";
            InputHook.RecordAllowMouse = allowMouse;
            InputHook.RecordCallback = c =>
            {
                if (_recBtn != b) return;
                _recBtn = null;
                b.Content = _recOld;
                if (c != null) done(c);
            };
        }

        void CancelRecord()
        {
            if (_recBtn == null) return;
            InputHook.RecordCallback = null;
            _recBtn.Content = _recOld;
            _recBtn = null;
        }

        /// <summary>Zweistufige Rückfrage direkt auf der Schaltfläche statt eines Dialogs.</summary>
        void Confirm(Button b, string question, Action act)
        {
            if (b.Tag as string == "armed")
            {
                b.Tag = null;
                act();
                return;
            }
            object old = b.Content;
            b.Content = question;
            b.Tag = "armed";
            var t = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
            t.Tick += (s, e) =>
            {
                t.Stop();
                if (b.Tag as string != "armed") return;
                b.Tag = null;
                b.Content = old;
            };
            t.Start();
        }

        // ---------------------------------------------------------------- Bilder und Ziele

        void PickImage()
        {
            var dlg = new OpenFileDialog
            {
                Title = "Bild für das Segment wählen",
                Filter = "Bilder und Programme|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.ico;*.exe;*.lnk|Alle Dateien|*.*"
            };
            if (dlg.ShowDialog(_w) != true) return;
            try
            {
                Cur.Image = EncodeImage(dlg.FileName);
                Cur.Icon = "";
                SlotChanged();
            }
            catch (Exception ex)
            {
                MessageBox.Show(_w, "Das Bild konnte nicht gelesen werden:\n\n" + ex.Message, "Kyklos", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// Setzt eine Lücke an die Schreibmarke. Markierter Text wird zum Namen der Lücke, sonst ist der Platzhaltername
        /// markiert, damit man ihn gleich überschreiben kann.
        /// </summary>
        void InsertGap(string open, string close, string name)
        {
            string picked = TextBody.SelectedText.Trim();
            if (picked.Length > 0 && picked.IndexOfAny(new[] { '{', '}', ':', '|', '\n' }) < 0) name = picked;
            int at = TextBody.SelectionStart;
            TextBody.SelectedText = open + name + close;
            TextBody.Focus();
            TextBody.Select(at + open.Length, name.Length);
        }

        /// <summary>Hängt eine weitere ganze Fassung an; die Schreibmarke steht danach in der neuen Zeile.</summary>
        void AddVariant()
        {
            string t = TextBody.Text.TrimEnd();
            string nl = Environment.NewLine;    // wie die Eingabetaste im Textfeld
            TextBody.Text = (t.Length == 0 ? "" : t + nl) + "{oder}" + nl;
            TextBody.Focus();
            TextBody.Select(TextBody.Text.Length, 0);
            TextBody.ScrollToEnd();
        }

        /// <summary>
        /// Setzt eine Wechselformulierung an die Schreibmarke. Markierter Text wird zur ersten Formulierung und die Schreibmarke
        /// steht bei der zweiten; sonst sind beide Platzhalter da und der erste ist markiert.
        /// </summary>
        void InsertAlternation()
        {
            string picked = TextBody.SelectedText.Trim();
            int at = TextBody.SelectionStart;
            if (picked.Length > 0 && picked.IndexOfAny(new[] { '{', '}', '|', '\n' }) < 0)
            {
                TextBody.SelectedText = "{~" + picked + " | }";
                TextBody.Focus();
                TextBody.Select(at + picked.Length + 5, 0);
                return;
            }
            TextBody.SelectedText = "{~Formulierung 1 | Formulierung 2}";
            TextBody.Focus();
            TextBody.Select(at + 2, "Formulierung 1".Length);
        }

        void PickTarget()
        {
            var dlg = new OpenFileDialog { Title = "Programm oder Datei wählen", Filter = "Alle Dateien|*.*", DereferenceLinks = false };
            if (dlg.ShowDialog(_w) != true) return;
            var slot = Cur;
            slot.Action.Path = dlg.FileName;
            if (string.IsNullOrWhiteSpace(slot.Label)) slot.Label = Path.GetFileNameWithoutExtension(dlg.FileName);
            if (!slot.HasIcon)
            {
                try { slot.Image = EncodeImage(dlg.FileName); }
                catch (Exception) { }   // kein Symbol ermittelbar: Segment bleibt ohne Bild
            }
            SlotChanged();
        }

        /// <summary>Liest ein Bild (oder das Symbol eines Programms), verkleinert es auf höchstens 96 px und liefert Base64-PNG.</summary>
        static string EncodeImage(string path)
        {
            BitmapSource src;
            string ext = Path.GetExtension(path).ToLowerInvariant();
            bool image = ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".bmp" || ext == ".gif" || ext == ".ico";
            if (image)
            {
                var dec = BitmapDecoder.Create(new Uri(path), BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
                src = dec.Frames.OrderByDescending(f => f.PixelWidth).First();
            }
            else
            {
                using (var ico = System.Drawing.Icon.ExtractAssociatedIcon(path))
                {
                    src = Imaging.CreateBitmapSourceFromHIcon(ico.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                    src.Freeze();
                }
            }
            double k = Math.Min(1.0, 96.0 / Math.Max(src.PixelWidth, src.PixelHeight));
            int w = Math.Max(1, (int)Math.Round(src.PixelWidth * k)), h = Math.Max(1, (int)Math.Round(src.PixelHeight * k));
            var dv = new DrawingVisual();
            RenderOptions.SetBitmapScalingMode(dv, BitmapScalingMode.HighQuality);
            using (var dc = dv.RenderOpen()) dc.DrawImage(src, new Rect(0, 0, w, h));
            var rtb = new RenderTargetBitmap(w, h, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(dv);
            var enc = new PngBitmapEncoder();
            enc.Frames.Add(BitmapFrame.Create(rtb));
            using (var ms = new MemoryStream())
            {
                enc.Save(ms);
                return Convert.ToBase64String(ms.ToArray());
            }
        }

        // ---------------------------------------------------------------- Allgemein

        void LoadGeneral()
        {
            _loading = true;
            var st = Cfg.Settings;
            TapSwitch.IsChecked = st.TapSticky;
            CursorSwitch.IsChecked = st.RestoreCursor;
            ClipSwitch.IsChecked = st.RestoreClipboard;
            ScaleSlider.Value = Math.Round(st.Scale * 100 / 5) * 5;
            ScaleText.Text = ((int)ScaleSlider.Value) + " %";
            DelayBox.Text = st.PasteDelayMs.ToString();
            DelayBox.IsEnabled = st.RestoreClipboard;
            AutostartSwitch.IsChecked = AutostartOn();
            foreach (RadioButton rb in SkinTiles.Children) rb.IsChecked = (string)rb.Tag == st.Skin;
            SkinHelp.Text = Skin.Get(st.Skin).Note;
            Preview.Skin = Skin.Get(st.Skin);
            UpdateTicker();
            ConfigPathText.Text = _host.ConfigPath;
            VersionText.Text = "Kyklos " + Program.Version + " · MIT-Lizenz";
            _loading = false;
        }

        const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";

        static string ExePath { get { return Assembly.GetExecutingAssembly().Location; } }

        static bool AutostartOn()
        {
            try
            {
                using (var k = Registry.CurrentUser.OpenSubKey(RunKey))
                {
                    var v = k == null ? null : k.GetValue("Kyklos") as string;
                    return v != null && v.IndexOf(ExePath, StringComparison.OrdinalIgnoreCase) >= 0;
                }
            }
            catch (Exception) { return false; }
        }

        static void SetAutostart(bool on)
        {
            using (var k = Registry.CurrentUser.CreateSubKey(RunKey))
            {
                if (on) k.SetValue("Kyklos", "\"" + ExePath + "\"");
                else k.DeleteValue("Kyklos", false);
            }
        }

        void Export()
        {
            var dlg = new SaveFileDialog { Title = "Konfiguration exportieren", Filter = "Kyklos-Konfiguration|*.json", FileName = "Kyklos-Export.json" };
            if (dlg.ShowDialog(_w) != true) return;
            try { File.WriteAllText(dlg.FileName, Json.Write(Cfg.ToJson()), new UTF8Encoding(false)); }
            catch (Exception ex)
            {
                MessageBox.Show(_w, "Der Export ist fehlgeschlagen:\n\n" + ex.Message, "Kyklos", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        void Import()
        {
            var dlg = new OpenFileDialog { Title = "Konfiguration importieren", Filter = "Kyklos-Konfiguration|*.json|Alle Dateien|*.*" };
            if (dlg.ShowDialog(_w) != true) return;
            Config cfg;
            try
            {
                cfg = Config.FromJson(Json.Parse(File.ReadAllText(dlg.FileName, Encoding.UTF8)) as Dictionary<string, object>);
                if (cfg.Wheels.Count == 0) throw new FormatException("Die Datei enthält keine Räder.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(_w, "Die Datei ist keine gültige Kyklos-Konfiguration:\n\n" + ex.Message, "Kyklos",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            string q = "Alle " + Cfg.Wheels.Count + " vorhandenen Räder durch die " + cfg.Wheels.Count + " Räder aus der Datei ersetzen?";
            if (MessageBox.Show(_w, q, "Konfiguration importieren", MessageBoxButton.OKCancel, MessageBoxImage.Question) != MessageBoxResult.OK) return;
            _host.ReplaceConfig(cfg);
            _wheel = null;
            RebuildWheelList();
            LoadGeneral();
            ShowEditor();
            SelectWheel(Cfg.Wheels[0]);
        }
    }
}
