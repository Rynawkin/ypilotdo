# YolPilot Marketing

YolPilot marketing sitesi, `Next.js App Router` ile geliştirilmiş ürün anlatımı, lead toplama ve kampanya ölçümleme uygulamasıdır.

## Amaç

Site üç temel hedefi destekler:

1. YolPilot ürününü ve kullanım senaryolarını anlatmak
2. Demo ve teklif taleplerini toplamak
3. Reklam ve organik trafik performansını ölçmek

## Ana akışlar

- Ana dönüşüm hedefi: `Demo Talep Edin`
- Form hedefi: `/contact?intent=demo`
- İkincil keşif hedefi: `/features`
- Reklam ve içerik sayfaları için sektör bazlı landing page yapısı bulunur

## Ölçümleme

Site şu ölçümleme katmanlarını içerir:

- Yerleşik marketing analytics event toplama
- UTM parametrelerini saklama
- Visitor ve session kimlikleri üretme
- CTA tıklama ve form gönderim eventleri
- Super admin panelinde görüntülenebilen ziyaretçi / oturum / lead özeti

Opsiyonel üçüncü parti entegrasyonlar:

- `NEXT_PUBLIC_GA_MEASUREMENT_ID`
- `NEXT_PUBLIC_META_PIXEL_ID`

Bu environment değişkenleri tanımlandığında GA4 ve Meta Pixel scriptleri yüklenir.

## API proxy

Marketing site, backend çağrılarını Vercel üzerinden aşağıdaki rewrite ile iletir:

- `/api/proxy/:path*` -> `https://api.yolpilot.com/:path*`

Aktif konfigürasyon:

- `next.config.ts`

## Sayfa yapısı

Yüksek öncelikli satış sayfaları:

- `/`
- `/features`
- `/use-cases`
- `/industries/ecommerce`
- `/industries/logistics`
- `/industries/retail`
- `/industries/services`
- `/industries/food-delivery`
- `/industries/cold-chain`
- `/customers`
- `/security`
- `/integrations`
- `/comparison`
- `/contact`

## SEO

Site şu SEO yapılarını içerir:

- Global metadata
- Per-page metadata helper
- `robots.ts`
- `sitemap.ts`
- Canonical, OpenGraph ve Twitter metadata

## Geliştirme

```bash
npm install
npm run dev
```

## Production notları

- Vercel Root Directory: `yolpilot-marketing`
- Form eventleri backend tarafında `MarketingLead` ve `MarketingAnalyticsEvent` tablolarına gider
- Super admin analytics ekranı `app.yolpilot.com` içinde ayrı panelden görüntülenir
