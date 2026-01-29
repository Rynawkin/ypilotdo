import React from 'react';
import { useNavigate } from 'react-router-dom';
import { XCircle } from 'lucide-react';

const PaymentFailed: React.FC = () => {
  const navigate = useNavigate();

  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-50 p-6">
      <div className="bg-white rounded-lg shadow-lg p-8 max-w-md w-full text-center">
        <div className="flex justify-center mb-4">
          <XCircle className="h-12 w-12 text-red-500" />
        </div>
        <h1 className="text-xl font-semibold text-gray-900 mb-2">Ödeme Başarısız</h1>
        <p className="text-sm text-gray-600 mb-6">
          Ödeme işlemi tamamlanamadı. Lütfen kart bilgilerinizi kontrol edip tekrar deneyin.
        </p>
        <div className="flex items-center justify-center gap-3">
          <button
            onClick={() => navigate('/settings')}
            className="px-4 py-2 text-sm font-medium text-white bg-blue-600 rounded-md hover:bg-blue-700"
          >
            Ayarlara Dön
          </button>
          <button
            onClick={() => navigate(-1)}
            className="px-4 py-2 text-sm font-medium text-gray-700 bg-gray-100 rounded-md hover:bg-gray-200"
          >
            Tekrar Dene
          </button>
        </div>
      </div>
    </div>
  );
};

export default PaymentFailed;
