import ResourcePageLayout from '@/components/landing/ResourcePageLayout';
import { createPageMetadata } from '@/lib/seo';

export const metadata = createPageMetadata({
  title: 'Destek',
  description:
    'YolPilot destek kanalları, onboarding ve operasyon soruları için iletişim seçenekleri.',
  path: '/support',
  keywords: ['YolPilot destek', 'lojistik yazılım destek', 'yardım merkezi']
});

export default function SupportPage() {
  return (
    <ResourcePageLayout
      eyebrow="Destek"
      title="Kurulumdan günlük kullanıma kadar doğru kanaldan destek alın"
      description="YolPilot destek yapısı; onboarding, operasyon akışları, ürün kullanımı ve teknik yönlendirme ihtiyaçlarını tek bir genel iletişim kutusuna sıkıştırmak yerine başlıklara ayırarak ele alır."
      highlights={[
        {
          title: 'Kurulum ve onboarding',
          description:
            'İlk hesap açılışı, ekip kurulumu, süreç devri ve başlangıç kurgusu için planlı destek sağlar.'
        },
        {
          title: 'Operasyon soruları',
          description:
            'Rota, sefer, saha uygulaması, teslimat kanıtı ve raporlama başlıklarında hızlı yönlendirme sunar.'
        },
        {
          title: 'Ürün geri bildirimi',
          description:
            'Sürekli kullanılan operasyon akışlarında oluşan ihtiyaçları ürün geliştirme tarafına taşır.'
        }
      ]}
      sections={[
        {
          title: 'E-posta',
          description:
            'info@yolpilot.com üzerinden demo sonrası sorularınızı, kurulum taleplerinizi veya destek notlarınızı paylaşabilirsiniz.'
        },
        {
          title: 'Telefon',
          description:
            '0850 756 62 67 üzerinden iş saatleri içinde ekip yapınızı ve ihtiyaçlarınızı daha hızlı değerlendirebiliriz.'
        },
        {
          title: 'Doğru kanal, doğru konu',
          description:
            'Teknik, operasyonel ve satış sonrası soruları uygun akışa ayırarak daha net ve hızlı geri dönüş sağlarız.'
        }
      ]}
      bulletSection={{
        title: 'En sık destek konuları',
        description:
          'Bu başlıklar en sık destek ihtiyacı doğuran alanlar olduğu için rehber ve onboarding tarafında da önceliklendirilir.',
        items: [
          'Kurulum ve hesap açılışı',
          'Rota planlama ve sefer akışı',
          'Saha uygulaması kullanımı',
          'Bildirim ve teslimat kanıtı yapısı',
          'Raporlama ve operasyon görünürlüğü'
        ]
      }}
      footerTitle="Desteğe mi ihtiyacınız var?"
      footerDescription="İhtiyacınızı kısa bir notla paylaşın, ekibimiz sizi doğru başlık üzerinden geri arasın."
      primaryTrackingName="support-primary-cta"
      secondaryCtaLabel="İletişim formu"
      secondaryCtaHref="/contact?intent=demo"
      secondaryTrackingName="support-secondary-cta"
    />
  );
}
