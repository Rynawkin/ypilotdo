import Link from 'next/link';

const topics = [
  'Hesap ve yetki yonetimi',
  'Rota planlama ve seferler',
  'Saha uygulamasi kullanimi',
  'Bildirim ve iletisim ayarlari',
  'Raporlama ve teslimat kaniti'
];

export default function HelpPage() {
  return (
    <div className="min-h-screen bg-gray-50 pt-24">
      <section className="py-20 bg-gradient-to-br from-blue-600 via-blue-700 to-indigo-700 text-white">
        <div className="max-w-5xl mx-auto px-4 sm:px-6 lg:px-8 text-center">
          <h1 className="text-4xl lg:text-5xl font-bold mb-6">Yardim Merkezi</h1>
          <p className="text-blue-100 max-w-3xl mx-auto">
            YolPilot kullanimina dair kisa yanitlar ve temel konular.
          </p>
        </div>
      </section>

      <section className="py-16 bg-white">
        <div className="max-w-6xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="bg-gray-50 rounded-2xl p-6 border border-gray-200 shadow-sm">
            <h2 className="text-lg font-semibold text-gray-900 mb-3">Yardim Basliklari</h2>
            <ul className="grid sm:grid-cols-2 gap-3 text-sm text-gray-700">
              {topics.map((item) => (
                <li key={item} className="flex items-center gap-2">
                  <span className="h-2 w-2 rounded-full bg-blue-600"></span>
                  <span>{item}</span>
                </li>
              ))}
            </ul>
          </div>
        </div>
      </section>

      <section className="py-16 bg-gray-50">
        <div className="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 text-center">
          <h2 className="text-3xl font-bold text-gray-900 mb-4">Sorunuz mu var?</h2>
          <p className="text-gray-600 mb-8">
            Ekibimiz destek taleplerinizi toparlayip en kisa surede yanitlar.
          </p>
          <div className="flex flex-col sm:flex-row gap-4 justify-center">
            <Link
              href="/support"
              className="bg-blue-600 hover:bg-blue-700 text-white px-8 py-4 rounded-xl font-semibold transition-all duration-200 shadow-lg"
            >
              Destek Kanallari
            </Link>
            <Link
              href="/contact"
              className="border-2 border-blue-600 text-blue-600 hover:bg-blue-50 px-8 py-4 rounded-xl font-semibold transition-all duration-200"
            >
              Iletisim
            </Link>
          </div>
        </div>
      </section>
    </div>
  );
}
