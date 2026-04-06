import TrackedLink from '@/components/ui/TrackedLink';
import { PRIMARY_CTA_HREF } from '@/lib/marketing';
import { createPageMetadata } from '@/lib/seo';

export const metadata = createPageMetadata({
  title: 'Güvenlik ve Gizlilik',
  description: 'YolPilot veri güvenliği, erişim kontrolü, yedekleme, loglama ve KVKK yaklaşımı hakkında özet bilgiler.',
  path: '/security',
  keywords: ['veri güvenliği', 'KVKK', 'lojistik yazılım güvenliği', 'teslimat verisi güvenliği']
});

const pillars = [
  {
    title: 'Rol bazlı erişim',
    description: 'Panel ve operasyon akışında kullanıcı rolleri üzerinden yetki sınırları tanımlanır.'
  },
  {
    title: 'İşlem kayıtları',
    description: 'Operasyon içinde yapılan kritik güncellemeler ve durum değişimleri takip edilebilir şekilde kayıt altına alınır.'
  },
  {
    title: 'Yedekleme ve süreklilik',
    description: 'Veri sürekliliği için yedekleme yaklaşımı ve operasyon devamlılığı planı uygulanır.'
  },
  {
    title: 'Veri işleme prensipleri',
    description: 'Veri saklama, erişim ve işleme süreçleri ihtiyaç odaklı ve kontrollü şekilde ele alınır.'
  }
];

const details = [
  'Operasyon ve teslimat verisi, yetkilendirilmiş kullanıcılar üzerinden erişilecek şekilde kurgulanır.',
  'Müşteri bilgisi, teslimat kanıtı ve saha notları tek arayüzde görünür olsa da kullanıcı rolüne göre sınırlandırılır.',
  'Kritik süreçlerde işlem geçmişi ve log görünürlüğü sayesinde sonradan izleme ve doğrulama yapılabilir.',
  'Gizlilik ve veri işleme süreçleri için KVKK odaklı yaklaşım benimsenir; detay süreçler talep halinde paylaşılır.'
];

const faqs = [
  {
    question: 'Veriler nerede tutulur?',
    answer: 'Barındırma ve yedekleme yapısı operasyon ihtiyaçlarına göre belirlenir. Detaylı bilgi güvenlik görüşmesi sırasında paylaşılır.'
  },
  {
    question: 'Teslimat kanıtı ve müşteri verisi nasıl korunur?',
    answer: 'Yetkilendirme, işlem kayıtları ve kontrollü erişim yaklaşımı ile operasyon verisinin kim tarafından görüleceği netleştirilir.'
  },
  {
    question: 'KVKK tarafında nasıl ilerleniyor?',
    answer: 'Gizlilik, veri saklama ve silme talepleri KVKK odaklı süreçler üzerinden ele alınır. İlgili dokümanlar ayrıca paylaşılır.'
  }
];

export default function SecurityPage() {
  return (
    <div className="min-h-screen bg-gray-50 pt-24">
      <section className="bg-slate-950 py-20 text-white">
        <div className="mx-auto max-w-6xl px-4 sm:px-6 lg:px-8">
          <div className="max-w-4xl">
            <div className="inline-flex rounded-full bg-white/10 px-4 py-2 text-sm font-semibold text-slate-100">
              Güvenlik ve gizlilik yaklaşımı
            </div>
            <h1 className="mt-6 text-4xl font-bold leading-tight lg:text-5xl">
              Operasyon verisinin güvenliği, görünürlük kadar önemlidir
            </h1>
            <p className="mt-6 max-w-3xl text-lg leading-8 text-slate-300">
              YolPilot; saha, teslimat ve müşteri verisini tek yerde toplarken erişim, kayıt ve süreklilik tarafını da operasyonun doğal bir parçası olarak ele alır.
            </p>
          </div>
        </div>
      </section>

      <section className="bg-white py-16">
        <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
          <div className="grid gap-6 md:grid-cols-2 xl:grid-cols-4">
            {pillars.map((item) => (
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
              <div className="text-sm font-semibold uppercase tracking-[0.18em] text-blue-600">Temel yaklaşım</div>
              <h2 className="mt-3 text-3xl font-bold text-gray-900">Teknik detay yerine neyi nasıl yönettiğimizi açık anlatıyoruz</h2>
            </div>
            <div className="space-y-4">
              {details.map((item) => (
                <div key={item} className="rounded-2xl border border-gray-200 bg-white px-5 py-4 text-sm leading-7 text-gray-700 shadow-sm">
                  {item}
                </div>
              ))}
            </div>
          </div>
        </div>
      </section>

      <section className="bg-white py-16">
        <div className="mx-auto max-w-5xl px-4 sm:px-6 lg:px-8">
          <div className="text-center">
            <div className="text-sm font-semibold uppercase tracking-[0.18em] text-blue-600">Sık sorulan sorular</div>
            <h2 className="mt-3 text-3xl font-bold text-gray-900">Güvenlik değerlendirmesi öncesi en çok sorulanlar</h2>
          </div>
          <div className="mt-10 space-y-4">
            {faqs.map((item) => (
              <div key={item.question} className="rounded-2xl border border-gray-200 bg-gray-50 p-6">
                <h3 className="text-lg font-semibold text-gray-900">{item.question}</h3>
                <p className="mt-3 text-sm leading-6 text-gray-600">{item.answer}</p>
              </div>
            ))}
          </div>
        </div>
      </section>

      <section className="bg-slate-950 py-20 text-white">
        <div className="mx-auto max-w-4xl px-4 text-center sm:px-6 lg:px-8">
          <h2 className="text-3xl font-bold lg:text-4xl">Güvenlik ve uyumluluk akışını ekibinizle birlikte değerlendirelim</h2>
          <p className="mt-5 text-base leading-7 text-slate-300">
            KVKK, operasyon görünürlüğü ve kullanıcı yetkileri tarafındaki sorularınızı demo veya güvenlik görüşmesinde detaylı ele alabiliriz.
          </p>
          <div className="mt-8">
            <TrackedLink
              href={PRIMARY_CTA_HREF}
              trackingName="security-footer-cta"
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
