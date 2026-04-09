'use client';

import React from 'react';
import { motion } from 'framer-motion';
import TrackedLink from '@/components/ui/TrackedLink';
import { salesFaqItems } from '@/lib/faq';

export default function FaqPreview() {
  const items = salesFaqItems.slice(0, 4);

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
              Reklamdan gelen sorular
            </div>
            <h2 className="text-3xl font-bold tracking-tight text-slate-950 lg:text-5xl">
              Ziyaretçilerin ilk baktığı sorular için hazır cevaplar.
            </h2>
            <p className="mt-5 text-lg leading-8 text-slate-600">
              Fiyat, kurulum süresi, otomatik yenileme, mobil uygulama ve entegrasyon gibi karar anında sorulan
              başlıkları açık şekilde cevaplıyoruz.
            </p>
          </div>
          <TrackedLink
            href="/faq"
            trackingName="homepage-faq-detaylari"
            className="inline-flex items-center gap-2 text-sm font-semibold text-[color:var(--brand)] transition-colors hover:text-blue-700"
          >
            Tüm soruları görün
          </TrackedLink>
        </motion.div>

        <div className="mt-12 grid gap-6 lg:grid-cols-2">
          {items.map((item, index) => (
            <motion.article
              key={item.question}
              initial={{ opacity: 0, y: 24 }}
              whileInView={{ opacity: 1, y: 0 }}
              transition={{ duration: 0.7, delay: index * 0.08 }}
              viewport={{ once: true }}
              className="rounded-[1.8rem] border border-slate-200 bg-white p-7 shadow-sm shadow-slate-900/5"
            >
              <h3 className="text-xl font-semibold text-slate-950">{item.question}</h3>
              <p className="mt-4 text-base leading-7 text-slate-600">{item.answer}</p>
            </motion.article>
          ))}
        </div>
      </div>
    </section>
  );
}
