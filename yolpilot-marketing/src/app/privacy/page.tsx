import Link from 'next/link';
import { createPageMetadata } from '@/lib/seo';

export const metadata = createPageMetadata({
  title: 'Gizlilik Politikası',
  description: 'YolPilot gizlilik politikası; veri işleme, saklama ve kullanıcı hakları hakkında özet bilgiler içerir.',
  path: '/privacy',
  keywords: ['gizlilik politikası', 'KVKK', 'veri işleme']
});

const sections = [
  {
    title: 'Veri sorumlusu',
    content: 'YolPilot hizmeti kapsamında veri işleme süreçleri Bakırcılar Grup tarafından yürütülür. Güncel ve detaylı süreç bilgileri talep halinde paylaşılır.'
  },
  {
    title: 'Toplanan veriler',
    content: 'İletişim bilgileri, hesap verileri, operasyon kayıtları, cihaz verileri ve teslimat sürecine ilişkin alanlar hizmetin sunulması için işlenebilir.'
  },
  {
    title: 'İşleme amaçları',
    content: 'Veriler; hizmetin sunulması, destek sağlanması, güvenliğin iyileştirilmesi, raporlama ve yasal yükümlülüklerin yerine getirilmesi amacıyla kullanılabilir.'
  },
  {
    title: 'Saklama ve güvenlik',
    content: 'Veriler iş amacıyla gerekli olan süre boyunca saklanır ve yetkisiz erişime karşı teknik ve operasyonel önlemlerle korunur.'
  },
  {
    title: 'Haklarınız',
    content: 'Veri işleme ile ilgili taleplerinizi iletişim kanallarımız üzerinden iletebilirsiniz. İlgili süreçler KVKK çerçevesinde değerlendirilir.'
  }
];

export default function PrivacyPage() {
  return (
    <div className="min-h-screen bg-gray-50 pt-24">
      <section className="bg-gradient-to-br from-blue-600 via-blue-700 to-indigo-700 py-20 text-white">
        <div className="mx-auto max-w-5xl px-4 text-center sm:px-6 lg:px-8">
          <h1 className="text-4xl font-bold lg:text-5xl">Gizlilik Politikası</h1>
          <p className="mx-auto mt-6 max-w-3xl text-blue-100">
            YolPilot kullanıcı verilerini hizmet sunumu, güvenlik ve operasyon sürekliliği odağında işler.
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
          <h2 className="text-3xl font-bold text-gray-900 mb-4">KVKK ve detay süreçler</h2>
          <p className="text-gray-600 mb-8">
            KVKK aydınlatma metni ve özel talepleriniz için ilgili dokümanları inceleyebilir veya bizimle iletişime geçebilirsiniz.
          </p>
          <div className="flex flex-col justify-center gap-4 sm:flex-row">
            <Link href="/docs/kvkk" className="rounded-xl bg-blue-600 px-8 py-4 font-semibold text-white shadow-lg transition hover:bg-blue-700">
              KVKK Metni
            </Link>
            <Link href="/contact?intent=demo" className="rounded-xl border-2 border-blue-600 px-8 py-4 font-semibold text-blue-600 transition hover:bg-blue-50">
              İletişim
            </Link>
          </div>
        </div>
      </section>
    </div>
  );
}
