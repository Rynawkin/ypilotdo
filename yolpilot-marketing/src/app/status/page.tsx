import ResourcePageLayout from '@/components/landing/ResourcePageLayout';
import { createPageMetadata } from '@/lib/seo';

export const metadata = createPageMetadata({
  title: 'Durum Sayfası',
  description:
    'YolPilot hizmet durumu, planlı bakım ve operasyonel bilgilendirme yaklaşımı hakkında bilgi alın.',
  path: '/status',
  keywords: ['YolPilot durum', 'lojistik yazılım servis durumu', 'planlı bakım']
});

export default function StatusPage() {
  return (
    <ResourcePageLayout
      eyebrow="Servis durumu"
      title="Platform durumu ve planlı bilgilendirme yaklaşımını görün"
      description="YolPilot durum sayfası; olası kesintiler, planlı bakım dönemleri ve operasyonu etkileyebilecek teknik duyurular için resmi başvuru noktası olarak kurgulanır."
      highlights={[
        {
          title: 'Genel platform durumu',
          description:
            'Web paneli, mobil akış ve entegrasyon katmanını etkileyen kritik durumlar tek yerden paylaşılır.'
        },
        {
          title: 'Planlı çalışmalar',
          description:
            'Bakım ve iyileştirme dönemleri, operasyon ekiplerinin önceden hazırlık yapabilmesi için mümkün olduğunca önceden duyurulur.'
        },
        {
          title: 'Şeffaf olay kaydı',
          description:
            'Kesinti veya yavaşlama durumlarında özet durum, etki alanı ve çözüm süreci açık biçimde paylaşılır.'
        }
      ]}
      sections={[
        {
          title: 'Operasyon etkisi',
          description:
            'Duyurular teknik değil, operasyonu hangi seviyede etkilediği anlaşılacak netlikte paylaşılmalıdır.'
        },
        {
          title: 'Planlı bakım akışı',
          description:
            'Özellikle vardiya ve saha trafiğini etkileyebilecek saatlerde ekiplerin hazırlık yapabilmesi için önceden görünürlük gerekir.'
        },
        {
          title: 'Destek koordinasyonu',
          description:
            'Bir olay yaşandığında destek, ürün ve teknik ekiplerin aynı bilgi üzerinden hareket etmesi için tek kaynak görevi görür.'
        }
      ]}
      bulletSection={{
        title: 'Bu sayfa neyi amaçlar?',
        description:
          'Durum sayfası sadece teknik bir kayıt alanı değil; müşterinin operasyonel görünürlük ihtiyacını karşılayan resmi iletişim katmanıdır.',
        items: [
          'Kritik olayları hızlı duyurmak',
          'Planlı bakım dönemlerini önceden paylaşmak',
          'Operasyon etkisini sade bir dille anlatmak',
          'Destek ekibi ile aynı bilgi üzerinden ilerlemek'
        ]
      }}
      footerTitle="Canlı kullanım yaklaşımını konuşalım"
      footerDescription="Operasyonunuzun kritik saatleri, SLA beklentiniz veya bilgilendirme ihtiyaçlarınız varsa bunu demo öncesinde birlikte netleştirebiliriz."
      primaryTrackingName="status-primary-cta"
      secondaryCtaLabel="Destek ekibi"
      secondaryCtaHref="/support"
      secondaryTrackingName="status-secondary-cta"
    />
  );
}
