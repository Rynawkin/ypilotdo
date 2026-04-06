import IndustryLandingPage from '@/components/landing/IndustryLandingPage';
import { createPageMetadata } from '@/lib/seo';

export const metadata = createPageMetadata({
  title: 'E-ticaret Dağıtım Operasyonları',
  description: 'E-ticaret dağıtım ekipleri için rota planlama, müşteri bilgilendirme ve teslimat kanıtını tek akışta yönetin.',
  path: '/industries/ecommerce',
  keywords: ['e-ticaret dağıtım yazılımı', 'son kilometre teslimat', 'rota optimizasyonu e-ticaret']
});

export default function EcommerceIndustryPage() {
  return (
    <IndustryLandingPage
      eyebrow="E-ticaret"
      title="Yoğun sipariş akışını planlı, görünür ve yönetilebilir hale getirin"
      description="YolPilot, e-ticaret ekiplerinin çok duraklı dağıtımı tek merkezden planlamasını; saha operasyonunu, müşteri bilgilendirmesini ve teslimat kanıtını aynı akışta yönetmesini sağlar."
      proof={[
        { label: 'Odak alanı', value: 'Çok duraklı teslimat' },
        { label: 'Operasyon tipi', value: 'Son kilometre dağıtım' },
        { label: 'Takip çıktısı', value: 'Link + kanıt + rapor' }
      ]}
      problems={[
        'Siparişler farklı ekranlardan planlandığı için rota sıralaması ve sürücü ataması gecikiyor.',
        'Müşteriye giden bilgi ile saha ekibinin gördüğü durum aynı olmadığı için destek yükü artıyor.',
        'Teslimat kanıtı, iade ve başarısız teslimat kayıtları sonradan toparlanmak zorunda kalıyor.',
        'Gün içinde adres değişikliği, öncelik değişimi veya yoğunluk dalgalanması olduğunda ekip tekrar manuel müdahaleye dönüyor.'
      ]}
      outcomes={[
        {
          title: 'Planlama tek ekranda netleşir',
          description: 'Rota, sürücü ve araç atamalarını tek panelde görüp operasyon önceliklerine göre hızlıca güncelleyebilirsiniz.'
        },
        {
          title: 'Müşteri iletişimi tutarlı olur',
          description: 'Takip bağlantısı, bildirim akışları ve teslimat durumları merkezden yönetildiği için ekipler aynı bilgiyle çalışır.'
        },
        {
          title: 'Teslimat kanıtı operasyonun parçası olur',
          description: 'Fotoğraf, imza, not ve başarısız teslimat nedeni aynı akışta kayda geçtiği için sonradan arşiv toplama ihtiyacı azalır.'
        }
      ]}
      workflow={[
        {
          title: 'Siparişleri ve kuralları sisteme alın',
          description: 'Dağıtım yapınıza uygun depo, rota, zaman ve bilgilendirme kurallarını birlikte tanımlarız.'
        },
        {
          title: 'Saha akışını mobil uygulamaya indirin',
          description: 'Sürücü uygulamasında durak sırası, teslimat notu, navigasyon ve kanıt toplama adımlarını netleştiririz.'
        },
        {
          title: 'Operasyonu canlı izleyin ve raporlayın',
          description: 'Gün içindeki sapmaları, teslimat sonuçlarını ve kanıt arşivini panelden takip edip kararları hızlandırırsınız.'
        }
      ]}
      faqs={[
        {
          question: 'Yoğun kampanya günlerinde de kullanılabilir mi?',
          answer: 'Evet. Operasyon hacmine göre rota planı, sürücü ataması ve müşteri bilgilendirme akışları aynı panelden yönetilir.'
        },
        {
          question: 'Teslimat başarısız olursa nasıl kayıt altına alınır?',
          answer: 'Sürücü uygulaması üzerinden neden, not ve teslimat kanıtı girilebilir; bu kayıtlar panelde raporlanır.'
        },
        {
          question: 'Müşteriye takip bilgisi verebilir miyiz?',
          answer: 'Evet. Takip bağlantısı ve durum bildirimleriyle teslimat sürecini müşteriye daha görünür hale getirebilirsiniz.'
        }
      ]}
    />
  );
}
