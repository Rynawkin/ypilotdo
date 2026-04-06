'use client';

import React, { useEffect, useState } from 'react';
import Link from 'next/link';
import Image from 'next/image';
import { motion } from 'framer-motion';

const HeroSection: React.FC = () => {
  const [currentMobileScreen, setCurrentMobileScreen] = useState(0);
  const [currentWebScreen, setCurrentWebScreen] = useState(0);

  const mobileScreenshots = ['journeys.jpeg', 'create-route.jpeg', 'performance.jpeg', 'complete-stop.JPG', 'performance-2.jpeg'];
  const webScreenshots = ['web-dashboard.png', 'Web-createroute.png', 'web-reports.png'];

  useEffect(() => {
    const interval = setInterval(() => {
      setCurrentMobileScreen((prev) => (prev + 1) % mobileScreenshots.length);
      setCurrentWebScreen((prev) => (prev + 1) % webScreenshots.length);
    }, 4000);

    return () => clearInterval(interval);
  }, [mobileScreenshots.length, webScreenshots.length]);

  const highlights = ['Rota optimizasyonu', 'Teslimat kanıtı', 'Mobil sürücü uygulaması', 'Canlı sefer görünürlüğü'];

  const evidence = [
    { label: 'Planlama', value: 'Web panel' },
    { label: 'Saha', value: 'Mobil uygulama' },
    { label: 'Kontrol', value: 'Tek operasyon akışı' }
  ];

  return (
    <section className="relative overflow-hidden border-b border-[color:var(--line)] bg-transparent pb-18 pt-28 lg:pb-24 lg:pt-34">
      <div className="marketing-grid absolute inset-0 opacity-60" />
      <div className="absolute inset-0 bg-[radial-gradient(circle_at_top_right,rgba(25,85,214,0.14),transparent_34%),radial-gradient(circle_at_left_center,rgba(217,122,43,0.08),transparent_26%)]" />

      <div className="relative mx-auto grid max-w-7xl gap-14 px-4 sm:px-6 lg:grid-cols-[minmax(0,0.92fr)_minmax(0,1.08fr)] lg:items-center lg:px-8">
        <motion.div
          initial={{ opacity: 0, x: -32 }}
          animate={{ opacity: 1, x: 0 }}
          transition={{ duration: 0.7 }}
          className="text-center lg:text-left"
        >
          <div className="mb-6 inline-flex items-center gap-2 rounded-full border border-blue-200 bg-white/80 px-4 py-2 text-sm font-semibold text-[color:var(--brand-deep)] shadow-sm shadow-blue-950/5">
            Türkiye&apos;deki dağıtım ekipleri için operasyon yazılımı
          </div>

          <h1 className="max-w-3xl text-4xl font-bold tracking-tight text-slate-950 sm:text-5xl lg:text-6xl lg:leading-[1.05]">
            Rotaları daha doğru planlayın, sahayı tek panelden yönetin.
          </h1>

          <p className="mx-auto mt-6 max-w-2xl text-lg leading-8 text-slate-600 lg:mx-0 lg:text-xl">
            YolPilot; rota planlama, sefer takibi, mobil sürücü akışı ve teslimat kanıtını aynı operasyon yapısında
            birleştirir. Dağıtım ekipleri planlama ekranından sahadaki son durağa kadar aynı veriyle çalışır.
          </p>

          <div className="mt-8 flex flex-wrap justify-center gap-3 lg:justify-start">
            {highlights.map((item) => (
              <div
                key={item}
                className="rounded-full border border-slate-200 bg-white px-4 py-2 text-sm font-medium text-slate-700 shadow-sm"
              >
                {item}
              </div>
            ))}
          </div>

          <div className="mt-10 flex flex-col gap-4 sm:flex-row sm:justify-center lg:justify-start">
            <Link
              href="/contact"
              className="rounded-full bg-[color:var(--brand)] px-7 py-4 text-base font-semibold !text-white shadow-xl shadow-blue-900/15 transition-all duration-200 hover:-translate-y-0.5 hover:bg-blue-700"
            >
              Demo Talep Edin
            </Link>
            <Link
              href="/features"
              className="rounded-full border border-slate-300 bg-white px-7 py-4 text-base font-semibold text-slate-700 transition-colors duration-200 hover:border-slate-400 hover:bg-slate-50"
            >
              Platformu İnceleyin
            </Link>
          </div>

          <div className="mt-10 grid gap-4 sm:grid-cols-3">
            {evidence.map((item) => (
              <div key={item.label} className="rounded-2xl border border-white/70 bg-white/80 p-4 text-left shadow-sm shadow-slate-900/5">
                <div className="text-xs font-semibold uppercase tracking-[0.16em] text-[color:var(--ink-soft)]">{item.label}</div>
                <div className="mt-2 text-lg font-semibold text-slate-900">{item.value}</div>
              </div>
            ))}
          </div>
        </motion.div>

        <motion.div
          initial={{ opacity: 0, x: 32 }}
          animate={{ opacity: 1, x: 0 }}
          transition={{ duration: 0.8, delay: 0.2 }}
          className="relative lg:pl-6"
        >
          <div className="absolute inset-x-6 top-8 h-64 rounded-full bg-blue-600/12 blur-3xl lg:inset-x-10 lg:h-72" />

          <div className="relative flex flex-col gap-8 lg:min-h-[560px] lg:justify-center">
            <div className="relative mx-auto lg:absolute lg:bottom-4 lg:left-0 lg:z-20 lg:mx-0">
              <div className="relative h-[420px] w-52 rounded-[2.6rem] bg-slate-950 p-2 shadow-2xl shadow-slate-900/20 sm:h-[460px] sm:w-56">
                <div className="relative h-full w-full overflow-hidden rounded-[2rem] bg-slate-900">
                  <motion.div
                    key={currentMobileScreen}
                    initial={{ opacity: 0, x: 18 }}
                    animate={{ opacity: 1, x: 0 }}
                    transition={{ duration: 0.8, ease: 'easeOut' }}
                    className="relative h-full w-full"
                  >
                    <Image
                      src={`/screenshots/${mobileScreenshots[currentMobileScreen]}`}
                      alt="YolPilot mobil uygulama ekranları"
                      fill
                      sizes="(min-width: 1024px) 224px, 208px"
                      className="object-cover object-top"
                    />
                  </motion.div>
                  <div className="absolute left-1/2 top-0 h-5 w-24 -translate-x-1/2 rounded-b-2xl bg-black" />
                </div>
              </div>
              <div className="absolute -left-4 top-10 rounded-full bg-emerald-500 px-4 py-2 text-xs font-semibold text-white shadow-lg shadow-emerald-900/20">
                Mobil saha akışı
              </div>
            </div>

            <div className="relative w-full lg:ml-16 xl:ml-24">
              <div className="mx-auto w-full max-w-[860px] overflow-hidden rounded-[2rem] border border-slate-300 bg-slate-950 p-3 shadow-2xl shadow-slate-900/15">
                <div className="relative aspect-[16/10] w-full rounded-[1.45rem] bg-slate-950 lg:aspect-[16/9]">
                  <motion.div
                    key={currentWebScreen}
                    initial={{ opacity: 0, x: 24 }}
                    animate={{ opacity: 1, x: 0 }}
                    transition={{ duration: 0.8, ease: 'easeOut' }}
                    className="relative h-full w-full"
                  >
                    <Image
                      src={`/screenshots/${webScreenshots[currentWebScreen]}`}
                      alt="YolPilot web panel ekranları"
                      fill
                      sizes="(min-width: 1280px) 860px, (min-width: 1024px) 760px, (min-width: 640px) 620px, 380px"
                      className="object-cover object-top"
                    />
                  </motion.div>
                </div>
              </div>

              <div className="mx-auto h-6 w-[34%] max-w-[280px] rounded-b-[1.6rem] bg-slate-700" />
              <div className="mx-auto mt-1.5 h-2 w-[52%] max-w-[420px] rounded-full bg-slate-800" />

              <div className="absolute -right-3 bottom-14 rounded-full bg-[color:var(--brand)] px-4 py-2 text-xs font-semibold text-white shadow-lg shadow-blue-900/20">
                Web operasyon paneli
              </div>
            </div>
          </div>
        </motion.div>
      </div>
    </section>
  );
};

export default HeroSection;
