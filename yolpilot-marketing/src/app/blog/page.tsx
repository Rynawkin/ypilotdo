import Link from 'next/link';

const topics = [
  'Rota planlama ipuclari',
  'Saha ekipleri icin operasyon standardi',
  'Teslimat kaniti ve musteri guveni',
  'Bildirim senaryolari ve iletisim',
  'Entegrasyon ve veri akisi'
];

const highlights = [
  {
    title: 'Operasyon Notlari',
    description: 'Saha ve planlama ekipleri icin pratik yaklasimlar.'
  },
  {
    title: 'Urun Guncellemeleri',
    description: 'YolPilot uzerindeki yeni ozellik ve iyilestirmeler.'
  },
  {
    title: 'Rehberler',
    description: 'Kurulum ve kullanimi hizlandiran kisa rehberler.'
  }
];

export default function BlogPage() {
  return (
    <div className="min-h-screen bg-gray-50 pt-24">
      <section className="py-20 bg-gradient-to-br from-blue-600 via-blue-700 to-indigo-700 text-white">
        <div className="max-w-5xl mx-auto px-4 sm:px-6 lg:px-8 text-center">
          <h1 className="text-4xl lg:text-5xl font-bold mb-6">Blog</h1>
          <p className="text-blue-100 max-w-3xl mx-auto">
            Operasyon, saha uygulamalari ve musteri deneyimi uzerine pratik notlar.
          </p>
        </div>
      </section>

      <section className="py-16 bg-white">
        <div className="max-w-6xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="grid md:grid-cols-3 gap-6 mb-12">
            {highlights.map((item) => (
              <div key={item.title} className="bg-gray-50 rounded-2xl p-6 border border-gray-200 shadow-sm">
                <h2 className="text-lg font-semibold text-gray-900 mb-2">{item.title}</h2>
                <p className="text-sm text-gray-600">{item.description}</p>
              </div>
            ))}
          </div>
          <div className="bg-blue-50 rounded-2xl p-6 border border-blue-100">
            <h3 className="text-lg font-semibold text-gray-900 mb-3">Gundemdeki Basliklar</h3>
            <ul className="grid sm:grid-cols-2 gap-3 text-sm text-gray-700">
              {topics.map((topic) => (
                <li key={topic} className="flex items-center gap-2">
                  <span className="h-2 w-2 rounded-full bg-blue-600"></span>
                  <span>{topic}</span>
                </li>
              ))}
            </ul>
          </div>
        </div>
      </section>

      <section className="py-16 bg-gray-50">
        <div className="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 text-center">
          <h2 className="text-3xl font-bold text-gray-900 mb-4">Bize konu onerisi iletin</h2>
          <p className="text-gray-600 mb-8">
            Merak ettiginiz basliklari paylasin, icerigi birlikte sekillendirelim.
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
