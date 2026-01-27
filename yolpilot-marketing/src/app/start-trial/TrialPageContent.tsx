'use client';

import React, { useEffect, useState } from 'react';
import { ExternalLink } from 'lucide-react';
import { useSearchParams } from 'next/navigation';
import { useToast } from '@/components/ToastContainer';

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
    e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement>
  ) => {
    setFormData({
      ...formData,
      [e.target.name]: e.target.value
    });
  };

  const handleDemoRequest = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);

    try {
      const apiUrl = '/api/proxy/api/marketinglead';

      const response = await fetch(apiUrl, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json'
        },
        body: JSON.stringify({
          ...formData,
          selectedPlan: currentPlan,
          source: 'demo_request'
        })
      });

      if (response.ok) {
        showSuccess(
          'Demo talebiniz alindi',
          'Ekibimiz sizinle iletisime gecerek demo planlamasi yapacak.'
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
        let errorMessage = 'Bir hata olustu. Lutfen tekrar deneyin.';
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
        showError('Form gonderim hatasi', errorMessage);
      }
    } catch (error) {
      console.error('Form submission error:', error);
      showError('Baglanti hatasi', 'Lutfen baglantinizi kontrol edip tekrar deneyin.');
    } finally {
      setLoading(false);
    }
  };

  const handleSignupRedirect = () => {
    const signupUrl = currentPlan
      ? `https://app.yolpilot.com/signup?plan=${encodeURIComponent(currentPlan)}`
      : 'https://app.yolpilot.com/signup';

    window.open(signupUrl, '_blank');
  };

  return (
    <div className="min-h-screen bg-gray-50 pt-24">
      <section className="py-16 text-center bg-gradient-to-br from-blue-600 to-blue-700 text-white">
        <div className="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8">
          <h1 className="text-4xl lg:text-5xl font-bold mb-6">
            YolPilot ile Baslangic
          </h1>
          <p className="text-xl text-blue-100">
            Dilerseniz demo planlayin, dilerseniz uygulamaya gecis icin kayit olusturun.
          </p>
        </div>
      </section>

      <section className="py-16">
        <div className="max-w-6xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="grid lg:grid-cols-2 gap-8">
            <div className="bg-white rounded-2xl shadow-lg p-8 border-2 border-blue-500">
              <div className="text-center mb-6">
                <div className="w-16 h-16 bg-blue-100 rounded-full flex items-center justify-center mx-auto mb-4">
                  <ExternalLink className="w-8 h-8 text-blue-600" />
                </div>
                <h2 className="text-2xl font-bold text-gray-900 mb-2">Uygulamaya Gecis</h2>
                <p className="text-gray-600">
                  Hesap olusturun, kurulum ve ilk planlama icin ekibimiz size rehberlik etsin.
                </p>
              </div>

              {currentPlan && (
                <div className="mb-6 p-4 bg-blue-50 rounded-lg">
                  <h3 className="font-semibold text-blue-900 mb-2">
                    Secilen Plan: {currentPlan}
                  </h3>
                  <p className="text-sm text-blue-800">
                    Plan detaylarini gorusmede netlestirebiliriz.
                  </p>
                </div>
              )}

              <button
                onClick={handleSignupRedirect}
                className="w-full bg-blue-600 hover:bg-blue-700 text-white px-8 py-4 rounded-lg font-semibold text-lg transition-all duration-200 shadow-lg hover:shadow-xl flex items-center justify-center gap-2"
              >
                Kayit Olustur
                <ExternalLink className="w-5 h-5" />
              </button>
            </div>

            <div className="bg-white rounded-2xl shadow-lg p-8">
              <div className="text-center mb-6">
                <div className="w-16 h-16 bg-gray-100 rounded-full flex items-center justify-center mx-auto mb-4">
                  <svg className="w-8 h-8 text-gray-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M8 12h.01M12 12h.01M16 12h.01M21 12c0 4.418-4.03 8-9 8a9.863 9.863 0 01-4.255-.949L3 20l1.395-3.72C3.512 15.042 3 13.574 3 12c0-4.418 4.03-8 9-8s9 3.582 9 8z" />
                  </svg>
                </div>
                <h2 className="text-2xl font-bold text-gray-900 mb-2">Demo Talep Edin</h2>
                <p className="text-gray-600">Ihtiyaciniza uygun bir demo planlayalim.</p>
              </div>

              <form onSubmit={handleDemoRequest} className="space-y-4">
                <div className="grid md:grid-cols-2 gap-4">
                  <div>
                    <label htmlFor="name" className="block text-sm font-medium text-gray-700 mb-1">
                      Ad Soyad *
                    </label>
                    <input
                      type="text"
                      id="name"
                      name="name"
                      required
                      value={formData.name}
                      onChange={handleChange}
                      className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent transition-all duration-200 placeholder-gray-500 text-gray-900"
                      placeholder="Adiniz"
                    />
                  </div>

                  <div>
                    <label htmlFor="email" className="block text-sm font-medium text-gray-700 mb-1">
                      E-posta *
                    </label>
                    <input
                      type="email"
                      id="email"
                      name="email"
                      required
                      value={formData.email}
                      onChange={handleChange}
                      className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent transition-all duration-200 placeholder-gray-500 text-gray-900"
                      placeholder="ornek@firma.com"
                    />
                  </div>
                </div>

                <div className="grid md:grid-cols-2 gap-4">
                  <div>
                    <label htmlFor="company" className="block text-sm font-medium text-gray-700 mb-1">
                      Sirket Adi *
                    </label>
                    <input
                      type="text"
                      id="company"
                      name="company"
                      required
                      value={formData.company}
                      onChange={handleChange}
                      className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent transition-all duration-200 placeholder-gray-500 text-gray-900"
                      placeholder="Sirket adiniz"
                    />
                  </div>

                  <div>
                    <label htmlFor="phone" className="block text-sm font-medium text-gray-700 mb-1">
                      Telefon
                    </label>
                    <input
                      type="tel"
                      id="phone"
                      name="phone"
                      value={formData.phone}
                      onChange={handleChange}
                      className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent transition-all duration-200 placeholder-gray-500 text-gray-900"
                      placeholder="05xx xxx xx xx"
                    />
                  </div>
                </div>

                <div>
                  <label htmlFor="vehicleCount" className="block text-sm font-medium text-gray-700 mb-1">
                    Filo Buyuklugu
                  </label>
                  <select
                    id="vehicleCount"
                    name="vehicleCount"
                    value={formData.vehicleCount}
                    onChange={handleChange}
                    className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent transition-all duration-200 text-gray-700"
                  >
                    <option value="">Seciniz</option>
                    <option value="1-5">1-5 arac</option>
                    <option value="6-15">6-15 arac</option>
                    <option value="16-30">16-30 arac</option>
                    <option value="30+">30+ arac</option>
                  </select>
                </div>

                <div>
                  <label htmlFor="message" className="block text-sm font-medium text-gray-700 mb-1">
                    Mesaj
                  </label>
                  <textarea
                    id="message"
                    name="message"
                    rows={3}
                    value={formData.message}
                    onChange={handleChange}
                    className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent transition-all duration-200 placeholder-gray-500 text-gray-900"
                    placeholder="Demo ile ilgili kisa notunuzu yazin"
                  />
                </div>

                <button
                  type="submit"
                  disabled={loading}
                  className="w-full bg-gray-600 hover:bg-gray-700 text-white px-6 py-3 rounded-lg font-semibold transition-all duration-200 shadow-lg hover:shadow-xl disabled:opacity-50 disabled:cursor-not-allowed"
                >
                  {loading ? 'Gonderiliyor...' : 'Demo Talep Et'}
                </button>
              </form>
            </div>
          </div>
        </div>
      </section>
    </div>
  );
};

export default TrialPageContent;
