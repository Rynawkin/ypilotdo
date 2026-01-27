'use client';

import React, { useState } from 'react';
import Image from 'next/image';
import Link from 'next/link';
import { motion } from 'framer-motion';

const Header: React.FC = () => {
  const [isMobileMenuOpen, setIsMobileMenuOpen] = useState(false);
  const [openSubmenu, setOpenSubmenu] = useState<string | null>(null);

  const navItems = [
    { name: 'Ana Sayfa', href: '/' },
    { name: 'Ozellikler', href: '/features' },
    {
      name: 'Cozumler',
      submenu: [
        { name: 'Kullanim Senaryolari', href: '/use-cases' },
        { name: 'Sektorler', href: '/industries/ecommerce' },
        { name: 'Entegrasyonlar', href: '/integrations' },
        { name: 'Karsilastirma', href: '/comparison' }
      ]
    },
    { name: 'Musteriler', href: '/customers' },
    { name: 'Kaynaklar', href: '/resources' },
    { name: 'Hakkimizda', href: '/about' },
    { name: 'Iletisim', href: '/contact' }
  ];

  return (
    <header className="fixed top-0 left-0 right-0 z-50 bg-white/95 backdrop-blur-sm border-b border-gray-100">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <div className="flex justify-between items-center h-16">
          <Link href="/" className="flex items-center">
            <div className="w-16 h-16 relative">
              <Image
                src="/yolpilot-logo.png"
                alt="YolPilot Logo"
                width={64}
                height={64}
                className="object-contain"
              />
            </div>
          </Link>

          <nav className="hidden md:flex space-x-8">
            {navItems.map((item) =>
              'submenu' in item ? (
                <div
                  key={item.name}
                  className="relative group"
                  onMouseEnter={() => setOpenSubmenu(item.name)}
                  onMouseLeave={() => setOpenSubmenu(null)}
                >
                  <button className="text-gray-600 hover:text-blue-600 transition-colors duration-200 font-medium flex items-center gap-1">
                    {item.name}
                    <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 9l-7 7-7-7" />
                    </svg>
                  </button>
                  {openSubmenu === item.name && item.submenu && (
                    <div className="absolute top-full left-0 pt-2 w-56 z-50">
                      <div className="bg-white rounded-xl shadow-lg border border-gray-100 py-2">
                        {item.submenu.map((subItem) => (
                          <Link
                            key={subItem.name}
                            href={subItem.href}
                            className="block px-4 py-2 text-gray-600 hover:text-blue-600 hover:bg-gray-50 transition-colors duration-200"
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
                  className="text-gray-600 hover:text-blue-600 transition-colors duration-200 font-medium"
                >
                  {item.name}
                </Link>
              )
            )}
          </nav>

          <div className="hidden md:flex items-center space-x-4">
            <Link
              href="/contact"
              className="bg-blue-600 hover:bg-blue-700 text-white px-6 py-2 rounded-lg font-medium transition-all duration-200 shadow-sm hover:shadow-md"
            >
              Demo Talep Edin
            </Link>
          </div>

          <button
            onClick={() => setIsMobileMenuOpen(!isMobileMenuOpen)}
            className="md:hidden p-2 rounded-lg hover:bg-gray-100 transition-colors duration-200"
          >
            <svg className="w-6 h-6 text-gray-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
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
            className="md:hidden py-4 border-t border-gray-100"
          >
            <nav className="flex flex-col space-y-4">
              {navItems.map((item) =>
                'submenu' in item ? (
                  <div key={item.name}>
                    <button
                      onClick={() => setOpenSubmenu(openSubmenu === item.name ? null : item.name)}
                      className="text-gray-600 hover:text-blue-600 transition-colors duration-200 font-medium py-2 flex items-center justify-between w-full"
                    >
                      {item.name}
                      <svg
                        className={`w-4 h-4 transition-transform ${openSubmenu === item.name ? 'rotate-180' : ''}`}
                        fill="none"
                        stroke="currentColor"
                        viewBox="0 0 24 24"
                      >
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 9l-7 7-7-7" />
                      </svg>
                    </button>
                    {openSubmenu === item.name && item.submenu && (
                      <div className="pl-4 pt-2 space-y-2">
                        {item.submenu.map((subItem) => (
                          <Link
                            key={subItem.name}
                            href={subItem.href}
                            className="block text-gray-600 hover:text-blue-600 transition-colors duration-200 py-2"
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
                    className="text-gray-600 hover:text-blue-600 transition-colors duration-200 font-medium py-2"
                    onClick={() => setIsMobileMenuOpen(false)}
                  >
                    {item.name}
                  </Link>
                )
              )}
              <div className="pt-4 border-t border-gray-100 space-y-3">
                <Link
                  href="/contact"
                  className="block bg-blue-600 hover:bg-blue-700 text-white px-6 py-3 rounded-lg font-medium transition-all duration-200 text-center"
                  onClick={() => setIsMobileMenuOpen(false)}
                >
                  Demo Talep Edin
                </Link>
              </div>
            </nav>
          </motion.div>
        )}
      </div>
    </header>
  );
};

export default Header;
