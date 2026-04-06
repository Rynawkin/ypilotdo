import ResourcePageLayout from '@/components/landing/ResourcePageLayout';
import { createPageMetadata } from '@/lib/seo';

export const metadata = createPageMetadata({
  title: 'Operasyon İçerikleri ve Notlar',
  description:
    'Teslimat operasyonu, rota planlama, müşteri bilgilendirme ve saha süreçleri üzerine YolPilot içerik merkezi.',
  path: '/blog',
  keywords: ['rota planlama içerikleri', 'teslimat operasyonu blog', 'saha ekipleri rehber']
});

export default function BlogPage() {
  return (
    <ResourcePageLayout
      eyebrow="İçerik merkezi"
      title="Operasyon ekipleri için daha faydalı içerikler hazırlıyoruz"
      description="YolPilot blog tarafı; rota planlama, saha koordinasyonu, teslimat kanıtı, müşteri iletişimi ve entegrasyon süreçleri üzerine daha uygulanabilir içerikler için hazırlanıyor."
      highlights={[
        {
          title: 'Operasyon odaklı',
          description:
            'İçerik planı genel pazarlama dili yerine gerçek teslimat ve saha operasyonu sorunlarına odaklanır.'
        },
        {
          title: 'Kısa ve uygulanabilir',
          description:
            'Ekiplerin hızlıca kullanabileceği kısa rehberler, kontrol listeleri ve örnek akışlar hazırlanır.'
        },
        {
          title: 'Ürünle bağlantılı',
          description:
            'Her içerik başlığı, YolPilot içinde karşılığı olan ekranlar ve operasyon çıktılarıyla ilişkilendirilir.'
        }
      ]}
      sections={[
        {
          title: 'Rota planlama notları',
          description:
            'Durak yoğunluğu, zaman penceresi, depo çıkışı ve saha kapasitesi gibi başlıklarda uygulamaya dönük içerikler.'
        },
        {
          title: 'Saha uygulaması akışları',
          description:
            'Sürücü deneyimi, teslimat kanıtı, istisna yönetimi ve müşteri bilgilendirme süreçlerini anlatan kısa formatlar.'
        },
        {
          title: 'Entegrasyon ve raporlama',
          description:
            'Veri akışı, raporlama görünürlüğü ve operasyon çıktılarının ölçülmesiyle ilgili teknik ve operasyonel içerikler.'
        }
      ]}
      bulletSection={{
        title: 'Yakında yayınlamayı planladığımız başlıklar',
        description:
          'Reklam ve satış trafiği için boş bir blog görüntüsü bırakmak yerine önce gerçekten işe yarayan içerik kümelerini hazırlıyoruz.',
        items: [
          'Teslimat ekibinde rota değişikliği nasıl daha kontrollü yönetilir?',
          'Saha ekiplerinde teslimat kanıtı standardı nasıl kurulur?',
          'Müşteri bilgilendirme yükü operasyon ekibinden nasıl alınır?',
          'Depo çıkıştan sefer kapanışına kadar görünürlük nasıl artar?',
          'Dağınık Excel akışlarından operasyon paneline geçiş nasıl planlanır?'
        ]
      }}
      footerTitle="Önce operasyonunuzu birlikte görelim"
      footerDescription="Hazır içerik beklemek yerine mevcut operasyonunuza göre örnek akış ve ekranları doğrudan demo üzerinden gösterebiliriz."
      primaryTrackingName="blog-primary-cta"
      secondaryCtaLabel="Özellikleri inceleyin"
      secondaryCtaHref="/features"
      secondaryTrackingName="blog-secondary-cta"
    />
  );
}
