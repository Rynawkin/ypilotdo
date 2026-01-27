import Link from 'next/link';

const statusItems = [
  {
    title: 'Platform Durumu',
    description: 'Genel durum guncellemeleri bu sayfadan paylasilir.'
  },
  {
    title: 'Planli Calismalar',
    description: 'Planli bakim ve iyilestirmeler onceden duyurulur.'
  },
  {
    title: 'Olay Kayitlari',
    description: 'Kesintiler ve onlemler burada takip edilir.'
  }
];

export default function StatusPage() {
  return (
    <div className="min-h-screen bg-gray-50 pt-24">
      <section className="py-20 bg-gradient-to-br from-blue-600 via-blue-700 to-indigo-700 text-white">
        <div className="max-w-5xl mx-auto px-4 sm:px-6 lg:px-8 text-center">
          <h1 className="text-4xl lg:text-5xl font-bold mb-6">Durum</h1>
          <p className="text-blue-100 max-w-3xl mx-auto">
            YolPilot hizmet durumunu takip edebileceginiz resmi sayfa.
          </p>
        </div>
      </section>

      <section className="py-16 bg-white">
        <div className="max-w-6xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="grid md:grid-cols-3 gap-6">
            {statusItems.map((item) => (
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
          <h2 className="text-3xl font-bold text-gray-900 mb-4">Bir sorun mu yasiyorsunuz?</h2>
          <p className="text-gray-600 mb-8">
            Durumla ilgili sorulariniz icin destek ekibimizle iletisime gecebilirsiniz.
          </p>
          <Link
            href="/support"
            className="inline-flex items-center gap-2 bg-blue-600 hover:bg-blue-700 text-white px-8 py-4 rounded-xl font-semibold transition-all duration-200 shadow-lg"
          >
            Destek
          </Link>
        </div>
      </section>
    </div>
  );
}
