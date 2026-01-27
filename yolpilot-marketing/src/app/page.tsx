import HeroSection from '@/components/sections/HeroSection';
import SocialProof from '@/components/sections/SocialProof';
import ValuePropositions from '@/components/sections/ValuePropositions';
import FeaturesPreview from '@/components/sections/FeaturesPreview';
import TrustIndicators from '@/components/sections/TrustIndicators';
import ROICalculator from '@/components/sections/ROICalculator';

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
