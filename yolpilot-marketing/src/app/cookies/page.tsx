import Link from 'next/link';

const cookieTypes = [
  {
    title: 'Zorunlu Cerezler',
    description: 'Oturum yonetimi ve temel guvenlik icin kullanilir.'
  },
  {
    title: 'Tercih Cerezleri',
    description: 'Dil ve gorunum tercihlerinizin hatirlanmasini saglar.'
  },
  {
    title: 'Analitik Cerezler',
    description: 'Kullanim trendlerini anlamamiza yardimci olur.'
  }
];

const controls = [
  'Tarayici ayarlarinizdan cerezleri silebilir veya engelleyebilirsiniz.',
  'Cerezleri engellemek bazi islevleri sinirlayabilir.',
  'Guncel ayarlariniz icin tarayici yardim dokumanlarini kontrol edin.'
];

export default function CookiesPage() {
  return (
    <div className="min-h-screen bg-gray-50 pt-24">
      <section className="py-20 bg-gradient-to-br from-blue-600 via-blue-700 to-indigo-700 text-white">
        <div className="max-w-5xl mx-auto px-4 sm:px-6 lg:px-8 text-center">
          <h1 className="text-4xl lg:text-5xl font-bold mb-6">Cerez Politikasi</h1>
          <p className="text-blue-100 max-w-3xl mx-auto">
            Bu sayfa, YolPilot web sitesinde kullanilan cerezleri ve amaclarini aciklar.
          </p>
        </div>
      </section>

      <section className="py-16 bg-white">
        <div className="max-w-6xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="grid md:grid-cols-3 gap-6">
            {cookieTypes.map((item) => (
              <div key={item.title} className="bg-gray-50 rounded-2xl p-6 border border-gray-200 shadow-sm">
                <h2 className="text-lg font-semibold text-gray-900 mb-2">{item.title}</h2>
                <p className="text-sm text-gray-600">{item.description}</p>
              </div>
            ))}
          </div>
        </div>
      </section>

      <section className="py-16 bg-gray-50">
        <div className="max-w-5xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="bg-white rounded-2xl p-6 border border-gray-200 shadow-sm">
            <h2 className="text-lg font-semibold text-gray-900 mb-3">Cerezleri Yonetme</h2>
            <ul className="space-y-2 text-sm text-gray-700">
              {controls.map((item) => (
                <li key={item} className="flex items-start gap-2">
                  <span className="mt-1 h-2 w-2 rounded-full bg-blue-600"></span>
                  <span>{item}</span>
                </li>
              ))}
            </ul>
          </div>
        </div>
      </section>

      <section className="py-16 bg-white">
        <div className="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 text-center">
          <h2 className="text-3xl font-bold text-gray-900 mb-4">Daha fazla bilgi</h2>
          <p className="text-gray-600 mb-8">
            Gizlilik veya cerezlerle ilgili sorulariniz icin bize ulasabilirsiniz.
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
