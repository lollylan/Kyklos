# Erzeugt galerie\index.json aus den Rad-Dateien in galerie\raeder.
# Autor, Beschreibung und Programm stehen in jeder Rad-Datei unter "galerie" – so gibt es nur eine Stelle zu pflegen.
#
#   powershell -ExecutionPolicy Bypass -File tools\galerie-index.ps1

$ErrorActionPreference = 'Stop'
$dir = Join-Path (Split-Path -Parent $PSScriptRoot) 'galerie'
$list = @()
foreach ($f in Get-ChildItem (Join-Path $dir 'raeder') -Filter '*.json' | Sort-Object Name) {
    if ($f.Length -gt 256KB) { throw "$($f.Name) ist größer als 256 KB." }
    $d = Get-Content $f.FullName -Raw -Encoding UTF8 | ConvertFrom-Json
    if (-not $d.wheels -or $d.wheels.Count -eq 0) { throw "$($f.Name) enthält keine Räder." }
    $g = $d.galerie
    $list += [ordered]@{
        datei        = $f.Name
        name         = if ($g -and $g.name) { $g.name } else { $d.wheels[0].name }
        autor        = if ($g) { [string]$g.autor } else { '' }
        beschreibung = if ($g) { [string]$g.beschreibung } else { '' }
        programm     = if ($g) { [string]$g.programm } else { '' }
    }
}
$json = [ordered]@{ version = 1; raeder = $list } | ConvertTo-Json -Depth 5
[IO.File]::WriteAllText((Join-Path $dir 'index.json'), $json, (New-Object Text.UTF8Encoding $false))
"$($list.Count) Räder -> galerie\index.json"
