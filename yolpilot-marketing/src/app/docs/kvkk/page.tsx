import Link from 'next/link';

const sections = [
  {
    title: 'Veri Sorumlusu',
    content:
      'Bu aydinlatma metni, Bakircilar Grup tarafindan sunulan YolPilot hizmeti kapsaminda islenen kisiler veriler icindir.'
  },
  {
    title: 'Islenen Veri Kategorileri',
    items: [
      'Kimlik ve iletisim bilgileri',
      'Musteri ve operasyon verileri',
      'Cihaz ve konum bilgileri',
      'Islem ve log kayitlari'
    ]
  },
  {
    title: 'Isleme Amaclari',
    items: [
      'Hizmeti sunmak ve gelistirmek',
      'Musteri destegi saglamak',
      'Guvenlik ve yetkilendirme sureclerini yurutmek',
      'Yasal yukumlulukleri yerine getirmek'
    ]
  },
  {
    title: 'Veri Aktarimi',
    content:
      'Veriler, hizmetin sunulmasi icin gerekli olan tedarikcilerle ve yasal gerekliliklerde yetkili kurumlarla paylasilabilir.'
  },
  {
    title: 'Saklama ve Guvenlik',
    content:
      'Veriler amaca uygun sure boyunca saklanir ve yetkisiz erisimi onlemek icin guvenlik tedbirleri uygulanir.'
  },
  {
    title: 'Haklariniz',
    items: [
      'Veri isleme bilgisi talep etme',
      'Verilerin duzeltilmesini isteme',
      'Isleme faaliyetlerine itiraz etme',
      'Silme ve imha taleplerini iletme'
    ]
  }
];

export default function KvkkPage() {
  return (
    <div className="min-h-screen bg-gray-50 pt-24">
      <section className="py-20 bg-gradient-to-br from-blue-600 via-blue-700 to-indigo-700 text-white">
        <div className="max-w-5xl mx-auto px-4 sm:px-6 lg:px-8 text-center">
          <h1 className="text-4xl lg:text-5xl font-bold mb-6">KVKK Aydinlatma Metni</h1>
          <p className="text-blue-100 max-w-3xl mx-auto">
            Kisisel verilerin islenmesine iliskin temel prensipler ve haklariniz burada ozetlenir.
          </p>
        </div>
      </section>

      <section className="py-16 bg-white">
        <div className="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 space-y-6">
          {sections.map((section) => (
            <div key={section.title} className="bg-gray-50 rounded-2xl p-6 border border-gray-200">
              <h2 className="text-lg font-semibold text-gray-900 mb-2">{section.title}</h2>
              {section.items ? (
                <ul className="space-y-2 text-sm text-gray-600">
                  {section.items.map((item) => (
                    <li key={item} className="flex items-start gap-2">
                      <span className="mt-1 h-2 w-2 rounded-full bg-blue-600"></span>
                      <span>{item}</span>
                    </li>
                  ))}
                </ul>
              ) : (
                <p className="text-sm text-gray-600">{section.content}</p>
              )}
            </div>
          ))}
        </div>
      </section>

      <section className="py-16 bg-gray-50">
        <div className="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 text-center">
          <h2 className="text-3xl font-bold text-gray-900 mb-4">Iletisim</h2>
          <p className="text-gray-600 mb-6">
            KVKK kapsamindaki talepleriniz icin info@yolpilot.com adresinden veya 0850 756 62 67 numarasindan bize ulasabilirsiniz.
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
