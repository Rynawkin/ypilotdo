'use client';

import React from 'react';
import Link from 'next/link';
import { motion } from 'framer-motion';

export default function SecurityPage() {
  const securityAreas = [
    {
      title: 'Veri Güvenliği',
      description: 'Veri iletimi ve saklama süreçlerinde erişim kontrolü yaklaşımı.'
    },
    {
      title: 'Kimlik Doğrulama',
      description: 'Rol bazlı yetkilendirme ve oturum güvenliği politikası.'
    },
    {
      title: 'Yedekleme',
      description: 'Veri sürekliliği için yedekleme ve geri yükleme planları.'
    },
    {
      title: 'İzleme',
      description: 'Operasyonel izleme ve olay yönetimi süreçleri.'
    },
    {
      title: 'KVKK Yaklaşımı',
      description: 'Gizlilik ve veri işleme süreçleri için KVKK odaklı prensipler.'
    },
    {
      title: 'Denetim ve Loglama',
      description: 'İşlem kayıtlarıyla izlenebilirlik ve raporlama.'
    }
  ];

  const faqs = [
    {
      q: 'Veriler nerede saklanır?',
      a: 'Veriler yetkilendirilmiş veri merkezlerinde saklanır. Detaylar talep üzerine paylaşılır.'
    },
    {
      q: 'Veri silme talepleri nasıl yönetilir?',
      a: 'KVKK kapsamındaki talepler için belirli bir süreç uygulanır. İletişim sayfasından bize ulaşabilirsiniz.'
    },
    {
      q: 'Güvenlik bildirimi süreci nedir?',
      a: 'Olay yönetimi ve bilgilendirme süreçleri belirlenmiş olup gerekli durumlarda paydaşlara bilgilendirme yapılır.'
    }
  ];

  const badges = ['Veri Güvenliği', 'Güvenli İletişim', 'Yedekli Altyapı', 'Süreç İzleme'];

  const documents = [
    { title: 'KVKK Aydınlatma Metni', link: '/docs/kvkk' },
    { title: 'Gizlilik Politikası', link: '/privacy' },
    { title: 'Kullanım Koşulları', link: '/terms' }
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
            <h1 className="text-5xl font-bold mb-6">Güvenlik ve Gizlilik</h1>
            <p className="text-xl text-gray-300 mb-12 max-w-3xl mx-auto">
              YolPilot, veri güvenliği ve gizlilik prensiplerini operasyonun merkezine alır. Süreçlerimizi sizin ihtiyacınıza göre birlikte planlarız.
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
            <h2 className="text-4xl font-bold text-gray-900 mb-4">Güvenlik Başlıkları</h2>
            <p className="text-xl text-gray-600 max-w-2xl mx-auto">
              Aşağıdaki başlıklar, teknik ve operasyonel güvenlik yaklaşımımızın özeti niteliğindedir.
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
            <h2 className="text-4xl font-bold text-gray-900 mb-4">Politikalar ve Dokümanlar</h2>
            <p className="text-xl text-gray-600 max-w-2xl mx-auto">
              KVKK, gizlilik ve kullanım koşulları hakkındaki dokümanlara buradan erişebilirsiniz.
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
                  <p className="text-gray-500 text-sm">Detayları görüntüleyin</p>
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
            <h2 className="text-4xl font-bold text-gray-900 mb-4">Sık Sorulan Sorular</h2>
            <p className="text-xl text-gray-600">Güvenlik ve gizlilik hakkında genel sorular</p>
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
