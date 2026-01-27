import Link from 'next/link';

const values = [
  {
    title: 'Operasyon netligi',
    description: 'Planlama, saha ve raporlama tek akista birlesir.'
  },
  {
    title: 'Saha uyumu',
    description: 'Mobil uygulama ile ekipler ayni veriye bakar.'
  },
  {
    title: 'Musteri guveni',
    description: 'Teslimat kaniti ve bilgilendirme standart hale gelir.'
  }
];

const approach = [
  {
    title: 'Dinleme ve analiz',
    description: 'Operasyonunuzu ve ihtiyaclarinizi birlikte netlestiririz.'
  },
  {
    title: 'Uyarlama ve kurulum',
    description: 'Sistem ayarlari ve veri aktarimini planlariz.'
  },
  {
    title: 'Surekli gelisim',
    description: 'Geri bildirimleri toplayip sureci iyilestiririz.'
  }
];

export default function AboutPage() {
  return (
    <div className="min-h-screen bg-gray-50 pt-24">
      <section className="py-20 bg-gradient-to-br from-blue-600 via-blue-700 to-indigo-700 text-white">
        <div className="max-w-5xl mx-auto px-4 sm:px-6 lg:px-8 text-center">
          <h1 className="text-4xl lg:text-5xl font-bold mb-6">Hakkimizda</h1>
          <p className="text-blue-100 max-w-3xl mx-auto">
            YolPilot, Bakircilar Grup tarafindan saha ve dagitim operasyonlarini
            kolaylastirmak icin gelistirilen bir platformdur.
          </p>
        </div>
      </section>

      <section className="py-16 bg-white">
        <div className="max-w-6xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="text-center mb-12">
            <h2 className="text-3xl font-bold text-gray-900 mb-4">Odak Noktamiz</h2>
            <p className="text-gray-600 max-w-2xl mx-auto">
              Ekiplerin planlama, saha ve musteri iletisimi arasinda kaybolmasini
              engelleyip operasyonu netlestirmek.
            </p>
          </div>
          <div className="grid md:grid-cols-3 gap-6">
            {values.map((item) => (
              <div key={item.title} className="bg-gray-50 rounded-2xl p-6 border border-gray-200 shadow-sm">
                <h3 className="text-lg font-semibold text-gray-900 mb-2">{item.title}</h3>
                <p className="text-sm text-gray-600">{item.description}</p>
              </div>
            ))}
          </div>
        </div>
      </section>

      <section className="py-16 bg-gray-50">
        <div className="max-w-5xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="text-center mb-12">
            <h2 className="text-3xl font-bold text-gray-900 mb-4">Nasil Calisiyoruz</h2>
            <p className="text-gray-600 max-w-2xl mx-auto">
              Kurulumdan operasyon takibine kadar ekiplerinizle birlikte ilerleriz.
            </p>
          </div>
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
          <h2 className="text-3xl font-bold text-gray-900 mb-4">YolPilot ile tanisin</h2>
          <p className="text-gray-600 mb-8">
            Operasyonunuz icin dogru akisi birlikte belirleyelim.
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
