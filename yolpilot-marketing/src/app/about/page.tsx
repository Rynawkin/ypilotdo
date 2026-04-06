import TrackedLink from '@/components/ui/TrackedLink';
import { PRIMARY_CTA_HREF } from '@/lib/marketing';
import { createPageMetadata } from '@/lib/seo';

export const metadata = createPageMetadata({
  title: 'Hakkımızda',
  description: 'YolPilot’un neden geliştirildiğini, hangi operasyon problemlerine odaklandığını ve ekibimizin yaklaşımını keşfedin.',
  path: '/about',
  keywords: ['YolPilot hakkında', 'rota optimizasyon platformu', 'lojistik yazılım ekibi']
});

const values = [
  {
    title: 'Operasyon netliği',
    description: 'Planlama, saha ve raporlama aynı veri akışına bağlandığında ekipler daha hızlı karar verir.'
  },
  {
    title: 'Sahaya uygun tasarım',
    description: 'Ofis paneli kadar sürücü ve saha uygulamasının da gerçekten kullanılabilir olması gerekir.'
  },
  {
    title: 'Gerçekçi vaat',
    description: 'Hızlı satış yerine sürdürülebilir, raporlanabilir ve operasyon içinde karşılığı olan faydaya odaklanırız.'
  }
];

const approach = [
  {
    title: 'Operasyonu dinleriz',
    description: 'Önce saha, depo, sürücü ve müşteri akışınızı anlamaya çalışırız.'
  },
  {
    title: 'Doğru yapıyı kurarız',
    description: 'Tek tip çözüm yerine, ekip yapınıza uygun modül ve kuralları birlikte belirleriz.'
  },
  {
    title: 'Canlıya geçişi destekleriz',
    description: 'Kurulum, onboarding ve ilk operasyonların kontrolünü birlikte yürütürüz.'
  }
];

export default function AboutPage() {
  return (
    <div className="min-h-screen bg-gray-50 pt-24">
      <section className="bg-gradient-to-br from-blue-600 via-blue-700 to-indigo-700 py-20 text-white">
        <div className="mx-auto max-w-6xl px-4 sm:px-6 lg:px-8">
          <div className="max-w-4xl">
            <div className="inline-flex rounded-full bg-white/15 px-4 py-2 text-sm font-semibold text-blue-50">
              YolPilot hakkında
            </div>
            <h1 className="mt-6 text-4xl font-bold leading-tight lg:text-5xl">
              YolPilot, saha ve dağıtım operasyonlarını daha görünür hale getirmek için geliştirildi
            </h1>
            <p className="mt-6 max-w-3xl text-lg leading-8 text-blue-100">
              Operasyon ekipleri büyüdükçe problem yalnızca rota planlama değil; iletişim, kanıt, görünürlük ve koordinasyon oluyor. YolPilot bu yüzden tek ekrandan çok, tek operasyon yapısına odaklanır.
            </p>
          </div>
        </div>
      </section>

      <section className="bg-white py-16">
        <div className="mx-auto max-w-6xl px-4 sm:px-6 lg:px-8">
          <div className="grid gap-6 md:grid-cols-3">
            {values.map((item) => (
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
          <div className="max-w-3xl">
            <div className="text-sm font-semibold uppercase tracking-[0.18em] text-blue-600">Nasıl çalışıyoruz?</div>
            <h2 className="mt-3 text-3xl font-bold text-gray-900">Kurulumdan önce sistem değil, iş akışı konuşuruz</h2>
          </div>
          <div className="mt-10 grid gap-6 md:grid-cols-3">
            {approach.map((item, index) => (
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
          <h2 className="text-3xl font-bold lg:text-4xl">Operasyonunuzu birlikte dinleyelim</h2>
          <p className="mt-5 text-base leading-7 text-slate-300">
            Günlük dağıtım yapınızı, saha ekibinizi ve görmek istediğiniz modülleri birlikte konuşalım.
          </p>
          <div className="mt-8">
            <TrackedLink
              href={PRIMARY_CTA_HREF}
              trackingName="about-footer-cta"
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
