import TrackedLink from '@/components/ui/TrackedLink';
import { createPageMetadata } from '@/lib/seo';

export const metadata = createPageMetadata({
  title: 'KVKK Aydınlatma Metni',
  description:
    'YolPilot kapsamında kişisel verilerin işlenmesine dair temel prensipler ve başlıca haklar hakkında bilgi alın.',
  path: '/docs/kvkk',
  keywords: ['YolPilot KVKK', 'aydınlatma metni', 'kişisel veri işleme']
});

type KvkkSection =
  | {
      title: string;
      content: string;
      items?: never;
    }
  | {
      title: string;
      items: string[];
      content?: never;
    };

const sections: KvkkSection[] = [
  {
    title: 'Veri sorumlusu',
    content:
      'Bu aydınlatma metni, Bakıcılar Grup tarafından sunulan YolPilot hizmeti kapsamında işlenen kişisel veriler için hazırlanmıştır.'
  },
  {
    title: 'İşlenen veri kategorileri',
    items: ['Kimlik ve iletişim bilgileri', 'Müşteri ve operasyon verileri', 'Cihaz ve konum bilgileri', 'İşlem ve log kayıtları']
  },
  {
    title: 'İşleme amaçları',
    items: [
      'Hizmeti sunmak ve geliştirmek',
      'Müşteri desteği sağlamak',
      'Güvenlik ve yetkilendirme süreçlerini yürütmek',
      'Yasal yükümlülükleri yerine getirmek'
    ]
  },
  {
    title: 'Veri aktarımı',
    content:
      'Veriler, hizmetin sunulması için gerekli olan tedarikçilerle ve yasal zorunluluklarda yetkili kurumlarla paylaşılabilir.'
  },
  {
    title: 'Saklama ve güvenlik',
    content:
      'Veriler amaca uygun süre boyunca saklanır ve yetkisiz erişimi önlemek için teknik ve idari güvenlik tedbirleri uygulanır.'
  },
  {
    title: 'Haklarınız',
    items: [
      'Veri işleme bilgisi talep etme',
      'Verilerin düzeltilmesini isteme',
      'İşleme faaliyetlerine itiraz etme',
      'Silme ve imha taleplerini iletme'
    ]
  }
];

export default function KvkkPage() {
  return (
    <div className="min-h-screen bg-gray-50 pt-24">
      <section className="bg-gradient-to-br from-blue-700 via-blue-800 to-slate-900 py-20 text-white">
        <div className="mx-auto max-w-5xl px-4 text-center sm:px-6 lg:px-8">
          <div className="inline-flex rounded-full bg-white/10 px-4 py-2 text-sm font-semibold text-blue-50">
            Yasal bilgi
          </div>
          <h1 className="mt-6 text-4xl font-bold lg:text-5xl">KVKK Aydınlatma Metni</h1>
          <p className="mx-auto mt-6 max-w-3xl text-lg leading-8 text-blue-100">
            YolPilot kapsamında kişisel verilerin hangi amaçlarla işlendiği, saklandığı ve hangi
            haklara sahip olduğunuzun özetini burada bulabilirsiniz.
          </p>
        </div>
      </section>

      <section className="bg-white py-16">
        <div className="mx-auto max-w-4xl space-y-6 px-4 sm:px-6 lg:px-8">
          {sections.map((section) => (
            <div key={section.title} className="rounded-3xl border border-gray-200 bg-gray-50 p-6 shadow-sm">
              <h2 className="text-lg font-semibold text-gray-900">{section.title}</h2>
              {section.items ? (
                <ul className="mt-4 space-y-2 text-sm leading-6 text-gray-600">
                  {section.items.map((item) => (
                    <li key={item} className="flex gap-3">
                      <span className="mt-2 h-2.5 w-2.5 rounded-full bg-blue-600" />
                      <span>{item}</span>
                    </li>
                  ))}
                </ul>
              ) : (
                <p className="mt-4 text-sm leading-6 text-gray-600">{section.content}</p>
              )}
            </div>
          ))}
        </div>
      </section>

      <section className="bg-slate-950 py-20 text-white">
        <div className="mx-auto max-w-4xl px-4 text-center sm:px-6 lg:px-8">
          <h2 className="text-3xl font-bold lg:text-4xl">KVKK kapsamındaki talepleriniz için</h2>
          <p className="mt-5 text-base leading-7 text-slate-300">
            Veri işleme ve hak talepleriniz için bizimle iletişime geçebilirsiniz. Size doğru yasal
            yönlendirmeyi sağlayalım.
          </p>
          <div className="mt-8">
            <TrackedLink
              href="/contact?intent=demo"
              trackingName="docs-kvkk-primary-cta"
              className="inline-flex items-center justify-center rounded-xl bg-white px-8 py-4 text-base font-semibold text-slate-900 transition hover:bg-slate-100"
            >
              İletişim formu
            </TrackedLink>
          </div>
        </div>
      </section>
    </div>
  );
}
