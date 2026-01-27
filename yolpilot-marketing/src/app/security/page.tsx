'use client';

import React from 'react';
import Link from 'next/link';
import { motion } from 'framer-motion';

export default function SecurityPage() {
  const securityAreas = [
    {
      title: 'Veri Guvenligi',
      description: 'Veri iletimi ve saklama sureclerinde erisim kontrolu yaklasimi.'
    },
    {
      title: 'Kimlik Dogrulama',
      description: 'Rol bazli yetkilendirme ve oturum guvenligi politikasi.'
    },
    {
      title: 'Yedekleme',
      description: 'Veri surekliligi icin yedekleme ve geri yukleme planlari.'
    },
    {
      title: 'Izleme',
      description: 'Operasyonel izleme ve olay yonetimi surecleri.'
    },
    {
      title: 'KVKK Yaklasimi',
      description: 'Gizlilik ve veri isleme surecleri icin KVKK odakli prensipler.'
    },
    {
      title: 'Denetim ve Loglama',
      description: 'Islem kayitlariyla izlenebilirlik ve raporlama.'
    }
  ];

  const faqs = [
    {
      q: 'Veriler nerede saklanir?',
      a: 'Veriler yetkilendirilmis veri merkezlerinde saklanir. Detaylar talep uzerine paylasilir.'
    },
    {
      q: 'Veri silme talepleri nasil yonetilir?',
      a: 'KVKK kapsamindaki talepler icin belirli bir surec uygulanir. Iletisim sayfasindan bize ulasabilirsiniz.'
    },
    {
      q: 'Guvenlik bildirimi sureci nedir?',
      a: 'Olay yonetimi ve bilgilendirme surecleri belirlenmis olup gerekli durumlarda paydaslara bilgilendirme yapilir.'
    }
  ];

  const badges = ['Veri Guvenligi', 'Guvenli Iletisim', 'Yedekli Altyapi', 'Surec Izleme'];

  const documents = [
    { title: 'KVKK Aydinlatma Metni', link: '/docs/kvkk' },
    { title: 'Gizlilik Politikasi', link: '/privacy' },
    { title: 'Kullanim Kosullari', link: '/terms' }
  ];

  return (
    <div className="min-h-screen pt-20 bg-gradient-to-b from-gray-50 to-white">
      <section className="bg-gradient-to-r from-gray-900 to-gray-800 text-white py-20">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <motion.div
            initial={{ opacity: 0, y: 20 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.6 }}
            className="text-center"
          >
            <h1 className="text-5xl font-bold mb-6">Guvenlik ve Gizlilik</h1>
            <p className="text-xl text-gray-300 mb-12 max-w-3xl mx-auto">
              YolPilot, veri guvenligi ve gizlilik prensiplerini operasyonun merkezine alir. Sureclerimizi sizin ihtiyaciniza gore birlikte planlariz.
            </p>

            <div className="flex flex-wrap justify-center gap-6 mt-12">
              {badges.map((item) => (
                <div key={item} className="bg-gray-800 px-6 py-3 rounded-full">
                  <span className="font-semibold">{item}</span>
                </div>
              ))}
            </div>
          </motion.div>
        </div>
      </section>

      <section className="py-20">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <motion.div
            initial={{ opacity: 0, y: 20 }}
            whileInView={{ opacity: 1, y: 0 }}
            viewport={{ once: true }}
            transition={{ duration: 0.6 }}
            className="text-center mb-12"
          >
            <h2 className="text-4xl font-bold text-gray-900 mb-4">Guvenlik Basliklari</h2>
            <p className="text-xl text-gray-600 max-w-2xl mx-auto">
              Asagidaki basliklar, teknik ve operasyonel guvenlik yaklasimimizin ozeti niteligindedir.
            </p>
          </motion.div>

          <div className="grid md:grid-cols-2 lg:grid-cols-3 gap-8">
            {securityAreas.map((area) => (
              <motion.div
                key={area.title}
                initial={{ opacity: 0, y: 20 }}
                whileInView={{ opacity: 1, y: 0 }}
                viewport={{ once: true }}
                transition={{ duration: 0.6 }}
                className="bg-white rounded-2xl shadow-xl p-8"
              >
                <h3 className="text-2xl font-bold text-gray-900 mb-3">{area.title}</h3>
                <p className="text-gray-600 leading-relaxed">{area.description}</p>
              </motion.div>
            ))}
          </div>
        </div>
      </section>

      <section className="py-20 bg-gray-100">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <motion.div
            initial={{ opacity: 0, y: 20 }}
            whileInView={{ opacity: 1, y: 0 }}
            viewport={{ once: true }}
            transition={{ duration: 0.6 }}
            className="text-center mb-12"
          >
            <h2 className="text-4xl font-bold text-gray-900 mb-4">Politikalar ve Dokumanlar</h2>
            <p className="text-xl text-gray-600 max-w-2xl mx-auto">
              KVKK, gizlilik ve kullanim kosullari hakkindaki dokumanlara buradan erisebilirsiniz.
            </p>
          </motion.div>

          <div className="grid md:grid-cols-3 gap-6 max-w-5xl mx-auto">
            {documents.map((item) => (
              <Link
                key={item.title}
                href={item.link}
                className="bg-white rounded-xl shadow-lg p-6 flex items-center gap-4 hover:shadow-xl transition-all duration-300"
              >
                <div>
                  <h3 className="font-bold text-gray-900">{item.title}</h3>
                  <p className="text-gray-500 text-sm">Detaylari goruntuleyin</p>
                </div>
              </Link>
            ))}
          </div>
        </div>
      </section>

      <section className="py-20">
        <div className="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8">
          <motion.div
            initial={{ opacity: 0, y: 20 }}
            whileInView={{ opacity: 1, y: 0 }}
            viewport={{ once: true }}
            transition={{ duration: 0.6 }}
            className="text-center mb-12"
          >
            <h2 className="text-4xl font-bold text-gray-900 mb-4">Sik Sorulan Sorular</h2>
            <p className="text-xl text-gray-600">Guvenlik ve gizlilik hakkinda genel sorular</p>
          </motion.div>

          <div className="space-y-6">
            {faqs.map((item) => (
              <div key={item.q} className="bg-white rounded-xl shadow-lg p-6">
                <h3 className="text-xl font-semibold text-gray-900 mb-2">{item.q}</h3>
                <p className="text-gray-600">{item.a}</p>
              </div>
            ))}
          </div>
        </div>
      </section>
    </div>
  );
}
