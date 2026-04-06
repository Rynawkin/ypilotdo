import TrackedLink from '@/components/ui/TrackedLink';
import { PRIMARY_CTA_HREF } from '@/lib/marketing';

type DetailCard = {
  title: string;
  description: string;
};

type BulletSection = {
  title: string;
  description: string;
  items: string[];
};

type ResourcePageLayoutProps = {
  eyebrow: string;
  title: string;
  description: string;
  highlights: DetailCard[];
  sections?: DetailCard[];
  bulletSection?: BulletSection;
  footerTitle: string;
  footerDescription: string;
  primaryCtaLabel?: string;
  primaryCtaHref?: string;
  primaryTrackingName?: string;
  secondaryCtaLabel?: string;
  secondaryCtaHref?: string;
  secondaryTrackingName?: string;
};

export default function ResourcePageLayout({
  eyebrow,
  title,
  description,
  highlights,
  sections = [],
  bulletSection,
  footerTitle,
  footerDescription,
  primaryCtaLabel = 'Demo Talep Edin',
  primaryCtaHref = PRIMARY_CTA_HREF,
  primaryTrackingName = 'resource-primary-cta',
  secondaryCtaLabel,
  secondaryCtaHref,
  secondaryTrackingName = 'resource-secondary-cta'
}: ResourcePageLayoutProps) {
  return (
    <div className="min-h-screen bg-gray-50 pt-24">
      <section className="bg-gradient-to-br from-blue-700 via-blue-800 to-slate-900 py-20 text-white">
        <div className="mx-auto max-w-6xl px-4 sm:px-6 lg:px-8">
          <div className="max-w-4xl">
            <div className="inline-flex rounded-full bg-white/10 px-4 py-2 text-sm font-semibold text-blue-50">
              {eyebrow}
            </div>
            <h1 className="mt-6 text-4xl font-bold leading-tight lg:text-5xl">{title}</h1>
            <p className="mt-6 max-w-3xl text-lg leading-8 text-blue-100">{description}</p>
          </div>

          <div className="mt-12 grid gap-4 md:grid-cols-3">
            {highlights.map((item) => (
              <div key={item.title} className="rounded-2xl border border-white/10 bg-white/10 p-6 backdrop-blur">
                <h2 className="text-lg font-semibold text-white">{item.title}</h2>
                <p className="mt-3 text-sm leading-6 text-blue-100">{item.description}</p>
              </div>
            ))}
          </div>
        </div>
      </section>

      {sections.length > 0 && (
        <section className="bg-white py-16">
          <div className="mx-auto max-w-6xl px-4 sm:px-6 lg:px-8">
            <div className="grid gap-6 md:grid-cols-2 xl:grid-cols-3">
              {sections.map((item) => (
                <div key={item.title} className="rounded-2xl border border-gray-200 bg-gray-50 p-6 shadow-sm">
                  <h3 className="text-lg font-semibold text-gray-900">{item.title}</h3>
                  <p className="mt-3 text-sm leading-6 text-gray-600">{item.description}</p>
                </div>
              ))}
            </div>
          </div>
        </section>
      )}

      {bulletSection && (
        <section className="py-16">
          <div className="mx-auto max-w-6xl px-4 sm:px-6 lg:px-8">
            <div className="rounded-3xl border border-gray-200 bg-white p-8 shadow-sm">
              <div className="max-w-3xl">
                <div className="text-sm font-semibold uppercase tracking-[0.18em] text-blue-600">{bulletSection.title}</div>
                <p className="mt-3 text-base leading-7 text-gray-600">{bulletSection.description}</p>
              </div>
              <div className="mt-8 grid gap-4 md:grid-cols-2">
                {bulletSection.items.map((item) => (
                  <div key={item} className="flex gap-3 rounded-2xl border border-gray-100 bg-gray-50 px-5 py-4 text-sm leading-6 text-gray-700">
                    <span className="mt-2 h-2.5 w-2.5 shrink-0 rounded-full bg-blue-600" />
                    <span>{item}</span>
                  </div>
                ))}
              </div>
            </div>
          </div>
        </section>
      )}

      <section className="bg-slate-950 py-20 text-white">
        <div className="mx-auto max-w-4xl px-4 text-center sm:px-6 lg:px-8">
          <h2 className="text-3xl font-bold lg:text-4xl">{footerTitle}</h2>
          <p className="mt-5 text-base leading-7 text-slate-300">{footerDescription}</p>
          <div className="mt-8 flex flex-col justify-center gap-4 sm:flex-row">
            <TrackedLink
              href={primaryCtaHref}
              trackingName={primaryTrackingName}
              className="inline-flex items-center justify-center rounded-xl bg-white px-8 py-4 text-base font-semibold text-slate-900 transition hover:bg-slate-100"
            >
              {primaryCtaLabel}
            </TrackedLink>
            {secondaryCtaHref && secondaryCtaLabel && (
              <TrackedLink
                href={secondaryCtaHref}
                trackingName={secondaryTrackingName}
                className="inline-flex items-center justify-center rounded-xl border border-white/20 px-8 py-4 text-base font-semibold text-white transition hover:bg-white/10"
              >
                {secondaryCtaLabel}
              </TrackedLink>
            )}
          </div>
        </div>
      </section>
    </div>
  );
}
