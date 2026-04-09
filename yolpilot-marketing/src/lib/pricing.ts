export type MarketingPlan = {
  id: 'starter' | 'growth' | 'professional' | 'business';
  name: string;
  monthlyPrice: number;
  summary: string;
  audience: string;
  popular?: boolean;
  features: string[];
  notes: string[];
};

export const pricingPlans: MarketingPlan[] = [
  {
    id: 'starter',
    name: 'Başlangıç',
    monthlyPrice: 990,
    summary: 'Küçük ekipler için temel rota ve saha operasyon paketi.',
    audience: '3 sürücüye kadar küçük dağıtım ekipleri',
    features: [
      '3 sürücü, 3 araç',
      '150 müşteri',
      '2 kullanıcı',
      '750 durak / ay',
      '30 gün kanıt arşivi'
    ],
    notes: ['Ek durak: 3 TL', 'WhatsApp bildirimi dahil değildir']
  },
  {
    id: 'growth',
    name: 'Büyüme',
    monthlyPrice: 1790,
    summary: 'Büyüyen ekipler için en dengeli operasyon paketi.',
    audience: 'Birden fazla rota ve saha ekibi yöneten operasyonlar',
    popular: true,
    features: [
      'Sınırsız sürücü ve araç',
      '1.500 müşteri',
      '10 kullanıcı',
      '2.000 durak / ay',
      '150 WhatsApp bildirimi',
      'Zaman pencereleri'
    ],
    notes: ['Ek durak: 2,5 TL', 'Ek WhatsApp mesajı: 0,70 TL', '120 gün kanıt arşivi']
  },
  {
    id: 'professional',
    name: 'Profesyonel',
    monthlyPrice: 3490,
    summary: 'Yüksek hacimli ekipler için derin görünürlük ve raporlama.',
    audience: 'Birden fazla depo veya yoğun teslimat hacmi olan ekipler',
    features: [
      'Sınırsız sürücü ve araç',
      '5.000 müşteri',
      '20 kullanıcı',
      '5.000 durak / ay',
      '500 WhatsApp bildirimi',
      'Memnuniyet raporları'
    ],
    notes: ['Ek durak: 1,75 TL', 'Ek WhatsApp mesajı: 0,50 TL', '180 gün kanıt arşivi']
  },
  {
    id: 'business',
    name: 'İşletme',
    monthlyPrice: 7900,
    summary: 'Kurumsal ekipler için en geniş kapsam ve özel raporlama.',
    audience: 'Büyük filo, çoklu ekip ve özel süreç ihtiyacı olan yapılar',
    features: [
      'Sınırsız sürücü ve araç',
      'Sınırsız müşteri',
      '75 kullanıcı',
      '12.000 durak / ay',
      '1.500 WhatsApp bildirimi',
      'Özel raporlar'
    ],
    notes: ['Ek durak: 1,20 TL', 'Ek WhatsApp mesajı: 0,35 TL', '365 gün kanıt arşivi']
  }
];

export function formatTry(amount: number) {
  return new Intl.NumberFormat('tr-TR', {
    style: 'currency',
    currency: 'TRY',
    minimumFractionDigits: 0,
    maximumFractionDigits: 0
  }).format(amount);
}
