'use client';

import { useEffect } from 'react';
import { usePathname, useSearchParams } from 'next/navigation';
import { PRIMARY_CTA_PATHS } from '@/lib/marketing';
import { persistUtmParams, trackMarketingEvent } from '@/lib/marketingTracking';

export default function MarketingAnalyticsTracker() {
  const pathname = usePathname();
  const searchParams = useSearchParams();

  useEffect(() => {
    persistUtmParams(searchParams);

    trackMarketingEvent({
      eventType: 'page_view',
      eventName: 'page_view',
      pagePath: `${pathname}${searchParams.toString() ? `?${searchParams}` : ''}`
    });
  }, [pathname, searchParams]);

  useEffect(() => {
    const clickHandler = (event: MouseEvent) => {
      const target = event.target as HTMLElement | null;
      const link = target?.closest('a');

      if (!link) return;

      const href = link.getAttribute('href') || '';
      const text = (link.textContent || '').trim().slice(0, 120);
      const isPrimaryCta = PRIMARY_CTA_PATHS.some((item) => href.startsWith(item));

      if (!isPrimaryCta && !link.dataset.marketingCta) {
        return;
      }

      trackMarketingEvent({
        eventType: 'cta_click',
        eventName: link.dataset.marketingCta || text || href,
        pagePath: window.location.pathname,
        metadata: {
          href,
          text
        }
      });
    };

    document.addEventListener('click', clickHandler);
    return () => document.removeEventListener('click', clickHandler);
  }, []);

  return null;
}
