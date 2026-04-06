import IndustryLandingPage from '@/components/landing/IndustryLandingPage';
import { createPageMetadata } from '@/lib/seo';

export const metadata = createPageMetadata({
  title: 'Lojistik ve Filo Operasyonları',
  description: 'Depo, filo, sürücü ve sefer takibini tek panelde yöneten lojistik ekipleri için YolPilot çözümleri.',
  path: '/industries/logistics',
  keywords: ['lojistik yazılımı', 'filo yönetimi', 'depo operasyonu', 'sefer planlama']
});

export default function LogisticsIndustryPage() {
  return (
    <IndustryLandingPage
      eyebrow="Lojistik"
      title="Depo, filo ve sefer operasyonlarını tek merkezden görünür hale getirin"
      description="YolPilot, dağıtım ve lojistik ekiplerinin depo çıkışlarını, aktif seferleri, araç ve sürücü kullanımını aynı operasyon panelinde takip etmesini sağlar."
      proof={[
        { label: 'Odak alanı', value: 'Filo + depo görünürlüğü' },
        { label: 'Operasyon tipi', value: 'Sefer planlama ve takip' },
        { label: 'Çıktı', value: 'Canlı durum + rapor' }
      ]}
      problems={[
        'Depo çıkış planı, araç ataması ve sefer durumu farklı ekipler arasında parçalı kaldığında koordinasyon zorlaşıyor.',
        'Hangi aracın hangi rotada olduğu, hangi sürücünün aktif seferde bulunduğu gün içinde net görülemiyor.',
        'Teslimat sonucu, istisna durumu ve sahadan gelen notlar raporlamak için sonradan toplanıyor.',
        'Filo büyüdükçe planlama ve performans izleme tek bir görünüm yerine birden fazla araca bölünüyor.'
      ]}
      outcomes={[
        {
          title: 'Sefer yönetimi netleşir',
          description: 'Sefer, rota, sürücü ve araç ilişkilerini tek panelde takip ederek operasyonu daha hızlı koordine edersiniz.'
        },
        {
          title: 'Depo çıkışı ve dönüşü görünür olur',
          description: 'Depodan çıkış, aktif durak akışı ve dönüş bilgileri operasyon sırasında kayda geçer.'
        },
        {
          title: 'Raporlama karar almaya yaklaşır',
          description: 'Aktif operasyon, teslimat sonucu ve saha notları tek veri akışında toplandığı için raporlar daha anlamlı hale gelir.'
        }
      ]}
      workflow={[
        {
          title: 'Depo ve filo yapısını kurun',
          description: 'Depolar, araçlar, sürücüler ve operasyon kuralları sisteme tanımlanır.'
        },
        {
          title: 'Sefer ve rota akışını yönetin',
          description: 'Panel üzerinden planlama yapın, saha tarafına net görev akışı iletin ve değişiklikleri tek merkezden yönetin.'
        },
        {
          title: 'Operasyon sonucunu raporlayın',
          description: 'Teslimat sonucu, sefer özeti, sapmalar ve performans çıktıları tek raporlama katmanına taşınır.'
        }
      ]}
      faqs={[
        {
          question: 'Birden fazla depoyu aynı panelden yönetebilir miyiz?',
          answer: 'Evet. Depo bazlı yapı kurup rotaları, araçları ve seferleri ayrı görünürlüklerle yönetebilirsiniz.'
        },
        {
          question: 'Araç ve sürücü performansı izlenebilir mi?',
          answer: 'Evet. Araç, sürücü ve sefer bazlı görünüm ile günlük operasyonu ve tarihsel kayıtları takip edebilirsiniz.'
        },
        {
          question: 'Teslimat kanıtı lojistik operasyonunda da tutuluyor mu?',
          answer: 'Evet. Teslimat sonucu, fotoğraf, not ve istisna kayıtları operasyon akışının parçası olarak saklanabilir.'
        }
      ]}
    />
  );
}
