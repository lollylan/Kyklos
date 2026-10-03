# Baut dist\Kyklos.exe – eine einzelne, portable Exe für Windows 10/11.
#
# Voraussetzung: der Roslyn-C#-Compiler aus den "Build Tools für Visual Studio" (2019 oder neuer).
# Ein .NET SDK und NuGet-Pakete werden nicht gebraucht; die Exe läuft auf dem in Windows enthaltenen .NET Framework 4.8.
#
#   powershell -ExecutionPolicy Bypass -File build.ps1

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$src  = Join-Path $root 'src'
$dist = Join-Path $root 'dist'
$icon = Join-Path $root 'assets\app.ico'

function Find-Csc {
    $vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
    if (Test-Path $vswhere) {
        foreach ($path in (& $vswhere -products * -property installationPath)) {
            $csc = Join-Path $path 'MSBuild\Current\Bin\Roslyn\csc.exe'
            if (Test-Path $csc) { return $csc }
        }
    }
    throw 'Kein Roslyn-C#-Compiler gefunden. Bitte die "Build Tools für Visual Studio" (2019 oder neuer) installieren.'
}

function Invoke-Csc([string]$out, [bool]$withIcon) {
    $fw = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
    $refs = 'mscorlib.dll', 'System.dll', 'System.Core.dll', 'System.Xml.dll', 'System.Xaml.dll', 'System.Drawing.dll', 'System.Windows.Forms.dll',
            'WPF\WindowsBase.dll', 'WPF\PresentationCore.dll', 'WPF\PresentationFramework.dll'
    $cscArgs = @(
        '/nologo', '/target:winexe', '/platform:anycpu', '/optimize+', '/debug-', '/langversion:9.0', '/codepage:65001',
        '/nostdlib+', '/noconfig', '/warn:4', '/nowarn:1701,1702',
        "/out:$out",
        "/win32manifest:$(Join-Path $src 'app.manifest')",
        "/resource:$(Join-Path $src 'Theme.xaml'),Kyklos.Theme.xaml",
        "/resource:$(Join-Path $src 'SettingsWindow.xaml'),Kyklos.SettingsWindow.xaml"
    )
    if ($withIcon) {
        $cscArgs += "/win32icon:$icon"
        $cscArgs += "/resource:$icon,Kyklos.app.ico"
    }
    $cscArgs += $refs | ForEach-Object { "/r:$(Join-Path $fw $_)" }
    $cscArgs += Get-ChildItem (Join-Path $src '*.cs') | ForEach-Object { $_.FullName }
    & $script:csc @cscArgs
    if ($LASTEXITCODE -ne 0) { throw "Kompilieren fehlgeschlagen (Exitcode $LASTEXITCODE)." }
}

$script:csc = Find-Csc
New-Item -ItemType Directory -Force $dist, (Split-Path $icon) | Out-Null

# Das Programmsymbol zeichnet die App selbst. Fehlt es, wird einmal ohne Symbol gebaut, um es zu erzeugen.
if (-not (Test-Path $icon)) {
    $bootstrap = Join-Path $env:TEMP 'Kyklos-bootstrap.exe'
    Invoke-Csc $bootstrap $false
    $p = Start-Process $bootstrap -ArgumentList '--dev-icon', "`"$icon`"" -Wait -PassThru
    Remove-Item $bootstrap -ErrorAction SilentlyContinue
    if ($p.ExitCode -ne 0 -or -not (Test-Path $icon)) { throw 'Das Programmsymbol konnte nicht erzeugt werden.' }
}

$exe = Join-Path $dist 'Kyklos.exe'
Invoke-Csc $exe $true
'{0}  ({1:N0} KB)' -f $exe, ((Get-Item $exe).Length / 1KB)
