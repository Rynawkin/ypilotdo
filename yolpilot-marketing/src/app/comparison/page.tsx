'use client';

import React from 'react';
import Link from 'next/link';
import { motion } from 'framer-motion';

const ComparisonPage: React.FC = () => {
  const comparisonRows = [
    {
      title: 'Rota Planlama',
      manual: 'Elle planlama, coklu dosya ve tekrarli is adimlari',
      yolpilot: 'Tek panelden planlama ve otomatik rota onerileri',
      benefit: 'Planlama surecinde daha net akis'
    },
    {
      title: 'Gorunurluk',
      manual: 'Sahadan geri bildirim gec gelir',
      yolpilot: 'Anlik durum guncellemeleri ve teslimat takibi',
      benefit: 'Operasyonel netlik ve izlenebilirlik'
    },
    {
      title: 'Teslimat Kaniti',
      manual: 'Fiziksel evrak veya daginik kanitlar',
      yolpilot: 'Dijital imza ve fotograf akisi',
      benefit: 'Daha duzenli kanit arsivi'
    },
    {
      title: 'Musteri Bilgilendirme',
      manual: 'Telefon veya manuel bildirim',
      yolpilot: 'Otomatik bilgilendirme akislari',
      benefit: 'Daha tutarli iletisim'
    },
    {
      title: 'Raporlama',
      manual: 'Manuel rapor ve excel islemleri',
      yolpilot: 'Dashboard ve rapor ciktilari',
      benefit: 'Hizli karar destegi'
    }
  ];

  const highlights = [
    {
      title: 'Tek Platform',
      description: 'Planlama, takip ve teslimat kaniti tek ekranda.'
    },
    {
      title: 'Saha ve Ofis Senkronu',
      description: 'Sahadan gelen bilgiler panelde toplanir.'
    },
    {
      title: 'Operasyonel Kontrol',
      description: 'Rota, surucu ve teslimat sureci net bir sekilde izlenir.'
    }
  ];

  return (
    <div className="min-h-screen bg-gray-50 pt-24">
      <section className="py-16 text-center bg-gradient-to-br from-blue-600 to-blue-700 text-white">
        <div className="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8">
          <motion.div
            initial={{ opacity: 0, y: 30 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.8 }}
          >
            <h1 className="text-4xl lg:text-5xl font-bold mb-6">
              Manuel Planlama vs. YolPilot
            </h1>
            <p className="text-xl text-blue-100">
              Rota planlama ve teslimat takibini tek bir panelde topladiginizda surecler daha net hale gelir.
            </p>
          </motion.div>
        </div>
      </section>

      <section className="py-16">
        <div className="max-w-6xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="grid md:grid-cols-3 gap-6 mb-12">
            {highlights.map((item) => (
              <div key={item.title} className="bg-white rounded-xl shadow-lg p-6">
                <h3 className="text-lg font-semibold text-gray-900 mb-2">{item.title}</h3>
                <p className="text-gray-600">{item.description}</p>
              </div>
            ))}
          </div>

          <div className="bg-white rounded-2xl shadow-xl overflow-hidden">
            <div className="grid grid-cols-4 gap-0 border-b border-gray-200 text-sm font-semibold text-gray-700 bg-gray-50">
              <div className="px-6 py-4">Baslik</div>
              <div className="px-6 py-4">Manuel</div>
              <div className="px-6 py-4">YolPilot</div>
              <div className="px-6 py-4">Kazanim</div>
            </div>
            {comparisonRows.map((row) => (
              <div key={row.title} className="grid grid-cols-4 gap-0 border-b border-gray-100 text-sm">
                <div className="px-6 py-4 font-medium text-gray-900">{row.title}</div>
                <div className="px-6 py-4 text-gray-600">{row.manual}</div>
                <div className="px-6 py-4 text-gray-600">{row.yolpilot}</div>
                <div className="px-6 py-4 text-blue-600 font-medium">{row.benefit}</div>
              </div>
            ))}
          </div>

          <div className="mt-12 text-center">
            <Link
              href="/contact"
              className="inline-flex items-center gap-2 bg-blue-600 hover:bg-blue-700 text-white px-8 py-4 rounded-xl font-semibold transition-all duration-200 shadow-lg hover:shadow-xl"
            >
              Demo Talep Edin
              <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M13 7l5 5m0 0l-5 5m5-5H6" />
              </svg>
            </Link>
          </div>
        </div>
      </section>
    </div>
  );
};

export default ComparisonPage;
