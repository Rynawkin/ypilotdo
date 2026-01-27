import Link from 'next/link';

const stories = [
  {
    title: 'E-ticaret Dagitim Ekibi',
    sector: 'E-ticaret',
    challenge: 'Manuel planlama ve daginik musteri bilgilendirme.',
    outcome: [
      'Rota planlama merkezi bir akisa tasindi',
      'Musteri bilgilendirmeleri otomatiklestirildi',
      'Teslimat kaniti tek panelde toplandi'
    ],
    quote:
      'Operasyon ekibi tek panelden tum teslimatlari goruyor ve musteri iletisimini yonetiyor.'
  },
  {
    title: 'Saha Servis Operasyonu',
    sector: 'Teknik Servis',
    challenge: 'Gorev atama ve saha raporlari icin ortak bir sistem yoktu.',
    outcome: [
      'Gorevler suruculere net bir akista iletildi',
      'Saha notlari ve teslimat kaniti arsivlendi',
      'Raporlama sureci standarda oturdu'
    ],
    quote:
      'Saha ekibi artik gorevleri mobil uygulamadan takip ediyor, raporlar hazir geliyor.'
  },
  {
    title: 'Perakende Dagitim Agi',
    sector: 'Perakende',
    challenge: 'Depo cikislarinda takip eksigi ve sefer planlamasi karmasasi.',
    outcome: [
      'Depo ve sefer takibi gorunur hale geldi',
      'Planlama kurallari operasyon icin standartlasti',
      'Iade ve iptal akislari netlesti'
    ],
    quote:
      'Depolar arasi koordinasyon artik tek panelden yurutuluyor.'
  },
  {
    title: 'Soguk Zincir Dagitim',
    sector: 'Gida',
    challenge: 'Zaman penceresi ve teslimat kaniti kritik hale geldi.',
    outcome: [
      'Zaman pencereleri net kurallarla yonetildi',
      'Teslimat kaniti her durakta kaydedildi',
      'Raporlama ile surec izlenebilir oldu'
    ],
    quote:
      'Teslimat kanitlari tek yerde toplaninca ekip icinde guven artti.'
  }
];

const outcomes = [
  {
    title: 'Daha duzenli planlama',
    description: 'Rota ve sefer kurallari tutarli hale gelir.'
  },
  {
    title: 'Musteri gorunurlugu',
    description: 'Bildirim ve takip linkleri ile surec netlesir.'
  },
  {
    title: 'Saha uyumu',
    description: 'Mobil uygulama ile gorevler dogru sirada ilerler.'
  },
  {
    title: 'Dokumantasyon',
    description: 'Foto, imza ve notlar tek arsivde saklanir.'
  }
];

export default function CustomersPage() {
  return (
    <div className="min-h-screen bg-gray-50 pt-24">
      <section className="py-20 bg-gradient-to-br from-blue-600 via-blue-700 to-indigo-700 text-white">
        <div className="max-w-5xl mx-auto px-4 sm:px-6 lg:px-8 text-center">
          <h1 className="text-4xl lg:text-5xl font-bold mb-6">Musteri Deneyimleri</h1>
          <p className="text-blue-100 max-w-3xl mx-auto">
            Farkli operasyon tiplerinden ekiplerin YolPilot ile olusturdugu ortak faydalar.
          </p>
          <div className="mt-8">
            <Link
              href="/contact"
              className="inline-flex items-center gap-2 bg-white text-blue-600 hover:bg-blue-50 px-8 py-4 rounded-xl font-semibold text-lg transition-all duration-200 shadow-lg"
            >
              Demo Talep Edin
            </Link>
          </div>
        </div>
      </section>

      <section className="py-16 bg-white">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="grid md:grid-cols-2 gap-8">
            {stories.map((story) => (
              <div
                key={story.title}
                className="bg-gray-50 rounded-2xl p-8 border border-gray-200 shadow-sm"
              >
                <div className="text-xs uppercase tracking-wide text-blue-600 mb-2">
                  {story.sector}
                </div>
                <h2 className="text-2xl font-semibold text-gray-900 mb-3">{story.title}</h2>
                <p className="text-sm text-gray-600 mb-4">{story.challenge}</p>
                <ul className="space-y-2 text-sm text-gray-700 mb-4">
                  {story.outcome.map((item) => (
                    <li key={item} className="flex items-start gap-2">
                      <span className="mt-1 h-2 w-2 rounded-full bg-green-500"></span>
                      <span>{item}</span>
                    </li>
                  ))}
                </ul>
                <p className="text-sm italic text-gray-600">&quot;{story.quote}&quot;</p>
              </div>
            ))}
          </div>
        </div>
      </section>

      <section className="py-16 bg-gray-50">
        <div className="max-w-6xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="text-center mb-12">
            <h2 className="text-3xl font-bold text-gray-900 mb-4">Ortak Ciktilar</h2>
            <p className="text-gray-600 max-w-2xl mx-auto">
              Farkli ekipler, ortak bir operasyon standardi etrafinda bulusuyor.
            </p>
          </div>
          <div className="grid sm:grid-cols-2 lg:grid-cols-4 gap-6">
            {outcomes.map((item) => (
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
          <h2 className="text-3xl font-bold text-gray-900 mb-4">
            Kendi operasyonunuza uygun akisi birlikte kuralim
          </h2>
          <p className="text-gray-600 mb-8">
            Planlama, saha ve musteri tarafini tek deneyimde birlestirmek icin ekibimizle gorusun.
          </p>
          <div className="flex flex-col sm:flex-row gap-4 justify-center">
            <Link
              href="/contact"
              className="bg-blue-600 hover:bg-blue-700 text-white px-8 py-4 rounded-xl font-semibold transition-all duration-200 shadow-lg"
            >
              Iletisim
            </Link>
            <Link
              href="/use-cases"
              className="border-2 border-blue-600 text-blue-600 hover:bg-blue-50 px-8 py-4 rounded-xl font-semibold transition-all duration-200"
            >
              Kullanim Senaryolari
            </Link>
          </div>
        </div>
      </section>
    </div>
  );
}
