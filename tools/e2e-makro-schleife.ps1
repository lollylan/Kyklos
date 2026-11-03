# End-to-End-Test der Makro-Schleife: startet dist\Kyklos.exe mit Test-Makros, löst sie über das Rad aus und prüft,
# was in einem eigenen Testfenster ankommt: feste Anzahl, Abfrage beim Auslösen, Abbruch der Abfrage, Abbruch mit Esc.
#
#   powershell -STA -ExecutionPolicy Bypass -File tools\e2e-makro-schleife.ps1
#
# Etwa 25 Sekunden nichts anfassen (Taste F13 als Auslöser). Kommt das Testfenster nicht nach vorn, bricht der Test ab,
# ohne zu tippen. Eine laufende Kyklos-Instanz vorher beenden (Kyklos.exe --quit).
$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$dir = Join-Path $env:TEMP 'kyklos-e2e-schleife'
New-Item -ItemType Directory -Force $dir | Out-Null
Copy-Item (Join-Path $repo 'dist\Kyklos.exe') $dir -Force
$json = @'
{ "version": 1, "settings": { "scale": 1 }, "wheels": [ { "id": "test", "name": "Test", "trigger": { "code": 124 }, "slots": [
  { "label": "Fest", "color": "#62ABFF", "action": { "type": "macro", "stepDelayMs": 20, "steps": [
      { "type": "text", "mode": "type", "text": "[" },
      { "type": "loop", "repeat": 3, "steps": [ { "type": "text", "mode": "type", "text": "a" }, { "type": "text", "mode": "type", "text": "b" } ] },
      { "type": "text", "mode": "type", "text": "]" } ] } },
  { "label": "Fragen", "color": "#F5C542", "action": { "type": "macro", "stepDelayMs": 20, "steps": [
      { "type": "text", "mode": "type", "text": "<" },
      { "type": "loop", "repeat": 2, "askRepeat": true, "steps": [ { "type": "text", "mode": "type", "text": "x" } ] },
      { "type": "text", "mode": "type", "text": ">" } ] } },
  { "label": "Lang", "color": "#5DD28B", "action": { "type": "macro", "stepDelayMs": 100, "steps": [
      { "type": "loop", "repeat": 200, "steps": [ { "type": "text", "mode": "type", "text": "z" } ] },
      { "type": "text", "mode": "type", "text": "ENDE" } ] } },
  { "label": "Leer", "color": "#FF7B7B", "action": { "type": "none" } }
] } ] }
'@
[System.IO.File]::WriteAllText((Join-Path $dir 'Kyklos.json'), $json, (New-Object System.Text.UTF8Encoding($false)))

