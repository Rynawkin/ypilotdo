import TrackedLink from '@/components/ui/TrackedLink';
import { PRIMARY_CTA_HREF } from '@/lib/marketing';
import { createPageMetadata } from '@/lib/seo';

export const metadata = createPageMetadata({
  title: 'Manuel Planlama ile Karşılaştırma',
  description: 'Excel, telefon ve dağınık araçlardan tek operasyon paneline geçişte YolPilot farkını görün.',
  path: '/comparison',
  keywords: ['excel yerine rota planlama', 'manuel planlama karşılaştırma', 'teslimat operasyonu dijital dönüşüm']
});

const comparisonRows = [
  {
    title: 'Planlama',
    manual: 'Excel, mesajlaşma ve telefon üzerinden tekrar eden koordinasyon gerekir.',
    yolpilot: 'Rota, araç ve sürücü planı tek panelden oluşturulur ve güncellenir.'
  },
  {
    title: 'Saha görünürlüğü',
    manual: 'Gün içindeki gerçek durum çoğu zaman sonradan öğrenilir.',
    yolpilot: 'Durum güncellemesi, teslimat sonucu ve saha notları operasyon sırasında görünür hale gelir.'
  },
  {
    title: 'Teslimat kanıtı',
    manual: 'Fotoğraf, imza ve notlar dağınık şekilde toplanır veya hiç saklanmaz.',
    yolpilot: 'Kanıt akışı mobil uygulama üzerinden standart hale gelir.'
  },
  {
    title: 'Müşteri iletişimi',
    manual: 'Destek ekibi teslimat bilgisini sahadan tek tek toplamak zorunda kalır.',
    yolpilot: 'Takip bağlantısı ve bildirim yapısı merkezi olarak yönetilir.'
  },
  {
    title: 'Raporlama',
    manual: 'Operasyon sonrası rapor hazırlığı ek iş yükü yaratır.',
    yolpilot: 'Sefer, teslimat ve performans çıktıları panelde hazır tutulur.'
  }
];

const outcomes = [
  'Daha hızlı planlama ve yeniden düzenleme',
  'Sahada daha net görev akışı',
  'Destek yükünde azalma',
  'Teslimat kanıtı ve istisna kayıtlarının düzenli arşivi'
];

export default function ComparisonPage() {
  return (
    <div className="min-h-screen bg-gray-50 pt-24">
      <section className="bg-gradient-to-br from-blue-600 to-blue-700 py-16 text-white">
        <div className="mx-auto max-w-5xl px-4 text-center sm:px-6 lg:px-8">
          <h1 className="text-4xl font-bold lg:text-5xl">Excel, telefon ve mesaj trafiğinden tek operasyon paneline geçin</h1>
          <p className="mt-6 text-xl text-blue-100">
            YolPilot, manuel planlamayı sadece hızlandırmaz; görünürlüğü, kanıt yapısını ve ekipler arası koordinasyonu da düzene sokar.
          </p>
        </div>
      </section>

      <section className="py-16">
        <div className="mx-auto max-w-6xl px-4 sm:px-6 lg:px-8">
          <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
            {outcomes.map((item) => (
              <div key={item} className="rounded-2xl border border-gray-200 bg-white px-5 py-4 text-sm font-medium text-gray-700 shadow-sm">
                {item}
              </div>
            ))}
          </div>

          <div className="mt-10 overflow-hidden rounded-3xl border border-gray-200 bg-white shadow-xl">
            <div className="grid grid-cols-3 border-b border-gray-200 bg-gray-50 text-sm font-semibold text-gray-700">
              <div className="px-6 py-4">Konu</div>
              <div className="px-6 py-4">Manuel yapı</div>
              <div className="px-6 py-4">YolPilot ile</div>
            </div>
            {comparisonRows.map((row) => (
              <div key={row.title} className="grid grid-cols-3 border-b border-gray-100 text-sm">
                <div className="px-6 py-5 font-medium text-gray-900">{row.title}</div>
                <div className="px-6 py-5 text-gray-600">{row.manual}</div>
                <div className="px-6 py-5 text-gray-700">{row.yolpilot}</div>
              </div>
            ))}
          </div>
        </div>
      </section>

      <section className="bg-slate-950 py-20 text-white">
        <div className="mx-auto max-w-4xl px-4 text-center sm:px-6 lg:px-8">
          <h2 className="text-3xl font-bold lg:text-4xl">Mevcut operasyon yapınızı birlikte değerlendirelim</h2>
          <p className="mt-5 text-base leading-7 text-slate-300">
            Hangi işleri hâlâ manuel yürüttüğünüzü konuşalım. Bunları YolPilot içinde nasıl tek akışa taşıyabileceğinizi size özel demo üzerinden gösterelim.
          </p>
          <div className="mt-8">
            <TrackedLink
              href={PRIMARY_CTA_HREF}
              trackingName="comparison-footer-cta"
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
