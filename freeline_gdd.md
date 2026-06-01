# 🎨 Game Design Document — Codename: *Freeline*

## 1. Oyun Kimliği

| | |
|---|---|
| **Tür** | Cozy Yaşam Simülasyonu / Idle Hybrid |
| **Platform** | iOS & Android |
| **Oryantasyon** | Dikey (Portrait) |
| **Hedef Kitle** | 16-30 yaş, dijital sanat ve cozy oyun kültürüne aşina oyuncular |
| **Ton** | Sıcak, motivasyonel, stressiz |
| **Referans Hissi** | Unpacking + Coffee Inc + Webtoon kültürü |

---

## 2. Core Loop

```
Görev Al → Çiz (zaman harca + enerji harca) → Para Kazan
→ Yemek ye / Uyu → Oda/Karakter Geliştir → Daha İyi Görev Al
```

Bu döngü içine webtoon pasif geliri ve fatura mekaniği oturur. Oyuncu her gün biraz daha iyi bir freelancer olma hissiyle uyanır.

---

## 3. Zaman & Gün Sistemi

Oyun **24 saatlik bir iç saatle** ilerler. Gerçek zamanlı değil, görev bazlı ilerler.

- Sabah **09:00'da** oyuncu uyanır, gün başlar.
- Her görevin üzerinde kaç saat süreceği yazar.
- Saat **21:00–24:00** arası oyuncu isteğe bağlı uyuyabilir.
- Saat **24:00'de** otomatik uyku gelir. O an devam eden görev yarıda kalır, sabah kaldığı yerden devam edilir.
- Uyumak → **Gün Sonu Raporunu** tetikler.
- Görev ücreti tamamlanınca ödenir, yarıda bırakılan iş için ödeme yapılmaz.

---

## 4. Core Mechanics

### 4.1 Freelance Çizim Görevleri

Oyunun temel para kazanma mekaniği. Üç seviyeye bölünür, oyuncu ilerledikçe yeni görev tipleri açılır.

**Seviye 1 — Başlangıç:**
- Slide bar: ekrana basılı tut, bar dolunca görev tamamlanır.

**Seviye 2 — Orta:**
- Line trace: ekranda beliren çizgiyi parmakla takip et.

**Seviye 3 — İleri:**
- Basit boyama: verilen alana renk uygula (taşma = kalite düşer).

Her görevin üzerinde şunlar yazar: süre (saat), ödeme miktarı, zorluk seviyesi. Oyuncu işi almak ya da reddetmek için 3 görev arasından seçim yapar (refresh hakkı sınırlı).

### 4.2 Webtoon Sistemi

Pasif gelir ve uzun vadeli büyüme mekaniği.

- Oyuncu belirli saatler harcayarak **webtoon bölümü üretir** (freelance görevden farklı, daha uzun sürer).
- Bölüm yayınlanınca **takipçi kazanma/kaybetme** hesaplanır. Etkileyen faktörler: bölüm kalitesi (harcanan zaman + ekipman), yayın sıklığı, reklam/viral şans.
- Takipçi sayısına göre **günlük pasif gelir** akar (reklam geliri simülasyonu).
- Uzun süre bölüm yayınlanmazsa takipçi azalır.
- Gün sonu raporunda takipçi değişimi ve webtoon geliri ayrı gösterilir.

### 4.3 Enerji Sistemi

Tek bar: **Enerji.** Açlık ayrı bar değil, enerji sisteminin girdisi.

- Görev yaptıkça enerji azalır.
- Uzun süre yemek yenmezse enerji yenilenme hızı düşer (görsel ipucu: karakter animasyonu değişir, ses efekti).
- Yemek/içecek tüketmek enerji yeniler + buff verir.
- Uyumak enerjiyi tamamen doldurur.
- Enerji sıfırlanınca yeni görev alınamaz, sadece uyuma veya yemek seçeneği aktif olur.
- Cozy kural: Enerji bitmesi "game over" değil, "dur biraz dinlen" sinyali.

### 4.4 Yemek & Tüketim Öğeleri

Anlık satın alım ve oyun içi para ile erişilir.

| Öğe | Etki |
|---|---|
| Kahve | Hız buff (kısa süreli) + enerji yenile |
| Enerji İçeceği | Güçlü hız buff + yüksek enerji yenile |
| Hamburger | Yüksek enerji yenile + uzun tokluk |
| Tatlı | Orta enerji + viral şans micro-buff |

