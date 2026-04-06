'use client';

import React from 'react';
import Image from 'next/image';
import Link from 'next/link';

const Footer: React.FC = () => {
  const footerSections = [
    {
      title: 'Platform',
      links: [
        { name: 'Özellikler', href: '/features' },
        { name: 'Kullanım Senaryoları', href: '/use-cases' },
        { name: 'ROI Hesaplayıcı', href: '/roi-calculator' }
      ]
    },
    {
      title: 'Sektörler',
      links: [
        { name: 'E-ticaret', href: '/industries/ecommerce' },
        { name: 'Lojistik', href: '/industries/logistics' },
        { name: 'Perakende', href: '/industries/retail' }
      ]
    },
    {
      title: 'Şirket',
      links: [
        { name: 'Hakkımızda', href: '/about' },
        { name: 'Müşteriler', href: '/customers' },
        { name: 'İletişim', href: '/contact' }
      ]
    },
    {
      title: 'Destek',
      links: [
        { name: 'Güvenlik', href: '/security' },
        { name: 'Gizlilik Politikası', href: '/privacy' },
        { name: 'Kullanım Koşulları', href: '/terms' }
      ]
    }
  ];

  const trustItems = ['Web panel + mobil uygulama', 'Kurulumda rehberlik', 'Teslimat kanıtı akışı', 'Türkçe destek'];

  return (
    <footer className="border-t border-[color:var(--line)] bg-[#0f1725] text-slate-300">
      <div className="mx-auto max-w-7xl px-4 py-14 sm:px-6 lg:px-8">
        <div className="grid gap-10 lg:grid-cols-6">
          <div className="lg:col-span-2">
            <Link href="/" className="mb-5 flex items-center gap-3">
              <div className="relative h-14 w-14">
                <Image
                  src="/yolpilot-logo.png"
                  alt="YolPilot Logo"
                  width={56}
                  height={56}
                  className="object-contain brightness-0 invert"
                />
              </div>
              <div>
                <div className="text-sm font-semibold uppercase tracking-[0.2em] text-slate-400">YolPilot</div>
                <div className="text-sm text-slate-500">Rota optimizasyonu ve teslimat operasyonu</div>
              </div>
            </Link>

            <p className="max-w-md text-sm leading-7 text-slate-400">
              Dağıtım ekiplerinin planlama, saha yönetimi ve teslimat kanıtı ihtiyacını tek panelde toplayan
              operasyon yazılımı.
            </p>

            <div className="mt-6 space-y-3 text-sm text-slate-400">
              <div>
                E-posta:{' '}
                <a href="mailto:info@yolpilot.com" className="font-medium text-slate-200 transition-colors hover:text-white">
                  info@yolpilot.com
                </a>
              </div>
              <div>
                Telefon:{' '}
                <a href="tel:+908507566267" className="font-medium text-slate-200 transition-colors hover:text-white">
                  0850 756 62 67
                </a>
              </div>
              <div>İstanbul, Türkiye</div>
            </div>
          </div>

          {footerSections.map((section) => (
            <div key={section.title}>
              <h3 className="mb-4 text-sm font-semibold uppercase tracking-[0.18em] text-slate-200">{section.title}</h3>
              <ul className="space-y-3">
                {section.links.map((link) => (
                  <li key={link.name}>
                    <Link href={link.href} className="text-sm text-slate-400 transition-colors duration-200 hover:text-white">
                      {link.name}
                    </Link>
                  </li>
                ))}
              </ul>
            </div>
          ))}
        </div>

        <div className="mt-10 flex flex-wrap gap-3 border-t border-white/8 pt-8">
          {trustItems.map((item) => (
            <div key={item} className="rounded-full border border-white/10 bg-white/5 px-4 py-2 text-sm text-slate-300">
              {item}
            </div>
          ))}
        </div>
      </div>

      <div className="border-t border-white/8 bg-black/10">
        <div className="mx-auto flex max-w-7xl flex-col gap-3 px-4 py-5 text-sm text-slate-500 sm:px-6 lg:flex-row lg:items-center lg:justify-between lg:px-8">
          <div>(c) 2026 YolPilot. Tüm hakları saklıdır.</div>
          <div className="flex flex-wrap gap-5">
            <Link href="/privacy" className="transition-colors hover:text-white">
              Gizlilik Politikası
            </Link>
            <Link href="/terms" className="transition-colors hover:text-white">
              Kullanım Koşulları
            </Link>
            <Link href="/cookies" className="transition-colors hover:text-white">
              Çerez Politikası
            </Link>
          </div>
        </div>
      </div>
    </footer>
  );
};

export default Footer;
