import TrackedLink from '@/components/ui/TrackedLink';
import { PRIMARY_CTA_HREF } from '@/lib/marketing';
import { createPageMetadata } from '@/lib/seo';

export const metadata = createPageMetadata({
  title: 'Özellikler',
  description: 'Rota optimizasyonu, saha uygulaması, teslimat kanıtı, müşteri bildirimleri ve raporlama modüllerini tek platformda keşfedin.',
  path: '/features',
  keywords: ['rota optimizasyon özellikleri', 'teslimat yönetimi özellikleri', 'saha uygulaması', 'teslimat kanıtı']
});

const featureGroups = [
  {
    title: 'Planlama ve rota optimizasyonu',
    summary: 'Durak sıralama, zaman kısıtı, depo çıkışı ve operasyon kurallarını tek plan yapısında yönetin.',
    bullets: ['Durak sıralama ve rota akışı', 'Depo çıkış ve dönüş kurgusu', 'Öncelik, kısıt ve görev ataması', 'Time window destekli planlama']
  },
  {
    title: 'Sürücü ve saha uygulaması',
    summary: 'Sürücülerin günlük operasyonu tek uygulamadan takip etmesini sağlayın.',
    bullets: ['Mobil görev listesi', 'Navigasyon yönlendirmesi', 'Durak notları ve müşteri bilgisi', 'Canlı durum güncellemesi']
  },
  {
    title: 'Teslimat kanıtı ve istisna yönetimi',
    summary: 'Tamamlanan, başarısız veya iade edilen teslimatları standart kayıt yapısına alın.',
    bullets: ['Fotoğraf ve imza ile kanıt', 'Başarısız teslimat nedeni', 'Not ve açıklama alanları', 'Arşivlenebilir teslimat geçmişi']
  },
  {
    title: 'Müşteri iletişimi',
    summary: 'Müşteriye giden bilgi ile operasyon panelinde görülen durumu aynı akışta tutun.',
    bullets: ['Takip bağlantısı', 'Durum bildirimleri', 'Teslimat özeti', 'Süreç bazlı bilgilendirme']
  },
  {
    title: 'Operasyon görünürlüğü',
    summary: 'Aktif rota, sefer, sürücü ve araç durumunu tek panelde izleyin.',
    bullets: ['Dashboard ve özet kartlar', 'Liste, detay ve form ekranları', 'Depo, araç ve sürücü görünürlüğü', 'Operasyon notları ve geçmiş']
  },
  {
    title: 'Raporlama ve entegrasyon',
    summary: 'Veriyi operasyon kararına çevirecek raporlar ve sistem bağlantıları kurun.',
    bullets: ['Teslimat performansı raporları', 'Sürücü ve araç özetleri', 'REST API ve webhook desteği', 'CSV ve dışa aktarım akışları']
  }
];

const journey = [
  {
    title: 'Merkez planlar',
    description: 'Operasyon ekibi rotaları, seferleri, araçları ve sürücüleri panelden planlar.'
  },
  {
    title: 'Saha uygular',
    description: 'Sürücü uygulaması sayesinde durak sırası, notlar ve teslimat kanıtı tek mobil akışta yürür.'
  },
  {
    title: 'Müşteri izler',
    description: 'Takip linki ve bildirimler sayesinde müşteri daha görünür bir teslimat deneyimi yaşar.'
  },
  {
    title: 'Yönetim raporlar',
    description: 'Teslimat sonucu, istisna ve performans çıktıları rapor ekranlarında toplanır.'
  }
];

const audience = [
  'E-ticaret dağıtım ekipleri',
  'Lojistik ve filo operasyonları',
  'Perakende, bayi ve mağaza sevkiyatları',
  'Saha servis ve görev yönetimi ekipleri',
  'Soğuk zincir ve zaman hassas dağıtım operasyonları'
];

