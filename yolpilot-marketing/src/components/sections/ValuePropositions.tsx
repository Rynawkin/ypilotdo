'use client';

import React from 'react';
import { motion } from 'framer-motion';

const ValuePropositions: React.FC = () => {
  const valueProps = [
    {
      icon: 'RP',
      title: 'Rota ve Sefer Planlama',
      subtitle: 'Kurallar net, planlama hizli',
      description: 'Durak siralama, zaman penceresi ve oncelikler tek panelde toplanir.',
      color: 'from-blue-400 to-blue-600'
    },
    {
      icon: 'SA',
      title: 'Saha Uygulamasi',
      subtitle: 'Surucu akisi sade',
      description: 'Mobil uygulama ile gorev listesi, teslimat notu ve kanit kaydi birlikte ilerler.',
      color: 'from-green-400 to-green-600'
    },
    {
      icon: 'RA',
      title: 'Raporlama ve Arsiv',
      subtitle: 'Gorunurluk ve takip',
      description: 'Sefer ozetleri, teslimat kaniti ve operasyon notlari tek yerde saklanir.',
      color: 'from-purple-400 to-purple-600'
    }
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
          <h2 className="text-3xl lg:text-4xl font-bold text-gray-900 mb-4">
            Operasyonunuzu netlestirin
          </h2>
          <p className="text-xl text-gray-600 max-w-3xl mx-auto">
            Planlama, saha ve musteri iletisimini tek akista birlestiren bir kurgu.
          </p>
        </motion.div>

        <div className="grid md:grid-cols-3 gap-8">
          {valueProps.map((prop, index) => (
            <motion.div
              key={prop.title}
              initial={{ opacity: 0, y: 30 }}
              whileInView={{ opacity: 1, y: 0 }}
              transition={{ duration: 0.8, delay: index * 0.2 }}
              viewport={{ once: true }}
              className="group relative bg-white rounded-2xl p-8 shadow-lg hover:shadow-2xl transition-all duration-300 border border-gray-100 hover:border-gray-200"
            >
              <div className={`w-16 h-16 rounded-2xl bg-gradient-to-r ${prop.color} flex items-center justify-center text-lg font-semibold text-white mb-6 group-hover:scale-110 transition-transform duration-300`}>
                {prop.icon}
              </div>
              <h3 className="text-2xl font-bold text-gray-900 mb-2">{prop.title}</h3>
              <p className="text-lg font-semibold text-blue-600 mb-4">{prop.subtitle}</p>
              <p className="text-gray-600 leading-relaxed">{prop.description}</p>
              <div className="absolute inset-0 bg-gradient-to-br from-blue-50 to-transparent rounded-2xl opacity-0 group-hover:opacity-100 transition-opacity duration-300 -z-10"></div>
            </motion.div>
          ))}
        </div>

        <motion.div
          initial={{ opacity: 0, y: 30 }}
          whileInView={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.8, delay: 0.6 }}
          viewport={{ once: true }}
          className="mt-16 pt-16 border-t border-gray-200"
        >
          <div className="grid md:grid-cols-4 gap-8 text-center">
            <div>
              <div className="text-lg font-bold text-blue-600 mb-2">Mobil + Web + API</div>
              <div className="text-gray-600">Tek platform deneyimi</div>
            </div>
            <div>
              <div className="text-lg font-bold text-green-600 mb-2">Kurulum Rehberligi</div>
              <div className="text-gray-600">Adim adim baslangic</div>
            </div>
            <div>
              <div className="text-lg font-bold text-orange-600 mb-2">Turkce Destek</div>
              <div className="text-gray-600">Sorulara hizli geri donus</div>
            </div>
            <div>
              <div className="text-lg font-bold text-purple-600 mb-2">Teslimat Kaniti</div>
              <div className="text-gray-600">Imza ve fotograf akisi</div>
            </div>
          </div>
        </motion.div>
      </div>
    </section>
  );
};

export default ValuePropositions;
