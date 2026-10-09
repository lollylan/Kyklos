# End-to-End-Test: startet dist\Kyklos.exe mit einer Testkonfiguration und spielt echte Eingaben ein.
#
#   powershell -STA -ExecutionPolicy Bypass -File tools\e2e.ps1
#
# Achtung: Der Test bewegt etwa 35 Sekunden lang den Mauszeiger und tippt in ein eigenes Testfenster (Tasten F13 und F14
# als Ausloeser). Solange nichts anfassen. Kommt das Testfenster nicht in den Vordergrund, bricht er ab, ohne zu tippen.
# Eine laufende Kyklos-Instanz vorher beenden (Kyklos.exe --quit). Bildschirmfotos landen in %TEMP%\kyklos-e2e.
$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$here = Join-Path $env:TEMP 'kyklos-e2e'
$dir = Join-Path $here 'app'
New-Item -ItemType Directory -Force $dir | Out-Null
Copy-Item (Join-Path $repo 'dist\Kyklos.exe') $dir -Force

$json = @'
{
  "version": 1,
  "settings": { "scale": 1, "tapSticky": true, "restoreCursor": true, "restoreClipboard": true, "pasteDelayMs": 300 },
  "wheels": [
    { "id": "test", "name": "Test", "trigger": { "code": 124 },
      "slots": [
        { "label": "Oben", "color": "#62ABFF", "icon": "v:lunge", "action": { "type": "text", "text": "ALPHA \u00e4\u00f6\u00fc\u00df \u20ac\nZeile zwei {cursor}Ende" } },
        { "label": "Rechts", "color": "#F5C542", "action": { "type": "text", "mode": "type", "text": "BETA getippt \u00e4\u00df" } },
        { "label": "Unten", "color": "#5DD28B", "action": { "type": "macro", "stepDelayMs": 60, "steps": [
            { "type": "text", "text": "M1" }, { "type": "keys", "keys": { "code": 9 } }, { "type": "text", "mode": "type", "text": "M2" } ] } },
        { "label": "Links", "color": "#FF8A3D", "icon": "g:E90F", "action": { "type": "folder", "slots": [
            { "label": "SubOben", "color": "#62ABFF", "action": { "type": "text", "text": "SUB-OBEN" } },
            { "label": "SubRechts", "color": "#F5C542", "action": { "type": "text", "text": "SUB-RECHTS", "sample": true } },
            { "label": "SubUnten", "color": "#5DD28B", "action": { "type": "text", "text": "SEIT {?Tage} TAGEN: {?Symptome: Husten | Schnupfen | Fieber}" } },
            { "label": "SubLinks", "color": "#FF7B7B", "action": { "type": "text", "text": "SUB-LINKS" } } ] } }
      ] },
    { "id": "ocr", "name": "OCR", "trigger": { "code": 125 },
      "slots": [
        { "label": "Text erkennen", "color": "#45CFC6", "action": { "type": "media", "media": "ocr" } },
        { "label": "Leer", "color": "#5DD28B", "action": { "type": "none" } },
        { "label": "Makro", "color": "#62ABFF", "action": { "type": "macro", "stepDelayMs": 60, "steps": [
            { "type": "media", "media": "ocr" }, { "type": "text", "text": "DANACH" } ] } },
        { "label": "Leer", "color": "#FF7B7B", "action": { "type": "none" } } ] }
  ]
}
'@
[System.IO.File]::WriteAllText((Join-Path $dir 'Kyklos.json'), $json, (New-Object System.Text.UTF8Encoding($false)))

