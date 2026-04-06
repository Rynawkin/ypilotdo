import ResourcePageLayout from '@/components/landing/ResourcePageLayout';
import { createPageMetadata } from '@/lib/seo';

export const metadata = createPageMetadata({
  title: 'Kaynaklar',
  description:
    'YolPilot kaynak merkezi: dokümantasyon, rehberler, yardım başlıkları ve yasal metinlere tek noktadan erişin.',
  path: '/resources',
  keywords: ['YolPilot kaynaklar', 'lojistik yazılım dokümanları', 'operasyon rehberleri']
});

export default function ResourcesPage() {
  return (
    <ResourcePageLayout
      eyebrow="Kaynak merkezi"
      title="Teknik ve operasyonel kaynakları tek noktada toplayın"
      description="YolPilot kaynak merkezi; satış öncesi değerlendirme, kurulum planı ve günlük kullanım süreci için ihtiyaç duyulan içerikleri daha düzenli bir yapıda toplamak üzere kurgulandı."
      highlights={[
        {
          title: 'Dokümantasyon',
          description:
            'API, KVKK, gizlilik ve kullanım şartları gibi temel belgeleri tek çatı altında toplar.'
        },
        {
          title: 'Kullanım rehberleri',
          description:
            'Operasyon ekibinin ürünü daha hızlı benimsemesi için pratik anlatımlar ve kısa senaryolar sunar.'
        },
        {
          title: 'Destek yönlendirmesi',
          description:
            'Sorununuzu doğru kanala taşımak için yardım merkezi ve destek akışlarıyla birlikte çalışır.'
        }
      ]}
      sections={[
        {
          title: 'Satış öncesi kaynaklar',
          description:
            'Karar vericilerin ürün kapsamını, güven yaklaşımını ve entegrasyon mantığını daha rahat değerlendirmesine yardımcı olur.'
        },
        {
          title: 'Kurulum dönemi kaynakları',
          description:
            'İlk kurulum, kullanıcı eğitimi ve saha operasyonu devri için ihtiyaç duyulan içerikleri aynı akışta sunar.'
        },
        {
          title: 'Sürekli kullanım kaynakları',
          description:
            'Raporlama, destek ve yeni ekip üyelerinin onboard edilmesi için tekrar başvurulabilecek bir kaynak katmanı oluşturur.'
        }
      ]}
      bulletSection={{
        title: 'Bu merkezde hangi içerikler olmalı?',
        description:
          'Kaynak merkezi sadece bir link listesi değil, satış ve kullanım sürecini kısaltan içerik kümesi olarak düşünülmeli.',
        items: [
          'Ürün ve entegrasyon özetleri',
          'Kurulum ve eğitim rehberleri',
          'Sık sorulan sorular ve yardım içerikleri',
          'Yasal ve güvenlik başlıkları',
          'Operasyon ekipleri için örnek akışlar'
        ]
      }}
      footerTitle="İhtiyacınıza göre doğru kaynakları birlikte seçelim"
      footerDescription="Teknik ekip, operasyon yöneticisi veya karar verici olarak hangi başlıklara ihtiyacınız olduğunu paylaşın. Sizi doğru içerik akışına yönlendirelim."
      primaryTrackingName="resources-primary-cta"
      secondaryCtaLabel="Dokümantasyon merkezi"
      secondaryCtaHref="/docs"
      secondaryTrackingName="resources-secondary-cta"
    />
  );
}
