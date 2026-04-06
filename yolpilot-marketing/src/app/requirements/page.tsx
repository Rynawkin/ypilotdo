import ResourcePageLayout from '@/components/landing/ResourcePageLayout';
import { createPageMetadata } from '@/lib/seo';

export const metadata = createPageMetadata({
  title: 'Sistem Gereksinimleri',
  description:
    'YolPilot web paneli, mobil uygulama ve operasyon verisi tarafında gerekli temel kurulum ihtiyaçlarını inceleyin.',
  path: '/requirements',
  keywords: ['YolPilot sistem gereksinimleri', 'lojistik yazılım kurulum', 'mobil saha uygulaması gereksinim']
});

export default function RequirementsPage() {
  return (
    <ResourcePageLayout
      eyebrow="Sistem gereksinimleri"
      title="Kurulum öncesi ihtiyaçları netleştirip devreye almayı hızlandırın"
      description="YolPilot için ağır teknik yatırım gerekmiyor; ancak web paneli, mobil uygulama ve operasyon verisi tarafında temel hazırlıkların net olması kurulum süresini ciddi biçimde kısaltıyor."
      highlights={[
        {
          title: 'Web paneli',
          description:
            'Güncel bir tarayıcı ve istikrarlı internet bağlantısı; operasyon yöneticileri için temel başlangıç ihtiyacıdır.'
        },
        {
          title: 'Mobil uygulama',
          description:
            'Saha ekipleri için konum, kamera ve bildirim izinlerinin doğru açıldığı iOS veya Android cihazlar gerekir.'
        },
        {
          title: 'Operasyon verisi',
          description:
            'Müşteri adresleri, araç ve sürücü bilgileri ile teslimat notlarının düzenli şekilde hazırlanması önemlidir.'
        }
      ]}
      sections={[
        {
          title: 'İlk kurulum hazırlığı',
          description:
            'Depo, araç, sürücü ve müşteri yapısının hangi sırayla yükleneceğini önceden netleştirmek canlıya geçişi hızlandırır.'
        },
        {
          title: 'Saha kullanımı hazırlığı',
          description:
            'Mobil ekiplerde konum, kamera ve teslimat kanıtı akışlarının sorunsuz çalışması için cihaz izinleri kritik önemdedir.'
        },
        {
          title: 'Veri kalitesi',
          description:
            'Adres doğruluğu, durak notları ve teslimat talimatlarının temiz olması, ilk optimizasyon sonuçlarını doğrudan etkiler.'
        }
      ]}
      bulletSection={{
        title: 'Kurulumdan önce hazırlanması gerekenler',
        description:
          'Aşağıdaki başlıklar hazır olduğunda onboarding ve ilk demo sonrası canlı kullanım daha kısa sürede başlar.',
        items: [
          'Müşteri adres listesi ve teslimat notları',
          'Araç, sürücü ve depo verisi',
          'Operasyonda rol alacak kullanıcı listesi',
          'Bildirim ve takip akışına dair temel beklentiler',
          'Varsa entegrasyon gereksinimlerinin ön listesi'
        ]
      }}
      footerTitle="Kurulum hazırlığını birlikte çıkaralım"
      footerDescription="Elinizdeki veriyi ve ekip yapınızı paylaşın. YolPilot için minimum kurulum adımlarını birlikte netleştirelim."
      primaryTrackingName="requirements-primary-cta"
      secondaryCtaLabel="Demo planlayın"
      secondaryCtaHref="/contact?intent=demo"
      secondaryTrackingName="requirements-secondary-cta"
    />
  );
}
