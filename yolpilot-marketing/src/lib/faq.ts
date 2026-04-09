export type FaqItem = {
  question: string;
  answer: string;
};

export const salesFaqItems: FaqItem[] = [
  {
    question: 'YolPilot kimler için uygun?',
    answer:
      'Kendi dağıtım ekibini, sürücülerini ve araçlarını yöneten firmalar için uygundur. E-ticaret dağıtımı, lojistik, perakende dağıtımı, soğuk zincir ve saha operasyonlarında kullanılır.'
  },
  {
    question: 'Kurulum ne kadar sürer?',
    answer:
      'Temel kurulum aynı gün içinde başlatılabilir. Müşteri, sürücü ve araç verisi hazırsa ilk rota optimizasyonu çoğu ekipte birkaç saat içinde yapılır.'
  },
  {
    question: 'Mobil uygulama da dahil mi?',
    answer:
      'Evet. Web panel planlama ve operasyon kontrolü için, mobil uygulama ise sürücü ve saha ekibi akışı için aynı ürün ailesinin parçası olarak çalışır.'
  },
  {
    question: 'Rota optimizasyonu nasıl hesaplanıyor?',
    answer:
      'Rota sıralaması gerçek yol ağı verisiyle çalışır. Depoya dönüş, durak akışı ve operasyon kuralları birlikte değerlendirilir.'
  },
  {
    question: 'Fiyatlar sabit mi, ek kullanım oluyor mu?',
    answer:
      'Aylık plan fiyatı sabittir. Paketinizde dahil olan durak ve WhatsApp kullanımı aşıldığında yalnızca aşan miktar kadar ek kullanım ücreti yansır.'
  },
  {
    question: 'Ödeme ve yenileme nasıl işliyor?',
    answer:
      'Ücretli planlarda ilk ödeme sırasında kart güvenli ödeme sağlayıcısında saklanır. Abonelik her ay otomatik yenilenir. Tahsilat başarısız olursa 3 günlük süre sonunda erişim kapanır.'
  },
  {
    question: 'Demo almadan başlayabilir miyim?',
    answer:
      'Evet. İsterseniz önce demo talep edebilirsiniz, isterseniz doğrudan kayıt olup deneme veya ücretli planla başlayabilirsiniz.'
  },
  {
    question: 'Entegrasyon desteği var mı?',
    answer:
      'ERP, e-ticaret, CRM veya iç sistemlerle entegrasyon ihtiyacınız varsa bunu kurulum aşamasında birlikte planlayabiliriz. Geniş paketlerde özel entegrasyon kapsamı ayrıca değerlendirilir.'
  }
];
