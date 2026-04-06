'use client';

import React from 'react';
import Link from 'next/link';
import { motion } from 'framer-motion';
import { ArrowRight, PackageCheck, Route, Smartphone, Truck } from 'lucide-react';

const SocialProof: React.FC = () => {
  const operatingModels = [
    {
      icon: Route,
      title: 'Dağıtım planlama ekipleri',
      description: 'Durak sıralama, teslimat sırasını değiştirme ve aynı gün rota güncelleme ihtiyacı olan operasyonlar.',
      bullets: ['Birden fazla rota yönetimi', 'Planlama değişikliğine hızlı tepki', 'Depoya dönüş senaryoları']
    },
    {
      icon: Smartphone,
      title: 'Saha ve sürücü ekipleri',
      description: 'Mobil uygulama üzerinden görev listesi, kanıt toplama ve teslimat sonucu kaydı tutan saha ekipleri.',
      bullets: ['Teslimat kanıtı', 'Foto ve imza akışı', 'Durak bazlı durum güncelleme']
    },
    {
      icon: Truck,
      title: 'Operasyon yöneticileri',
      description: 'Rotaların durumu, aktif seferler ve teslimat performansını tek ekrandan izlemek isteyen yöneticiler.',
      bullets: ['Canlı sefer görünürlüğü', 'Raporlama ve arşiv', 'Ekiplere göre erişim']
    }
  ];

  const modules = [
    { title: 'Rota planlama', icon: Route },
    { title: 'Sefer takibi', icon: Truck },
    { title: 'Mobil saha uygulaması', icon: Smartphone },
    { title: 'Teslimat kanıtı', icon: PackageCheck }
  ];

  return (
    <section className="border-b border-[color:var(--line)] bg-[#f8fafc] py-16 lg:py-22">
      <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
        <motion.div
          initial={{ opacity: 0, y: 24 }}
          whileInView={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.7 }}
          viewport={{ once: true }}
          className="mx-auto max-w-3xl text-center"
        >
          <div className="mb-5 inline-flex rounded-full border border-slate-200 bg-white px-4 py-2 text-sm font-semibold text-[color:var(--brand-deep)] shadow-sm">
            Kimler için uygun?
          </div>
          <h2 className="text-3xl font-bold tracking-tight text-slate-950 lg:text-5xl">
            YolPilot, dağıtımı kaotik değil kontrollü yönetmek isteyen ekipler için tasarlandı.
          </h2>
          <p className="mt-5 text-lg leading-8 text-slate-600">
            Amaç daha “gösterişli” bir panel vermek değil; planlamadan teslimat kanıtına kadar herkesin aynı operasyon
            akışı üzerinden çalışmasını sağlamak.
          </p>
        </motion.div>

        <div className="mt-12 grid gap-6 lg:grid-cols-3">
          {operatingModels.map((item, index) => {
            const Icon = item.icon;
            return (
              <motion.div
                key={item.title}
                initial={{ opacity: 0, y: 24 }}
                whileInView={{ opacity: 1, y: 0 }}
                transition={{ duration: 0.7, delay: index * 0.08 }}
                viewport={{ once: true }}
                className="rounded-[1.8rem] border border-slate-200 bg-white p-7 shadow-sm shadow-slate-900/5"
              >
                <div className="mb-5 flex h-13 w-13 items-center justify-center rounded-2xl bg-[color:var(--surface-muted)] text-[color:var(--brand)]">
                  <Icon className="h-6 w-6" />
                </div>
                <h3 className="text-2xl font-semibold text-slate-950">{item.title}</h3>
                <p className="mt-4 text-base leading-7 text-slate-600">{item.description}</p>
                <ul className="mt-5 space-y-3">
                  {item.bullets.map((bullet) => (
                    <li key={bullet} className="flex items-start gap-3 text-sm text-slate-700">
                      <span className="mt-1 h-2 w-2 rounded-full bg-[color:var(--accent)]" />
                      <span>{bullet}</span>
                    </li>
                  ))}
                </ul>
              </motion.div>
            );
          })}
        </div>

        <motion.div
          initial={{ opacity: 0, y: 24 }}
          whileInView={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.7, delay: 0.25 }}
          viewport={{ once: true }}
          className="mt-12 rounded-[2rem] border border-slate-200 bg-white px-6 py-8 shadow-sm shadow-slate-900/5 lg:px-8"
        >
          <div className="flex flex-col gap-6 lg:flex-row lg:items-center lg:justify-between">
            <div>
              <div className="text-sm font-semibold uppercase tracking-[0.18em] text-[color:var(--ink-soft)]">Platform kapsamı</div>
              <h3 className="mt-2 text-2xl font-semibold text-slate-950">Aynı operasyon dilini web panel ve mobilde korur.</h3>
            </div>
            <Link
              href="/features"
              className="inline-flex items-center gap-2 text-sm font-semibold text-[color:var(--brand)] transition-colors hover:text-blue-700"
            >
              Tüm özellikleri inceleyin
              <ArrowRight className="h-4 w-4" />
            </Link>
          </div>

          <div className="mt-7 grid gap-4 md:grid-cols-2 xl:grid-cols-4">
            {modules.map((item) => {
              const Icon = item.icon;
              return (
                <div key={item.title} className="rounded-2xl bg-[color:var(--surface-muted)] px-5 py-4">
                  <div className="flex items-center gap-3">
                    <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-white text-[color:var(--brand)] shadow-sm">
                      <Icon className="h-5 w-5" />
                    </div>
                    <div className="text-sm font-semibold text-slate-800">{item.title}</div>
                  </div>
                </div>
              );
            })}
          </div>
        </motion.div>
      </div>
    </section>
  );
};

export default SocialProof;
