import TrackedLink from '@/components/ui/TrackedLink';
import ROICalculator from '@/components/sections/ROICalculator';
import { PRIMARY_CTA_HREF } from '@/lib/marketing';
import { createPageMetadata } from '@/lib/seo';

export const metadata = createPageMetadata({
  title: 'ROI Hesaplayıcı',
  description: 'Operasyon büyüklüğünüze göre yakıt ve planlama etkisini yaklaşık aralıklarla görün.',
  path: '/roi-calculator',
  keywords: ['roi hesaplayıcı', 'rota optimizasyon tasarruf', 'lojistik maliyet hesabı']
});

export default function RoiCalculatorPage() {
  return (
    <div className="pt-24">
      <ROICalculator />
      <section className="bg-white py-16">
        <div className="mx-auto max-w-4xl px-4 text-center sm:px-6 lg:px-8">
          <h2 className="text-3xl font-bold text-gray-900 mb-4">Hesabı birlikte netleştirelim</h2>
          <p className="text-gray-600 mb-8">
            Buradaki sonuçlar örnek aralıklardır. Gerçek tabloyu araç yapınız, şehir dağılımınız ve zaman pencerelerinizle birlikte değerlendirelim.
          </p>
          <TrackedLink
            href={PRIMARY_CTA_HREF}
            trackingName="roi-page-footer-cta"
            className="inline-flex items-center gap-2 rounded-xl bg-blue-600 px-8 py-4 font-semibold text-white shadow-lg transition-all duration-200 hover:bg-blue-700"
          >
            Demo Talep Edin
          </TrackedLink>
        </div>
      </section>
    </div>
  );
}
