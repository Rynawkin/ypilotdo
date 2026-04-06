'use client';

import Link, { LinkProps } from 'next/link';
import React from 'react';
import { trackMarketingEvent } from '@/lib/marketingTracking';

type TrackedLinkProps = LinkProps &
  Omit<React.AnchorHTMLAttributes<HTMLAnchorElement>, 'href'> & {
    trackingName?: string;
  };

export default function TrackedLink({ trackingName, onClick, ...props }: TrackedLinkProps) {
  return (
    <Link
      {...props}
      data-marketing-cta={trackingName}
      onClick={(event) => {
        trackMarketingEvent({
          eventType: 'cta_click',
          eventName: trackingName || (typeof props.children === 'string' ? props.children : undefined),
          pagePath: typeof window !== 'undefined' ? window.location.pathname : props.href.toString()
        });

        onClick?.(event);
      }}
    />
  );
}
