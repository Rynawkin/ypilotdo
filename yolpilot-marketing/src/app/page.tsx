import HeroSection from '@/components/sections/HeroSection';
import SocialProof from '@/components/sections/SocialProof';
import ValuePropositions from '@/components/sections/ValuePropositions';
import FeaturesPreview from '@/components/sections/FeaturesPreview';
import TrustIndicators from '@/components/sections/TrustIndicators';
import ROICalculator from '@/components/sections/ROICalculator';
import { createPageMetadata } from '@/lib/seo';

export const metadata = createPageMetadata({
  title: 'Teslimat ve Rota Operasyon Platformu',
  description:
    'Rota optimizasyonu, saha uygulaması, teslimat kanıtı ve müşteri bilgilendirmesini tek platformda yönetin.',
  path: '/',
  keywords: ['rota optimizasyonu', 'teslimat yönetimi', 'araç takibi', 'lojistik yazılımı', 'Türkiye']
});

export default function Home() {
  return (
    <div className="min-h-screen">
      <HeroSection />
      <SocialProof />
      <ValuePropositions />
      <FeaturesPreview />
      <TrustIndicators />
      <ROICalculator />
    </div>
  );
}
