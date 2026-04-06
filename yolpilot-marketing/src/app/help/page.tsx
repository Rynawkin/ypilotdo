import ResourcePageLayout from '@/components/landing/ResourcePageLayout';
import { createPageMetadata } from '@/lib/seo';

export const metadata = createPageMetadata({
  title: 'Yardım Merkezi',
  description:
    'YolPilot yardım merkezi: hesap, rota planlama, saha uygulaması ve raporlama başlıkları için hızlı yönlendirme.',
  path: '/help',
  keywords: ['YolPilot yardım', 'lojistik yazılım destek', 'rota planlama yardım']
});

export default function HelpPage() {
  return (
    <ResourcePageLayout
      eyebrow="Yardım merkezi"
      title="Doğru soruyu doğru kanala daha hızlı taşıyın"
      description="YolPilot yardım merkezi, operasyon ekibinin en sık takıldığı başlıkları toparlar ve sizi destek, dokümantasyon veya demo akışına doğru şekilde yönlendirir."
      highlights={[
        {
          title: 'Hesap ve yetkiler',
          description:
            'Rol yapısı, kullanıcı erişimleri ve ekip kurulumu ile ilgili temel başlıklarda hızlı yönlendirme sağlar.'
        },
        {
          title: 'Rota ve sefer akışları',
          description:
            'Planlama, atama, saha ilerleyişi ve teslimat sonucu gibi ana operasyon akışlarını gruplayarak gösterir.'
        },
        {
          title: 'Saha ve raporlama',
          description:
            'Mobil kullanım, bildirimler, teslimat kanıtı ve rapor ekranlarıyla ilgili sık sorulan soruları öne çıkarır.'
        }
      ]}
      sections={[
        {
          title: 'Kurulum ve onboarding',
          description:
            'İlk ekip kurulumu, kullanıcı hesapları, depo ve araç yapısı gibi başlangıç adımlarını hızla netleştirir.'
        },
        {
          title: 'Operasyon kullanımı',
          description:
            'Rota planlama, sefer oluşturma, saha takibi ve teslimat sonrası süreçlerin temel mantığını açıklar.'
        },
        {
          title: 'Bildirim ve kanıt akışı',
          description:
            'Müşteri bilgilendirme, takip linki ve teslimat kanıtı gibi görünürlük sağlayan modüller için yönlendirme sunar.'
        }
      ]}
      bulletSection={{
        title: 'En çok aranan başlıklar',
        description:
          'Yardım merkezi özellikle aşağıdaki sorular için hızlı giriş noktası gibi çalışmalıdır.',
        items: [
          'Yeni kullanıcı veya sürücü nasıl eklenir?',
          'Rota ve sefer akışı hangi sırayla ilerler?',
          'Teslimat kanıtı ve fotoğraf zorunluluğu nasıl yönetilir?',
          'Müşteri bilgilendirme akışları nasıl yapılandırılır?',
          'Rapor ekranlarında hangi çıktılar takip edilmelidir?'
        ]
      }}
      footerTitle="Doğrudan destek ekibiyle ilerlemek isterseniz"
      footerDescription="Sorununuzu kısa bir notla paylaşın. Ekibimiz sizi doğru başlığa ve doğru sonraki adıma yönlendirsin."
      primaryTrackingName="help-primary-cta"
      secondaryCtaLabel="Destek kanalları"
      secondaryCtaHref="/support"
      secondaryTrackingName="help-secondary-cta"
    />
  );
}
