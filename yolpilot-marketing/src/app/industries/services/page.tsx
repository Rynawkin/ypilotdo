import IndustryLandingPage from '@/components/landing/IndustryLandingPage';
import { createPageMetadata } from '@/lib/seo';

export const metadata = createPageMetadata({
  title: 'Saha Servis ve Hizmet Operasyonları',
  description: 'Teknik servis ve saha ekipleri için görev planlama, mobil iş akışı ve işlem kanıtını tek sistemde yönetin.',
  path: '/industries/services',
  keywords: ['saha servis yazılımı', 'teknik servis rota planlama', 'mobil görev yönetimi']
});

export default function ServicesIndustryPage() {
  return (
    <IndustryLandingPage
      eyebrow="Hizmet Operasyonları"
      title="Saha ekipleri için görev planlama ve işlem takibini tek akışta toplayın"
      description="YolPilot, teknik servis ve saha ekiplerinin görev planlama, adres notu, işlem kanıtı ve müşteri bilgilendirme akışını tek platformda yönetmesine yardımcı olur."
      proof={[
        { label: 'Odak alanı', value: 'Saha ekipleri' },
        { label: 'Operasyon tipi', value: 'Görev ve randevu akışı' },
        { label: 'Çıktı', value: 'Mobil iş emri + rapor' }
      ]}
      problems={[
        'Randevular, görev notları ve saha ekiplerinin günlük planı farklı araçlarda tutulduğunda sahaya net iş akışı verilemiyor.',
        'Adreste yapılan işlem, kullanılan malzeme veya servis notu düzenli kayda geçmediğinde ekip içi görünürlük düşüyor.',
        'Müşteriye verilen bilgi ile saha ekibinin gerçek durumu aynı olmadığında memnuniyet etkileniyor.',
        'Teknik servis ekiplerinde fotoğraf, imza ve iş tamamlama kaydı operasyonun doğal parçası haline gelemiyor.'
      ]}
      outcomes={[
        {
          title: 'Görev akışı standartlaşır',
          description: 'Saha ekibi hangi işin ne zaman ve hangi notlarla yapılacağını aynı mobil akıştan görür.'
        },
        {
          title: 'İşlem kanıtı kayda girer',
          description: 'Fotoğraf, imza, not ve işlem tamamlanma bilgisi operasyon sırasında toplanır.'
        },
        {
          title: 'Merkez ekip daha net yönetir',
          description: 'Hangi görev tamamlandı, hangisi bekliyor, hangi ekip hangi adreste bunu tek ekrandan izleyebilirsiniz.'
        }
      ]}
      workflow={[
        {
          title: 'Görev tiplerini ve ekipleri kurun',
          description: 'Servis, saha ziyaretleri ve randevu tiplerini operasyonunuza uygun şekilde tanımlarız.'
        },
        {
          title: 'Mobil iş akışını sadeleştirin',
          description: 'Saha uygulamasında adres, görev notu, müşteri bilgisi ve kanıt toplama adımlarını netleştiririz.'
        },
        {
          title: 'Tamamlanan işleri raporlayın',
          description: 'Günlük görev sonucu, ekip performansı ve işlem arşivi aynı panelde görünür hale gelir.'
        }
      ]}
      faqs={[
        {
          question: 'Teknik servis ve teslimat benzeri saha operasyonları birlikte yönetilebilir mi?',
          answer: 'Evet. Farklı görev tiplerini aynı platform içinde farklı akışlarla yönetebilirsiniz.'
        },
        {
          question: 'Sahada yapılan işlemler için fotoğraf ve imza tutulabiliyor mu?',
          answer: 'Evet. Saha ekibi mobil uygulama üzerinden işlem kanıtını anında ekleyebilir.'
        },
        {
          question: 'Randevu bazlı planlama destekleniyor mu?',
          answer: 'Operasyon yapınıza göre zaman penceresi ve görev önceliği kurguları oluşturulabilir.'
        }
      ]}
    />
  );
}
