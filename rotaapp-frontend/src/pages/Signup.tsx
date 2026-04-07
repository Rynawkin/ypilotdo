import React, { useMemo, useState } from 'react';
import { ArrowRight, Building2, CheckCircle, CreditCard, Info, Loader2, Lock, Mail, Phone, User } from 'lucide-react';
import { Link } from 'react-router-dom';
import { authService } from '@/services/auth.service';
import { paymentService, type PlanType } from '@/services/payment.service';

interface SignupForm {
  workspaceName: string;
  workspaceEmail: string;
  workspacePhone: string;
  adminFullName: string;
  adminEmail: string;
  adminPassword: string;
  adminPasswordConfirm: string;
  termsAccepted: boolean;
  privacyAccepted: boolean;
}

interface CardForm {
  cardHolderName: string;
  cardNumber: string;
  expiryMonth: string;
  expiryYear: string;
  cvv: string;
  cardHolderPhone: string;
}

const planCards: Array<{
  id: PlanType;
  name: string;
  price: string;
  period: string;
  description: string;
  features: string[];
  popular?: boolean;
}> = [
  {
    id: 'Trial',
    name: 'Deneme',
    price: '0₺',
    period: '14 gün',
    description: 'Önce sistemi görün, sonra planınızı yükseltin.',
    features: ['2 sürücü', '1 araç', '50 müşteri', '100 durak/ay', '25 WhatsApp mesajı']
  },
  {
    id: 'Starter',
    name: 'Başlangıç',
    price: '850₺',
    period: '/ay',
    description: 'Küçük ekipler için temel operasyon paketi.',
    features: ['3 sürücü', '3 araç', '100 müşteri', '2 kullanıcı', '500 durak/ay']
  },
  {
    id: 'Growth',
    name: 'Büyüme',
    price: '1.250₺',
    period: '/ay',
    description: 'Gerçek operasyon ekibi için en dengeli paket.',
    features: ['Sınırsız sürücü/araç', '1.000 müşteri', '10 kullanıcı', 'Zaman pencereleri', '100 WhatsApp'],
    popular: true
  },
  {
    id: 'Professional',
    name: 'Profesyonel',
    price: '2.400₺',
    period: '/ay',
    description: 'Yüksek hacimli ekipler için gelişmiş görünürlük.',
    features: ['2.000 durak/ay', 'Memnuniyet raporları', '10 kullanıcı', 'Gelişmiş operasyon görünümü']
  },
  {
    id: 'Business',
    name: 'İşletme',
    price: '5.900₺',
    period: '/ay',
    description: 'Büyük ekipler ve özel rapor ihtiyacı olanlar için.',
    features: ['50 kullanıcı', '5.000 durak/ay', '500 WhatsApp', 'Özel raporlar', 'Uzun arşiv süresi']
  }
];

