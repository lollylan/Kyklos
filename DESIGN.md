---
name: Kyklos
description: Ein Stream Deck, zum Drehrad gebogen – Auswahlrad am Mauszeiger für Windows
colors:
  # Gerät (src/WheelView.cs)
  CChassis: "#15161A"
  CKey: "#26282E"
  CKeyHot: "#343841"
  CKeyEmpty: "#1B1C20"
  BHub: "#0A0B0D"
  BBezel: "#1D1F24"
  BSeat: "#0C0D10"
  PRim: "#2E3036"
  PHubRim: "#2A2C32"
  CText: "#EDEEF0"
  CText2: "#A4A9B1"
  BText3: "#7B808A"
  PSelect: "#FFFFFF"
  # Tastenfarben (src/Model.cs, Palette)
  Palette-Orange: "#FF8A3D"
  Palette-Gelb: "#F5C542"
  Palette-Gruen: "#5DD28B"
  Palette-Tuerkis: "#45CFC6"
  Palette-Blau: "#62ABFF"
  Palette-Violett: "#AC94FF"
  Palette-Rot: "#FF7B7B"
  Palette-Hell: "#E8EAED"
  # Einstellungsfenster (src/Theme.xaml)
  Bg: "#F2F3F5"
  Rail: "#E7E9ED"
  Surface: "#FFFFFF"
  Line: "#D5D8DD"
  FieldLine: "#8D929B"
  Ink: "#15161A"
  Ink2: "#525760"
  Ink3: "#6A6F78"
  Accent: "#D9480F"
  AccentSoft: "#FFE9DC"
  AccentRing: "#D9480F59"
  HoverSurface: "#EEF0F2"
  PressSurface: "#E1E4E8"
  HoverRail: "#DBDEE3"
  InkHover: "#2C2F36"
  Danger: "#B42318"
typography:
  headline:
    fontFamily: "Segoe UI"
    fontSize: "22px"
    fontWeight: 600
  title:
    fontFamily: "Segoe UI"
    fontSize: "17px"
    fontWeight: 600
  section-title:
    fontFamily: "Segoe UI"
    fontSize: "15px"
    fontWeight: 600
  body:
    fontFamily: "Segoe UI"
    fontSize: "13px"
    fontWeight: 400
  label:
    fontFamily: "Segoe UI"
    fontSize: "12px"
    fontWeight: 600
  help:
    fontFamily: "Segoe UI"
    fontSize: "12px"
    fontWeight: 400
    lineHeight: "17px"
  hub-title:
    fontFamily: "Segoe UI"
    fontSize: "15px"
    fontWeight: 600
  hub-desc:
    fontFamily: "Segoe UI"
    fontSize: "13px"
    fontWeight: 400
  key-label:
    fontFamily: "Segoe UI"
    fontSize: "12.5px"
    fontWeight: 600
  key-label-solo:
    fontFamily: "Segoe UI"
    fontSize: "13.5px"
    fontWeight: 600
rounded:
  slider-track: "2px"
  scroll-thumb: "3px"
  list-item: "5px"
  control: "6px"
  key: "8px"
  popup: "8px"
  focus-ring: "9px"
  icon-popup: "10px"
  switch: "11px"
spacing:
  field-gap: "6px"
  control-gap: "8px"
  rail-pad: "12px"
  row-pad: "14px"
  field-top: "18px"
  panel-pad: "24px"
  stage-pad: "32px"
  page-pad: "40px"
