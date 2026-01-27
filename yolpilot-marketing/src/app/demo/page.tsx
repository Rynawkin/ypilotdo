import Link from 'next/link';

const steps = [
  {
    title: 'Ihtiyac Analizi',
    description: 'Operasyon akisini ve ekip yapisini birlikte netlestiririz.'
  },
  {
    title: 'Canli Gosterim',
    description: 'Rota planlama, saha uygulamasi ve raporlama akisini gosteririz.'
  },
  {
    title: 'Pilot Plan',
    description: 'Uygun kurulum ve devreye alma adimlarini birlikte belirleriz.'
  }
];

const focusAreas = [
  'Rota planlama ve oncelik kurallari',
  'Surucu ve arac yonetimi',
  'Musteri bildirimleri',
  'Teslimat kaniti arsivi',
  'Raporlama panolari'
];

export default function DemoPage() {
  return (
    <div className="min-h-screen bg-gray-50 pt-24">
      <section className="py-20 bg-gradient-to-br from-blue-600 via-blue-700 to-indigo-700 text-white">
        <div className="max-w-5xl mx-auto px-4 sm:px-6 lg:px-8 text-center">
          <h1 className="text-4xl lg:text-5xl font-bold mb-6">Demo</h1>
          <p className="text-blue-100 max-w-3xl mx-auto">
            YolPilot&apos;in operasyonunuza nasil uyum saglayacagini birlikte gorun.
          </p>
        </div>
      </section>

      <section className="py-16 bg-white">
        <div className="max-w-6xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="grid md:grid-cols-3 gap-6 mb-12">
            {steps.map((step) => (
              <div key={step.title} className="bg-gray-50 rounded-2xl p-6 border border-gray-200 shadow-sm">
                <h2 className="text-lg font-semibold text-gray-900 mb-2">{step.title}</h2>
                <p className="text-sm text-gray-600">{step.description}</p>
              </div>
            ))}
          </div>
          <div className="bg-blue-50 rounded-2xl p-6 border border-blue-100">
            <h3 className="text-lg font-semibold text-gray-900 mb-3">Demo sirasinda odaklanilan noktalar</h3>
            <ul className="grid sm:grid-cols-2 gap-3 text-sm text-gray-700">
              {focusAreas.map((item) => (
                <li key={item} className="flex items-center gap-2">
                  <span className="h-2 w-2 rounded-full bg-blue-600"></span>
                  <span>{item}</span>
                </li>
              ))}
            </ul>
          </div>
        </div>
      </section>

      <section className="py-16 bg-gray-50">
        <div className="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 text-center">
          <h2 className="text-3xl font-bold text-gray-900 mb-4">Demo planlamak ister misiniz?</h2>
          <p className="text-gray-600 mb-8">
            Ekibimiz size uygun bir zaman planlamak icin iletisim kurar.
          </p>
          <Link
            href="/contact"
            className="inline-flex items-center gap-2 bg-blue-600 hover:bg-blue-700 text-white px-8 py-4 rounded-xl font-semibold transition-all duration-200 shadow-lg"
          >
            Demo Talep Et
          </Link>
        </div>
      </section>
    </div>
  );
}
