# Bulgular

Bu paketin çalıştırılması sırasında bulunanlar. **Doğrulanmış hata sayısı: 0.** Bir adet gözlem var.

| Kimlik | Başlık | Önem | Durum | Kanıt |
|--------|--------|------|-------|-------|
| BUL-001 | Özel kâğıtta dengeli blok seçimi, mümkün olan en az boş hücreyi her zaman bırakmıyor | Düşük | Açık – ürün sahibi kararı bekliyor (tasarım gereği olabilir) | aşağıda |

## BUL-001 – Dengeli blok seçimi bazı durumlarda 1 fazla boş hücre bırakıyor
**Önem:** Düşük. **Durum:** Açık (gözlem). Test başarısızlık saymaz.

**Ne gözlendi:** Özel kâğıtta "adet" seçilince uygulama, fotoğrafları sütun×satır bir blok olarak dizer. Blok
seçiminde "boş hücre sayısı" ve "ızgaranın en-boy oranına yakınlık" birlikte puanlanıyor. Çok geniş ya da çok
yüksek ızgaralarda oran farkı puana baskın gelip, hiç boş hücre bırakmayan seçeneğe tercih edilmiyor.

**Tekrar üretme (test çıktısından):** Schengen (35×45 mm) fotoğrafı, 50×70 cm özel kâğıt.
- 17 adet istenince blok 6×3 (18 hücre, **1 boş**); oysa 17×1 (0 boş) seçeneği kâğıda sığıyor.
- 19 adet istenince 5×4 (20 hücre, **1 boş**); 19×1 (0 boş) sığıyor.

Tarama: 5 belge türü × 3 kâğıt ölçüsü (21×29,7 · 30×40 · 50×70 cm) × tüm adetler = 1 232 durum; 2 durumda görüldü.

**Etki:** Taranan durumlarda yalnızca 50×70 cm kâğıtta ve iki adette görüldü (17 ve 19; kâğıt yan çevrilince 20 sütunlu
ızgara). Baskı yine doğru, yalnızca blok bir hücre fazla; en çok görünüm farkı yaratır.

**Polo Kart için not:** Polo Kart'ın yerleşim kodunda aynı puanlama formülü bulunuyor (koddan okunarak görüldü). Aynı
tarama Polo Kart için **çalıştırılmadı**; 10×15 cm sayfada en-boy oranı bu kadar uç olmadığı için pratik etkisi daha
da sınırlı olmalı, ama bu doğrulanmış bir sonuç değildir.

**Öneri (üretim koduna):** Boş hücre sayısı ile oran farkı ağırlıklarını gözden geçirmek ya da önce "en az boş hücre"
adaylarını seçip yalnızca bunlar arasında orana bakmak. Davranış kasıtlı ise gözlem kapatılabilir.

## Bulunmayanlar (dürüst not)
- Sayfa dışına taşma, negatif aralık, yanlış belge ölçüsü, yanlış piksel boyutu ya da yanlış panel açılması
  **bulunamadı**. Bu, çalıştırılan senaryolar için geçerlidir; test edilmeyen alanlar için bir iddia değildir
  ([kapsam dışı listesi](kapsam-analizi.md#3-kapsam-dışı-bilerek)).
