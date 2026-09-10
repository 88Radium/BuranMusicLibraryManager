$ErrorActionPreference = 'Stop'

$Dest = Join-Path $env:LOCALAPPDATA 'Programs\Buran'
$lnk  = Join-Path $env:APPDATA 'Microsoft\Windows\Start Menu\Programs\Buran Music Library Manager.lnk'

if (Test-Path -LiteralPath $lnk) {
    Remove-Item -LiteralPath $lnk -Force
}

if (Test-Path -LiteralPath $Dest) {
    Remove-Item -LiteralPath $Dest -Recurse -Force
}

Write-Host "Buran wurde entfernt ($Dest)."
Write-Host "Katalog und Einstellungen liegen noch unter $env:LOCALAPPDATA\Buran"
Write-Host "Diesen Ordner bei Bedarf manuell loeschen."
