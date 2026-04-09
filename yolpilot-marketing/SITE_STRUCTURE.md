# YolPilot Marketing Site Structure

## Birincil hedef

Instagram reklamları, organik arama ve doğrudan ziyaretçiler için YolPilot'u güven veren, ürün odaklı ve dönüşüm odaklı bir yapıda anlatmak.

## Ana CTA sistemi

- Primary CTA: `Demo Talep Edin`
- Primary hedef: `/contact?intent=demo`
- Secondary CTA: `Platformu İnceleyin`
- Secondary hedefler:
  - `/features`
  - `/pricing`
  - `/faq`

Bu yapı özellikle reklam trafiğinde tek ana dönüşüm hattı oluşturmak için kullanılır.

## Temel sayfalar

### Ana sayfa `/`

- Hero
- Sosyal kanıt / güven katmanları
- Ürün değer önerileri
- Özellik önizlemesi
- Pricing önizlemesi
- Reklam SSS önizlemesi
- Güven göstergeleri
- Aralıklı ROI hesaplayıcısı

### Ürün sayfaları

- `/features`
- `/pricing`
- `/faq`
- `/comparison`
- `/integrations`
- `/security`
- `/customers`

Bu sayfalar genel bilgi sayfası değil, satışa destek olan landing page kalitesinde içerik taşır.

### Senaryo ve sektör sayfaları

- `/use-cases`
- `/industries/ecommerce`
- `/industries/logistics`
- `/industries/retail`
- `/industries/services`
- `/industries/food-delivery`
- `/industries/cold-chain`

Bu sayfalar reklam veya içerik trafiğini doğrudan karşılayacak şekilde tasarlanır:

- problem tanımı
- YolPilot yaklaşımı
- operasyon akışı
- CTA

### Dönüşüm sayfaları

- `/contact`
- `/start-trial`
- `/demo`

## Fiyatlandırma mantığı

- Fiyatlar aylık plan bazında görünür
- Ücretli planlarda ilk ödeme sırasında kart kaydı zorunludur
- Abonelik her ay otomatik yenilenir
- Tahsilat başarısız olursa 3 gün sonunda erişim kapanır
- Dahil kullanımı aşan durak ve WhatsApp bildirimleri ayrıca ücretlenir

## Ölçümleme yapısı

Marketing site şu eventleri toplar:

- `page_view`
- `cta_click`
- `form_submit`

Toplanan alanlar:

- `visitorId`
- `sessionId`
- `landingPage`
- `referrer`
- `utmSource`
- `utmMedium`
- `utmCampaign`
- `utmContent`
- `utmTerm`
- `deviceType`
- `browser`
- `os`
- `country`
- `region`
- `city`
- `ip`

## Super Admin görünürlüğü

`app.yolpilot.com` içinde super admin için ayrı analytics paneli bulunur:

- ziyaretçi sayısı
- oturum sayısı
- sayfa görüntüleme
- CTA tıklamaları
- form gönderimleri
- lead oranı
- top landing pages
- top campaigns
- recent sessions
- şehir / IP kırılımı
- recent leads

## SEO yapısı

- Global site metadata
- Per-page metadata helper
- Canonical
- OpenGraph
- Twitter cards
- `robots.ts`
- `sitemap.ts`

## İçerik prensipleri

- Türkçe karakterler düzgün kullanılmalı
- Genel pazarlama jargonu yerine operasyon odaklı net dil kullanılmalı
- Gerçekçi, ölçülü vaatler tercih edilmeli
- ROI alanı kesin sonuç değil, aralık ve varsayım mantığıyla anlatılmalı
- Pricing ve FAQ sayfaları reklam trafiği için görünür olmalı
- Görsel estetik kadar güven, açıklık ve dönüşüm mimarisi önemsenmeli
