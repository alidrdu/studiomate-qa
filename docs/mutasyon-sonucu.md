# Mutasyon Kontrolü

**Amaç:** Testlerin gerçekten hata yakaladığını göstermek. Ürünün **bir kopyasında** bilerek küçük bozulmalar
(mutasyon) yapıldı, kopya derlendi ve test paketleri çalıştırıldı. Üretim koduna dokunulmadı; kopyalar geçici klasörde
yapıldı ve silindi.

**Sonuç:** Kontrol (bozulmamış kopya) tüm testlerden geçti. **15 bozulmanın 15'i en az bir test tarafından yakalandı.**

**Sınırlar:**
- 15 bozulmayı ben seçtim (seçim yanlılığı olabilir). Hepsinin yakalanması, her olası hatanın yakalanacağı anlamına gelmez.
- Eşdeğer (davranışı değiştirmeyen) mutant bu listede yok; böyle bir mutant "kaçtı" görünürdü ve ayrıca incelenirdi.
- Mutasyon betiği üretim kaynağından kısa kod parçaları içerdiği için bu depoya **konmadı**; yalnızca sonuçlar paylaşıldı.
- Test adlarındaki "(19x19 ölçü)" gibi ifadeler, o testin denediği ölçü ızgarasını gösterir.
| Kod | Bozulma | Sonuç | Düşen testler |
|---|---|---|---|
| M0 | KONTROL: bozulmamış kopya (tüm testler geçmeli) | KONTROL GEÇTİ (0 test düştü) | - |
| M1 | Polo: sütun sayısı Floor yerine Ceiling | YAKALANDI (7 test düştü) | `Polo Kart / Çoklu mod: sığdığı kadar adet, ondalık referans hesapla birebir aynı (19x19 ölçü)`<br>`Polo Kart / Hazır ölçüler (9x9 ve 8x8 tek fotoğraf, 7x7, 6x6, 5x5) sayfa başı adet`<br>`Polo Kart / Sayfaya sığmayan ölçü: 0 adet, çökmeden`<br>`Polo Kart / Tüm yerleşimler sayfa içinde kalır ve aralıklar negatif olmaz (ölçü x mod x adet x aralık x eşit-yay)`<br>`Polo Kart / Eşit yay: kenar boşlukları = fotoğraflar arası boşluk (yatay ve dikey ayrı ayrı)`<br>`Polo Kart / Kâğıt yönü: yatayda daha çok sığıyorsa yatay seçilir, eşitse dikey`<br>`Polo Kart / Aralık 0 (bitişik) ve varsayılan aralık: ikisi de sayfa içinde, bitişikte aralık tam 0` |
| M2 | Polo: satır sayısı yanlış ölçüye (genişliğe) bölünüyor | YAKALANDI (4 test düştü) | `Polo Kart / Çoklu mod: sığdığı kadar adet, ondalık referans hesapla birebir aynı (19x19 ölçü)`<br>`Polo Kart / Sayfaya sığmayan ölçü: 0 adet, çökmeden`<br>`Polo Kart / Tüm yerleşimler sayfa içinde kalır ve aralıklar negatif olmaz (ölçü x mod x adet x aralık x eşit-yay)`<br>`Polo Kart / Kâğıt yönü: yatayda daha çok sığıyorsa yatay seçilir, eşitse dikey` |
| M3 | Polo: eşit yaymada yatay boşluk bölen (Cols+1) yerine (Cols+2) | YAKALANDI (1 test düştü) | `Polo Kart / Eşit yay: kenar boşlukları = fotoğraflar arası boşluk (yatay ve dikey ayrı ayrı)` |
| M4 | Polo: elle aralık sığabilecek en büyük değere kırpılmıyor | YAKALANDI (2 test düştü) | `Polo Kart / Tüm yerleşimler sayfa içinde kalır ve aralıklar negatif olmaz (ölçü x mod x adet x aralık x eşit-yay)`<br>`Polo Kart / Elle aralık: istenen değer, o yönde sığabilecek en büyük değerle sınırlanır; grup ortalı` |
| M5 | Polo: yön seçiminde eşitlikte de yatay seçiliyor (> yerine >=) | YAKALANDI (1 test düştü) | `Polo Kart / Kâğıt yönü: yatayda daha çok sığıyorsa yatay seçilir, eşitse dikey` |
| M6 | Polo: istenen adet bir fazla uygulanıyor (Used = adet+1) | YAKALANDI (1 test düştü) | `Polo Kart / Adet seçimi: 0, 1, max-1, max, max+1 -> sayfadaki adet = min(n, max); adet 0 = sığdığı kadar` |
| M7 | Kimlik: özel kâğıtta yön seçimi ters (> yerine <) | YAKALANDI (2 test düştü) | `Özel kâğıt / Sığan adet, ondalık referans hesapla birebir aynı (5 tür x 29x29 ölçü)`<br>`Özel kâğıt / Tam sığma sınırı: ölçü kâğıda tam eşitse 1 adet sığar` |
| M8 | Kimlik: Schengen fotoğraf genişliği 35 mm yerine 36 mm | YAKALANDI (2 test düştü) | `Belge ölçüleri / 5 belge türünün fotoğraf ölçüsü şartnameyle aynı`<br>`Piksel dönüşümü / Tek fotoğraf çıktısı (RenderSingle): her belge türü ve dpi'da ölçü = referans (sentetik görsel)` |
| M9 | Piksel: sayfa boyutu yuvarlama yerine aşağı kırpma (Round -> Floor) | YAKALANDI (3 test düştü) | `Piksel dönüşümü / Hazır kâğıtların çıktı boyutu = cm x dpi / 2,54 (300, 600, 700 dpi)`<br>`Piksel dönüşümü / Bilinen referans değerler: 10x15 cm @300 dpi = 1181x1772 px`<br>`Piksel dönüşümü / Özel kâğıt boyutlarında (0,1 cm adımlı örnekler) yuvarlama referansla aynı: en çok ±0 piksel` |
| M10 | Piksel: tek fotoğraf çıktısı aşağı kırpılıyor (Round -> Floor) | YAKALANDI (1 test düştü) | `Piksel dönüşümü / Tek fotoğraf çıktısı (RenderSingle): her belge türü ve dpi'da ölçü = referans (sentetik görsel)` |
| M11 | Kimlik: özel kâğıtta adet sınırı bir eksik (count < max-1) | YAKALANDI (1 test düştü) | `Özel kâğıt / Adet 0, 1, tam max, max+1, çok büyük ve negatif` |
| M12 | Kimlik: biyometrik 6 adetlik hazır kâğıtta sütun 3 yerine 4 | YAKALANDI (2 test düştü) | `Hazır kâğıtlar / Her hazır kâğıtta fotoğraflar kâğıda sığıyor (taşma yok)`<br>`Hazır kâğıtlar / Hazır kâğıt adetleri (Vesikalık 4/6, Biyometrik 4/6, Schengen 4/6, GreenCard 4, Çin 4/6)` |
| M13 | Arayüz: GreenCard seçilince Çin paneli açılıyor | YAKALANDI (1 test düştü) | `Belge türü seçimi: 5 türün her biri doğru paneli açar (Biyometrik, Vesikalık, Schengen, GreenCard, Çin)` |
| M14 | Arayüz: 2. sekme adı "Polo Kart" yerine "Polo Kartı" | YAKALANDI (1 test düştü) | `Üst sekme anahtarında tam 3 sekme var: Kimlik Fotoğrafı, Polo Kart, Takvim` |
| M15 | Arayüz: sekme geçişi 3,5 sn takılıyor (duyarlılık) | YAKALANDI (1 test düştü) | `Duyarlılık: 30 sekme geçişi, her biri en çok 3000 ms` |
