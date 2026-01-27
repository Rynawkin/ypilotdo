import Link from 'next/link';

const items = [
  'Kimlik dogrulama ve yetki yonetimi',
  'Siparis ve musteri aktarimi',
  'Rota ve teslimat durumlari',
  'Webhook tetikleyicileri',
  'Test senaryolari ve dogrulama'
];

export default function ApiDocsPage() {
  return (
    <div className="min-h-screen bg-gray-50 pt-24">
      <section className="py-20 bg-gradient-to-br from-blue-600 via-blue-700 to-indigo-700 text-white">
        <div className="max-w-5xl mx-auto px-4 sm:px-6 lg:px-8 text-center">
          <h1 className="text-4xl lg:text-5xl font-bold mb-6">API Dokumantasyonu</h1>
          <p className="text-blue-100 max-w-3xl mx-auto">
            Entegrasyon ekibiniz icin temel API kapsamini burada ozetliyoruz.
          </p>
        </div>
      </section>

      <section className="py-16 bg-white">
        <div className="max-w-5xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="bg-gray-50 rounded-2xl p-6 border border-gray-200 shadow-sm">
            <h2 className="text-lg font-semibold text-gray-900 mb-3">Kapsam</h2>
            <ul className="grid sm:grid-cols-2 gap-3 text-sm text-gray-700">
              {items.map((item) => (
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
          <h2 className="text-3xl font-bold text-gray-900 mb-4">Dokuman talebi</h2>
          <p className="text-gray-600 mb-8">
            Teknik dokumanlar ve erisim bilgileri icin bizimle iletisime gecin.
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
