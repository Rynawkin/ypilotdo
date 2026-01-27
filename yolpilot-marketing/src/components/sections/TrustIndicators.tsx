'use client';

import React from 'react';
import { motion } from 'framer-motion';

const TrustIndicators: React.FC = () => {
  const indicators = [
    {
      id: 1,
      icon: 'G',
      title: 'Veri Guvenligi',
      description: 'Erisim kontrolu ve kayitlarla veri guvenligi odakli mimari',
      color: 'from-green-500 to-green-600',
      bgColor: 'bg-green-50'
    },
    {
      id: 2,
      icon: 'I',
      title: 'Guvenli Iletisim',
      description: 'Sifreli aktarim ve guvenli protokoller',
      color: 'from-blue-500 to-blue-600',
      bgColor: 'bg-blue-50'
    },
    {
      id: 3,
      icon: 'D',
      title: 'Turkce Destek',
      description: 'Sorulariniz icin Turkce destek ekibi ile iletisim',
      color: 'from-orange-500 to-orange-600',
      bgColor: 'bg-orange-50'
    },
    {
      id: 4,
      icon: 'Y',
      title: 'Duzenli Yedekleme',
      description: 'Veri yedekleme ve geri yukleme surecleri',
      color: 'from-purple-500 to-purple-600',
      bgColor: 'bg-purple-50'
    },
    {
      id: 5,
      icon: 'A',
      title: 'Yedekli Altyapi',
      description: 'Izleme ve yedeklilik ile operasyon surekliligi',
      color: 'from-yellow-500 to-yellow-600',
      bgColor: 'bg-yellow-50'
    },
    {
      id: 6,
      icon: 'O',
      title: 'Olceklenebilir Mimari',
      description: 'Farkli ekip boyutlarina uyumlu esnek altyapi',
      color: 'from-indigo-500 to-indigo-600',
      bgColor: 'bg-indigo-50'
    }
  ];

  const safeguards = [
    'Erisim kontrolu',
    'Loglama ve izleme',
    'Veri saklama politikalari'
  ];

  const features = [
    { title: 'Rol Bazli Erisim', description: 'Kullanici yetkilendirme sistemi' },
    { title: 'Islem Loglari', description: 'Islem kayitlari ve izleme' },
    { title: 'Yedekleme Politikasi', description: 'Veri geri yukleme yaklasimi' },
    { title: 'Sistem Izleme', description: 'Operasyon gorunurlugu' }
  ];

  return (
    <section className="py-16 lg:py-24 bg-white">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <motion.div
          initial={{ opacity: 0, y: 30 }}
          whileInView={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.8 }}
          viewport={{ once: true }}
          className="text-center mb-16"
        >
          <div className="inline-flex items-center gap-2 bg-blue-100 px-4 py-2 rounded-full mb-6">
            <span className="text-sm font-semibold text-blue-900">Guvenlik ve Sureklilik</span>
          </div>

          <h2 className="text-3xl lg:text-5xl font-bold text-gray-900 mb-6">
            Operasyonlariniz Icin Guvenli Altyapi
          </h2>
          <p className="text-xl text-gray-600 max-w-3xl mx-auto">
            Veri guvenligi, erisim kontrolu ve yedeklilik odakli altyapi yaklasimi.
          </p>
        </motion.div>

        <div className="grid md:grid-cols-2 lg:grid-cols-3 gap-8">
          {indicators.map((indicator, index) => (
            <motion.div
              key={indicator.id}
              initial={{ opacity: 0, y: 30 }}
              whileInView={{ opacity: 1, y: 0 }}
              transition={{ duration: 0.6, delay: index * 0.1 }}
              viewport={{ once: true }}
              className="group relative bg-white rounded-2xl p-8 shadow-lg hover:shadow-2xl transition-all duration-300 border border-gray-100 hover:border-gray-200"
            >
              <div className={`w-16 h-16 rounded-2xl bg-gradient-to-r ${indicator.color} flex items-center justify-center text-white text-xl font-semibold mb-6 group-hover:scale-110 transition-transform duration-300 shadow-lg`}>
                {indicator.icon}
              </div>
              <h3 className="text-2xl font-bold text-gray-900 mb-3">{indicator.title}</h3>
              <p className="text-gray-600 leading-relaxed">{indicator.description}</p>
              <div className={`absolute inset-0 ${indicator.bgColor} rounded-2xl opacity-0 group-hover:opacity-10 transition-opacity duration-300 -z-10`}></div>
            </motion.div>
          ))}
        </div>

        <motion.div
          initial={{ opacity: 0, y: 30 }}
          whileInView={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.8, delay: 0.6 }}
          viewport={{ once: true }}
          className="mt-16 bg-gradient-to-br from-blue-600 via-blue-700 to-indigo-700 rounded-3xl shadow-2xl p-8 lg:p-12 text-white relative overflow-hidden"
        >
          <div className="absolute inset-0 bg-grid-white/[0.05] bg-[size:30px_30px]"></div>
          <div className="relative z-10">
            <div className="grid lg:grid-cols-2 gap-8 items-center">
              <div>
                <h3 className="text-3xl lg:text-4xl font-bold mb-4">Guvenlik Odakli Yaklasim</h3>
                <p className="text-blue-100 text-lg leading-relaxed mb-6">
                  Veri gizliligi ve operasyon surekliligi icin sifreleme, yedekleme ve izleme sureclerini birlikte yurutuyoruz.
                </p>
                <div className="flex flex-wrap gap-4">
                  {safeguards.map((item) => (
                    <div key={item} className="flex items-center gap-2 bg-white/20 backdrop-blur-sm px-4 py-2 rounded-full">
                      <svg className="w-5 h-5" fill="currentColor" viewBox="0 0 20 20">
                        <path fillRule="evenodd" d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z" clipRule="evenodd" />
                      </svg>
                      <span className="text-sm font-semibold">{item}</span>
                    </div>
                  ))}
                </div>
              </div>

              <div className="grid grid-cols-2 gap-6">
                {[
                  { title: 'Iletisim', label: 'Sifreli aktarim' },
                  { title: 'Yedekleme', label: 'Veri saklama' },
                  { title: 'Izleme', label: 'Durum takibi' },
                  { title: 'Erisim', label: 'Yetki yonetimi' }
                ].map((item) => (
                  <div key={item.title} className="bg-white/10 backdrop-blur-sm rounded-2xl p-6 border border-white/20">
                    <div className="text-2xl font-bold mb-2">{item.title}</div>
                    <div className="text-blue-100">{item.label}</div>
                  </div>
                ))}
              </div>
            </div>
          </div>
        </motion.div>

        <motion.div
          initial={{ opacity: 0, y: 30 }}
          whileInView={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.8, delay: 0.8 }}
          viewport={{ once: true }}
          className="mt-16"
        >
          <h3 className="text-2xl font-bold text-gray-900 mb-8 text-center">Guvenlik Ozellikleri</h3>
          <div className="grid md:grid-cols-2 lg:grid-cols-4 gap-6">
            {features.map((feature) => (
              <div
                key={feature.title}
                className="text-center p-6 bg-gray-50 rounded-xl hover:bg-gray-100 transition-colors duration-200"
              >
                <div className="font-bold text-gray-900 mb-2">{feature.title}</div>
                <div className="text-sm text-gray-600">{feature.description}</div>
              </div>
            ))}
          </div>
        </motion.div>
      </div>
    </section>
  );
};

export default TrustIndicators;
