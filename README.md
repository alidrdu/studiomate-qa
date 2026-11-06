# StudioMate QA Otomasyon Paketi

[StudioMate](#test-edilen-uygulama) için hazırlanmış kalite güvence (QA) test paketi: **yerleşim ve ölçü hesapları için
birim/davranış testleri**, **arayüz duyarlılık testleri** ve testlerin gerçekten hata yakaladığını gösteren
**mutasyon kontrolü**.

> Bu depoda yalnızca test kodu, dokümantasyon ve sentetik (uydurma) test verisi vardır.
> Ürünün kaynak kodu, lisans mantığı, kurulum dosyaları, yapay zekâ modelleri ve gerçek kişi fotoğrafları **yoktur**.

## Test edilen uygulama
StudioMate, fotoğrafçı dükkanları için bir Windows masaüstü uygulamasıdır (C#, WinForms, .NET Framework 4, yerel
ONNX yapay zekâ modelleri). Üç sekmesi vardır: **Kimlik Fotoğrafı** (Biyometrik, Vesikalık, Schengen, GreenCard,
Çin vizesi), **Polo Kart** (sayfaya çoklu fotoğraf dizme) ve **Takvim**.

## Ne test ediliyor?
| Alan | Nasıl |
|------|-------|
| Polo Kart yerleşimi: sığan adet, adet +/−, aralık, eşit yay, tekli mod, kâğıt yönü | ondalık referans hesap + değişmezler + sınır değerleri |
| 5 belge türünün ölçüsü ve hazır/özel kâğıt düzeni | şartname değerleri + referans hesap + sınır değerleri |
| cm → piksel dönüşümü (300 / 600 / 700 dpi) ve yuvarlama | ondalık referansla birebir karşılaştırma |
| Arayüz: açılış, 3 sekme, sekme geçişi, belge türü seçimi, yeniden boyutlandırma, duyarlılık | kontrol **durumu** doğrulaması (ekran görüntüsü karşılaştırması yok) |
| Testlerin gücü | mutasyon kontrolü (bilerek bozulmuş kopyalarda testler düşüyor mu) |

Ayrıntı: [docs/kapsam-analizi.md](docs/kapsam-analizi.md) · [docs/test-plani.md](docs/test-plani.md)

## Son çalıştırma sonuçları
| Paket | Sonuç |
|-------|-------|
| Birim / davranış testleri | **25 test: 24 geçti, 0 kaldı, 0 hata, 1 gözlem** (244 148 doğrulama) |
| Arayüz testleri | **9 test: 9 geçti, 0 kaldı** (70 doğrulama) |
| Mutasyon kontrolü | bkz. [docs/mutasyon-sonucu.md](docs/mutasyon-sonucu.md) |

"Gözlem", başarısızlık sayılmayan, ürün sahibine sorulacak bir durumdur. Bkz. [docs/bulgular.md](docs/bulgular.md).
Ham çıktılar: [sonuclar/](sonuclar/).

## Nasıl çalıştırılır?
**Gereksinim:** Windows, .NET Framework 4 (Windows ile gelir, ek kurulum yok) ve uygulamanın **geliştirme
derlemesi** (`StudioMate.exe`; lisanssız, kod karıştırılmamış). Bu derleme depoda yoktur.
Kod karıştırılmış/lisanslı sürümlerde sınıf adları değiştiği için testler çalışmaz.

```powershell
# Birim / davranış testleri (derler ve çalıştırır)
powershell -ExecutionPolicy Bypass -File .\unit\run-unit.ps1 -Exe "C:\yol\StudioMate.exe"

# Arayüz testleri (pencere ekran dışında açılır)
powershell -ExecutionPolicy Bypass -File .\ui\ui-smoke.ps1 -Exe "C:\yol\StudioMate.exe"
```
Çıkış kodu: tüm testler geçtiyse `0`, aksi halde `1`. Sonuç dosyaları `sonuclar/` klasörüne yazılır.

## Klasör yapısı
```
unit/    SmQa.cs (test çalıştırıcı, C# 5) · run-unit.ps1
ui/      ui-smoke.ps1
docs/    kapsam-analizi · test-plani · bulgular · mutasyon-sonucu · hata-raporu-sablonu
sonuclar/  son çalıştırmaların ham çıktıları
```

## Yöntem (kısaca)
- Üretim koduna dokunulmadı. Uygulama yansıma (reflection) ile yüklenir ve gerçek metotları çağrılır.
- Beklenen değerler, üretim algoritmasının kopyası değil, **kural düzeyinde bağımsız hesaplardır** (ondalık aritmetik).
- Neden xUnit/NUnit değil de tek dosyalık çalıştırıcı: [docs/test-plani.md](docs/test-plani.md#2-yaklaşım-ve-gerekçe).

## Kapsam dışı (bilerek)
Yapay zekâ çıktısının görsel kalitesi, lisans/sunucu iletişimi, admin paneli, kurulum programı, yazdırma/kaydetme
iletişim kutuları, takvim düzen hesabı, yüz algılama ve ICAO kırpma kuralları. Gerekçeleri:
[docs/kapsam-analizi.md](docs/kapsam-analizi.md#3-kapsam-dışı-bilerek).

## Sınırlar (dürüstlük notu)
- Testler yalnızca **belirtilen sürümde** (`sonuclar/` dosyalarındaki sürüm) çalıştırıldı.
- Mutasyonları paketi hazırlayan kişi seçti; seçim yanlılığı olabilir. Tüm mutasyonlar yakalandı, ama bu "hata yok"
  anlamına gelmez.
- Referans hesaplar şartname *yorumunu* sınamaz, yalnızca uygulamanın o yoruma uyduğunu sınar.
