import Link from 'next/link';

const categories = [
  {
    title: 'E-ticaret ve Siparis Sistemleri',
    description: 'Siparis verisini rota planina aktarip teslimat akisina baglayin.'
  },
  {
    title: 'ERP ve Muhasebe',
    description: 'Sefer ve teslimat verisini mevcut kurumsal sistemlerle senkronize edin.'
  },
  {
    title: 'CRM ve Saha Satis',
    description: 'Musteri bilgilerini tek yerden yonetip saha ekibine yansitin.'
  },
  {
    title: 'Harita ve Navigasyon',
    description: 'Harita servisleri ile adres arama ve navigasyon akisini netlestirin.'
  },
  {
    title: 'Bildirim Kanallari',
    description: 'WhatsApp, e-posta ve SMS gibi kanallari senaryoya gore baglayin.'
  },
  {
    title: 'Dosya Aktarimi',
    description: 'CSV ve benzeri formatlarla toplu veri aktarimi yapin.'
  }
];

const apiHighlights = [
  'REST API ile siparis ve musteri aktarimi',
  'Webhook ile durum tetikleme',
  'Guvenli anahtar ve yetki yonetimi',
  'Teknik dokumantasyon paylasimi',
  'Test ortaminda dogrulama'
];

const steps = [
  {
    title: 'Kesif',
    description: 'Veri kaynaklari ve is akislarini birlikte belirleriz.'
  },
  {
    title: 'Planlama',
    description: 'Entegrasyon yontemi, veri eslestirme ve test senaryolari netlesir.'
  },
  {
    title: 'Test ve Yayina Gecis',
    description: 'Dogrulama yapilir, sonra canli ortama gecilir.'
  }
];

export default function IntegrationsPage() {
  return (
    <div className="min-h-screen bg-gray-50 pt-24">
      <section className="py-20 bg-gradient-to-br from-blue-600 via-blue-700 to-indigo-700 text-white">
        <div className="max-w-5xl mx-auto px-4 sm:px-6 lg:px-8 text-center">
          <h1 className="text-4xl lg:text-5xl font-bold mb-6">Entegrasyonlar</h1>
          <p className="text-blue-100 max-w-3xl mx-auto">
            YolPilot, mevcut sistemlerinize uyum saglamak icin esnek entegrasyon secenekleri sunar.
          </p>
          <div className="mt-8">
            <Link
              href="/contact"
              className="inline-flex items-center gap-2 bg-white text-blue-600 hover:bg-blue-50 px-8 py-4 rounded-xl font-semibold text-lg transition-all duration-200 shadow-lg"
            >
              Entegrasyon Gorusun
            </Link>
          </div>
        </div>
      </section>

      <section className="py-16 bg-white">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="grid md:grid-cols-2 lg:grid-cols-3 gap-8">
            {categories.map((item) => (
              <div key={item.title} className="bg-gray-50 rounded-2xl p-6 border border-gray-200 shadow-sm">
                <h2 className="text-xl font-semibold text-gray-900 mb-3">{item.title}</h2>
                <p className="text-sm text-gray-600">{item.description}</p>
              </div>
            ))}
          </div>
        </div>
      </section>

      <section className="py-16 bg-gray-50">
        <div className="max-w-6xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="text-center mb-12">
            <h2 className="text-3xl font-bold text-gray-900 mb-4">API ve Veri Akisi</h2>
            <p className="text-gray-600 max-w-2xl mx-auto">
              Sistemler arasinda saglam bir veri akisi kurarak operasyonu tek noktadan takip edin.
            </p>
          </div>
          <div className="grid sm:grid-cols-2 gap-4">
            {apiHighlights.map((item) => (
              <div key={item} className="flex items-center gap-3 bg-white rounded-xl px-4 py-3 border border-gray-200 shadow-sm">
                <span className="h-2.5 w-2.5 rounded-full bg-blue-600"></span>
                <span className="text-sm text-gray-700">{item}</span>
              </div>
            ))}
          </div>
        </div>
      </section>

      <section className="py-16 bg-white">
        <div className="max-w-6xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="text-center mb-12">
            <h2 className="text-3xl font-bold text-gray-900 mb-4">Entegrasyon Sureci</h2>
            <p className="text-gray-600 max-w-2xl mx-auto">
              Entegrasyon kurulumunu sade ve takip edilebilir adimlara boluyoruz.
            </p>
          </div>
          <div className="grid md:grid-cols-3 gap-6">
            {steps.map((step) => (
              <div key={step.title} className="bg-gray-50 rounded-2xl p-6 border border-gray-200 shadow-sm">
                <h3 className="text-lg font-semibold text-gray-900 mb-2">{step.title}</h3>
                <p className="text-sm text-gray-600">{step.description}</p>
              </div>
            ))}
          </div>
        </div>
      </section>

      <section className="py-16 bg-gray-50">
        <div className="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 text-center">
          <h2 className="text-3xl font-bold text-gray-900 mb-4">Entegrasyon ihtiyacinizi birlikte planlayalim</h2>
          <p className="text-gray-600 mb-8">
            Verinizin kaynagini ve hedefini birlikte netlestirip uygun yontemi belirleyelim.
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
