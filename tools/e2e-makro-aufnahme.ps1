# End-to-End-Test der Makro-Aufnahme: startet dist\Kyklos.exe mit leerem Makro, drückt „Aufnehmen“,
# tippt echte Tasten in ein eigenes Testfenster, drückt „Stopp“ und prüft die gespeicherten Schritte.
#
#   powershell -STA -ExecutionPolicy Bypass -File tools\e2e-makro-aufnahme.ps1
#
# Etwa 15 Sekunden nichts anfassen. Eine laufende Kyklos-Instanz vorher beenden (Kyklos.exe --quit).
$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$dir = Join-Path $env:TEMP 'kyklos-e2e-makro'
New-Item -ItemType Directory -Force $dir | Out-Null
Copy-Item (Join-Path $repo 'dist\Kyklos.exe') $dir -Force
$cfg = Join-Path $dir 'Kyklos.json'
$json = '{ "version": 1, "settings": { "scale": 1 }, "wheels": [ { "id": "test", "name": "Test", "trigger": { "code": 124 }, ' +
        '"slots": [ { "label": "Makro", "action": { "type": "macro", "stepDelayMs": 60, "steps": [] } }, { "label": "Leer", "action": { "type": "none" } } ] } ] }'
[System.IO.File]::WriteAllText($cfg, $json, (New-Object System.Text.UTF8Encoding($false)))