Add-Type -AssemblyName System.Windows.Forms, System.Drawing
Add-Type @"
using System; using System.Runtime.InteropServices;
public static class U {
  [DllImport("user32.dll")] public static extern void keybd_event(byte vk, byte scan, uint flags, UIntPtr extra);
  [DllImport("user32.dll")] public static extern void mouse_event(uint flags, uint dx, uint dy, uint data, UIntPtr extra);
  [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
  [DllImport("user32.dll")] public static extern bool GetCursorPos(out POINT p);
  [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
  [DllImport("user32.dll", CharSet=CharSet.Unicode)] public static extern IntPtr FindWindow(string cls, string title);
  [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr h);
  [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
  public struct POINT { public int x, y; }
}
"@
[void][U]::SetProcessDPIAware()

function Wait($ms) { $end = (Get-Date).AddMilliseconds($ms); while ((Get-Date) -lt $end) { [System.Windows.Forms.Application]::DoEvents(); Start-Sleep -Milliseconds 10 } }
function KeyDown($vk) { [U]::keybd_event($vk, 0, 0, [UIntPtr]::Zero) }
function KeyUp($vk) { [U]::keybd_event($vk, 0, 2, [UIntPtr]::Zero) }
function Shot($name) {
  $b = $form.Bounds
  $bmp = New-Object System.Drawing.Bitmap $b.Width, $b.Height
  $g = [System.Drawing.Graphics]::FromImage($bmp)
  $g.CopyFromScreen($b.Location, [System.Drawing.Point]::Empty, $b.Size)
  $g.Dispose(); $bmp.Save((Join-Path $here "$name.png")); $bmp.Dispose()
}
function OverlayVisible { $h = [U]::FindWindow([NullString]::Value, 'Kyklos-Rad'); return ($h -ne [IntPtr]::Zero) -and [U]::IsWindowVisible($h) }
$results = New-Object System.Collections.ArrayList
function Check($name, $ok, $detail) { [void]$results.Add(('{0}  {1}  {2}' -f $(if ($ok) { 'OK    ' } else { 'FEHLER' }), $name, $detail)) }

$F13 = 0x7C
$orig = New-Object U+POINT; [void][U]::GetCursorPos([ref]$orig)
$t0 = Get-Date
$proc = Start-Process (Join-Path $dir 'Kyklos.exe') -PassThru
# bereit, sobald das (unsichtbare) Rad-Fenster existiert
while (([U]::FindWindow([NullString]::Value, 'Kyklos-Rad') -eq [IntPtr]::Zero) -and ((Get-Date) -lt $t0.AddSeconds(40))) { Start-Sleep -Milliseconds 50 }
[void]$results.Add(('INFO    Startzeit bis bereit: {0:N1} s' -f ((Get-Date) - $t0).TotalSeconds))
Start-Sleep -Milliseconds 600

$screen = [System.Windows.Forms.Screen]::PrimaryScreen.WorkingArea
$form = New-Object System.Windows.Forms.Form
$form.Text = 'Kyklos E2E'; $form.StartPosition = 'Manual'; $form.TopMost = $true
$form.Size = New-Object System.Drawing.Size 820, 700
$form.Location = New-Object System.Drawing.Point (($screen.Left + ($screen.Width - 820) / 2), ($screen.Top + ($screen.Height - 700) / 2))
$tb = New-Object System.Windows.Forms.TextBox
$tb.Multiline = $true; $tb.Dock = 'Fill'; $tb.AcceptsTab = $true; $tb.Font = New-Object System.Drawing.Font 'Segoe UI', 11
$script:clicks = 0
$tb.Add_MouseDown({ $script:clicks++ })
$form.Controls.Add($tb)
$form.Show()
Wait 200
# Ein Klick ins Testfenster holt es zuverlaessig in den Vordergrund (SetForegroundWindow darf ein Hintergrundprozess nicht).
$mid = $form.PointToScreen((New-Object System.Drawing.Point ($form.ClientSize.Width / 2), ($form.ClientSize.Height / 2)))
[void][U]::SetCursorPos($mid.X, $mid.Y); Wait 80
[U]::mouse_event(2, 0, 0, 0, [UIntPtr]::Zero); Wait 40; [U]::mouse_event(4, 0, 0, 0, [UIntPtr]::Zero)
Wait 400
[void]$tb.Focus(); $script:clicks = 0

try {
  if ([U]::GetForegroundWindow() -ne $form.Handle) { throw 'Testfenster ist nicht im Vordergrund - Test abgebrochen, es wurde nichts eingegeben.' }
  [System.Windows.Forms.Clipboard]::SetText('VORHER-123')
  $c = $form.PointToScreen((New-Object System.Drawing.Point ($form.ClientSize.Width / 2), ($form.ClientSize.Height / 2)))
  $cx = $c.X; $cy = $c.Y

  # 1: Halten, nach oben, loslassen -> Text per Zwischenablage, {cursor}, Zeiger und Zwischenablage wiederhergestellt
  [void][U]::SetCursorPos($cx, $cy); Wait 120
  KeyDown $F13; Wait 260
  Check 'Rad erscheint beim Halten' (OverlayVisible) ''
  Check 'Fokus bleibt im Textfeld' ([U]::GetForegroundWindow() -eq $form.Handle) ''
  Shot 'e2e-1-offen'
  [void][U]::SetCursorPos($cx, $cy - 130); Wait 220
  Shot 'e2e-2-oben'
  KeyUp $F13; Wait 900
  $expect = "ALPHA äöüß €`r`nZeile zwei Ende"
  Check 'Text eingefuegt (Zwischenablage)' ($tb.Text -eq $expect) ("'" + $tb.Text.Replace("`r`n", '|') + "'")
  Check '{cursor} setzt Schreibmarke' ($tb.SelectionStart -eq ($tb.Text.Length - 4)) ("SelectionStart=" + $tb.SelectionStart + " von " + $tb.Text.Length)
  $p = New-Object U+POINT; [void][U]::GetCursorPos([ref]$p)
  Check 'Mauszeiger zurueckgesetzt' (($p.x -eq $cx) -and ($p.y -eq $cy)) ("$($p.x),$($p.y) erwartet $cx,$cy")
  Check 'Zwischenablage wiederhergestellt' ([System.Windows.Forms.Clipboard]::GetText() -eq 'VORHER-123') ("'" + [System.Windows.Forms.Clipboard]::GetText() + "'")
  Check 'Rad wieder zu' (-not (OverlayVisible)) ''

  # 2: getippter Text (rechts)
  $tb.Clear(); Wait 100
  KeyDown $F13; Wait 200; [void][U]::SetCursorPos($cx + 130, $cy); Wait 150; KeyUp $F13; Wait 800
  Check 'Text getippt' ($tb.Text -eq 'BETA getippt äß') ("'" + $tb.Text + "'")

  # 3: Antippen -> Rad bleibt offen, Klick waehlt (Makro unten), Klick erreicht das Programm nicht
  $tb.Clear(); $script:clicks = 0; Wait 100
  KeyDown $F13; Wait 60; KeyUp $F13; Wait 450
  Check 'Antippen haelt Rad offen' (OverlayVisible) ''
  [void][U]::SetCursorPos($cx, $cy + 130); Wait 200
  [U]::mouse_event(2, 0, 0, 0, [UIntPtr]::Zero); Wait 40; [U]::mouse_event(4, 0, 0, 0, [UIntPtr]::Zero); Wait 1300
  Check 'Makro (Text, Tab, getippt)' ($tb.Text -eq "M1`tM2") ("'" + $tb.Text.Replace("`t", '<TAB>') + "'")
  Check 'Klick wurde abgefangen' ($script:clicks -eq 0) ("Klicks im Textfeld: " + $script:clicks)

  # 4: Unterrad durch Hinausfahren ueber den Rand, dann oben waehlen
  $tb.Clear(); Wait 100
  [void][U]::SetCursorPos($cx, $cy); Wait 100
  KeyDown $F13; Wait 220; [void][U]::SetCursorPos($cx - 215, $cy); Wait 300
  Shot 'e2e-3-unterrad'
  [void][U]::SetCursorPos($cx - 215, $cy - 125); Wait 200; KeyUp $F13; Wait 800
  Check 'Unterrad -> Eintrag oben' ($tb.Text -eq 'SUB-OBEN') ("'" + $tb.Text + "'")

  # 4b: mitgelieferter Beispieltext wird mit Vermerk eingefuegt
  $tb.Clear(); Wait 100
  [void][U]::SetCursorPos($cx, $cy); Wait 100
  KeyDown $F13; Wait 220; [void][U]::SetCursorPos($cx - 215, $cy); Wait 300
  [void][U]::SetCursorPos($cx - 215 + 125, $cy); Wait 200; KeyUp $F13; Wait 800
  Check 'Beispieltext traegt Vermerk' ($tb.Text -eq '[Beispieltext] SUB-RECHTS') ("'" + $tb.Text + "'")

  # 4c: Lueckentext im Unterrad unten - Fenster nimmt den Fokus, Tab/Leertaste/Enter, danach zurueck und einfuegen
  function OpenGaps {
    [void][U]::SetCursorPos($cx, $cy); Wait 100
    KeyDown $F13; Wait 220; [void][U]::SetCursorPos($cx - 215, $cy); Wait 300
    [void][U]::SetCursorPos($cx - 215, $cy + 125); Wait 200; KeyUp $F13; Wait 700
  }
  function Tap($vk) { KeyDown $vk; Wait 30; KeyUp $vk; Wait 90 }
  $tb.Clear(); Wait 100
  OpenGaps
  $fill = [U]::FindWindow([NullString]::Value, 'Kyklos ' + [char]0x2013 + ' SubUnten')
  Check 'Lueckenfenster erscheint' (($fill -ne [IntPtr]::Zero) -and [U]::IsWindowVisible($fill)) ''
  Check 'Lueckenfenster hat den Fokus' ([U]::GetForegroundWindow() -eq $fill) ''
  Tap 0x33; Tap 0x09; Tap 0x20; Tap 0x09; Tap 0x09; Tap 0x20
  $b = $form.Bounds; Shot 'e2e-4-luecken'
  Tap 0x0D; Wait 1100
  Check 'Luecken: Tab, Leertaste, Enter fuegt ein' ($tb.Text -eq 'SEIT 3 TAGEN: Husten und Fieber') ("'" + $tb.Text + "'")
  Check 'Fokus wieder im Testfenster' ([U]::GetForegroundWindow() -eq $form.Handle) ''
  Check 'Zwischenablage nach Luecken wiederhergestellt' ([System.Windows.Forms.Clipboard]::GetText() -eq 'VORHER-123') ("'" + [System.Windows.Forms.Clipboard]::GetText() + "'")

  $tb.Clear(); Wait 100
  OpenGaps
  Tap 0x35; Tap 0x0D; Tap 0x28; Tap 0x20; Tap 0x0D; Wait 1100
  Check 'Luecken: Enter springt zur naechsten Luecke, Pfeil runter' ($tb.Text -eq 'SEIT 5 TAGEN: Schnupfen') ("'" + $tb.Text + "'")

  $tb.Clear(); Wait 100
  OpenGaps
  Tap 0x37; Tap 0x1B; Wait 700
  Check 'Luecken: Esc bricht ab, nichts eingefuegt' ($tb.Text -eq '') ("'" + $tb.Text + "'")
  Check 'Fokus nach Abbruch wieder im Testfenster' ([U]::GetForegroundWindow() -eq $form.Handle) ''

  # 5: Esc bricht ab
  $tb.Clear(); Wait 100
  KeyDown $F13; Wait 200; [void][U]::SetCursorPos($cx + 130, $cy); Wait 120
  KeyDown 0x1B; KeyUp 0x1B; Wait 350
  $closed = -not (OverlayVisible)
  # echte Tastaturen wiederholen eine gehaltene Taste: das darf das Rad nicht wieder oeffnen
  $reopened = $false
  for ($n = 0; $n -lt 12; $n++) { KeyDown $F13; Wait 35; if (OverlayVisible) { $reopened = $true } }
  KeyUp $F13; Wait 600
  Check 'Esc schliesst das Rad' $closed ''
  Check 'Gehaltene Taste oeffnet nicht erneut' (-not $reopened) ''
  Check 'Esc loest nichts aus' ($tb.Text -eq '') ("'" + $tb.Text + "'")

  # 6: lange halten und in der Mitte loslassen -> Abbruch
  KeyDown $F13; Wait 600; KeyUp $F13; Wait 500
  Check 'Mitte loslassen bricht ab' ((-not (OverlayVisible)) -and ($tb.Text -eq '')) ("'" + $tb.Text + "'")

  # 7: Text erkennen (zweites Rad, F14) - Rahmen ueber einem Brieftext aufziehen, Text landet in der Zwischenablage.
  #    Der Brieftext steht in einem eigenen Feld ohne Schreibmarke; deren Blinken gehoert nicht zum Text.
  $F14 = 0x7D
  $capTitle = 'Kyklos ' + [char]0x2013 + ' Text erkennen'
  function CaptureVisible { $h = [U]::FindWindow([NullString]::Value, $capTitle); return ($h -ne [IntPtr]::Zero) -and [U]::IsWindowVisible($h) }
  function Drag($x0, $y0, $x1, $y1) {
    [void][U]::SetCursorPos($x0, $y0); Wait 80
    [U]::mouse_event(2, 0, 0, 0, [UIntPtr]::Zero); Wait 60
    for ($s = 1; $s -le 8; $s++) { [void][U]::SetCursorPos($x0 + ($x1 - $x0) * $s / 8, $y0 + ($y1 - $y0) * $s / 8); Wait 25 }
    Wait 80; [U]::mouse_event(4, 0, 0, 0, [UIntPtr]::Zero)
  }
  function OpenOcr($dy) {
    [void][U]::SetCursorPos($cx, $cy); Wait 100
    KeyDown $F14; Wait 220; [void][U]::SetCursorPos($cx, $cy + $dy); Wait 200; KeyUp $F14; Wait 700
  }
  $letter = New-Object System.Windows.Forms.Label
  $letter.Font = New-Object System.Drawing.Font 'Segoe UI', 20
  $letter.BackColor = [System.Drawing.Color]::White
  $letter.Text = "Kreatinin 1,2 mg/dl, Kalium 4,1 mmol/l`n`n`nZweiter Absatz bleibt getrennt"
  $letter.Dock = 'Top'; $letter.Height = 5 * $letter.Font.Height
  $form.Controls.Add($letter)
  $tb.Clear(); [void]$tb.Focus(); Wait 300
  $p0 = $letter.PointToScreen((New-Object System.Drawing.Point 0, 0))
  $line = $letter.Font.Height
  $cy = $tb.PointToScreen((New-Object System.Drawing.Point 0, ($tb.ClientSize.Height / 2))).Y

  [System.Windows.Forms.Clipboard]::SetText('VORHER-OCR')
  OpenOcr -130
  Check 'Auswahl erscheint' (CaptureVisible) ''
  Check 'Auswahl hat den Fokus' ([U]::GetForegroundWindow() -eq [U]::FindWindow([NullString]::Value, $capTitle)) ''
  Drag $p0.X ($p0.Y + 2) ($p0.X + $letter.Width - 4) ($p0.Y + $letter.Height - 2)
  Wait 1800
  $got = [System.Windows.Forms.Clipboard]::GetText()
  Check 'Erkannter Text in der Zwischenablage' ($got -eq "Kreatinin 1,2 mg/dl, Kalium 4,1 mmol/l`r`n`r`nZweiter Absatz bleibt getrennt") ("'" + $got.Replace("`r`n", '|') + "'")
  Check 'Auswahl wieder zu' (-not (CaptureVisible)) ''
  $toast = [U]::FindWindow([NullString]::Value, 'Kyklos ' + [char]0x2013 + ' Texterkennung')
  Check 'Rueckmeldung erscheint' (($toast -ne [IntPtr]::Zero) -and [U]::IsWindowVisible($toast)) ''
  Check 'Fokus nach Erkennen im Testfenster' ([U]::GetForegroundWindow() -eq $form.Handle) ''
  Shot 'e2e-5-erkannt'
  Check 'Erkennen tippt nichts ins Feld' ($tb.Text -eq '') ("'" + $tb.Text + "'")

  # 7b: Esc bricht ab, Zwischenablage bleibt
  Wait 3000
  [System.Windows.Forms.Clipboard]::SetText('VORHER-OCR')
  OpenOcr -130
  $opened = CaptureVisible
  Tap 0x1B; Wait 500
  Check 'Auswahl: Esc bricht ab' ($opened -and -not (CaptureVisible)) ''
  Check 'Auswahl: Zwischenablage unveraendert' ([System.Windows.Forms.Clipboard]::GetText() -eq 'VORHER-OCR') ("'" + [System.Windows.Forms.Clipboard]::GetText() + "'")
  Check 'Fokus nach Abbruch der Auswahl im Testfenster' ([U]::GetForegroundWindow() -eq $form.Handle) ''

  # 7c: Im Makro haelt ein Abbruch alle folgenden Schritte an
  OpenOcr 130
  $opened = CaptureVisible
  Tap 0x1B; Wait 900
  Check 'Makro haelt nach abgebrochener Auswahl an' ($opened -and $tb.Text -eq '') ("'" + $tb.Text + "'")

  # 7d: Im Makro geht es nach dem Erkennen weiter; danach liegt wieder der erkannte Text in der Zwischenablage
  OpenOcr 130
  Drag $p0.X ($p0.Y + 2) ($p0.X + $letter.Width - 4) ($p0.Y + $line + 4)
  Wait 2200
  Check 'Makro laeuft nach dem Erkennen weiter' ($tb.Text -eq 'DANACH') ("'" + $tb.Text + "'")
  Check 'Makro: erkannter Text bleibt in der Zwischenablage' ([System.Windows.Forms.Clipboard]::GetText() -eq 'Kreatinin 1,2 mg/dl, Kalium 4,1 mmol/l') ("'" + [System.Windows.Forms.Clipboard]::GetText() + "'")
}
catch { [void]$results.Add('ABBRUCH  ' + $_.Exception.Message) }
finally {
  KeyUp $F13; KeyUp 0x7D
  $form.Close()
  [void][U]::SetCursorPos($orig.x, $orig.y)
  Start-Process (Join-Path $dir 'Kyklos.exe') -ArgumentList '--quit' -Wait
  Start-Sleep -Milliseconds 800
  if (-not $proc.HasExited) { [void]$results.Add('HINWEIS  Prozess lief nach --quit noch, wird beendet'); $proc.Kill() } else { [void]$results.Add('OK      Beenden ueber --quit') }
}
$results
$log = Join-Path $dir 'Kyklos.log'
if (Test-Path $log) { '--- Kyklos.log ---'; Get-Content $log -Encoding UTF8 }
