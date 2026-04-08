import React, { useEffect, useState } from 'react';
import { AlertCircle, CheckCircle2, CreditCard, Loader2, ShieldCheck } from 'lucide-react';
import { paymentService, type BillingStatusResponse, type PaymentCard } from '@/services/payment.service';

const emptyCard: PaymentCard = {
  cardHolderName: '',
  cardNumber: '',
  expiryMonth: '',
  expiryYear: '',
  cvv: '',
  cardHolderPhone: ''
};

export const PaymentMethodSettings: React.FC = () => {
  const [billingStatus, setBillingStatus] = useState<BillingStatusResponse | null>(null);
  const [card, setCard] = useState<PaymentCard>(emptyCard);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const loadBillingStatus = async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await paymentService.getBillingStatus();
      setBillingStatus(data);
      if (data.paymentMethod?.cardHolderName) {
        setCard(prev => ({ ...prev, cardHolderName: data.paymentMethod?.cardHolderName || '' }));
      }
    } catch (err: any) {
      setError(err?.response?.data?.errorMessage || err?.response?.data?.message || 'Ödeme durumu yüklenemedi.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    void loadBillingStatus();
  }, []);

  const updateField = (field: keyof PaymentCard, value: string) => {
    setCard(prev => ({ ...prev, [field]: value }));
  };

  const handleSave = async (event: React.FormEvent) => {
    event.preventDefault();
    setSaving(true);
    setMessage(null);
    setError(null);

    try {
      const result = await paymentService.saveDefaultPaymentMethod({
        alias: card.cardHolderName,
        card
      });

      if (!result.isSuccess) {
        setError(result.errorMessage || 'Kart kaydedilemedi.');
        return;
      }

      setMessage('Kart otomatik yenilemeler için kaydedildi.');
      setCard(prev => ({ ...emptyCard, cardHolderName: prev.cardHolderName }));
      await loadBillingStatus();
    } catch (err: any) {
      setError(err?.response?.data?.errorMessage || err?.response?.data?.message || 'Kart kaydedilemedi.');
    } finally {
      setSaving(false);
    }
  };

  if (loading) {
    return (
      <div className="rounded-2xl border border-slate-200 bg-white p-6">
        <div className="flex items-center gap-2 text-slate-500">
          <Loader2 className="h-4 w-4 animate-spin" />
          <span>Ödeme yöntemi yükleniyor...</span>
        </div>
      </div>
    );
  }

  return (
    <div className="grid gap-6 lg:grid-cols-[1.1fr_0.9fr]">
      <div className="rounded-2xl border border-slate-200 bg-white p-6">
        <div className="flex items-start justify-between gap-4">
          <div>
            <h3 className="text-lg font-semibold text-slate-900">Otomatik ödeme yöntemi</h3>
            <p className="mt-1 text-sm text-slate-500">
              Aylık yenilemeler kayıtlı karttan otomatik tahsil edilir. Tahsilat başarısız olursa 3 gün sonra erişim kapanır.
            </p>
          </div>
          <div className="rounded-full bg-slate-100 p-2 text-slate-600">
            <CreditCard className="h-5 w-5" />
          </div>
        </div>

        <div className="mt-6 rounded-xl border border-slate-200 bg-slate-50 p-4">
          {billingStatus?.paymentMethod ? (
            <div className="space-y-2">
              <div className="flex items-center gap-2 text-emerald-700">
                <CheckCircle2 className="h-4 w-4" />
                <span className="text-sm font-medium">Varsayılan kart kayıtlı</span>
              </div>
              <p className="text-sm text-slate-700">
                {billingStatus.paymentMethod.cardHolderName || 'Kart Sahibi'} •••• {billingStatus.paymentMethod.lastFourDigits}
              </p>
              <p className="text-xs text-slate-500">
                Son kullanma: {billingStatus.paymentMethod.expiryMonth}/{billingStatus.paymentMethod.expiryYear}
              </p>
            </div>
          ) : (
            <div className="flex items-start gap-3 text-amber-700">
              <AlertCircle className="mt-0.5 h-4 w-4" />
              <div>
                <p className="text-sm font-medium">Kayıtlı kart bulunmuyor</p>
                <p className="mt-1 text-xs text-amber-800">
                  Bu workspace için otomatik aylık tahsilat başlayabilsin diye bir kart kaydetmen gerekiyor.
                </p>
              </div>
            </div>
          )}
        </div>

        {billingStatus?.graceStatus && billingStatus.graceStatus.status !== 'Paid' && (
          <div className="mt-4 rounded-xl border border-amber-200 bg-amber-50 p-4 text-sm text-amber-900">
            <div className="font-medium">Bekleyen fatura var</div>
            <div className="mt-1">
              {billingStatus.graceStatus.remainingGraceHours > 0
                ? `Kalan süre: yaklaşık ${billingStatus.graceStatus.remainingGraceHours} saat`
                : 'Grace süresi dolmuş. Ödeme tamamlanana kadar erişim kapalı kalır.'}
            </div>
          </div>
        )}

        {message && (
          <div className="mt-4 rounded-xl border border-emerald-200 bg-emerald-50 p-3 text-sm text-emerald-800">
            {message}
          </div>
        )}

        {error && (
          <div className="mt-4 rounded-xl border border-rose-200 bg-rose-50 p-3 text-sm text-rose-800">
            {error}
          </div>
        )}
      </div>

      <form onSubmit={handleSave} className="rounded-2xl border border-slate-200 bg-white p-6">
        <div className="flex items-center gap-2">
          <ShieldCheck className="h-5 w-5 text-slate-600" />
          <h3 className="text-lg font-semibold text-slate-900">Kart ekle / güncelle</h3>
        </div>

        <div className="mt-6 space-y-4">
          <label className="block">
            <span className="mb-1 block text-sm font-medium text-slate-700">Kart sahibi</span>
            <input
              value={card.cardHolderName}
              onChange={(e) => updateField('cardHolderName', e.target.value)}
              className="w-full rounded-xl border border-slate-300 px-3 py-2.5 text-sm outline-none transition focus:border-slate-500"
              placeholder="Ad Soyad"
              required
            />
          </label>

          <label className="block">
            <span className="mb-1 block text-sm font-medium text-slate-700">Kart numarası</span>
            <input
              value={card.cardNumber}
              onChange={(e) => updateField('cardNumber', e.target.value)}
              className="w-full rounded-xl border border-slate-300 px-3 py-2.5 text-sm outline-none transition focus:border-slate-500"
              placeholder="0000 0000 0000 0000"
              inputMode="numeric"
              required
            />
          </label>

          <div className="grid gap-4 sm:grid-cols-3">
            <label className="block">
              <span className="mb-1 block text-sm font-medium text-slate-700">Ay</span>
              <input
                value={card.expiryMonth}
                onChange={(e) => updateField('expiryMonth', e.target.value)}
                className="w-full rounded-xl border border-slate-300 px-3 py-2.5 text-sm outline-none transition focus:border-slate-500"
                placeholder="MM"
                inputMode="numeric"
                required
              />
            </label>

            <label className="block">
              <span className="mb-1 block text-sm font-medium text-slate-700">Yıl</span>
              <input
                value={card.expiryYear}
                onChange={(e) => updateField('expiryYear', e.target.value)}
                className="w-full rounded-xl border border-slate-300 px-3 py-2.5 text-sm outline-none transition focus:border-slate-500"
                placeholder="YY"
                inputMode="numeric"
                required
              />
            </label>

            <label className="block">
              <span className="mb-1 block text-sm font-medium text-slate-700">CVV</span>
              <input
                value={card.cvv}
                onChange={(e) => updateField('cvv', e.target.value)}
                className="w-full rounded-xl border border-slate-300 px-3 py-2.5 text-sm outline-none transition focus:border-slate-500"
                placeholder="123"
                inputMode="numeric"
                required
              />
            </label>
          </div>

          <label className="block">
            <span className="mb-1 block text-sm font-medium text-slate-700">Kart sahibinin telefonu</span>
            <input
              value={card.cardHolderPhone || ''}
              onChange={(e) => updateField('cardHolderPhone', e.target.value)}
              className="w-full rounded-xl border border-slate-300 px-3 py-2.5 text-sm outline-none transition focus:border-slate-500"
              placeholder="05xx xxx xx xx"
              inputMode="tel"
              required
            />
          </label>
        </div>

        <button
          type="submit"
          disabled={saving}
          className="mt-6 inline-flex w-full items-center justify-center rounded-xl bg-slate-900 px-4 py-2.5 text-sm font-medium text-white transition hover:bg-slate-800 disabled:cursor-not-allowed disabled:opacity-60"
        >
          {saving ? <Loader2 className="h-4 w-4 animate-spin" /> : 'Kartı Kaydet'}
        </button>
      </form>
    </div>
  );
};
