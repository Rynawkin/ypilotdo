import Link from 'next/link';

const featureGroups = [
  {
    title: 'Rota Planlama ve Optimizasyon',
    description: 'Seferleri tek ekranda planlayin, onceliklere gore hizla guncelleyin.',
    items: [
      'Durak siralama ve rota akisi',
      'Zaman penceresi kurallari',
      'Depo cikis ve varis akisi',
      'Kisit ve oncelik yonetimi',
      'Dinamik guncelleme senaryolari'
    ]
  },
  {
    title: 'Saha Uygulamasi',
    description: 'Suruculerin tum gorevlerini tek uygulamadan yurutmesini saglayin.',
    items: [
      'Mobil gorev listesi',
      'Navigasyon baglantilari',
      'Offline calisma modu',
      'Teslimat notlari ve talimatlar',
      'Imza ve fotograf ile kanit'
    ]
  },
  {
    title: 'Musteri Iletisimi',
    description: 'Musterilere zamaninda ve tutarli bilgilendirme gonderin.',
    items: [
      'Takip linki ve durum paylasimi',
      'WhatsApp ve e-posta bildirimleri',
      'Tahmini varis bilgilendirmesi',
      'Geri bildirim formlari',
      'Iptal ve iade akislari'
    ]
  },
  {
    title: 'Raporlama ve Analiz',
    description: 'Operasyonu gorunur kilan raporlarla karar surecini hizlandirin.',
    items: [
      'Sefer ozeti ve teslimat arsivi',
      'Basarisiz teslimat yonetimi',
      'Performans panosu',
      'Rota ve maliyet analizi',
      'Kisi ve ekip bazli gorunumler'
    ]
  },
  {
    title: 'Entegrasyonlar',
    description: 'Mevcut sistemlere baglanarak akisi tek yerde yonetin.',
    items: [
      'REST API ve webhook destegi',
      'ERP ve CRM baglantilari',
      'Dosya aktarim senaryolari',
      'Harita servisleri ile uyum',
      'Veri disa aktarim'
    ]
  },
  {
    title: 'Guvenlik ve Yonetim',
    description: 'Yetki ve kayitlarla operasyonu denetlenebilir kilin.',
    items: [
      'Rol bazli yetkilendirme',
      'Islem kayitlari ve loglama',
      'Veri yedekleme politikasi',
      'KVKK odakli veri yonetimi',
      'Erisim kontrolu'
    ]
  }
];

const modules = [
  {
    title: 'Yonetim Paneli',
    description: 'Planlama, takip ve raporlamayi tek panelde birlestirin.',
    items: ['Sefer planlama', 'Durum takibi', 'Raporlama ekranlari']
  },
  {
    title: 'Saha Uygulamasi',
    description: 'Suruculer icin sade ve hizli bir is akisi olusturun.',
    items: ['Gorev listesi', 'Teslimat kaniti', 'Not ve talimatlar']
  },
  {
    title: 'Musteri Takip Deneyimi',
    description: 'Musterilerin teslimatini guvenle takip etmesini saglayin.',
    items: ['Takip linki', 'Durum bildirimleri', 'Teslimat ozeti']
  }
];

const highlights = [
  'Coklu depo ve filoya uygun planlama',
  'Surucu ve arac yonetimi',
  'Etiketleme ve notlar',
  'Takvim ve vardiya planlamasi',
  'Bildirim senaryolari',
  'Teslimat kaniti arsivi',
  'Iade ve iptal akislari',
  'Esnek raporlama filtreleri'
];

