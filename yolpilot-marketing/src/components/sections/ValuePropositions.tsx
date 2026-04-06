'use client';

import React from 'react';
import { motion } from 'framer-motion';
import { BadgeCheck, Clock3, MapPinned } from 'lucide-react';

const ValuePropositions: React.FC = () => {
  const valueProps = [
    {
      icon: MapPinned,
      title: 'Planlama aynı veriyle başlar',
      description:
        'Depo, araç, sürücü ve durak bilgileri tek akışta bir araya gelir. Planlama, farklı Excel dosyaları ve mesaj grupları arasına dağılmaz.',
      details: ['Rota sıralama', 'Depoya dönüş kurgusu', 'Durak bazlı kural yönetimi']
    },
    {
      icon: Clock3,
      title: 'Sahadaki durum anlık görünür',
      description:
        'Sefer ilerlemesi, geciken duraklar ve teslimat sonuçları planlama ekranından izlenir. Operasyon, rapor beklemeden aynı gün karar alabilir.',
      details: ['Canlı sefer durumu', 'Durak ilerleme bilgisi', 'Gecikme ve istisna takibi']
    },
    {
      icon: BadgeCheck,
      title: 'Teslimat sonu kayıtları tek yerde kalır',
      description:
        'İmza, fotoğraf, not ve teslimat sonucu kayıtları aynı sipariş akışına bağlanır. Arşiv, takip ve müşteriye dönüş daha düzenli olur.',
      details: ['Teslimat kanıtı', 'Müşteri bilgilendirme akışı', 'Rapor ve arşiv kayıtları']
    }
  ];

  return (
    <section className="border-b border-[color:var(--line)] bg-white py-16 lg:py-24">
      <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
        <motion.div
          initial={{ opacity: 0, y: 24 }}
          whileInView={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.7 }}
          viewport={{ once: true }}
          className="mx-auto max-w-3xl text-center"
        >
          <div className="mb-5 inline-flex rounded-full border border-slate-200 bg-slate-50 px-4 py-2 text-sm font-semibold text-[color:var(--brand-deep)]">
            Neyi düzeltir?
          </div>
          <h2 className="text-3xl font-bold tracking-tight text-slate-950 lg:text-5xl">Operasyonun kritik kopukluklarını kapatır.</h2>
          <p className="mt-5 text-lg leading-8 text-slate-600">
            YolPilot, planlama ile sahayı ayrı sistemler gibi değil aynı operasyon zincirinin parçası olarak ele alır.
          </p>
        </motion.div>

        <div className="mt-12 grid gap-6 lg:grid-cols-3">
          {valueProps.map((item, index) => {
            const Icon = item.icon;

            return (
              <motion.div
                key={item.title}
                initial={{ opacity: 0, y: 24 }}
                whileInView={{ opacity: 1, y: 0 }}
                transition={{ duration: 0.7, delay: index * 0.08 }}
                viewport={{ once: true }}
                className="flex h-full flex-col rounded-[1.8rem] border border-slate-200 bg-[#fbfcfe] p-7 shadow-sm shadow-slate-900/5"
              >
                <div className="mb-5 flex h-13 w-13 items-center justify-center rounded-2xl bg-white text-[color:var(--brand)] shadow-sm">
                  <Icon className="h-6 w-6" />
                </div>
                <h3 className="text-2xl font-semibold text-slate-950">{item.title}</h3>
                <p className="mt-4 flex-1 text-base leading-7 text-slate-600">{item.description}</p>
                <div className="mt-6 border-t border-slate-200 pt-5">
                  <div className="mb-3 text-xs font-semibold uppercase tracking-[0.16em] text-[color:var(--ink-soft)]">Bu başlıkta</div>
                  <div className="flex flex-wrap gap-2">
                    {item.details.map((detail) => (
                      <span key={detail} className="rounded-full bg-white px-3 py-2 text-sm text-slate-700 shadow-sm">
                        {detail}
                      </span>
                    ))}
                  </div>
                </div>
              </motion.div>
            );
          })}
        </div>
      </div>
    </section>
  );
};

export default ValuePropositions;