---

## 5. Ekonomi & Para Harcama

### 5.1 Para Kaynakları

- Freelance görev ödemeleri (birincil)
- Webtoon pasif geliri (ikincil, büyür)
- Görev erken bitirme bahşişi (yetenek ile açılır)

### 5.2 Harcama Kategorileri

**Dekorasyon**
Bilgisayardan online mağazaya sipariş verilir. Minimum 1 gün sonra kargo gelir, otomatik yerleşir. Pasif özellik bonusları verir.

Örnekler: kaliteli yatak (uyku sonrası enerji +), kitaplık (webtoon kalite +), ergonomik sandalye (enerji tüketimi -), sarı lamba (görev buff), saksı/halı (dekoratif + küçük morale).

**Upgrade**
Ekipman yükseltmeleri. Pahalı, uzun vadeli yatırım. Doğrudan kazancı etkiler.

Örnekler: daha iyi tablet (line trace hassasiyeti +, görev ücreti +), güçlü bilgisayar (webtoon üretim hızı +), kaliteli monitör (boyama görevi kalitesi +).

**Yetenek Ağacı**
Para ile alınan pasif beceriler. Oyunun tematik dünyasına yedirilmiş şekilde sunulur ("Çizim Kursu aldın", "YouTube videosu izledin" gibi).

Örnek yetenekler: çizim hızı +, enerji tüketimi -, erken bitirme bahşişi, webtoon viral şans +, görev refresh hakkı +, gece geç saate kadar enerji cezasız çalışma.

---

## 6. Fatura & Gider Sistemi

Oyuncuya düzenli gider baskısı verir ama cezalandırmaz.

- **Günlük:** Yemek masrafları (aktif olarak satın alınır).
- **Haftalık:** Elektrik, internet, uygulama abonelikleri. "Zarf geldi" animasyonu ile bildirim yapılır.
- **Aylık:** Kira. Büyük zarf, özel animasyon.
- Ödeme yapılamazsa 2 gün uyarı süresi gelir. Bu sürede ödenmezse küçük bir debuff uygulanır (morale düşüklüğü gibi), hard ceza yok.
- Gün sonu raporunda tüm gelir/gider özeti gösterilir.

---

## 7. Gün Sonu Raporu

Uyuma anında tetiklenir. Kısa bir animasyon eşliğinde gelir.

İçerik:
- Günlük kazanç / gider / net bakiye
- Webtoon: yeni takipçi sayısı, takipçi değişimi, günlük pasif gelir
- Tamamlanan görev sayısı ve toplam çizim süresi
- Açılan yetenek veya milestone varsa kutlama bildirimi
- Yaklaşan fatura veya kargo hatırlatması
- Haftanın özeti (her 7. günde ek haftalık rapor)

---

## 8. Monetization

**Para Birimi:** Oyun içi coin (kazanılır) + Gem (premium)

**Gem Kazanma:**
- Belirli milestone'lar (ilk 100 takipçi, ilk upgrade vs.)
- Günlük giriş (çok az, Clash of Clans modeli)
- Özel görevler / eventler
- Satın alma

**Gem Harcama:**
- Sadece gem ile gelen özel dekorasyon/kostüm paketleri
- Reklam kaldırma (kalıcı, tek seferlik satın alım)
- Kargo süresini kısaltma
- Sınırlı sezonluk içerik

**Reklam Modeli:**
- Reklam izle → o anki görevi için 2x para
- Reklam izle → enerji yenile
- Reklam izle → kargo süresini yarıya indir
- Reklam zorla çıkmaz, her zaman oyuncu inisiyatifiyle

**Starter Pack:** İlk 3 gün içinde bir kez gösterilir. Düşük fiyat, yüksek değer (gem + özel dekorasyon + coin).

**Sezonluk Paketler:** "Kış Atölyesi", "Lo-fi Stüdyo" gibi temalı dekorasyon setleri, gem ile.

---

## 9. Kapsam Planı

**V1.0 — MVP:**
Freelance sistem, webtoon sistemi, enerji/yemek/uyku, saat & gün döngüsü, gün sonu raporu, dekorasyon & kargo, upgrade sistemi, yetenek ağacı, fatura mekaniği, temel monetization.

**V1.5 — İlk Büyük Güncelleme:**
Sergi & dükkan sistemi, müşteri diyalog mekaniği, sergi için eser üretimi, sezonluk içerik altyapısı.

---