components:
  wheel-key:
    backgroundColor: "{colors.CKey}"
    textColor: "{colors.CText}"
    typography: "{typography.key-label}"
    rounded: "{rounded.key}"
  wheel-key-lit:
    backgroundColor: "{colors.Palette-Orange}"
    textColor: "{colors.CChassis}"
    typography: "{typography.key-label}"
    rounded: "{rounded.key}"
  wheel-key-empty:
    backgroundColor: "{colors.CKeyEmpty}"
    textColor: "{colors.BText3}"
    rounded: "{rounded.key}"
  wheel-hub:
    backgroundColor: "{colors.BHub}"
    textColor: "{colors.CText}"
    typography: "{typography.hub-title}"
    size: "168px"
  Btn:
    backgroundColor: "{colors.Surface}"
    textColor: "{colors.Ink}"
    typography: "{typography.body}"
    rounded: "{rounded.control}"
    padding: "0 12px"
    height: "34px"
  Btn-hover:
    backgroundColor: "{colors.HoverSurface}"
  Btn-pressed:
    backgroundColor: "{colors.PressSurface}"
  BtnPrimary:
    backgroundColor: "{colors.Ink}"
    textColor: "{colors.Surface}"
    rounded: "{rounded.control}"
    padding: "0 16px"
    height: "34px"
  BtnPrimary-hover:
    backgroundColor: "{colors.InkHover}"
  BtnPrimary-pressed:
    backgroundColor: "#000000"
  BtnGhost:
    backgroundColor: "transparent"
    textColor: "{colors.Ink}"
    rounded: "{rounded.control}"
    padding: "0 10px"
    height: "34px"
  BtnIcon:
    backgroundColor: "transparent"
    textColor: "{colors.Ink}"
    rounded: "{rounded.control}"
    size: "32px"
  TextBox:
    backgroundColor: "{colors.Surface}"
    textColor: "{colors.Ink}"
    typography: "{typography.body}"
    rounded: "{rounded.control}"
    padding: "0 8px"
    height: "34px"
  TitleBox:
    backgroundColor: "transparent"
    textColor: "{colors.Ink}"
    typography: "{typography.headline}"
    rounded: "{rounded.control}"
    padding: "0 7px"
    height: "40px"
  RailItem:
    backgroundColor: "transparent"
    textColor: "{colors.Ink}"
    rounded: "{rounded.control}"
    padding: "8px 10px"
  RailItem-hover:
    backgroundColor: "{colors.HoverRail}"
  RailItem-selected:
    backgroundColor: "{colors.Surface}"
  Switch-off:
    backgroundColor: "{colors.FieldLine}"
    rounded: "{rounded.switch}"
    width: "40px"
    height: "22px"
  Switch-on:
    backgroundColor: "{colors.Accent}"
    rounded: "{rounded.switch}"
    width: "40px"
    height: "22px"
  Swatch:
    rounded: "50%"
    size: "30px"
  ToolTip:
    backgroundColor: "{colors.Ink}"
    textColor: "{colors.Surface}"
    rounded: "{rounded.control}"
    padding: "6px 9px"
---

# Design System: Kyklos

