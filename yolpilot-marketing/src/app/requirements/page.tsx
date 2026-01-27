import Link from 'next/link';

const requirements = [
  {
    title: 'Web Panel',
    items: [
      'Guncel bir web tarayici',
      'Internet baglantisi',
      'Ekran goruntuleme icin standart cozum'
    ]
  },
  {
    title: 'Mobil Uygulama',
    items: [
      'iOS veya Android cihaz',
      'Konum izni',
      'Kamera erisimi (teslimat kaniti icin)'
    ]
  },
  {
    title: 'Operasyon Verisi',
    items: [
      'Musteri adres listesi',
      'Arac ve surucu bilgileri',
      'Teslimat notlari veya talimatlar'
    ]
  }
];

export default function RequirementsPage() {
  return (
    <div className="min-h-screen bg-gray-50 pt-24">
      <section className="py-20 bg-gradient-to-br from-blue-600 via-blue-700 to-indigo-700 text-white">
        <div className="max-w-5xl mx-auto px-4 sm:px-6 lg:px-8 text-center">
          <h1 className="text-4xl lg:text-5xl font-bold mb-6">Sistem Gereksinimleri</h1>
          <p className="text-blue-100 max-w-3xl mx-auto">
            YolPilot&apos;i kullanmak icin gerekli temel kosullari bu sayfada bulabilirsiniz.
          </p>
        </div>
      </section>

      <section className="py-16 bg-white">
        <div className="max-w-6xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="grid md:grid-cols-3 gap-6">
            {requirements.map((section) => (
              <div key={section.title} className="bg-gray-50 rounded-2xl p-6 border border-gray-200 shadow-sm">
                <h2 className="text-lg font-semibold text-gray-900 mb-3">{section.title}</h2>
                <ul className="space-y-2 text-sm text-gray-700">
                  {section.items.map((item) => (
                    <li key={item} className="flex items-start gap-2">
                      <span className="mt-1 h-2 w-2 rounded-full bg-blue-600"></span>
                      <span>{item}</span>
                    </li>
                  ))}
                </ul>
              </div>
            ))}
          </div>
        </div>
      </section>

      <section className="py-16 bg-gray-50">
        <div className="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 text-center">
          <h2 className="text-3xl font-bold text-gray-900 mb-4">Kurulum icin hazir misiniz?</h2>
          <p className="text-gray-600 mb-8">
            Operasyon verilerinizi hazirlayip ekibimizle birlikte ilerleyebilirsiniz.
          </p>
          <Link
            href="/contact"
            className="inline-flex items-center gap-2 bg-blue-600 hover:bg-blue-700 text-white px-8 py-4 rounded-xl font-semibold transition-all duration-200 shadow-lg"
          >
            Iletisim
          </Link>
        </div>
      </section>
    </div>
  );
}
