'use client';

import React from 'react';
import Link from 'next/link';
import { motion } from 'framer-motion';

const SocialProof: React.FC = () => {
  const testimonials = [
    {
      id: 1,
      name: 'Operasyon Ekibi',
      company: 'Lojistik',
      role: 'Planlama ve Dagitim',
      industry: 'Lojistik',
      quote:
        'Rota planlamasi tek ekrandan daha duzenli hale geldi. Ekip ici koordinasyon kolaylasti ve gecikmeler daha erken gorunur oldu.'
    },
    {
      id: 2,
      name: 'Saha Yonetimi',
      company: 'Perakende Dagitim',
      role: 'Saha Koordinasyonu',
      industry: 'Perakende',
      quote:
        'Sahadaki sureci takip etmek ve teslimat kanitlarini toplamak artik daha duzenli ve izlenebilir.'
    },
    {
      id: 3,
      name: 'Musteri Deneyimi',
      company: 'E-ticaret',
      role: 'Musteri Iletisimi',
      industry: 'E-ticaret',
      quote:
        'Teslimat bilgileri net paylasildigi icin musteri bilgilendirme surecimiz sadelesti.'
    }
  ];

  const highlights = [
    {
      title: 'Planlama',
      description: 'Rota ve is emri yonetimi'
    },
    {
      title: 'Gorunurluk',
      description: 'Anlik izleme ve durum guncelleme'
    },
    {
      title: 'Kanit',
      description: 'Teslimat belgesi ve fotograf'
    },
    {
      title: 'Iletisim',
      description: 'Musteri bilgilendirme akisi'
    }
  ];

  return (
    <section className="py-16 lg:py-24 bg-gradient-to-br from-gray-50 to-blue-50 relative overflow-hidden">
      <div className="absolute inset-0 overflow-hidden opacity-5">
        <svg className="absolute top-0 left-0 w-full h-full" viewBox="0 0 1200 800">
          <circle cx="200" cy="200" r="300" fill="#3b82f6" />
          <circle cx="1000" cy="600" r="250" fill="#10b981" />
        </svg>
      </div>

      <div className="relative max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <motion.div
          initial={{ opacity: 0, y: 30 }}
          whileInView={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.8 }}
          viewport={{ once: true }}
          className="text-center mb-16"
        >
          <div className="inline-flex items-center gap-2 bg-blue-100 px-4 py-2 rounded-full mb-6">
            <span className="text-sm font-semibold text-blue-900">Musteri Deneyimi</span>
          </div>

          <h2 className="text-3xl lg:text-5xl font-bold text-gray-900 mb-6">
            Farkli Ekiplerden Ortak Geri Bildirimler
          </h2>
          <p className="text-xl text-gray-600 max-w-3xl mx-auto">
            Lojistik, e-ticaret ve saha ekipleri YolPilot ile surecleri daha net ve izlenebilir hale getiriyor.
          </p>
        </motion.div>

        <motion.div
          initial={{ opacity: 0, y: 30 }}
          whileInView={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.8, delay: 0.2 }}
          viewport={{ once: true }}
          className="grid grid-cols-2 lg:grid-cols-4 gap-6 mb-16"
        >
          {highlights.map((item) => (
            <motion.div
              key={item.title}
              initial={{ opacity: 0, y: 20 }}
              whileInView={{ opacity: 1, y: 0 }}
              transition={{ duration: 0.6 }}
              viewport={{ once: true }}
              className="bg-white rounded-2xl p-6 shadow-lg hover:shadow-xl transition-all duration-300 text-center border border-gray-100"
            >
              <div className="text-2xl font-bold text-blue-600 mb-2">{item.title}</div>
              <div className="text-sm lg:text-base text-gray-600 font-medium">{item.description}</div>
            </motion.div>
          ))}
        </motion.div>

        <motion.div
          initial={{ opacity: 0, y: 30 }}
          whileInView={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.8, delay: 0.4 }}
          viewport={{ once: true }}
          className="bg-white rounded-3xl shadow-xl p-8 lg:p-12 mb-16 border border-gray-100"
        >
          <h3 className="text-2xl font-bold text-gray-900 text-center mb-6">Referans Paylasimi</h3>
          <p className="text-center text-gray-600 max-w-2xl mx-auto">
            Referans gorusmesi veya detayli ornek akislari icin bizimle iletisime gecin. Uygun oldugunda paylasim sagliyoruz.
          </p>
        </motion.div>

        <div className="grid md:grid-cols-2 lg:grid-cols-3 gap-8">
          {testimonials.map((testimonial, index) => (
            <motion.div
              key={testimonial.id}
              initial={{ opacity: 0, y: 30 }}
              whileInView={{ opacity: 1, y: 0 }}
              transition={{ duration: 0.6, delay: 0.5 + index * 0.1 }}
              viewport={{ once: true }}
              className="bg-white rounded-2xl shadow-lg p-8 hover:shadow-2xl transition-all duration-300 border border-gray-100 flex flex-col"
            >
              <div className="inline-flex items-center gap-2 bg-blue-50 px-3 py-1 rounded-full text-sm font-medium text-blue-700 mb-4 self-start">
                {testimonial.industry}
              </div>

              <p className="text-gray-700 leading-relaxed mb-6 flex-grow italic">
                &quot;{testimonial.quote}&quot;
              </p>

              <div className="flex items-center gap-4 pt-4 border-t border-gray-100">
                <div className="w-12 h-12 rounded-full bg-gradient-to-br from-blue-500 to-indigo-600 flex items-center justify-center text-xl flex-shrink-0 text-white">
                  {testimonial.name.charAt(0)}
                </div>
                <div>
                  <div className="font-bold text-gray-900">{testimonial.name}</div>
                  <div className="text-sm text-gray-600">{testimonial.role}</div>
                  <div className="text-sm text-blue-600 font-medium">{testimonial.company}</div>
                </div>
              </div>
            </motion.div>
          ))}
        </div>

        <motion.div
          initial={{ opacity: 0, y: 30 }}
          whileInView={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.6, delay: 0.8 }}
          viewport={{ once: true }}
          className="mt-16 text-center"
        >
          <Link
            href="/contact"
            className="inline-flex items-center gap-2 bg-blue-600 hover:bg-blue-700 text-white px-8 py-4 rounded-xl font-semibold transition-all duration-200 shadow-lg hover:shadow-xl transform hover:-translate-y-1"
          >
            Demo Talep Edin
            <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M13 7l5 5m0 0l-5 5m5-5H6" />
            </svg>
          </Link>
        </motion.div>
      </div>
    </section>
  );
};

export default SocialProof;
