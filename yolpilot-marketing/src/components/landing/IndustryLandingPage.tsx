import TrackedLink from '@/components/ui/TrackedLink';
import { PRIMARY_CTA_HREF } from '@/lib/marketing';

type IndustryLandingPageProps = {
  eyebrow: string;
  title: string;
  description: string;
  proof: Array<{ label: string; value: string }>;
  problems: string[];
  outcomes: Array<{ title: string; description: string }>;
  workflow: Array<{ title: string; description: string }>;
  faqs: Array<{ question: string; answer: string }>;
  ctaLabel?: string;
};

export default function IndustryLandingPage({
  eyebrow,
  title,
  description,
  proof,
  problems,
  outcomes,
  workflow,
  faqs,
  ctaLabel = 'Demo Talep Edin'
}: IndustryLandingPageProps) {
  return (
    <div className="min-h-screen bg-gray-50 pt-24">
      <section className="bg-gradient-to-br from-blue-600 via-blue-700 to-indigo-700 py-20 text-white">
        <div className="mx-auto max-w-6xl px-4 sm:px-6 lg:px-8">
          <div className="max-w-4xl">
            <div className="inline-flex rounded-full bg-white/15 px-4 py-2 text-sm font-semibold text-blue-50">
              {eyebrow}
            </div>
            <h1 className="mt-6 text-4xl font-bold leading-tight lg:text-5xl">{title}</h1>
            <p className="mt-6 max-w-3xl text-lg leading-8 text-blue-100">{description}</p>
            <div className="mt-8 flex flex-col gap-4 sm:flex-row">
              <TrackedLink
                href={PRIMARY_CTA_HREF}
                trackingName={`industry-${eyebrow.toLowerCase()}-primary-cta`}
                className="inline-flex items-center justify-center rounded-xl bg-white px-8 py-4 text-base font-semibold text-blue-700 transition hover:bg-blue-50"
              >
                {ctaLabel}
              </TrackedLink>
              <TrackedLink
                href="/features"
                trackingName={`industry-${eyebrow.toLowerCase()}-secondary-cta`}
                className="inline-flex items-center justify-center rounded-xl border border-white/30 px-8 py-4 text-base font-semibold text-white transition hover:bg-white/10"
              >
                Platformu İnceleyin
              </TrackedLink>
            </div>
          </div>

          <div className="mt-12 grid gap-4 md:grid-cols-3">
            {proof.map((item) => (
              <div key={item.label} className="rounded-2xl border border-white/15 bg-white/10 px-5 py-5 backdrop-blur">
                <div className="text-sm font-medium text-blue-100">{item.label}</div>
                <div className="mt-2 text-2xl font-semibold text-white">{item.value}</div>
              </div>
            ))}
          </div>
        </div>
      </section>

      <section className="bg-white py-16">
        <div className="mx-auto max-w-6xl px-4 sm:px-6 lg:px-8">
          <div className="max-w-3xl">
            <div className="text-sm font-semibold uppercase tracking-[0.18em] text-blue-600">Karşılaşılan sorunlar</div>
            <h2 className="mt-3 text-3xl font-bold text-gray-900">Ekibin en çok vakit kaybettiği alanları görünür hale getirin</h2>
          </div>
          <div className="mt-10 grid gap-4 md:grid-cols-2">
            {problems.map((item) => (
              <div key={item} className="rounded-2xl border border-gray-200 bg-gray-50 px-5 py-4 text-sm leading-6 text-gray-700">
                {item}
              </div>
            ))}
          </div>
        </div>
      </section>

      <section className="py-16">
        <div className="mx-auto max-w-6xl px-4 sm:px-6 lg:px-8">
          <div className="max-w-3xl">
            <div className="text-sm font-semibold uppercase tracking-[0.18em] text-blue-600">YolPilot yaklaşımı</div>
            <h2 className="mt-3 text-3xl font-bold text-gray-900">Planlama, saha ve müşteri iletişimini tek operasyonda toplayın</h2>
          </div>
          <div className="mt-10 grid gap-6 md:grid-cols-3">
            {outcomes.map((item) => (
              <div key={item.title} className="rounded-2xl border border-gray-200 bg-white p-6 shadow-sm">
                <h3 className="text-lg font-semibold text-gray-900">{item.title}</h3>
                <p className="mt-3 text-sm leading-6 text-gray-600">{item.description}</p>
              </div>
            ))}
          </div>
        </div>
      </section>

      <section className="bg-white py-16">
        <div className="mx-auto max-w-6xl px-4 sm:px-6 lg:px-8">
          <div className="max-w-3xl">
            <div className="text-sm font-semibold uppercase tracking-[0.18em] text-blue-600">Nasıl ilerliyoruz?</div>
            <h2 className="mt-3 text-3xl font-bold text-gray-900">Karmaşık operasyonları birkaç net adıma bölüyoruz</h2>
          </div>
          <div className="mt-10 grid gap-6 md:grid-cols-3">
            {workflow.map((item, index) => (
              <div key={item.title} className="rounded-2xl border border-gray-200 bg-gray-50 p-6">
                <div className="text-sm font-semibold text-blue-600">Adım {index + 1}</div>
                <h3 className="mt-3 text-lg font-semibold text-gray-900">{item.title}</h3>
                <p className="mt-3 text-sm leading-6 text-gray-600">{item.description}</p>
              </div>
            ))}
          </div>
        </div>
      </section>

      <section className="py-16">
        <div className="mx-auto max-w-5xl px-4 sm:px-6 lg:px-8">
          <div className="text-center">
            <div className="text-sm font-semibold uppercase tracking-[0.18em] text-blue-600">Sık sorulanlar</div>
            <h2 className="mt-3 text-3xl font-bold text-gray-900">Bu yapı bizim operasyonumuza uyar mı?</h2>
          </div>
          <div className="mt-10 space-y-4">
            {faqs.map((item) => (
              <div key={item.question} className="rounded-2xl border border-gray-200 bg-white p-6 shadow-sm">
                <h3 className="text-lg font-semibold text-gray-900">{item.question}</h3>
                <p className="mt-3 text-sm leading-6 text-gray-600">{item.answer}</p>
              </div>
            ))}
          </div>
        </div>
      </section>

      <section className="bg-slate-950 py-20 text-white">
        <div className="mx-auto max-w-4xl px-4 text-center sm:px-6 lg:px-8">
          <h2 className="text-3xl font-bold lg:text-4xl">Ekibinize uygun kurulumu birlikte netleştirelim</h2>
          <p className="mt-5 text-base leading-7 text-slate-300">
            Sektörünüze özel süreçleri, bildirim akışlarını ve saha operasyonunu gerçek senaryonuz üzerinden birlikte planlayalım.
          </p>
          <div className="mt-8">
            <TrackedLink
              href={PRIMARY_CTA_HREF}
              trackingName={`industry-${eyebrow.toLowerCase()}-footer-cta`}
              className="inline-flex items-center justify-center rounded-xl bg-white px-8 py-4 text-base font-semibold text-slate-900 transition hover:bg-slate-100"
            >
              {ctaLabel}
            </TrackedLink>
          </div>
        </div>
      </section>
    </div>
  );
}