Plattform: natives Windows (C#/WPF, .NET Framework 4.8). Es gibt kein CSS. Die Tokens oben sind die Werte aus dem Code:
Gerätewerte stehen als Konstanten in `src/WheelView.cs`, die Tastenfarben in `src/Model.cs` (`Palette`), die Tokens des
Einstellungsfensters als Pinsel in `src/Theme.xaml`. Alle Maße sind geräteunabhängige Pixel (DIP) bei Skalierung 1.
`AccentRing` steht in XAML als `#59D9480F` (Alpha vorn); oben ist derselbe Wert in CSS-Schreibweise notiert.

## Overview

**Creative North Star: "Das gebogene Stream Deck"**

Das Rad ist ein Gerät, kein HUD. Eine opake, matte Graphit-Scheibe liegt unter dem Mauszeiger, mit echten Tastenkappen in
gleich breiten, dunklen Fugen und einem eingelassenen Display in der Nabe. Es erscheint, während eine Taste gehalten wird,
nimmt dem Zielprogramm nie den Fokus und verschwindet beim Loslassen. Deshalb ist es dicht und ruhig: nichts daran
bewegt sich, außer der Taste unter dem Zeiger und der Richtungsmarke.

Farbe ist Funktion. Jede Taste trägt eine von acht Farben. Im Ruhezustand färbt sie nur das oberste Element der Taste
(das Symbol, sonst die Beschriftung); unter dem Zeiger flutet sie die ganze Taste, und Schrift und Symbol kippen auf
Graphit. Das Display in der Mitte zeigt vor dem Loslassen, was passieren wird.

Das Einstellungsfenster ist die Gegenseite: ein helles, kühles Arbeitsblatt, auf dem das dunkle Gerät liegt und angeklickt
wird. Graphit ist dort Schrift und Hauptaktion, ein dunkles Orange zeigt ausschließlich Zustand (Fokus, eingeschaltet,
Hinweis). Die Oberfläche spricht Deutsch und duzt.

**Key Characteristics:**
- Opakes Graphit-Gerät mit Tasten in konstant 5 px breiten Fugen; kein Glühen, keine Transparenz am Gerät.
- Acht Tastenfarben: in Ruhe nur am Symbol, unter dem Zeiger auf der ganzen Taste.
- Display in der Nabe (Ø 168) mit Titel und Vorschau des Ergebnisses.
- Richtungsmarke in der Fuge zwischen Nabe und Tasten folgt dem Zeiger stufenlos.
- Einziger Verlauf im ganzen System ist der zweischichtige Schlagschatten unter dem Gerät.
- Helles Arbeitsblatt mit drei Spalten (Radliste, Gerät, Inspektor) für die Einstellungen.
- Segoe UI in zwei Schnitten (Normal, Semibold), kleine und enge Größenstaffel.

## Colors

Zwei Welten, die sich eine Farbe teilen: Graphit (`Ink` = `CChassis`) ist im Einstellungsfenster die Schrift und auf dem
Bildschirm das Gerät.

### Primary
- **Graphit** (`Ink`, `CChassis`): Chassis des Rads, Schrift auf beleuchteten Tasten, Schrift und Hauptaktion
  („Aktion testen") im Einstellungsfenster, Tooltip-Fläche, Schieberegler-Füllung, Auswahlring der Farbfelder.

### Secondary
- **Zustands-Orange** (`Accent`): nur im Einstellungsfenster und nur für Zustand – Fokusring (2 px), Rahmen des
  fokussierten Felds, eingeschalteter Schalter, Textauswahl (28 % Deckkraft).
- **Zustands-Orange, weich** (`AccentSoft`): gewählter Eintrag in Auswahllisten und die Hinweisfläche für
  Beispieltexte.
- **Fokus-Hof** (`AccentRing`): 3 px breiter, durchscheinender Ring um das fokussierte Textfeld.

### Tertiary
Die acht Tastenfarben (`Palette` in `src/Model.cs`). Alle sind hell genug, dass Graphit-Schrift darauf mindestens 4,5:1
erreicht. Standard ist Orange.
- **Orange** (`Palette-Orange`), **Gelb** (`Palette-Gelb`), **Grün** (`Palette-Gruen`), **Türkis** (`Palette-Tuerkis`),
  **Blau** (`Palette-Blau`), **Violett** (`Palette-Violett`), **Rot** (`Palette-Rot`), **Hell** (`Palette-Hell`).
- Dieselbe Farbe färbt die Richtungsmarke, solange der Zeiger auf einer belegten Taste steht.

### Neutral
Gerät:
- **Tastenkappe** (`CKey`): belegte Taste in Ruhe. **Tastenkappe, überfahren** (`CKeyHot`): nur in der Vorschau der
  Einstellungen. **Leere Taste** (`CKeyEmpty`): kaum heller als das Chassis, liest sich als Blindkappe.
- **Display** (`BHub`), **Blende** (`BBezel`, 4 px breit, mit Haarlinie `PHubRim`), **Chassisrand** (`PRim`, 1 px).
- **Mulde** (`BSeat`): bleibt stehen, wo eine Taste sich hebt.
- **Geräteschrift** (`CText`), **Geräteschrift, zweite Ebene** (`CText2`: Vorschau im Display, Unterrad-Winkel,
  Richtungsmarke über leeren Tasten), **Geräteschrift, dritte Ebene** (`BText3`: Plus auf leeren Tasten).
- **Auswahlkontur** (`PSelect`): 2 px Weiß um die gewählte Taste, nur in den Einstellungen.

Einstellungsfenster:
- **Arbeitsblatt** (`Bg`), **Seitenleiste** (`Rail`), **Fläche** (`Surface`: Inspektor, Felder, Schaltflächen, gewählter
  Eintrag der Seitenleiste).
- **Trennlinie** (`Line`): Spaltengrenzen, Zeilenlinien, Popup-Rahmen. **Feldrahmen** (`FieldLine`): Rahmen von Feldern
  und Schaltflächen, Schalter aus, Schieberegler-Schiene – dunkler als die Trennlinie, damit Bedienelemente als solche
  erkennbar sind.
- **Schrift, zweite Ebene** (`Ink2`: Beschriftungen, Hilfetexte), **Platzhalter** (`Ink3`).
- **Zustandsflächen**: `HoverSurface`, `PressSurface` (Schaltflächen), `HoverRail` (Seitenleiste), `InkHover`
  (Hauptaktion überfahren).
- **Warnung** (`Danger`): Text für Konflikte beim Auslöser.

### Named Rules
**Die Farbflut-Regel.** In Ruhe trägt nur das oberste Element einer Taste ihre Farbe (Symbol, sonst Beschriftung); die
Beschriftung unter einem Symbol bleibt `CText`. Unter dem Zeiger füllt die Farbe die ganze Taste, und alles darauf wird
Graphit. Eigene Bilder behalten ihre Farben.

**Die Zustands-Orange-Regel.** `Accent` zeigt im Einstellungsfenster nur Zustand: Fokus, eingeschaltet, gewählt, Hinweis.
Die Hauptaktion ist Graphit, nie Orange.

**Die Zwei-Orange-Regel.** `Palette-Orange` gehört auf das dunkle Gerät, `Accent` auf das helle Arbeitsblatt. Sie werden
nicht getauscht: Das helle Orange trägt auf Weiß keine Schrift, das dunkle leuchtet auf Graphit nicht.

## Typography

**Display Font:** keine eigene Auszeichnungsschrift
**Body Font:** Segoe UI (Systemschrift von Windows, bewusst gewählt: kein Netzwerk, keine mitgelieferten Schriften)
**Label/Mono Font:** Segoe UI; Symbole aus Segoe Fluent Icons mit Rückfall auf Segoe MDL2 Assets

**Character:** Eine Schrift, zwei Schnitte (Normal 400, Semibold 600). Hierarchie entsteht durch Gewicht und wenige,
eng gestaffelte Größen, nicht durch große Überschriften. Im Einstellungsfenster wird Text im Display-Modus gesetzt
(pixelgenau), auf dem Gerät im Ideal-Modus, weil es skaliert wird.

### Hierarchy
- **Headline** (600, 22): Name des Rads (als Feld, das wie eine Überschrift aussieht) und Seitentitel „Allgemein".
- **Title** (600, 17): Titel des Inspektors (Name des gewählten Segments).
- **Section title** (600, 15): Abschnitte der Seite „Allgemein"; 32 Abstand darüber, 4 darunter.
- **Body** (400, 13): Grundschrift des Fensters, Feldinhalte, Schaltflächen. Die Hauptaktion ist 600.
- **Label** (600, 12, `Ink2`): Feld- und Gruppenbeschriftungen, in normaler Schreibung, nie in Versalien.
- **Help** (400, 12, Zeilenhöhe 17, `Ink2`): Hilfetexte unter Feldern und Schaltern, Tooltips (12, Weiß auf Graphit).
- **Hub title** (600, 15, `CText`): Titel im Display, höchstens 2 Zeilen auf 128 Breite.
- **Hub description** (400, 13, `CText2`): Vorschau im Display, 4 Zeilen (3, wenn der Titel umbricht), auf 126 Breite.
- **Key label** (600, 12,5): Beschriftung unter einem Symbol, höchstens 2 Zeilen.
- **Key label, solo** (600, 13,5): Beschriftung ohne Symbol, höchstens 3 Zeilen, in der Tastenfarbe.

### Named Rules
**Die Aufrecht-Regel.** Text auf dem Rad steht immer waagerecht und zentriert, nie entlang des Bogens gedreht. Die
verfügbare Breite wird je Taste abgetastet (mindestens 44); was nicht passt, endet mit Auslassungspunkten.

**Die Zwei-Schnitte-Regel.** Nur Normal und Semibold. Keine dritte Stärke, keine Kursive, keine Versalien mit Sperrung.

## Layout

**Gerät.** Ursprung ist die Radmitte. Von innen nach außen: Display (Radius 80) in einer Blende (`HubRadius` 84), eine
9 breite Fuge für die Richtungsmarke, Tasten ab `InnerRadius` 93 bis zum Außenradius minus `RimWidth` 6. Der Außenradius
wächst mit der Zahl der Segmente (`OuterFor`): bis 4 Segmente 200, bis 6 208, bis 8 222, bis 10 240, bis 12 258. Die Nabe
bleibt dabei gleich groß. Das erste Segment sitzt oben, weitere folgen im Uhrzeigersinn. Zwischen den Tasten liegt eine
überall gleich breite Fuge (`KeyGap` 5). Die Totzone in der Mitte hat Radius 34; von dort gilt die Richtung bis ins
Unendliche. Das Rad lässt sich von 70 % bis 140 % skalieren und bleibt immer ganz auf dem Bildschirm.

**Inhalt einer Taste.** Mittig auf der Taste, 1 nach außen versetzt. Symbol über Beschriftung: Symbol 26, Abstand 5.
Nur Symbol: 36. Eigenes Bild: 36 mit Beschriftung, 60 ohne. Nur Beschriftung: mittig. Hat eine Taste ein Symbol, aber
keine eigene Beschriftung, bleibt das Symbol allein. Unterräder tragen einen kleinen Winkel am Außenrand (Strich 1,75).

**Einstellungsfenster.** 1220 × 800, mindestens 1080 × 680. Drei Spalten: Seitenleiste 244 (`Rail`), Bühne (flexibel,
`Bg`), Inspektor 396 (`Surface`), getrennt durch 1 px `Line`. Die Bühne zeigt oben Radname, Auslöser und Segmentzahl
(bricht bei schmalem Fenster in zwei Zeilen um), in der Mitte das Gerät (532 × 532, wird nur verkleinert, nie vergrößert),
unten die Bedienhinweise. Der Inspektor rollt; seine Fußleiste mit der Hauptaktion steht fest. Die Seite „Allgemein" ist
eine einzelne Spalte, höchstens 600 breit, linksbündig: Zeilen mit 14 Innenabstand und einer Linie darunter, Bedienelement
rechts.

**Rhythmus.** Bedienelemente sind 34 hoch (Symbolschaltflächen 32, Zellen der Symbolauswahl 40). Abstände: 6 zwischen
Beschriftung und Feld, 8 zwischen benachbarten Schaltflächen, 18 über einer Feldbeschriftung, 24 Innenabstand im
Inspektor, 32 auf der Bühne, 40 auf der Seite „Allgemein", 12 in der Seitenleiste.

## Elevation & Depth

Tiefe hat genau eine Quelle: Das Gerät liegt auf dem Bildschirm und wirft einen Schatten. Die Tasten selbst sind flach;
ihre Körperlichkeit kommt aus den Fugen, der Blende und dem Hub. Das Einstellungsfenster ist flach und trennt Ebenen über
Tonwerte (`Rail`, `Bg`, `Surface`) und Linien; nur Popups schweben.

### Shadow Vocabulary
- **Raumschatten** (Gerät): radial, um 22 nach unten versetzt, Radius Außenradius + 60; Schwarz mit 29 % Deckkraft
  (Alpha 74) bis Außenradius − 30, danach in acht Stufen mit Potenz 2,0 auslaufend.
- **Kontaktschatten** (Gerät): radial, um 5 nach unten versetzt, Radius Außenradius + 10; Schwarz mit 52 % Deckkraft
  (Alpha 132) bis Außenradius − 6, danach mit Potenz 1,6 auslaufend.
- **Mulde** (Gerät): keine Schattierung, sondern die Fläche `BSeat` unter der gehobenen Taste.
- **Popup** (Einstellungen): Schlagschatten nach unten, Auslistung: Weichzeichnung 14, Versatz 4, 18 % Deckkraft;
  Symbolauswahl: 16, 5, 20 %.

### Named Rules
**Die Ein-Schatten-Regel.** Der zweischichtige Schatten unter dem Gerät ist der einzige Verlauf im System. Tasten, Nabe
und Flächen bekommen weder Verlauf noch Glanzlicht noch Glühen.

**Die Opak-Regel.** Das Gerät ist vollständig deckend. Durchscheinend sind nur das Ein- und Ausblenden des ganzen Rads
und kleine Zustandsschleier im Einstellungsfenster (überfahrene Geister-Schaltfläche, Rollbalken, Fokus-Hof).

## Shapes

Die Grundform ist der Keil mit gerundeten Ecken (`KeyCorner` 8). Seine Seitenkanten laufen parallel zur Trennlinie statt
auf die Mitte zu; dadurch ist die Fuge innen so breit wie außen. Dazu kommen konzentrische Kreise: Chassis, Blende,
Display. Die Richtungsmarke ist ein kurzer Bogen (Strich 4, runde Enden, etwa 19° lang) in der Fuge zwischen Nabe und
Tasten.

Im Einstellungsfenster sind Ecken leicht gerundet und einheitlich: Bedienelemente 6, Listeneinträge 5, Auslisten 8,
Symbolauswahl 10, Fokusring 9 (liegt 3 außerhalb des Elements). Ganz rund sind nur Schalter, Schieberegler-Knopf und
Farbfelder. Rahmen sind 1 px; der Fokusring und der Auswahlring der Farbfelder sind 2 px.

Symbole sind Liniensymbole auf 24er-Raster mit Strich 1,75, runden Enden und runden Ecken (Lucide, ISC, und Tabler, MIT),
in den Gruppen „Medizin" und „Werkzeuge". Sie skalieren samt Strichstärke mit der Symbolgröße. Zusätzlich wählbar sind
Zeichen der Windows-Symbolschrift (auf 92 % gesetzt, damit sie optisch zu den Liniensymbolen passen). Eigene Bilder werden
mit 20 % der kürzeren Seite als Eckenradius beschnitten, damit ein Foto wie ein Tastenbild sitzt. Das Programmsymbol
zeichnet der Code selbst: das Rad in klein, vier Tasten, die obere orange.

## Components

### Auswahlrad (Signaturkomponente)
Ein Hardware-Bedienteil: schwer, still, eindeutig.
- **Taste in Ruhe:** Fläche `CKey`, Symbol in Tastenfarbe, Beschriftung `CText`.
- **Taste unter dem Zeiger:** Fläche in Tastenfarbe, Inhalt Graphit, um `LiftPx` 4 nach außen gehoben, darunter die
  Mulde. Füllen dauert etwa 70 ms (exponentiell, Zeitkonstante 30 ms), Zurückfallen ist langsamer (85 ms). Die ausgelöste
  Taste bleibt beim Ausblenden erleuchtet.
- **Leere Taste:** `CKeyEmpty`, ohne Inhalt, reagiert nicht; das Display meldet „Frei".
- **Unterrad:** Winkel am Außenrand. Öffnet sich sofort beim Überfahren des Außenrands, sonst nach 450 ms Verweilen.
- **Display:** in Ruhe Name des Rads und „Mitte bricht ab"; über einer Taste deren Beschriftung und die Vorschau der
  Aktion.
- **Richtungsmarke:** folgt dem Zeigerwinkel stufenlos; `CText2` über leeren Tasten, sonst in der Farbe der Taste. In
  der Totzone ist sie aus.
- **Erscheinen und Verschwinden:** Einblenden 130 ms (Deckkraft und Größe 94 % → 100 %, stark abbremsend), Ausblenden
  110 ms (Deckkraft fällt, Größe wächst auf 102 %). Sind Windows-Animationen abgeschaltet, steht das Rad sofort da und
  verschwindet sofort, und die Taste hebt sich nicht; die Farbe wechselt weiterhin.
- **In den Einstellungen:** dieselbe Ansicht als Vorschau. Überfahren hellt die Taste auf `CKeyHot` auf, die gewählte
  Taste ist gefüllt und trägt die Auswahlkontur, leere Tasten zeigen ein Plus. Keine Richtungsmarke.

### Lückenabfrage (`src/FillIn.cs`)
Ein Stück des Geräts, kein Dialog des Arbeitsblatts: Gerätefarben, öffnet mittig am Zeiger, 480 breit.
- **Gehäuse:** `CChassis`, Rahmen 1 px `PRim`, Radius 10, Innenabstand 20; Schatten wie unter dem Gerät (nach unten,
  Weichzeichnung 28, Versatz 10, 50 %). Kopf: Segmentname in Hub title (600, 15, `CText`).
- **Display:** `BHub` mit Haarlinie `PHubRim`, Radius 8 – zeigt den fertigen Text. Gefüllte Lücken `CText`, leere als
  `[Name]` in `BText3`, die Lücke unter dem Fokus in der Segmentfarbe und unterstrichen (die Farbflut-Regel im Kleinen).
- **Eingabefeld:** `CKey`, Rahmen `PRim`, Höhe 34, Radius 6; überfahren Rahmen `#4A4E57`, fokussiert Rahmen in
  Segmentfarbe. Textauswahl in Segmentfarbe (45 %).
- **Option:** Zeile 32 hoch, Kästchen 18 (Radius 4, Rahmen 1,5 `CText2`); angekreuzt in Segmentfarbe mit Graphit-Haken.
  Fokussiert: Zeile `CKey` mit Rahmen in Segmentfarbe; überfahren nur ein Weiß-Schleier von 5 %, weil die Maus beim
  Öffnen zufällig über einer Option stehen kann.
- **Hauptaktion „Einfügen":** Fläche in Segmentfarbe, Schrift Graphit Semibold – die leuchtende Taste. „Abbrechen" als
  Geist. Fokusring 2 px `CText`.
- **Tastenhinweise:** Tastenkappen (`CKey`, Rahmen `PRim`, Radius 4, 11 Semibold) mit Wirkung in Help-Größe `CText2`.
- **Bewegung:** 130 ms Einblenden der Deckkraft, nur wenn Windows-Animationen an sind.

### Buttons
Sachlich und flach; die Form sagt „Bedienelement", die Farbe sagt nichts.
- **Shape:** leicht gerundet (6), Höhe 34, Rahmen 1 px.
- **Standard (`Btn`):** `Surface` mit Rahmen `FieldLine`, Schrift `Ink`, Innenabstand 12. Überfahren `HoverSurface`,
  gedrückt `PressSurface`.
- **Hauptaktion (`BtnPrimary`):** `Ink` mit weißer Semibold-Schrift, Innenabstand 16. Überfahren `InkHover`, gedrückt
  Schwarz. Eine pro Ansicht („Aktion testen").
- **Geist (`BtnGhost`):** ohne Fläche und Rahmen; überfahren ein Graphit-Schleier von 8 %, gedrückt 15 %. Für
  nachrangige und entfernende Aktionen („Rad löschen", „Segment entfernen", „Ohne Symbol").
- **Symbolschaltfläche (`BtnIcon`):** Geist in 32 × 32. **Zelle der Symbolauswahl (`IconCell`):** Geist in 40 × 40.
- **Fokus:** Ring 2 px `Accent`, 3 außerhalb, nur bei Tastaturbedienung. **Deaktiviert:** 45 % Deckkraft (Geist 40 %).

### Inputs / Fields
- **Style:** `Surface`, Rahmen 1 px `FieldLine`, Radius 6, Höhe 34, Innenabstand 8. Platzhalter in `Ink3`.
- **Hover / Focus:** überfahren Rahmen `Ink2`; fokussiert Rahmen `Accent` und Fokus-Hof (3 px `AccentRing`).
- **Textbereich (`TextArea`):** mehrzeilig, Innenabstand 8/7, eigener schmaler Rollbalken.
- **Titelfeld (`TitleBox`):** sieht aus wie eine Überschrift (22, Semibold, ohne Fläche und Rahmen) und wird beim
  Überfahren und im Fokus zum Feld.
- **Auswahlliste:** wie das Feld, mit Pfeil rechts; geöffnet Rahmen `Accent`. Die Ausliste ist `Surface` mit Rahmen
  `Line`, Radius 8, Popup-Schatten; Einträge 32 hoch, überfahren `HoverSurface`, gewählt `AccentSoft`.
- **Deaktiviert:** 50 % Deckkraft.

### Schalter, Schieberegler, Farbfeld
- **Schalter (`Switch`):** Beschriftung links, Schiene rechts (40 × 22, ganz rund). Aus `FieldLine`, an `Accent`; weißer
  Knopf (16), fährt in 160 ms mit starkem Abbremsen.
- **Schieberegler:** Schiene 4 hoch in `FieldLine`, gefüllter Teil `Ink`, Knopf 18 weiß mit 2 px Rand `Ink`.
- **Farbfeld (`Swatch`):** Kreis 20 in einem 30er-Feld; gewählt mit 2 px Ring `Ink`, überfahren derselbe Ring mit 35 %.

### Navigation
- **Seitenleiste:** Einträge mit Radius 6 und Innenabstand 10/8; überfahren `HoverRail`, gewählt `Surface` mit Rahmen
  `Line`. Ein Rad zeigt Namen (13) und darunter den Auslöser (12, `Ink2`). „Allgemein" sitzt unten.
- **Brotkrumen:** über dem Gerät, sobald ein Unterrad bearbeitet wird.

### Hinweis, Tooltip, Rollbalken
- **Hinweisfläche:** `AccentSoft`, Radius 6, Innenabstand 12, Text `Ink`, mit einer Standard-Schaltfläche darin. Dient
  der Kennzeichnung von Beispieltexten.
- **Tooltip:** `Ink` mit weißer Schrift (12), Radius 6, erscheint unter dem Element.
- **Rollbalken:** 10 breit, ohne Pfeile, Daumen als Graphit-Schleier (45 %, überfahren 65 %, gezogen 80 %).

## Do's and Don'ts

### Do:
- **Do** das Gerät opak halten: `CChassis` als Scheibe, `CKey` als Tastenkappe, Fugen konstant 5 breit (`KeyGap`).
- **Do** Farbe in Ruhe auf das Symbol beschränken und erst unter dem Zeiger die ganze Taste fluten; Inhalt dann Graphit.
- **Do** nur die acht Tastenfarben aus `Palette` verwenden; neue Farben müssen Graphit-Schrift mit mindestens 4,5:1
  tragen.
- **Do** Text auf dem Rad waagerecht setzen und die Breite je Taste abtasten.
- **Do** im Display zeigen, was beim Loslassen passiert, bevor es passiert.
- **Do** die Richtungsmarke in der Fuge zwischen Nabe und Tasten führen, in der Farbe der Taste.
- **Do** Liniensymbole auf 24er-Raster mit Strich 1,75 und runden Enden zeichnen.
- **Do** die Windows-Einstellung „Animationen anzeigen" achten: ohne sie kein Einblenden, kein Ausblenden, kein Hub.
- **Do** im Einstellungsfenster Graphit für die eine Hauptaktion und `Accent` nur für Zustand einsetzen.
- **Do** Bedienelemente mit `FieldLine` rahmen und Flächen mit `Line` trennen.
- **Do** auf Deutsch und per Du schreiben, kurz und konkret („Anklicken und belegen", „Mitte bricht ab").

### Don't:
- **Don't** das Rad durchscheinend machen, weichzeichnen oder glühen lassen – verweigert wird der „durchscheinende Donut
  mit Neon-Glühen und Blur".
- **Don't** Verläufe einsetzen, außer im Schlagschatten unter dem Gerät.
- **Don't** Tasten in Ruhe einfärben oder Beschriftungen entlang des Bogens drehen.
- **Don't** `Accent` als Schaltflächenfarbe oder `Palette-Orange` auf hellem Grund verwenden.
- **Don't** dem Zielprogramm den Fokus nehmen: Das Rad ist ein Gast, kein Fenster. Einzige Ausnahme ist die
  Lückenabfrage, in die getippt wird; sie gibt den Fokus vor dem Einfügen zurück.
- **Don't** eine dritte Schriftstärke, Versalien-Etiketten oder eine zweite Schrift einführen.
- **Don't** Rasterbilder ausliefern, die nicht der Code erzeugt; das Programmsymbol entsteht beim Bauen.
