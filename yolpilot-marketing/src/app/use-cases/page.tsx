import Link from 'next/link';

const useCases = [
  {
    title: 'E-ticaret Dagitim',
    summary: 'Gun icinde cok durakli dagitimlarda teslimat akisini sadelestirin.',
    needs: ['Hizli planlama', 'Musteri bilgilendirme', 'Teslimat kaniti'],
    solutions: ['Rota siralama ve oncelik', 'Takip linki', 'Imza ve fotograf']
  },
  {
    title: 'Perakende ve Bayi Sevkiyat',
    summary: 'Depo cikislarini tek merkezden gorunur hale getirin.',
    needs: ['Sefer takibi', 'Depo cikis kontrolu', 'Iade ve iptal yonetimi'],
    solutions: ['Canli durum panosu', 'Depo ve rota ayarlari', 'Teslimat notlari']
  },
  {
    title: 'Teknik Servis ve Saha Ekipleri',
    summary: 'Saha ekiplerinin gorev akisini standartlastirin.',
    needs: ['Gorev planlama', 'Adres ve notlar', 'Raporlama'],
    solutions: ['Mobil gorev listesi', 'Saha notlari', 'Islem arsivi']
  },
  {
    title: 'Gida ve Soguk Zincir',
    summary: 'Zaman hassas teslimatlarda surec guvencesi olusturun.',
    needs: ['Zaman penceresi', 'Teslimat kaniti', 'Rapor arsivi'],
    solutions: ['Kurallar ve oncelikler', 'Foto ve imza', 'Dokuman saklama']
  },
  {
    title: 'Saha Satis ve Dagitim',
    summary: 'Dagitim, tahsilat ve satis ekiplerini tek akista yonetin.',
    needs: ['Gorev takibi', 'Not ve talimat', 'Musteri iletisim'],
    solutions: ['Yonetim paneli', 'Saha uygulamasi', 'Bildirim senaryolari']
  },
  {
    title: 'Lojistik ve Operasyon',
    summary: 'Filo, surucu ve depotakibini tek panelde toplayin.',
    needs: ['Filo gorunurlugu', 'Rota kontrolu', 'Performans takibi'],
    solutions: ['Harita uzerinden izleme', 'Sefer ozeti', 'Raporlama panosu']
  }
];

const steps = [
  {
    title: 'Kesif ve Ihtiyac Analizi',
    description: 'Operasyon akisini birlikte inceler, gerekli modulleri belirleriz.'
  },
  {
    title: 'Kurulum ve Aktarim',
    description: 'Veri aktarimi ve ilk rota kurallari birlikte ayarlanir.'
  },
  {
    title: 'Yayina Gecis',
    description: 'Ekip egitimi tamamlanir, ilk seferler takip edilir.'
  }
];

export default function UseCasesPage() {
  return (
    <div className="min-h-screen bg-gray-50 pt-24">
      <section className="py-20 bg-gradient-to-br from-blue-600 via-blue-700 to-indigo-700 text-white">
        <div className="max-w-5xl mx-auto px-4 sm:px-6 lg:px-8 text-center">
          <h1 className="text-4xl lg:text-5xl font-bold mb-6">Kullanim Senaryolari</h1>
          <p className="text-blue-100 max-w-3xl mx-auto">
            YolPilot, farkli operasyon tiplerinde planlama, saha ve musteri iletisimini tek akista birlestirir.
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
          <div className="grid md:grid-cols-2 lg:grid-cols-3 gap-8">
            {useCases.map((useCase) => (
              <div key={useCase.title} className="bg-gray-50 rounded-2xl p-6 border border-gray-200 shadow-sm">
                <h2 className="text-xl font-semibold text-gray-900 mb-2">{useCase.title}</h2>
                <p className="text-sm text-gray-600 mb-4">{useCase.summary}</p>
                <div className="mb-4">
                  <div className="text-xs uppercase tracking-wide text-gray-500 mb-2">Ihtiyaclar</div>
                  <ul className="space-y-2 text-sm text-gray-700">
                    {useCase.needs.map((item) => (
                      <li key={item} className="flex items-start gap-2">
                        <span className="mt-1 h-2 w-2 rounded-full bg-blue-600"></span>
                        <span>{item}</span>
                      </li>
                    ))}
                  </ul>
                </div>
                <div>
                  <div className="text-xs uppercase tracking-wide text-gray-500 mb-2">YolPilot Yaklasimi</div>
                  <ul className="space-y-2 text-sm text-gray-700">
                    {useCase.solutions.map((item) => (
                      <li key={item} className="flex items-start gap-2">
                        <span className="mt-1 h-2 w-2 rounded-full bg-green-500"></span>
                        <span>{item}</span>
                      </li>
                    ))}
                  </ul>
                </div>
              </div>
            ))}
          </div>
        </div>
      </section>

      <section className="py-16 bg-gray-50">
        <div className="max-w-6xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="text-center mb-12">
            <h2 className="text-3xl font-bold text-gray-900 mb-4">Baslangic Adimlari</h2>
            <p className="text-gray-600 max-w-2xl mx-auto">
              Sureci netlestirir, ekibi hazirlar ve yayina hizli gecis saglariz.
            </p>
          </div>
          <div className="grid md:grid-cols-3 gap-6">
            {steps.map((step) => (
              <div key={step.title} className="bg-white rounded-2xl p-6 border border-gray-200 shadow-sm">
                <h3 className="text-lg font-semibold text-gray-900 mb-2">{step.title}</h3>
                <p className="text-sm text-gray-600">{step.description}</p>
              </div>
            ))}
          </div>
        </div>
      </section>

      <section className="py-16 bg-white">
        <div className="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 text-center">
          <h2 className="text-3xl font-bold text-gray-900 mb-4">Sektor bazli bilgi mi ariyorsunuz?</h2>
          <p className="text-gray-600 mb-8">
            Sektor sayfalarinda ihtiyaclara ozel kullanim detaylarini bulabilirsiniz.
          </p>
          <Link
            href="/industries/ecommerce"
            className="inline-flex items-center gap-2 bg-blue-600 hover:bg-blue-700 text-white px-8 py-4 rounded-xl font-semibold transition-all duration-200 shadow-lg"
          >
            Sektorleri Goruntuleyin
          </Link>
        </div>
      </section>
    </div>
  );
}
