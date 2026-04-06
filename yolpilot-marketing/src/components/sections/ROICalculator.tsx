'use client';

import React from 'react';
import Link from 'next/link';
import { motion } from 'framer-motion';
import { ArrowRight, Clock3, Fuel, Route } from 'lucide-react';

const ROICalculator: React.FC = () => {
  const outcomes = [
    {
      icon: Route,
      title: 'Planlama süresi kısalır',
      description: 'Rotalar aynı gün içinde daha hızlı hazırlanır ve son dakika değişiklikleri daha kontrollü yönetilir.'
    },
    {
      icon: Clock3,
      title: 'Sahadaki belirsizlik azalır',
      description: 'Aktif sefer, geciken durak ve teslimat sonucu aynı panelden görüldüğü için ekip içi telefon trafiği düşer.'
    },
    {
      icon: Fuel,
      title: 'Yakıt ve zaman kaybı görünür olur',
      description: 'Tek tek tasarruf sözü vermek yerine, rota kalitesindeki farkı ve operasyon etkisini daha net izlersiniz.'
    }
  ];

  const exampleBreakdown = [
    'Gün içinde manuel planlamaya ayrılan zamanın azalması',
    'Tekrar arama ve durum sorma trafiğinin düşmesi',
    'Teslimat kanıtı ve durak notlarının tek yerde toplanması'
  ];

  return (
    <section className="bg-[#0f1725] py-16 text-white lg:py-24">
      <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
        <motion.div
          initial={{ opacity: 0, y: 24 }}
          whileInView={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.7 }}
          viewport={{ once: true }}
          className="mx-auto max-w-3xl text-center"
        >
          <div className="mb-5 inline-flex rounded-full border border-white/10 bg-white/8 px-4 py-2 text-sm font-semibold text-blue-100">
            Örnek etki alanları
          </div>
          <h2 className="text-3xl font-bold tracking-tight lg:text-5xl">Her operasyonda aynı sayıyı vaat etmek doğru değil.</h2>
          <p className="mt-5 text-lg leading-8 text-slate-300">
            Bu yüzden burada karmaşık bir tasarruf hesap makinesi yerine, YolPilot&apos;ın pratikte en hızlı etki ettiği
            başlıkları özetliyoruz. Gerçek tabloyu demo sırasında kendi operasyon verinizle birlikte konuşuruz.
          </p>
        </motion.div>

        <div className="mt-12 grid gap-6 lg:grid-cols-3">
          {outcomes.map((item, index) => {
            const Icon = item.icon;

            return (
              <motion.div
                key={item.title}
                initial={{ opacity: 0, y: 24 }}
                whileInView={{ opacity: 1, y: 0 }}
                transition={{ duration: 0.7, delay: index * 0.08 }}
                viewport={{ once: true }}
                className="rounded-[1.8rem] border border-white/10 bg-white/6 p-7 shadow-2xl shadow-black/10 backdrop-blur"
              >
                <div className="flex h-12 w-12 items-center justify-center rounded-2xl bg-white/10 text-blue-100">
                  <Icon className="h-6 w-6" />
                </div>
                <h3 className="mt-5 text-2xl font-semibold text-white">{item.title}</h3>
                <p className="mt-4 text-base leading-7 text-slate-300">{item.description}</p>
              </motion.div>
            );
          })}
        </div>

        <motion.div
          initial={{ opacity: 0, y: 24 }}
          whileInView={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.7, delay: 0.25 }}
          viewport={{ once: true }}
          className="mt-12 rounded-[2rem] border border-white/10 bg-white/6 p-7"
        >
          <div className="grid gap-8 lg:grid-cols-[minmax(0,0.95fr)_minmax(0,1.05fr)] lg:items-center">
            <div>
              <div className="text-xs font-semibold uppercase tracking-[0.18em] text-slate-400">Demo sırasında netleştirilen başlıklar</div>
              <h3 className="mt-3 text-2xl font-semibold">Etki hesabını sizin rota yapınıza göre birlikte çıkaralım.</h3>
              <p className="mt-4 text-base leading-7 text-slate-300">
                Operasyon tipi, teslimat yoğunluğu, şehir dağılımı ve saha akışına göre değişen gerçek tabloyu demo
                sırasında kendi veriniz üzerinden değerlendirmek daha sağlıklıdır.
              </p>
            </div>

            <div className="space-y-3">
              {exampleBreakdown.map((item) => (
                <div key={item} className="rounded-2xl border border-white/10 bg-black/10 px-4 py-4 text-sm text-slate-200">
                  {item}
                </div>
              ))}
            </div>
          </div>

          <div className="mt-8 flex flex-col gap-3 sm:flex-row">
            <Link
              href="/contact"
              className="inline-flex items-center justify-center gap-2 rounded-full bg-white px-6 py-3 text-sm font-semibold !text-slate-900 transition-colors hover:bg-slate-100"
            >
              Demo Talep Edin
              <ArrowRight className="h-4 w-4" />
            </Link>
            <Link
              href="/features"
              className="inline-flex items-center justify-center rounded-full border border-white/10 px-6 py-3 text-sm font-semibold !text-white transition-colors hover:bg-white/6"
            >
              Özellikleri İnceleyin
            </Link>
          </div>
        </motion.div>
      </div>
    </section>
  );
};

export default ROICalculator;
