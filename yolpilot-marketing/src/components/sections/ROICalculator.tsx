'use client';

import React, { useCallback, useEffect, useState } from 'react';
import Link from 'next/link';
import { motion } from 'framer-motion';
import { ArrowRight, Calculator, Clock3, Fuel } from 'lucide-react';

const ROICalculator: React.FC = () => {
  const [deliveriesPerDay, setDeliveriesPerDay] = useState<number>(50);
  const [vehicleCount, setVehicleCount] = useState<number>(5);
  const [avgDistancePerDelivery, setAvgDistancePerDelivery] = useState<number>(15);
  const [fuelPricePerLiter, setFuelPricePerLiter] = useState<number>(40);
  const [fuelSavingsPercent, setFuelSavingsPercent] = useState<number>(10);

  const [results, setResults] = useState({
    monthlyFuelSavings: 0,
    yearlyFuelSavings: 0,
    monthlyPlanningHoursSaved: 0,
    yearlyPlanningHoursSaved: 0
  });

  const calculateROI = useCallback(() => {
    const workingDaysPerMonth = 22;
    const fuelConsumptionPer100Km = 10;
    const planningHoursSavedPerVehiclePerDay = 0.45;

    const monthlyDeliveries = deliveriesPerDay * workingDaysPerMonth;
    const monthlyKilometers = monthlyDeliveries * avgDistancePerDelivery;
    const currentFuelConsumption = (monthlyKilometers * fuelConsumptionPer100Km) / 100;
    const currentFuelCost = currentFuelConsumption * fuelPricePerLiter;

    const fuelSavings = currentFuelCost * (fuelSavingsPercent / 100);
    const yearlySavings = fuelSavings * 12;
    const monthlyPlanningHoursSaved = vehicleCount * planningHoursSavedPerVehiclePerDay * workingDaysPerMonth;

    setResults({
      monthlyFuelSavings: Math.round(fuelSavings),
      yearlyFuelSavings: Math.round(yearlySavings),
      monthlyPlanningHoursSaved: Math.round(monthlyPlanningHoursSaved),
      yearlyPlanningHoursSaved: Math.round(monthlyPlanningHoursSaved * 12)
    });
  }, [avgDistancePerDelivery, deliveriesPerDay, fuelPricePerLiter, fuelSavingsPercent, vehicleCount]);

  useEffect(() => {
    calculateROI();
  }, [calculateROI]);

  const formatCurrency = (value: number) =>
    new Intl.NumberFormat('tr-TR', {
      style: 'currency',
      currency: 'TRY',
      minimumFractionDigits: 0,
      maximumFractionDigits: 0
    }).format(value);

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
          <div className="mb-5 inline-flex items-center gap-2 rounded-full border border-white/10 bg-white/8 px-4 py-2 text-sm font-semibold text-blue-100">
            <Calculator className="h-4 w-4" />
            Ornek ROI hesaplayici
          </div>
          <h2 className="text-3xl font-bold tracking-tight lg:text-5xl">Kendi operasyonunuza yakin bir senaryo ile kabaca hesap yapin.</h2>
          <p className="mt-5 text-lg leading-8 text-slate-300">
            Bu alan satis vaadi degil, planlama icin bir ornek hesap aracidir. Gercek sonuc; teslimat yogunlugu, rota
            yapisi ve operasyon duzenine gore degisir.
          </p>
        </motion.div>

        <div className="mt-12 grid gap-8 lg:grid-cols-[minmax(0,0.95fr)_minmax(0,1.05fr)]">
          <motion.div
            initial={{ opacity: 0, x: -24 }}
            whileInView={{ opacity: 1, x: 0 }}
            transition={{ duration: 0.7 }}
            viewport={{ once: true }}
            className="rounded-[2rem] border border-white/10 bg-white/6 p-7 shadow-2xl shadow-black/15 backdrop-blur"
          >
            <div className="mb-8">
              <div className="text-xs font-semibold uppercase tracking-[0.18em] text-blue-100">Senaryo girisi</div>
              <h3 className="mt-2 text-2xl font-semibold">Temel operasyon degerleri</h3>
            </div>

            <div className="space-y-7">
              <div>
                <label className="mb-3 flex items-center justify-between text-sm font-semibold text-slate-100">
                  <span>Gunluk teslimat sayisi</span>
                  <span>{deliveriesPerDay}</span>
                </label>
                <input
                  type="range"
                  min="10"
                  max="500"
                  value={deliveriesPerDay}
                  onChange={(e) => setDeliveriesPerDay(Number(e.target.value))}
                  className="slider h-3 w-full cursor-pointer appearance-none rounded-lg bg-white/15"
                />
              </div>

              <div>
                <label className="mb-3 flex items-center justify-between text-sm font-semibold text-slate-100">
                  <span>Arac sayisi</span>
                  <span>{vehicleCount}</span>
                </label>
                <input
                  type="range"
                  min="1"
                  max="50"
                  value={vehicleCount}
                  onChange={(e) => setVehicleCount(Number(e.target.value))}
                  className="slider h-3 w-full cursor-pointer appearance-none rounded-lg bg-white/15"
                />
              </div>

              <div className="grid gap-5 sm:grid-cols-2">
                <div>
                  <label className="mb-3 block text-sm font-semibold text-slate-100">Ortalama mesafe / teslimat (km)</label>
                  <input
                    type="number"
                    min="1"
                    max="100"
                    value={avgDistancePerDelivery}
                    onChange={(e) => setAvgDistancePerDelivery(Number(e.target.value))}
                    className="w-full rounded-2xl border border-white/10 bg-white/8 px-4 py-3 text-white outline-none ring-0 placeholder:text-slate-400"
                  />
                </div>

                <div>
                  <label className="mb-3 block text-sm font-semibold text-slate-100">Yakit fiyati (TL/litre)</label>
                  <input
                    type="number"
                    min="10"
                    max="100"
                    value={fuelPricePerLiter}
                    onChange={(e) => setFuelPricePerLiter(Number(e.target.value))}
                    className="w-full rounded-2xl border border-white/10 bg-white/8 px-4 py-3 text-white outline-none ring-0 placeholder:text-slate-400"
                  />
                </div>
              </div>

              <div>
                <label className="mb-3 flex items-center justify-between text-sm font-semibold text-slate-100">
                  <span>Varsayilan yakit tasarrufu orani</span>
                  <span>%{fuelSavingsPercent}</span>
                </label>
                <input
                  type="range"
                  min="5"
                  max="30"
                  value={fuelSavingsPercent}
                  onChange={(e) => setFuelSavingsPercent(Number(e.target.value))}
                  className="slider h-3 w-full cursor-pointer appearance-none rounded-lg bg-white/15"
                />
              </div>
            </div>
          </motion.div>

          <motion.div
            initial={{ opacity: 0, x: 24 }}
            whileInView={{ opacity: 1, x: 0 }}
            transition={{ duration: 0.7, delay: 0.1 }}
            viewport={{ once: true }}
            className="flex flex-col gap-6"
          >
            <div className="grid gap-5 sm:grid-cols-2">
              <div className="rounded-[2rem] border border-emerald-400/20 bg-emerald-400/8 p-6">
                <div className="flex items-center gap-3">
                  <div className="flex h-11 w-11 items-center justify-center rounded-2xl bg-emerald-400/18 text-emerald-200">
                    <Fuel className="h-5 w-5" />
                  </div>
                  <div className="text-sm font-semibold text-emerald-100">Aylik yakit tasarrufu</div>
                </div>
                <div className="mt-5 text-4xl font-bold text-emerald-200">{formatCurrency(results.monthlyFuelSavings)}</div>
                <div className="mt-2 text-sm text-slate-300">Varsayilan tasarruf orani ile hesaplanir.</div>
              </div>

              <div className="rounded-[2rem] border border-amber-400/20 bg-amber-400/8 p-6">
                <div className="flex items-center gap-3">
                  <div className="flex h-11 w-11 items-center justify-center rounded-2xl bg-amber-400/18 text-amber-200">
                    <Clock3 className="h-5 w-5" />
                  </div>
                  <div className="text-sm font-semibold text-amber-100">Aylik planlama saati</div>
                </div>
                <div className="mt-5 text-4xl font-bold text-amber-200">{results.monthlyPlanningHoursSaved} saat</div>
                <div className="mt-2 text-sm text-slate-300">Arac basina gunluk planlama sure kazanimi varsayimiyla hesaplanir.</div>
              </div>
            </div>

            <div className="rounded-[2rem] border border-white/10 bg-white/6 p-7">
              <div className="grid gap-5 sm:grid-cols-2">
                <div>
                  <div className="text-xs font-semibold uppercase tracking-[0.18em] text-slate-400">Yillik yaklasik tasarruf</div>
                  <div className="mt-3 text-3xl font-bold text-white">{formatCurrency(results.yearlyFuelSavings)}</div>
                </div>
                <div>
                  <div className="text-xs font-semibold uppercase tracking-[0.18em] text-slate-400">Yillik planlama zamani</div>
                  <div className="mt-3 text-3xl font-bold text-white">{results.yearlyPlanningHoursSaved} saat</div>
                </div>
              </div>

              <div className="mt-6 rounded-2xl border border-white/10 bg-black/10 p-4 text-sm leading-7 text-slate-300">
                Bu hesaplayici tahmini fikir vermek icin vardir. Gercek sonuc rota yogunlugu, sehir dagilimi, yakit tuketimi,
                teslimat pencereleri ve operasyon disiplinine gore degisir.
              </div>

              <div className="mt-6 flex flex-col gap-3 sm:flex-row">
                <Link
                  href="/contact"
                  className="inline-flex items-center justify-center gap-2 rounded-full bg-white px-6 py-3 text-sm font-semibold text-slate-900 transition-colors hover:bg-slate-100"
                >
                  Demo talep edin
                  <ArrowRight className="h-4 w-4" />
                </Link>
                <Link
                  href="/features"
                  className="inline-flex items-center justify-center rounded-full border border-white/10 px-6 py-3 text-sm font-semibold text-white transition-colors hover:bg-white/6"
                >
                  Ozellikleri inceleyin
                </Link>
              </div>
            </div>
          </motion.div>
        </div>
      </div>

      <style jsx>{`
        .slider::-webkit-slider-thumb {
          appearance: none;
          width: 22px;
          height: 22px;
          border-radius: 9999px;
          background: #ffffff;
          cursor: pointer;
          box-shadow: 0 6px 16px rgba(15, 23, 37, 0.3);
        }

        .slider::-moz-range-thumb {
          width: 22px;
          height: 22px;
          border-radius: 9999px;
          background: #ffffff;
          cursor: pointer;
          border: none;
          box-shadow: 0 6px 16px rgba(15, 23, 37, 0.3);
        }
      `}</style>
    </section>
  );
};

export default ROICalculator;
