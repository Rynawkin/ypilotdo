'use client';

import React from 'react';
import Link from 'next/link';
import { motion } from 'framer-motion';
import { ArrowRight, Camera, ChartNoAxesCombined, Map, Smartphone, Truck } from 'lucide-react';

const FeaturesPreview: React.FC = () => {
  const features = [
    {
      icon: Map,
      title: 'Rota planlama',
      description: 'Durak sirasini, depoya donus kurgusunu ve planlama kurallarini ayni ekranda yonetin.',
      bullets: ['Durak ve rota kurgusu', 'Manuel ve otomatik duzenleme', 'Depoya donus takibi']
    },
    {
      icon: Smartphone,
      title: 'Mobil saha uygulamasi',
      description: 'Suruculer gorev listesini, notlarini ve teslimat sonucunu mobil uygulamadan yonetsin.',
      bullets: ['Gorev listesi', 'Durum guncelleme', 'Offline kullanim destegi']
    },
    {
      icon: Camera,
      title: 'Teslimat kaniti',
      description: 'Imza, fotograf ve aciklama kayitlari siparis akisina bagli kalsin.',
      bullets: ['Foto ve imza', 'Durak notlari', 'Arsivlenebilir kayit']
    },
    {
      icon: ChartNoAxesCombined,
      title: 'Rapor ve izleme',
      description: 'Sefer performansi, gecikme alanlari ve operasyon ozetleri ayni panelden gorunsun.',
      bullets: ['Sefer raporlari', 'Canli operasyon gorunurlugu', 'Gecikme analizi']
    }
  ];

  return (
    <section className="border-b border-[color:var(--line)] bg-[#f6f8fb] py-16 lg:py-24">
      <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
        <motion.div
          initial={{ opacity: 0, y: 24 }}
          whileInView={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.7 }}
          viewport={{ once: true }}
          className="flex flex-col gap-6 lg:flex-row lg:items-end lg:justify-between"
        >
          <div className="max-w-3xl">
            <div className="mb-5 inline-flex rounded-full border border-slate-200 bg-white px-4 py-2 text-sm font-semibold text-[color:var(--brand-deep)] shadow-sm">
              Urun kapsami
            </div>
            <h2 className="text-3xl font-bold tracking-tight text-slate-950 lg:text-5xl">Platformun ana parcalari ayni is akisi icin tasarlandi.</h2>
            <p className="mt-5 text-lg leading-8 text-slate-600">
              YolPilot sadece rota sirasini degil, sahadaki teslimat sonucunu ve operasyon kayitlarini da ayni zincire
              baglar.
            </p>
          </div>
          <Link
            href="/features"
            className="inline-flex items-center gap-2 text-sm font-semibold text-[color:var(--brand)] transition-colors hover:text-blue-700"
          >
            Tum ozellikleri gorun
            <ArrowRight className="h-4 w-4" />
          </Link>
        </motion.div>

        <div className="mt-12 grid gap-6 md:grid-cols-2">
          {features.map((feature, index) => {
            const Icon = feature.icon;

            return (
              <motion.div
                key={feature.title}
                initial={{ opacity: 0, y: 24 }}
                whileInView={{ opacity: 1, y: 0 }}
                transition={{ duration: 0.7, delay: index * 0.08 }}
                viewport={{ once: true }}
                className="rounded-[1.8rem] border border-slate-200 bg-white p-7 shadow-sm shadow-slate-900/5"
              >
                <div className="flex items-start justify-between gap-4">
                  <div className="flex h-13 w-13 items-center justify-center rounded-2xl bg-[color:var(--surface-muted)] text-[color:var(--brand)]">
                    <Icon className="h-6 w-6" />
                  </div>
                  <div className="rounded-full border border-slate-200 px-3 py-1 text-xs font-semibold uppercase tracking-[0.16em] text-[color:var(--ink-soft)]">
                    Modul
                  </div>
                </div>

                <h3 className="mt-6 text-2xl font-semibold text-slate-950">{feature.title}</h3>
                <p className="mt-4 text-base leading-7 text-slate-600">{feature.description}</p>

                <div className="mt-6 grid gap-3 sm:grid-cols-3">
                  {feature.bullets.map((bullet) => (
                    <div key={bullet} className="rounded-2xl bg-[color:var(--surface-muted)] px-4 py-3 text-sm font-medium text-slate-700">
                      {bullet}
                    </div>
                  ))}
                </div>
              </motion.div>
            );
          })}
        </div>

        <motion.div
          initial={{ opacity: 0, y: 24 }}
          whileInView={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.7, delay: 0.25 }}
          viewport={{ once: true }}
          className="mt-12 rounded-[2rem] bg-[linear-gradient(135deg,#102d64,#153f8a)] px-6 py-8 text-white shadow-xl shadow-blue-950/15 lg:px-8"
        >
          <div className="flex flex-col gap-6 lg:flex-row lg:items-center lg:justify-between">
            <div className="max-w-3xl">
              <div className="mb-3 text-xs font-semibold uppercase tracking-[0.2em] text-blue-100">YolPilot akisi</div>
              <h3 className="text-2xl font-semibold lg:text-3xl">Planlama, saha ve teslimat sonucu ayni veri yapisi uzerinden ilerler.</h3>
            </div>
            <div className="inline-flex items-center gap-3 rounded-full bg-white/10 px-5 py-3 text-sm font-medium text-blue-50">
              <Truck className="h-4 w-4" />
              Web panel ile mobil uygulama birlikte calisir
            </div>
          </div>
        </motion.div>
      </div>
    </section>
  );
};

export default FeaturesPreview;
