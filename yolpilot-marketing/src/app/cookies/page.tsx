import ResourcePageLayout from '@/components/landing/ResourcePageLayout';
import { createPageMetadata } from '@/lib/seo';

export const metadata = createPageMetadata({
  title: 'Çerez Politikası',
  description:
    'YolPilot web sitesinde kullanılan çerez türleri, amaçları ve kullanıcı kontrol seçenekleri hakkında bilgi alın.',
  path: '/cookies',
  keywords: ['YolPilot çerez politikası', 'çerez ayarları', 'site analitik çerezleri']
});

export default function CookiesPage() {
  return (
    <ResourcePageLayout
      eyebrow="Çerez politikası"
      title="Web deneyimini ölçmek ve iyileştirmek için kullandığımız çerezler"
      description="YolPilot marketing sitesinde; temel site işlevleri, kullanıcı tercihleri ve pazarlama performansını anlamak için sınırlı ve amaç odaklı çerez kullanımı benimsiyoruz."
      highlights={[
        {
          title: 'Zorunlu çerezler',
          description:
            'Temel oturum, güvenlik ve sayfa işlevlerinin sağlıklı çalışması için kullanılan çekirdek çerezlerdir.'
        },
        {
          title: 'Analitik amaçlı kullanım',
          description:
            'Site ziyaretlerini, CTA performansını ve kampanya etkisini anlamak için ölçümleme amaçlı sinyaller kullanılır.'
        },
        {
          title: 'Kullanıcı kontrolü',
          description:
            'Tarayıcı ayarları ve çerez tercihleri üzerinden belirli kullanım türlerini yönetebilirsiniz.'
        }
      ]}
      sections={[
        {
          title: 'Neden kullanıyoruz?',
          description:
            'Sayfa performansını, kampanya kaynaklarını ve form dönüşüm akışını anlamak için gerektiği kadar veri toplamayı hedefliyoruz.'
        },
        {
          title: 'Ne toplamıyoruz?',
          description:
            'Gereksiz kişisel veri toplamak yerine trafik kaynağı, ziyaret akışı ve dönüşüm davranışına odaklanıyoruz.'
        },
        {
          title: 'Nasıl yönetebilirsiniz?',
          description:
            'Tarayıcı ayarlarınız üzerinden çerezleri silebilir, kısıtlayabilir veya tamamen kapatabilirsiniz. Bu durumda bazı deneyimler sınırlanabilir.'
        }
      ]}
      bulletSection={{
        title: 'Kullanıcı kontrol seçenekleri',
        description:
          'Çerez kullanımı üzerinde doğrudan kontrol sağlamak için aşağıdaki yolları kullanabilirsiniz.',
        items: [
          'Tarayıcı ayarlarınızdan çerezleri silebilir veya engelleyebilirsiniz.',
          'Çerezleri kapatmak bazı form veya gezinme deneyimlerini sınırlayabilir.',
          'Yeni cihaz veya tarayıcıda tercihlerinizi tekrar ayarlamanız gerekebilir.',
          'Ek veri işleme bilgileri için gizlilik ve KVKK metinlerini inceleyebilirsiniz.'
        ]
      }}
      footerTitle="Veri işleme ve ölçümleme yaklaşımını birlikte netleştirelim"
      footerDescription="Gizlilik, ölçümleme veya kampanya takibiyle ilgili özel gereksinimleriniz varsa ekibimizle iletişime geçebilirsiniz."
      primaryTrackingName="cookies-primary-cta"
      secondaryCtaLabel="Gizlilik politikası"
      secondaryCtaHref="/privacy"
      secondaryTrackingName="cookies-secondary-cta"
    />
  );
}