export default function FeaturesPage() {
  return (
    <div className="min-h-screen bg-gray-50 pt-24">
      <section className="bg-gradient-to-br from-blue-600 via-blue-700 to-indigo-700 py-20 text-white">
        <div className="mx-auto max-w-6xl px-4 sm:px-6 lg:px-8">
          <div className="max-w-4xl">
            <div className="inline-flex rounded-full bg-white/15 px-4 py-2 text-sm font-semibold text-blue-50">
              YolPilot ürün modülleri
            </div>
            <h1 className="mt-6 text-4xl font-bold leading-tight lg:text-5xl">
              Planlama, saha ve müşteri deneyimini tek operasyon platformunda toplayın
            </h1>
            <p className="mt-6 max-w-3xl text-lg leading-8 text-blue-100">
              YolPilot, rota planlama ekranından sürücü mobil uygulamasına; teslimat kanıtından müşteri bilgilendirmesine kadar operasyonun tüm kritik parçalarını tek yapıda birleştirir.
            </p>
            <div className="mt-8 flex flex-col gap-4 sm:flex-row">
              <TrackedLink
                href={PRIMARY_CTA_HREF}
                trackingName="features-primary-cta"
                className="inline-flex items-center justify-center rounded-xl bg-white px-8 py-4 text-base font-semibold text-blue-700 transition hover:bg-blue-50"
              >
                Demo Talep Edin
              </TrackedLink>
              <TrackedLink
                href="/use-cases"
                trackingName="features-secondary-cta"
                className="inline-flex items-center justify-center rounded-xl border border-white/30 px-8 py-4 text-base font-semibold text-white transition hover:bg-white/10"
              >
                Kullanım senaryolarını görün
              </TrackedLink>
            </div>
          </div>
        </div>
      </section>

      <section className="bg-white py-16">
        <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
          <div className="max-w-3xl">
            <div className="text-sm font-semibold uppercase tracking-[0.18em] text-blue-600">Temel modüller</div>
            <h2 className="mt-3 text-3xl font-bold text-gray-900">Her ekip için aynı panel, rolüne göre doğru görünürlük</h2>
          </div>
          <div className="mt-10 grid gap-6 md:grid-cols-2 xl:grid-cols-3">
            {featureGroups.map((group) => (
              <div key={group.title} className="rounded-2xl border border-gray-200 bg-gray-50 p-6 shadow-sm">
                <h3 className="text-xl font-semibold text-gray-900">{group.title}</h3>
                <p className="mt-3 text-sm leading-6 text-gray-600">{group.summary}</p>
                <ul className="mt-5 space-y-3">
                  {group.bullets.map((item) => (
                    <li key={item} className="flex items-start gap-3 text-sm text-gray-700">
                      <span className="mt-2 h-2 w-2 rounded-full bg-blue-600" />
                      <span>{item}</span>
                    </li>
                  ))}
                </ul>
              </div>
            ))}
          </div>
        </div>
      </section>

      <section className="py-16">
        <div className="mx-auto max-w-6xl px-4 sm:px-6 lg:px-8">
          <div className="max-w-3xl">
            <div className="text-sm font-semibold uppercase tracking-[0.18em] text-blue-600">Operasyon akışı</div>
            <h2 className="mt-3 text-3xl font-bold text-gray-900">YolPilot içinde tipik bir gün nasıl ilerler?</h2>
          </div>
          <div className="mt-10 grid gap-6 md:grid-cols-2 xl:grid-cols-4">
            {journey.map((item, index) => (
              <div key={item.title} className="rounded-2xl border border-gray-200 bg-white p-6 shadow-sm">
                <div className="text-sm font-semibold text-blue-600">Adım {index + 1}</div>
                <h3 className="mt-3 text-lg font-semibold text-gray-900">{item.title}</h3>
                <p className="mt-3 text-sm leading-6 text-gray-600">{item.description}</p>
              </div>
            ))}
          </div>
        </div>
      </section>

      <section className="bg-white py-16">
        <div className="mx-auto max-w-6xl px-4 sm:px-6 lg:px-8">
          <div className="grid gap-12 lg:grid-cols-[minmax(0,1.1fr)_minmax(0,0.9fr)]">
            <div>
              <div className="text-sm font-semibold uppercase tracking-[0.18em] text-blue-600">Kimler için uygun?</div>
              <h2 className="mt-3 text-3xl font-bold text-gray-900">Büyüme aşamasındaki ekipten çok araçlı operasyona kadar</h2>
              <p className="mt-4 max-w-2xl text-sm leading-7 text-gray-600">
                YolPilot; operasyon yoğunluğu arttıkça mesaj, Excel ve telefon trafiğiyle büyümek istemeyen ekipler için tasarlanır. Ekip büyüdüğünde görünürlüğü koruyan yapı sunar.
              </p>
            </div>
            <div className="grid gap-4">
              {audience.map((item) => (
                <div key={item} className="rounded-2xl border border-gray-200 bg-gray-50 px-5 py-4 text-sm font-medium text-gray-700">
                  {item}
                </div>
              ))}
            </div>
          </div>
        </div>
      </section>

      <section className="bg-slate-950 py-20 text-white">
        <div className="mx-auto max-w-4xl px-4 text-center sm:px-6 lg:px-8">
          <h2 className="text-3xl font-bold lg:text-4xl">Operasyonunuzu canlı bir demo üzerinden birlikte değerlendirelim</h2>
          <p className="mt-5 text-base leading-7 text-slate-300">
            Ekip yapınızı, günlük durak hacminizi ve görmek istediğiniz modülleri konuşalım. Size uygun kurulum akışını birlikte çıkaralım.
          </p>
          <div className="mt-8">
            <TrackedLink
              href={PRIMARY_CTA_HREF}
              trackingName="features-footer-cta"
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
