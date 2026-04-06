'use client';

import React from 'react';
import { motion } from 'framer-motion';
import { BadgeCheck, DatabaseBackup, LifeBuoy, ShieldCheck, UserRoundCog } from 'lucide-react';

const TrustIndicators: React.FC = () => {
  const safeguards = [
    {
      icon: ShieldCheck,
      title: 'Rol bazli erisim',
      description: 'Dispatcher, surucu ve yonetici gibi farkli roller icin ayri akislar kurgulanabilir.'
    },
    {
      icon: DatabaseBackup,
      title: 'Kayit ve arsiv yapisi',
      description: 'Sefer, teslimat kaniti ve operasyon notlari ayni veri akisinda tutulur.'
    },
    {
      icon: UserRoundCog,
      title: 'Kurulumda rehberlik',
      description: 'Mevcut operasyon modeline gore ilk kurulum ve gecis adimlari birlikte planlanir.'
    },
    {
      icon: LifeBuoy,
      title: 'Turkce destek',
      description: 'Canli kullanim sirasinda ekipler sorularina Turkce destek ile karsilik bulur.'
    }
  ];

  const checklist = ['Erisim ve rol kurgusu', 'Islem takibi ve kayit mantigi', 'Operasyona gore onboarding', 'Destek ve sureklilik'];

  return (
    <section className="border-b border-[color:var(--line)] bg-white py-16 lg:py-24">
      <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
        <div className="grid gap-10 lg:grid-cols-[minmax(0,0.95fr)_minmax(0,1.05fr)] lg:items-start">
          <motion.div
            initial={{ opacity: 0, y: 24 }}
            whileInView={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.7 }}
            viewport={{ once: true }}
          >
            <div className="inline-flex rounded-full border border-slate-200 bg-slate-50 px-4 py-2 text-sm font-semibold text-[color:var(--brand-deep)]">
              Gecis ve guven
            </div>
            <h2 className="mt-5 text-3xl font-bold tracking-tight text-slate-950 lg:text-5xl">Yeni bir operasyon araci alirken sadece ozellige degil gecise de bakilir.</h2>
            <p className="mt-5 text-lg leading-8 text-slate-600">
              Pazarlama sayfalarinda en kolay kisim “guvenliyiz” demektir. Bizim odagimiz daha pratik: ekibin farkli
              rolleri ayni sisteme nasil gececek, kayit mantigi nasil korunacak ve operasyon bozulmadan nasil devreye alinacak?
            </p>

            <div className="mt-8 rounded-[2rem] bg-[#102d64] p-7 text-white shadow-xl shadow-blue-950/15">
              <div className="flex items-center gap-3">
                <div className="flex h-11 w-11 items-center justify-center rounded-2xl bg-white/12">
                  <BadgeCheck className="h-5 w-5" />
                </div>
                <div>
                  <div className="text-xs font-semibold uppercase tracking-[0.18em] text-blue-100">YolPilot yaklasimi</div>
                  <div className="mt-1 text-xl font-semibold">Urun kurulumu kadar operasyon gecisini de sahiplenir.</div>
                </div>
              </div>

              <div className="mt-6 grid gap-3 sm:grid-cols-2">
                {checklist.map((item) => (
                  <div key={item} className="rounded-2xl border border-white/10 bg-white/8 px-4 py-3 text-sm text-blue-50">
                    {item}
                  </div>
                ))}
              </div>
            </div>
          </motion.div>

          <div className="grid gap-5 sm:grid-cols-2">
            {safeguards.map((item, index) => {
              const Icon = item.icon;

              return (
                <motion.div
                  key={item.title}
                  initial={{ opacity: 0, y: 24 }}
                  whileInView={{ opacity: 1, y: 0 }}
                  transition={{ duration: 0.7, delay: index * 0.08 }}
                  viewport={{ once: true }}
                  className="rounded-[1.8rem] border border-slate-200 bg-[#f8fafc] p-7 shadow-sm shadow-slate-900/5"
                >
                  <div className="flex h-12 w-12 items-center justify-center rounded-2xl bg-white text-[color:var(--brand)] shadow-sm">
                    <Icon className="h-6 w-6" />
                  </div>
                  <h3 className="mt-5 text-xl font-semibold text-slate-950">{item.title}</h3>
                  <p className="mt-3 text-base leading-7 text-slate-600">{item.description}</p>
                </motion.div>
              );
            })}
          </div>
        </div>
      </div>
    </section>
  );
};

export default TrustIndicators;
