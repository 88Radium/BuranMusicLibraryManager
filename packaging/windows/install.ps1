$ErrorActionPreference = 'Stop'

$Root = Split-Path -Parent $MyInvocation.MyCommand.Path
$Src  = $Root
if (-not (Test-Path (Join-Path $Src 'BuranUI.exe'))) {
    throw "BuranUI.exe wurde neben install.ps1 nicht gefunden: $Src"
}

$Dest = Join-Path $env:LOCALAPPDATA 'Programs\Buran'
New-Item -ItemType Directory -Force -Path $Dest | Out-Null

Get-ChildItem -LiteralPath $Src -Force | ForEach-Object {
    $name = $_.Name
    if ($name -in @('install.ps1', 'install.bat', 'uninstall.ps1', 'uninstall.bat', 'Liesmich.txt')) {
        return
    }
    $target = Join-Path $Dest $name
    if ($_.PSIsContainer) {
        Copy-Item -LiteralPath $_.FullName -Destination $target -Recurse -Force
    }
    else {
        Copy-Item -LiteralPath $_.FullName -Destination $target -Force
    }
}

Copy-Item -LiteralPath (Join-Path $Root 'uninstall.ps1') -Destination (Join-Path $Dest 'uninstall.ps1') -Force -ErrorAction SilentlyContinue
Copy-Item -LiteralPath (Join-Path $Root 'uninstall.bat') -Destination (Join-Path $Dest 'uninstall.bat') -Force -ErrorAction SilentlyContinue

$Programs = Join-Path $env:APPDATA 'Microsoft\Windows\Start Menu\Programs'
New-Item -ItemType Directory -Force -Path $Programs | Out-Null
$Wsh = New-Object -ComObject WScript.Shell
$lnk = Join-Path $Programs 'Buran Music Library Manager.lnk'
$Shortcut = $Wsh.CreateShortcut($lnk)
$Shortcut.TargetPath       = Join-Path $Dest 'BuranUI.exe'
$Shortcut.WorkingDirectory = $Dest
$Shortcut.WindowStyle      = 1
$Shortcut.Description      = 'Buran Music Library Manager'
$exe = Join-Path $Dest 'BuranUI.exe'
$Shortcut.IconLocation     = "$exe,0"
$Shortcut.Save()

Write-Host "Buran ist installiert in:"
Write-Host "  $Dest"
Write-Host "Startmenue: Buran Music Library Manager"
Write-Host ""
Write-Host "Einstellungen und Katalog bleiben unter:"
Write-Host "  $env:LOCALAPPDATA\Buran"
