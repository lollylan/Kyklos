using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
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
        readonly TextBlock Status, CountText, TriggerNote, ScopeNote, SlotTitle, SlotPos, ScaleText, ConfigPathText, VersionText, UsageHint;
        readonly Border SampleNote;
        readonly Button SampleKeep;
        readonly Grid EditorPanel;
        readonly ScrollViewer GeneralPanel;
        readonly Button AddWheel, ImportWheel, OnlineWheel, CloneWheel, ExportWheel, DeleteWheel, TriggerBtn, AddTrigger2, Trigger2Btn, Trigger2Clear, AddSlot, TestBtn, RemoveSlot, MoveCcw, MoveCw, IconBtn, ImageBtn, IconClear,
                        KeysBtn, BrowseBtn, OpenFolderBtn, OpenConfigDir, ExportBtn, ImportBtn, QuitBtn, GapFieldBtn, GapChoiceBtn, GapPickBtn,
                        VariantBtn, AlternBtn, FolderFromFile, FolderFromWeb;
        readonly TextBox WheelName, LabelBox, TextBody, PathBox, ArgsBox, UrlBox, StepDelayBox, DelayBox;
        readonly ComboBox TypeBox, TextModeBox, MediaBox, AddStepBox;
        readonly StackPanel TriggerGroup, Trigger2Group, ScopeHost, Crumbs, Swatches, PText, PKeys, POpen, PUrl, PMedia, PMacro, PFolder, StepsHost, IconHost;
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
        readonly StackPanel PAi, ProviderHost;
        readonly TextBox AiPrompt;
        readonly ComboBox AiTemplateBox, AiSourceBox, AiOutputBox, AiProviderBox, AddProviderBox;
        readonly CheckBox AiLocalSwitch;
        readonly Border AiNote;
        readonly TextBlock AiNoteTitle, AiNoteText, AiOutputHelp, AiSection;
        readonly Button AiSetupBtn;

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
            ScopeNote = F<TextBlock>("ScopeNote"); ScopeHost = F<StackPanel>("ScopeHost");
            SlotTitle = F<TextBlock>("SlotTitle"); SlotPos = F<TextBlock>("SlotPos"); ScaleText = F<TextBlock>("ScaleText");
            ConfigPathText = F<TextBlock>("ConfigPathText"); VersionText = F<TextBlock>("VersionText");
            EditorPanel = F<Grid>("EditorPanel"); GeneralPanel = F<ScrollViewer>("GeneralPanel");
            AddWheel = F<Button>("AddWheel"); ImportWheel = F<Button>("ImportWheel"); OnlineWheel = F<Button>("OnlineWheel"); CloneWheel = F<Button>("CloneWheel");
            ExportWheel = F<Button>("ExportWheel"); DeleteWheel = F<Button>("DeleteWheel"); TriggerBtn = F<Button>("TriggerBtn");
            AddTrigger2 = F<Button>("AddTrigger2"); Trigger2Btn = F<Button>("Trigger2Btn"); Trigger2Clear = F<Button>("Trigger2Clear");
            TriggerGroup = F<StackPanel>("TriggerGroup"); Trigger2Group = F<StackPanel>("Trigger2Group");
            AddSlot = F<Button>("AddSlot"); TestBtn = F<Button>("TestBtn"); RemoveSlot = F<Button>("RemoveSlot");
            MoveCcw = F<Button>("MoveCcw"); MoveCw = F<Button>("MoveCw"); IconBtn = F<Button>("IconBtn"); ImageBtn = F<Button>("ImageBtn");
            IconClear = F<Button>("IconClear"); KeysBtn = F<Button>("KeysBtn"); BrowseBtn = F<Button>("BrowseBtn");
            OpenFolderBtn = F<Button>("OpenFolderBtn"); OpenConfigDir = F<Button>("OpenConfigDir"); ExportBtn = F<Button>("ExportBtn");
            ImportBtn = F<Button>("ImportBtn"); QuitBtn = F<Button>("QuitBtn");
            GapFieldBtn = F<Button>("GapFieldBtn"); GapChoiceBtn = F<Button>("GapChoiceBtn"); GapPickBtn = F<Button>("GapPickBtn");
            VariantBtn = F<Button>("VariantBtn"); AlternBtn = F<Button>("AlternBtn");
            FolderFromFile = F<Button>("FolderFromFile"); FolderFromWeb = F<Button>("FolderFromWeb");
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
            PAi = F<StackPanel>("PAi"); ProviderHost = F<StackPanel>("ProviderHost"); AiPrompt = F<TextBox>("AiPrompt");
            AiTemplateBox = F<ComboBox>("AiTemplateBox"); AiSourceBox = F<ComboBox>("AiSourceBox"); AiOutputBox = F<ComboBox>("AiOutputBox");
            AiProviderBox = F<ComboBox>("AiProviderBox"); AddProviderBox = F<ComboBox>("AddProviderBox");
            AiLocalSwitch = F<CheckBox>("AiLocalSwitch"); AiNote = F<Border>("AiNote");
            AiNoteTitle = F<TextBlock>("AiNoteTitle"); AiNoteText = F<TextBlock>("AiNoteText"); AiOutputHelp = F<TextBlock>("AiOutputHelp");
            AiSection = F<TextBlock>("AiSection"); AiSetupBtn = F<Button>("AiSetupBtn");

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
        public void DevSelectWheel(int i) { ShowEditor(); RebuildWheelList(); SelectWheel(Cfg.Wheels[i]); }
        public void DevShowAiProviders() { LoadGeneral(); ShowAiProviders(); }
        public void DevScrollGeneral() { GeneralPanel.UpdateLayout(); GeneralPanel.ScrollToEnd(); }
        public void DevScrollInspector(bool end = true)
        {
            DependencyObject d = PText;
            while (d != null && !(d is ScrollViewer)) d = VisualTreeHelper.GetParent(d);
            var sv = d as ScrollViewer;
            if (sv == null) return;
            sv.UpdateLayout();
            if (end) sv.ScrollToEnd(); else sv.ScrollToHome();
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
            foreach (var t in new[] { ActionType.None, ActionType.Text, ActionType.Ai, ActionType.Keys, ActionType.Open, ActionType.Url,
                                      ActionType.Media, ActionType.Macro, ActionType.Folder })
                TypeBox.Items.Add(Item(ActionDef.TypeName(t), t));

            TextModeBox.Items.Add(Item("Zwischenablage – schnell, auch für lange Texte", TextMode.Paste));
            TextModeBox.Items.Add(Item("Tippen – Zeichen für Zeichen", TextMode.Type));

            foreach (var m in ActionDef.MediaNames) MediaBox.Items.Add(Item(m[1], m[0]));

            foreach (var t in new[] { ActionType.Text, ActionType.Ai, ActionType.Keys, ActionType.Open, ActionType.Url, ActionType.Media, ActionType.Delay, ActionType.Click,
                                      ActionType.Focus })
                AddStepBox.Items.Add(Item(ActionDef.TypeName(t), t));

            foreach (var t in AiTemplates.All) AiTemplateBox.Items.Add(Item(t[0], t));
            FillAiChoices(AiSourceBox, AiOutputBox);
            foreach (var k in AiKinds.All)
                AddProviderBox.Items.Add(Item(k == AiKinds.Ollama ? "Ollama – lokal oder im Praxisnetz" : AiKinds.Name(k) + " – extern, über das Internet", k));

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
            ExportWheel.Click += (s, e) => { CancelRecord(); ExportOne(); };
            ImportWheel.Click += (s, e) => { CancelRecord(); ImportAdd(); };
            OnlineWheel.Click += (s, e) => { CancelRecord(); Gallery.Show(_w, Skin.Get(Cfg.Settings.Skin), AddWheels); };
            var head = F<Grid>("WheelHead");
            var actions = F<StackPanel>("WheelActions");
            head.SizeChanged += (s, e) =>
            {
                // Unter 260 px Platz für den Namen rücken die Knöpfe in eine eigene Zeile, bündig mit dem Namen.
                actions.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                bool narrow = e.NewSize.Width - actions.DesiredSize.Width < 260;
                Grid.SetRow(actions, narrow ? 1 : 0);
                Grid.SetColumn(actions, narrow ? 0 : 1);
                actions.HorizontalAlignment = narrow ? HorizontalAlignment.Left : HorizontalAlignment.Stretch;
                actions.Margin = narrow ? new Thickness(-10, 4, 0, 0) : new Thickness(12, 0, 0, 0);
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
                if (a.Type == ActionType.Ai && !Cur.HasIcon) Cur.Icon = "v:ki";
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
                var step = new ActionDef { Type = (ActionType)TagOf(AddStepBox) };
                // Nach „Text erkennen“ liegt der Text in der Zwischenablage, nicht in einer Markierung.
                if (step.Type == ActionType.Ai && Cur.Action.Steps.Any(x => x.Type == ActionType.Media && x.Media == "ocr"))
                    step.AiSource = AiSources.Clipboard;
                Cur.Action.Steps.Add(step);
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
            FolderFromFile.Click += (s, e) => { CancelRecord(); FillFolderFromFile(Cur); };
            FolderFromWeb.Click += (s, e) =>
            {
                CancelRecord();
                var target = Cur;
                Gallery.Show(_w, Skin.Get(Cfg.Settings.Skin), w => FillFolder(target, w), "Einsetzen", "Eingesetzt", true);
            };

            AiPrompt.TextChanged += (s, e) => { if (_loading) return; Cur.Action.Text = AiPrompt.Text; SlotChanged(false); };
            AiTemplateBox.SelectionChanged += (s, e) =>
            {
                var t = TagOf(AiTemplateBox) as string[];
                if (_loading || t == null) return;
                _loading = true; AiTemplateBox.SelectedIndex = -1; _loading = false;
                var a = Cur.Action;
                string now = (a.Text ?? "").Trim();
                if (now.Length > 0 && now != t[1] &&
                    MessageBox.Show(_w, "Die bisherige Anweisung durch die Vorlage „" + t[0] + "“ ersetzen?", "Vorlage einsetzen",
                                    MessageBoxButton.OKCancel, MessageBoxImage.Question) != MessageBoxResult.OK) return;
                a.Text = t[1];
                if (t[0].StartsWith("Arztbrief", StringComparison.Ordinal)) a.LocalOnly = true;
                if (string.IsNullOrWhiteSpace(Cur.Label)) Cur.Label = t[0];
                SlotChanged();
            };
            AiSourceBox.SelectionChanged += (s, e) =>
            {
                if (_loading || TagOf(AiSourceBox) == null) return;
                Cur.Action.AiSource = (string)TagOf(AiSourceBox);
                Touch();
            };
            AiOutputBox.SelectionChanged += (s, e) =>
            {
                if (_loading || TagOf(AiOutputBox) == null) return;
                Cur.Action.AiOutput = (string)TagOf(AiOutputBox);
                AiOutputHelp.Text = OutputHelp(Cur.Action.AiOutput);
                Touch();
            };
            AiProviderBox.SelectionChanged += (s, e) =>
            {
                if (_loading || TagOf(AiProviderBox) == null) return;
                Cur.Action.Provider = (string)TagOf(AiProviderBox);
                UpdateAiNote();
                Touch();
            };
            RoutedEventHandler local = (s, e) =>
            {
                if (_loading) return;
                Cur.Action.LocalOnly = AiLocalSwitch.IsChecked == true;
                UpdateAiNote();
                Touch();
            };
            AiLocalSwitch.Checked += local;
            AiLocalSwitch.Unchecked += local;
            AiSetupBtn.Click += (s, e) => ShowAiProviders();
            AddProviderBox.SelectionChanged += (s, e) =>
            {
                var kind = TagOf(AddProviderBox) as string;
                if (_loading || kind == null) return;
                _loading = true; AddProviderBox.SelectedIndex = -1; _loading = false;
                AddProvider(kind);
            };

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
                    Text = (w.HasTrigger ? w.TriggerDisplay() + " halten" : "Kein Auslöser") + (w.Everywhere ? "" : " · nur in " + w.AppsDisplay()),
                    FontSize = 12, Foreground = Res("Ink2"),
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
            BuildScope();
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

            UpdateNotes();

            LoadPreview();
            BuildCrumbs();
            _loading = was;
        }

        /// <summary>Hinweise unter dem Kopf: Probleme mit dem Auslöser, welches Rad wo aufgeht, wie man es öffnet.</summary>
        void UpdateNotes()
        {
            TriggerNote.Text = TriggerProblem();
            TriggerNote.Visibility = TriggerNote.Text.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
            ScopeNote.Text = TriggerNote.Text.Length > 0 ? "" : ScopeInfo();
            ScopeNote.Visibility = ScopeNote.Text.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
            UsageHint.Text = !_wheel.HasTrigger
                ? "Lege oben einen Auslöser fest, um dieses Rad zu öffnen."
                : "So öffnest du das Rad: " + (_wheel.Everywhere ? "" : "in " + _wheel.AppsDisplay() + " ") +
                  _wheel.TriggerDisplay() + " halten, Maus in eine Richtung bewegen, loslassen.";
        }

        void SetTrigger(Chord c, bool second)
        {
            if (second) _wheel.Trigger2 = c;
            else _wheel.Trigger = c;
            RebuildWheelList();
            LoadStage();
            Touch();
        }

        /// <summary>Warnung unter den Auslösern: fehlt einer, sind beide gleich, oder nutzt ein anderes Rad denselben in denselben Programmen?</summary>
        string TriggerProblem()
        {
            if (_wheel.Trigger.IsEmpty) return "Ohne Auslöser lässt sich dieses Rad nicht öffnen.";
            if (_wheel.Trigger2.SameAs(_wheel.Trigger)) return "Beide Auslöser sind gleich. Lege als zweiten eine andere Taste fest.";
            bool two = !_wheel.Trigger2.IsEmpty;
            foreach (var t in _wheel.Triggers)
            {
                var clash = Cfg.Wheels.FirstOrDefault(o => o != _wheel && o.SameScope(_wheel) && o.Triggers.Any(x => x.SameAs(t)));
                if (clash == null) continue;
                return (two ? t.Display() + " nutzt" : "Diesen Auslöser nutzt") + " auch das Rad „" + clash.Name +
                       "“. Es öffnet sich das Rad, das in der Liste weiter oben steht.";
            }
            return "";
        }

        // ---------------------------------------------------------------- Programme, in denen das Rad gilt

        readonly Dictionary<string, ImageSource> _exeIcons = new Dictionary<string, ImageSource>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Programme, die viel Verschiedenes zeigen (Browser, Java, Fernzugriff) – bei ihnen hilft meist erst der Titel.</summary>
        static readonly HashSet<string> Hosts = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "chrome", "msedge", "firefox", "opera", "brave", "vivaldi", "iexplore", "java", "javaw", "mstsc", "msrdc", "wfica32", "CDViewer"
        };

        /// <summary>Baut die Zeile „Gilt in“: ohne Einträge „Allen Programmen“, sonst ein Programm pro Zeile.</summary>
        void BuildScope()
        {
            ScopeHost.Children.Clear();
            _wheel.Apps.RemoveAll(a => a.IsEmpty);
            if (_wheel.Apps.Count == 0)
            {
                var row = new WrapPanel();
                row.Children.Add(new TextBlock { Text = "Allen Programmen", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 14, 8) });
                row.Children.Add(AppPicker("Auf ein Programm beschränken …"));
                ScopeHost.Children.Add(row);
                return;
            }
            foreach (var a in _wheel.Apps) ScopeHost.Children.Add(AppRow(a));
            ScopeHost.Children.Add(AppPicker("Weiteres Programm …"));
        }

        void ScopeChanged()
        {
            // Erst nach dem Ereignis neu aufbauen: Es kommt aus einem Element, das dabei verschwindet.
            _w.Dispatcher.BeginInvoke(new Action(() =>
            {
                BuildScope();
                RebuildWheelList();
                LoadStage();
            }));
            Touch();
        }

        FrameworkElement AppRow(AppScope a)
        {
            var panel = new StackPanel { Margin = new Thickness(0, 0, 0, 8) };
            // Symbol | Name | Titelfeld | Entfernen | Rest. Name und Titelfeld teilen sich den Platz bis zu ihrer Höchstbreite
            // und schrumpfen bei schmalem Fenster gemeinsam, statt abgeschnitten zu werden. Jede Zeile rechnet gleich,
            // deshalb stehen die Titelfelder untereinander.
            var row = new Grid();
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star), MinWidth = 70, MaxWidth = 170 });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star), MinWidth = 130, MaxWidth = 260 });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.001, GridUnitType.Star) });
            var icon = ExeIcon(a.Path);
            var glyph = icon != null
                ? (FrameworkElement)new Image { Source = icon, Width = 16, Height = 16, Margin = new Thickness(0, 0, 8, 0), VerticalAlignment = VerticalAlignment.Center }
                : new TextBlock { Text = "", Style = (Style)_w.FindResource("Glyph"), Foreground = Res("Ink2"), Width = 16, Margin = new Thickness(0, 0, 8, 0) };
            row.Children.Add(glyph);
            var name = new TextBlock
            {
                Text = a.DisplayName, FontWeight = FontWeights.SemiBold, VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis, Margin = new Thickness(0, 0, 12, 0),
                ToolTip = a.DisplayName + (a.Exe.Length > 0 ? " (" + a.Exe + ")" : " – erkannt am Fenstertitel")
            };
            Grid.SetColumn(name, 1);
            row.Children.Add(name);
            var title = new TextBox { Text = a.Title, Tag = "Titel enthält … (optional)" };
            title.ToolTip = "Nur Fenster, deren Titel diesen Text enthält – etwa „Gmail“ im Browser. Leer: alle Fenster des Programms.";
            Grid.SetColumn(title, 2);
            row.Children.Add(title);
            var remove = new Button
            {
                Style = (Style)_w.FindResource("BtnIcon"), Margin = new Thickness(4, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center,
                ToolTip = "Programm entfernen", Content = new TextBlock { Text = "", Style = (Style)_w.FindResource("Glyph"), FontSize = 11 }
            };
            remove.Click += (s, e) => { _wheel.Apps.Remove(a); ScopeChanged(); };
            Grid.SetColumn(remove, 3);
            row.Children.Add(remove);
            panel.Children.Add(row);

            // Ein Browser zeigt jede Website – ohne Titel gälte das Rad in allen.
            var hint = new TextBlock
            {
                Style = (Style)_w.FindResource("Help"), Margin = new Thickness(24, 4, 0, 0), MaxWidth = 460, HorizontalAlignment = HorizontalAlignment.Left
            };
            Action showHint = () =>
            {
                bool host = Hosts.Contains(a.Key) && a.TitleKey.Length == 0;
                hint.Text = host ? "Gilt in jedem Fenster von " + a.DisplayName + ". Für eine bestimmte Website oder ein bestimmtes Programm darin " +
                                   "einen Teil des Fenstertitels eintragen, der immer dasteht – etwa „Gmail“." : "";
                hint.Visibility = host ? Visibility.Visible : Visibility.Collapsed;
            };
            showHint();
            panel.Children.Add(hint);
            title.TextChanged += (s, e) =>
            {
                a.Title = title.Text;
                showHint();
                UpdateNotes();
                Touch();
            };
            return panel;
        }

        /// <summary>Liste der gerade offenen Programme – entsteht erst beim Aufklappen, damit sie aktuell ist.</summary>
        ComboBox AppPicker(string placeholder)
        {
            var pick = new ComboBox { Tag = placeholder, MinWidth = 260, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 0, 0, 8) };
            bool filling = false;
            pick.DropDownOpened += (s, e) =>
            {
                filling = true;
                pick.Items.Clear();
                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var w in WindowFocus.List())
                {
                    var c = Candidate(w);
                    if (c.IsEmpty || !seen.Add(c.Key + "|" + c.TitleKey) || _wheel.Apps.Any(x => x.SameAs(c))) continue;
                    pick.Items.Add(new ComboBoxItem { Content = PickerLine(c, w.Title), Tag = c });
                }
                if (pick.Items.Count == 0) pick.Items.Add(new ComboBoxItem { Content = "Keine weiteren Programme offen", IsEnabled = false });
                filling = false;
            };
            pick.SelectionChanged += (s, e) =>
            {
                var c = TagOf(pick) as AppScope;
                if (filling || c == null) return;
                _wheel.Apps.Add(c);
                ScopeChanged();
            };
            return pick;
        }

        FrameworkElement PickerLine(AppScope c, string windowTitle)
        {
            var line = new StackPanel { Orientation = Orientation.Horizontal };
            line.Children.Add(new Image { Source = ExeIcon(c.Path), Width = 16, Height = 16, Margin = new Thickness(0, 0, 8, 0), VerticalAlignment = VerticalAlignment.Center });
            line.Children.Add(new TextBlock { Text = c.DisplayName, VerticalAlignment = VerticalAlignment.Center });
            string t = c.Exe.Length > 0 ? (windowTitle ?? "").Trim() : "";
            if (t.Length > 0 && !string.Equals(t, c.DisplayName, StringComparison.OrdinalIgnoreCase))
                line.Children.Add(new TextBlock
                {
                    Text = "  ·  " + t, Foreground = Res("Ink2"), MaxWidth = 300, TextTrimming = TextTrimming.CharacterEllipsis,
                    VerticalAlignment = VerticalAlignment.Center
                });
            return line;
        }

        /// <summary>Universal-Apps teilen sich einen Rahmenprozess; bei ihnen hilft nur der Titel.</summary>
        static AppScope Candidate(WindowFocus.Info w)
        {
            bool frame = string.Equals(w.ExeName, WindowFocus.AppFrameHost, StringComparison.OrdinalIgnoreCase) || w.ExePath.Length == 0;
            if (frame) return new AppScope { Name = w.Title, Title = w.Title };
            return new AppScope { Exe = w.ExeName, Path = w.ExePath, Name = FriendlyName(w.ExePath) };
        }

        /// <summary>„Microsoft Outlook“ statt „OUTLOOK.EXE“ – aus der Dateibeschreibung der Exe, notfalls ihr Name.</summary>
        static string FriendlyName(string path)
        {
            try
            {
                var v = FileVersionInfo.GetVersionInfo(path);
                foreach (var n in new[] { v.FileDescription, v.ProductName })
                {
                    string t = (n ?? "").Trim();
                    if (t.Length > 1 && t.Length <= 40) return t;
                }
            }
            catch (Exception) { }   // Datei nicht lesbar: dann eben der Dateiname
            return AppScope.ProgramKey(path);
        }

        ImageSource ExeIcon(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            ImageSource img;
            if (_exeIcons.TryGetValue(path, out img)) return img;
            try
            {
                if (File.Exists(path))
                    using (var ico = System.Drawing.Icon.ExtractAssociatedIcon(path))
                    {
                        var src = Imaging.CreateBitmapSourceFromHIcon(ico.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                        src.Freeze();
                        img = src;
                    }
            }
            catch (Exception) { }   // ohne Symbol geht es auch
            _exeIcons[path] = img;
            return img;
        }

        /// <summary>Welches Rad geht wo auf? Nur wenn sich Räder einen Auslöser teilen oder dieses auf Programme beschränkt ist.</summary>
        string ScopeInfo()
        {
            if (!_wheel.HasTrigger) return "";
            foreach (var t in _wheel.Triggers)
            {
                var others = Cfg.Wheels.Where(o => o != _wheel && !o.SameScope(_wheel) && o.Triggers.Any(x => x.SameAs(t))).ToList();
                if (!_wheel.Everywhere)
                {
                    var fallback = others.FirstOrDefault(o => o.Everywhere);
                    if (fallback != null)
                        return "In " + _wheel.AppsDisplay() + " öffnet " + t.Display() + " dieses Rad, sonst das Rad „" + fallback.Name + "“.";
                }
                else
                {
                    var special = others.Where(o => !o.Everywhere).ToList();
                    if (special.Count == 1)
                        return "In " + special[0].AppsDisplay() + " öffnet " + t.Display() + " stattdessen das Rad „" + special[0].Name + "“.";
                    if (special.Count > 1)
                        return t.Display() + " öffnet in manchen Programmen ein eigenes Rad: " +
                               string.Join(", ", special.Select(o => "„" + o.Name + "“ in " + o.AppsDisplay())) + ".";
                }
            }
            if (!_wheel.Everywhere) return "In anderen Programmen bleibt " + _wheel.Trigger.Display() + " eine ganz normale Taste.";
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

        void FillFolderFromFile(Slot folder)
        {
            var dlg = new OpenFileDialog { Title = "Rad in Unterrad einsetzen", Filter = "Kyklos-Datei|*.json|Alle Dateien|*.*" };
            if (dlg.ShowDialog(_w) != true) return;
            List<Wheel> wheels;
            try
            {
                wheels = Config.FromJson(Json.Parse(File.ReadAllText(dlg.FileName, Encoding.UTF8)) as Dictionary<string, object>).Wheels;
                if (wheels.Count == 0) throw new FormatException("Die Datei enthält keine Räder.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(_w, "Die Datei ist keine gültige Kyklos-Datei:\n\n" + ex.Message, "Kyklos", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            FillFolder(folder, wheels);
        }

        /// <summary>
        /// Setzt die Segmente des ersten Rads als Inhalt des Unterrads ein (Kopie). Auslöser und Programmzuordnung des Rads
        /// entfallen. Liefert die Zahl der eingesetzten Segmente, 0 bei Abbruch.
        /// </summary>
        int FillFolder(Slot folder, List<Wheel> wheels)
        {
            if (folder == null || folder.Action.Type != ActionType.Folder || !CurrentSlots.Contains(folder))
            {
                MessageBox.Show(_w, "Das Unterrad-Segment ist nicht mehr gewählt. Wähle es aus und versuche es noch einmal.", "Kyklos", MessageBoxButton.OK, MessageBoxImage.Information);
                return 0;
            }
            var src = wheels[0];
            var slots = src.Slots.Where(s => !s.IsEmpty).ToList();
            if (slots.Count > Wheel.MaxSlots)
            {
                MessageBox.Show(_w, "Das Rad „" + src.Name + "“ hat " + slots.Count + " Segmente, ein Unterrad fasst höchstens " + Wheel.MaxSlots + ".",
                                "Kyklos", MessageBoxButton.OK, MessageBoxImage.Information);
                return 0;
            }
            if (folder.Action.Slots.Any(s => !s.IsEmpty) &&
                MessageBox.Show(_w, "Der bisherige Inhalt dieses Unterrads wird durch die Segmente von „" + src.Name + "“ ersetzt.",
                                "Rad einsetzen", MessageBoxButton.OKCancel, MessageBoxImage.Question, MessageBoxResult.Cancel) != MessageBoxResult.OK)
                return 0;
            folder.Action.Slots = slots;
            Wheel.Normalize(folder.Action.Slots);
            if (string.IsNullOrWhiteSpace(folder.Label)) folder.Label = src.Name;
            LoadStage();
            LoadInspector();
            Touch();
            if (wheels.Count > 1)
                MessageBox.Show(_w, "Die Datei enthält " + wheels.Count + " Räder. Eingesetzt wurde das erste: „" + src.Name + "“.",
                                "Rad einsetzen", MessageBoxButton.OK, MessageBoxImage.Information);
            return slots.Count;
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
            if (AiPrompt.Text != a.Text) AiPrompt.Text = a.Text;
            SelectTag(AiSourceBox, a.AiSource);
            SelectTag(AiOutputBox, a.AiOutput);
            AiOutputHelp.Text = OutputHelp(a.AiOutput);
            FillProviderBox(AiProviderBox, a.Provider);
            AiLocalSwitch.IsChecked = a.LocalOnly;
            PAi.Visibility = Vis(a.Type == ActionType.Ai);
            if (a.Type == ActionType.Ai) UpdateAiNote();

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
                case ActionType.Focus:
                    return FocusEditor(step);
                case ActionType.Ai:
                    return AiStepEditor(step);
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

        FrameworkElement FocusEditor(ActionDef step)
        {
            var panel = new StackPanel();
            var prog = new TextBox { Text = step.Path, Tag = "Programm, z. B. Transkript.exe" };
            var title = new TextBox { Text = step.Title, Tag = "Teil des Fenstertitels" };
            prog.TextChanged += (s, e) => { step.Path = prog.Text; Touch(); };
            title.TextChanged += (s, e) => { step.Title = title.Text; Touch(); };

            // Die Liste entsteht erst beim Aufklappen, damit sie die Fenster zeigt, die gerade offen sind.
            var pick = new ComboBox { Tag = "Aus offenen Fenstern übernehmen …" };
            bool filling = false;
            pick.DropDownOpened += (s, e) =>
            {
                filling = true;
                pick.Items.Clear();
                foreach (var w in WindowFocus.List())
                {
                    string t = w.Title.Length > 60 ? w.Title.Substring(0, 58).TrimEnd() + " …" : w.Title;
                    if (t.Length == 0) t = "Ohne Titel";
                    pick.Items.Add(Item(w.ExeName.Length > 0 ? t + "  ·  " + w.ExeName : t, w));
                }
                if (pick.Items.Count == 0) pick.Items.Add(new ComboBoxItem { Content = "Keine Fenster gefunden", IsEnabled = false });
                filling = false;
            };
            pick.SelectionChanged += (s, e) =>
            {
                var w = TagOf(pick) as WindowFocus.Info;
                if (filling || w == null) return;
                // Universal-Apps teilen sich einen Rahmenprozess; bei ihnen hilft nur der Titel.
                bool frame = string.Equals(w.ExeName, WindowFocus.AppFrameHost, StringComparison.OrdinalIgnoreCase) || w.ExePath.Length == 0;
                prog.Text = frame ? "" : w.ExePath;
                title.Text = frame ? w.Title : "";
                filling = true; pick.SelectedIndex = -1; filling = false;
            };
            panel.Children.Add(pick);

            panel.Children.Add(new TextBlock { Text = "Programm", Style = (Style)_w.FindResource("FieldLabel"), Margin = new Thickness(0, 12, 0, 6) });
            panel.Children.Add(prog);
            panel.Children.Add(new TextBlock { Text = "Fenstertitel enthält (optional)", Style = (Style)_w.FindResource("FieldLabel"), Margin = new Thickness(0, 12, 0, 6) });
            panel.Children.Add(title);

            var launch = new CheckBox
            {
                Style = (Style)_w.FindResource("Switch"), IsChecked = step.Launch, Margin = new Thickness(0, 12, 0, 0),
                Content = new TextBlock { Text = "Starten, wenn kein Fenster offen ist", VerticalAlignment = VerticalAlignment.Center }
            };
            launch.Checked += (s, e) => { step.Launch = true; Touch(); };
            launch.Unchecked += (s, e) => { step.Launch = false; Touch(); };
            panel.Children.Add(launch);

            panel.Children.Add(new TextBlock
            {
                Style = (Style)_w.FindResource("Help"), Margin = new Thickness(0, 8, 0, 0),
                Text = "Holt das zuletzt benutzte Fenster des Programms nach vorn – auch wenn es verdeckt, verschoben oder minimiert ist. " +
                       "Findet sich keins, bricht das Makro ab, damit die folgenden Tasten nicht im falschen Programm landen."
            });
            return panel;
        }

        // ---------------------------------------------------------------- KI-Anweisung

        static void FillAiChoices(ComboBox source, ComboBox output)
        {
            source.Items.Add(Item("Markierten Text (wird mit Strg+C kopiert)", AiSources.Selection));
            source.Items.Add(Item("Inhalt der Zwischenablage", AiSources.Clipboard));
            output.Items.Add(Item("In die Zwischenablage legen", AiOutputs.Clipboard));
            output.Items.Add(Item("Markierung direkt ersetzen", AiOutputs.Replace));
        }

        static string OutputHelp(string output)
        {
            return output == AiOutputs.Replace
                ? "Sobald die KI fertig ist, wird die Markierung ersetzt – aber nur, wenn dasselbe Feld noch aktiv ist. Sonst liegt das Ergebnis in der Zwischenablage. Bis dahin nicht im Feld weitertippen."
                : "Ist die KI fertig, meldet sich Kyklos am Mauszeiger. Dann mit Strg+V einfügen – auch in einem anderen Programm.";
        }

        string EffectiveDefaultId()
        {
            var st = Cfg.Settings;
            var p = st.Providers.FirstOrDefault(x => x.Id == st.DefaultProvider) ?? st.Providers.FirstOrDefault();
            return p == null ? "" : p.Id;
        }

        static string ProviderLine(AiProvider p) { return p.DisplayName + (p.External ? " · extern" : " · lokal"); }

        /// <summary>„Standard: …“ und alle Anbieter. Eine Id, die es nicht mehr gibt, zeigt den Standard – den nutzt die Aktion dann auch.</summary>
        void FillProviderBox(ComboBox box, string selected)
        {
            bool was = _loading;
            _loading = true;
            box.Items.Clear();
            var st = Cfg.Settings;
            var def = st.Providers.FirstOrDefault(x => x.Id == EffectiveDefaultId());
            box.Items.Add(Item(def == null ? "Standard-Anbieter (noch keiner eingerichtet)" : "Standard: " + ProviderLine(def), ""));
            foreach (var p in st.Providers) box.Items.Add(Item(ProviderLine(p), p.Id));
            if (st.Providers.Any(p => p.Id == selected)) SelectTag(box, selected); else box.SelectedIndex = 0;
            _loading = was;
        }

        /// <summary>Hinweis unter der Anweisung: kein Anbieter, externer Anbieter, oder extern trotz „Nur lokal“.</summary>
        void UpdateAiNote()
        {
            var a = Cur.Action;
            var p = AiRunner.Resolve(a, Cfg.Settings);
            string title = null, text = null;
            bool danger = false;
            if (p == null)
            {
                title = "Noch kein KI-Anbieter eingerichtet";
                text = "Richte unter Allgemein Ollama oder einen Dienst ein. Bis dahin meldet das Segment beim Auslösen nur einen Hinweis.";
            }
            else if (p.External && a.LocalOnly)
            {
                danger = true;
                title = "So wird die Anweisung nicht ausgeführt";
                text = "„" + p.DisplayName + "“ ist ein externer Anbieter, die Anweisung ist aber auf lokal beschränkt. Wähle einen Ollama-Anbieter im eigenen Netz.";
            }
            else if (p.External)
            {
                danger = true;
                title = "Externer Anbieter – keine Patientendaten";
                text = "Der Text geht an „" + p.DisplayName + "“ und verlässt die Praxis. Unter keinen Umständen Patientendaten, Befunde oder Arztbriefe damit bearbeiten.";
            }
            AiNote.Visibility = Vis(title != null);
            if (title == null) return;
            AiNote.Background = Res(danger ? "DangerSoft" : "AccentSoft");
            AiNoteTitle.Foreground = Res(danger ? "Danger" : "Ink");
            AiNoteTitle.Text = title;
            AiNoteText.Text = text;
            AiSetupBtn.Visibility = Vis(p == null);
        }

        void ShowAiProviders()
        {
            ShowGeneral();
            ScrollGeneralTo(AiSection);
        }

        void ScrollGeneralTo(FrameworkElement e)
        {
            _w.Dispatcher.BeginInvoke(new Action(() =>
            {
                GeneralPanel.UpdateLayout();
                double y = e.TransformToAncestor(GeneralPanel).Transform(new Point(0, 0)).Y;
                GeneralPanel.ScrollToVerticalOffset(GeneralPanel.VerticalOffset + y - 20);
            }), DispatcherPriority.Loaded);
        }

        FrameworkElement AiStepEditor(ActionDef step)
        {
            var panel = new StackPanel();
            var prompt = new TextBox
            {
                Style = (Style)_w.FindResource("TextArea"), Text = step.Text, MinHeight = 60, MaxHeight = 140,
                Tag = "Was soll die KI mit dem Text tun?"
            };
            prompt.TextChanged += (s, e) => { step.Text = prompt.Text; Touch(); };
            panel.Children.Add(prompt);

            var src = new ComboBox();
            var output = new ComboBox();
            var prov = new ComboBox();
            FillAiChoices(src, output);
            SelectTag(src, step.AiSource);
            SelectTag(output, step.AiOutput);
            FillProviderBox(prov, step.Provider);
            Func<string, TextBlock> label = t => new TextBlock { Text = t, Style = (Style)_w.FindResource("FieldLabel"), Margin = new Thickness(0, 10, 0, 4) };
            panel.Children.Add(label("Text holen"));
            panel.Children.Add(src);
            panel.Children.Add(label("Ergebnis"));
            panel.Children.Add(output);
            panel.Children.Add(label("KI-Anbieter"));
            panel.Children.Add(prov);

            var local = new CheckBox
            {
                Style = (Style)_w.FindResource("Switch"), IsChecked = step.LocalOnly, Margin = new Thickness(0, 10, 0, 0),
                Content = new TextBlock { Text = "Nur lokal ausführen (Patientendaten)", VerticalAlignment = VerticalAlignment.Center }
            };
            panel.Children.Add(local);
            var warn = new TextBlock { Style = (Style)_w.FindResource("Help"), Foreground = Res("Danger") };
            panel.Children.Add(warn);
            Action note = () =>
            {
                var p = AiRunner.Resolve(step, Cfg.Settings);
                string t = p == null ? "Noch kein KI-Anbieter eingerichtet."
                         : p.External && step.LocalOnly ? "„" + p.DisplayName + "“ ist extern – dieser Schritt bricht ab, weil er auf lokal beschränkt ist."
                         : p.External ? "Externer Anbieter: keine Patientendaten!" : "";
                warn.Text = t;
                warn.Visibility = Vis(t.Length > 0);
            };
            note();

            src.SelectionChanged += (s, e) => { if (TagOf(src) != null) { step.AiSource = (string)TagOf(src); Touch(); } };
            output.SelectionChanged += (s, e) => { if (TagOf(output) != null) { step.AiOutput = (string)TagOf(output); Touch(); } };
            prov.SelectionChanged += (s, e) => { if (TagOf(prov) != null) { step.Provider = (string)TagOf(prov); note(); Touch(); } };
            local.Checked += (s, e) => { step.LocalOnly = true; note(); Touch(); };
            local.Unchecked += (s, e) => { step.LocalOnly = false; note(); Touch(); };

            panel.Children.Add(new TextBlock
            {
                Style = (Style)_w.FindResource("Help"), Margin = new Thickness(0, 8, 0, 0),
                Text = "Im Makro wartet der nächste Schritt, bis die KI fertig ist. Nach „Text erkennen“ als Quelle die Zwischenablage wählen."
            });
            return panel;
        }

        // ---------------------------------------------------------------- KI-Anbieter

        void BuildProviders()
        {
            ProviderHost.Children.Clear();
            foreach (var p in Cfg.Settings.Providers) ProviderHost.Children.Add(ProviderEditor(p));
        }

        void AddProvider(string kind)
        {
            if (kind != AiKinds.Ollama)
            {
                string q = AiKinds.Name(kind) + " ist ein Dienst im Internet. Alles, was du damit bearbeitest, verlässt die Praxis und wird auf den Servern des Anbieters verarbeitet.\n\n" +
                           "UNTER KEINEN UMSTÄNDEN DÜRFEN DAMIT PATIENTENDATEN VERARBEITET WERDEN – keine Namen, Befunde, Arztbriefe, Diagnosen oder andere " +
                           "personenbezogene Gesundheitsdaten (ärztliche Schweigepflicht § 203 StGB, DSGVO Art. 9).\n\n" +
                           "Nur für Texte ohne jeden Personenbezug verwenden. Anweisungen für Patientendaten lassen sich mit „Nur lokal ausführen“ gegen externe Anbieter sperren.\n\n" +
                           "Anbieter trotzdem hinzufügen?";
                if (MessageBox.Show(_w, q, "Externer KI-Anbieter – keine Patientendaten", MessageBoxButton.OKCancel, MessageBoxImage.Warning,
                                    MessageBoxResult.Cancel) != MessageBoxResult.OK) return;
            }
            var st = Cfg.Settings;
            var p = new AiProvider { Kind = kind };
            var current = st.Providers.FirstOrDefault(x => x.Id == EffectiveDefaultId());
            if (current != null) st.DefaultProvider = current.Id;
            st.Providers.Add(p);
            // Ein lokaler Anbieter löst einen externen als Standard ab – so geht nichts aus Versehen ins Internet.
            if (current == null || (current.External && !p.External)) st.DefaultProvider = p.Id;
            BuildProviders();
            Touch();
            var editor = ProviderHost.Children[ProviderHost.Children.Count - 1] as FrameworkElement;
            if (editor != null) ScrollGeneralTo(editor);
            if (kind == AiKinds.Ollama)
            {
                var load = editor == null ? null : editor.Tag as Func<Task>;
                if (load != null) _w.Dispatcher.BeginInvoke(new Action(async () => await load()), DispatcherPriority.Loaded);
            }
        }

        void SetStatus(TextBlock t, string text, bool bad)
        {
            t.Text = text;
            t.Foreground = Res(bad ? "Danger" : "Ink2");
            t.Visibility = Visibility.Visible;
        }

        static string Explain(Exception ex)
        {
            var ai = ex as AiException;
            if (ai != null) return ai.Message + (ai.Help.Length > 0 ? ". " + ai.Help : ".");
            if (ex is OperationCanceledException) return "Keine Antwort in der vorgesehenen Zeit.";
            return ex.Message;
        }

        /// <summary>Ein Anbieter als Zeile der Liste. Tag trägt die Funktion „Modelle laden“, damit ein neuer Ollama-Anbieter sie gleich ausführen kann.</summary>
        FrameworkElement ProviderEditor(AiProvider p)
        {
            var panel = new StackPanel();
            Action refresh = () => { };

            // Kopf: Name | Standard | Entfernen
            var head = new DockPanel();
            var remove = new Button { Style = (Style)_w.FindResource("BtnGhost"), Content = "Entfernen", Margin = new Thickness(4, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
            DockPanel.SetDock(remove, Dock.Right);
            remove.Click += (s, e) => Confirm(remove, "Wirklich entfernen?", () =>
            {
                Cfg.Settings.Providers.Remove(p);
                if (Cfg.Settings.DefaultProvider == p.Id) Cfg.Settings.DefaultProvider = "";
                try { ApiKeys.Remove(p.Id); }
                catch (Exception ex) { Log.Write("API-Schlüssel nicht entfernt: " + ex.Message); }
                BuildProviders();
                Touch();
            });
            head.Children.Add(remove);
            FrameworkElement std;
            if (EffectiveDefaultId() == p.Id)
                std = new TextBlock
                {
                    Text = "Standard", Style = (Style)_w.FindResource("InlineLabel"), Margin = new Thickness(14, 0, 8, 0),
                    ToolTip = "Segmente ohne eigene Anbieterwahl nutzen diesen Anbieter."
                };
            else
            {
                var b = new Button
                {
                    Style = (Style)_w.FindResource("BtnGhost"), Content = "Als Standard", Margin = new Thickness(8, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center,
                    ToolTip = "Segmente ohne eigene Anbieterwahl nutzen dann diesen Anbieter."
                };
                b.Click += (s, e) => { Cfg.Settings.DefaultProvider = p.Id; BuildProviders(); Touch(); };
                std = b;
            }
            DockPanel.SetDock(std, Dock.Right);
            head.Children.Add(std);
            var name = new TextBox { Text = p.Name, Tag = AiKinds.Name(p.Kind), FontWeight = FontWeights.SemiBold, MaxLength = 40 };
            name.TextChanged += (s, e) => { p.Name = name.Text; Touch(); };
            head.Children.Add(name);
            panel.Children.Add(head);

            var kindLine = new TextBlock { Style = (Style)_w.FindResource("Help") };
            panel.Children.Add(kindLine);

            var warnTitle = new TextBlock
            {
                Text = "Externer Anbieter – keine Patientendaten", FontWeight = FontWeights.SemiBold, Foreground = Res("Danger"), TextWrapping = TextWrapping.Wrap
            };
            var warnText = new TextBlock { TextWrapping = TextWrapping.Wrap, LineHeight = 18, Margin = new Thickness(0, 3, 0, 0) };
            var warnBody = new StackPanel();
            warnBody.Children.Add(warnTitle);
            warnBody.Children.Add(warnText);
            var warn = new Border
            {
                Background = Res("DangerSoft"), CornerRadius = new CornerRadius(6), Padding = new Thickness(12, 10, 12, 12),
                Margin = new Thickness(0, 12, 0, 0), Child = warnBody
            };
            panel.Children.Add(warn);

            Func<string, TextBlock> label = t => new TextBlock { Text = t, Style = (Style)_w.FindResource("FieldLabel"), Margin = new Thickness(0, 14, 0, 6) };
            Func<string, TextBlock> help = t => new TextBlock { Text = t, Style = (Style)_w.FindResource("Help") };

            if (p.Kind == AiKinds.Ollama)
            {
                panel.Children.Add(label("Adresse"));
                var url = new TextBox { Text = p.Url, Tag = "localhost:11434 – dieser PC" };
                url.TextChanged += (s, e) => { p.Url = url.Text; refresh(); Touch(); };
                panel.Children.Add(url);
                panel.Children.Add(help("Leer lassen für diesen PC. Für einen Rechner im Praxisnetz dessen Namen oder IP-Adresse eintragen, z. B. 192.168.178.20."));
            }

            if (p.NeedsKey)
            {
                panel.Children.Add(label("API-Schlüssel"));
                var keyHost = new StackPanel();
                panel.Children.Add(keyHost);
                panel.Children.Add(help("Wird mit Windows für dein Benutzerkonto verschlüsselt gespeichert, nie angezeigt und nie exportiert. Auf einem anderen PC muss er neu eingetragen werden."));
                Action<bool> buildKey = null;
                buildKey = edit =>
                {
                    keyHost.Children.Clear();
                    bool has = ApiKeys.Has(p.Id);
                    if (has && !edit)
                    {
                        string hint = ApiKeys.Hint(p.Id);
                        var row = new DockPanel();
                        var tools = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(12, 0, 0, 0) };
                        DockPanel.SetDock(tools, Dock.Right);
                        var replace = new Button { Style = (Style)_w.FindResource("Btn"), Content = "Ersetzen" };
                        replace.Click += (s, e) => buildKey(true);
                        var del = new Button { Style = (Style)_w.FindResource("BtnGhost"), Content = "Entfernen", Margin = new Thickness(4, 0, 0, 0) };
                        del.Click += (s, e) => Confirm(del, "Wirklich entfernen?", () =>
                        {
                            try { ApiKeys.Remove(p.Id); }
                            catch (Exception ex) { MessageBox.Show(_w, "Der Schlüssel konnte nicht entfernt werden:\n\n" + ex.Message, "Kyklos", MessageBoxButton.OK, MessageBoxImage.Warning); }
                            buildKey(false);
                        });
                        tools.Children.Add(replace);
                        tools.Children.Add(del);
                        row.Children.Add(tools);
                        string shown = hint == null ? "Gespeichert, aber auf diesem PC nicht lesbar. Bitte ersetzen."
                                     : hint.Length > 1 ? "Verschlüsselt gespeichert, endet auf " + hint.Substring(1) : "Verschlüsselt gespeichert";
                        row.Children.Add(new TextBlock
                        {
                            Text = shown, TextWrapping = TextWrapping.Wrap, VerticalAlignment = VerticalAlignment.Center,
                            Foreground = Res(hint == null ? "Danger" : "Ink")
                        });
                        keyHost.Children.Add(row);
                        return;
                    }
                    var input = new DockPanel();
                    var save = new Button { Style = (Style)_w.FindResource("Btn"), Content = "Speichern", Margin = new Thickness(8, 0, 0, 0) };
                    DockPanel.SetDock(save, Dock.Right);
                    input.Children.Add(save);
                    var pw = new PasswordBox();
                    input.Children.Add(pw);
                    Action store = () =>
                    {
                        string k = pw.Password.Trim();
                        if (k.Length == 0) { pw.Focus(); return; }
                        try { ApiKeys.Set(p.Id, k); }
                        catch (Exception ex)
                        {
                            MessageBox.Show(_w, "Der Schlüssel konnte nicht gespeichert werden:\n\n" + ex.Message, "Kyklos", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
                        pw.Clear();
                        buildKey(false);
                    };
                    save.Click += (s, e) => store();
                    pw.KeyDown += (s, e) => { if (e.Key == Key.Enter) { e.Handled = true; store(); } };
                    keyHost.Children.Add(input);
                    if (edit) pw.Focus();
                };
                buildKey(false);
            }

            panel.Children.Add(label("Modell"));
            var modelRow = new DockPanel();
            var load = new Button { Style = (Style)_w.FindResource("Btn"), Content = "Modelle laden", Margin = new Thickness(8, 0, 0, 0) };
            DockPanel.SetDock(load, Dock.Right);
            modelRow.Children.Add(load);
            var model = new TextBox { Text = p.Model, Tag = "Modellname – oder „Modelle laden“" };
            modelRow.Children.Add(model);
            panel.Children.Add(modelRow);
            var pick = new ComboBox { Tag = "Modell aus der Liste wählen …", Margin = new Thickness(0, 8, 0, 0), Visibility = Visibility.Collapsed };
            panel.Children.Add(pick);
            model.TextChanged += (s, e) => { p.Model = model.Text.Trim(); refresh(); Touch(); };
            bool filling = false;
            pick.SelectionChanged += (s, e) =>
            {
                var id = TagOf(pick) as string;
                if (!filling && id != null) model.Text = id;
            };

            var test = new Button { Style = (Style)_w.FindResource("Btn"), Content = "Verbindung testen", HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 12, 0, 0) };
            panel.Children.Add(test);
            var status = new TextBlock { Style = (Style)_w.FindResource("Help"), Visibility = Visibility.Collapsed };
            panel.Children.Add(status);

            Func<string> keyOrThrow = () =>
            {
                if (!p.NeedsKey) return null;
                string k = ApiKeys.Get(p.Id);
                if (k == null) throw new AiException("Kein API-Schlüssel", "Erst den Schlüssel eintragen und speichern");
                return k;
            };

            Func<Task> loadModels = async () =>
            {
                load.IsEnabled = false;
                load.Content = "Lädt …";
                try
                {
                    List<string> ids;
                    using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20)))
                        ids = await AiClient.Models(p, keyOrThrow(), cts.Token);
                    filling = true;
                    pick.Items.Clear();
                    foreach (var id in ids) pick.Items.Add(Item(id, id));
                    SelectTag(pick, p.Model);
                    filling = false;
                    pick.Visibility = Vis(ids.Count > 0);
                    if (ids.Count == 0)
                        SetStatus(status, p.Kind == AiKinds.Ollama
                            ? "Ollama läuft, hat aber noch kein Modell. Auf dem Ollama-Rechner zum Beispiel „ollama pull qwen3:8b“ ausführen."
                            : "Keine Modelle gefunden.", true);
                    else
                        SetStatus(status, (ids.Count == 1 ? "1 Modell" : ids.Count + " Modelle") + " verfügbar." +
                                          (p.Model.Length == 0 ? " Wähle eines aus der Liste." : ""), false);
                }
                catch (Exception ex) { SetStatus(status, Explain(ex), true); }
                finally
                {
                    filling = false;
                    load.IsEnabled = true;
                    load.Content = "Modelle laden";
                }
            };
            load.Click += async (s, e) => await loadModels();

            test.Click += async (s, e) =>
            {
                test.IsEnabled = false;
                test.Content = "Teste …";
                var clock = Stopwatch.StartNew();
                try
                {
                    AiResult r;
                    // Nur ein fester Prüftext – nie Inhalte aus der Praxis.
                    using (var cts = new CancellationTokenSource(TimeSpan.FromMinutes(3)))
                        r = await AiClient.Complete(p, keyOrThrow(), "Antworte nur mit dem Wort OK.", "Verbindungstest", cts.Token);
                    string answer = r.Text.Length > 40 ? r.Text.Substring(0, 38) + " …" : r.Text;
                    SetStatus(status, "Verbunden. Antwort nach " + clock.Elapsed.TotalSeconds.ToString("0.0") + " s: „" + answer + "“", false);
                }
                catch (Exception ex) { SetStatus(status, Explain(ex), true); }
                finally
                {
                    test.IsEnabled = true;
                    test.Content = "Verbindung testen";
                }
            };

            refresh = () =>
            {
                string reason = p.ExternalReason();
                kindLine.Text = AiKinds.Name(p.Kind) + (reason == null ? " · lokal: die Texte bleiben im eigenen Netz" : " · extern");
                warn.Visibility = Vis(reason != null);
                if (reason != null)
                    warnText.Text = reason + " Was du damit bearbeitest, verlässt die Praxis. Unter keinen Umständen Patientendaten, Befunde oder Arztbriefe " +
                                    "verarbeiten (Schweigepflicht § 203 StGB, DSGVO Art. 9) – nur Texte ohne Personenbezug.";
            };
            refresh();

            return new Border
            {
                BorderBrush = Res("Line"), BorderThickness = new Thickness(0, 0, 0, 1), Padding = new Thickness(0, 16, 0, 18),
                Child = panel, Tag = loadModels
            };
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
            // Ein Auslöser bricht mit Esc ab (Esc schließt das Rad). Eine zu sendende Kombination darf Esc sein,
            // dort bricht ein zweiter Klick auf die Schaltfläche ab.
            b.Content = allowMouse ? "Taste oder Maus-Seitentaste drücken …" : "Kombination drücken … (Klick bricht ab)";
            InputHook.RecordAllowMouse = allowMouse;
            InputHook.RecordAllowEscape = !allowMouse;
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
            BuildProviders();
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

        /// <summary>Speichert nur das gewählte Rad – im selben Format wie der Gesamt-Export, nur ohne Einstellungen.</summary>
        void ExportOne()
        {
            string name = (_wheel.Name ?? "").Trim();
            foreach (char c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');
            if (name.Length == 0) name = "Rad";
            var dlg = new SaveFileDialog { Title = "Rad exportieren", Filter = "Kyklos-Rad|*.json", FileName = name + ".kyklos.json" };
            if (dlg.ShowDialog(_w) != true) return;
            var d = new Dictionary<string, object>();
            d["version"] = 1;
            d["wheels"] = new List<object> { _wheel.ToJson() };
            try { File.WriteAllText(dlg.FileName, Json.Write(d), new UTF8Encoding(false)); }
            catch (Exception ex)
            {
                MessageBox.Show(_w, "Der Export ist fehlgeschlagen:\n\n" + ex.Message, "Kyklos", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// Hängt die Räder aus einer Datei an – aus einem Einzel- wie aus einem Gesamt-Export. Ein Auslöser, den im selben
        /// Programm schon ein anderes Rad nutzt, wird geleert, damit nicht zwei Räder um dieselbe Taste konkurrieren.
        /// </summary>
        void ImportAdd()
        {
            var dlg = new OpenFileDialog { Title = "Rad importieren", Filter = "Kyklos-Datei|*.json|Alle Dateien|*.*" };
            if (dlg.ShowDialog(_w) != true) return;
            List<Wheel> wheels;
            try
            {
                wheels = Config.FromJson(Json.Parse(File.ReadAllText(dlg.FileName, Encoding.UTF8)) as Dictionary<string, object>).Wheels;
                if (wheels.Count == 0) throw new FormatException("Die Datei enthält keine Räder.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(_w, "Die Datei ist keine gültige Kyklos-Datei:\n\n" + ex.Message, "Kyklos",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            AddWheels(wheels);
        }

        /// <summary>Hängt Räder an die vorhandenen an (neue Ids, Auslöser-Konflikte geleert) und wählt das erste. Liefert die Anzahl.</summary>
        int AddWheels(List<Wheel> wheels)
        {
            var cleared = new List<string>();
            foreach (var w in wheels)
            {
                w.Id = new Wheel().Id;
                bool lost = false;
                if (Clashes(w, w.Trigger)) { w.Trigger = new Chord(); lost = true; }
                if (Clashes(w, w.Trigger2)) { w.Trigger2 = new Chord(); lost = true; }
                if (w.Trigger.IsEmpty && !w.Trigger2.IsEmpty) { w.Trigger = w.Trigger2; w.Trigger2 = new Chord(); }
                if (lost) cleared.Add("„" + w.Name + "“");
                Cfg.Wheels.Add(w);
            }
            RebuildWheelList();
            ShowEditor();
            SelectWheel(wheels[0]);
            Touch();
            if (cleared.Count > 0)
                MessageBox.Show(_w, (cleared.Count == 1 ? "Beim Rad " + cleared[0] + " wurde" : "Bei den Rädern " + string.Join(", ", cleared) + " wurde") +
                                " der Auslöser entfernt, weil ein vorhandenes Rad dieselbe Taste nutzt.\n\nLege dafür eine eigene Taste fest.",
                                "Rad importieren", MessageBoxButton.OK, MessageBoxImage.Information);
            return wheels.Count;
        }

        bool Clashes(Wheel w, Chord t)
        {
            return !t.IsEmpty && Cfg.Wheels.Any(o => o != w && o.SameScope(w) && o.Triggers.Any(x => x.SameAs(t)));
        }

        void Import()
        {
            var dlg = new OpenFileDialog { Title = "Konfiguration importieren", Filter = "Kyklos-Konfiguration|*.json|Alle Dateien|*.*" };
            if (dlg.ShowDialog(_w) != true) return;
            Config cfg;
            try
            {
                var d = Json.Parse(File.ReadAllText(dlg.FileName, Encoding.UTF8)) as Dictionary<string, object>;
                cfg = Config.FromJson(d);
                if (cfg.Wheels.Count == 0) throw new FormatException("Die Datei enthält keine Räder.");
                // Ein Einzelrad-Export bringt keine Einstellungen mit – dann bleiben die bisherigen.
                if (d == null || !d.ContainsKey("settings")) cfg.Settings = Cfg.Settings;
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
