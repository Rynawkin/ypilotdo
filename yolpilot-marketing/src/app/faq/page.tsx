import TrackedLink from '@/components/ui/TrackedLink';
import { salesFaqItems } from '@/lib/faq';
import { createPageMetadata } from '@/lib/seo';
import { PRIMARY_CTA_HREF } from '@/lib/marketing';

export const metadata = createPageMetadata({
  title: 'Sık Sorulan Sorular',
  description:
    'YolPilot fiyatları, kurulum süresi, mobil uygulama, otomatik ödeme ve entegrasyonlarla ilgili sık sorulan sorular.',
  path: '/faq',
  keywords: ['yolpilot soru cevap', 'rota optimizasyonu sss', 'teslimat yazılımı sık sorulan sorular']
});

export default function FaqPage() {
  return (
    <div className="min-h-screen bg-[#f6f8fb] pt-24">
      <section className="border-b border-[color:var(--line)] bg-white py-16 lg:py-20">
        <div className="mx-auto max-w-5xl px-4 sm:px-6 lg:px-8">
          <div className="max-w-3xl">
            <div className="inline-flex rounded-full border border-slate-200 bg-slate-50 px-4 py-2 text-sm font-semibold text-[color:var(--brand-deep)]">
              Sık sorulan sorular
            </div>
            <h1 className="mt-6 text-4xl font-bold tracking-tight text-slate-950 lg:text-5xl">
              Reklamdan gelen kullanıcıların en çok sorduğu başlıklar.
            </h1>
            <p className="mt-6 text-lg leading-8 text-slate-600">
              Fiyat, kurulum, mobil uygulama, otomatik ödeme, entegrasyon ve kullanım kapsamı ile ilgili en sık
              soruları burada topladık.
            </p>
          </div>
        </div>
      </section>

      <section className="py-16 lg:py-20">
        <div className="mx-auto max-w-5xl px-4 sm:px-6 lg:px-8">
          <div className="space-y-5">
            {salesFaqItems.map((item) => (
              <article key={item.question} className="rounded-[1.8rem] border border-slate-200 bg-white p-7 shadow-sm shadow-slate-900/5">
                <h2 className="text-2xl font-semibold text-slate-950">{item.question}</h2>
                <p className="mt-4 text-base leading-8 text-slate-600">{item.answer}</p>
              </article>
            ))}
          </div>

          <div className="mt-10 rounded-[2rem] bg-[linear-gradient(135deg,#102d64,#153f8a)] px-6 py-8 text-white shadow-xl shadow-blue-950/15 lg:px-8">
            <div className="flex flex-col gap-5 lg:flex-row lg:items-center lg:justify-between">
              <div className="max-w-3xl">
                <div className="text-xs font-semibold uppercase tracking-[0.2em] text-blue-100">Hâlâ karar aşamasında mısınız?</div>
                <p className="mt-3 text-lg leading-8 text-blue-50">
                  Operasyon yapınıza uygun kurulum ve fiyat önerisini birlikte netleştirelim. Demo görüşmesinde
                  hacminize uygun planı birlikte seçebiliriz.
                </p>
              </div>
              <TrackedLink
                href={PRIMARY_CTA_HREF}
                trackingName="faq-demo-talep"
                className="rounded-full bg-white px-6 py-3 text-sm font-semibold text-slate-900 transition hover:bg-slate-100"
              >
                Demo talep edin
              </TrackedLink>
            </div>
          </div>
        </div>
      </section>
    </div>
  );
}
