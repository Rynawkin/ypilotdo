'use client';

import React from 'react';
import { motion } from 'framer-motion';
import TrackedLink from '@/components/ui/TrackedLink';
import { formatTry, pricingPlans } from '@/lib/pricing';
import { PRIMARY_CTA_HREF } from '@/lib/marketing';

export default function PricingPreview() {
  return (
    <section className="border-b border-[color:var(--line)] bg-white py-16 lg:py-24">
      <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
        <motion.div
          initial={{ opacity: 0, y: 24 }}
          whileInView={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.7 }}
          viewport={{ once: true }}
          className="max-w-3xl"
        >
          <div className="mb-5 inline-flex rounded-full border border-slate-200 bg-slate-50 px-4 py-2 text-sm font-semibold text-[color:var(--brand-deep)]">
            Fiyatlandırma
          </div>
          <h2 className="text-3xl font-bold tracking-tight text-slate-950 lg:text-5xl">
            Operasyon büyüklüğüne göre yön veren fiyatlar.
          </h2>
          <p className="mt-5 text-lg leading-8 text-slate-600">
            Fiyatlarımız aylık plan bazındadır. Ücretli planlarda kart kaydı zorunludur ve abonelik her ay otomatik
            yenilenir. Ek durak ve bildirim ücretleri yalnızca dahil kullanımı aşan miktar için işler.
          </p>
        </motion.div>

        <div className="mt-12 grid gap-6 xl:grid-cols-4">
          {pricingPlans.map((plan, index) => (
            <motion.article
              key={plan.id}
              initial={{ opacity: 0, y: 24 }}
              whileInView={{ opacity: 1, y: 0 }}
              transition={{ duration: 0.7, delay: index * 0.07 }}
              viewport={{ once: true }}
              className={`rounded-[1.8rem] border p-7 shadow-sm shadow-slate-900/5 ${
                plan.popular ? 'border-blue-300 bg-blue-50/60' : 'border-slate-200 bg-white'
              }`}
            >
              <div className="flex items-center justify-between gap-3">
                <h3 className="text-2xl font-semibold text-slate-950">{plan.name}</h3>
                {plan.popular ? (
                  <span className="rounded-full bg-blue-600 px-3 py-1 text-xs font-semibold text-white">Önerilen</span>
                ) : null}
              </div>
              <p className="mt-3 text-sm leading-6 text-slate-600">{plan.summary}</p>
              <div className="mt-5 text-3xl font-bold text-slate-950">{formatTry(plan.monthlyPrice)}</div>
              <div className="mt-1 text-sm text-slate-500">/ay</div>
              <div className="mt-4 rounded-2xl bg-[color:var(--surface-muted)] px-4 py-3 text-sm font-medium text-slate-700">
                {plan.audience}
              </div>
              <ul className="mt-6 space-y-3 text-sm text-slate-700">
                {plan.features.map((feature) => (
                  <li key={feature} className="flex items-start gap-3">
                    <span className="mt-1.5 h-2 w-2 rounded-full bg-[color:var(--brand)]" />
                    <span>{feature}</span>
                  </li>
                ))}
              </ul>
              <div className="mt-6 border-t border-slate-200 pt-5">
                <div className="text-xs font-semibold uppercase tracking-[0.16em] text-slate-500">Notlar</div>
                <ul className="mt-3 space-y-2 text-sm text-slate-600">
                  {plan.notes.map((note) => (
                    <li key={note}>{note}</li>
                  ))}
                </ul>
              </div>
            </motion.article>
          ))}
        </div>

        <motion.div
          initial={{ opacity: 0, y: 24 }}
          whileInView={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.7, delay: 0.2 }}
          viewport={{ once: true }}
          className="mt-10 flex flex-col gap-4 rounded-[2rem] bg-[linear-gradient(135deg,#102d64,#153f8a)] px-6 py-8 text-white lg:flex-row lg:items-center lg:justify-between lg:px-8"
        >
          <div className="max-w-3xl">
            <div className="text-xs font-semibold uppercase tracking-[0.2em] text-blue-100">Açık abonelik mantığı</div>
            <p className="mt-3 text-lg leading-8 text-blue-50">
              Ücretli planlarda ilk ödeme sırasında kart kaydedilir. Aylık yenileme otomatik yapılır, tahsilat
              başarısız olursa 3 günlük süre sonunda erişim kapanır.
            </p>
          </div>
          <div className="flex flex-wrap gap-3">
            <TrackedLink
              href="/pricing"
              trackingName="homepage-pricing-detaylari"
              className="rounded-full bg-white px-6 py-3 text-sm font-semibold text-slate-900 transition hover:bg-slate-100"
            >
              Tüm fiyatları inceleyin
            </TrackedLink>
            <TrackedLink
              href={PRIMARY_CTA_HREF}
              trackingName="homepage-pricing-demo-talep"
              className="rounded-full border border-white/20 px-6 py-3 text-sm font-semibold text-white transition hover:bg-white/10"
            >
              Demo talep edin
            </TrackedLink>
          </div>
        </motion.div>
      </div>
    </section>
  );
}
