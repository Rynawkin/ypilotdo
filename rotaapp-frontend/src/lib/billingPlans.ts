import type { PlanLimits, PlanType } from '@/services/payment.service';

type PlanMarketingMeta = {
  description: string;
  popular?: boolean;
  trialLabel?: string;
};

const planMeta: Record<PlanType, PlanMarketingMeta> = {
  Trial: {
    description: 'Sistemi 14 gün boyunca canlı veriyle deneyin.',
    trialLabel: '14 gün'
  },
  Starter: {
    description: 'Küçük ekipler için temel rota ve saha operasyon paketi.'
  },
  Growth: {
    description: 'Büyüyen dağıtım ekipleri için en dengeli paket.',
    popular: true
  },
  Professional: {
    description: 'Yoğun operasyon ve daha derin görünürlük isteyen ekipler için.'
  },
  Business: {
    description: 'Büyük ekipler ve özel süreçler için en geniş kapsamlı paket.'
  }
};

function formatPrice(amount: number) {
  return new Intl.NumberFormat('tr-TR', {
    style: 'currency',
    currency: 'TRY',
    minimumFractionDigits: 0,
    maximumFractionDigits: 0
  }).format(amount);
}

function formatCount(value: number | null | undefined, singular: string, plural = singular) {
  if (value == null) {
    return `Sınırsız ${plural}`;
  }

  return `${value} ${value === 1 ? singular : plural}`;
}

export function getPlanDisplayName(planType: PlanType) {
  const names: Record<PlanType, string> = {
    Trial: 'Deneme',
    Starter: 'Başlangıç',
    Growth: 'Büyüme',
    Professional: 'Profesyonel',
    Business: 'İşletme'
  };

  return names[planType];
}

export function getPlanCardData(planType: PlanType, limits: PlanLimits) {
  const meta = planMeta[planType];
  const features: string[] = [
    formatCount(limits.maxDrivers, 'sürücü'),
    formatCount(limits.maxVehicles, 'araç'),
    formatCount(limits.maxCustomers, 'müşteri'),
    `${limits.maxUsers} kullanıcı`,
    `${limits.includedMonthlyStops.toLocaleString('tr-TR')} durak/ay`
  ];

  if (limits.hasTimeWindows) {
    features.push('Zaman pencereleri');
  }

  if (limits.hasCustomerWhatsAppNotifications && limits.includedWhatsAppMessages > 0) {
    features.push(`${limits.includedWhatsAppMessages.toLocaleString('tr-TR')} WhatsApp bildirimi`);
  }

  if (limits.hasCustomerSatisfactionReport) {
    features.push('Memnuniyet raporları');
  }

  if (limits.hasCustomReports) {
    features.push('Özel raporlar');
  }

  features.push(`${limits.proofArchiveDays} gün kanıt arşivi`);

  return {
    id: planType,
    name: getPlanDisplayName(planType),
    price: planType === 'Trial' ? formatPrice(0) : formatPrice(limits.monthlyPrice),
    period: planType === 'Trial' ? meta.trialLabel ?? '14 gün' : '/ay',
    description: meta.description,
    features,
    popular: meta.popular
  };
}
