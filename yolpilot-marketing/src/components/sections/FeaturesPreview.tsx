'use client';

import React from 'react';
import Link from 'next/link';
import { motion } from 'framer-motion';

const FeaturesPreview: React.FC = () => {
  const features = [
    {
      icon: 'RP',
      title: 'Rota Planlama',
      description: 'Durak siralama, zaman penceresi ve oncelik kurallari tek panelde.',
      benefits: ['Rota kurallari', 'Durak siralama', 'Planlama akisi'],
      gradient: 'from-blue-500 to-blue-700',
      businessValue: 'Daha net planlama'
    },
    {
      icon: 'TT',
      title: 'Teslimat Takibi',
      description: 'Teslimat durumlari, notlar ve kanitlar tek yerde toplanir.',
      benefits: ['Durum takibi', 'Teslimat notlari', 'Kanit arsivi'],
      gradient: 'from-green-500 to-green-700',
      businessValue: 'Gorunurluk artisi'
    },
    {
      icon: 'SU',
      title: 'Saha Uygulamasi',
      description: 'Suruculer gorev listesini mobil uygulamada takip eder.',
      benefits: ['Mobil gorevler', 'Navigasyon baglantisi', 'Offline mod'],
      gradient: 'from-purple-500 to-purple-700',
      businessValue: 'Saha uyumu'
    },
    {
      icon: 'RP',
      title: 'Raporlama',
      description: 'Sefer ozetleri ve operasyon gorunumleri tek ekranda.',
      benefits: ['Sefer ozeti', 'Rapor filtreleri', 'Arsivleme'],
      gradient: 'from-orange-500 to-orange-700',
      businessValue: 'Karar destegi'
    }
  ];

  return (
    <section className="py-16 lg:py-24 bg-gray-50">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <motion.div
          initial={{ opacity: 0, y: 30 }}
          whileInView={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.8 }}
          viewport={{ once: true }}
          className="text-center mb-16"
        >
          <h2 className="text-3xl lg:text-4xl font-bold text-gray-900 mb-4">
            Ihtiyaciniz olan araclar tek platformda
          </h2>
          <p className="text-xl text-gray-600 max-w-3xl mx-auto mb-8">
            Rota planlamadan teslimat kanitina kadar operasyonu sade tutan bir akis.
          </p>
          <Link href="/features" className="inline-flex items-center text-blue-600 hover:text-blue-700 font-semibold">
            Tum ozellikleri kesfedin
            <svg className="ml-2 w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M17 8l4 4m0 0l-4 4m4-4H3" />
            </svg>
          </Link>
        </motion.div>

        <div className="grid md:grid-cols-2 lg:grid-cols-4 gap-8">
          {features.map((feature, index) => (
            <motion.div
              key={feature.title}
              initial={{ opacity: 0, y: 30 }}
              whileInView={{ opacity: 1, y: 0 }}
              transition={{ duration: 0.8, delay: index * 0.1 }}
              viewport={{ once: true }}
              className="group bg-white rounded-2xl p-6 shadow-sm hover:shadow-xl transition-all duration-300 border border-gray-100 hover:border-blue-200 flex flex-col h-full relative"
            >
              <div className={`w-14 h-14 rounded-xl bg-gradient-to-r ${feature.gradient} flex items-center justify-center text-white text-sm font-semibold mb-4 group-hover:scale-110 transition-transform duration-300`}>
                {feature.icon}
              </div>

              <h3 className="text-xl font-bold text-gray-900 mb-3 group-hover:text-blue-600 transition-colors duration-200">
                {feature.title}
              </h3>

              <p className="text-gray-600 mb-4 leading-relaxed">{feature.description}</p>

              <ul className="space-y-2 mb-4">
                {feature.benefits.map((benefit) => (
                  <li key={benefit} className="flex items-center text-sm text-gray-500">
                    <svg className="w-4 h-4 text-green-500 mr-2 flex-shrink-0" fill="currentColor" viewBox="0 0 20 20">
                      <path fillRule="evenodd" d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z" clipRule="evenodd" />
                    </svg>
                    {benefit}
                  </li>
                ))}
              </ul>

              <div className="bg-blue-50 rounded-lg p-3 mt-auto">
                <div className="text-xs text-blue-600 uppercase tracking-wide font-medium mb-1">Is Degeri</div>
                <div className="text-sm text-blue-800 font-semibold">{feature.businessValue}</div>
              </div>

              <div className="absolute inset-0 bg-gradient-to-br from-blue-50 to-transparent rounded-2xl opacity-0 group-hover:opacity-100 transition-opacity duration-300 -z-10"></div>
            </motion.div>
          ))}
        </div>

        <motion.div
          initial={{ opacity: 0, y: 30 }}
          whileInView={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.8, delay: 0.4 }}
          viewport={{ once: true }}
          className="mt-16 text-center bg-gradient-to-r from-blue-600 to-blue-700 rounded-2xl p-8 lg:p-12 text-white"
        >
          <h3 className="text-2xl lg:text-3xl font-bold mb-4">
            Operasyonunuza uygun kurgu icin bizimle gorusun
          </h3>
          <p className="text-blue-100 text-lg mb-8 max-w-2xl mx-auto">
            Ihtiyaciniza uygun planlama ve kurulum adimlarini birlikte belirleyelim.
          </p>
          <div className="flex flex-col sm:flex-row gap-4 justify-center">
            <Link
              href="/contact"
              className="bg-white text-blue-600 hover:bg-gray-50 px-8 py-4 rounded-lg font-semibold text-lg transition-all duration-200 shadow-lg hover:shadow-xl"
            >
              Demo Talep Edin
            </Link>
            <Link
              href="/features"
              className="bg-blue-700 hover:bg-blue-800 text-white border-2 border-white px-8 py-4 rounded-lg font-semibold text-lg transition-all duration-200 shadow-lg hover:shadow-xl"
            >
              Ozellikleri Inceleyin
            </Link>
          </div>
        </motion.div>
      </div>
    </section>
  );
};

export default FeaturesPreview;
