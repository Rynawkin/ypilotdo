import React, { useEffect, useMemo, useState } from 'react';
import { AlertCircle, Check, CreditCard, Loader2, ShieldCheck, Star, X } from 'lucide-react';
import { paymentService, type PlanLimits, type PlanType, type UpgradePlanRequest } from '../../services/payment.service';
import { getPlanCardData } from '@/lib/billingPlans';

interface UpgradePlanProps {
  onClose?: () => void;
  currentPlan?: PlanType;
  className?: string;
}

const paidPlans: PlanType[] = ['Starter', 'Growth', 'Professional', 'Business'];

export const UpgradePlan: React.FC<UpgradePlanProps> = ({
  onClose,
  currentPlan = 'Trial',
  className = ''
}) => {
  const [selectedPlan, setSelectedPlan] = useState<PlanType>('Growth');
  const [planLimits, setPlanLimits] = useState<Record<PlanType, PlanLimits>>({} as Record<PlanType, PlanLimits>);
  const [loading, setLoading] = useState(true);
  const [upgrading, setUpgrading] = useState(false);
  const [step, setStep] = useState<1 | 2>(1);
  const [error, setError] = useState('');
  const [customerInfo, setCustomerInfo] = useState({
    name: '',
    email: '',
    phone: ''
  });
  const [cardInfo, setCardInfo] = useState({
    cardHolderName: '',
    cardNumber: '',
    expiryMonth: '',
    expiryYear: '',
    cvv: '',
    cardHolderPhone: ''
  });
  const [autoRenewalAccepted, setAutoRenewalAccepted] = useState(false);

  useEffect(() => {
    void loadPlanLimits();
  }, []);

  const selectedPlanCard = useMemo(() => {
    const limits = planLimits[selectedPlan];
    return limits ? getPlanCardData(selectedPlan, limits) : null;
  }, [planLimits, selectedPlan]);

  const loadPlanLimits = async () => {
    setLoading(true);
    setError('');
    try {
      const entries = await Promise.all(
        paidPlans.map(async (plan) => [plan, await paymentService.getPlanLimits(plan)] as const)
      );
      setPlanLimits(Object.fromEntries(entries) as Record<PlanType, PlanLimits>);
    } catch (loadError: any) {
      setError(loadError?.response?.data?.errorMessage || 'Plan bilgileri yüklenemedi.');
    } finally {
      setLoading(false);
    }
  };

  const handleUpgrade = async () => {
    setError('');

    if (step === 1) {
      if (!customerInfo.name || !customerInfo.email || !customerInfo.phone) {
        setError('Lütfen tüm fatura bilgilerini doldurun.');
        return;
      }
      setStep(2);
      return;
    }

    if (
      !cardInfo.cardNumber ||
      !cardInfo.expiryMonth ||
      !cardInfo.expiryYear ||
      !cardInfo.cvv ||
      !autoRenewalAccepted
    ) {
      setError('Kart bilgilerini tamamlayın ve otomatik yenileme onayını verin.');
      return;
    }

    const cardHolderName = cardInfo.cardHolderName || customerInfo.name;
    const cardHolderPhone = cardInfo.cardHolderPhone || customerInfo.phone;

    setUpgrading(true);
    try {
      const request: UpgradePlanRequest = {
        planType: selectedPlan,
        customerEmail: customerInfo.email,
        customerName: customerInfo.name,
        customerPhone: customerInfo.phone,
        autoRenewalAccepted,
        referrerUrl: window.location.href,
        card: {
          cardHolderName,
          cardNumber: cardInfo.cardNumber,
          expiryMonth: cardInfo.expiryMonth,
          expiryYear: cardInfo.expiryYear,
          cvv: cardInfo.cvv,
          cardHolderPhone
        },
        successUrl: `${window.location.origin}/payment/success`,
        failUrl: `${window.location.origin}/payment/failed`
      };

      const result = await paymentService.initiatePlanUpgrade(request);

      if (result.isSuccess && result.paymentUrl) {
        window.location.href = result.paymentUrl;
        return;
      }

      setError(result.errorMessage || 'Ödeme başlatılamadı.');
    } catch (upgradeError: any) {
      setError(
        upgradeError.userFriendlyMessage ||
          upgradeError.response?.data?.errorMessage ||
          upgradeError.response?.data?.message ||
          'Bir hata oluştu. Lütfen tekrar deneyin.'
      );
    } finally {
      setUpgrading(false);
    }
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center p-8">
        <Loader2 className="h-8 w-8 animate-spin text-blue-600" />
      </div>
    );
  }

  return (
    <div className={`rounded-lg bg-white shadow-lg ${className}`}>
      <div className="flex items-center justify-between border-b p-6">
        <div>
          <h2 className="text-xl font-semibold text-gray-900">Planınızı yükseltin</h2>
          <p className="mt-1 text-sm text-gray-600">
            İhtiyacınıza uygun paketi seçin. Ücretli planlarda kart kaydı ve otomatik yenileme zorunludur.
          </p>
        </div>
        {onClose && (
          <button onClick={onClose} className="text-gray-400 hover:text-gray-600">
            <X className="h-6 w-6" />
          </button>
        )}
      </div>

      <div className="p-6">
        {error && (
          <div className="mb-6 rounded-xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-800">
            {error}
          </div>
        )}

        <div className="mb-8 grid grid-cols-1 gap-4 md:grid-cols-2 xl:grid-cols-4">
          {paidPlans.map((plan) => {
            const limits = planLimits[plan];
            const planCard = limits ? getPlanCardData(plan, limits) : null;
            const isSelected = selectedPlan === plan;
            const isCurrent = currentPlan === plan;

            if (!planCard) {
              return null;
            }

            return (
              <button
                key={plan}
                type="button"
                className={`relative rounded-2xl border p-5 text-left transition ${
                  isSelected ? 'border-blue-500 bg-blue-50 shadow-sm' : 'border-gray-200 bg-white hover:border-gray-300'
                } ${isCurrent ? 'cursor-not-allowed opacity-50' : ''}`}
                onClick={() => !isCurrent && setSelectedPlan(plan)}
              >
                {isCurrent && (
                  <span className="absolute right-4 top-4 rounded-full bg-gray-100 px-2 py-1 text-xs font-medium text-gray-700">
                    Mevcut plan
                  </span>
                )}
                <div className="flex items-center gap-2">
                  <h3 className="text-lg font-semibold text-gray-900">{planCard.name}</h3>
                  {planCard.popular && (
                    <span className="rounded-full bg-blue-600 px-2 py-1 text-xs font-medium text-white">Önerilen</span>
                  )}
                </div>
                <p className="mt-2 text-sm text-gray-600">{planCard.description}</p>
                <div className="mt-4">
                  <div className="text-2xl font-bold text-gray-900">{planCard.price}</div>
                  <div className="text-sm text-gray-500">{planCard.period}</div>
                </div>
                <ul className="mt-4 space-y-2 text-sm text-gray-700">
                  {planCard.features.slice(0, 6).map((feature) => (
                    <li key={feature} className="flex items-start gap-2">
                      <Check className="mt-0.5 h-4 w-4 shrink-0 text-green-500" />
                      <span>{feature}</span>
                    </li>
                  ))}
                </ul>
              </button>
            );
          })}
        </div>

        <div className="mb-6 rounded-2xl border border-blue-200 bg-blue-50 p-4">
          <div className="flex items-start gap-3">
            <ShieldCheck className="mt-0.5 h-5 w-5 shrink-0 text-blue-700" />
            <div className="text-sm text-blue-950">
              <div className="font-medium">Seçilen plan: {selectedPlanCard?.name}</div>
              <p className="mt-1 leading-6 text-blue-900/80">
                İlk ödeme şimdi alınır. Kartınız güvenli ödeme sağlayıcısında saklanır ve aboneliğiniz her ay
                otomatik yenilenir. Tahsilat başarısız olursa 3 gün sonra erişim kapanır.
              </p>
            </div>
          </div>
        </div>

        <div className="mb-6 rounded-2xl border border-slate-200 bg-slate-50 p-4">
          <div className="mb-4 flex items-center gap-2">
            <Star className="h-4 w-4 text-slate-600" />
            <h3 className="text-sm font-semibold text-slate-900">Fatura bilgileri</h3>
          </div>
          <div className="grid grid-cols-1 gap-4 md:grid-cols-3">
            <input
              type="text"
              value={customerInfo.name}
              onChange={(event) => setCustomerInfo((prev) => ({ ...prev, name: event.target.value }))}
              className="w-full rounded-xl border border-gray-300 px-3 py-2.5 text-sm outline-none focus:border-blue-500"
              placeholder="Ad Soyad"
            />
            <input
              type="email"
              value={customerInfo.email}
              onChange={(event) => setCustomerInfo((prev) => ({ ...prev, email: event.target.value }))}
              className="w-full rounded-xl border border-gray-300 px-3 py-2.5 text-sm outline-none focus:border-blue-500"
              placeholder="ornek@firma.com"
            />
            <input
              type="tel"
              value={customerInfo.phone}
              onChange={(event) => setCustomerInfo((prev) => ({ ...prev, phone: event.target.value }))}
              className="w-full rounded-xl border border-gray-300 px-3 py-2.5 text-sm outline-none focus:border-blue-500"
              placeholder="05xx xxx xx xx"
            />
          </div>
        </div>

        {step === 2 && (
          <div className="mb-6 rounded-2xl border border-slate-200 bg-white p-5">
            <div className="mb-4 flex items-center gap-2">
              <CreditCard className="h-4 w-4 text-slate-600" />
              <h3 className="text-sm font-semibold text-slate-900">Kart bilgileri</h3>
            </div>
            <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
              <input
                type="text"
                value={cardInfo.cardHolderName}
                onChange={(event) => setCardInfo((prev) => ({ ...prev, cardHolderName: event.target.value }))}
                className="w-full rounded-xl border border-gray-300 px-3 py-2.5 text-sm outline-none focus:border-blue-500"
                placeholder="Kart üzerindeki isim"
              />
              <input
                type="text"
                value={cardInfo.cardNumber}
                onChange={(event) =>
                  setCardInfo((prev) => ({ ...prev, cardNumber: event.target.value.replace(/[^0-9 ]/g, '') }))
                }
                className="w-full rounded-xl border border-gray-300 px-3 py-2.5 text-sm outline-none focus:border-blue-500"
                placeholder="4242 4242 4242 4242"
                inputMode="numeric"
                autoComplete="cc-number"
              />
              <div className="grid grid-cols-3 gap-4 md:col-span-2">
                <input
                  type="text"
                  value={cardInfo.expiryMonth}
                  onChange={(event) =>
                    setCardInfo((prev) => ({ ...prev, expiryMonth: event.target.value.replace(/[^0-9]/g, '') }))
                  }
                  className="w-full rounded-xl border border-gray-300 px-3 py-2.5 text-sm outline-none focus:border-blue-500"
                  placeholder="MM"
                  inputMode="numeric"
                  maxLength={2}
                  autoComplete="cc-exp-month"
                />
                <input
                  type="text"
                  value={cardInfo.expiryYear}
                  onChange={(event) =>
                    setCardInfo((prev) => ({ ...prev, expiryYear: event.target.value.replace(/[^0-9]/g, '') }))
                  }
                  className="w-full rounded-xl border border-gray-300 px-3 py-2.5 text-sm outline-none focus:border-blue-500"
                  placeholder="YYYY"
                  inputMode="numeric"
                  maxLength={4}
                  autoComplete="cc-exp-year"
                />
                <input
                  type="password"
                  value={cardInfo.cvv}
                  onChange={(event) =>
                    setCardInfo((prev) => ({ ...prev, cvv: event.target.value.replace(/[^0-9]/g, '') }))
                  }
                  className="w-full rounded-xl border border-gray-300 px-3 py-2.5 text-sm outline-none focus:border-blue-500"
                  placeholder="CVV"
                  inputMode="numeric"
                  maxLength={4}
                  autoComplete="cc-csc"
                />
              </div>
              <input
                type="tel"
                value={cardInfo.cardHolderPhone}
                onChange={(event) => setCardInfo((prev) => ({ ...prev, cardHolderPhone: event.target.value }))}
                className="w-full rounded-xl border border-gray-300 px-3 py-2.5 text-sm outline-none focus:border-blue-500 md:col-span-2"
                placeholder="Kart sahibinin telefonu"
              />
            </div>

            <label className="mt-5 flex items-start gap-3 rounded-xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm text-slate-700">
              <input
                type="checkbox"
                checked={autoRenewalAccepted}
                onChange={(event) => setAutoRenewalAccepted(event.target.checked)}
                className="mt-1 h-4 w-4 rounded border-slate-300 text-blue-600"
              />
              <span>
                Kartımın güvenli ödeme sağlayıcısında saklanmasını, aylık abonelik ücretinin otomatik tahsil
                edilmesini ve başarısız tahsilatta 3 gün sonra erişimin kapanacağını kabul ediyorum.
              </span>
            </label>
          </div>
        )}

        {step === 1 && (
          <div className="mb-6 flex items-start gap-3 rounded-xl border border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-900">
            <AlertCircle className="mt-0.5 h-4 w-4 shrink-0" />
            <span>
              Bir sonraki adımda kart bilgileri alınır. Kart kaydı olmadan ücretli plan başlatılamaz.
            </span>
          </div>
        )}

        <div className="flex items-center justify-end gap-3">
          {onClose && (
            <button
              onClick={onClose}
              className="rounded-md border border-gray-300 bg-white px-4 py-2 text-sm font-medium text-gray-700 hover:bg-gray-50"
              disabled={upgrading}
            >
              İptal
            </button>
          )}
          {step === 2 && (
            <button
              onClick={() => setStep(1)}
              className="rounded-md border border-gray-300 bg-white px-4 py-2 text-sm font-medium text-gray-700 hover:bg-gray-50"
              disabled={upgrading}
            >
              Geri
            </button>
          )}
          <button
            onClick={handleUpgrade}
            disabled={upgrading || currentPlan === selectedPlan}
            className="inline-flex items-center rounded-md border border-transparent bg-blue-600 px-4 py-2 text-sm font-medium text-white hover:bg-blue-700 disabled:cursor-not-allowed disabled:opacity-50"
          >
            {upgrading && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
            {upgrading ? 'Yönlendiriliyor...' : step === 1 ? 'Kart Bilgilerine Geç' : 'Ödemeye Geç'}
          </button>
        </div>
      </div>
    </div>
  );
};
