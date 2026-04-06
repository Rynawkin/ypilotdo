'use client';

import React, { useEffect, useState } from 'react';
import { ExternalLink } from 'lucide-react';
import { useSearchParams } from 'next/navigation';
import { useToast } from '@/components/ToastContainer';
import { getTrackingContext, trackMarketingEvent } from '@/lib/marketingTracking';

interface FormData {
  name: string;
  email: string;
  company: string;
  phone: string;
  vehicleCount: string;
  message: string;
}

const TrialPageContent: React.FC = () => {
  const searchParams = useSearchParams();
  const planParam = searchParams.get('plan');
  const { showSuccess, showError } = useToast();

  const [formData, setFormData] = useState<FormData>({
    name: '',
    email: '',
    company: '',
    phone: '',
    vehicleCount: '',
    message: ''
  });

  const [loading, setLoading] = useState(false);
  const [currentPlan, setCurrentPlan] = useState<string | null>(null);

  useEffect(() => {
    if (planParam) {
      setCurrentPlan(decodeURIComponent(planParam));
    }
  }, [planParam]);

  const handleChange = (
    event: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement>
  ) => {
    setFormData({
      ...formData,
      [event.target.name]: event.target.value
    });
  };

  const handleDemoRequest = async (event: React.FormEvent) => {
    event.preventDefault();
    setLoading(true);

    try {
      const response = await fetch('/api/proxy/api/marketinglead', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json'
        },
        body: JSON.stringify({
          ...formData,
          selectedPlan: currentPlan,
          source: 'demo_request',
          ...getTrackingContext()
        })
      });

      if (response.ok) {
        trackMarketingEvent({
          eventType: 'form_submit',
          eventName: 'start_trial_demo_request',
          pagePath:
            typeof window !== 'undefined'
              ? `${window.location.pathname}${window.location.search}`
              : '/start-trial'
        });

        showSuccess(
          'Demo talebiniz alındı',
          'Ekibimiz sizinle iletişime geçerek demo planlaması yapacak.'
        );
        setFormData({
          name: '',
          email: '',
          company: '',
          phone: '',
          vehicleCount: '',
          message: ''
        });
      } else {
        let errorMessage = 'Bir hata oluştu. Lütfen tekrar deneyin.';
        try {
          const errorData = await response.json();
          errorMessage = errorData.message || errorMessage;
        } catch {
          try {
            const errorText = await response.text();
            if (errorText && errorText.length < 200) {
              errorMessage = errorText;
            }
          } catch {
            // ignore
          }
        }
        showError('Form gönderim hatası', errorMessage);
      }
    } catch (error) {
      console.error('Form submission error:', error);
      showError('Bağlantı hatası', 'Lütfen bağlantınızı kontrol edip tekrar deneyin.');
    } finally {
      setLoading(false);
    }
  };

  const handleSignupRedirect = () => {
    const signupUrl = currentPlan
      ? `https://app.yolpilot.com/signup?plan=${encodeURIComponent(currentPlan)}`
      : 'https://app.yolpilot.com/signup';

    trackMarketingEvent({
      eventType: 'cta_click',
      eventName: 'start_trial_signup_redirect',
      pagePath:
        typeof window !== 'undefined'
          ? `${window.location.pathname}${window.location.search}`
          : '/start-trial',
      metadata: {
        selectedPlan: currentPlan ?? null
      }
    });

    window.open(signupUrl, '_blank');
  };

  return (
    <div className="min-h-screen bg-gray-50 pt-24">
      <section className="bg-gradient-to-br from-blue-700 via-blue-800 to-slate-900 py-20 text-center text-white">
        <div className="mx-auto max-w-4xl px-4 sm:px-6 lg:px-8">
          <h1 className="mb-6 text-4xl font-bold lg:text-5xl">
            YolPilot ile operasyona daha hızlı başlayın
          </h1>
          <p className="text-xl text-blue-100">
            İsterseniz demo planlayın, isterseniz uygulama hesabınızı oluşturup ekibinizle kurulum
            sürecine geçin.
          </p>
        </div>
      </section>

      <section className="py-16">
        <div className="mx-auto grid max-w-6xl gap-8 px-4 sm:px-6 lg:grid-cols-2 lg:px-8">
          <div className="rounded-3xl border-2 border-blue-500 bg-white p-8 shadow-lg">
            <div className="mb-6 text-center">
              <div className="mx-auto mb-4 flex h-16 w-16 items-center justify-center rounded-full bg-blue-100">
                <ExternalLink className="h-8 w-8 text-blue-600" />
              </div>
              <h2 className="mb-2 text-2xl font-bold text-gray-900">Hesap Oluşturun</h2>
              <p className="text-gray-600">
                Uygulamaya giriş yapın, ekibiniz için temel kurulumu oluşturun ve onboarding
                sürecini hızla başlatın.
              </p>
            </div>

            {currentPlan && (
              <div className="mb-6 rounded-lg bg-blue-50 p-4">
                <h3 className="mb-2 font-semibold text-blue-900">Seçilen plan: {currentPlan}</h3>
                <p className="text-sm text-blue-800">
                  Plan detaylarını demo veya kurulum görüşmesinde birlikte netleştirebiliriz.
                </p>
              </div>
            )}

            <button
              onClick={handleSignupRedirect}
              className="flex w-full items-center justify-center gap-2 rounded-lg bg-blue-600 px-8 py-4 text-lg font-semibold text-white shadow-lg transition-all duration-200 hover:bg-blue-700 hover:shadow-xl"
            >
              Kayıt Oluştur
              <ExternalLink className="h-5 w-5" />
            </button>
          </div>

          <div className="rounded-3xl border border-gray-200 bg-white p-8 shadow-sm">
            <div className="mb-6 text-center">
              <div className="mx-auto mb-4 flex h-16 w-16 items-center justify-center rounded-full bg-gray-100">
                <svg className="h-8 w-8 text-gray-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M8 12h.01M12 12h.01M16 12h.01M21 12c0 4.418-4.03 8-9 8a9.863 9.863 0 01-4.255-.949L3 20l1.395-3.72C3.512 15.042 3 13.574 3 12c0-4.418 4.03-8 9-8s9 3.582 9 8z" />
                </svg>
              </div>
              <h2 className="mb-2 text-2xl font-bold text-gray-900">Demo Talep Edin</h2>
              <p className="text-gray-600">
                Ekibimize mevcut yapınızı anlatın, size uygun kurguyu birlikte planlayalım.
              </p>
            </div>

            <form onSubmit={handleDemoRequest} className="space-y-4">
              <div className="grid gap-4 md:grid-cols-2">
                <div>
                  <label htmlFor="name" className="mb-1 block text-sm font-medium text-gray-700">
                    Ad Soyad *
                  </label>
                  <input
                    type="text"
                    id="name"
                    name="name"
                    required
                    value={formData.name}
                    onChange={handleChange}
                    className="w-full rounded-lg border border-gray-300 px-3 py-2 text-gray-900 transition-all duration-200 placeholder-gray-500 focus:border-transparent focus:ring-2 focus:ring-blue-500"
                    placeholder="Adınız"
                  />
                </div>

                <div>
                  <label htmlFor="email" className="mb-1 block text-sm font-medium text-gray-700">
                    E-posta *
                  </label>
                  <input
                    type="email"
                    id="email"
                    name="email"
                    required
                    value={formData.email}
                    onChange={handleChange}
                    className="w-full rounded-lg border border-gray-300 px-3 py-2 text-gray-900 transition-all duration-200 placeholder-gray-500 focus:border-transparent focus:ring-2 focus:ring-blue-500"
                    placeholder="ornek@firma.com"
                  />
                </div>
              </div>

              <div className="grid gap-4 md:grid-cols-2">
                <div>
                  <label htmlFor="company" className="mb-1 block text-sm font-medium text-gray-700">
                    Şirket Adı *
                  </label>
                  <input
                    type="text"
                    id="company"
                    name="company"
                    required
                    value={formData.company}
                    onChange={handleChange}
                    className="w-full rounded-lg border border-gray-300 px-3 py-2 text-gray-900 transition-all duration-200 placeholder-gray-500 focus:border-transparent focus:ring-2 focus:ring-blue-500"
                    placeholder="Şirket adınız"
                  />
                </div>

                <div>
                  <label htmlFor="phone" className="mb-1 block text-sm font-medium text-gray-700">
                    Telefon
                  </label>
                  <input
                    type="tel"
                    id="phone"
                    name="phone"
                    value={formData.phone}
                    onChange={handleChange}
                    className="w-full rounded-lg border border-gray-300 px-3 py-2 text-gray-900 transition-all duration-200 placeholder-gray-500 focus:border-transparent focus:ring-2 focus:ring-blue-500"
                    placeholder="05xx xxx xx xx"
                  />
                </div>
              </div>

              <div>
                <label htmlFor="vehicleCount" className="mb-1 block text-sm font-medium text-gray-700">
                  Filo Büyüklüğü
                </label>
                <select
                  id="vehicleCount"
                  name="vehicleCount"
                  value={formData.vehicleCount}
                  onChange={handleChange}
                  className="w-full rounded-lg border border-gray-300 px-3 py-2 text-gray-700 transition-all duration-200 focus:border-transparent focus:ring-2 focus:ring-blue-500"
                >
                  <option value="">Seçiniz</option>
                  <option value="1-5">1-5 araç</option>
                  <option value="6-15">6-15 araç</option>
                  <option value="16-30">16-30 araç</option>
                  <option value="30+">30+ araç</option>
                </select>
              </div>

              <div>
                <label htmlFor="message" className="mb-1 block text-sm font-medium text-gray-700">
                  Mesaj
                </label>
                <textarea
                  id="message"
                  name="message"
                  rows={3}
                  value={formData.message}
                  onChange={handleChange}
                  className="w-full rounded-lg border border-gray-300 px-3 py-2 text-gray-900 transition-all duration-200 placeholder-gray-500 focus:border-transparent focus:ring-2 focus:ring-blue-500"
                  placeholder="Demo sırasında özellikle görmek istediğiniz akışları yazın"
                />
              </div>

              <button
                type="submit"
                disabled={loading}
                className="w-full rounded-lg bg-gray-900 px-6 py-3 font-semibold text-white shadow-lg transition-all duration-200 hover:bg-gray-800 hover:shadow-xl disabled:cursor-not-allowed disabled:opacity-50"
              >
                {loading ? 'Gönderiliyor...' : 'Demo Talep Et'}
              </button>
            </form>
          </div>
        </div>
      </section>
    </div>
  );
};

export default TrialPageContent;
