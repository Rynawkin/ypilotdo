import ResourcePageLayout from '@/components/landing/ResourcePageLayout';
import { createPageMetadata } from '@/lib/seo';

export const metadata = createPageMetadata({
  title: 'Rehberler',
  description:
    'Rota planlama, saha uygulaması, müşteri bilgilendirme ve raporlama akışları için YolPilot rehberleri.',
  path: '/guides',
  keywords: ['YolPilot rehber', 'rota planlama rehberi', 'saha ekipleri kullanım']
});

export default function GuidesPage() {
  return (
    <ResourcePageLayout
      eyebrow="Rehberler"
      title="Kurulum ve kullanım sürecini hızlandıran kısa rehberler"
      description="YolPilot rehber alanı; operasyon yöneticileri, saha ekipleri ve destek ekipleri için ürün kullanımını hızlandıran kısa ve uygulanabilir içeriklere odaklanır."
      highlights={[
        {
          title: 'Rota planlama',
          description:
            'Durak yapısı, zaman penceresi ve operasyon kuralları gibi konuları başlangıç seviyesinde netleştirir.'
        },
        {
          title: 'Saha uygulaması',
          description:
            'Sürücü akışı, teslimat kanıtı, not yönetimi ve istisna durumlarını sade bir akışta anlatır.'
        },
        {
          title: 'Raporlama ve görünürlük',
          description:
            'Sefer özeti, performans görünümü ve müşteri iletişimi çıktılarının nasıl yorumlanacağını açıklar.'
        }
      ]}
      sections={[
        {
          title: 'Operasyon yöneticisi başlangıç rehberi',
          description:
            'İlk kurulumdan sonra hangi ekranların hangi sırayla devreye alınacağını ve ekibin nasıl organize edileceğini gösterir.'
        },
        {
          title: 'Sürücü onboarding akışı',
          description:
            'Mobil uygulama kullanımı, teslimat kanıtı, not ve durum güncelleme mantığını hızlıca öğretir.'
        },
        {
          title: 'Müşteri iletişimi akışı',
          description:
            'Takip linki, bildirim senaryosu ve operasyon ile destek ekipleri arasındaki sorumluluk dağılımını netleştirir.'
        }
      ]}
      bulletSection={{
        title: 'En çok talep edilen rehber başlıkları',
        description:
          'Kurulum öncesinde veya ilk kullanım döneminde genelde aşağıdaki başlıklar daha fazla ihtiyaç yaratıyor.',
        items: [
          'İlk rota ve araç yapısı nasıl kurulmalı?',
          'Sürücü ekibi için minimum eğitim akışı ne olmalı?',
          'Teslimat kanıtı süreci nasıl standardize edilmeli?',
          'Operasyon yöneticisi günlük olarak hangi ekranları izlemeli?',
          'Destek ve operasyon ekipleri arasındaki iletişim nasıl sadeleştirilmeli?'
        ]
      }}
      footerTitle="Ekibinize uygun rehber akışını birlikte çıkaralım"
      footerDescription="Hazır rehberler dışında ekibinizin en çok zorlandığı kullanım senaryolarını konuşup size uygun demo akışını planlayabiliriz."
      primaryTrackingName="guides-primary-cta"
      secondaryCtaLabel="Özellikleri inceleyin"
      secondaryCtaHref="/features"
      secondaryTrackingName="guides-secondary-cta"
    />
  );
}
