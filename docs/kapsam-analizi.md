# Kapsam Analizi

Bu belge, StudioMate'te **neyin test edilebilir olduğunu** ve neden bu şekilde test edildiğini açıklar.
Ürünün kaynak kodu bu depoda yoktur; analiz, uygulamanın davranışı ve belgelenmiş kurallar üzerinden yapılmıştır.

## 1. Test edilebilir saf mantık (arayüzden bağımsız hesaplar)

| # | Alan | Girdi | Çıktı | Test türü |
|---|------|-------|-------|-----------|
| M1 | Polo Kart: sayfaya sığan adet | fotoğraf ölçüsü (cm), 10×15 cm sayfa | sütun × satır, dikey/yatay yön | referans hesapla karşılaştırma |
| M2 | Polo Kart: adet seçimi (+/−) | istenen adet | sayfadaki adet, dengeli blok (sütun×satır) | değişmez (invariant) + sınır değerleri |
| M3 | Polo Kart: aralık | elle aralık (cm) | yatay/dikey aralık, grup konumu | sınır değerleri (0, çok küçük, çok büyük, negatif) |
| M4 | Polo Kart: eşit yay | – | kenar boşluğu = fotoğraflar arası boşluk | değişmez |
| M5 | Polo Kart: tekli mod | – | sayfada 1 fotoğraf, ortalı | değişmez |
| M6 | Kimlik: 5 belge türünün fotoğraf ölçüsü | belge türü | genişlik × yükseklik (cm) | şartname değerleriyle karşılaştırma |
| M7 | Kimlik: hazır kâğıtlar (4/6 adetlik) | belge türü | sütun, satır, bitişik/aralıklı | tablo doğrulaması + kâğıda sığma |
| M8 | Kimlik: özel kâğıt | kâğıt ölçüsü, adet, aralık, eşit yay | sığan adet, yön | referans hesap (ondalık), sınır değerleri |
| M9 | cm → piksel dönüşümü | cm, dpi (300/600/700) | piksel | ondalık referansla birebir karşılaştırma |

### Belge ölçüleri (test edilen beklenen değerler)

| Belge | Ölçü (mm) |
|-------|-----------|
| Vesikalık | 45 × 60 |
| Biyometrik | 50 × 60 |
| Schengen | 35 × 45 |
| GreenCard | 50 × 50 |
| Çin vizesi | 33 × 48 |

## 2. Arayüz akışları

| # | Akış | Doğrulanan durum |
|---|------|------------------|
| U1 | Uygulama açılışı | pencere oluştu, görünür, başlık dolu |
| U2 | Üst sekmeler | tam 3 sekme: Kimlik Fotoğrafı, Polo Kart, Takvim |
| U3 | Sekme geçişi | her sekmede tam bir içerik paneli görünür, doğru olan |
| U4 | Belge türü seçimi (5 tür) | her tür doğru paneli açar; seçim sekmeler arasında korunur |
| U5 | Geçersiz sekme indeksi | seçim değişmez |
| U6 | Duyarlılık | 30 sekme geçişi, hiçbiri süre sınırını aşmaz |
| U7 | Pencere yeniden boyutlandırma | paneller çökmez |
| U8 | Polo Kart ölçü listesi | hazır ölçüler listede |

## 3. Kapsam dışı (bilerek)

- **Yapay zekâ çıktısının görsel kalitesi** (arka plan beyazlatma, rötuş, kıyafet rengi): sonuç göz kararı değerlendirme
  gerektirir, ondan otomatik "doğru/yanlış" üretmedim. Bu depo gerçek insan fotoğrafı içermediği için de test verisi yok.
- **Lisans doğrulama, sunucu iletişimi, admin paneli:** güvenlik nedeniyle bu depoya girmez.
- **Kurulum programı (installer):** test edilmedi.
- **Yazdırma / dosyaya kaydetme akışı:** sistem iletişim kutuları gerektirdiği için otomatikleştirilmedi; yalnızca
  üretilen görüntünün **boyutu** (Render/RenderSingle) test edildi.
- **Takvim sekmesinin içeriği:** yalnızca sekmenin açılması test edildi; takvim düzen hesabı kapsam dışı.
- **Fotoğraf yüz algılama ve kırpma kuralları** (ICAO yüz oranları): sentetik veriyle anlamlı sınanamaz; kapsam dışı.
- **Ekran görüntüsü karşılaştırması:** bilerek kullanılmadı (kırılgan); durum doğrulaması tercih edildi.

## 4. Risk değerlendirmesi

| Risk | Olasılık | Etki | Karşılığı |
|------|----------|------|-----------|
| Yerleşim hesabında sayfa dışına taşma → müşteri baskısı kesilir | Orta | Yüksek | M1–M5 değişmezleri |
| Belge ölçüsü yanlış → vize/pasaport başvurusu reddedilir | Düşük | Çok yüksek | M6, M7, M9 |
| 1 piksel yuvarlama farkı → baskıda fark edilmez | Düşük | Düşük | M9 |
| Sekme / panel karışması | Düşük | Orta | U3, U4 |
