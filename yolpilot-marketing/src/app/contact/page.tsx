import { createPageMetadata } from '@/lib/seo';
import ContactPageContent from './ContactPageContent';

export const metadata = createPageMetadata({
  title: 'Demo Talep Edin',
  description:
    'YolPilot demo talebi bırakın. Rota planlama, saha uygulaması, teslimat kanıtı ve müşteri bilgilendirme akışınızı birlikte değerlendirelim.',
  path: '/contact',
  keywords: ['YolPilot demo', 'rota planlama demo', 'lojistik yazılım teklif']
});

export default function ContactPage() {
  return <ContactPageContent />;
}
