import TrackedLink from '@/components/ui/TrackedLink';
import { PRIMARY_CTA_HREF } from '@/lib/marketing';
import { createPageMetadata } from '@/lib/seo';

export const metadata = createPageMetadata({
  title: 'API Dokümantasyonu Özeti',
  description:
    'YolPilot API erişimi, veri kapsamı ve entegrasyon akışı hakkında teknik ekipler için özet bilgi.',
  path: '/docs/api',
  keywords: ['YolPilot API dokümantasyon', 'teslimat API', 'lojistik entegrasyon akışı']
});

const items = [
  'Kimlik doğrulama ve yetki yönetimi',
  'Sipariş ve müşteri aktarımı',
  'Rota, sefer ve teslimat durumları',
  'Webhook tetikleyicileri ve dönüş akışları',
  'Test senaryoları ve canlıya geçiş doğrulaması'
];

const steps = [
  {
    title: 'Kapsam belirleme',
    description:
      'İlk görüşmede hangi sistemlerin veri sağlayacağı ve hangi çıktıların geri döneceği netleştirilir.'
  },
  {
    title: 'Erişim ve güvenlik',
    description:
      'API erişim modeli, yetki yapısı ve loglama beklentileri teknik ekiplerle birlikte doğrulanır.'
  },
  {
    title: 'Test ve canlıya geçiş',
    description:
      'Entegrasyon akışı gerçek operasyon senaryosu üzerinde test edilerek canlı kullanıma hazırlanır.'
  }
];

export default function ApiDocsPage() {
  return (
    <div className="min-h-screen bg-gray-50 pt-24">
      <section className="bg-gradient-to-br from-blue-700 via-blue-800 to-slate-900 py-20 text-white">
        <div className="mx-auto max-w-5xl px-4 text-center sm:px-6 lg:px-8">
          <div className="inline-flex rounded-full bg-white/10 px-4 py-2 text-sm font-semibold text-blue-50">
            Teknik ekipler için
          </div>
          <h1 className="mt-6 text-4xl font-bold lg:text-5xl">API dokümantasyonu özeti</h1>
          <p className="mx-auto mt-6 max-w-3xl text-lg leading-8 text-blue-100">
            YolPilot API; sipariş, müşteri, rota ve teslimat akışlarını mevcut sistemlerinizle
            daha kontrollü şekilde bağlamayı hedefler.
          </p>
        </div>
      </section>

      <section className="bg-white py-16">
        <div className="mx-auto max-w-6xl px-4 sm:px-6 lg:px-8">
          <div className="grid gap-6 md:grid-cols-3">
            {steps.map((item) => (
              <div key={item.title} className="rounded-2xl border border-gray-200 bg-gray-50 p-6 shadow-sm">
                <h2 className="text-lg font-semibold text-gray-900">{item.title}</h2>
                <p className="mt-3 text-sm leading-6 text-gray-600">{item.description}</p>
              </div>
            ))}
          </div>

          <div className="mt-10 rounded-3xl border border-gray-200 bg-white p-8 shadow-sm">
            <h3 className="text-lg font-semibold text-gray-900">Kapsam başlıkları</h3>
            <div className="mt-6 grid gap-4 md:grid-cols-2">
              {items.map((item) => (
                <div key={item} className="flex gap-3 rounded-2xl border border-gray-100 bg-gray-50 px-5 py-4 text-sm leading-6 text-gray-700">
                  <span className="mt-2 h-2.5 w-2.5 rounded-full bg-blue-600" />
                  <span>{item}</span>
                </div>
              ))}
            </div>
          </div>
        </div>
      </section>

      <section className="bg-slate-950 py-20 text-white">
        <div className="mx-auto max-w-4xl px-4 text-center sm:px-6 lg:px-8">
          <h2 className="text-3xl font-bold lg:text-4xl">Teknik kurulum senaryonuzu paylaşın</h2>
          <p className="mt-5 text-base leading-7 text-slate-300">
            Ekiplerinizin kullandığı sistemleri ve veri akışını paylaşın. YolPilot ile nasıl bir
            entegrasyon modeli kurulabileceğini birlikte netleştirelim.
          </p>
          <div className="mt-8 flex flex-col justify-center gap-4 sm:flex-row">
            <TrackedLink
              href={PRIMARY_CTA_HREF}
              trackingName="docs-api-primary-cta"
              className="inline-flex items-center justify-center rounded-xl bg-white px-8 py-4 text-base font-semibold text-slate-900 transition hover:bg-slate-100"
            >
              Demo ve teknik görüşme talep edin
            </TrackedLink>
            <TrackedLink
              href="/api-docs"
              trackingName="docs-api-secondary-cta"
              className="inline-flex items-center justify-center rounded-xl border border-white/20 px-8 py-4 text-base font-semibold text-white transition hover:bg-white/10"
            >
              API kapsamını özet görün
            </TrackedLink>
          </div>
        </div>
      </section>
    </div>
  );
}
