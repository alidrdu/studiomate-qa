# Test Planı

## 1. Amaç
StudioMate'in **hesap ve yerleşim mantığının** (Polo Kart yerleşimi, 5 belge türünün ölçü ve kâğıt düzeni, cm→piksel
dönüşümü) ve **ana arayüz akışlarının** (açılış, 3 sekme, belge türü seçimi) doğru ve kararlı çalıştığını,
ürün kodunu değiştirmeden otomatik olarak doğrulamak.

## 2. Yaklaşım ve gerekçe

**Seçenek (a): modern .NET + xUnit/NUnit ayrı test projesi** — uygulanmadı. Uygulama .NET Framework 4 / C# 5
ile derlenen bir WinForms `.exe`'sidir ve yerleşim mantığı **form sınıfının özel (private) metotlarındadır**; bu
mantığa modern bir test projesinden erişmek için üretim kodunda değişiklik (sınıfı ayırmak, görünürlüğü açmak)
gerekirdi. Kural gereği üretim koduna dokunulmadı.

**Seçenek (b): `csc.exe` ile tek dosyalık test çalıştırıcı — seçildi.** Windows ile birlikte gelen derleyici
kullanılır, ek paket gerekmez. Test çalıştırıcı, uygulamayı **yansıma (reflection)** ile yükler, gerçek üretim
metotlarını çağırır (ör. `Build`, `ComputeLayout`, `IdPanel.Custom`, `IdPanel.Render`) ve sonuçları şunlarla karşılaştırır:

1. **Bağımsız referans hesap (oracle):** Testin içinde, belgelenmiş kuraldan ondalık (`decimal`) aritmetikle yazılmış
   basit hesaplar. Üretim kodu kayan nokta (`float`) kullandığı için yuvarlama hataları da bu yolla yakalanır.
   Referans, üretim algoritmasının kopyası değildir; "10 cm'ye kaç tane sığar" gibi kural düzeyindedir.
2. **Değişmezler (invariant):** Girdi ne olursa olsun doğru olması gereken özellikler. Örn. "fotoğraflar sayfa
   dışına taşmaz", "aralık negatif olmaz", "eşit yaymada kenar boşluğu = aralık", "istenen adet kadar fotoğraf yerleşir".
3. **Sınır değerleri:** 0, 1, en çok, en çok+1, çok büyük, negatif.

Üretim algoritmasının dengeli blok seçimi gibi buluşsal (heuristic) kısımları için tam değer yerine **özellik**
(adedi karşılar, ızgarayı aşmaz, boş satır bırakmaz) doğrulanır.

**Arayüz testleri** PowerShell + yansıma ile yapılır; **kontrol durumu** doğrulanır (hangi panel görünür, hangi sekme
seçili, kaç sekme var). Ekran görüntüsü karşılaştırması bilerek kullanılmaz.

## 3. Kapsam
Bkz. [kapsam-analizi.md](kapsam-analizi.md).

## 4. Test girdi verisi
- Tüm veriler **sentetik**: sayısal ölçüler ve test sırasında üretilen düz renk/gradyan bir PNG.
- Gerçek kişi fotoğrafı, müşteri verisi, model dosyası kullanılmaz.

## 5. Test listesi

### Birim / davranış testleri (`unit/SmQa.cs`) — 24 test + 1 gözlem
Gözlem: "dengeli blok mümkün olan en az boş hücreyi bırakıyor mu" taraması; başarısızlık saymaz, sonucu
[bulgular.md](bulgular.md) içinde.

| Grup | Test | Doğruladığı |
|------|------|-------------|
| Belge ölçüleri | 5 belge türünün fotoğraf ölçüsü | 45×60, 50×60, 35×45, 50×50, 33×48 mm |
| Hazır kâğıtlar | taşma yok | sütun/satır × ölçü kâğıda sığar |
| Hazır kâğıtlar | adetler | Vesikalık 4/6, Biyometrik 4/6, Schengen 4/6, GreenCard 4, Çin 4/6 |
| Hazır kâğıtlar | bitişik/aralıklı | Biyometrik ve GreenCard bitişik |
| Özel kâğıt | sığan adet | 5 tür × 22 × 22 kâğıt ölçüsü, ondalık referansla birebir |
| Özel kâğıt | sığmama | çok küçük kâğıtta `null` |
| Özel kâğıt | tam sığma sınırı | kâğıt = fotoğraf ölçüsü → 1 adet |
| Özel kâğıt | adet sınırları | 0, 1, en çok−1, en çok, en çok+1, 1000, negatif |
| Özel kâğıt | aralık sınırları | negatif, 0, 0,0001, çok büyük |
| Özel kâğıt | dengeli blok | adedi karşılar, ızgarayı aşmaz, boş satır yok |
| Polo Kart | sığdığı kadar adet | 19 × 19 ölçü, ondalık referansla birebir |
| Polo Kart | hazır ölçüler | 9×9, 8×8, 7×7, 6×6, 5×5 |
| Polo Kart | sığmayan ölçü | 15×15, 11×11 → 0 adet, çökme yok; 10×15 ve 15×10 → 1 adet |
| Polo Kart | sayfa içinde kalma | ölçü × mod × adet × aralık × eşit-yay (5000+ kombinasyon) |
| Polo Kart | adet seçimi | 0, 1, 2, max−1, max, max+1, 999, −1 |
| Polo Kart | tekli mod | 1 adet, yatayda ortalı |
| Polo Kart | elle aralık | en büyük sığan değere kırpma, grup ortalama |
| Polo Kart | eşit yay | dört kenarda boşluk = aralık |
| Polo Kart | kâğıt yönü | yatayda daha çok sığıyorsa yatay; eşitse dikey |
| Polo Kart | bitişik aralık | aralık 0 → tam 0 |
| Piksel dönüşümü | hazır kâğıt çıktısı | 300/600/700 dpi, ondalık referans |
| Piksel dönüşümü | bilinen değer | 10×15 cm @300 dpi = 1181×1772 px |
| Piksel dönüşümü | özel kâğıt yuvarlama | 10 farklı kâğıt boyutu |
| Piksel dönüşümü | tek fotoğraf çıktısı | 5 tür × 3 dpi, sentetik görsel |

### Arayüz testleri (`ui/ui-smoke.ps1`) — 9 test
Açılış, 3 sekme adı, sekme geçişi, 5 belge türü seçimi, seçimin korunması, geçersiz indeks, 30 geçişte duyarlılık,
yeniden boyutlandırma, Polo Kart ölçü listesi.

## 6. Giriş / çıkış ölçütleri
- **Giriş:** `StudioMate.exe` **geliştirme** derlemesi (lisanssız, kod karıştırılmamış) elde mevcut.
- **Çıkış:** Tüm testler geçer; mutasyon kontrolünde bilerek bozulan her davranış en az bir testi düşürür.

## 7. Nasıl çalıştırılır
Bkz. [../README.md](../README.md).

## 8. Bilinen sınırlar
- Testler uygulamanın **geliştirme derlemesine** ihtiyaç duyar (yansıma, kod karıştırmalı derlemede adlar
  değiştiği için çalışmaz). Derleme bu depoda yoktur.
- Referans hesaplar ve değişmezler, hatalı bir **şartname yorumunu** yakalayamaz; yalnızca hesabın şartnameye
  uygunluğunu doğrular.
- Duyarlılık testi, geçiş süresi için cömert bir üst sınır kullanır; performans ölçümü (benchmark) değildir.
