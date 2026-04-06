import Link from 'next/link';

const useCases = [
  {
    title: 'E-ticaret Dağıtım',
    summary: 'Gün içinde çok duraklı dağıtımlarda teslimat akışını sadeleştirin.',
    needs: ['Hızlı planlama', 'Müşteri bilgilendirme', 'Teslimat kanıtı'],
    solutions: ['Rota sıralama ve öncelik', 'Takip linki', 'İmza ve fotoğraf']
  },
  {
    title: 'Perakende ve Bayi Sevkiyat',
    summary: 'Depo çıkışlarını tek merkezden görünür hale getirin.',
    needs: ['Sefer takibi', 'Depo çıkış kontrolü', 'İade ve iptal yönetimi'],
    solutions: ['Canlı durum panosu', 'Depo ve rota ayarları', 'Teslimat notları']
  },
  {
    title: 'Teknik Servis ve Saha Ekipleri',
    summary: 'Saha ekiplerinin görev akışını standartlaştırın.',
    needs: ['Görev planlama', 'Adres ve notlar', 'Raporlama'],
    solutions: ['Mobil görev listesi', 'Saha notları', 'İşlem arşivi']
  },
  {
    title: 'Gıda ve Soğuk Zincir',
    summary: 'Zaman hassas teslimatlarda süreç güvencesi oluşturun.',
    needs: ['Zaman penceresi', 'Teslimat kanıtı', 'Rapor arşivi'],
    solutions: ['Kurallar ve öncelikler', 'Foto ve imza', 'Doküman saklama']
  },
  {
    title: 'Saha Satış ve Dağıtım',
    summary: 'Dağıtım, tahsilat ve satış ekiplerini tek akışta yönetin.',
    needs: ['Görev takibi', 'Not ve talimat', 'Müşteri iletişimi'],
    solutions: ['Yönetim paneli', 'Saha uygulaması', 'Bildirim senaryoları']
  },
  {
    title: 'Lojistik ve Operasyon',
    summary: 'Filo, sürücü ve depo takibini tek panelde toplayın.',
    needs: ['Filo görünürlüğü', 'Rota kontrolü', 'Performans takibi'],
    solutions: ['Harita üzerinden izleme', 'Sefer özeti', 'Raporlama panosu']
  }
];

const steps = [
  {
    title: 'Keşif ve İhtiyaç Analizi',
    description: 'Operasyon akışını birlikte inceler, gerekli modülleri belirleriz.'
  },
  {
    title: 'Kurulum ve Aktarım',
    description: 'Veri aktarımı ve ilk rota kuralları birlikte ayarlanır.'
  },
  {
    title: 'Yayına Geçiş',
    description: 'Ekip eğitimi tamamlanır, ilk seferler takip edilir.'
  }
];

export default function UseCasesPage() {
  return (
    <div className="min-h-screen bg-gray-50 pt-24">
      <section className="py-20 bg-gradient-to-br from-blue-600 via-blue-700 to-indigo-700 text-white">
        <div className="max-w-5xl mx-auto px-4 sm:px-6 lg:px-8 text-center">
          <h1 className="text-4xl lg:text-5xl font-bold mb-6">Kullanım Senaryoları</h1>
          <p className="text-blue-100 max-w-3xl mx-auto">
            YolPilot, farklı operasyon tiplerinde planlama, saha ve müşteri iletişimini tek akışta birleştirir.
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
                  <div className="text-xs uppercase tracking-wide text-gray-500 mb-2">İhtiyaçlar</div>
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
                  <div className="text-xs uppercase tracking-wide text-gray-500 mb-2">YolPilot Yaklaşımı</div>
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
            <h2 className="text-3xl font-bold text-gray-900 mb-4">Başlangıç Adımları</h2>
            <p className="text-gray-600 max-w-2xl mx-auto">
              Süreci netleştirir, ekibi hazırlar ve yayına hızlı geçiş sağlarız.
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
          <h2 className="text-3xl font-bold text-gray-900 mb-4">Sektör bazlı bilgi mi arıyorsunuz?</h2>
          <p className="text-gray-600 mb-8">
            Sektör sayfalarında ihtiyaçlara özel kullanım detaylarını bulabilirsiniz.
          </p>
          <Link
            href="/industries/ecommerce"
            className="inline-flex items-center gap-2 bg-blue-600 hover:bg-blue-700 text-white px-8 py-4 rounded-xl font-semibold transition-all duration-200 shadow-lg"
          >
            Sektörleri Görüntüleyin
          </Link>
        </div>
      </section>
    </div>
  );
}
