import Link from 'next/link';

const sections = [
  {
    title: 'Hizmet Kapsami',
    content:
      'YolPilot, Bakircilar Grup tarafindan sunulan ve rota planlama, saha uygulamasi ile teslimat takibini tek panelde toplayan bir yazilim hizmetidir.'
  },
  {
    title: 'Kullanim Sorumluluklari',
    content:
      'Kullanicilar, sisteme girilen verilerin dogrulugundan ve mevzuata uygunlugundan sorumludur.'
  },
  {
    title: 'Ucretlendirme',
    content:
      'Plan, fiyat ve kapsam detaylari taraflar arasinda belirlenir ve ayrica iletilir.'
  },
  {
    title: 'Veri ve Icerik',
    content:
      'Musteri verileri size aittir. Sistem uzerindeki verilerin kopyasini talep edebilirsiniz.'
  },
  {
    title: 'Sorumluluk Siniri',
    content:
      'Hizmetin kesintisiz sunulmasi hedeflenir. Zorunlu hallerde hizmette gecici aksama olabilir.'
  }
];

export default function TermsPage() {
  return (
    <div className="min-h-screen bg-gray-50 pt-24">
      <section className="py-20 bg-gradient-to-br from-blue-600 via-blue-700 to-indigo-700 text-white">
        <div className="max-w-5xl mx-auto px-4 sm:px-6 lg:px-8 text-center">
          <h1 className="text-4xl lg:text-5xl font-bold mb-6">Kullanim Kosullari</h1>
          <p className="text-blue-100 max-w-3xl mx-auto">
            Bu sayfa, YolPilot hizmetinin temel kullanim kosullarini ozetler.
          </p>
        </div>
      </section>

      <section className="py-16 bg-white">
        <div className="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 space-y-6">
          {sections.map((item) => (
            <div key={item.title} className="bg-gray-50 rounded-2xl p-6 border border-gray-200">
              <h2 className="text-lg font-semibold text-gray-900 mb-2">{item.title}</h2>
              <p className="text-sm text-gray-600">{item.content}</p>
            </div>
          ))}
        </div>
      </section>

      <section className="py-16 bg-gray-50">
        <div className="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 text-center">
          <h2 className="text-3xl font-bold text-gray-900 mb-4">Sorulariniz icin</h2>
          <p className="text-gray-600 mb-8">
            Kullanim kosullari hakkinda detayli bilgi icin bizimle iletisime gecin.
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