Add-Type -AssemblyName System.Windows.Forms, System.Drawing
Add-Type -ReferencedAssemblies System.Windows.Forms @"
using System; using System.Runtime.InteropServices;
public static class U {
  [DllImport("user32.dll")] public static extern void keybd_event(byte vk, byte scan, uint flags, UIntPtr extra);
  [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
  [DllImport("user32.dll")] public static extern bool GetCursorPos(out POINT p);
  [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
  [DllImport("user32.dll", CharSet=CharSet.Unicode)] public static extern IntPtr FindWindow(string cls, string title);
  [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
  [DllImport("user32.dll", CharSet=CharSet.Unicode)] public static extern int GetWindowText(IntPtr h, System.Text.StringBuilder s, int n);
  public static string FrontTitle() { var s = new System.Text.StringBuilder(200); GetWindowText(GetForegroundWindow(), s, 200); return s.ToString(); }
  public struct POINT { public int x, y; }
}
// Zählt, wie oft Esc wirklich als Tastendruck beim Testfenster ankommt.
public sealed class EscCounter : System.Windows.Forms.IMessageFilter {
  public int Count;
  public bool PreFilterMessage(ref System.Windows.Forms.Message m) { if (m.Msg == 0x100 && (int)m.WParam == 0x1B) Count++; return false; }
}
"@
[void][U]::SetProcessDPIAware()
function Wait($ms) { $end = (Get-Date).AddMilliseconds($ms); while ((Get-Date) -lt $end) { [System.Windows.Forms.Application]::DoEvents(); Start-Sleep -Milliseconds 10 } }
function KeyDown($vk) { [U]::keybd_event($vk, 0, 0, [UIntPtr]::Zero) }
function KeyUp($vk) { [U]::keybd_event($vk, 0, 2, [UIntPtr]::Zero) }
function Tap($vk) { KeyDown $vk; Wait 40; KeyUp $vk; Wait 80 }
$results = New-Object System.Collections.ArrayList
function Check($name, $ok, $detail) { [void]$results.Add(('{0}  {1}  {2}' -f $(if ($ok) { 'OK    ' } else { 'FEHLER' }), $name, $detail)) }

$F13 = 0x7C
$ESC = 0x1B
$ENTER = 0x0D
$screen = [System.Windows.Forms.Screen]::PrimaryScreen.WorkingArea
$form = New-Object System.Windows.Forms.Form
$form.Text = 'Kyklos Schleife Testfenster'; $form.StartPosition = 'Manual'
$form.Size = New-Object System.Drawing.Size 700, 400
$form.Location = New-Object System.Drawing.Point (($screen.Left + ($screen.Width - 700) / 2), ($screen.Top + 80))
$tb = New-Object System.Windows.Forms.TextBox
$tb.Multiline = $true; $tb.Dock = 'Fill'; $tb.Font = New-Object System.Drawing.Font 'Segoe UI', 11
$form.Controls.Add($tb)

$orig = New-Object U+POINT; [void][U]::GetCursorPos([ref]$orig)
$t0 = Get-Date
$proc = Start-Process (Join-Path $dir 'Kyklos.exe') -PassThru
while (([U]::FindWindow([NullString]::Value, 'Kyklos-Rad') -eq [IntPtr]::Zero) -and ((Get-Date) -lt $t0.AddSeconds(40))) { Start-Sleep -Milliseconds 50 }
Start-Sleep -Milliseconds 600
try {
  $form.Show(); Wait 200; [void][U]::SetForegroundWindow($form.Handle); $tb.Focus() | Out-Null; Wait 300
  if ([U]::GetForegroundWindow() -ne $form.Handle) { throw 'Testfenster ist nicht im Vordergrund - nichts getippt.' }
  $r = $form.RectangleToScreen($form.ClientRectangle)
  $cx = $r.Left + $r.Width / 2; $cy = $r.Top + $r.Height / 2
  # Vier Segmente: oben, rechts, unten, links
  function Trigger($dx, $dy) {
    $tb.Clear(); [void][U]::SetCursorPos($cx, $cy); Wait 100
    KeyDown $F13; Wait 220; [void][U]::SetCursorPos($cx + $dx, $cy + $dy); Wait 150; KeyUp $F13
  }
  function PromptWindow { return [U]::GetForegroundWindow() -ne $form.Handle }

  # Gegenprobe: Esc kommt ohne laufende Schleife beim Programm an
  $counter = New-Object EscCounter
  [System.Windows.Forms.Application]::AddMessageFilter($counter)
  Tap $ESC; Wait 200
  Check 'Esc kommt vorher an' ($counter.Count -eq 1) "$($counter.Count)-mal"

  # 1: feste Anzahl
  Trigger 0 -130; Wait 1500
  Check 'Feste Anzahl, Makro läuft danach weiter' ($tb.Text -eq '[ababab]') ("'" + $tb.Text + "'")

  # 2: Abfrage mit Vorschlag übernommen: läuft erst nach der Bestätigung
  Trigger 130 0; Wait 800
  Check 'Abfrage hat den Fokus' (PromptWindow) ''
  Check 'Vor der Abfrage läuft kein Schritt' ($tb.Text -eq '') ("'" + $tb.Text + "'")
  Tap $ENTER; Wait 900
  Check 'Vorschlag 2 per Enter' ($tb.Text -eq '<xx>') ("'" + $tb.Text + "'")

  # 3: Abfrage mit getippter Zahl (Buchstabe wird ignoriert); der Fokus ist wieder im Testfenster
  Trigger 130 0; Wait 800
  Tap 0x41; Tap 0x35; Tap $ENTER; Wait 1000
  Check 'Eingegebene Anzahl 5' ($tb.Text -eq '<xxxxx>') ("'" + $tb.Text + "'")
  Check 'Fokus zurück im Programm' (-not (PromptWindow)) ''

  # 4: Null ist keine Anzahl: das Fenster bleibt, Esc bricht ab und nichts läuft
  Trigger 130 0; Wait 800
  Tap 0x30; Tap $ENTER; Wait 300
  Check 'Null wird abgelehnt' ((PromptWindow) -and $tb.Text -eq '') ("'" + $tb.Text + "'")
  Tap $ESC; Wait 800
  Check 'Abbruch startet das Makro nicht' ((-not (PromptWindow)) -and $tb.Text -eq '') ("'" + $tb.Text + "' vorn: " + [U]::FrontTitle())

  # 5: Esc mitten in der langen Schleife hält alles an, auch den Rest des Makros, und erreicht das Programm nicht
  Trigger 0 130; Wait 1200
  $before = $tb.Text.Length
  Tap $ESC; Wait 800
  $after = $tb.Text
  Wait 700
  Check 'Schleife lief vor dem Abbruch' ($before -gt 2) "$before Zeichen"
  Check 'Esc hält die Schleife an' ($after.Length -lt 60 -and $tb.Text -eq $after) "$($after.Length) Zeichen"
  Check 'Rest des Makros entfällt' ($tb.Text -notmatch 'ENDE') ''
  Check 'Esc erreicht das Programm nicht' ($counter.Count -eq 1) "$($counter.Count)-mal"

  # 6: Danach ist Esc wieder eine normale Taste
  Tap $ESC; Wait 200
  Check 'Esc danach wieder normal' ($counter.Count -eq 2) "$($counter.Count)-mal insgesamt"
}
finally {
  [void][U]::SetCursorPos($orig.x, $orig.y)
  Start-Process (Join-Path $dir 'Kyklos.exe') -ArgumentList '--quit' -Wait
  if (-not $proc.HasExited) { $proc.Kill() }
  $form.Close()
  $results | ForEach-Object { $_ }
}