Add-Type -AssemblyName System.Windows.Forms, System.Drawing, UIAutomationClient, UIAutomationTypes
Add-Type @"
using System; using System.Runtime.InteropServices;
public static class U {
  [DllImport("user32.dll")] public static extern void keybd_event(byte vk, byte scan, uint flags, UIntPtr extra);
  [DllImport("user32.dll")] public static extern void mouse_event(uint flags, uint dx, uint dy, uint data, UIntPtr extra);
  [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
  [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
  [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
}
"@
[void][U]::SetProcessDPIAware()
function Wait($ms) { $end = (Get-Date).AddMilliseconds($ms); while ((Get-Date) -lt $end) { [System.Windows.Forms.Application]::DoEvents(); Start-Sleep -Milliseconds 10 } }
function Tap($vk) { [U]::keybd_event($vk, 0, 0, [UIntPtr]::Zero); Wait 30; [U]::keybd_event($vk, 0, 2, [UIntPtr]::Zero); Wait 60 }
function TypeWord($s) { foreach ($c in $s.ToCharArray()) { if ([char]::IsUpper($c)) { [U]::keybd_event(0x10, 0, 0, [UIntPtr]::Zero) }
                                                          Tap ([byte][char]::ToUpper($c))
                                                          if ([char]::IsUpper($c)) { [U]::keybd_event(0x10, 0, 2, [UIntPtr]::Zero) } } }
$AE = [System.Windows.Automation.AutomationElement]
$TS = [System.Windows.Automation.TreeScope]
function FindIn($root, $prop, $value, $secs) {
  $cond = New-Object System.Windows.Automation.AndCondition (New-Object System.Windows.Automation.PropertyCondition $prop, $value),
                                                            (New-Object System.Windows.Automation.PropertyCondition $AE::ProcessIdProperty, $proc.Id)
  $end = (Get-Date).AddSeconds($secs)
  do { $e = $root.FindFirst($TS::Descendants, $cond); if ($e) { return $e }; Wait 200 } while ((Get-Date) -lt $end)
  return $null
}
function Invoke($e) { $e.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke() }
$results = New-Object System.Collections.ArrayList
function Check($name, $ok, $detail) { [void]$results.Add(('{0}  {1}  {2}' -f $(if ($ok) { 'OK    ' } else { 'FEHLER' }), $name, $detail)) }

$screen = [System.Windows.Forms.Screen]::PrimaryScreen.WorkingArea
$form = New-Object System.Windows.Forms.Form
$form.Text = 'Kyklos Makro-Aufnahme Testfenster'; $form.StartPosition = 'Manual'
$form.Size = New-Object System.Drawing.Size 700, 400
$form.Location = New-Object System.Drawing.Point (($screen.Left + ($screen.Width - 700) / 2), ($screen.Top + 80))
$tb = New-Object System.Windows.Forms.TextBox
$tb.Multiline = $true; $tb.Dock = 'Fill'; $tb.AcceptsTab = $true; $tb.Font = New-Object System.Drawing.Font 'Segoe UI', 11
$form.Controls.Add($tb)
$form.Show(); Wait 200

$proc = Start-Process (Join-Path $dir 'Kyklos.exe') -ArgumentList '--settings' -PassThru
try {
  $btn = FindIn $AE::RootElement $AE::AutomationIdProperty 'RecordMacroBtn' 30
  Check 'Knopf Aufnehmen sichtbar' ($btn -ne $null) ''
  if (-not $btn) { throw 'Abbruch' }
  Invoke $btn; Wait 700
  $stop = FindIn $AE::RootElement $AE::NameProperty 'Stopp' 5
  Check 'Aufnahmeleiste erscheint' ($stop -ne $null) ''
  if ($stop) {
    # Bildschirmfoto der Leiste zur Sichtprüfung: %TEMP%\kyklos-e2e-makro\leiste.png
    $r = $stop.Current.BoundingRectangle
    $bmp = New-Object System.Drawing.Bitmap 520, 90
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.CopyFromScreen([int]($r.Right - 500), [int]($r.Top - 30), 0, 0, $bmp.Size)
    $g.Dispose(); $bmp.Save((Join-Path $dir 'leiste.png')); $bmp.Dispose()
  }

  # Ein Klick ins Testfenster holt es nach vorn (das Klicken selbst wird nicht aufgenommen).
  $mid = $form.PointToScreen((New-Object System.Drawing.Point ($form.ClientSize.Width / 2), ($form.ClientSize.Height / 2)))
  [void][U]::SetCursorPos($mid.X, $mid.Y); Wait 80
  [U]::mouse_event(2, 0, 0, 0, [UIntPtr]::Zero); Wait 40; [U]::mouse_event(4, 0, 0, 0, [UIntPtr]::Zero); Wait 400
  if ([U]::GetForegroundWindow() -ne $form.Handle) { throw 'Testfenster ist nicht im Vordergrund - nichts getippt.' }

  # Standard: jeder Buchstabe ein eigener Tastendruck (Programme mit Buchstaben-Kürzeln)
  TypeWord 'ab'
  Wait 900
  Tap 0x09
  # Umschalten auf „Buchstaben als Text“: Getipptes wird ein Text-Schritt, die Rücktaste korrigiert ihn
  $switch = FindIn $AE::RootElement $AE::NameProperty 'Buchstaben als Text' 2
  Check 'Umschalter in der Leiste' ($switch -ne $null) ''
  # Echter Mausklick wie beim Menschen: Die Leiste nimmt keinen Fokus, das Testfenster bleibt vorn.
  $sr = $switch.Current.BoundingRectangle
  [void][U]::SetCursorPos([int]($sr.Left + 20), [int]($sr.Top + $sr.Height / 2)); Wait 80
  [U]::mouse_event(2, 0, 0, 0, [UIntPtr]::Zero); Wait 40; [U]::mouse_event(4, 0, 0, 0, [UIntPtr]::Zero); Wait 150
  Check 'Testfenster bleibt nach dem Umschalten vorn' ([U]::GetForegroundWindow() -eq $form.Handle) ''
  Check 'Umschalter ist an' ($switch.GetCurrentPattern([System.Windows.Automation.TogglePattern]::Pattern).Current.ToggleState -eq 'On') ''
  TypeWord 'Hallo'; Tap 0x20; TypeWord 'Weltt'; Tap 0x08
  Wait 300
  Check 'Tasten kommen im Programm an' ($tb.Text -eq "ab`tHallo Welt") ("'" + $tb.Text.Replace("`t", '<TAB>') + "'")

  Invoke $stop; Wait 2500
  $saved = Get-Content $cfg -Raw -Encoding UTF8 | ConvertFrom-Json
  $steps = @($saved.wheels[0].slots[0].action.steps)
  $desc = ($steps | ForEach-Object { if ($_.type -eq 'text') { "text:$($_.text)" } elseif ($_.type -eq 'delay') { "pause:$($_.delayMs)" } else { "keys:$($_.keys.code)" } }) -join ' | '
  # Das Umschalten selbst kann eine Pause vor dem Text ergeben – die zählt hier nicht.
  $core = @($steps | Select-Object -Skip 4 | Where-Object { $_.type -ne 'delay' })
  Check 'Schritte gespeichert' ($steps.Count -ge 5 -and $steps[0].keys.code -eq 65 -and $steps[1].keys.code -eq 66 -and $steps[2].type -eq 'delay' -and
                                $steps[2].delayMs -ge 800 -and $steps[2].delayMs -le 1300 -and $steps[3].keys.code -eq 9 -and
                                $core.Count -eq 1 -and $core[0].text -eq 'Hallo Welt') $desc
  $gone = $false; try { [void]$stop.Current.BoundingRectangle; $gone = $stop.Current.IsOffscreen } catch { $gone = $true }
  Check 'Aufnahmeleiste wieder weg' $gone ''
}
finally {
  Start-Process (Join-Path $dir 'Kyklos.exe') -ArgumentList '--quit' -Wait
  if (-not $proc.HasExited) { $proc.Kill() }
  $form.Close()
  $results | ForEach-Object { $_ }
}