const Signup: React.FC = () => {
  const [step, setStep] = useState(1);
  const [selectedPlan, setSelectedPlan] = useState<PlanType>('Trial');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [formData, setFormData] = useState<SignupForm>({
    workspaceName: '',
    workspaceEmail: '',
    workspacePhone: '',
    adminFullName: '',
    adminEmail: '',
    adminPassword: '',
    adminPasswordConfirm: '',
    termsAccepted: false,
    privacyAccepted: false
  });
  const [cardData, setCardData] = useState<CardForm>({
    cardHolderName: '',
    cardNumber: '',
    expiryMonth: '',
    expiryYear: '',
    cvv: '',
    cardHolderPhone: ''
  });

  const passwordErrors = useMemo(() => validatePassword(formData.adminPassword), [formData.adminPassword]);
  const requiresPayment = selectedPlan !== 'Trial';

  const selectedPlanInfo = planCards.find(plan => plan.id === selectedPlan)!;

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const { name, value, type, checked } = e.target;
    setFormData(prev => ({
      ...prev,
      [name]: type === 'checkbox' ? checked : value
    }));
  };

  const handleCardChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const { name, value } = e.target;
    setCardData(prev => ({
      ...prev,
      [name]: value
    }));
  };

  const validateStep = (stepNumber: number) => {
    if (stepNumber === 1) {
      return (
        formData.workspaceName.trim().length > 1 &&
        validateEmail(formData.workspaceEmail) &&
        validatePhone(formData.workspacePhone)
      );
    }

    if (stepNumber === 2) {
      return (
        formData.adminFullName.trim().length > 1 &&
        validateEmail(formData.adminEmail) &&
        passwordErrors.length === 0 &&
        formData.adminPassword === formData.adminPasswordConfirm &&
        formData.adminPasswordConfirm.length > 0
      );
    }

    const legalAccepted = formData.termsAccepted && formData.privacyAccepted;
    if (!requiresPayment) {
      return legalAccepted;
    }

    return (
      legalAccepted &&
      !!(cardData.cardHolderName || formData.adminFullName) &&
      cardData.cardNumber.replace(/\s/g, '').length >= 16 &&
      cardData.expiryMonth.length >= 2 &&
      cardData.expiryYear.length >= 2 &&
      cardData.cvv.length >= 3
    );
  };

  const handleSubmit = async () => {
    setLoading(true);
    setError('');

    try {
      if (!requiresPayment) {
        await authService.register(
          formData.adminEmail,
          formData.adminPassword,
          formData.adminFullName,
          formData.workspaceName,
          formData.workspaceEmail,
          formData.workspacePhone
        );

        window.location.href = '/dashboard';
        return;
      }

      const result = await paymentService.initiateSignupPayment({
        planType: selectedPlan,
        workspaceName: formData.workspaceName,
        workspaceEmail: formData.workspaceEmail,
        workspacePhone: formData.workspacePhone,
        adminFullName: formData.adminFullName,
        adminEmail: formData.adminEmail,
        adminPassword: formData.adminPassword,
        referrerUrl: window.location.href,
        successUrl: `${window.location.origin}/payment/success?flow=signup`,
        failUrl: `${window.location.origin}/payment/failed?flow=signup`,
        card: {
          cardHolderName: cardData.cardHolderName || formData.adminFullName,
          cardNumber: cardData.cardNumber,
          expiryMonth: cardData.expiryMonth,
          expiryYear: cardData.expiryYear,
          cvv: cardData.cvv,
          cardHolderPhone: cardData.cardHolderPhone || formData.workspacePhone
        }
      });

      if (!result.isSuccess || !result.paymentUrl || !result.transactionId || !result.signupToken) {
        throw new Error(result.errorMessage || 'Ödeme başlatılamadı');
      }

      localStorage.setItem('signupPaymentTransactionId', result.transactionId);
      localStorage.setItem('signupPaymentToken', result.signupToken);
      localStorage.setItem('signupPaymentPlan', selectedPlan);

      window.location.href = result.paymentUrl;
    } catch (err: any) {
      setError(err.userFriendlyMessage || err.response?.data?.message || err.message || 'Kayıt sırasında bir hata oluştu');
      setLoading(false);
    }
  };

  return (
    <div className="min-h-screen bg-slate-50">
      <div className="border-b bg-white">
        <div className="mx-auto flex max-w-7xl items-center justify-between px-4 py-4 sm:px-6 lg:px-8">
          <Link to="/" className="flex items-center">
            <img src="/yolpilot-logo.png" alt="YolPilot" className="h-10 w-auto" />
          </Link>
          <Link to="/login" className="text-sm text-slate-600 hover:text-slate-900">
            Zaten hesabınız var mı? <span className="font-medium text-blue-600">Giriş yapın</span>
          </Link>
        </div>
      </div>

      <div className="border-b bg-white">
        <div className="mx-auto max-w-4xl px-4 py-5 sm:px-6 lg:px-8">
          <div className="flex items-center justify-between">
            {[1, 2, 3].map(index => (
              <div key={index} className="flex items-center">
                <div className={`flex h-10 w-10 items-center justify-center rounded-full text-sm font-semibold ${step >= index ? 'bg-blue-600 text-white' : 'bg-slate-200 text-slate-600'}`}>
                  {index}
                </div>
                {index < 3 && <div className={`h-1 w-20 sm:w-28 ${step > index ? 'bg-blue-600' : 'bg-slate-200'}`} />}
              </div>
            ))}
          </div>
          <div className="mt-2 flex justify-between text-xs text-slate-500">
            <span>Firma</span>
            <span>Yönetici</span>
            <span>Plan ve Ödeme</span>
          </div>
        </div>
      </div>

      <div className="mx-auto max-w-4xl px-4 py-10 sm:px-6 lg:px-8">
        <div className="rounded-3xl border border-slate-200 bg-white p-6 shadow-sm sm:p-8">
          {error && (
            <div className="mb-6 rounded-2xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
              {error}
            </div>
          )}

          {step === 1 && (
            <section className="space-y-6">
              <div>
                <h1 className="text-2xl font-semibold text-slate-950">Firma Bilgileri</h1>
                <p className="mt-2 text-sm text-slate-600">Önce operasyonu yönetecek şirket hesabını oluşturalım.</p>
              </div>

              <div className="grid gap-4 sm:grid-cols-2">
                <Field label="Firma Adı *" icon={<Building2 className="h-5 w-5" />}>
                  <input name="workspaceName" value={formData.workspaceName} onChange={handleInputChange} className={inputClassName} placeholder="Örn: ABC Lojistik" />
                </Field>
                <Field label="Firma E-posta *" icon={<Mail className="h-5 w-5" />}>
                  <input name="workspaceEmail" type="email" value={formData.workspaceEmail} onChange={handleInputChange} className={inputClassName} placeholder="info@firma.com" />
                </Field>
                <Field label="Firma Telefon *" icon={<Phone className="h-5 w-5" />}>
                  <input name="workspacePhone" value={formData.workspacePhone} onChange={handleInputChange} className={inputClassName} placeholder="0532 123 45 67" />
                </Field>
              </div>
            </section>
          )}

          {step === 2 && (
            <section className="space-y-6">
              <div>
                <h1 className="text-2xl font-semibold text-slate-950">Yönetici Hesabı</h1>
                <p className="mt-2 text-sm text-slate-600">İlk giriş yapacak yönetici kullanıcının bilgilerini girin.</p>
              </div>

              <div className="grid gap-4 sm:grid-cols-2">
                <Field label="Ad Soyad *" icon={<User className="h-5 w-5" />}>
                  <input name="adminFullName" value={formData.adminFullName} onChange={handleInputChange} className={inputClassName} placeholder="Adınız Soyadınız" />
                </Field>
                <Field label="Yönetici E-posta *" icon={<Mail className="h-5 w-5" />}>
                  <input name="adminEmail" type="email" value={formData.adminEmail} onChange={handleInputChange} className={inputClassName} placeholder="yonetici@firma.com" />
                </Field>
                <Field label="Şifre *" icon={<Lock className="h-5 w-5" />}>
                  <input name="adminPassword" type="password" value={formData.adminPassword} onChange={handleInputChange} className={inputClassName} placeholder="••••••••" />
                </Field>
                <Field label="Şifre Tekrar *" icon={<Lock className="h-5 w-5" />}>
                  <input name="adminPasswordConfirm" type="password" value={formData.adminPasswordConfirm} onChange={handleInputChange} className={inputClassName} placeholder="••••••••" />
                </Field>
              </div>

              <div className="rounded-2xl border border-slate-200 bg-slate-50 p-4">
                <div className="mb-2 flex items-center gap-2 text-sm font-medium text-slate-700">
                  <Info className="h-4 w-4" />
                  Şifre gereksinimleri
                </div>
                <div className="grid gap-2 text-sm text-slate-600 sm:grid-cols-2">
                  <PasswordRule ok={formData.adminPassword.length >= 6} text="En az 6 karakter" />
                  <PasswordRule ok={/[A-Z]/.test(formData.adminPassword)} text="En az 1 büyük harf" />
                  <PasswordRule ok={/[a-z]/.test(formData.adminPassword)} text="En az 1 küçük harf" />
                  <PasswordRule ok={/[0-9]/.test(formData.adminPassword)} text="En az 1 rakam" />
                  <PasswordRule ok={/[!@#$%^&*()_+\-=[\]{};':"\\|,.<>/?]/.test(formData.adminPassword)} text="En az 1 özel karakter" />
                  <PasswordRule ok={formData.adminPassword === formData.adminPasswordConfirm && formData.adminPasswordConfirm.length > 0} text="Şifreler eşleşmeli" />
                </div>
              </div>
            </section>
          )}

          {step === 3 && (
            <section className="space-y-6">
              <div>
                <h1 className="text-2xl font-semibold text-slate-950">Plan Seçimi</h1>
                <p className="mt-2 text-sm text-slate-600">
                  Deneme ile başlayabilir veya ödemeyi şimdi yaparak doğrudan seçtiğiniz planla açabilirsiniz.
                </p>
              </div>

              <div className="grid gap-4 lg:grid-cols-2">
                {planCards.map(plan => {
                  const isSelected = selectedPlan === plan.id;
                  return (
                    <button
                      key={plan.id}
                      type="button"
                      onClick={() => setSelectedPlan(plan.id)}
                      className={`rounded-2xl border p-5 text-left transition ${isSelected ? 'border-blue-500 bg-blue-50 shadow-sm' : 'border-slate-200 bg-white hover:border-slate-300'}`}
                    >
                      <div className="flex items-start justify-between gap-4">
                        <div>
                          <div className="flex items-center gap-2">
                            <h3 className="text-lg font-semibold text-slate-950">{plan.name}</h3>
                            {plan.popular && <span className="rounded-full bg-blue-600 px-2 py-1 text-xs font-medium text-white">Önerilen</span>}
                          </div>
                          <p className="mt-1 text-sm text-slate-600">{plan.description}</p>
                        </div>
                        <div className="text-right">
                          <div className="text-2xl font-semibold text-slate-950">{plan.price}</div>
                          <div className="text-sm text-slate-500">{plan.period}</div>
                        </div>
                      </div>
                      <ul className="mt-4 space-y-2 text-sm text-slate-700">
                        {plan.features.map(feature => (
                          <li key={feature} className="flex items-center gap-2">
                            <CheckCircle className="h-4 w-4 text-green-500" />
                            <span>{feature}</span>
                          </li>
                        ))}
                      </ul>
                    </button>
                  );
                })}
              </div>

              <div className="rounded-2xl border border-slate-200 bg-slate-50 p-4">
                <div className="flex items-start justify-between gap-4">
                  <div>
                    <div className="text-sm font-medium text-slate-950">Seçilen plan: {selectedPlanInfo.name}</div>
                    <div className="mt-1 text-sm text-slate-600">
                      {requiresPayment
                        ? `Bu plan için ödeme şimdi alınır ve hesabınız ${selectedPlanInfo.name} planıyla açılır.`
                        : 'Hesabınız 14 günlük deneme ile açılır. Daha sonra panel içinden yükseltebilirsiniz.'}
                    </div>
                  </div>
                  {requiresPayment && <CreditCard className="h-5 w-5 text-slate-500" />}
                </div>
              </div>

              {requiresPayment && (
                <div className="grid gap-4 rounded-2xl border border-slate-200 bg-white p-5 sm:grid-cols-2">
                  <Field label="Kart Üzerindeki İsim *" icon={<User className="h-5 w-5" />}>
                    <input name="cardHolderName" value={cardData.cardHolderName} onChange={handleCardChange} className={inputClassName} placeholder={formData.adminFullName || 'Kart üzerindeki isim'} />
                  </Field>
                  <Field label="Kart Numarası *" icon={<CreditCard className="h-5 w-5" />}>
                    <input name="cardNumber" value={cardData.cardNumber} onChange={handleCardChange} className={inputClassName} placeholder="4242 4242 4242 4242" inputMode="numeric" />
                  </Field>
                  <Field label="Son Kullanma Ay *" icon={<CreditCard className="h-5 w-5" />}>
                    <input name="expiryMonth" value={cardData.expiryMonth} onChange={handleCardChange} className={inputClassName} placeholder="MM" inputMode="numeric" />
                  </Field>
                  <Field label="Son Kullanma Yıl *" icon={<CreditCard className="h-5 w-5" />}>
                    <input name="expiryYear" value={cardData.expiryYear} onChange={handleCardChange} className={inputClassName} placeholder="YYYY" inputMode="numeric" />
                  </Field>
                  <Field label="CVV *" icon={<Lock className="h-5 w-5" />}>
                    <input name="cvv" type="password" value={cardData.cvv} onChange={handleCardChange} className={inputClassName} placeholder="123" inputMode="numeric" />
                  </Field>
                  <Field label="Kart Sahibi Telefon" icon={<Phone className="h-5 w-5" />}>
                    <input name="cardHolderPhone" value={cardData.cardHolderPhone} onChange={handleCardChange} className={inputClassName} placeholder={formData.workspacePhone || '5xx xxx xx xx'} />
                  </Field>
                </div>
              )}

              <div className="space-y-3 rounded-2xl border border-slate-200 bg-slate-50 p-4 text-sm text-slate-600">
                <label className="flex items-start gap-3">
                  <input type="checkbox" name="termsAccepted" checked={formData.termsAccepted} onChange={handleInputChange} className="mt-1 h-4 w-4 rounded border-slate-300 text-blue-600" />
                  <span>
                    <Link to="/terms" target="_blank" className="font-medium text-blue-600 hover:underline">Kullanım koşullarını</Link> okudum ve kabul ediyorum.
                  </span>
                </label>
                <label className="flex items-start gap-3">
                  <input type="checkbox" name="privacyAccepted" checked={formData.privacyAccepted} onChange={handleInputChange} className="mt-1 h-4 w-4 rounded border-slate-300 text-blue-600" />
                  <span>
                    <Link to="/privacy" target="_blank" className="font-medium text-blue-600 hover:underline">Gizlilik politikasını</Link> okudum ve kabul ediyorum.
                  </span>
                </label>
              </div>
            </section>
          )}

          <div className="mt-8 flex items-center justify-between">
            {step > 1 ? (
              <button type="button" onClick={() => setStep(step - 1)} className="rounded-xl border border-slate-300 px-5 py-2.5 text-sm font-medium text-slate-700 hover:bg-slate-50">
                Geri
              </button>
            ) : <div />}

            {step < 3 ? (
              <button
                type="button"
                onClick={() => setStep(step + 1)}
                disabled={!validateStep(step)}
                className="inline-flex items-center gap-2 rounded-xl bg-blue-600 px-5 py-2.5 text-sm font-medium text-white transition hover:bg-blue-700 disabled:cursor-not-allowed disabled:bg-slate-300"
              >
                İleri
                <ArrowRight className="h-4 w-4" />
              </button>
            ) : (
              <button
                type="button"
                onClick={handleSubmit}
                disabled={loading || !validateStep(3)}
                className="inline-flex items-center gap-2 rounded-xl bg-green-600 px-5 py-2.5 text-sm font-medium text-white transition hover:bg-green-700 disabled:cursor-not-allowed disabled:bg-slate-300"
              >
                {loading ? <Loader2 className="h-4 w-4 animate-spin" /> : <CheckCircle className="h-4 w-4" />}
                {loading ? 'İşleniyor...' : requiresPayment ? 'Ödemeye Geç ve Hesabı Aç' : 'Hesabı Oluştur'}
              </button>
            )}
          </div>
        </div>
      </div>
    </div>
  );
};

const Field: React.FC<{ label: string; icon: React.ReactNode; children: React.ReactNode }> = ({ label, icon, children }) => (
  <label className="space-y-1.5">
    <span className="block text-sm font-medium text-slate-700">{label}</span>
    <div className="relative">
      <div className="pointer-events-none absolute inset-y-0 left-0 flex items-center pl-3 text-slate-400">{icon}</div>
      {children}
    </div>
  </label>
);

const PasswordRule: React.FC<{ ok: boolean; text: string }> = ({ ok, text }) => (
  <div className={`flex items-center gap-2 ${ok ? 'text-green-600' : 'text-slate-500'}`}>
    <CheckCircle className={`h-4 w-4 ${ok ? 'opacity-100' : 'opacity-30'}`} />
    <span>{text}</span>
  </div>
);

const inputClassName =
  'w-full rounded-xl border border-slate-300 bg-white py-2.5 pl-10 pr-3 text-sm text-slate-900 outline-none transition focus:border-blue-500 focus:ring-2 focus:ring-blue-100';

function validatePassword(password: string): string[] {
  const errors: string[] = [];
  if (password.length < 6) errors.push('En az 6 karakter');
  if (!/[A-Z]/.test(password)) errors.push('En az 1 büyük harf');
  if (!/[a-z]/.test(password)) errors.push('En az 1 küçük harf');
  if (!/[0-9]/.test(password)) errors.push('En az 1 rakam');
  if (!/[!@#$%^&*()_+\-=[\]{};':"\\|,.<>/?]/.test(password)) errors.push('En az 1 özel karakter');
  return errors;
}

function validateEmail(email: string) {
  return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email);
}

function validatePhone(phone: string) {
  const numbersOnly = phone.replace(/[^\d]/g, '');
  if (numbersOnly.length === 10 && numbersOnly.startsWith('5')) return true;
  if (numbersOnly.length === 11 && numbersOnly.startsWith('05')) return true;
  return numbersOnly.length === 12 && numbersOnly.startsWith('905');
}

export default Signup;
