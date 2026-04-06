import IndustryLandingPage from '@/components/landing/IndustryLandingPage';
import { createPageMetadata } from '@/lib/seo';

export const metadata = createPageMetadata({
  title: 'Soğuk Zincir ve Zaman Pencereli Teslimatlar',
  description: 'Soğuk zincir ve hassas teslimat operasyonlarında zaman penceresi, teslimat kanıtı ve rapor görünürlüğünü tek panelde yönetin.',
  path: '/industries/cold-chain',
  keywords: ['soğuk zincir yazılımı', 'zaman pencereli teslimat', 'hassas teslimat yönetimi']
});

export default function ColdChainIndustryPage() {
  return (
    <IndustryLandingPage
      eyebrow="Soğuk Zincir"
      title="Zaman hassas teslimatlarda operasyon kalitesini görünür ve denetlenebilir tutun"
      description="YolPilot, soğuk zincir ve hassas dağıtım ekiplerinin rota görünürlüğünü, teslimat kanıtını ve operasyon kayıtlarını aynı akışta toplamasını sağlar."
      proof={[
        { label: 'Odak alanı', value: 'Zaman penceresi' },
        { label: 'Operasyon tipi', value: 'Hassas teslimat' },
        { label: 'Çıktı', value: 'Kanıt + rapor + arşiv' }
      ]}
      problems={[
        'Zaman penceresi, teslimat hassasiyeti ve saha kaydı birlikte yönetilmediğinde kalite kaybı oluşuyor.',
        'Teslimatın nasıl ve ne zaman tamamlandığına dair düzenli kayıt tutulmadığında denetim zorlaşıyor.',
        'Soğuk zincir operasyonlarında istisna ve gecikme bilgisinin dağınık olması saha ile merkez arasında kopukluk yaratıyor.',
        'Operasyon sonunda rapor ve kanıt arşivini manuel derlemek ekip üzerinde ek yük oluşturuyor.'
      ]}
      outcomes={[
        {
          title: 'Zaman hassasiyeti görünür hale gelir',
          description: 'Teslimat planı, operasyon durumu ve saha kaydı tek panelde daha takip edilebilir olur.'
        },
        {
          title: 'Kanıt ve kayıt arşivi güçlenir',
          description: 'Fotoğraf, imza, not ve durum kayıtları daha düzenli saklanır.'
        },
        {
          title: 'Denetim ve raporlama kolaylaşır',
          description: 'Operasyon sonucu, teslimat geçmişi ve istisna kayıtlarını tek bir raporlama yapısında toplayabilirsiniz.'
        }
      ]}
      workflow={[
        {
          title: 'Hassas operasyon kurallarını belirleyin',
          description: 'Zaman, teslimat önceliği ve saha doğrulama adımlarını operasyon yapınıza göre kurarız.'
        },
        {
          title: 'Sahada kanıt toplamayı standardize edin',
          description: 'Mobil uygulamada teslimat kanıtı ve operasyon notlarının zorunlu akışlarını netleştiririz.'
        },
        {
          title: 'Geçmişi ve sonucu izleyin',
          description: 'Denetim, müşteri geri bildirimi ve raporlama için kayıtları tek yerde toplarsınız.'
        }
      ]}
      faqs={[
        {
          question: 'Zaman pencereli teslimatlar için uygun mu?',
          answer: 'Evet. Operasyon planı ve saha akışı zaman hassasiyetini destekleyecek şekilde kurgulanabilir.'
        },
        {
          question: 'Teslimat kanıtı denetim için saklanabiliyor mu?',
          answer: 'Evet. Fotoğraf, imza ve not kayıtları düzenli arşiv yapısında tutulabilir.'
        },
        {
          question: 'Soğuk zincir operasyonu için özel ekran mı gerekiyor?',
          answer: 'Genellikle mevcut operasyon paneli ve mobil akış yeterlidir; ihtiyaç halinde süreçler birlikte kurgulanır.'
        }
      ]}
    />
  );
}
