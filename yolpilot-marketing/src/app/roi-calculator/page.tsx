import Link from 'next/link';
import ROICalculator from '@/components/sections/ROICalculator';

export default function RoiCalculatorPage() {
  return (
    <div className="pt-24">
      <ROICalculator />
      <section className="py-16 bg-white">
        <div className="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 text-center">
          <h2 className="text-3xl font-bold text-gray-900 mb-4">Hesabi birlikte netlestirelim</h2>
          <p className="text-gray-600 mb-8">
            Varsayimlarinizi paylasin, size ozel bir senaryo uzerinden birlikte ilerleyelim.
          </p>
          <Link
            href="/contact"
            className="inline-flex items-center gap-2 bg-blue-600 hover:bg-blue-700 text-white px-8 py-4 rounded-xl font-semibold transition-all duration-200 shadow-lg"
          >
            Iletisim
          </Link>
        </div>
      </section>
    </div>
  );
}
