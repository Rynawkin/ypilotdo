'use client';

import React, { useCallback, useEffect, useState } from 'react';
import { motion } from 'framer-motion';

const ROICalculator: React.FC = () => {
  const [deliveriesPerDay, setDeliveriesPerDay] = useState<number>(50);
  const [vehicleCount, setVehicleCount] = useState<number>(5);
  const [avgDistancePerDelivery, setAvgDistancePerDelivery] = useState<number>(15);
  const [fuelPricePerLiter, setFuelPricePerLiter] = useState<number>(40);
  const [fuelSavingsPercent, setFuelSavingsPercent] = useState<number>(10);

  const [results, setResults] = useState({
    monthlyFuelSavings: 0,
    yearlyFuelSavings: 0
  });

  const [isCalculated, setIsCalculated] = useState(false);

  const calculateROI = useCallback(() => {
    const workingDaysPerMonth = 22;
    const fuelConsumptionPer100Km = 10;

    const monthlyDeliveries = deliveriesPerDay * workingDaysPerMonth;
    const monthlyKilometers = monthlyDeliveries * avgDistancePerDelivery;
    const currentFuelConsumption = (monthlyKilometers * fuelConsumptionPer100Km) / 100;
    const currentFuelCost = currentFuelConsumption * fuelPricePerLiter;

    const fuelSavings = currentFuelCost * (fuelSavingsPercent / 100);
    const yearlySavings = fuelSavings * 12;

    setResults({
      monthlyFuelSavings: Math.round(fuelSavings),
      yearlyFuelSavings: Math.round(yearlySavings)
    });

    setIsCalculated(true);
  }, [deliveriesPerDay, avgDistancePerDelivery, fuelPricePerLiter, fuelSavingsPercent]);

  useEffect(() => {
    calculateROI();
  }, [calculateROI]);

  const formatCurrency = (value: number) => {
    return new Intl.NumberFormat('tr-TR', {
      style: 'currency',
      currency: 'TRY',
      minimumFractionDigits: 0,
      maximumFractionDigits: 0
    }).format(value);
  };

  return (
    <section className="py-16 lg:py-24 bg-gradient-to-br from-blue-900 via-indigo-900 to-purple-900 text-white relative overflow-hidden">
      <div className="absolute inset-0 bg-grid-white/[0.05] bg-[size:30px_30px]"></div>
      <div className="absolute inset-0 overflow-hidden opacity-20">
        <svg className="absolute top-0 left-0 w-full h-full" viewBox="0 0 1200 800">
          <circle cx="100" cy="100" r="200" fill="#60a5fa" />
          <circle cx="1100" cy="700" r="250" fill="#818cf8" />
          <circle cx="600" cy="400" r="150" fill="#a78bfa" />
        </svg>
      </div>

      <div className="relative max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <motion.div
          initial={{ opacity: 0, y: 30 }}
          whileInView={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.8 }}
          viewport={{ once: true }}
          className="text-center mb-12"
        >
          <div className="inline-flex items-center gap-2 bg-white/20 backdrop-blur-sm px-4 py-2 rounded-full mb-6">
            <span className="text-2xl">ROI</span>
            <span className="text-sm font-semibold">ROI Hesaplayici</span>
          </div>

          <h2 className="text-3xl lg:text-5xl font-bold mb-6">Ornek Tasarruf Hesabi</h2>
          <p className="text-xl text-blue-100 max-w-3xl mx-auto">
            Degerleri kendi operasyonunuza gore guncelleyerek ornek bir hesaplama yapin.
          </p>
        </motion.div>

        <div className="grid lg:grid-cols-2 gap-12 items-start">
          <motion.div
            initial={{ opacity: 0, x: -30 }}
            whileInView={{ opacity: 1, x: 0 }}
            transition={{ duration: 0.8, delay: 0.2 }}
            viewport={{ once: true }}
            className="bg-white/10 backdrop-blur-md rounded-3xl p-8 lg:p-10 border border-white/20"
          >
            <h3 className="text-2xl font-bold mb-8">Isletme Bilgileriniz</h3>

            <div className="mb-8">
              <label className="flex justify-between items-center mb-3">
                <span className="text-lg font-semibold">Gunluk Teslimat Sayisi</span>
                <span className="text-2xl font-bold text-blue-300">{deliveriesPerDay}</span>
              </label>
              <input
                type="range"
                min="10"
                max="500"
                value={deliveriesPerDay}
                onChange={(e) => setDeliveriesPerDay(Number(e.target.value))}
                className="w-full h-3 bg-white/20 rounded-lg appearance-none cursor-pointer slider"
              />
              <div className="flex justify-between text-sm text-blue-200 mt-2">
                <span>10</span>
                <span>500</span>
              </div>
            </div>

            <div className="mb-8">
              <label className="flex justify-between items-center mb-3">
                <span className="text-lg font-semibold">Arac Sayisi</span>
                <span className="text-2xl font-bold text-green-300">{vehicleCount}</span>
              </label>
              <input
                type="range"
                min="1"
                max="50"
                value={vehicleCount}
                onChange={(e) => setVehicleCount(Number(e.target.value))}
                className="w-full h-3 bg-white/20 rounded-lg appearance-none cursor-pointer slider"
              />
              <div className="flex justify-between text-sm text-blue-200 mt-2">
                <span>1</span>
                <span>50</span>
              </div>
            </div>

            <div className="mb-8">
              <label className="block mb-3">
                <span className="text-lg font-semibold">Ortalama Mesafe / Teslimat (km)</span>
              </label>
              <input
                type="number"
                min="1"
                max="100"
                value={avgDistancePerDelivery}
                onChange={(e) => setAvgDistancePerDelivery(Number(e.target.value))}
                className="w-full px-4 py-3 bg-white/20 backdrop-blur-sm border border-white/30 rounded-xl text-white placeholder-blue-200 focus:outline-none focus:ring-2 focus:ring-blue-400"
                placeholder="Orn: 15"
              />
            </div>

            <div className="mb-8">
              <label className="block mb-3">
                <span className="text-lg font-semibold">Yakit Fiyati (TL/litre)</span>
              </label>
              <input
                type="number"
                min="10"
                max="100"
                value={fuelPricePerLiter}
                onChange={(e) => setFuelPricePerLiter(Number(e.target.value))}
                className="w-full px-4 py-3 bg-white/20 backdrop-blur-sm border border-white/30 rounded-xl text-white placeholder-blue-200 focus:outline-none focus:ring-2 focus:ring-blue-400"
                placeholder="Orn: 40"
              />
            </div>

            <div className="mb-8">
              <label className="flex justify-between items-center mb-3">
                <span className="text-lg font-semibold">Varsayilan Tasarruf Orani</span>
                <span className="text-2xl font-bold text-purple-300">%{fuelSavingsPercent}</span>
              </label>
              <input
                type="range"
                min="5"
                max="30"
                value={fuelSavingsPercent}
                onChange={(e) => setFuelSavingsPercent(Number(e.target.value))}
                className="w-full h-3 bg-white/20 rounded-lg appearance-none cursor-pointer slider"
              />
              <div className="flex justify-between text-sm text-blue-200 mt-2">
                <span>%5</span>
                <span>%30</span>
              </div>
            </div>

            <div className="bg-blue-500/20 backdrop-blur-sm rounded-xl p-4 border border-blue-400/30">
              <div className="flex items-start gap-3">
                <svg className="w-6 h-6 text-blue-300 flex-shrink-0 mt-0.5" fill="currentColor" viewBox="0 0 20 20">
                  <path fillRule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a1 1 0 000 2v3a1 1 0 001 1h1a1 1 0 100-2v-3a1 1 0 00-1-1H9z" clipRule="evenodd" />
                </svg>
                <p className="text-sm text-blue-100">
                  Bu hesaplama ornek bir varsayima dayanir. Gercek sonuc operasyonunuza gore degisebilir.
                </p>
              </div>
            </div>
          </motion.div>

          <motion.div
            initial={{ opacity: 0, x: 30 }}
            whileInView={{ opacity: 1, x: 0 }}
            transition={{ duration: 0.8, delay: 0.4 }}
            viewport={{ once: true }}
            className="space-y-6"
          >
            <div className="bg-white rounded-3xl p-8 lg:p-10 shadow-2xl">
              <h3 className="text-2xl font-bold text-gray-900 mb-8">Tahmini Yakit Tasarrufu</h3>

              <motion.div
                initial={{ scale: 0.9, opacity: 0 }}
                animate={isCalculated ? { scale: 1, opacity: 1 } : {}}
                transition={{ duration: 0.5, delay: 0.6 }}
                className="mb-6 p-6 bg-gradient-to-br from-green-50 to-emerald-50 rounded-2xl border-2 border-green-200"
              >
                <div className="flex items-center gap-3 mb-2">
                  <div className="w-10 h-10 rounded-full bg-green-500 flex items-center justify-center">
                    <span className="text-xl text-white">A</span>
                  </div>
                  <h4 className="text-lg font-semibold text-gray-700">Aylik Tasarruf</h4>
                </div>
                <div className="text-4xl font-bold text-green-600 mt-4">
                  {formatCurrency(results.monthlyFuelSavings)}
                </div>
                <p className="text-sm text-gray-600 mt-2">Varsayilan orana gore hesaplanir</p>
              </motion.div>

              <motion.div
                initial={{ scale: 0.9, opacity: 0 }}
                animate={isCalculated ? { scale: 1, opacity: 1 } : {}}
                transition={{ duration: 0.5, delay: 0.8 }}
                className="mb-6 p-6 bg-gradient-to-br from-purple-50 to-pink-50 rounded-2xl border-2 border-purple-200"
              >
                <div className="flex items-center gap-3 mb-2">
                  <div className="w-10 h-10 rounded-full bg-purple-500 flex items-center justify-center">
                    <span className="text-xl text-white">Y</span>
                  </div>
                  <h4 className="text-lg font-semibold text-gray-700">Yillik Tasarruf</h4>
                </div>
                <div className="text-4xl font-bold text-purple-600 mt-4">
                  {formatCurrency(results.yearlyFuelSavings)}
                </div>
                <p className="text-sm text-gray-600 mt-2">Aylik tasarrufun yillik karsiligi</p>
              </motion.div>
            </div>

            <motion.div
              initial={{ opacity: 0, y: 20 }}
              animate={isCalculated ? { opacity: 1, y: 0 } : {}}
              transition={{ duration: 0.5, delay: 1 }}
              className="text-center"
            >
              <a
                href="/contact"
                className="inline-flex items-center gap-3 bg-gradient-to-r from-blue-600 to-indigo-600 hover:from-blue-700 hover:to-indigo-700 text-white px-8 py-4 rounded-xl font-bold text-lg transition-all duration-200 shadow-xl hover:shadow-2xl hover:scale-105"
              >
                <span>Demo Talep Edin</span>
                <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M13 7l5 5m0 0l-5 5m5-5H6" />
                </svg>
              </a>
              <p className="text-sm text-blue-200 mt-4">Detayli analiz icin ekibimizle gorusebilirsiniz</p>
            </motion.div>
          </motion.div>
        </div>
      </div>

      <style jsx>{`
        .slider::-webkit-slider-thumb {
          appearance: none;
          width: 24px;
          height: 24px;
          border-radius: 50%;
          background: linear-gradient(135deg, #3b82f6, #8b5cf6);
          cursor: pointer;
          box-shadow: 0 4px 12px rgba(59, 130, 246, 0.4);
        }

        .slider::-moz-range-thumb {
          width: 24px;
          height: 24px;
          border-radius: 50%;
          background: linear-gradient(135deg, #3b82f6, #8b5cf6);
          cursor: pointer;
          box-shadow: 0 4px 12px rgba(59, 130, 246, 0.4);
          border: none;
        }
      `}</style>
    </section>
  );
};

export default ROICalculator;
