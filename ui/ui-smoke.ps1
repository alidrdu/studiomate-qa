# StudioMate QA - arayüz duyarlılık ve durum testleri (smoke)
# Yöntem: Uygulama yansıma (reflection) ile açılır, kontrollerin DURUMU doğrulanır (assert).
#         Ekran görüntüsü karşılaştırması KULLANILMAZ.
# Kullanım:  powershell -ExecutionPolicy Bypass -File .\ui-smoke.ps1 -Exe "C:\yol\StudioMate.exe"
# Not: Pencere ekran dışında (-5000,-5000) açılır, bilgisayarınızı rahatsız etmez. İlk açılış turu kapatılır (SM_NOTOUR=1).
param(
    [Parameter(Mandatory = $true)][string]$Exe,
    [string]$Sonuc,
    [int]$GecisSinirMs = 3000    # bir sekme geçişinin kabul edilen en uzun süresi (ms); cömert bir üst sınır
)
$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [Text.Encoding]::UTF8
if (-not $Sonuc) { $Sonuc = Join-Path (Split-Path -Parent $MyInvocation.MyCommand.Path) '..\sonuclar\ui-sonuc.txt' }
$env:SM_NOTOUR = '1'
Add-Type -AssemblyName System.Windows.Forms, System.Drawing
$bf = [Reflection.BindingFlags]'Public,NonPublic,Instance,Static'

$sonuclar = New-Object Collections.ArrayList
$assertSayisi = 0
function Assert-That($kosul, $mesaj) { $script:assertSayisi++; if (-not $kosul) { throw "BEKLENEN DEĞİL: $mesaj" } }
function Test($ad, [scriptblock]$govde) {
    $durum = 'GECTI'; $ayrinti = ''
    try { & $govde } catch { $durum = 'KALDI'; $ayrinti = $_.Exception.Message }
    $satir = "[$durum] $ad" + $(if ($ayrinti) { "  -> $ayrinti" } else { '' })
    Write-Host $satir
    [void]$sonuclar.Add([pscustomobject]@{ Durum = $durum; Satir = $satir })
}
function Pump { 1..3 | ForEach-Object { [Windows.Forms.Application]::DoEvents(); Start-Sleep -Milliseconds 60 } }
function Field($o, $n) { $o.GetType().GetField($n, $bf).GetValue($o) }

$asm = [Reflection.Assembly]::LoadFrom((Resolve-Path $Exe).Path)
$mt = $asm.GetType('PoloKart.MainForm')
$f = [Activator]::CreateInstance($mt, $true)
$f.StartPosition = 'Manual'; $f.Location = New-Object Drawing.Point(-5000, -5000); $f.ShowInTaskbar = $false
$f.Size = New-Object Drawing.Size(1400, 900)

$hostAdlari = 'cardHost', 'calHost', 'idHost', 'bioHost', 'schHost', 'gcHost', 'cnHost'
$hostlar = @{}
function GorunenHostlar { ($hostAdlari | Where-Object { $hostlar[$_].Visible }) }

