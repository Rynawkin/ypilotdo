'use client';

import React from 'react';
import Image from 'next/image';
import Link from 'next/link';

const Footer: React.FC = () => {
  const footerSections = [
    {
      title: 'Urun',
      links: [
        { name: 'Ozellikler', href: '/features' },
        { name: 'Kullanim Senaryolari', href: '/use-cases' },
        { name: 'Karsilastirma', href: '/comparison' },
        { name: 'ROI Hesaplayici', href: '/roi-calculator' }
      ]
    },
    {
      title: 'Cozumler',
      links: [
        { name: 'E-ticaret', href: '/industries/ecommerce' },
        { name: 'Lojistik', href: '/industries/logistics' },
        { name: 'Perakende', href: '/industries/retail' },
        { name: 'Yemek Servisi', href: '/industries/food-delivery' },
        { name: 'Saha Hizmetleri', href: '/industries/services' },
        { name: 'Soguk Zincir', href: '/industries/cold-chain' },
        { name: 'Entegrasyonlar', href: '/integrations' }
      ]
    },
    {
      title: 'Sirket',
      links: [
        { name: 'Hakkimizda', href: '/about' },
        { name: 'Musteriler', href: '/customers' },
        { name: 'Kariyer', href: '/careers' },
        { name: 'Blog', href: '/blog' },
        { name: 'Iletisim', href: '/contact' }
      ]
    },
    {
      title: 'Destek',
      links: [
        { name: 'Dokumantasyon', href: '/docs' },
        { name: 'API Dokumanlari', href: '/docs/api' },
        { name: 'Guvenlik', href: '/security' },
        { name: 'KVKK', href: '/docs/kvkk' },
        { name: 'Gizlilik Politikasi', href: '/privacy' },
        { name: 'Kullanim Kosullari', href: '/terms' },
        { name: 'Durum Sayfasi', href: '/status' }
      ]
    }
  ];

  const trustItems = [
    'Veri guvenligi odakli',
    'Guvenli iletisim',
    'Yedekli altyapi',
    'Turkce destek'
  ];

  return (
    <footer className="bg-gray-900 text-gray-300">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-12 lg:py-16">
        <div className="grid lg:grid-cols-6 gap-8">
          <div className="lg:col-span-2">
            <Link href="/" className="flex items-center mb-4">
              <div className="w-16 h-16 relative">
                <Image
                  src="/yolpilot-logo.png"
                  alt="YolPilot Logo"
                  width={64}
                  height={64}
                  className="object-contain brightness-0 invert"
                />
              </div>
            </Link>

            <p className="text-gray-400 mb-6 max-w-md">
              Teslimat ve rota optimizasyonu icin tek bir platform. Operasyonlarinizi takip edin,
              surecleri sadelestirin.
            </p>

            <div className="space-y-3 mb-6">
              <div className="flex items-center gap-2 text-sm">
                <svg className="w-5 h-5 text-blue-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z" />
                </svg>
                <a href="mailto:info@yolpilot.com" className="hover:text-white">info@yolpilot.com</a>
              </div>
              <div className="flex items-center gap-2 text-sm">
                <svg className="w-5 h-5 text-green-400" fill="currentColor" viewBox="0 0 24 24">
                  <path d="M17.472 14.382c-.297-.149-1.758-.867-2.03-.967-.273-.099-.471-.148-.67.15-.197.297-.767.966-.94 1.164-.173.199-.347.223-.644.075-.297-.15-1.255-.463-2.39-1.475-.883-.788-1.48-1.761-1.653-2.059-.173-.297-.018-.458.13-.606.134-.133.298-.347.446-.52.149-.174.198-.298.298-.497.099-.198.05-.371-.025-.52-.075-.149-.669-1.612-.916-2.207-.242-.579-.487-.5-.669-.51-.173-.008-.371-.01-.57-.01-.198 0-.52.074-.792.372-.272.297-1.04 1.016-1.04 2.479 0 1.462 1.065 2.875 1.213 3.074.149.198 2.096 3.2 5.077 4.487.709.306 1.262.489 1.694.625.712.227 1.36.195 1.871.118.571-.085 1.758-.719 2.006-1.413.248-.694.248-1.289.173-1.413-.074-.124-.272-.198-.57-.347m-5.421 7.403h-.004a9.87 9.87 0 01-5.031-1.378l-.361-.214-3.741.982.998-3.648-.235-.374a9.86 9.86 0 01-1.51-5.26c.001-5.45 4.436-9.884 9.888-9.884 2.64 0 5.122 1.03 6.988 2.898a9.825 9.825 0 012.893 6.994c-.003 5.45-4.437 9.884-9.885 9.884m8.413-18.297A11.815 11.815 0 0012.05 0C5.495 0 .16 5.335.157 11.892c0 2.096.547 4.142 1.588 5.945L.057 24l6.305-1.654a11.882 11.882 0 005.683 1.448h.005c6.554 0 11.890-5.335 11.893-11.893A11.821 11.821 0 0020.885 3.251" />
                </svg>
                <a href="tel:+908507566267" className="hover:text-white">0850 756 62 67</a>
              </div>
              <div className="flex items-start gap-2 text-sm">
                <svg className="w-5 h-5 text-purple-400 mt-0.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M17.657 16.657L13.414 20.9a1.998 1.998 0 01-2.827 0l-4.244-4.243a8 8 0 1111.314 0z" />
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 11a3 3 0 11-6 0 3 3 0 016 0z" />
                </svg>
                <span className="text-gray-400">Istanbul, Turkiye</span>
              </div>
            </div>
          </div>

          {footerSections.map((section) => (
            <div key={section.title}>
              <h3 className="text-white font-semibold mb-4">{section.title}</h3>
              <ul className="space-y-2">
                {section.links.map((link) => (
                  <li key={link.name}>
                    <Link href={link.href} className="text-gray-400 hover:text-white transition-colors duration-200">
                      {link.name}
                    </Link>
                  </li>
                ))}
              </ul>
            </div>
          ))}
        </div>
      </div>

      <div className="border-t border-gray-800 bg-gray-800/50">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
          <div className="flex flex-wrap justify-center gap-4">
            {trustItems.map((item) => (
              <div key={item} className="flex items-center gap-2 bg-gray-700/50 px-4 py-2 rounded-full">
                <span className="text-sm font-semibold text-gray-300">{item}</span>
              </div>
            ))}
          </div>
        </div>
      </div>

      <div className="border-t border-gray-800">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-6">
          <div className="flex flex-col lg:flex-row justify-between items-center gap-4">
            <div className="text-gray-400 text-sm">(c) 2024 YolPilot. Tum haklari saklidir.</div>

            <div className="flex flex-wrap justify-center gap-4 lg:gap-6 text-sm">
              <Link href="/privacy" className="text-gray-400 hover:text-white transition-colors duration-200">
                Gizlilik Politikasi
              </Link>
              <Link href="/terms" className="text-gray-400 hover:text-white transition-colors duration-200">
                Kullanim Kosullari
              </Link>
              <Link href="/cookies" className="text-gray-400 hover:text-white transition-colors duration-200">
                Cerez Politikasi
              </Link>
              <Link href="/support" className="text-gray-400 hover:text-white transition-colors duration-200">
                Destek
              </Link>
            </div>
          </div>
        </div>
      </div>
    </footer>
  );
};

export default Footer;
