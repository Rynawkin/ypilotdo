import TrackedLink from '@/components/ui/TrackedLink';
import { PRIMARY_CTA_HREF } from '@/lib/marketing';
import { createPageMetadata } from '@/lib/seo';

export const metadata = createPageMetadata({
  title: 'Demo',
  description: 'YolPilot demosunda rota planlama, saha uygulaması, teslimat kanıtı ve raporlama akışını görün.',
  path: '/demo',
  keywords: ['YolPilot demo', 'rota optimizasyon demo', 'lojistik yazılım demosu']
});

const steps = [
  {
    title: 'Operasyon yapınızı konuşuruz',
    description: 'Araç, sürücü, depo ve günlük rota hacminizi demo öncesinde netleştiririz.'
  },
  {
    title: 'Doğru ekranları gösteririz',
    description: 'Planlama paneli, mobil saha uygulaması ve teslimat kanıtı akışını örnek senaryo üzerinden anlatırız.'
  },
  {
    title: 'Kurulum yaklaşımını çıkarırız',
    description: 'Demo sonunda hangi modüllerle başlamanın daha mantıklı olduğunu birlikte belirleriz.'
  }
];

const focusAreas = [
  'Rota planlama ve optimizasyon',
  'Sürücü ve araç yönetimi',
  'Müşteri bilgilendirme akışları',
  'Teslimat kanıtı ve istisna yönetimi',
  'Raporlama ve günlük operasyon görünürlüğü'
];

export default function DemoPage() {
  return (
    <div className="min-h-screen bg-gray-50 pt-24">
      <section className="bg-gradient-to-br from-blue-600 via-blue-700 to-indigo-700 py-20 text-white">
        <div className="mx-auto max-w-6xl px-4 sm:px-6 lg:px-8">
          <div className="max-w-4xl">
            <div className="inline-flex rounded-full bg-white/15 px-4 py-2 text-sm font-semibold text-blue-50">
              Demo süreci
            </div>
            <h1 className="mt-6 text-4xl font-bold leading-tight lg:text-5xl">
              YolPilot’un operasyona nasıl oturduğunu gerçek senaryo üzerinden görün
            </h1>
            <p className="mt-6 max-w-3xl text-lg leading-8 text-blue-100">
              Demo sürecinde sadece ekranları göstermiyoruz. Günlük operasyonunuzdaki karar noktalarını, ekip akışını ve kurulum yaklaşımını birlikte netleştiriyoruz.
            </p>
          </div>
        </div>
      </section>

      <section className="bg-white py-16">
        <div className="mx-auto max-w-6xl px-4 sm:px-6 lg:px-8">
          <div className="grid gap-6 md:grid-cols-3">
            {steps.map((step, index) => (
              <div key={step.title} className="rounded-2xl border border-gray-200 bg-gray-50 p-6 shadow-sm">
                <div className="text-sm font-semibold text-blue-600">Adım {index + 1}</div>
                <h2 className="mt-3 text-lg font-semibold text-gray-900">{step.title}</h2>
                <p className="mt-3 text-sm leading-6 text-gray-600">{step.description}</p>
              </div>
            ))}
          </div>

          <div className="mt-10 rounded-3xl border border-blue-100 bg-blue-50 p-6">
            <h3 className="text-lg font-semibold text-gray-900">Demo sırasında odaklandığımız başlıklar</h3>
            <ul className="mt-4 grid gap-3 text-sm text-gray-700 sm:grid-cols-2">
              {focusAreas.map((item) => (
                <li key={item} className="flex items-center gap-2">
                  <span className="h-2 w-2 rounded-full bg-blue-600" />
                  <span>{item}</span>
                </li>
              ))}
            </ul>
          </div>
        </div>
      </section>

      <section className="bg-slate-950 py-20 text-white">
        <div className="mx-auto max-w-4xl px-4 text-center sm:px-6 lg:px-8">
          <h2 className="text-3xl font-bold lg:text-4xl">Demo planlamak ister misiniz?</h2>
          <p className="mt-5 text-base leading-7 text-slate-300">
            Ekibimiz sizin için uygun zamanı planlasın ve gerçek operasyonunuza göre demoyu hazırlasın.
          </p>
          <div className="mt-8">
            <TrackedLink
              href={PRIMARY_CTA_HREF}
              trackingName="demo-footer-cta"
              className="inline-flex items-center justify-center rounded-xl bg-white px-8 py-4 text-base font-semibold text-slate-900 transition hover:bg-slate-100"
            >
              Demo Talep Et
            </TrackedLink>
          </div>
        </div>
      </section>
    </div>
  );
}
