import Link from 'next/link';

const focus = [
  'Depo cikis ve magaza sevkiyati',
  'Bayi ve sube planlamasi',
  'Sefer durum takibi',
  'Iade ve yeniden dagitim akisi'
];

const approach = [
  {
    title: 'Planlama',
    description: 'Magaza ve bayi cikislarini kurallara gore planlariz.'
  },
  {
    title: 'Saha',
    description: 'Surucu ekipleri teslimat adimlarini mobil uygulamada izler.'
  },
  {
    title: 'Raporlama',
    description: 'Sefer ozetleri ve teslimat kaniti tek arsivde toplanir.'
  }
];

export default function RetailIndustryPage() {
  return (
    <div className="min-h-screen bg-gray-50 pt-24">
      <section className="py-20 bg-gradient-to-br from-blue-600 via-blue-700 to-indigo-700 text-white">
        <div className="max-w-5xl mx-auto px-4 sm:px-6 lg:px-8 text-center">
          <h1 className="text-4xl lg:text-5xl font-bold mb-6">Perakende</h1>
          <p className="text-blue-100 max-w-3xl mx-auto">
            Magaza ve bayi sevkiyatlarini tek panelde planlayip gorunur hale getirin.
          </p>
        </div>
      </section>

      <section className="py-16 bg-white">
        <div className="max-w-6xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="text-center mb-10">
            <h2 className="text-3xl font-bold text-gray-900 mb-4">Odak Noktalari</h2>
            <p className="text-gray-600 max-w-2xl mx-auto">
              Perakende sevkiyatinda surec ve gorunurluk kritik oldugunda yaninizdayiz.
            </p>
          </div>
          <div className="grid sm:grid-cols-2 gap-4">
            {focus.map((item) => (
              <div key={item} className="bg-gray-50 rounded-xl px-4 py-3 border border-gray-200 shadow-sm flex items-center gap-3">
                <span className="h-2.5 w-2.5 rounded-full bg-blue-600"></span>
                <span className="text-sm text-gray-700">{item}</span>
              </div>
            ))}
          </div>
        </div>
      </section>

      <section className="py-16 bg-gray-50">
        <div className="max-w-6xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="grid md:grid-cols-3 gap-6">
            {approach.map((item) => (
              <div key={item.title} className="bg-white rounded-2xl p-6 border border-gray-200 shadow-sm">
                <h3 className="text-lg font-semibold text-gray-900 mb-2">{item.title}</h3>
                <p className="text-sm text-gray-600">{item.description}</p>
              </div>
            ))}
          </div>
        </div>
      </section>

      <section className="py-16 bg-white">
        <div className="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 text-center">
          <h2 className="text-3xl font-bold text-gray-900 mb-4">Perakende ekibiniz icin demo planlayin</h2>
          <p className="text-gray-600 mb-8">
            Magaza ve bayi operasyonunuza uygun akisi birlikte kuralim.
          </p>
          <Link
            href="/contact"
            className="inline-flex items-center gap-2 bg-blue-600 hover:bg-blue-700 text-white px-8 py-4 rounded-xl font-semibold transition-all duration-200 shadow-lg"
          >
            Demo Talep Edin
          </Link>
        </div>
      </section>
    </div>
  );
}
