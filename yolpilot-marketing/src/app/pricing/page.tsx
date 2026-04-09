import TrackedLink from '@/components/ui/TrackedLink';
import { createPageMetadata } from '@/lib/seo';
import { formatTry, pricingPlans } from '@/lib/pricing';
import { PRIMARY_CTA_HREF } from '@/lib/marketing';

export const metadata = createPageMetadata({
  title: 'Fiyatlandırma',
  description:
    'YolPilot planlarını, aylık fiyatları, dahil kullanım haklarını ve otomatik yenileme koşullarını inceleyin.',
  path: '/pricing',
  keywords: ['yolpilot fiyatları', 'rota optimizasyonu fiyat', 'teslimat yazılımı fiyatlandırma']
});

export default function PricingPage() {
  return (
    <div className="min-h-screen bg-[#f6f8fb] pt-24">
      <section className="border-b border-[color:var(--line)] bg-white py-16 lg:py-20">
        <div className="mx-auto max-w-6xl px-4 sm:px-6 lg:px-8">
          <div className="max-w-3xl">
            <div className="inline-flex rounded-full border border-slate-200 bg-slate-50 px-4 py-2 text-sm font-semibold text-[color:var(--brand-deep)]">
              Fiyatlandırma
            </div>
            <h1 className="mt-6 text-4xl font-bold tracking-tight text-slate-950 lg:text-5xl">
              Operasyon hacminize göre yön veren planlar.
            </h1>
            <p className="mt-6 text-lg leading-8 text-slate-600">
              YolPilot fiyatları aylık plan mantığıyla çalışır. Ücretli planlarda ilk ödeme sırasında kart
              kaydedilir ve abonelik her ay otomatik yenilenir.
            </p>
          </div>
        </div>
      </section>

      <section className="py-16 lg:py-20">
        <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
          <div className="grid gap-6 xl:grid-cols-4">
            {pricingPlans.map((plan) => (
              <article
                key={plan.id}
                className={`rounded-[1.8rem] border p-7 shadow-sm shadow-slate-900/5 ${
                  plan.popular ? 'border-blue-300 bg-blue-50/50' : 'border-slate-200 bg-white'
                }`}
              >
                <div className="flex items-center justify-between gap-3">
                  <h2 className="text-2xl font-semibold text-slate-950">{plan.name}</h2>
                  {plan.popular ? (
                    <span className="rounded-full bg-blue-600 px-3 py-1 text-xs font-semibold text-white">Önerilen</span>
                  ) : null}
                </div>
                <p className="mt-3 text-sm leading-6 text-slate-600">{plan.summary}</p>
                <div className="mt-5 text-3xl font-bold text-slate-950">{formatTry(plan.monthlyPrice)}</div>
                <div className="mt-1 text-sm text-slate-500">/ay</div>
                <div className="mt-4 rounded-2xl bg-[color:var(--surface-muted)] px-4 py-3 text-sm font-medium text-slate-700">
                  {plan.audience}
                </div>
                <ul className="mt-6 space-y-3 text-sm text-slate-700">
                  {plan.features.map((feature) => (
                    <li key={feature} className="flex items-start gap-3">
                      <span className="mt-1.5 h-2 w-2 rounded-full bg-[color:var(--brand)]" />
                      <span>{feature}</span>
                    </li>
                  ))}
                </ul>
                <div className="mt-6 border-t border-slate-200 pt-5">
                  <div className="text-xs font-semibold uppercase tracking-[0.16em] text-slate-500">Notlar</div>
                  <ul className="mt-3 space-y-2 text-sm text-slate-600">
                    {plan.notes.map((note) => (
                      <li key={note}>{note}</li>
                    ))}
                  </ul>
                </div>
              </article>
            ))}
          </div>

          <div className="mt-10 rounded-[2rem] bg-white p-8 shadow-sm shadow-slate-900/5">
            <h2 className="text-2xl font-semibold text-slate-950">Abonelik ve tahsilat notları</h2>
            <ul className="mt-5 space-y-3 text-base leading-7 text-slate-600">
              <li>Ücretli planlarda ilk ödeme sırasında kart güvenli ödeme sağlayıcısında saklanır.</li>
              <li>Aylık yenileme otomatik yapılır; başarısız tahsilatta 3 günlük süre sonunda erişim kapanır.</li>
              <li>Dahil kullanımı aşan durak ve WhatsApp bildirimleri yalnızca aşan miktar kadar ücretlenir.</li>
              <li>Kurulum ve plan seçimi konusunda emin değilseniz önce demo talep ederek birlikte karar verebiliriz.</li>
            </ul>
            <div className="mt-6 flex flex-wrap gap-3">
              <TrackedLink
                href={PRIMARY_CTA_HREF}
                trackingName="pricing-demo-talep"
                className="rounded-full bg-[color:var(--brand)] px-6 py-3 text-sm font-semibold text-white transition hover:bg-blue-700"
              >
                Demo talep edin
              </TrackedLink>
              <TrackedLink
                href="/faq"
                trackingName="pricing-faq-gor"
                className="rounded-full border border-slate-300 bg-white px-6 py-3 text-sm font-semibold text-slate-700 transition hover:bg-slate-50"
              >
                Sık sorulan sorular
              </TrackedLink>
            </div>
          </div>
        </div>
      </section>
    </div>
  );
}
