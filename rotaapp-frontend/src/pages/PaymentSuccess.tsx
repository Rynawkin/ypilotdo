import React from 'react';
import { useNavigate } from 'react-router-dom';
import { CheckCircle } from 'lucide-react';

const PaymentSuccess: React.FC = () => {
  const navigate = useNavigate();

  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-50 p-6">
      <div className="bg-white rounded-lg shadow-lg p-8 max-w-md w-full text-center">
        <div className="flex justify-center mb-4">
          <CheckCircle className="h-12 w-12 text-green-500" />
        </div>
        <h1 className="text-xl font-semibold text-gray-900 mb-2">Ödeme Başarılı</h1>
        <p className="text-sm text-gray-600 mb-6">
          Ödemeniz başarıyla tamamlandı. Planınız kısa süre içinde güncellenecektir.
        </p>
        <button
          onClick={() => navigate('/settings')}
          className="px-4 py-2 text-sm font-medium text-white bg-blue-600 rounded-md hover:bg-blue-700"
        >
          Ayarlara Dön
        </button>
      </div>
    </div>
  );
};

export default PaymentSuccess;
