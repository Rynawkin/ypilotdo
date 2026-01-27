import Link from 'next/link';

const guides = [
  {
    title: 'Rota Planlama Rehberi',
    description: 'Durak siralama, zaman penceresi ve oncelik kurallari.'
  },
  {
    title: 'Saha Uygulamasi Baslangic',
    description: 'Surucu akisi, teslimat kaniti ve not yonetimi.'
  },
  {
    title: 'Musteri Bildirimleri',
    description: 'Takip linki ve bildirim senaryolari ayarlari.'
  },
  {
    title: 'Raporlama Panolari',
    description: 'Sefer ozeti ve performans gorunumleri.'
  }
];

export default function GuidesPage() {
  return (
    <div className="min-h-screen bg-gray-50 pt-24">
      <section className="py-20 bg-gradient-to-br from-blue-600 via-blue-700 to-indigo-700 text-white">
        <div className="max-w-5xl mx-auto px-4 sm:px-6 lg:px-8 text-center">
          <h1 className="text-4xl lg:text-5xl font-bold mb-6">Rehberler</h1>
          <p className="text-blue-100 max-w-3xl mx-auto">
            YolPilot kullanimini hizlandiran pratik anlatimlar ve adim adim kilavuzlar.
          </p>
        </div>
      </section>

      <section className="py-16 bg-white">
        <div className="max-w-6xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="grid md:grid-cols-2 gap-6">
            {guides.map((item) => (
              <div key={item.title} className="bg-gray-50 rounded-2xl p-6 border border-gray-200 shadow-sm">
                <h2 className="text-lg font-semibold text-gray-900 mb-2">{item.title}</h2>
                <p className="text-sm text-gray-600">{item.description}</p>
              </div>
            ))}
          </div>
        </div>
      </section>

      <section className="py-16 bg-gray-50">
        <div className="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 text-center">
          <h2 className="text-3xl font-bold text-gray-900 mb-4">Rehber talebiniz var mi?</h2>
          <p className="text-gray-600 mb-8">
            Ihtiyac duydugunuz konuyu paylasin, birlikte planlayalim.
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
