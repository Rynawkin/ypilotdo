import Link from 'next/link';

const sections = [
  {
    title: 'Veri Sorumlusu',
    content:
      'YolPilot hizmetinin veri sorumlusu Bakircilar Grup olup, gizlilik prensipleri bu sayfada ozetlenmektedir.'
  },
  {
    title: 'Toplanan Veriler',
    content:
      'Iletisim bilgileri, hesap verileri, operasyon kayitlari, cihaz ve konum bilgileri hizmetin sunulmasi icin islenebilir.'
  },
  {
    title: 'Kullanim Amaclari',
    content:
      'Hizmeti sunmak, destek saglamak, guvenligi iyilestirmek ve yasal yukumlulukleri yerine getirmek icin kullanilir.'
  },
  {
    title: 'Veri Paylasimi',
    content:
      'Veriler, hizmetin devami icin gerekli olan tedarikcilerle ve yasal zorunluluklarda yetkili kurumlarla paylasilabilir.'
  },
  {
    title: 'Saklama ve Guvenlik',
    content:
      'Veriler is amacina uygun sure boyunca saklanir ve yetkisiz erisime karsi korunur.'
  },
  {
    title: 'Haklariniz',
    content:
      'Veri isleme ile ilgili taleplerinizi iletisim kanallarindan bize iletebilirsiniz.'
  }
];

export default function PrivacyPage() {
  return (
    <div className="min-h-screen bg-gray-50 pt-24">
      <section className="py-20 bg-gradient-to-br from-blue-600 via-blue-700 to-indigo-700 text-white">
        <div className="max-w-5xl mx-auto px-4 sm:px-6 lg:px-8 text-center">
          <h1 className="text-4xl lg:text-5xl font-bold mb-6">Gizlilik Politikasi</h1>
          <p className="text-blue-100 max-w-3xl mx-auto">
            YolPilot kullanicilarinin verileri gizlilik odakli bir yaklasimla islenir.
          </p>
        </div>
      </section>

      <section className="py-16 bg-white">
        <div className="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 space-y-6">
          {sections.map((item) => (
            <div key={item.title} className="bg-gray-50 rounded-2xl p-6 border border-gray-200">
              <h2 className="text-lg font-semibold text-gray-900 mb-2">{item.title}</h2>
              <p className="text-sm text-gray-600">{item.content}</p>
            </div>
          ))}
        </div>
      </section>

      <section className="py-16 bg-gray-50">
        <div className="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 text-center">
          <h2 className="text-3xl font-bold text-gray-900 mb-4">KVKK Bilgilendirme</h2>
          <p className="text-gray-600 mb-8">
            KVKK kapsamindaki bilgilendirme metnini dokumanlar alaninda bulabilirsiniz.
          </p>
          <div className="flex flex-col sm:flex-row gap-4 justify-center">
            <Link
              href="/docs/kvkk"
              className="bg-blue-600 hover:bg-blue-700 text-white px-8 py-4 rounded-xl font-semibold transition-all duration-200 shadow-lg"
            >
              KVKK Metni
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
