import IndustryLandingPage from '@/components/landing/IndustryLandingPage';
import { createPageMetadata } from '@/lib/seo';

export const metadata = createPageMetadata({
  title: 'Perakende ve Şube Sevkiyatı',
  description: 'Mağaza, bayi ve şube sevkiyatlarında rota planlama, teslimat takibi ve saha koordinasyonu için YolPilot.',
  path: '/industries/retail',
  keywords: ['perakende sevkiyat', 'mağaza dağıtımı', 'bayi sevkiyat yönetimi']
});

export default function RetailIndustryPage() {
  return (
    <IndustryLandingPage
      eyebrow="Perakende"
      title="Mağaza ve bayi sevkiyatlarını planlı, izlenebilir ve standart hale getirin"
      description="YolPilot, perakende ekiplerinin şube, bayi ve mağaza teslimatlarını tek operasyon akışında planlamasını; teslimat sonucunu, iadeyi ve istisnaları daha düzenli takip etmesini sağlar."
      proof={[
        { label: 'Odak alanı', value: 'Şube ve bayi sevkiyatı' },
        { label: 'Operasyon tipi', value: 'Düzenli dağıtım' },
        { label: 'Çıktı', value: 'Teslimat özeti + kanıt' }
      ]}
      problems={[
        'Mağaza ve bayi teslimatları gün içinde farklı önceliklerle değiştiğinde planlama tekrar manuel hale geliyor.',
        'Depodan çıkış, teslimat sonucu ve iade akışı aynı görünümde takip edilmediği için saha ile merkez kopuyor.',
        'Sevkiyat sonucu ve teslimat kanıtı arşivi standart tutulmadığında sonradan kontrol zorlaşıyor.',
        'Perakende operasyonlarında düzenli rota akışı ile istisna yönetimi aynı sistemde toplanmadığında ekipler yavaşlıyor.'
      ]}
      outcomes={[
        {
          title: 'Düzenli dağıtım akışı kurulur',
          description: 'Şube, bayi ve mağaza teslimatlarını tekrarlanabilir bir plan yapısına oturtabilirsiniz.'
        },
        {
          title: 'İade ve sapmalar daha görünür olur',
          description: 'Teslimat istisnası, mağaza notu ve iade bilgisi operasyon sırasında kayıt altına alınır.'
        },
        {
          title: 'Merkez ekip daha hızlı karar verir',
          description: 'Teslimat sonucu, araç kullanımı ve mağaza bazlı dağıtım görünürlüğü tek panelde toplanır.'
        }
      ]}
      workflow={[
        {
          title: 'Dağıtım yapınızı tanımlayın',
          description: 'Mağaza, bayi ve dağıtım önceliklerinizi depo ve rota kurallarıyla birlikte kurarız.'
        },
        {
          title: 'Saha ekiplerini akışa bağlayın',
          description: 'Sürücü uygulamasında durak sırası, mağaza notları ve teslimat kanıtı akışı sadeleştirilir.'
        },
        {
          title: 'Sonuçları raporlayın',
          description: 'Hangi mağazaya ne zaman gidildiğini, hangi teslimatların tamamlandığını ve hangi istisnaların oluştuğunu raporlayabilirsiniz.'
        }
      ]}
      faqs={[
        {
          question: 'Bayi ve mağaza teslimatları aynı yapıdan yönetilebilir mi?',
          answer: 'Evet. Farklı teslimat tiplerini aynı operasyon panelinde kurguya göre ayırıp yönetebilirsiniz.'
        },
        {
          question: 'İade ve başarısız teslimat kaydı tutulabiliyor mu?',
          answer: 'Evet. İade, eksik teslimat veya başarısız ziyaret durumları not ve kanıt ile birlikte kayda alınabilir.'
        },
        {
          question: 'Perakende ekipleri için tekrar eden rota kurgusu yapılabilir mi?',
          answer: 'Evet. Operasyon tipinize göre tekrar eden dağıtım akışları ve planlama yapısı oluşturulabilir.'
        }
      ]}
    />
  );
}