try {
    Test 'Pencere açılıyor (handle var, görünür, başlık dolu)' {
        $f.Show(); Pump
        Assert-That $f.IsHandleCreated 'pencere handle oluşturmalı'
        Assert-That $f.Visible 'pencere görünür olmalı'
        Assert-That ($f.Text.Length -gt 0) 'pencere başlığı boş olmamalı'
        Assert-That ($f.Width -ge 800 -and $f.Height -ge 600) 'pencere makul boyutta olmalı'
    }

    foreach ($h in $hostAdlari) { $hostlar[$h] = Field $f $h }
    $ms = Field $f 'modeSwitch'
    $tur = $mt.GetMethod('SetIdType', $bf)

    Test 'Üst sekme anahtarında tam 3 sekme var: Kimlik Fotoğrafı, Polo Kart, Takvim' {
        $items = Field $ms 'items'
        Assert-That ($items.Count -eq 3) "3 sekme beklenirdi, bulunan: $($items.Count)"
        Assert-That ($items[0] -eq 'Kimlik Fotoğrafı') "1. sekme adı: $($items[0])"
        Assert-That ($items[1] -eq 'Polo Kart') "2. sekme adı: $($items[1])"
        Assert-That ($items[2] -eq 'Takvim') "3. sekme adı: $($items[2])"
    }

    Test 'Sekme geçişi: her sekmede tam olarak bir içerik paneli görünür ve doğru panel' {
        $ms.SelectedIndex = 1; Pump
        Assert-That ($ms.SelectedIndex -eq 1) 'Polo Kart sekmesi seçilmeli'
        Assert-That ((GorunenHostlar) -eq 'cardHost') "Polo sekmesinde yalnız cardHost görünür olmalı: $(GorunenHostlar)"
        $ms.SelectedIndex = 2; Pump
        Assert-That ((GorunenHostlar) -eq 'calHost') "Takvim sekmesinde yalnız calHost görünür olmalı: $(GorunenHostlar)"
        $ms.SelectedIndex = 0; Pump
        $g = @(GorunenHostlar)
        Assert-That ($g.Count -eq 1) "Kimlik sekmesinde tam 1 panel görünür olmalı: $g"
        Assert-That (@('idHost', 'bioHost', 'schHost', 'gcHost', 'cnHost') -contains $g[0]) "Kimlik sekmesi paneli yanlış: $g"
    }

    Test 'Belge türü seçimi: 5 türün her biri doğru paneli açar (Biyometrik, Vesikalık, Schengen, GreenCard, Çin)' {
        $ms.SelectedIndex = 0; Pump
        $beklenen = 'bioHost', 'idHost', 'schHost', 'gcHost', 'cnHost'   # SetIdType sırası: 0 Biyometrik, 1 Vesikalık, 2 Schengen, 3 GreenCard, 4 Çin
        foreach ($i in 0..4) {
            if ((Field $f 'idType') -eq $i) { [void]$tur.Invoke($f, @(($i + 1) % 5)); Pump }   # aynı indekse geçiş etkisiz olduğundan önce başka türe geç
            [void]$tur.Invoke($f, @($i)); Pump
            Assert-That ((Field $f 'idType') -eq $i) "idType=$i olmalı"
            Assert-That ((GorunenHostlar) -eq $beklenen[$i]) "tür $i için $($beklenen[$i]) görünür olmalı, görünen: $(GorunenHostlar)"
        }
    }

    Test 'Belge türü değişimi sekmeler arası dolaşınca korunur' {
        [void]$tur.Invoke($f, @(2)); $ms.SelectedIndex = 1; Pump; $ms.SelectedIndex = 0; Pump
        Assert-That ((Field $f 'idType') -eq 2) 'Schengen seçimi korunmalı'
        Assert-That ((GorunenHostlar) -eq 'schHost') 'dönüşte schHost görünmeli'
    }

    Test 'Geçersiz sekme indeksi yok sayılır (-1, 3, 99)' {
        $ms.SelectedIndex = 1; Pump
        foreach ($v in -1, 3, 99) { $ms.SelectedIndex = $v; Assert-That ($ms.SelectedIndex -eq 1) "indeks $v seçimi değiştirmemeli" }
        $ms.SelectedIndex = 0; Pump
    }

    Test "Duyarlılık: 30 sekme geçişi, her biri en çok $GecisSinirMs ms" {
        $enUzun = 0
        1..10 | ForEach-Object {
            foreach ($t in 1, 2, 0) {
                $sw = [Diagnostics.Stopwatch]::StartNew()
                $ms.SelectedIndex = $t; [Windows.Forms.Application]::DoEvents()
                $sw.Stop()
                if ($sw.ElapsedMilliseconds -gt $enUzun) { $enUzun = $sw.ElapsedMilliseconds }
                Assert-That ($sw.ElapsedMilliseconds -le $GecisSinirMs) "geçiş $($sw.ElapsedMilliseconds) ms sürdü (sınır $GecisSinirMs)"
            }
        }
        Write-Host "    (en uzun geçiş: $enUzun ms)"
    }

    Test 'Pencere yeniden boyutlandırılınca (1000x700 -> 1600x1000 -> 1400x900) paneller bozulmaz' {
        $ms.SelectedIndex = 1; Pump
        foreach ($b in @(@(1000, 700), @(1600, 1000), @(1400, 900))) {
            $f.Size = New-Object Drawing.Size($b[0], $b[1]); Pump
            Assert-That ((GorunenHostlar) -eq 'cardHost') "boyut $($b -join 'x') sonrası cardHost görünür kalmalı"
            Assert-That ($hostlar['cardHost'].Width -gt 300 -and $hostlar['cardHost'].Height -gt 200) "boyut $($b -join 'x') sonrası panel çökmemeli"
        }
        $ms.SelectedIndex = 0; Pump
    }

    Test 'Polo Kart ölçü listesi hazır ölçüleri içerir (9, 8, 7, 6, 5 cm ve Özel...)' {
        $cb = Field $f 'cbSize'
        $liste = @($cb.Items | ForEach-Object { $_.ToString() })
        foreach ($e in '9 x 9 cm', '8 x 8 cm', '7 x 7 cm', '6 x 6 cm', '5 x 5 cm', 'Özel...') { Assert-That ($liste -contains $e) "ölçü listesinde '$e' yok" }
    }
}
finally {
    try { $f.Close() } catch { }
}

$gecen = @($sonuclar | Where-Object { $_.Durum -eq 'GECTI' }).Count
$kalan = @($sonuclar | Where-Object { $_.Durum -ne 'GECTI' }).Count
$ozet = "TOPLAM: $($sonuclar.Count) test, $gecen geçti, $kalan kaldı; $assertSayisi doğrulama (assert) yapıldı."
Write-Host $ozet
New-Item -ItemType Directory -Force (Split-Path $Sonuc) | Out-Null
$basliklar = "# StudioMate QA arayüz testi sonucu`r`nTarih: $(Get-Date -Format 'yyyy-MM-dd HH:mm')`r`n$ozet`r`n`r`n"
[IO.File]::WriteAllText($Sonuc, $basliklar + (($sonuclar | ForEach-Object { $_.Satir }) -join "`r`n"), (New-Object Text.UTF8Encoding($true)))
if ($kalan -gt 0) { exit 1 } else { exit 0 }
