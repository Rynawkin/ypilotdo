'use client';

import React, { useMemo, useState } from 'react';
import { motion } from 'framer-motion';
import { ArrowRight, Calculator, Clock3, Fuel, Route } from 'lucide-react';
import TrackedLink from '@/components/ui/TrackedLink';
import { PRIMARY_CTA_HREF, SECONDARY_CTA_HREF } from '@/lib/marketing';

const formatCurrency = (value: number) =>
  new Intl.NumberFormat('tr-TR', {
    style: 'currency',
    currency: 'TRY',
    minimumFractionDigits: 0,
    maximumFractionDigits: 0
  }).format(value);

const ROICalculator: React.FC = () => {
  const [deliveriesPerDay, setDeliveriesPerDay] = useState<number>(80);
  const [vehicleCount, setVehicleCount] = useState<number>(6);
  const [avgKmPerDelivery, setAvgKmPerDelivery] = useState<number>(12);

  const estimate = useMemo(() => {
    const workingDaysPerMonth = 22;
    const fuelConsumptionPer100Km = 10;
    const fuelPrice = 42;

    const monthlyDeliveries = deliveriesPerDay * workingDaysPerMonth;
    const monthlyKm = monthlyDeliveries * avgKmPerDelivery;
    const monthlyFuelCost = ((monthlyKm * fuelConsumptionPer100Km) / 100) * fuelPrice;

    const fuelSavingsMin = monthlyFuelCost * 0.05;
    const fuelSavingsMax = monthlyFuelCost * 0.12;

    const monthlyPlanningHoursMin = vehicleCount * 5;
    const monthlyPlanningHoursMax = vehicleCount * 11;

    return {
      monthlyFuelSavingsMin: Math.round(fuelSavingsMin),
      monthlyFuelSavingsMax: Math.round(fuelSavingsMax),
      annualFuelSavingsMin: Math.round(fuelSavingsMin * 12),
      annualFuelSavingsMax: Math.round(fuelSavingsMax * 12),
      monthlyPlanningHoursMin,
      monthlyPlanningHoursMax
    };
  }, [avgKmPerDelivery, deliveriesPerDay, vehicleCount]);

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
            Örnek etki hesaplayıcısı
          </div>
          <h2 className="text-3xl font-bold tracking-tight lg:text-5xl">Net vaat yerine makul bir aralıkla konuşmayı tercih ediyoruz.</h2>
          <p className="mt-5 text-lg leading-8 text-slate-300">
            Aşağıdaki alan, operasyon büyüklüğüne göre oluşabilecek etkiyi yaklaşık bir aralıkta gösterir. Gerçek sonuç; şehir dağılımı, zaman pencereleri, araç tipi ve saha disiplini gibi değişkenlere göre farklılaşır.
          </p>
        </motion.div>

        <div className="mt-12 grid gap-8 lg:grid-cols-[minmax(0,0.95fr)_minmax(0,1.05fr)]">
          <motion.div
            initial={{ opacity: 0, x: -24 }}
            whileInView={{ opacity: 1, x: 0 }}
            transition={{ duration: 0.7 }}
            viewport={{ once: true }}
            className="rounded-[2rem] border border-white/10 bg-white/6 p-7 shadow-2xl shadow-black/10 backdrop-blur"
          >
            <div className="mb-8">
              <div className="text-xs font-semibold uppercase tracking-[0.18em] text-blue-100">Senaryo girişi</div>
              <h3 className="mt-2 text-2xl font-semibold">Operasyon yoğunluğunu seçin</h3>
            </div>

            <div className="space-y-8">
              <div>
                <label className="mb-3 flex items-center justify-between text-sm font-semibold text-slate-100">
                  <span>Günlük teslimat sayısı</span>
                  <span>{deliveriesPerDay}</span>
                </label>
                <input
                  type="range"
                  min="20"
                  max="300"
                  value={deliveriesPerDay}
                  onChange={(e) => setDeliveriesPerDay(Number(e.target.value))}
                  className="slider h-3 w-full cursor-pointer appearance-none rounded-lg bg-white/15"
                />
              </div>

              <div>
                <label className="mb-3 flex items-center justify-between text-sm font-semibold text-slate-100">
                  <span>Aktif araç sayısı</span>
                  <span>{vehicleCount}</span>
                </label>
                <input
                  type="range"
                  min="2"
                  max="25"
                  value={vehicleCount}
                  onChange={(e) => setVehicleCount(Number(e.target.value))}
                  className="slider h-3 w-full cursor-pointer appearance-none rounded-lg bg-white/15"
                />
              </div>

              <div>
                <label className="mb-3 flex items-center justify-between text-sm font-semibold text-slate-100">
                  <span>Teslimat başına ortalama km</span>
                  <span>{avgKmPerDelivery} km</span>
                </label>
                <input
                  type="range"
                  min="5"
                  max="35"
                  value={avgKmPerDelivery}
                  onChange={(e) => setAvgKmPerDelivery(Number(e.target.value))}
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
                  <div className="text-sm font-semibold text-emerald-100">Aylık yakıt etkisi</div>
                </div>
                <div className="mt-5 text-3xl font-bold text-emerald-200">
                  {formatCurrency(estimate.monthlyFuelSavingsMin)} - {formatCurrency(estimate.monthlyFuelSavingsMax)}
                </div>
                <div className="mt-2 text-sm text-slate-300">Yaklaşık %5-%12 bandında potansiyel fark.</div>
              </div>

              <div className="rounded-[2rem] border border-amber-400/20 bg-amber-400/8 p-6">
                <div className="flex items-center gap-3">
                  <div className="flex h-11 w-11 items-center justify-center rounded-2xl bg-amber-400/18 text-amber-200">
                    <Clock3 className="h-5 w-5" />
                  </div>
                  <div className="text-sm font-semibold text-amber-100">Aylık planlama zamanı</div>
                </div>
                <div className="mt-5 text-3xl font-bold text-amber-200">
                  {estimate.monthlyPlanningHoursMin} - {estimate.monthlyPlanningHoursMax} saat
                </div>
                <div className="mt-2 text-sm text-slate-300">Araç sayısına göre değişebilen yaklaşık kazanım.</div>
              </div>
            </div>

            <div className="rounded-[2rem] border border-white/10 bg-white/6 p-7">
              <div className="grid gap-5 sm:grid-cols-2">
                <div>
                  <div className="text-xs font-semibold uppercase tracking-[0.18em] text-slate-400">Yıllık yakıt etkisi</div>
                  <div className="mt-3 text-3xl font-bold text-white">
                    {formatCurrency(estimate.annualFuelSavingsMin)} - {formatCurrency(estimate.annualFuelSavingsMax)}
                  </div>
                </div>
                <div>
                  <div className="text-xs font-semibold uppercase tracking-[0.18em] text-slate-400">Beklenen operasyon etkisi</div>
                  <div className="mt-3 text-2xl font-bold text-white">Daha az manuel planlama, daha net saha görünürlüğü</div>
                </div>
              </div>

              <div className="mt-6 rounded-2xl border border-white/10 bg-black/10 p-4 text-sm leading-7 text-slate-300">
                Bu hesaplama genel dağıtım operasyonları için hazırlanmış yaklaşık bir aralıktır. Gerçek tabloyu demo sırasında kendi rota yapınız, teslimat pencereleriniz ve şehir dağılımınız ile birlikte değerlendiririz.
              </div>

              <div className="mt-6 grid gap-3 text-sm text-slate-300 sm:grid-cols-3">
                <div className="rounded-2xl border border-white/10 bg-white/5 px-4 py-4">
                  <div className="mb-2 flex items-center gap-2 text-white">
                    <Route className="h-4 w-4" />
                    Planlama kalitesi
                  </div>
                  Rota sırası, durak yoğunluğu ve depoya dönüş kurgusu.
                </div>
                <div className="rounded-2xl border border-white/10 bg-white/5 px-4 py-4">
                  <div className="mb-2 flex items-center gap-2 text-white">
                    <Fuel className="h-4 w-4" />
                    Maliyet etkisi
                  </div>
                  Yakıt, boşa giden km ve tekrar planlama ihtiyacı.
                </div>
                <div className="rounded-2xl border border-white/10 bg-white/5 px-4 py-4">
                  <div className="mb-2 flex items-center gap-2 text-white">
                    <Clock3 className="h-4 w-4" />
                    Operasyon ritmi
                  </div>
                  Saha takibi, gecikme görünürlüğü ve ekip içi koordinasyon.
                </div>
              </div>

              <div className="mt-8 flex flex-col gap-3 sm:flex-row">
                <TrackedLink
                  href={PRIMARY_CTA_HREF}
                  trackingName="roi-primary-cta"
                  className="inline-flex items-center justify-center gap-2 rounded-full bg-white px-6 py-3 text-sm font-semibold !text-slate-900 transition-colors hover:bg-slate-100"
                >
                  Demo Talep Edin
                  <ArrowRight className="h-4 w-4" />
                </TrackedLink>
                <TrackedLink
                  href={SECONDARY_CTA_HREF}
                  trackingName="roi-secondary-cta"
                  className="inline-flex items-center justify-center rounded-full border border-white/10 px-6 py-3 text-sm font-semibold !text-white transition-colors hover:bg-white/6"
                >
                  Özellikleri İnceleyin
                </TrackedLink>
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
