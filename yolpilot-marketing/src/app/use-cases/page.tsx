import TrackedLink from '@/components/ui/TrackedLink';
import { PRIMARY_CTA_HREF } from '@/lib/marketing';
import { createPageMetadata } from '@/lib/seo';

export const metadata = createPageMetadata({
  title: 'Kullanım Senaryoları',
  description: 'E-ticaret, lojistik, perakende, saha servis ve soğuk zincir operasyonlarında YolPilot kullanım senaryolarını keşfedin.',
  path: '/use-cases',
  keywords: ['kullanım senaryoları', 'lojistik use case', 'e-ticaret teslimat yönetimi', 'saha servis operasyonu']
});

const scenarios = [
  {
    title: 'E-ticaret dağıtımı',
    challenge: 'Yoğun sipariş hacmi, müşteri bilgilendirme yükü ve teslimat kanıtı ihtiyacı aynı gün içinde büyür.',
    fit: 'YolPilot rota planlamayı, takip linkini ve teslimat sonucunu tek yapıda toplar.'
  },
  {
    title: 'Lojistik ve depo operasyonu',
    challenge: 'Depo çıkışı, aktif sefer, sürücü ve araç durumunu aynı görünümde tutmak zorlaşır.',
    fit: 'YolPilot sefer, depo, araç ve sürücü ilişkilerini operasyon panelinde görünür hale getirir.'
  },
  {
    title: 'Perakende sevkiyatları',
    challenge: 'Mağaza, bayi ve şube teslimatları düzenli rota akışıyla istisna yönetimini birlikte ister.',
    fit: 'YolPilot planlama, teslimat sonucu ve iade benzeri istisnaları daha düzenli takip etmenizi sağlar.'
  },
  {
    title: 'Saha servis ekipleri',
    challenge: 'Randevu, görev notu, saha kanıtı ve müşteri iletişimi farklı araçlara dağıldığında ekip yavaşlar.',
    fit: 'YolPilot mobil görev akışı ve operasyon panelini tek sisteme toplar.'
  },
  {
    title: 'Soğuk zincir ve hassas teslimatlar',
    challenge: 'Zaman baskısı ve kanıt ihtiyacı arttıkça operasyonun her adımını kayıt altına almak zorlaşır.',
    fit: 'YolPilot zaman hassas operasyonlarda görünürlük ve arşiv yapısını güçlendirir.'
  },
  {
    title: 'Hızlı teslimat ve kurye akışları',
    challenge: 'Kurye yoğunluğu arttığında sahadaki gerçek durum ile merkezdeki bilgi ayrışır.',
    fit: 'YolPilot aktif operasyonu tek panelde görünür kılar, kurye akışını izlenebilir hale getirir.'
  }
];

const rollout = [
  {
    title: 'Operasyonu dinleriz',
    description: 'Teslimat, saha veya sefer yapınızı; ekip, araç ve depo ilişkilerini birlikte netleştiririz.'
  },
  {
    title: 'Doğru akışı kurgularız',
    description: 'Ekranları, rollerinizi, bildirimleri ve saha uygulaması adımlarını gerçek operasyona göre şekillendiririz.'
  },
  {
    title: 'Canlıya geçişi planlarız',
    description: 'Ekibin onboarding sürecini, veri aktarımını ve ilk operasyonların kontrolünü birlikte yürütürüz.'
  }
];

export default function UseCasesPage() {
  return (
    <div className="min-h-screen bg-gray-50 pt-24">
      <section className="bg-gradient-to-br from-blue-600 via-blue-700 to-indigo-700 py-20 text-white">
        <div className="mx-auto max-w-6xl px-4 sm:px-6 lg:px-8">
          <div className="max-w-4xl">
            <div className="inline-flex rounded-full bg-white/15 px-4 py-2 text-sm font-semibold text-blue-50">
              Kullanım senaryoları
            </div>
            <h1 className="mt-6 text-4xl font-bold leading-tight lg:text-5xl">
              YolPilot hangi operasyon tiplerinde nasıl değer üretir?
            </h1>
            <p className="mt-6 max-w-3xl text-lg leading-8 text-blue-100">
              Her ekip aynı problemi yaşamıyor. Bu yüzden YolPilot’u; e-ticaret dağıtımından saha servise, filo yönetiminden zaman hassas teslimatlara kadar farklı operasyon tiplerinde nasıl kullandığımızı netleştiriyoruz.
            </p>
            <div className="mt-8">
              <TrackedLink
                href={PRIMARY_CTA_HREF}
                trackingName="use-cases-primary-cta"
                className="inline-flex items-center justify-center rounded-xl bg-white px-8 py-4 text-base font-semibold text-blue-700 transition hover:bg-blue-50"
              >
                Demo Talep Edin
              </TrackedLink>
            </div>
          </div>
        </div>
      </section>

      <section className="bg-white py-16">
        <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
          <div className="grid gap-6 lg:grid-cols-2">
            {scenarios.map((item) => (
              <div key={item.title} className="rounded-2xl border border-gray-200 bg-gray-50 p-6 shadow-sm">
                <h2 className="text-xl font-semibold text-gray-900">{item.title}</h2>
                <div className="mt-4">
                  <div className="text-xs font-semibold uppercase tracking-[0.18em] text-gray-500">Karşılaşılan ihtiyaç</div>
                  <p className="mt-2 text-sm leading-6 text-gray-600">{item.challenge}</p>
                </div>
                <div className="mt-5 rounded-2xl bg-white p-4">
                  <div className="text-xs font-semibold uppercase tracking-[0.18em] text-blue-600">YolPilot ile</div>
                  <p className="mt-2 text-sm leading-6 text-gray-700">{item.fit}</p>
                </div>
              </div>
            ))}
          </div>
        </div>
      </section>

      <section className="py-16">
        <div className="mx-auto max-w-6xl px-4 sm:px-6 lg:px-8">
          <div className="max-w-3xl">
            <div className="text-sm font-semibold uppercase tracking-[0.18em] text-blue-600">Geçiş süreci</div>
            <h2 className="mt-3 text-3xl font-bold text-gray-900">Sizi hazır olmayan bir yapıya değil, size uygun kurguya geçiririz</h2>
          </div>
          <div className="mt-10 grid gap-6 md:grid-cols-3">
            {rollout.map((item, index) => (
              <div key={item.title} className="rounded-2xl border border-gray-200 bg-white p-6 shadow-sm">
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
          <h2 className="text-3xl font-bold lg:text-4xl">Operasyon tipinizi birlikte konuşalım</h2>
          <p className="mt-5 text-base leading-7 text-slate-300">
            Günlük durak sayınız, araç yapınız, saha ekibi akışınız ve müşteri bilgilendirme ihtiyaçlarınız üzerinden size uygun senaryoyu birlikte çıkaralım.
          </p>
          <div className="mt-8">
            <TrackedLink
              href={PRIMARY_CTA_HREF}
              trackingName="use-cases-footer-cta"
              className="inline-flex items-center justify-center rounded-xl bg-white px-8 py-4 text-base font-semibold text-slate-900 transition hover:bg-slate-100"
            >
              Demo Talep Edin
            </TrackedLink>
          </div>
        </div>
      </section>
    </div>
  );
}
