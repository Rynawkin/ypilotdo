import type { MetadataRoute } from 'next';
import { SITE_URL } from '@/lib/marketing';

const routes = [
  '',
  '/features',
  '/pricing',
  '/faq',
  '/contact',
  '/customers',
  '/comparison',
  '/security',
  '/integrations',
  '/use-cases',
  '/industries/ecommerce',
  '/industries/logistics',
  '/industries/retail',
  '/industries/services',
  '/industries/food-delivery',
  '/industries/cold-chain',
  '/about',
  '/demo',
  '/roi-calculator',
  '/privacy',
  '/terms',
  '/cookies'
];

export default function sitemap(): MetadataRoute.Sitemap {
  const now = new Date();

  return routes.map((route) => ({
    url: `${SITE_URL}${route}`,
    lastModified: now,
    changeFrequency: route === '' ? 'weekly' : 'monthly',
    priority:
      route === ''
        ? 1
        : route === '/contact' || route === '/features' || route === '/pricing' || route === '/faq'
          ? 0.9
          : 0.7
  }));
}
