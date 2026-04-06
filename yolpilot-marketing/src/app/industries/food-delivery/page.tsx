import IndustryLandingPage from '@/components/landing/IndustryLandingPage';
import { createPageMetadata } from '@/lib/seo';

export const metadata = createPageMetadata({
  title: 'Yemek ve Hızlı Teslimat Operasyonları',
  description: 'Yemek ve hızlı dağıtım ekipleri için rota görünürlüğü, teslimat akışı ve müşteri bilgilendirme yapısını güçlendirin.',
  path: '/industries/food-delivery',
  keywords: ['yemek teslimatı yazılımı', 'hızlı teslimat operasyonu', 'kurye yönetimi']
});

export default function FoodDeliveryIndustryPage() {
  return (
    <IndustryLandingPage
      eyebrow="Yemek Teslimatı"
      title="Zaman hassas teslimatlarda saha hızını kaybetmeden görünürlüğü artırın"
      description="YolPilot, hızlı teslimat ve yemek operasyonlarında kurye akışını, teslimat durumunu ve müşteri bilgilendirme yapısını merkezden yönetmenize yardımcı olur."
      proof={[
        { label: 'Odak alanı', value: 'Hızlı teslimat' },
        { label: 'Operasyon tipi', value: 'Kurye koordinasyonu' },
        { label: 'Çıktı', value: 'Durum + hız + görünürlük' }
      ]}
      problems={[
        'Kısa teslimat sürelerinde operasyon büyüdükçe hangi kurye ne durumda sorusu görünmez hale geliyor.',
        'Sipariş ve saha bilgisi farklı sistemlerde kaldığında müşteri tarafında tutarlı iletişim zorlaşıyor.',
        'Gecikme, başarısız teslimat veya rota sapması ancak operasyon sonrasında fark ediliyor.',
        'Yüksek hacimde teslimat sonucu ve iş kanıtı arşivi düzenli tutulmadığında kalite kontrol zorlaşıyor.'
      ]}
      outcomes={[
        {
          title: 'Kurye akışı daha net görünür',
          description: 'Aktif görev, teslimat sonucu ve saha notları merkezi panelde daha net izlenir.'
        },
        {
          title: 'Müşteri tarafı daha tutarlı olur',
          description: 'Teslimat durumuna göre bilgilendirme kurgusu ve son kayıtlar daha güvenilir hale gelir.'
        },
        {
          title: 'Yoğun saatlerde kontrol kaybı azalır',
          description: 'Hızlı teslimat temposunda operasyonu yalnızca mesajlarla değil, görünür veriyle yönetebilirsiniz.'
        }
      ]}
      workflow={[
        {
          title: 'Teslimat tiplerini ve akışlarını kurun',
          description: 'Hızlı teslimat yapınıza göre görev tipi, öncelik ve bilgilendirme kurgusunu belirleriz.'
        },
        {
          title: 'Kurye uygulamasını netleştirin',
          description: 'Kuryelerin teslimat sırası, notlar ve durum güncellemeleri tek mobil akışta toplanır.'
        },
        {
          title: 'Yoğunluğu merkezden izleyin',
          description: 'Gecikme, saha sapması ve teslimat sonucu günlük operasyonda daha hızlı görünür hale gelir.'
        }
      ]}
      faqs={[
        {
          question: 'Canlı yemek teslimatı kadar hızlı operasyonlarda da kullanılabilir mi?',
          answer: 'Evet. Yoğun ve zaman baskısı olan teslimat akışlarında görünürlük ve saha koordinasyonu için kullanılabilir.'
        },
        {
          question: 'Kurye tarafı mobil uygulamadan mı yürür?',
          answer: 'Evet. Görev listesi, teslimat durumu ve not akışı mobil uygulama üzerinden yönetilebilir.'
        },
        {
          question: 'Müşteri iletişimi destekleniyor mu?',
          answer: 'Bildirim ve takip yapısı, operasyonunuza göre kurgulanabilir.'
        }
      ]}
    />
  );
}
