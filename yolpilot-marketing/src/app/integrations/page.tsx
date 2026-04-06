import TrackedLink from '@/components/ui/TrackedLink';
import { PRIMARY_CTA_HREF } from '@/lib/marketing';
import { createPageMetadata } from '@/lib/seo';

export const metadata = createPageMetadata({
  title: 'Entegrasyonlar',
  description: 'ERP, CRM, e-ticaret, harita ve webhook akışlarıyla YolPilot entegrasyon yaklaşımını keşfedin.',
  path: '/integrations',
  keywords: ['lojistik entegrasyon', 'rota optimizasyon API', 'ERP entegrasyonu', 'webhook']
});

const categories = [
  {
    title: 'Sipariş ve e-ticaret sistemleri',
    description: 'Sipariş, müşteri ve teslimat verisini operasyon paneline taşıyın.'
  },
  {
    title: 'ERP ve kurumsal veri akışları',
    description: 'Sefer, teslimat ve operasyon bilgisini mevcut sistemlerle eşleyin.'
  },
  {
    title: 'CRM ve saha süreçleri',
    description: 'Müşteri verisini saha akışı ve bilgilendirme süreçleriyle birleştirin.'
  },
  {
    title: 'Harita ve navigasyon servisleri',
    description: 'Adres, rota ve navigasyon akışlarını mevcut servislerle uyumlu hale getirin.'
  },
  {
    title: 'Bildirim kanalları',
    description: 'WhatsApp, e-posta veya başka iletişim kanallarını senaryonuza göre bağlayın.'
  },
  {
    title: 'Dosya aktarımı ve dışa aktarma',
    description: 'CSV benzeri akışlar ve rapor dışa aktarma ihtiyaçlarını standardize edin.'
  }
];

const flow = [
  {
    title: 'Mevcut veri kaynakları çıkarılır',
    description: 'Sipariş, müşteri, ürün, araç veya saha verisinin nereden geldiği netleştirilir.'
  },
  {
    title: 'Uygun entegrasyon modeli seçilir',
    description: 'API, webhook, dosya aktarımı veya hibrit yapı arasında operasyonunuza uygun yöntem belirlenir.'
  },
  {
    title: 'Test ve canlı geçiş planlanır',
    description: 'Önce doğrulama yapılır, sonra operasyonu aksatmadan canlıya alınır.'
  }
];

const apiHighlights = [
  'REST API ile sipariş ve müşteri aktarımı',
  'Webhook ile durum ve teslimat sonucu tetikleme',
  'Test ortamı ile doğrulama süreci',
  'Yetki ve anahtar yönetimi',
  'Dışa aktarım ve rapor akışları'
];

export default function IntegrationsPage() {
  return (
    <div className="min-h-screen bg-gray-50 pt-24">
      <section className="bg-gradient-to-br from-blue-600 via-blue-700 to-indigo-700 py-20 text-white">
        <div className="mx-auto max-w-6xl px-4 sm:px-6 lg:px-8">
          <div className="max-w-4xl">
            <div className="inline-flex rounded-full bg-white/15 px-4 py-2 text-sm font-semibold text-blue-50">
              Entegrasyon yaklaşımı
            </div>
            <h1 className="mt-6 text-4xl font-bold leading-tight lg:text-5xl">
              Mevcut sistemlerinizi değiştirmeden operasyon akışını tek yerde toplayın
            </h1>
            <p className="mt-6 max-w-3xl text-lg leading-8 text-blue-100">
              YolPilot; sipariş, müşteri, teslimat ve operasyon verisini farklı kaynaklardan alıp planlama ve saha akışına bağlayacak esnek entegrasyon seçenekleri sunar.
            </p>
            <div className="mt-8">
              <TrackedLink
                href={PRIMARY_CTA_HREF}
                trackingName="integrations-primary-cta"
                className="inline-flex items-center justify-center rounded-xl bg-white px-8 py-4 text-base font-semibold text-blue-700 transition hover:bg-blue-50"
              >
                Entegrasyon Görüşmesi Planlayın
              </TrackedLink>
            </div>
          </div>
        </div>
      </section>

      <section className="bg-white py-16">
        <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
          <div className="grid gap-6 md:grid-cols-2 xl:grid-cols-3">
            {categories.map((item) => (
              <div key={item.title} className="rounded-2xl border border-gray-200 bg-gray-50 p-6 shadow-sm">
                <h2 className="text-xl font-semibold text-gray-900">{item.title}</h2>
                <p className="mt-3 text-sm leading-6 text-gray-600">{item.description}</p>
              </div>
            ))}
          </div>
        </div>
      </section>

      <section className="py-16">
        <div className="mx-auto max-w-6xl px-4 sm:px-6 lg:px-8">
          <div className="grid gap-10 lg:grid-cols-[minmax(0,0.9fr)_minmax(0,1.1fr)]">
            <div>
              <div className="text-sm font-semibold uppercase tracking-[0.18em] text-blue-600">API ve veri akışı</div>
              <h2 className="mt-3 text-3xl font-bold text-gray-900">Teknik ekip için sade, operasyon için güvenilir veri akışı</h2>
            </div>
            <div className="grid gap-4 sm:grid-cols-2">
              {apiHighlights.map((item) => (
                <div key={item} className="rounded-2xl border border-gray-200 bg-white px-5 py-4 text-sm text-gray-700 shadow-sm">
                  {item}
                </div>
              ))}
            </div>
          </div>
        </div>
      </section>

      <section className="bg-white py-16">
        <div className="mx-auto max-w-6xl px-4 sm:px-6 lg:px-8">
          <div className="max-w-3xl">
            <div className="text-sm font-semibold uppercase tracking-[0.18em] text-blue-600">Nasıl ilerliyoruz?</div>
            <h2 className="mt-3 text-3xl font-bold text-gray-900">Entegrasyonu belirsiz teknik iş değil, yönetilebilir proje olarak ele alıyoruz</h2>
          </div>
          <div className="mt-10 grid gap-6 md:grid-cols-3">
            {flow.map((item, index) => (
              <div key={item.title} className="rounded-2xl border border-gray-200 bg-gray-50 p-6">
                <div className="text-sm font-semibold text-blue-600">Adım {index + 1}</div>
                <h3 className="mt-3 text-lg font-semibold text-gray-900">{item.title}</h3>
                <p className="mt-3 text-sm leading-6 text-gray-600">{item.description}</p>
              </div>
            ))}
          </div>
        </div>
      </section>

      <section className="bg-slate-950 py-20 text-white">
        <div className="mx-auto max-w-4xl px-4 text-center sm:px-6 lg:px-8">
          <h2 className="text-3xl font-bold lg:text-4xl">Veri akışınızı ve entegrasyon yöntemini birlikte netleştirelim</h2>
          <p className="mt-5 text-base leading-7 text-slate-300">
            Sipariş kaynağınız, ERP akışınız veya saha veriniz nerede olursa olsun size uygun kurgu ve teknik geçiş planını birlikte çıkarabiliriz.
          </p>
          <div className="mt-8">
            <TrackedLink
              href={PRIMARY_CTA_HREF}
              trackingName="integrations-footer-cta"
              className="inline-flex items-center justify-center rounded-xl bg-white px-8 py-4 text-base font-semibold text-slate-900 transition hover:bg-slate-100"
            >
              İletişime Geçin
            </TrackedLink>
          </div>
        </div>
      </section>
    </div>
  );
}
