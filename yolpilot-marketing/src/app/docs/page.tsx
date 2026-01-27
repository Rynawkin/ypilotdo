import Link from 'next/link';

const docs = [
  {
    title: 'API Dokumantasyonu',
    description: 'Entegrasyon icin teknik detaylar ve erisim sureci.',
    href: '/docs/api'
  },
  {
    title: 'KVKK Metni',
    description: 'Kisisel veri isleme aydinlatma metni.',
    href: '/docs/kvkk'
  },
  {
    title: 'Kullanim Kosullari',
    description: 'Hizmetin temel kullanim kosullari.',
    href: '/terms'
  },
  {
    title: 'Gizlilik Politikasi',
    description: 'Veri isleme ve gizlilik prensipleri.',
    href: '/privacy'
  }
];

export default function DocsPage() {
  return (
    <div className="min-h-screen bg-gray-50 pt-24">
      <section className="py-20 bg-gradient-to-br from-blue-600 via-blue-700 to-indigo-700 text-white">
        <div className="max-w-5xl mx-auto px-4 sm:px-6 lg:px-8 text-center">
          <h1 className="text-4xl lg:text-5xl font-bold mb-6">Dokumantasyon</h1>
          <p className="text-blue-100 max-w-3xl mx-auto">
            YolPilot kullanimi ve yasal dokumanlar icin temel basvuru noktasi.
          </p>
        </div>
      </section>

      <section className="py-16 bg-white">
        <div className="max-w-6xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="grid md:grid-cols-2 gap-6">
            {docs.map((item) => (
              <Link
                key={item.title}
                href={item.href}
                className="bg-gray-50 rounded-2xl p-6 border border-gray-200 shadow-sm hover:border-blue-300 hover:bg-blue-50 transition-all duration-200"
              >
                <h2 className="text-lg font-semibold text-gray-900 mb-2">{item.title}</h2>
                <p className="text-sm text-gray-600">{item.description}</p>
              </Link>
            ))}
          </div>
        </div>
      </section>
    </div>
  );
}
