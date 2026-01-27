'use client';

import React, { useEffect, useState } from 'react';
import Link from 'next/link';
import Image from 'next/image';
import { motion } from 'framer-motion';

const HeroSection: React.FC = () => {
  const [currentMobileScreen, setCurrentMobileScreen] = useState(0);
  const [currentWebScreen, setCurrentWebScreen] = useState(0);

  const mobileScreenshots = [
    'journeys.jpeg',
    'create-route.jpeg',
    'performance.jpeg',
    'complete-stop.JPG',
    'performance-2.jpeg'
  ];

  const webScreenshots = [
    'web-dashboard.png',
    'Web-createroute.png',
    'web-reports.png'
  ];

  useEffect(() => {
    const interval = setInterval(() => {
      setCurrentMobileScreen((prev) => (prev + 1) % mobileScreenshots.length);
      setCurrentWebScreen((prev) => (prev + 1) % webScreenshots.length);
    }, 4000);
    return () => clearInterval(interval);
  }, [mobileScreenshots.length, webScreenshots.length]);

  return (
    <section className="relative bg-gradient-to-br from-blue-50 via-white to-blue-50 pt-24 pb-16 lg:pt-32 lg:pb-24 overflow-hidden">
      <div className="absolute inset-0 overflow-hidden">
        <svg className="absolute top-0 left-0 w-full h-full opacity-8" viewBox="0 0 1200 800">
          <path d="M100,200 Q300,150 500,200 T900,180" fill="none" stroke="#3b82f6" strokeWidth="2" opacity="0.4" />
          <path d="M200,400 Q400,350 600,400 T1000,380" fill="none" stroke="#10b981" strokeWidth="1.5" opacity="0.3" />
          <path d="M50,600 Q250,550 450,600 T850,580" fill="none" stroke="#f59e0b" strokeWidth="1.5" opacity="0.25" />
          <circle cx="100" cy="200" r="4" fill="#3b82f6" opacity="0.5" />
          <circle cx="500" cy="200" r="4" fill="#3b82f6" opacity="0.5" />
          <circle cx="900" cy="180" r="4" fill="#3b82f6" opacity="0.5" />
          <circle cx="200" cy="400" r="3" fill="#10b981" opacity="0.4" />
          <circle cx="600" cy="400" r="3" fill="#10b981" opacity="0.4" />
          <circle cx="1000" cy="380" r="3" fill="#10b981" opacity="0.4" />
        </svg>
      </div>

      <div className="relative max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <div className="grid lg:grid-cols-2 gap-12 items-center">
          <motion.div
            initial={{ opacity: 0, x: -50 }}
            animate={{ opacity: 1, x: 0 }}
            transition={{ duration: 0.8 }}
            className="text-center lg:text-left"
          >
            <motion.h1
              initial={{ opacity: 0, y: 20 }}
              animate={{ opacity: 1, y: 0 }}
              transition={{ duration: 0.8, delay: 0.2 }}
              className="text-4xl sm:text-5xl lg:text-6xl font-bold text-gray-900 leading-tight mb-6"
            >
              Teslimatlarinizi <span className="text-blue-600">hizlandirin</span>, maliyetlerinizi{' '}
              <span className="text-green-600">azaltin</span>
            </motion.h1>

            <motion.p
              initial={{ opacity: 0, y: 20 }}
              animate={{ opacity: 1, y: 0 }}
              transition={{ duration: 0.8, delay: 0.4 }}
              className="text-xl text-gray-600 mb-6 max-w-2xl mx-auto lg:mx-0"
            >
              Akilli rota planlama ile seferleri hizli hazirlayin, teslimatlari takip edin ve
              operasyonu tek ekranda yonetin.
            </motion.p>

            <motion.div
              initial={{ opacity: 0, y: 20 }}
              animate={{ opacity: 1, y: 0 }}
              transition={{ duration: 0.8, delay: 0.5 }}
              className="flex flex-wrap gap-4 justify-center lg:justify-start mb-8"
            >
              <div className="flex items-center gap-2 bg-green-50 px-3 py-2 rounded-full">
                <svg className="w-4 h-4 text-green-600" fill="currentColor" viewBox="0 0 20 20">
                  <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clipRule="evenodd" />
                </svg>
                <span className="text-sm font-medium text-green-800">Yerel harita verileriyle planlama</span>
              </div>
              <div className="flex items-center gap-2 bg-blue-50 px-3 py-2 rounded-full">
                <svg className="w-4 h-4 text-blue-600" fill="currentColor" viewBox="0 0 20 20">
                  <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clipRule="evenodd" />
                </svg>
                <span className="text-sm font-medium text-blue-800">Offline calisma modu</span>
              </div>
              <div className="flex items-center gap-2 bg-orange-50 px-3 py-2 rounded-full">
                <svg className="w-4 h-4 text-orange-600" fill="currentColor" viewBox="0 0 20 20">
                  <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clipRule="evenodd" />
                </svg>
                <span className="text-sm font-medium text-orange-800">Turkce destek ekibi</span>
              </div>
            </motion.div>

            <motion.div
              initial={{ opacity: 0, y: 20 }}
              animate={{ opacity: 1, y: 0 }}
              transition={{ duration: 0.8, delay: 0.6 }}
              className="flex flex-col sm:flex-row gap-4 justify-center lg:justify-start mb-12"
            >
              <Link
                href="/contact"
                className="bg-blue-600 hover:bg-blue-700 text-white px-8 py-4 rounded-lg font-semibold text-lg transition-all duration-200 shadow-lg hover:shadow-xl transform hover:-translate-y-1"
              >
                Demo Talep Edin
              </Link>
              <Link
                href="/features"
                className="bg-white hover:bg-gray-50 text-blue-600 border-2 border-blue-600 px-8 py-4 rounded-lg font-semibold text-lg transition-all duration-200 shadow-lg hover:shadow-xl"
              >
                Tum Ozellikleri Gorun
              </Link>
            </motion.div>

            <motion.div
              initial={{ opacity: 0, y: 20 }}
              animate={{ opacity: 1, y: 0 }}
              transition={{ duration: 0.8, delay: 0.8 }}
              className="flex flex-col sm:flex-row items-center justify-center lg:justify-start gap-6 text-sm text-gray-500"
            >
              <div className="flex items-center gap-2">
                <svg className="w-5 h-5 text-green-500" fill="currentColor" viewBox="0 0 20 20">
                  <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clipRule="evenodd" />
                </svg>
                <span>Demo planlama</span>
              </div>
              <div className="flex items-center gap-2">
                <svg className="w-5 h-5 text-green-500" fill="currentColor" viewBox="0 0 20 20">
                  <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clipRule="evenodd" />
                </svg>
                <span>Kurulumda rehberlik</span>
              </div>
              <div className="flex items-center gap-2">
                <svg className="w-5 h-5 text-green-500" fill="currentColor" viewBox="0 0 20 20">
                  <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clipRule="evenodd" />
                </svg>
                <span>Turkce destek</span>
              </div>
            </motion.div>
          </motion.div>

          <motion.div
            initial={{ opacity: 0, x: 50 }}
            animate={{ opacity: 1, x: 0 }}
            transition={{ duration: 0.8, delay: 0.4 }}
            className="relative"
          >
            <div className="flex flex-col lg:flex-row items-center gap-8 lg:gap-8">
              <div className="relative order-1 lg:order-1">
                <div className="relative w-48 sm:w-56 lg:w-64 h-[400px] sm:h-[450px] lg:h-[500px] bg-black rounded-[2.5rem] p-2 shadow-2xl">
                  <div className="w-full h-full bg-gray-900 rounded-[2rem] overflow-hidden relative">
                    <motion.div
                      key={currentMobileScreen}
                      initial={{ opacity: 0, x: 20 }}
                      animate={{ opacity: 1, x: 0 }}
                      exit={{ opacity: 0, x: -20 }}
                      transition={{ duration: 1.2, ease: 'easeInOut' }}
                      className="relative w-full h-full"
                    >
                      <Image
                        src={`/screenshots/${mobileScreenshots[currentMobileScreen]}`}
                        alt="YolPilot Mobile App"
                        fill
                        sizes="(min-width: 1024px) 256px, (min-width: 640px) 224px, 192px"
                        className="object-cover object-top"
                      />
                    </motion.div>
                    <div className="absolute top-0 left-1/2 transform -translate-x-1/2 w-24 h-5 bg-black rounded-b-xl"></div>
                  </div>
                </div>
              </div>

              <div className="relative order-2 lg:order-2">
                <div className="relative w-[300px] sm:w-[400px] lg:w-[500px] h-[180px] sm:h-[250px] lg:h-[320px] bg-gray-900 rounded-t-2xl shadow-2xl overflow-hidden border-2 border-gray-700">
                  <motion.div
                    key={currentWebScreen}
                    initial={{ opacity: 0, x: 30 }}
                    animate={{ opacity: 1, x: 0 }}
                    exit={{ opacity: 0, x: -30 }}
                    transition={{ duration: 1.2, ease: 'easeInOut' }}
                    className="relative w-full h-full bg-white"
                  >
                    <Image
                      src={`/screenshots/${webScreenshots[currentWebScreen]}`}
                      alt="YolPilot Web Panel"
                      fill
                      sizes="(min-width: 1024px) 500px, (min-width: 640px) 400px, 300px"
                      className="object-fill"
                    />
                  </motion.div>
                </div>

                <div className="w-[320px] sm:w-[420px] lg:w-[520px] h-6 bg-gray-700 rounded-b-2xl mx-auto relative">
                  <div className="absolute top-1 left-1/2 transform -translate-x-1/2 w-12 sm:w-14 lg:w-16 h-3 bg-gray-600 rounded"></div>
                </div>

                <div className="w-[340px] sm:w-[440px] lg:w-[540px] h-2 bg-gray-800 rounded-full mx-auto mt-1"></div>
              </div>
            </div>

            <motion.div
              animate={{ y: [-10, 10, -10] }}
              transition={{ duration: 4, repeat: Infinity }}
              className="absolute -top-6 -left-4 bg-green-500 text-white px-4 py-2 rounded-full text-sm font-semibold shadow-lg z-10"
            >
              Mobil Uygulama
            </motion.div>
            <motion.div
              animate={{ y: [10, -10, 10] }}
              transition={{ duration: 3, repeat: Infinity, delay: 1 }}
              className="absolute -bottom-6 -right-4 bg-blue-500 text-white px-4 py-2 rounded-full text-sm font-semibold shadow-lg z-10"
            >
              Web Panel
            </motion.div>
          </motion.div>
        </div>
      </div>
    </section>
  );
};

export default HeroSection;
