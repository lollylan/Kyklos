using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Kyklos
{
    /// <summary>
    /// Lücken im Text, die erst beim Einfügen gefüllt werden:
    ///   {?Tage}                                   Eingabefeld
    ///   {?Symptome: Husten | Schnupfen | Fieber}  Mehrfachauswahl, wird zu „Husten, Schnupfen und Fieber"
    ///   {?FSME-Impfung: ja / nein / unbekannt}    Entweder-oder, genau eine Option
    /// Ein Schrägstrich zwischen zwei Ziffern (1/2, 5/10 mg) trennt keine Optionen.
    /// Steht dieselbe Lücke mehrmals im Text, wird sie einmal abgefragt.
    /// </summary>
    public static class Gaps
    {
        static readonly Regex Pattern = new Regex(@"\{\?([^{}:]*)(?::([^{}]*))?\}");
        static readonly Regex Slash = new Regex(@"\s*(?:(?<!\d)/|/(?!\d))\s*");

        public sealed class Gap
        {
            public string Key, Name;
            public List<string> Options;    // null = Eingabefeld
            public bool Single;             // Entweder-oder statt Mehrfachauswahl
            public string Value = "";
            public bool[] Picked;

            public bool IsChoice { get { return Options != null; } }

            public string Result()
            {
                if (!IsChoice) return (Value ?? "").Trim();
                return JoinList(Options.Where((o, i) => Picked[i]).ToList());
            }
        }

        public static bool Has(string text) { return !string.IsNullOrEmpty(text) && Pattern.IsMatch(text); }

        static string NameOf(Match m)
        {
            string n = m.Groups[1].Value.Trim();
            return n.Length > 0 ? n : "Lücke";
        }

        /// <summary>Mit | getrennt: Mehrfachauswahl. Nur mit / getrennt: Entweder-oder.</summary>
        static List<string> OptionsOf(Match m, out bool single)
        {
            single = false;
            if (!m.Groups[2].Success) return null;
            string raw = m.Groups[2].Value;
            single = raw.IndexOf('|') < 0 && Slash.IsMatch(raw);
            var o = (single ? Slash.Split(raw) : raw.Split('|')).Select(s => s.Trim()).Where(s => s.Length > 0).ToList();
            if (o.Count < 2) single = false;    // eine einzelne Option bleibt ein Kästchen zum Ankreuzen
            return o.Count > 0 ? o : null;
        }

        static string KeyOf(Match m)
        {
            bool single;
            var o = OptionsOf(m, out single);
            return NameOf(m) + "\n" + (o == null ? "" : (single ? "/\n" : "|\n") + string.Join("\n", o));
        }

        /// <summary>Die Lücken in der Reihenfolge ihres ersten Auftretens.</summary>
        public static List<Gap> Parse(string text)
        {
            var list = new List<Gap>();
            foreach (Match m in Pattern.Matches(text ?? ""))
            {
                string key = KeyOf(m);
                if (list.Any(g => g.Key == key)) continue;
                bool single;
                var o = OptionsOf(m, out single);
                list.Add(new Gap { Key = key, Name = NameOf(m), Options = o, Single = single, Picked = o == null ? null : new bool[o.Count] });
            }
            return list;
        }

        public static string Fill(string text, List<Gap> gaps)
        {
            return Pattern.Replace(text ?? "", m =>
            {
                string key = KeyOf(m);
                var g = gaps.FirstOrDefault(x => x.Key == key);
                return g == null ? "" : g.Result();
            });
        }

        /// <summary>Für das Display im Rad: Lücken als [Name].</summary>
        public static string Outline(string text)
        {
            return Pattern.Replace(text ?? "", m => "[" + NameOf(m) + "]");
        }

        /// <summary>Text in Stücke zerlegt: Literal (gap == null) oder Lücke.</summary>
        public static IEnumerable<KeyValuePair<string, Gap>> Pieces(string text, List<Gap> gaps)
        {
            int at = 0;
            foreach (Match m in Pattern.Matches(text ?? ""))
            {
                if (m.Index > at) yield return new KeyValuePair<string, Gap>(text.Substring(at, m.Index - at), null);
                string key = KeyOf(m);
                yield return new KeyValuePair<string, Gap>(null, gaps.FirstOrDefault(x => x.Key == key));
                at = m.Index + m.Length;
            }
            if (at < (text ?? "").Length) yield return new KeyValuePair<string, Gap>(text.Substring(at), null);
        }

        /// <summary>„A", „A und B", „A, B und C".</summary>
        public static string JoinList(IList<string> items)
        {
            if (items.Count == 0) return "";
            if (items.Count == 1) return items[0];
            return string.Join(", ", items.Take(items.Count - 1)) + " und " + items[items.Count - 1];
        }
    }

    /// <summary>
    /// Abwechslung, damit Normalbefunde nicht in jeder Karteikarte denselben Wortlaut tragen:
    ///   Text A {oder} Text B          ganze Varianten – eine davon wird eingefügt, nie zweimal hintereinander dieselbe
    ///   {~stabil | unverändert}       eine der Formulierungen, mitten im Satz
    /// Aufgelöst wird vor den Lücken, damit die Abfrage schon die gewählte Fassung zeigt.
    /// </summary>
    public static class Variants
    {
        static readonly Regex Separator = new Regex(@"\s*\{oder\}\s*", RegexOptions.IgnoreCase);
        static readonly Regex Inline = new Regex(@"\{~([^{}]*)\}");
        static readonly Random Rng = new Random();
        // Zuletzt gewählte Variante je Text (nicht je Segment, damit es auch nach dem Neuladen der Konfiguration gilt).
        static readonly Dictionary<string, int> Last = new Dictionary<string, int>();

        /// <summary>Die ganzen Varianten eines Textes; ohne {oder} nur der Text selbst. Nie leer.</summary>
        public static List<string> Of(string text)
        {
            text = text ?? "";
            if (!Separator.IsMatch(text)) return new List<string> { text };
            var list = Separator.Split(text).Where(v => v.Trim().Length > 0).ToList();
            return list.Count > 0 ? list : new List<string> { "" };
        }

        /// <summary>Wählt eine Variante und löst die Wechselformulierungen darin auf.</summary>
        public static string Resolve(string text)
        {
            text = text ?? "";
            var list = Of(text);
            int i = 0;
            if (list.Count > 1)
            {
                int last;
                bool had = Last.TryGetValue(text, out last) && last < list.Count;
                i = Rng.Next(had ? list.Count - 1 : list.Count);
                if (had && i >= last) i++;
                Last[text] = i;
            }
            return Inline.Replace(list[i], m =>
            {
                var o = OptionsOf(m);
                return o.Count == 0 ? "" : o[Rng.Next(o.Count)];
            });
        }

        static List<string> OptionsOf(Match m)
        {
            return m.Groups[1].Value.Split('|').Select(s => s.Trim()).Where(s => s.Length > 0).ToList();
        }

        /// <summary>Für das Display im Rad: Wechselformulierungen als „stabil / unverändert".</summary>
        public static string Outline(string text)
        {
            return Inline.Replace(text ?? "", m => string.Join(" / ", OptionsOf(m)));
        }
    }

    /// <summary>
    /// Fragt die Lücken eines Textes ab. Anders als das Rad nimmt dieses Fenster den Fokus – getippt wird ja hinein.
    /// Danach geht der Fokus an das Programm zurück, das vorher vorn war, und erst dann wird eingefügt.
    /// </summary>
    public sealed class FillWindow
    {
        // Gerätefarben wie in WheelView – das Fenster ist ein Teil des Rads, kein Dialog des Arbeitsblatts.
        const string Styles = @"
<ResourceDictionary xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
  <SolidColorBrush x:Key='Chassis' Color='#15161A'/>
  <SolidColorBrush x:Key='Rim' Color='#2E3036'/>
  <SolidColorBrush x:Key='Hub' Color='#0A0B0D'/>
  <SolidColorBrush x:Key='HubRim' Color='#2A2C32'/>
  <SolidColorBrush x:Key='KeyCap' Color='#26282E'/>
  <SolidColorBrush x:Key='KeyHot' Color='#343841'/>
  <SolidColorBrush x:Key='Text1' Color='#EDEEF0'/>
  <SolidColorBrush x:Key='Text2' Color='#A4A9B1'/>
  <SolidColorBrush x:Key='Text3' Color='#7B808A'/>
  <SolidColorBrush x:Key='FieldHover' Color='#4A4E57'/>
  <SolidColorBrush x:Key='Hot' Color='#FF8A3D'/>
  <SolidColorBrush x:Key='Ink' Color='#15161A'/>

  <Style x:Key='Ring'>
    <Setter Property='Control.Template'>
      <Setter.Value>
        <ControlTemplate>
          <Rectangle Margin='-3' RadiusX='9' RadiusY='9' Stroke='{DynamicResource Text1}' StrokeThickness='2' SnapsToDevicePixels='True'/>
        </ControlTemplate>
      </Setter.Value>
    </Setter>
  </Style>

  <Style x:Key='Label' TargetType='TextBlock'>
    <Setter Property='FontSize' Value='12'/>
    <Setter Property='FontWeight' Value='SemiBold'/>
    <Setter Property='Foreground' Value='{DynamicResource Text2}'/>
    <Setter Property='Margin' Value='0,18,0,6'/>
  </Style>

  <Style x:Key='GapBox' TargetType='TextBox'>
    <Setter Property='FontSize' Value='13'/>
    <Setter Property='Foreground' Value='{DynamicResource Text1}'/>
    <Setter Property='CaretBrush' Value='{DynamicResource Text1}'/>
    <Setter Property='SelectionBrush' Value='{DynamicResource Hot}'/>
    <Setter Property='SelectionOpacity' Value='0.45'/>
    <Setter Property='Height' Value='34'/>
    <Setter Property='Padding' Value='7,0'/>
    <Setter Property='VerticalContentAlignment' Value='Center'/>
    <Setter Property='FocusVisualStyle' Value='{x:Null}'/>
    <Setter Property='Template'>
      <Setter.Value>
        <ControlTemplate TargetType='TextBox'>
          <Border x:Name='Frame' Background='{DynamicResource KeyCap}' BorderBrush='{DynamicResource Rim}' BorderThickness='1' CornerRadius='6'>
            <ScrollViewer x:Name='PART_ContentHost' Margin='{TemplateBinding Padding}' VerticalAlignment='Center' Focusable='False'
                          HorizontalScrollBarVisibility='Hidden' VerticalScrollBarVisibility='Hidden'/>
          </Border>
          <ControlTemplate.Triggers>
            <Trigger Property='IsMouseOver' Value='True'>
              <Setter TargetName='Frame' Property='BorderBrush' Value='{DynamicResource FieldHover}'/>
            </Trigger>
            <Trigger Property='IsKeyboardFocused' Value='True'>
              <Setter TargetName='Frame' Property='BorderBrush' Value='{DynamicResource Hot}'/>
            </Trigger>
          </ControlTemplate.Triggers>
        </ControlTemplate>
      </Setter.Value>
    </Setter>
  </Style>

  <Style x:Key='Option' TargetType='CheckBox'>
    <Setter Property='FontSize' Value='13'/>
    <Setter Property='Foreground' Value='{DynamicResource Text1}'/>
    <Setter Property='FocusVisualStyle' Value='{x:Null}'/>
    <Setter Property='Cursor' Value='Hand'/>
    <Setter Property='Template'>
      <Setter.Value>
        <ControlTemplate TargetType='CheckBox'>
          <Border x:Name='Row' Background='Transparent' BorderBrush='Transparent' BorderThickness='1' CornerRadius='6' Height='32' Padding='8,0'>
            <StackPanel Orientation='Horizontal'>
              <Border x:Name='Box' Width='18' Height='18' CornerRadius='4' BorderThickness='1.5' BorderBrush='{DynamicResource Text2}'
                      Background='Transparent' VerticalAlignment='Center' SnapsToDevicePixels='True'>
                <Path x:Name='Tick' Data='M3.5,7.8 L6.4,10.6 L11.6,4.6' Stroke='{DynamicResource Ink}' StrokeThickness='2'
                      StrokeStartLineCap='Round' StrokeEndLineCap='Round' StrokeLineJoin='Round' Visibility='Collapsed'/>
              </Border>
              <ContentPresenter Margin='10,0,0,0' VerticalAlignment='Center' RecognizesAccessKey='False'/>
            </StackPanel>
          </Border>
          <ControlTemplate.Triggers>
            <!-- Das Fenster öffnet am Zeiger, die Maus steht also oft zufällig über einer Option: Überfahren bleibt
                 deshalb ein Hauch, damit nur der Tastaturfokus klar hervortritt. -->
            <Trigger Property='IsMouseOver' Value='True'>
              <Setter TargetName='Row' Property='Background' Value='#0DFFFFFF'/>
            </Trigger>
            <Trigger Property='IsKeyboardFocused' Value='True'>
              <Setter TargetName='Row' Property='Background' Value='{DynamicResource KeyCap}'/>
              <Setter TargetName='Row' Property='BorderBrush' Value='{DynamicResource Hot}'/>
            </Trigger>
            <Trigger Property='IsChecked' Value='True'>
              <Setter TargetName='Box' Property='Background' Value='{DynamicResource Hot}'/>
              <Setter TargetName='Box' Property='BorderBrush' Value='{DynamicResource Hot}'/>
              <Setter TargetName='Tick' Property='Visibility' Value='Visible'/>
            </Trigger>
          </ControlTemplate.Triggers>
        </ControlTemplate>
      </Setter.Value>
    </Setter>
  </Style>

  <!-- Entweder-oder: dieselbe Zeile wie die Option, mit Ring statt Kästchen -->
  <Style x:Key='Pick' TargetType='RadioButton'>
    <Setter Property='FontSize' Value='13'/>
    <Setter Property='Foreground' Value='{DynamicResource Text1}'/>
    <Setter Property='FocusVisualStyle' Value='{x:Null}'/>
    <Setter Property='Cursor' Value='Hand'/>
    <Setter Property='Template'>
      <Setter.Value>
        <ControlTemplate TargetType='RadioButton'>
          <Border x:Name='Row' Background='Transparent' BorderBrush='Transparent' BorderThickness='1' CornerRadius='6' Height='32' Padding='8,0'>
            <StackPanel Orientation='Horizontal'>
              <Grid Width='18' Height='18' VerticalAlignment='Center' SnapsToDevicePixels='True'>
                <Ellipse x:Name='Ring' Stroke='{DynamicResource Text2}' StrokeThickness='1.5' Fill='Transparent'/>
                <Ellipse x:Name='Dot' Width='7' Height='7' Fill='{DynamicResource Ink}' Visibility='Collapsed'/>
              </Grid>
              <ContentPresenter Margin='10,0,0,0' VerticalAlignment='Center' RecognizesAccessKey='False'/>
            </StackPanel>
          </Border>
          <ControlTemplate.Triggers>
            <Trigger Property='IsMouseOver' Value='True'>
              <Setter TargetName='Row' Property='Background' Value='#0DFFFFFF'/>
            </Trigger>
            <Trigger Property='IsKeyboardFocused' Value='True'>
              <Setter TargetName='Row' Property='Background' Value='{DynamicResource KeyCap}'/>
              <Setter TargetName='Row' Property='BorderBrush' Value='{DynamicResource Hot}'/>
            </Trigger>
            <Trigger Property='IsChecked' Value='True'>
              <Setter TargetName='Ring' Property='Fill' Value='{DynamicResource Hot}'/>
              <Setter TargetName='Ring' Property='Stroke' Value='{DynamicResource Hot}'/>
              <Setter TargetName='Dot' Property='Visibility' Value='Visible'/>
            </Trigger>
          </ControlTemplate.Triggers>
        </ControlTemplate>
      </Setter.Value>
    </Setter>
  </Style>

  <Style x:Key='Primary' TargetType='Button'>
    <Setter Property='FontSize' Value='13'/>
    <Setter Property='FontWeight' Value='SemiBold'/>
    <Setter Property='Foreground' Value='{DynamicResource Ink}'/>
    <Setter Property='Background' Value='{DynamicResource Hot}'/>
    <Setter Property='Height' Value='34'/>
    <Setter Property='Padding' Value='16,0'/>
    <Setter Property='Cursor' Value='Hand'/>
    <Setter Property='FocusVisualStyle' Value='{StaticResource Ring}'/>
    <Setter Property='Template'>
      <Setter.Value>
        <ControlTemplate TargetType='Button'>
          <Grid>
            <Border Background='{TemplateBinding Background}' CornerRadius='6'/>
            <Border x:Name='Veil' Background='Transparent' CornerRadius='6'/>
            <ContentPresenter Margin='{TemplateBinding Padding}' HorizontalAlignment='Center' VerticalAlignment='Center' RecognizesAccessKey='False'/>
          </Grid>
          <ControlTemplate.Triggers>
            <Trigger Property='IsMouseOver' Value='True'>
              <Setter TargetName='Veil' Property='Background' Value='#24FFFFFF'/>
            </Trigger>
            <Trigger Property='IsPressed' Value='True'>
              <Setter TargetName='Veil' Property='Background' Value='#26000000'/>
            </Trigger>
          </ControlTemplate.Triggers>
        </ControlTemplate>
      </Setter.Value>
    </Setter>
  </Style>

  <Style x:Key='Ghost' TargetType='Button' BasedOn='{StaticResource Primary}'>
    <Setter Property='FontWeight' Value='Normal'/>
    <Setter Property='Foreground' Value='{DynamicResource Text1}'/>
    <Setter Property='Background' Value='Transparent'/>
    <Setter Property='Padding' Value='12,0'/>
    <Setter Property='Template'>
      <Setter.Value>
        <ControlTemplate TargetType='Button'>
          <Border x:Name='Face' Background='Transparent' CornerRadius='6'>
            <ContentPresenter Margin='{TemplateBinding Padding}' HorizontalAlignment='Center' VerticalAlignment='Center' RecognizesAccessKey='False'/>
          </Border>
          <ControlTemplate.Triggers>
            <Trigger Property='IsMouseOver' Value='True'>
              <Setter TargetName='Face' Property='Background' Value='{DynamicResource KeyCap}'/>
            </Trigger>
            <Trigger Property='IsPressed' Value='True'>
              <Setter TargetName='Face' Property='Background' Value='{DynamicResource KeyHot}'/>
            </Trigger>
          </ControlTemplate.Triggers>
        </ControlTemplate>
      </Setter.Value>
    </Setter>
  </Style>

  <Style x:Key='Cap' TargetType='Border'>
    <Setter Property='Background' Value='{DynamicResource KeyCap}'/>
    <Setter Property='BorderBrush' Value='{DynamicResource Rim}'/>
    <Setter Property='BorderThickness' Value='1'/>
    <Setter Property='CornerRadius' Value='4'/>
    <Setter Property='Padding' Value='6,1,6,2'/>
    <Setter Property='VerticalAlignment' Value='Center'/>
  </Style>

  <Style x:Key='Thumb' TargetType='Thumb'>
    <Setter Property='Template'>
      <Setter.Value>
        <ControlTemplate TargetType='Thumb'>
          <Border x:Name='T' Background='#73A4A9B1' CornerRadius='3' Margin='2,0'/>
          <ControlTemplate.Triggers>
            <Trigger Property='IsMouseOver' Value='True'><Setter TargetName='T' Property='Background' Value='#A6A4A9B1'/></Trigger>
            <Trigger Property='IsDragging' Value='True'><Setter TargetName='T' Property='Background' Value='#CCA4A9B1'/></Trigger>
          </ControlTemplate.Triggers>
        </ControlTemplate>
      </Setter.Value>
    </Setter>
  </Style>
  <Style TargetType='ScrollBar'>
    <Setter Property='Width' Value='10'/>
    <Setter Property='MinWidth' Value='10'/>
    <Setter Property='Template'>
      <Setter.Value>
        <ControlTemplate TargetType='ScrollBar'>
          <Track x:Name='PART_Track' IsDirectionReversed='True'>
            <Track.Thumb><Thumb Style='{StaticResource Thumb}'/></Track.Thumb>
          </Track>
        </ControlTemplate>
      </Setter.Value>
    </Setter>
  </Style>
</ResourceDictionary>";

        readonly string _text;
        readonly List<Gaps.Gap> _gaps;
        readonly Window _win;
        readonly TextBlock _preview;
        readonly ScrollViewer _previewScroll;
        readonly List<Control> _firsts = new List<Control>();     // erstes Bedienelement je Lücke
        readonly Button _ok;
        readonly TaskCompletionSource<bool> _done = new TaskCompletionSource<bool>();
        readonly Brush _hot, _hotText, _text1, _text2, _text3;
        Gaps.Gap _active;
        bool _finished;

        /// <summary>
        /// Zeigt das Fenster am Zeiger und wartet. Ergebnis ist der Text mit gefüllten Lücken, null bei Abbruch.
        /// </summary>
        public static async Task<string> Ask(string text, string title, Color hot, Skin skin)
        {
            IntPtr target = Native.GetForegroundWindow();
            var f = new FillWindow(text, title, hot, skin);
            bool ok = await f.Run();
            string result = ok ? Gaps.Fill(text, f._gaps) : null;
            await GiveBack(target);
            return result;
        }

        FillWindow(string text, string title, Color hot, Skin skin)
        {
            _text = text;
            _gaps = Gaps.Parse(text);
            var res = (ResourceDictionary)XamlReader.Parse(Styles);
            // Gerätefarben des gewählten Aussehens. Milchglas wird hier deckend hell: Hinter einem Fenster, in das
            // getippt wird, soll nichts durchscheinen.
            var sk = skin == null ? Skin.Graphit : skin.Decor == Decor.Frost ? Skin.Hell : skin;
            res["Chassis"] = Solid(sk.Chassis); res["Rim"] = Solid(sk.Rim); res["Hub"] = Solid(sk.Hub); res["HubRim"] = Solid(sk.HubRim);
            res["KeyCap"] = Solid(sk.Key); res["KeyHot"] = Solid(sk.KeyHot); res["Text1"] = Solid(sk.Text); res["Text2"] = Solid(sk.Text2);
            res["Text3"] = Solid(sk.Text3); res["FieldHover"] = Solid(sk.FieldHover); res["Ink"] = Solid(sk.Ink);
            _hot = Solid(hot);
            _hotText = Solid(sk.Tint(hot));
            res["Hot"] = _hot;
            _text1 = (Brush)res["Text1"];
            _text2 = (Brush)res["Text2"];
            _text3 = (Brush)res["Text3"];

            var root = new DockPanel { Margin = new Thickness(20, 18, 20, 20) };

            var head = new TextBlock
            {
                Text = string.IsNullOrWhiteSpace(title) ? "Lücken ausfüllen" : title.Trim(),
                FontSize = 15, FontWeight = FontWeights.SemiBold, Foreground = _text1, TextTrimming = TextTrimming.CharacterEllipsis
            };
            DockPanel.SetDock(head, Dock.Top);
            root.Children.Add(head);

            // Das Display: zeigt den Text, wie er gleich im Feld steht. Die Lücke, an der du gerade bist, leuchtet.
            _preview = new TextBlock { FontSize = 13, LineHeight = 20, TextWrapping = TextWrapping.Wrap, Foreground = _text2 };
            _previewScroll = new ScrollViewer
            {
                Content = _preview, MaxHeight = 140, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Focusable = false
            };
            var display = new Border
            {
                Background = (Brush)res["Hub"], BorderBrush = (Brush)res["HubRim"], BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8), Padding = new Thickness(14, 11, 10, 12), Margin = new Thickness(0, 12, 0, 0),
                Child = _previewScroll
            };
            DockPanel.SetDock(display, Dock.Top);
            root.Children.Add(display);

            // Fußleiste: Tastenhinweise links, Schaltflächen rechts.
            var foot = new DockPanel { Margin = new Thickness(0, 20, 0, 0) };
            DockPanel.SetDock(foot, Dock.Bottom);
            var buttons = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Bottom };
            var cancel = new Button { Content = "Abbrechen", Style = (Style)res["Ghost"], ToolTip = "Esc" };
            _ok = new Button { Content = "Einfügen", Style = (Style)res["Primary"], Margin = new Thickness(8, 0, 0, 0), ToolTip = "Strg + Enter" };
            cancel.Click += (s, e) => Finish(false);
            _ok.Click += (s, e) => Finish(true);
            buttons.Children.Add(cancel);
            buttons.Children.Add(_ok);
            DockPanel.SetDock(buttons, Dock.Right);
            foot.Children.Add(buttons);
            var hints = new WrapPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 12, 0) };
            Hint(hints, res, "Tab", "weiter");
            if (_gaps.Any(g => g.IsChoice && !g.Single)) Hint(hints, res, "Leertaste", "an / aus");
            if (_gaps.Any(g => g.Single)) Hint(hints, res, "Pfeiltasten", "wählen");
            Hint(hints, res, "Enter", _gaps.Count > 1 ? "nächste Lücke" : "einfügen");
            foot.Children.Add(hints);
            root.Children.Add(foot);

            // Die Lücken
            var fields = new StackPanel { Margin = new Thickness(0, 0, 0, 0) };
            foreach (var gap in _gaps)
            {
                var g = gap;
                fields.Children.Add(new TextBlock { Text = g.Name, Style = (Style)res["Label"] });
                if (!g.IsChoice)
                {
                    var box = new TextBox { Style = (Style)res["GapBox"] };
                    box.TextChanged += (s, e) => { g.Value = box.Text; Render(); };
                    box.GotKeyboardFocus += (s, e) => { box.SelectAll(); Activate(g); };
                    box.Tag = g;
                    fields.Children.Add(box);
                    _firsts.Add(box);
                }
                else if (g.Single)
                {
                    // Tab springt einmal in die Gruppe – auf die gewählte Option, sonst die erste –, die Pfeile wählen darin.
                    var list = new StackPanel { Margin = new Thickness(-8, -2, 0, 0) };
                    KeyboardNavigation.SetTabNavigation(list, KeyboardNavigationMode.Once);
                    for (int i = 0; i < g.Options.Count; i++)
                    {
                        int n = i;
                        var rb = new RadioButton { Content = g.Options[i], Style = (Style)res["Pick"], Tag = g, GroupName = g.Key };
                        rb.Checked += (s, e) => { for (int k = 0; k < g.Picked.Length; k++) g.Picked[k] = k == n; Render(); };
                        rb.GotKeyboardFocus += (s, e) => Activate(g);
                        list.Children.Add(rb);
                        if (i == 0) _firsts.Add(rb);
                    }
                    fields.Children.Add(list);
                }
                else
                {
                    var list = new StackPanel { Margin = new Thickness(-8, -2, 0, 0) };   // Kästchen fluchten mit der Beschriftung
                    for (int i = 0; i < g.Options.Count; i++)
                    {
                        int n = i;
                        var cb = new CheckBox { Content = g.Options[i], Style = (Style)res["Option"], Tag = g };
                        cb.Checked += (s, e) => { g.Picked[n] = true; Render(); };
                        cb.Unchecked += (s, e) => { g.Picked[n] = false; Render(); };
                        cb.GotKeyboardFocus += (s, e) => Activate(g);
                        list.Children.Add(cb);
                        if (i == 0) _firsts.Add(cb);
                    }
                    fields.Children.Add(list);
                }
            }
            var scroll = new ScrollViewer
            {
                Content = fields, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Focusable = false,
                Padding = new Thickness(0, 0, 4, 2)
            };
            root.Children.Add(scroll);

            var chassis = new Border
            {
                Background = (Brush)res["Chassis"], BorderBrush = (Brush)res["Rim"], BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10), Width = 480, Margin = new Thickness(28, 18, 28, 38), Child = root,
                // Wie unter dem Rad: das Gerät liegt auf dem Bildschirm und wirft einen weichen Schatten nach unten.
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = Colors.Black, Direction = 270, ShadowDepth = 10, BlurRadius = 28, Opacity = 0.5
                }
            };
            KeyboardNavigation.SetTabNavigation(chassis, KeyboardNavigationMode.Cycle);

            _win = new Window
            {
                Title = "Kyklos – " + head.Text,
                WindowStyle = WindowStyle.None, AllowsTransparency = true, Background = Brushes.Transparent,
                ResizeMode = ResizeMode.NoResize, ShowInTaskbar = false, Topmost = true,
                SizeToContent = SizeToContent.WidthAndHeight, WindowStartupLocation = WindowStartupLocation.Manual,
                Left = -32000, Top = -32000, FontFamily = new FontFamily("Segoe UI"),
                Content = chassis
            };
            // Am Fenster, nicht am Inhalt: Auch der Fokusring auf der Schmuckebene muss die Farben finden.
            _win.Resources = res;
            TextOptions.SetTextFormattingMode(_win, TextFormattingMode.Display);
            _win.PreviewKeyDown += OnKey;
            _win.Closed += (s, e) => Finish(false);
            _win.MouseLeftButtonDown += (s, e) =>
            {
                // Das Fenster hat keine Titelleiste: an freien Stellen lässt es sich verschieben.
                if (e.OriginalSource == chassis || e.OriginalSource == root || e.OriginalSource == head) try { _win.DragMove(); } catch (InvalidOperationException) { }
            };
            Render();
        }

        static void Hint(Panel host, ResourceDictionary res, string key, string what)
        {
            var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 3, 14, 3) };
            row.Children.Add(new Border
            {
                Style = (Style)res["Cap"],
                Child = new TextBlock { Text = key, FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = (Brush)res["Text1"] }
            });
            row.Children.Add(new TextBlock
            {
                Text = what, FontSize = 12, Foreground = (Brush)res["Text2"], Margin = new Thickness(6, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center
            });
            host.Children.Add(row);
        }

        Task<bool> Run()
        {
            _win.SourceInitialized += (s, e) => Place();
            _win.Show();
            Place();    // ein zweites Mal: Landet das Fenster auf einem Monitor mit anderer Skalierung, ändert sich seine Größe
            bool motion = SystemParameters.ClientAreaAnimation;
            if (motion)
            {
                var root = (UIElement)_win.Content;
                root.Opacity = 0;
                root.BeginAnimation(UIElement.OpacityProperty,
                    new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(130)) { EasingFunction = new ExponentialEase { EasingMode = EasingMode.EaseOut, Exponent = 5 } });
            }
            TakeFocus();
            if (_firsts.Count > 0) { _firsts[0].Focus(); Keyboard.Focus(_firsts[0]); }
            return _done.Task;
        }

        /// <summary>Mittig über dem Zeiger, ganz auf dem Bildschirm.</summary>
        void Place()
        {
            IntPtr hwnd = new WindowInteropHelper(_win).Handle;
            if (hwnd == IntPtr.Zero) return;
            Native.POINT p;
            Native.GetCursorPos(out p);
            var mi = new Native.MONITORINFO { cbSize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(Native.MONITORINFO)) };
            Native.GetMonitorInfo(Native.MonitorFromPoint(p, 2), ref mi);
            var wa = mi.rcWork;
            for (int n = 0; n < 2; n++)
            {
                Native.RECT r;
                Native.GetWindowRect(hwnd, out r);
                int w = r.right - r.left, h = r.bottom - r.top;
                if (h > wa.bottom - wa.top)
                {
                    // Sehr viele Lücken: Höhe auf den Bildschirm begrenzen, die Liste rollt.
                    double k = h / Math.Max(1.0, _win.ActualHeight);
                    _win.SizeToContent = SizeToContent.Width;
                    _win.Height = (wa.bottom - wa.top) / k;
                    h = wa.bottom - wa.top;
                }
                int x = Math.Max(wa.left, Math.Min(wa.right - w, p.x - w / 2));
                int y = Math.Max(wa.top, Math.Min(wa.bottom - h, p.y - h / 2));
                Native.SetWindowPos(hwnd, Native.HWND_TOPMOST, x, y, 0, 0, Native.SWP_NOSIZE);
            }
        }

        /// <summary>
        /// Windows gibt den Vordergrund nur ungern an ein Programm, das gerade nicht vorn ist. Mit der Eingabe des
        /// vorderen Programms verbunden darf es das.
        /// </summary>
        void TakeFocus()
        {
            IntPtr hwnd = new WindowInteropHelper(_win).Handle;
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
            _win.Activate();
        }

        static async Task GiveBack(IntPtr target)
        {
            if (target == IntPtr.Zero) return;
            Native.SetForegroundWindow(target);
            // Erst einfügen, wenn das Zielprogramm wirklich wieder vorn ist und seinen Fokus zurückhat.
            for (int n = 0; n < 30 && Native.GetForegroundWindow() != target; n++) await Task.Delay(20);
            await Task.Delay(60);
        }

        void OnKey(object sender, KeyEventArgs e)
        {
            var focused = Keyboard.FocusedElement as Control;
            if (e.Key == Key.Escape) { e.Handled = true; Finish(false); return; }
            if (e.Key == Key.Enter || e.Key == Key.Return)
            {
                e.Handled = true;
                if (focused == _ok || (Keyboard.Modifiers & ModifierKeys.Control) != 0) { Finish(true); return; }
                // Enter springt zur nächsten Lücke; in der letzten fügt es ein.
                var gap = focused == null ? null : focused.Tag as Gaps.Gap;
                int at = gap == null ? -1 : _gaps.IndexOf(gap);
                if (at >= 0 && at + 1 < _firsts.Count) _firsts[at + 1].Focus();
                else Finish(true);
                return;
            }
            var rb = focused as RadioButton;
            if (rb != null && (e.Key == Key.Down || e.Key == Key.Up || e.Key == Key.Left || e.Key == Key.Right))
            {
                // Entweder-oder: Die Pfeile wandern innerhalb der Gruppe und wählen dabei, wie unter Windows gewohnt.
                e.Handled = true;
                var group = (Panel)rb.Parent;
                int at = group.Children.IndexOf(rb) + (e.Key == Key.Down || e.Key == Key.Right ? 1 : -1);
                var to = at >= 0 && at < group.Children.Count ? (RadioButton)group.Children[at] : rb;
                to.Focus();
                to.IsChecked = true;
                return;
            }
            if (focused is CheckBox && (e.Key == Key.Down || e.Key == Key.Up))
            {
                e.Handled = true;
                focused.MoveFocus(new TraversalRequest(e.Key == Key.Down ? FocusNavigationDirection.Next : FocusNavigationDirection.Previous));
            }
        }

        void Activate(Gaps.Gap g)
        {
            if (_active == g) return;
            _active = g;
            Render();
        }

        void Render()
        {
            _preview.Inlines.Clear();
            Run activeRun = null;
            foreach (var piece in Gaps.Pieces(ActionRunner.ExpandForPreview(_text), _gaps))
            {
                if (piece.Value == null)
                {
                    _preview.Inlines.Add(new Run(piece.Key));
                    continue;
                }
                var g = piece.Value;
                string v = g.Result();
                bool hot = g == _active;
                var run = new Run(v.Length > 0 ? v : "[" + g.Name + "]")
                {
                    Foreground = hot ? _hotText : v.Length > 0 ? _text1 : _text3
                };
                if (hot) { run.TextDecorations = TextDecorations.Underline; if (activeRun == null) activeRun = run; }
                _preview.Inlines.Add(run);
            }
            // Lange Texte: die aktive Lücke in den sichtbaren Bereich holen.
            if (activeRun != null && _previewScroll.IsLoaded)
            {
                var r = activeRun;
                _preview.Dispatcher.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        Rect b = r.ContentStart.GetCharacterRect(LogicalDirection.Forward);
                        if (b.Top < _previewScroll.VerticalOffset || b.Bottom > _previewScroll.VerticalOffset + _previewScroll.ViewportHeight)
                            _previewScroll.ScrollToVerticalOffset(Math.Max(0, b.Top - 20));
                    }
                    catch (InvalidOperationException) { }
                }), System.Windows.Threading.DispatcherPriority.Loaded);
            }
        }

        void Finish(bool ok)
        {
            if (_finished) return;
            _finished = true;
            if (_win.IsVisible) _win.Close();
            _done.TrySetResult(ok);
        }

        static SolidColorBrush Solid(Color c)
        {
            var b = new SolidColorBrush(c);
            b.Freeze();
            return b;
        }

        // ---------------------------------------------------------------- Sichtprüfung (--dev-render)

        /// <summary>Baut das Fenster mit Beispielwerten auf, ohne es zu zeigen – für Dev.Render.</summary>
        public static Window DevBuild(string text, string title, Color hot, Action<List<Gaps.Gap>> fill, int focusGap, Skin skin = null)
        {
            var f = new FillWindow(text, title, hot, skin);
            if (fill != null) fill(f._gaps);
            var chassis = (Border)f._win.Content;
            var fields = (StackPanel)((ScrollViewer)((DockPanel)chassis.Child).Children[3]).Content;
            foreach (var child in fields.Children)
            {
                var box = child as TextBox;
                if (box != null) box.Text = ((Gaps.Gap)box.Tag).Value;
                var list = child as StackPanel;
                if (list != null)
                    foreach (System.Windows.Controls.Primitives.ToggleButton cb in list.Children)
                    {
                        var g = (Gaps.Gap)cb.Tag;
                        if (g.Picked[g.Options.IndexOf((string)cb.Content)]) cb.IsChecked = true;
                        else if (!g.Single) cb.IsChecked = false;
                    }
            }
            if (focusGap >= 0 && focusGap < f._gaps.Count) f.Activate(f._gaps[focusGap]);
            return f._win;
        }
    }
}
