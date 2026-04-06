import TrackedLink from '@/components/ui/TrackedLink';
import { PRIMARY_CTA_HREF } from '@/lib/marketing';
import { createPageMetadata } from '@/lib/seo';

export const metadata = createPageMetadata({
  title: 'Müşteri Deneyimleri',
  description: 'Farklı operasyon tiplerinden ekiplerin YolPilot ile daha görünür, düzenli ve denetlenebilir operasyon kurduğu örnek kullanım alanları.',
  path: '/customers',
  keywords: ['müşteri deneyimleri', 'lojistik vaka örnekleri', 'teslimat yönetimi kullanım örnekleri']
});

const stories = [
  {
    sector: 'E-ticaret dağıtımı',
    title: 'Yoğun günlerde bile rota, müşteri bilgisi ve teslimat kanıtı aynı akışta toplandı',
    summary: 'Sipariş hacmi arttıkça ekipte mesaj, telefon ve manuel planlama trafiği büyüyordu. YolPilot ile planlama ekranı, sürücü uygulaması ve müşteri bilgilendirme tek operasyon akışına alındı.',
    outcomes: ['Rota planlama tek panele taşındı', 'Takip bağlantıları ve teslimat bilgisi düzenlendi', 'Kanıt ve istisna kayıtları arşivlenebilir hale geldi']
  },
  {
    sector: 'Saha servis',
    title: 'Görev planlama ve saha çıktıları tek sistemde standardize edildi',
    summary: 'Görev atamaları ve servis notları ekipten ekibe değişiyordu. Mobil görev akışı ile hangi saha personelinin neyi, ne zaman ve hangi notla tamamladığı daha görünür hale geldi.',
    outcomes: ['Görev akışı mobil uygulamaya taşındı', 'Fotoğraf, not ve iş tamamlama kaydı toplandı', 'Merkez ekip için günlük görünürlük arttı']
  },
  {
    sector: 'Perakende sevkiyatı',
    title: 'Mağaza ve bayi sevkiyatlarında istisna yönetimi daha net hale geldi',
    summary: 'Şube ve bayi teslimatlarında iade, eksik teslimat ve not yönetimi operasyon sonunda toparlanıyordu. YolPilot ile bu kayıtlar süreç içinde daha düzenli izlendi.',
    outcomes: ['Depo çıkışı ve teslimat sonucu daha görünür oldu', 'İade ve istisna akışları netleşti', 'Teslimat geçmişi raporlanabilir hale geldi']
  }
];

const benefits = [
  {
    title: 'Tek panelde görünürlük',
    description: 'Merkez ekip operasyonu rota, sürücü, araç ve teslimat düzeyinde daha rahat izler.'
  },
  {
    title: 'Sahada daha net iş akışı',
    description: 'Sürücü ve saha ekipleri görev sırasını, müşteri bilgisini ve notları tek uygulamada görür.'
  },
  {
    title: 'Daha düzenli kayıt yapısı',
    description: 'Teslimat kanıtı, durum ve istisna bilgisi sonradan toplanmak yerine operasyon sırasında kayda alınır.'
  },
  {
    title: 'Daha güçlü müşteri iletişimi',
    description: 'Takip ve bildirim yapısı sayesinde müşteriye giden bilgi daha tutarlı hale gelir.'
  }
];

export default function CustomersPage() {
  return (
    <div className="min-h-screen bg-gray-50 pt-24">
      <section className="bg-gradient-to-br from-blue-600 via-blue-700 to-indigo-700 py-20 text-white">
        <div className="mx-auto max-w-6xl px-4 sm:px-6 lg:px-8">
          <div className="max-w-4xl">
            <div className="inline-flex rounded-full bg-white/15 px-4 py-2 text-sm font-semibold text-blue-50">
              Operasyondan gerçek örnekler
            </div>
            <h1 className="mt-6 text-4xl font-bold leading-tight lg:text-5xl">
              YolPilot, farklı ekiplerde aynı problemi çözer: görünürlük eksikliği
            </h1>
            <p className="mt-6 max-w-3xl text-lg leading-8 text-blue-100">
              Buradaki örnekler belirli bir sektörün tek ihtiyacını değil, planlama, saha ve teslimat tarafında farklı ekiplerin tekrar tekrar yaşadığı operasyon problemlerini anlatır.
            </p>
          </div>
        </div>
      </section>

      <section className="bg-white py-16">
        <div className="mx-auto grid max-w-7xl gap-8 px-4 sm:px-6 lg:px-8">
          {stories.map((story) => (
            <div key={story.title} className="rounded-3xl border border-gray-200 bg-gray-50 p-8 shadow-sm">
              <div className="text-sm font-semibold uppercase tracking-[0.18em] text-blue-600">{story.sector}</div>
              <h2 className="mt-3 text-2xl font-semibold text-gray-900">{story.title}</h2>
              <p className="mt-4 max-w-3xl text-sm leading-7 text-gray-600">{story.summary}</p>
              <div className="mt-6 grid gap-3 md:grid-cols-3">
                {story.outcomes.map((item) => (
                  <div key={item} className="rounded-2xl bg-white px-4 py-4 text-sm text-gray-700">
                    {item}
                  </div>
                ))}
              </div>
            </div>
          ))}
        </div>
      </section>

      <section className="py-16">
        <div className="mx-auto max-w-6xl px-4 sm:px-6 lg:px-8">
          <div className="max-w-3xl">
            <div className="text-sm font-semibold uppercase tracking-[0.18em] text-blue-600">En sık görülen etkiler</div>
            <h2 className="mt-3 text-3xl font-bold text-gray-900">Sektör değişse de operasyon kazanımları benzerleşir</h2>
          </div>
          <div className="mt-10 grid gap-6 md:grid-cols-2 xl:grid-cols-4">
            {benefits.map((item) => (
              <div key={item.title} className="rounded-2xl border border-gray-200 bg-white p-6 shadow-sm">
                <h3 className="text-lg font-semibold text-gray-900">{item.title}</h3>
                <p className="mt-3 text-sm leading-6 text-gray-600">{item.description}</p>
              </div>
            ))}
          </div>
        </div>
      </section>

      <section className="bg-slate-950 py-20 text-white">
        <div className="mx-auto max-w-4xl px-4 text-center sm:px-6 lg:px-8">
          <h2 className="text-3xl font-bold lg:text-4xl">Kendi operasyon yapınızı birlikte değerlendirelim</h2>
          <p className="mt-5 text-base leading-7 text-slate-300">
            Günlük hacminizi, saha ekibinizi ve müşteri bilgilendirme ihtiyaçlarınızı konuşalım. Hangi modüllerle başlamanız gerektiğini birlikte çıkaralım.
          </p>
          <div className="mt-8">
            <TrackedLink
              href={PRIMARY_CTA_HREF}
              trackingName="customers-footer-cta"
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