export default function FeaturesPage() {
  return (
    <div className="min-h-screen bg-gray-50 pt-24">
      <section className="py-20 bg-gradient-to-br from-blue-600 via-blue-700 to-indigo-700 text-white">
        <div className="max-w-5xl mx-auto px-4 sm:px-6 lg:px-8 text-center">
          <div className="inline-flex items-center gap-2 bg-white/20 px-4 py-2 rounded-full mb-6">
            <span className="text-sm font-semibold">YolPilot Platformu</span>
          </div>
          <h1 className="text-4xl lg:text-5xl font-bold mb-6">
            Operasyonunuzu tek ekranda yonetin
          </h1>
          <p className="text-lg text-blue-100 max-w-3xl mx-auto mb-10">
            Planlama, saha, teslimat kaniti ve musteri iletisimini tek akista birlestiren
            sade ve guvenilir bir platform.
          </p>
          <div className="flex flex-col sm:flex-row gap-4 justify-center">
            <Link
              href="/contact"
              className="bg-white text-blue-600 hover:bg-blue-50 px-8 py-4 rounded-xl font-semibold text-lg transition-all duration-200 shadow-lg"
            >
              Demo Talep Edin
            </Link>
            <Link
              href="/contact"
              className="border-2 border-white text-white hover:bg-white hover:text-blue-600 px-8 py-4 rounded-xl font-semibold text-lg transition-all duration-200"
            >
              Iletisim
            </Link>
          </div>
        </div>
      </section>

      <section className="py-16 bg-white">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="text-center mb-12">
            <h2 className="text-3xl lg:text-4xl font-bold text-gray-900 mb-4">
              Temel Ozellik Alanlari
            </h2>
            <p className="text-gray-600 max-w-2xl mx-auto">
              Her ekip icin anlasilir, esnek ve buyumeye uygun bir kurgu.
            </p>
          </div>
          <div className="grid md:grid-cols-2 lg:grid-cols-3 gap-8">
            {featureGroups.map((group) => (
              <div
                key={group.title}
                className="bg-gray-50 rounded-2xl p-6 border border-gray-200 shadow-sm hover:shadow-md transition-shadow duration-200"
              >
                <h3 className="text-xl font-semibold text-gray-900 mb-3">{group.title}</h3>
                <p className="text-sm text-gray-600 mb-4">{group.description}</p>
                <ul className="space-y-2 text-sm text-gray-700">
                  {group.items.map((item) => (
                    <li key={item} className="flex items-start gap-2">
                      <span className="mt-1 h-2 w-2 rounded-full bg-blue-600"></span>
                      <span>{item}</span>
                    </li>
                  ))}
                </ul>
              </div>
            ))}
          </div>
        </div>
      </section>

      <section className="py-16 bg-gray-50">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="text-center mb-12">
            <h2 className="text-3xl lg:text-4xl font-bold text-gray-900 mb-4">
              Platform Modulleri
            </h2>
            <p className="text-gray-600 max-w-2xl mx-auto">
              Operasyonun her adimini baglayan moduller ile tam gorunurluk.
            </p>
          </div>
          <div className="grid md:grid-cols-3 gap-8">
            {modules.map((module) => (
              <div
                key={module.title}
                className="bg-white rounded-2xl p-6 border border-gray-200 shadow-sm"
              >
                <h3 className="text-xl font-semibold text-gray-900 mb-3">{module.title}</h3>
                <p className="text-sm text-gray-600 mb-4">{module.description}</p>
                <ul className="space-y-2 text-sm text-gray-700">
                  {module.items.map((item) => (
                    <li key={item} className="flex items-start gap-2">
                      <span className="mt-1 h-2 w-2 rounded-full bg-green-500"></span>
                      <span>{item}</span>
                    </li>
                  ))}
                </ul>
              </div>
            ))}
          </div>
        </div>
      </section>

      <section className="py-16 bg-white">
        <div className="max-w-6xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="text-center mb-10">
            <h2 className="text-3xl lg:text-4xl font-bold text-gray-900 mb-4">
              Ek Yetkinlikler
            </h2>
            <p className="text-gray-600 max-w-2xl mx-auto">
              Operasyonunuzu destekleyen, ayarlanabilir ve guvenli ek yetenekler.
            </p>
          </div>
          <div className="grid sm:grid-cols-2 gap-4">
            {highlights.map((item) => (
              <div
                key={item}
                className="flex items-center gap-3 bg-gray-50 rounded-xl px-4 py-3 border border-gray-200"
              >
                <span className="h-2.5 w-2.5 rounded-full bg-indigo-600"></span>
                <span className="text-sm text-gray-700">{item}</span>
              </div>
            ))}
          </div>
        </div>
      </section>

      <section className="py-20 bg-gradient-to-br from-blue-600 via-blue-700 to-indigo-700 text-white">
        <div className="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 text-center">
          <h2 className="text-3xl lg:text-4xl font-bold mb-6">
            Operasyonunuzu YolPilot ile netlestirin
          </h2>
          <p className="text-blue-100 mb-8">
            Ihtiyaciniza uygun kurgu ve kurulum icin ekibimizle gorusun.
          </p>
          <Link
            href="/contact"
            className="inline-flex items-center gap-2 bg-white text-blue-600 hover:bg-blue-50 px-8 py-4 rounded-xl font-semibold text-lg transition-all duration-200 shadow-lg"
          >
            Demo Planlayin
          </Link>
        </div>
      </section>
    </div>
  );
}
