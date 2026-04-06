import ResourcePageLayout from '@/components/landing/ResourcePageLayout';
import { createPageMetadata } from '@/lib/seo';

export const metadata = createPageMetadata({
  title: 'API ve Entegrasyon Desteği',
  description:
    'YolPilot API kapsamı, entegrasyon yaklaşımı ve teknik ekip desteği hakkında özet bilgi alın.',
  path: '/api-docs',
  keywords: ['YolPilot API', 'lojistik yazılım entegrasyonu', 'rota planlama API']
});

export default function ApiDocsPage() {
  return (
    <ResourcePageLayout
      eyebrow="API ve entegrasyon"
      title="YolPilot’u mevcut sistemlerinize kontrollü şekilde bağlayın"
      description="Sipariş, müşteri, rota ve teslimat verisini mevcut ERP, e-ticaret veya operasyon altyapınızla uyumlu şekilde akıtmak için teknik ekiplerle birlikte ilerliyoruz."
      highlights={[
        {
          title: 'Güvenli erişim modeli',
          description:
            'Rol bazlı erişim, yetki kontrolleri ve loglama ile entegrasyon trafiği daha kontrollü yürütülür.'
        },
        {
          title: 'Gerçek operasyon verisi',
          description:
            'Müşteri, durak, sefer ve teslimat durumu gibi ana nesneler aynı operasyon akışında birlikte taşınır.'
        },
        {
          title: 'Kurulum desteği',
          description:
            'Teknik ekipler arası devri hızlandırmak için örnek akışlar, erişim kapsamı ve kurulum adımları birlikte netleştirilir.'
        }
      ]}
      sections={[
        {
          title: 'Entegrasyon kapsamı',
          description:
            'Sipariş oluşturma, müşteri güncelleme, rota atama, teslimat durumu ve operasyon raporları gibi başlıkları birlikte planlıyoruz.'
        },
        {
          title: 'Teknik yaklaşım',
          description:
            'API erişimi, webhook senaryoları ve veri eşleme mantığı; mevcut operasyon sisteminize göre sadeleştirilmiş bir kurulum planı ile ilerler.'
        },
        {
          title: 'Onboarding akışı',
          description:
            'İlk entegrasyon görüşmesinde veri modeli, güvenlik gereksinimleri ve canlıya geçiş sırası netleştirilir.'
        }
      ]}
      bulletSection={{
        title: 'Teknik ekipler en çok neleri soruyor?',
        description:
          'Dokümantasyon paylaşımı öncesinde genelde aşağıdaki başlıkları birlikte netleştiriyoruz.',
        items: [
          'API erişim modeli ve yetkilendirme yaklaşımı',
          'Sipariş ve müşteri verisinin hangi sıklıkta senkronlanacağı',
          'Teslimat sonucu ve durum değişimlerinin hangi sistemlere döneceği',
          'Webhook veya batch veri akışının daha uygun olduğu senaryolar',
          'Canlıya geçişten önce hangi test akışlarının çalıştırılacağı'
        ]
      }}
      footerTitle="Teknik kurulum senaryonuzu birlikte netleştirelim"
      footerDescription="Ekibinizin kullandığı sistemleri ve veri akışını paylaşın. YolPilot’un hangi entegrasyon modeliyle daha hızlı devreye alınabileceğini birlikte planlayalım."
      primaryTrackingName="api-docs-primary-cta"
      secondaryCtaLabel="Teknik doküman özeti"
      secondaryCtaHref="/docs/api"
      secondaryTrackingName="api-docs-secondary-cta"
    />
  );
}
