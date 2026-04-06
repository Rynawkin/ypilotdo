import ResourcePageLayout from '@/components/landing/ResourcePageLayout';
import { createPageMetadata } from '@/lib/seo';

export const metadata = createPageMetadata({
  title: 'Kariyer',
  description:
    'YolPilot ekibinin çalışma biçimi, odak alanları ve açık roller hakkında bilgi alın.',
  path: '/careers',
  keywords: ['YolPilot kariyer', 'lojistik yazılım ekip', 'operasyon teknolojileri kariyer']
});

export default function CareersPage() {
  return (
    <ResourcePageLayout
      eyebrow="Kariyer"
      title="Operasyon problemlerini çözmeyi seven küçük ve odaklı ekip"
      description="YolPilot tarafında ürün, backend, tasarım ve müşteri başarısı ekipleri; sahadaki gerçek operasyon sorunlarını sade, güvenilir ve uygulanabilir akışlara çevirmeye odaklanır."
      highlights={[
        {
          title: 'Net sorumluluk',
          description:
            'Hangi işin kimde olduğu, müşteriye neyin teslim edildiği ve başarı kriterinin ne olduğu baştan nettir.'
        },
        {
          title: 'Ürün ve operasyon birlikte',
          description:
            'Teknik kararlar, sahadaki ekiplerin gerçekten kullandığı akışlarla birlikte değerlendirilir.'
        },
        {
          title: 'Küçük ekip, yüksek etki',
          description:
            'Uzun katmanlar yerine hızlı karar alma ve doğrudan çıktı üretme kültürü ön plandadır.'
        }
      ]}
      sections={[
        {
          title: 'Ürün ve tasarım',
          description:
            'Web paneli, mobil sürücü deneyimi ve operasyon dilinin tutarlı kurulması üzerinde çalışıyoruz.'
        },
        {
          title: 'Backend ve entegrasyonlar',
          description:
            'Gerçek zamanlı operasyon akışları, optimizasyon, raporlama ve dış sistem bağlantıları ana teknik odağımız.'
        },
        {
          title: 'Müşteri başarısı ve satış',
          description:
            'Kurulum sürecini hızlandıran, ürün kullanımını yaygınlaştıran ve doğru beklenti kuran ekip yapısını önemsiyoruz.'
        }
      ]}
      bulletSection={{
        title: 'Ekipte önem verdiğimiz çalışma biçimi',
        description:
          'Geniş söylemler yerine doğrudan iş çıkaran, net iletişim kuran ve sorumluluk alan ekip arkadaşlarıyla ilerlemek istiyoruz.',
        items: [
          'Kararlarını açık gerekçeyle savunabilmek',
          'Belirsiz alanları somutlaştırıp sahiplenmek',
          'Müşteri etkisini teknik detay kadar ciddiye almak',
          'Dağınık işleri sadeleştirmeyi doğal refleks haline getirmek'
        ]
      }}
      footerTitle="Rol olmasa da tanışabiliriz"
      footerDescription="İlginizi çeken bir alan varsa kısa bir not bırakın. YolPilot ekibi ve çalışma şekli hakkında size daha net bilgi verelim."
      primaryTrackingName="careers-primary-cta"
      secondaryCtaLabel="İletişim formu"
      secondaryCtaHref="/contact?intent=demo"
      secondaryTrackingName="careers-secondary-cta"
    />
  );
}
