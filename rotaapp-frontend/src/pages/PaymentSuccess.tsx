import React, { useEffect, useState } from 'react';
import { CheckCircle, Loader2 } from 'lucide-react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { authService } from '@/services/auth.service';
import { paymentService } from '@/services/payment.service';

const PaymentSuccess: React.FC = () => {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const [loading, setLoading] = useState(true);
  const [message, setMessage] = useState('Ödeme doğrulanıyor...');
  const isSignupFlow = searchParams.get('flow') === 'signup';

  useEffect(() => {
    if (!isSignupFlow) {
      setLoading(false);
      setMessage('Ödemeniz başarıyla tamamlandı. Planınız kısa süre içinde güncellenecektir.');
      return;
    }

    const transactionId = localStorage.getItem('signupPaymentTransactionId');
    const signupToken = localStorage.getItem('signupPaymentToken');

    if (!transactionId || !signupToken) {
      setLoading(false);
      setMessage('Signup ödeme bilgisi bulunamadı. Lütfen tekrar deneyin.');
      return;
    }

    let attempts = 0;
    const maxAttempts = 15;

    const checkStatus = async () => {
      try {
        const result = await paymentService.getSignupPaymentStatus(transactionId, signupToken);

        if (result.isCompleted && result.auth) {
          authService.setSession(result.auth);
          localStorage.removeItem('signupPaymentTransactionId');
          localStorage.removeItem('signupPaymentToken');
          localStorage.removeItem('signupPaymentPlan');
          window.location.href = '/dashboard';
          return;
        }

        if (result.status === 'Failed' || result.status === 'Cancelled') {
          setLoading(false);
          setMessage(result.errorMessage || 'Ödeme tamamlanamadı. Lütfen tekrar deneyin.');
          return;
        }

        attempts += 1;
        if (attempts >= maxAttempts) {
          setLoading(false);
          setMessage('Ödeme alındı, hesap hazırlanıyor. Birkaç dakika sonra giriş yapabilirsiniz.');
          return;
        }

        setTimeout(checkStatus, 2000);
      } catch (error: any) {
        attempts += 1;
        if (attempts >= maxAttempts) {
          setLoading(false);
          setMessage(error?.message || 'Ödeme durumu alınamadı. Lütfen giriş ekranından tekrar deneyin.');
          return;
        }
        setTimeout(checkStatus, 2000);
      }
    };

    checkStatus();
  }, [isSignupFlow]);

  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-50 p-6">
      <div className="bg-white rounded-lg shadow-lg p-8 max-w-md w-full text-center">
        <div className="flex justify-center mb-4">
          {loading ? <Loader2 className="h-12 w-12 text-blue-500 animate-spin" /> : <CheckCircle className="h-12 w-12 text-green-500" />}
        </div>
        <h1 className="text-xl font-semibold text-gray-900 mb-2">Ödeme Başarılı</h1>
        <p className="text-sm text-gray-600 mb-6">{message}</p>
        {!loading && !isSignupFlow && (
          <button
            onClick={() => navigate('/settings')}
            className="px-4 py-2 text-sm font-medium text-white bg-blue-600 rounded-md hover:bg-blue-700"
          >
            Ayarlara Dön
          </button>
        )}
        {!loading && isSignupFlow && (
          <button
            onClick={() => navigate('/login')}
            className="px-4 py-2 text-sm font-medium text-white bg-blue-600 rounded-md hover:bg-blue-700"
          >
            Giriş Ekranına Git
          </button>
        )}
      </div>
    </div>
  );
};

export default PaymentSuccess;
