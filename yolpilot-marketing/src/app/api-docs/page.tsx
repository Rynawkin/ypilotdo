import Link from 'next/link';

const sections = [
  {
    title: 'API Kapsami',
    description: 'Siparis, musteri ve rota verisini sistemler arasinda akitmak icin tasarlanmistir.'
  },
  {
    title: 'Guvenlik',
    description: 'Yetki anahtari, rol bazli erisim ve loglama ile korumali bir yapi sunar.'
  },
  {
    title: 'Entegrasyon Destegi',
    description: 'Kurulumda teknik ekip ve dokumantasyon destegi ile birlikte ilerlenir.'
  }
];

export default function ApiDocsPage() {
  return (
    <div className="min-h-screen bg-gray-50 pt-24">
      <section className="py-20 bg-gradient-to-br from-blue-600 via-blue-700 to-indigo-700 text-white">
        <div className="max-w-5xl mx-auto px-4 sm:px-6 lg:px-8 text-center">
          <h1 className="text-4xl lg:text-5xl font-bold mb-6">API Dokumantasyonu</h1>
          <p className="text-blue-100 max-w-3xl mx-auto">
            YolPilot API, mevcut sistemlerinize baglanip teslimat akisini tek noktadan yonetmenizi saglar.
          </p>
        </div>
      </section>

      <section className="py-16 bg-white">
        <div className="max-w-6xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="grid md:grid-cols-3 gap-6">
            {sections.map((item) => (
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
          <h2 className="text-3xl font-bold text-gray-900 mb-4">Dokumantasyona erisim</h2>
          <p className="text-gray-600 mb-8">
            API dokumanlarini paylasmamiz icin teknik ekibimizle iletisime gecin.
          </p>
          <div className="flex flex-col sm:flex-row gap-4 justify-center">
            <Link
              href="/docs/api"
              className="bg-blue-600 hover:bg-blue-700 text-white px-8 py-4 rounded-xl font-semibold transition-all duration-200 shadow-lg"
            >
              API Dokumani
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
