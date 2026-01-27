import Link from 'next/link';

const channels = [
  {
    title: 'E-posta',
    value: 'info@yolpilot.com',
    description: 'Destek taleplerinizi e-posta ile iletebilirsiniz.'
  },
  {
    title: 'Telefon',
    value: '0850 756 62 67',
    description: 'Is saatleri icinde destek hattimizdan ulasabilirsiniz.'
  }
];

const topics = [
  'Kurulum ve hesap acilis',
  'Rota planlama ve seferler',
  'Saha uygulamasi kullanimi',
  'Bildirim senaryolari',
  'Raporlama ve arsiv'
];

export default function SupportPage() {
  return (
    <div className="min-h-screen bg-gray-50 pt-24">
      <section className="py-20 bg-gradient-to-br from-blue-600 via-blue-700 to-indigo-700 text-white">
        <div className="max-w-5xl mx-auto px-4 sm:px-6 lg:px-8 text-center">
          <h1 className="text-4xl lg:text-5xl font-bold mb-6">Destek</h1>
          <p className="text-blue-100 max-w-3xl mx-auto">
            Sorulariniz ve destek talepleriniz icin yaninizdayiz.
          </p>
        </div>
      </section>

      <section className="py-16 bg-white">
        <div className="max-w-6xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="grid md:grid-cols-2 gap-6">
            {channels.map((item) => (
              <div key={item.title} className="bg-gray-50 rounded-2xl p-6 border border-gray-200 shadow-sm">
                <h2 className="text-lg font-semibold text-gray-900 mb-1">{item.title}</h2>
                <p className="text-blue-600 font-semibold mb-2">{item.value}</p>
                <p className="text-sm text-gray-600">{item.description}</p>
              </div>
            ))}
          </div>
        </div>
      </section>

      <section className="py-16 bg-gray-50">
        <div className="max-w-6xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="bg-white rounded-2xl p-6 border border-gray-200 shadow-sm">
            <h3 className="text-lg font-semibold text-gray-900 mb-3">Sik sorulan basliklar</h3>
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

      <section className="py-16 bg-white">
        <div className="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 text-center">
          <h2 className="text-3xl font-bold text-gray-900 mb-4">Destek talebi olusturun</h2>
          <p className="text-gray-600 mb-8">
            Ihtiyacinizi kisa bir notla paylasin, ekibimiz size geri donsun.
          </p>
          <Link
            href="/contact"
            className="inline-flex items-center gap-2 bg-blue-600 hover:bg-blue-700 text-white px-8 py-4 rounded-xl font-semibold transition-all duration-200 shadow-lg"
          >
            Iletisim Formu
          </Link>
        </div>
      </section>
    </div>
  );
}
