import Link from 'next/link';

const values = [
  {
    title: 'Netlik',
    description: 'Hedefler ve sorumluluklar acik tanimlanir.'
  },
  {
    title: 'Is birligi',
    description: 'Tek ekip gibi hareket edip birlikte sonuc aliriz.'
  },
  {
    title: 'Sorumluluk',
    description: 'Musteri deneyimini sahiplenir ve teslim ederiz.'
  }
];

const focusAreas = [
  'Urun gelistirme ve tasarim',
  'Backend ve entegrasyonlar',
  'Musteri basarisi',
  'Satis ve is gelistirme'
];

export default function CareersPage() {
  return (
    <div className="min-h-screen bg-gray-50 pt-24">
      <section className="py-20 bg-gradient-to-br from-blue-600 via-blue-700 to-indigo-700 text-white">
        <div className="max-w-5xl mx-auto px-4 sm:px-6 lg:px-8 text-center">
          <h1 className="text-4xl lg:text-5xl font-bold mb-6">Kariyer</h1>
          <p className="text-blue-100 max-w-3xl mx-auto">
            YolPilot ekibi, operasyonel problemlere sade ve etkili cozumler uretmeye odaklanir.
          </p>
        </div>
      </section>

      <section className="py-16 bg-white">
        <div className="max-w-6xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="text-center mb-12">
            <h2 className="text-3xl font-bold text-gray-900 mb-4">Calisma Seklimiz</h2>
            <p className="text-gray-600 max-w-2xl mx-auto">
              Net hedefler, ortak dil ve sorumluluk odakli bir kultur.
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
          <div className="text-center mb-10">
            <h2 className="text-3xl font-bold text-gray-900 mb-4">Odak Alanlari</h2>
            <p className="text-gray-600 max-w-2xl mx-auto">
              Asagidaki alanlarda yeni ekip arkadaslariyla calismayi seviyoruz.
            </p>
          </div>
          <div className="grid sm:grid-cols-2 gap-4">
            {focusAreas.map((item) => (
              <div key={item} className="bg-white rounded-xl px-4 py-3 border border-gray-200 shadow-sm flex items-center gap-3">
                <span className="h-2.5 w-2.5 rounded-full bg-blue-600"></span>
                <span className="text-sm text-gray-700">{item}</span>
              </div>
            ))}
          </div>
        </div>
      </section>

      <section className="py-16 bg-white">
        <div className="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 text-center">
          <h2 className="text-3xl font-bold text-gray-900 mb-4">Basvurular icin</h2>
          <p className="text-gray-600 mb-8">
            Ilginizi ceken bir rol varsa bize kisaca kendinizden bahsedin.
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
