# StudioMate QA - birim testlerini derler ve çalıştırır.
# Kullanım:  .\run-unit.ps1 -Exe "C:\yol\StudioMate.exe"
# Gereksinim: Windows + .NET Framework 4 (csc.exe Windows ile birlikte gelir). Ek paket gerekmez.
param(
    [Parameter(Mandatory = $true)][string]$Exe,
    [string]$Sonuc
)
$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [Text.Encoding]::UTF8
$klasor = Split-Path -Parent $MyInvocation.MyCommand.Path
if (-not $Sonuc) { $Sonuc = Join-Path $klasor '..\sonuclar\birim-sonuc.txt' }
$csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path $csc)) { $csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe' }
if (-not (Test-Path $Exe)) { throw "StudioMate.exe bulunamadı: $Exe" }

$derleme = Join-Path ([IO.Path]::GetTempPath()) 'smqa_build'
New-Item -ItemType Directory -Force $derleme | Out-Null
$runner = Join-Path $derleme 'SmQa.exe'
& $csc /nologo /codepage:65001 /target:exe "/out:$runner" /r:System.Windows.Forms.dll /r:System.Drawing.dll (Join-Path $klasor 'SmQa.cs')
if ($LASTEXITCODE -ne 0) { throw 'Derleme başarısız.' }

New-Item -ItemType Directory -Force (Split-Path $Sonuc) | Out-Null
& $runner (Resolve-Path $Exe).Path $Sonuc
exit $LASTEXITCODE
