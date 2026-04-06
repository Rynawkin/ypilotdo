'use client';

import React, { useState } from 'react';
import Image from 'next/image';
import Link from 'next/link';
import { motion } from 'framer-motion';
import TrackedLink from '@/components/ui/TrackedLink';
import { PRIMARY_CTA_HREF, SECONDARY_CTA_HREF } from '@/lib/marketing';

const Header: React.FC = () => {
  const [isMobileMenuOpen, setIsMobileMenuOpen] = useState(false);
  const [openSubmenu, setOpenSubmenu] = useState<string | null>(null);

  const navItems = [
    { name: 'Özellikler', href: '/features' },
    {
      name: 'Çözümler',
      submenu: [
        { name: 'Kullanım Senaryoları', href: '/use-cases' },
        { name: 'E-ticaret', href: '/industries/ecommerce' },
        { name: 'Lojistik', href: '/industries/logistics' },
        { name: 'Entegrasyonlar', href: '/integrations' }
      ]
    },
    { name: 'Güvenlik', href: '/security' },
    { name: 'ROI', href: '/roi-calculator' },
    { name: 'İletişim', href: '/contact' }
  ];

  return (
    <header className="fixed top-0 left-0 right-0 z-50 border-b border-[color:var(--line)] bg-white/88 backdrop-blur-xl">
      <div className="mx-auto flex max-w-7xl items-center justify-between gap-6 px-4 py-2 sm:px-6 lg:px-8">
        <Link href="/" className="flex items-center gap-3">
          <div className="relative h-14 w-14">
            <Image src="/yolpilot-logo.png" alt="YolPilot Logo" width={56} height={56} className="object-contain" />
          </div>
          <div className="hidden sm:block">
            <div className="text-sm font-semibold uppercase tracking-[0.18em] text-[color:var(--ink-soft)]">YolPilot</div>
            <div className="text-sm text-slate-600">Rota optimizasyonu ve saha operasyonu</div>
          </div>
        </Link>

        <nav className="hidden items-center gap-8 md:flex">
          {navItems.map((item) =>
            'submenu' in item ? (
              <div
                key={item.name}
                className="relative"
                onMouseEnter={() => setOpenSubmenu(item.name)}
                onMouseLeave={() => setOpenSubmenu(null)}
              >
                <button className="flex items-center gap-1 text-sm font-semibold text-slate-600 transition-colors duration-200 hover:text-[color:var(--brand)]">
                  {item.name}
                  <svg className="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 9l-7 7-7-7" />
                  </svg>
                </button>
                {openSubmenu === item.name && item.submenu && (
                  <div className="absolute top-full left-0 z-50 w-56 pt-3">
                    <div className="rounded-2xl border border-[color:var(--line)] bg-white p-2 shadow-xl shadow-slate-900/10">
                      {item.submenu.map((subItem) => (
                        <Link
                          key={subItem.name}
                          href={subItem.href}
                          className="block rounded-xl px-4 py-3 text-sm text-slate-600 transition-colors duration-200 hover:bg-slate-50 hover:text-[color:var(--brand)]"
                        >
                          {subItem.name}
                        </Link>
                      ))}
                    </div>
                  </div>
                )}
              </div>
            ) : (
              <Link
                key={item.name}
                href={item.href}
                className="text-sm font-semibold text-slate-600 transition-colors duration-200 hover:text-[color:var(--brand)]"
              >
                {item.name}
              </Link>
            )
          )}
        </nav>

        <div className="hidden items-center gap-3 md:flex">
          <TrackedLink
            href={SECONDARY_CTA_HREF}
            trackingName="header-platformu-inceleyin"
            className="rounded-full border border-slate-200 px-4 py-2 text-sm font-semibold text-slate-700 transition-colors duration-200 hover:border-slate-300 hover:bg-slate-50"
          >
            Platformu İnceleyin
          </TrackedLink>
          <TrackedLink
            href={PRIMARY_CTA_HREF}
            trackingName="header-demo-talep-edin"
            className="rounded-full bg-[color:var(--brand)] px-5 py-2.5 text-sm font-semibold !text-white shadow-lg shadow-blue-900/15 transition-all duration-200 hover:bg-blue-700"
          >
            Demo Talep Edin
          </TrackedLink>
        </div>

        <button
          onClick={() => setIsMobileMenuOpen(!isMobileMenuOpen)}
          className="rounded-xl border border-slate-200 p-2 transition-colors duration-200 hover:bg-slate-50 md:hidden"
        >
          <svg className="h-6 w-6 text-slate-700" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            {isMobileMenuOpen ? (
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
            ) : (
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 6h16M4 12h16M4 18h16" />
            )}
          </svg>
        </button>
      </div>

      {isMobileMenuOpen && (
        <motion.div
          initial={{ opacity: 0, height: 0 }}
          animate={{ opacity: 1, height: 'auto' }}
          exit={{ opacity: 0, height: 0 }}
          className="border-t border-[color:var(--line)] bg-white px-4 py-4 md:hidden"
        >
          <nav className="flex flex-col gap-4">
            {navItems.map((item) =>
              'submenu' in item ? (
                <div key={item.name}>
                  <button
                    onClick={() => setOpenSubmenu(openSubmenu === item.name ? null : item.name)}
                    className="flex w-full items-center justify-between py-2 text-sm font-semibold text-slate-700 transition-colors duration-200 hover:text-[color:var(--brand)]"
                  >
                    {item.name}
                    <svg
                      className={`h-4 w-4 transition-transform ${openSubmenu === item.name ? 'rotate-180' : ''}`}
                      fill="none"
                      stroke="currentColor"
                      viewBox="0 0 24 24"
                    >
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 9l-7 7-7-7" />
                    </svg>
                  </button>
                  {openSubmenu === item.name && item.submenu && (
                    <div className="space-y-2 pl-4 pt-2">
                      {item.submenu.map((subItem) => (
                        <Link
                          key={subItem.name}
                          href={subItem.href}
                          className="block py-2 text-sm text-slate-600 transition-colors duration-200 hover:text-[color:var(--brand)]"
                          onClick={() => {
                            setIsMobileMenuOpen(false);
                            setOpenSubmenu(null);
                          }}
                        >
                          {subItem.name}
                        </Link>
                      ))}
                    </div>
                  )}
                </div>
              ) : (
                <Link
                  key={item.name}
                  href={item.href}
                  className="py-2 text-sm font-semibold text-slate-700 transition-colors duration-200 hover:text-[color:var(--brand)]"
                  onClick={() => setIsMobileMenuOpen(false)}
                >
                  {item.name}
                </Link>
              )
            )}
            <div className="space-y-3 border-t border-[color:var(--line)] pt-4">
              <TrackedLink
                href={SECONDARY_CTA_HREF}
                trackingName="mobile-header-platformu-inceleyin"
                className="block rounded-full border border-slate-200 px-6 py-3 text-center text-sm font-semibold text-slate-700 transition-colors duration-200 hover:bg-slate-50"
                onClick={() => setIsMobileMenuOpen(false)}
              >
                Platformu İnceleyin
              </TrackedLink>
              <TrackedLink
                href={PRIMARY_CTA_HREF}
                trackingName="mobile-header-demo-talep-edin"
                className="block rounded-full bg-[color:var(--brand)] px-6 py-3 text-center text-sm font-semibold !text-white transition-all duration-200 hover:bg-blue-700"
                onClick={() => setIsMobileMenuOpen(false)}
              >
                Demo Talep Edin
              </TrackedLink>
            </div>
          </nav>
        </motion.div>
      )}
    </header>
  );
};

export default Header;
