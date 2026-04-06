import Link from 'next/link';
import { createPageMetadata } from '@/lib/seo';

export const metadata = createPageMetadata({
  title: 'Kullanım Koşulları',
  description: 'YolPilot kullanım koşulları; hizmet kapsamı, veri sorumlulukları ve genel şartlar hakkında özet bilgiler içerir.',
  path: '/terms',
  keywords: ['kullanım koşulları', 'hizmet şartları', 'yolpilot sözleşme']
});

const sections = [
  {
    title: 'Hizmet kapsamı',
    content: 'YolPilot; rota planlama, saha uygulaması, teslimat takibi ve teslimat kanıtı süreçlerini destekleyen yazılım hizmetidir.'
  },
  {
    title: 'Kullanıcı sorumlulukları',
    content: 'Sisteme girilen veri, kullanıcı işlemleri ve operasyon akışında paylaşılan bilgilerin doğruluğu müşteri tarafında yönetilir.'
  },
  {
    title: 'Ücretlendirme ve planlar',
    content: 'Plan, kapsam ve fiyat detayları teklif ve sözleşme süreci içinde ayrıca belirlenir.'
  },
  {
    title: 'Veri sahipliği',
    content: 'Müşteri verisi müşteriye aittir. Saklama, erişim ve dışa aktarma süreçleri sözleşme ve operasyon yapısına göre ele alınır.'
  },
  {
    title: 'Süreklilik ve sorumluluk sınırı',
    content: 'Hizmetin kesintisiz sunulması hedeflenir; bakım, altyapı veya zorunlu hallerde geçici kesintiler yaşanabilir.'
  }
];

export default function TermsPage() {
  return (
    <div className="min-h-screen bg-gray-50 pt-24">
      <section className="bg-gradient-to-br from-blue-600 via-blue-700 to-indigo-700 py-20 text-white">
        <div className="mx-auto max-w-5xl px-4 text-center sm:px-6 lg:px-8">
          <h1 className="text-4xl font-bold lg:text-5xl">Kullanım Koşulları</h1>
          <p className="mx-auto mt-6 max-w-3xl text-blue-100">
            Bu sayfa, YolPilot hizmetinin genel kullanım çerçevesini özetler. Sözleşme detayları ayrıca paylaşılır.
          </p>
        </div>
      </section>

      <section className="bg-white py-16">
        <div className="mx-auto max-w-4xl space-y-6 px-4 sm:px-6 lg:px-8">
          {sections.map((item) => (
            <div key={item.title} className="rounded-2xl border border-gray-200 bg-gray-50 p-6">
              <h2 className="text-lg font-semibold text-gray-900">{item.title}</h2>
              <p className="mt-3 text-sm leading-6 text-gray-600">{item.content}</p>
            </div>
          ))}
        </div>
      </section>

      <section className="py-16">
        <div className="mx-auto max-w-4xl px-4 text-center sm:px-6 lg:px-8">
          <h2 className="text-3xl font-bold text-gray-900 mb-4">Sorularınız mı var?</h2>
          <p className="text-gray-600 mb-8">
            Kullanım koşulları, sözleşme kapsamı veya hizmet detayları için bizimle iletişime geçebilirsiniz.
          </p>
          <Link href="/contact?intent=demo" className="inline-flex items-center gap-2 rounded-xl bg-blue-600 px-8 py-4 font-semibold text-white shadow-lg transition hover:bg-blue-700">
            İletişim
          </Link>
        </div>
      </section>
    </div>
  );
}
