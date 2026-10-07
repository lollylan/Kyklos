# Product

<!-- impeccable:product-schema 1 -->

## Platform

windows-desktop (native Windows 10/11; keine der Schema-Plattformen web/ios/android trifft zu)

## Stack

C# / WPF auf .NET Framework 4.8 (in Windows 10/11 enthalten). Vom Nutzer bestätigt: native Windows-App statt Web-Technik.
Gebaut wird direkt mit dem Roslyn-Compiler der Visual-Studio-Build-Tools über `build.ps1` – kein .NET SDK, keine NuGet-Pakete.
Ergebnis ist eine einzelne portable `Kyklos.exe` (≈ 1 MB), die Konfiguration liegt als `Kyklos.json` daneben.

## Users

Florian Rasche, Allgemeinarzt mit eigener Praxis in Würzburg. Er sitzt im Sprechzimmer am PC, dokumentiert während oder
direkt nach der Untersuchung in der lokal installierten Praxissoftware und will wiederkehrende Texte (Normalbefunde) und
Handgriffe ohne Umweg auslösen. Die Hand liegt dabei ohnehin an Tastatur und Maus; der Blick soll in der Karteikarte bleiben.

## Product Purpose

Ein Auswahlrad, das beim Halten eines Auslösers am Mauszeiger erscheint. Die Maus wird in Richtung eines Segments bewegt,
beim Loslassen wird dessen Aktion ausgeführt. Wichtigster Fall: einen vordefinierten Text in das Textfeld einfügen, in dem
der Cursor bereits steht. Erfolg heißt: Normalbefund in unter einer Sekunde im Feld, ohne dass das Feld den Fokus verliert.

## Positioning

Die Funktionen eines Stream Decks (frei belegbare Tasten mit Bild und Text, Makros, Ordner) als reine Software, bedient wie
ein Auswahlrad aus Computerspielen: Richtung statt Tastenposition, eine Geste statt Zielen und Klicken.

## Operating Context

- Auslöser: standardmäßig eine einzelne Taste (halten), alternativ Tastenkombination oder Maus-Seitentaste; pro Rad einstellbar.
- Halten → Rad erscheint; Maus in eine Richtung; Loslassen löst aus. Mitte = abbrechen.
- Praxissoftware läuft lokal als normales Windows-Programm. Texte werden standardmäßig über die Zwischenablage eingefügt
  (alter Inhalt wird wiederhergestellt), alternativ Zeichen für Zeichen getippt.
- Das Tool läuft im Infobereich (Tray), ohne Installation, auch vom USB-Stick oder Netzlaufwerk.

## Capabilities and Constraints

- 2 bis 12 Segmente pro Rad, mehrere Räder mit eigenem Auslöser, Unterräder (Ordner).
- Segment: Beschriftung und/oder Symbol bzw. eigenes Bild, eigene Farbe.
- Lückentext: Lücken im Text (`{?Tage}` Eingabefeld, `{?Symptome: A | B | C}` Mehrfachauswahl, `{?FSME: ja / nein}` Entweder-oder) werden beim Auslösen in
  einem Fenster am Zeiger abgefragt, per Tastatur (Tab, Leertaste, Enter). Dieses Fenster ist die einzige Stelle, die den
  Fokus kurz übernimmt; danach geht er an das Zielprogramm zurück, erst dann wird eingefügt.
- Aktionen: Text einfügen (mit Platzhaltern für Datum, Uhrzeit, Zwischenablage, Cursorposition), Tastenkombination,
  Programm/Datei/Ordner öffnen, Website, Medien- und Systemtasten, Makro (mehrere Schritte mit Pausen und Mausklicks an festen Koordinaten), Unterrad.
- Das Rad darf dem Zielprogramm nie den Tastaturfokus nehmen.
- Fenster von Programmen, die als Administrator laufen, sind für ein normal gestartetes Tool nicht erreichbar (Windows-Schutz).
- Offen: programmabhängige Räder (anderes Rad je nach aktivem Programm) sind noch nicht umgesetzt.

## Brand Commitments

Name und Dateiname „Kyklos" (griechisch: Kreis, Rad; vorher Arbeitstitel „Shortcut"), ein Projekt unter der Marke
Asklaion (asklaion.de), kostenlos und quelloffen unter der MIT-Lizenz. Oberfläche auf Deutsch, Ansprache per Du. Vom Nutzer gesetzte Bildwelt:
„Kombination aus Auswahlrädern aus Computerspielen und dem Stream Deck" – und es soll schön aussehen.
Das Praxis-CI gilt nur für Praxisdokumente, nicht für dieses persönliche Werkzeug.

## Evidence on Hand

Keine echten Befundtexte des Nutzers. Die mitgelieferten Normalbefunde (Lunge, Herz, Abdomen, HNO, Allgemeinzustand) sind
Beispieltexte und als solche zu behandeln, bis er sie ersetzt. Die App markiert sie (Feld `sample`): Sie werden mit dem
Vermerk „[Beispieltext]" eingefügt, bis der Nutzer sie ändert oder ausdrücklich übernimmt.

## Product Principles

1. Der Fokus bleibt im Zielfeld – das Rad ist ein Gast, kein Fenster.
2. Richtung schlägt Genauigkeit: Die Auswahl gilt ab der Mitte bis ins Unendliche, nicht nur auf der Taste.
3. Vor dem Loslassen ist sichtbar, was passieren wird (Vorschau in der Mitte).
4. Alles ist ohne Datei-Editor einstellbar; Änderungen gelten sofort.
5. Eine Datei zum Mitnehmen: Exe plus Konfiguration, keine Installation.
