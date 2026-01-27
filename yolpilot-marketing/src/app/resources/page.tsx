import Link from 'next/link';

const resources = [
  {
    title: 'Dokumantasyon',
    description: 'Kurulum ve kullanim adimlarini iceren temel dokumanlar.'
  },
  {
    title: 'Rehberler',
    description: 'Saha ekibi, rota planlama ve bildirim akislari icin pratik anlatimlar.'
  },
  {
    title: 'SSS',
    description: 'Sik sorulan sorular ve kisa yanitlar.'
  },
  {
    title: 'Yasal Metinler',
    description: 'KVKK, gizlilik ve kullanim kosullari bilgileri.'
  }
];

export default function ResourcesPage() {
  return (
    <div className="min-h-screen bg-gray-50 pt-24">
      <section className="py-20 bg-gradient-to-br from-blue-600 via-blue-700 to-indigo-700 text-white">
        <div className="max-w-5xl mx-auto px-4 sm:px-6 lg:px-8 text-center">
          <h1 className="text-4xl lg:text-5xl font-bold mb-6">Kaynaklar</h1>
          <p className="text-blue-100 max-w-3xl mx-auto">
            YolPilot hakkinda dokumanlar, rehberler ve yardim basliklari.
          </p>
        </div>
      </section>

      <section className="py-16 bg-white">
        <div className="max-w-6xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="grid md:grid-cols-2 gap-6">
            {resources.map((item) => (
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
          <h2 className="text-3xl font-bold text-gray-900 mb-4">Dogru kaynaklara ulasalim</h2>
          <p className="text-gray-600 mb-8">
            Ihtiyac duydugunuz icerigi birlikte belirleyip paylasabiliriz.
          </p>
          <div className="flex flex-col sm:flex-row gap-4 justify-center">
            <Link
              href="/guides"
              className="bg-blue-600 hover:bg-blue-700 text-white px-8 py-4 rounded-xl font-semibold transition-all duration-200 shadow-lg"
            >
              Rehberler
            </Link>
            <Link
              href="/docs"
              className="border-2 border-blue-600 text-blue-600 hover:bg-blue-50 px-8 py-4 rounded-xl font-semibold transition-all duration-200"
            >
              Dokumanlar
            </Link>
          </div>
        </div>
      </section>
    </div>
  );
}
