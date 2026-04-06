import ResourcePageLayout from '@/components/landing/ResourcePageLayout';
import { createPageMetadata } from '@/lib/seo';

export const metadata = createPageMetadata({
  title: 'Dokümantasyon',
  description:
    'YolPilot dokümantasyon merkezi: API, KVKK, kullanım ve yasal metinler için ana başvuru sayfası.',
  path: '/docs',
  keywords: ['YolPilot dokümantasyon', 'lojistik yazılım doküman', 'teslimat operasyonu doküman']
});

export default function DocsPage() {
  return (
    <ResourcePageLayout
      eyebrow="Dokümantasyon"
      title="Teknik, operasyonel ve yasal kaynakları tek noktada toplayın"
      description="YolPilot ile çalışmaya başlamadan önce ihtiyaç duyulan teknik belgeleri, kullanım rehberlerini ve yasal metinleri tek akışta erişilebilir tutuyoruz."
      highlights={[
        {
          title: 'Teknik ekipler için',
          description:
            'API erişimi, entegrasyon kapsamı ve veri akışı gibi konular için temel başvuru noktası sağlar.'
        },
        {
          title: 'Operasyon ekipleri için',
          description:
            'Kurulum, kullanıcı rolleri, süreç akışları ve temel kullanım başlıklarını düzenli şekilde toplar.'
        },
        {
          title: 'Yasal çerçeve',
          description:
            'KVKK, gizlilik ve kullanım koşulları gibi resmi metinlerin güncel kopyalarını aynı merkezde tutar.'
        }
      ]}
      sections={[
        {
          title: 'API dokümanları',
          description:
            'Sipariş, müşteri, rota ve teslimat akışlarını sistemler arası bağlamak isteyen ekipler için özet bilgi ve erişim süreci.'
        },
        {
          title: 'Yasal metinler',
          description:
            'KVKK, gizlilik politikası, kullanım şartları ve çerez politikası gibi güven ve uyum başlıkları.'
        },
        {
          title: 'Yardım ve rehberler',
          description:
            'Operasyon ekibinin ürünü daha hızlı kullanması için yardım başlıkları ve rehber içeriklerin toplandığı alan.'
        }
      ]}
      bulletSection={{
        title: 'Bu merkez kimler için?',
        description:
          'Aynı sayfa içinde hem teknik ekiplerin hem operasyon yöneticilerinin hem de karar vericilerin ihtiyaç duyduğu başlıkları sadeleştiriyoruz.',
        items: [
          'Entegrasyon planlayan teknik ekipler',
          'Kurulum ve onboarding yöneten operasyon ekipleri',
          'Güvenlik ve veri işleme süreçlerini inceleyen karar vericiler',
          'Satış sonrası devri hızlandırmak isteyen müşteri ekipleri'
        ]
      }}
      footerTitle="İhtiyacınız olan dokümanı birlikte netleştirelim"
      footerDescription="Teknik veya operasyonel gereksiniminizi paylaşın. Sizi doğru doküman, doğru ekip ve doğru sonraki adımla eşleştirelim."
      primaryTrackingName="docs-primary-cta"
      secondaryCtaLabel="API doküman özeti"
      secondaryCtaHref="/docs/api"
      secondaryTrackingName="docs-secondary-cta"
    />
  );
}
