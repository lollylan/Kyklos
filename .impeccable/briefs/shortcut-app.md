# Surface brief: Kyklos (Rad-Overlay + Einstellungsfenster)

Scope: das Auswahlrad (Overlay) und das Einstellungsfenster der Windows-App. Mode: Operate.
Audience/job: siehe PRODUCT.md. Task: Segment per Richtung wählen; Räder und Aktionen ohne Datei-Editor pflegen.
Constraints: natives WPF, keine Web-Fonts, kein Netzwerk; das Overlay darf nie den Fokus nehmen.

## Direction contract

THESIS: Ein Stream Deck, zum Drehrad gebogen. Das Rad ist ein Gerät, kein HUD: opak, matt, mit echten Tasten und einem
Display in der Mitte. Verweigert wird der Kategorie-Standard „durchscheinender Donut mit Neon-Glühen und Blur".

OWN-WORLD: Graphit-Chassis (#15161A) mit etwas helleren Tastenkappen (#26282E) in konstant breiten, dunklen Fugen; jede Taste trägt eine von acht
Funktionsfarben (Braun-Taschenrechner-Palette: Orange, Gelb, Grün, Türkis, Blau, Violett, Rot, Hell). Im Ruhezustand färbt
die Farbe nur das Symbol, unter dem Zeiger füllt sie die ganze Taste und die Schrift kippt auf Graphit. Segoe UI, keine
Verläufe außer dem Schlagschatten, kein Glühen. Einstellungen: helles, kühles Arbeitsblatt, auf dem das dunkle Gerät liegt.

STORY: Taste halten – das Gerät liegt unter dem Zeiger. Richtung geben – die Taste leuchtet, das Display zeigt den Text,
der gleich im Feld steht. Loslassen – erledigt, Zeiger und Fokus sind wieder dort, wo sie waren.

FIRST VIEWPORT: Overlay: Scheibe Ø ≈ 444 px am Zeiger, acht Keiltasten (Symbol über Beschriftung, aufrecht), Nabe Ø 168 px
als Display mit Titel und bis zu vier Zeilen Vorschau, Richtungsmarke in der Fuge zwischen Nabe und Tasten.
Einstellungen: links Radliste, Mitte das Gerät zum Anklicken, rechts der Inspektor des gewählten Segments;
Primäraktion „Aktion testen" unten im Inspektor.

FORM: Hardware-Bedienteil (Stream Deck × Drehschalter), vom Briefing gesetzt – Position 1 der Liste. Signature interaction:
Richtungsmarke folgt dem Zeiger stufenlos, die Taste rastet ein, hebt sich 4 px nach außen und füllt sich in 70 ms.
Seed: keiner – der Konzept-Wurf (concept-seed) lief nicht, weil der Launcher seine Engine nicht laden konnte; die Richtung
ist durch das Briefing des Nutzers vorgegeben. Dem Nutzer so offengelegt.

FINISH: unreviewed and undocumented is unfinished; this build ends with the finish review, the verdict, DESIGN.md, and every shipping raster carrying its provenance
