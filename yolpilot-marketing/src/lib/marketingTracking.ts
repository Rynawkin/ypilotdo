'use client';

const VISITOR_KEY = 'yp_marketing_visitor_id';
const SESSION_KEY = 'yp_marketing_session_id';
const UTM_KEY = 'yp_marketing_utm';

type TrackingPayload = {
  eventType: 'page_view' | 'cta_click' | 'form_submit';
  eventName?: string;
  pagePath?: string;
  pageTitle?: string;
  referrer?: string;
  utmSource?: string | null;
  utmMedium?: string | null;
  utmCampaign?: string | null;
  utmContent?: string | null;
  utmTerm?: string | null;
  deviceType?: string;
  browser?: string;
  os?: string;
  metadata?: Record<string, unknown>;
};

type StoredUtm = {
  utmSource?: string | null;
  utmMedium?: string | null;
  utmCampaign?: string | null;
  utmContent?: string | null;
  utmTerm?: string | null;
};

function createId() {
  return typeof crypto !== 'undefined' && crypto.randomUUID ? crypto.randomUUID() : `${Date.now()}-${Math.random().toString(16).slice(2)}`;
}

export function getVisitorId() {
  if (typeof window === 'undefined') return '';
  const existing = localStorage.getItem(VISITOR_KEY);
  if (existing) return existing;
  const created = createId();
  localStorage.setItem(VISITOR_KEY, created);
  return created;
}

export function getSessionId() {
  if (typeof window === 'undefined') return '';
  const existing = sessionStorage.getItem(SESSION_KEY);
  if (existing) return existing;
  const created = createId();
  sessionStorage.setItem(SESSION_KEY, created);
  return created;
}

export function persistUtmParams(search: URLSearchParams) {
  if (typeof window === 'undefined') return;

  const stored = getStoredUtms();
  const payload: StoredUtm = {
    utmSource: search.get('utm_source') ?? stored.utmSource ?? undefined,
    utmMedium: search.get('utm_medium') ?? stored.utmMedium ?? undefined,
    utmCampaign: search.get('utm_campaign') ?? stored.utmCampaign ?? undefined,
    utmContent: search.get('utm_content') ?? stored.utmContent ?? undefined,
    utmTerm: search.get('utm_term') ?? stored.utmTerm ?? undefined
  };

  localStorage.setItem(UTM_KEY, JSON.stringify(payload));
}

export function getStoredUtms(): StoredUtm {
  if (typeof window === 'undefined') return {};

  try {
    const raw = localStorage.getItem(UTM_KEY);
    return raw ? JSON.parse(raw) : {};
  } catch {
    return {};
  }
}

function detectDeviceType() {
  if (typeof navigator === 'undefined') return 'unknown';
  const ua = navigator.userAgent.toLowerCase();
  if (/tablet|ipad/.test(ua)) return 'tablet';
  if (/mobile|android|iphone/.test(ua)) return 'mobile';
  return 'desktop';
}

function detectBrowser() {
  if (typeof navigator === 'undefined') return 'unknown';
  const ua = navigator.userAgent;
  if (ua.includes('Edg')) return 'edge';
  if (ua.includes('Chrome')) return 'chrome';
  if (ua.includes('Safari') && !ua.includes('Chrome')) return 'safari';
  if (ua.includes('Firefox')) return 'firefox';
  return 'other';
}

function detectOs() {
  if (typeof navigator === 'undefined') return 'unknown';
  const ua = navigator.userAgent;
  if (ua.includes('Windows')) return 'windows';
  if (ua.includes('Mac OS')) return 'macos';
  if (ua.includes('Android')) return 'android';
  if (ua.includes('iPhone') || ua.includes('iPad')) return 'ios';
  if (ua.includes('Linux')) return 'linux';
  return 'other';
}

export function getTrackingContext() {
  if (typeof window === 'undefined') {
    return {
      visitorId: '',
      sessionId: '',
      landingPage: '',
      referrer: undefined,
      utmSource: undefined,
      utmMedium: undefined,
      utmCampaign: undefined,
      utmContent: undefined,
      utmTerm: undefined,
      deviceType: 'unknown',
      browser: 'unknown',
      os: 'unknown'
    };
  }

  const utm = getStoredUtms();

  return {
    visitorId: getVisitorId(),
    sessionId: getSessionId(),
    landingPage: `${window.location.pathname}${window.location.search}`,
    referrer: document.referrer || undefined,
    utmSource: utm.utmSource,
    utmMedium: utm.utmMedium,
    utmCampaign: utm.utmCampaign,
    utmContent: utm.utmContent,
    utmTerm: utm.utmTerm,
    deviceType: detectDeviceType(),
    browser: detectBrowser(),
    os: detectOs()
  };
}

function pushThirdPartyEvent(eventType: TrackingPayload['eventType'], eventName?: string, metadata?: Record<string, unknown>) {
  if (typeof window === 'undefined') return;

  if (window.gtag) {
    window.gtag('event', eventType, {
      event_category: 'marketing',
      event_label: eventName,
      ...metadata
    });
  }

  if (window.fbq) {
    if (eventType === 'form_submit') {
      window.fbq('track', 'Lead', metadata || {});
    } else if (eventType === 'cta_click') {
      window.fbq('trackCustom', 'MarketingCtaClick', {
        name: eventName,
        ...metadata
      });
    }
  }
}

export async function trackMarketingEvent(payload: TrackingPayload) {
  if (typeof window === 'undefined') return;

  const context = getTrackingContext();
  const body = {
    visitorId: context.visitorId,
    sessionId: context.sessionId,
    eventType: payload.eventType,
    eventName: payload.eventName,
    pagePath: payload.pagePath ?? window.location.pathname,
    pageTitle: payload.pageTitle ?? document.title,
    referrer: payload.referrer ?? context.referrer,
    utmSource: payload.utmSource ?? context.utmSource,
    utmMedium: payload.utmMedium ?? context.utmMedium,
    utmCampaign: payload.utmCampaign ?? context.utmCampaign,
    utmContent: payload.utmContent ?? context.utmContent,
    utmTerm: payload.utmTerm ?? context.utmTerm,
    deviceType: payload.deviceType ?? context.deviceType,
    browser: payload.browser ?? context.browser,
    os: payload.os ?? context.os,
    metadataJson: payload.metadata ? JSON.stringify(payload.metadata) : undefined
  };

  pushThirdPartyEvent(payload.eventType, payload.eventName, payload.metadata);

  try {
    await fetch('/api/proxy/api/marketing-analytics/track', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json'
      },
      body: JSON.stringify(body),
      keepalive: true
    });
  } catch {
    // best effort only
  }
}
