import React, { useEffect, useMemo, useState } from 'react';
import {
  BarChart3,
  Eye,
  LayoutTemplate,
  MapPinned,
  MousePointerClick,
  Send,
  TrendingUp,
  Users
} from 'lucide-react';
import toast from 'react-hot-toast';
import { adminService } from '@/services/admin.service';

interface OverviewResponse {
  rangeDays: number;
  totals: {
    totalVisitors: number;
    totalSessions: number;
    pageViews: number;
    ctaClicks: number;
    formSubmits: number;
    leads: number;
    visitorToLeadRate: number;
  };
  topPages: Array<{ page: string; views: number; visitors: number }>;
  topCampaigns: Array<{ source: string; medium: string; campaign: string; sessions: number; visitors: number }>;
  topLocations: Array<{ city: string; region: string; country: string; sessions: number; visitors: number; pageViews: number }>;
  trend: Array<{ date: string; pageViews: number; ctaClicks: number; formSubmits: number; visitors: number }>;
  leadTrend: Array<{ date: string; leads: number }>;
  recentSessions: Array<{
    sessionId: string;
    visitorId: string;
    firstSeen: string;
    lastSeen: string;
    landingPage: string | null;
    source: string;
    medium: string;
    campaign: string;
    ipAddress?: string | null;
    city?: string | null;
    region?: string | null;
    country?: string | null;
    referrer?: string | null;
    browser?: string | null;
    os?: string | null;
    deviceType?: string | null;
    pageViews: number;
    ctaClicks: number;
    formSubmits: number;
  }>;
}

interface RecentLead {
  id: number;
  name: string;
  email: string;
  company: string;
  source: string;
  selectedPlan?: string;
  status: number;
  landingPage?: string;
  utmSource?: string;
  utmMedium?: string;
  utmCampaign?: string;
  createdAt: string;
}

const MarketingAnalyticsDashboard: React.FC = () => {
  const [days, setDays] = useState(30);
  const [loading, setLoading] = useState(true);
  const [overview, setOverview] = useState<OverviewResponse | null>(null);
  const [recentLeads, setRecentLeads] = useState<RecentLead[]>([]);

  useEffect(() => {
    const load = async () => {
      setLoading(true);
      try {
        const [overviewData, leadsData] = await Promise.all([
          adminService.getMarketingAnalyticsOverview(days),
          adminService.getRecentMarketingAnalyticsLeads(15)
        ]);

        setOverview(overviewData);
        setRecentLeads(leadsData);
      } catch (error) {
        console.error('Error loading marketing analytics:', error);
        toast.error('Marketing analytics verileri yüklenemedi');
      } finally {
        setLoading(false);
      }
    };

    load();
  }, [days]);

  const statCards = useMemo(() => {
    if (!overview) return [];

    return [
      {
        label: 'Ziyaretçi',
        value: overview.totals.totalVisitors,
        icon: Users,
        tone: 'bg-blue-50 text-blue-700 border-blue-100'
      },
      {
        label: 'Oturum',
        value: overview.totals.totalSessions,
        icon: Eye,
        tone: 'bg-slate-50 text-slate-700 border-slate-200'
      },
      {
        label: 'Sayfa Gösterimi',
        value: overview.totals.pageViews,
        icon: LayoutTemplate,
        tone: 'bg-emerald-50 text-emerald-700 border-emerald-100'
      },
      {
        label: 'CTA Tıklaması',
        value: overview.totals.ctaClicks,
        icon: MousePointerClick,
        tone: 'bg-amber-50 text-amber-700 border-amber-100'
      },
      {
        label: 'Form Gönderimi',
        value: overview.totals.formSubmits,
        icon: Send,
        tone: 'bg-violet-50 text-violet-700 border-violet-100'
      },
      {
        label: 'Lead Oranı',
        value: `%${overview.totals.visitorToLeadRate}`,
        icon: TrendingUp,
        tone: 'bg-rose-50 text-rose-700 border-rose-100'
      }
    ];
  }, [overview]);

  if (loading && !overview) {
    return (
      <div className="flex min-h-[50vh] items-center justify-center">
        <div className="text-center">
          <div className="mx-auto h-12 w-12 animate-spin rounded-full border-b-2 border-blue-600" />
          <p className="mt-4 text-sm text-slate-500">Marketing analytics yükleniyor...</p>
        </div>
      </div>
    );
  }

  if (!overview) {
    return <div className="app-card p-6 text-sm text-slate-500">Analytics verisi bulunamadı.</div>;
  }

  const maxTrendVisitors = Math.max(...overview.trend.map((item) => item.visitors), 1);

  return (
    <div className="space-y-6">
      <div className="app-surface flex flex-col gap-4 p-6 lg:flex-row lg:items-end lg:justify-between">
        <div>
          <div className="app-eyebrow">Marketing Analytics</div>
          <h1 className="mt-2 text-3xl font-semibold text-slate-950">Reklam trafiği ve landing performansı</h1>
          <p className="mt-3 max-w-3xl text-sm leading-6 text-slate-600">
            Ziyaretçi, oturum, CTA tıklaması, form gönderimi, kampanya kaynağı, IP ve şehir bazlı ziyaretleri tek yerden izleyin.
          </p>
        </div>
        <div className="flex items-center gap-3">
          <select
            value={days}
            onChange={(event) => setDays(Number(event.target.value))}
            className="rounded-2xl border border-slate-200 bg-white px-4 py-3 text-sm font-medium text-slate-700 shadow-sm outline-none transition focus:border-blue-300"
          >
            <option value={7}>Son 7 gün</option>
            <option value={14}>Son 14 gün</option>
            <option value={30}>Son 30 gün</option>
            <option value={60}>Son 60 gün</option>
            <option value={90}>Son 90 gün</option>
          </select>
        </div>
      </div>

      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-6">
        {statCards.map((card) => {
          const Icon = card.icon;
          return (
            <div key={card.label} className="app-card p-5">
              <div className={`inline-flex rounded-2xl border px-3 py-3 ${card.tone}`}>
                <Icon className="h-5 w-5" />
              </div>
              <div className="mt-4 text-sm font-medium text-slate-500">{card.label}</div>
              <div className="mt-2 text-3xl font-semibold text-slate-950">{card.value}</div>
            </div>
          );
        })}
      </div>

      <div className="grid gap-6 xl:grid-cols-[minmax(0,1.1fr)_minmax(0,0.9fr)]">
        <div className="app-card p-6">
          <div className="flex items-center gap-3">
            <div className="rounded-2xl bg-blue-50 p-3 text-blue-700">
              <BarChart3 className="h-5 w-5" />
            </div>
            <div>
              <h2 className="text-lg font-semibold text-slate-950">Günlük ziyaretçi ve etkileşim</h2>
              <p className="text-sm text-slate-500">Sayfa gösterimi, CTA tıklaması ve form gönderimi</p>
            </div>
          </div>

          <div className="mt-6 space-y-4">
            {overview.trend.slice(-10).map((item) => (
              <div key={item.date} className="space-y-2">
                <div className="flex items-center justify-between text-sm">
                  <span className="font-medium text-slate-700">
                    {new Date(item.date).toLocaleDateString('tr-TR', { day: '2-digit', month: 'short' })}
                  </span>
                  <div className="flex gap-4 text-xs text-slate-500">
                    <span>{item.visitors} ziyaretçi</span>
                    <span>{item.ctaClicks} CTA</span>
                    <span>{item.formSubmits} form</span>
                  </div>
                </div>
                <div className="h-2 overflow-hidden rounded-full bg-slate-100">
                  <div
                    className="h-full rounded-full bg-gradient-to-r from-blue-600 to-cyan-500"
                    style={{ width: `${(item.visitors / maxTrendVisitors) * 100}%` }}
                  />
                </div>
              </div>
            ))}
          </div>
        </div>

        <div className="app-card p-6">
          <h2 className="text-lg font-semibold text-slate-950">En çok trafik getiren kampanyalar</h2>
          <div className="mt-5 space-y-3">
            {overview.topCampaigns.length > 0 ? (
              overview.topCampaigns.map((item) => (
                <div key={`${item.source}-${item.medium}-${item.campaign}`} className="rounded-2xl border border-slate-100 bg-slate-50 px-4 py-4">
                  <div className="flex items-center justify-between gap-3">
                    <div>
                      <div className="text-sm font-semibold text-slate-900">{item.campaign}</div>
                      <div className="text-xs text-slate-500">
                        {item.source} / {item.medium}
                      </div>
                    </div>
                    <div className="text-right text-xs text-slate-500">
                      <div>{item.sessions} oturum</div>
                      <div>{item.visitors} ziyaretçi</div>
                    </div>
                  </div>
                </div>
              ))
            ) : (
              <div className="rounded-2xl border border-dashed border-slate-200 px-4 py-6 text-sm text-slate-500">
                Henüz kampanya verisi yok.
              </div>
            )}
          </div>
        </div>
      </div>

      <div className="grid gap-6 xl:grid-cols-2">
        <div className="app-card p-6">
          <h2 className="text-lg font-semibold text-slate-950">En çok görüntülenen landing sayfalar</h2>
          <div className="mt-5 space-y-3">
            {overview.topPages.map((item) => (
              <div key={item.page} className="flex items-center justify-between gap-4 rounded-2xl border border-slate-100 bg-white px-4 py-4">
                <div className="min-w-0">
                  <div className="truncate text-sm font-semibold text-slate-900">{item.page || '/'}</div>
                  <div className="text-xs text-slate-500">{item.visitors} benzersiz ziyaretçi</div>
                </div>
                <div className="text-sm font-semibold text-blue-700">{item.views} görüntüleme</div>
              </div>
            ))}
          </div>
        </div>

        <div className="app-card p-6">
          <div className="flex items-center gap-3">
            <div className="rounded-2xl bg-sky-50 p-3 text-sky-700">
              <MapPinned className="h-5 w-5" />
            </div>
            <div>
              <h2 className="text-lg font-semibold text-slate-950">En çok trafik gelen şehirler</h2>
              <p className="text-sm text-slate-500">IP başlıklarından çözümlenen lokasyon dağılımı</p>
            </div>
          </div>
          <div className="mt-5 space-y-3">
            {overview.topLocations.length > 0 ? (
              overview.topLocations.map((item) => (
                <div
                  key={`${item.city}-${item.region}-${item.country}`}
                  className="rounded-2xl border border-slate-100 bg-white px-4 py-4"
                >
                  <div className="flex items-center justify-between gap-4">
                    <div className="min-w-0">
                      <div className="truncate text-sm font-semibold text-slate-900">
                        {item.city !== 'Bilinmiyor' ? item.city : item.region}
                      </div>
                      <div className="truncate text-xs text-slate-500">
                        {[item.region, item.country].filter((value) => value && value !== 'Bilinmiyor').join(' · ') || 'Konum yok'}
                      </div>
                    </div>
                    <div className="text-right text-xs text-slate-500">
                      <div>{item.visitors} ziyaretçi</div>
                      <div>{item.sessions} oturum</div>
                    </div>
                  </div>
                </div>
              ))
            ) : (
              <div className="rounded-2xl border border-dashed border-slate-200 px-4 py-6 text-sm text-slate-500">
                Henüz şehir verisi yok. Yeni ziyaretler geldikçe burada görünecek.
              </div>
            )}
          </div>
        </div>
      </div>

      <div className="app-card p-6">
        <h2 className="text-lg font-semibold text-slate-950">Son gelen lead&apos;ler</h2>
        <div className="mt-5 space-y-3">
          {recentLeads.map((lead) => (
            <div key={lead.id} className="rounded-2xl border border-slate-100 bg-white px-4 py-4">
              <div className="flex items-center justify-between gap-4">
                <div className="min-w-0">
                  <div className="truncate text-sm font-semibold text-slate-900">{lead.name} · {lead.company}</div>
                  <div className="truncate text-xs text-slate-500">
                    {lead.utmSource || 'direct'} / {lead.utmMedium || 'none'} / {lead.utmCampaign || 'organic'}
                  </div>
                </div>
                <div className="text-xs text-slate-500">
                  {new Date(lead.createdAt).toLocaleDateString('tr-TR')}
                </div>
              </div>
              <div className="mt-3 flex flex-wrap gap-2 text-xs text-slate-500">
                {lead.landingPage && <span className="rounded-full bg-slate-100 px-3 py-1">{lead.landingPage}</span>}
                {lead.selectedPlan && <span className="rounded-full bg-blue-50 px-3 py-1 text-blue-700">{lead.selectedPlan}</span>}
                <span className="rounded-full bg-slate-100 px-3 py-1">{lead.source}</span>
              </div>
            </div>
          ))}
        </div>
      </div>

      <div className="app-card p-6">
        <h2 className="text-lg font-semibold text-slate-950">Son oturumlar</h2>
        <div className="mt-5 overflow-x-auto">
          <table className="min-w-full divide-y divide-slate-200 text-sm">
            <thead>
              <tr className="text-left text-xs uppercase tracking-[0.18em] text-slate-500">
                <th className="pb-3 pr-4">Landing</th>
                <th className="pb-3 pr-4">Kaynak</th>
                <th className="pb-3 pr-4">Şehir</th>
                <th className="pb-3 pr-4">IP / Cihaz</th>
                <th className="pb-3 pr-4">Sayfa</th>
                <th className="pb-3 pr-4">CTA</th>
                <th className="pb-3 pr-4">Form</th>
                <th className="pb-3 pr-0">Son Görülme</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {overview.recentSessions.map((session) => (
                <tr key={session.sessionId}>
                  <td className="py-4 pr-4">
                    <div className="max-w-[260px] truncate font-medium text-slate-900">{session.landingPage || '/'}</div>
                    <div className="text-xs text-slate-500">{session.visitorId.slice(0, 8)}...</div>
                  </td>
                  <td className="py-4 pr-4">
                    <div className="font-medium text-slate-700">{session.source}</div>
                    <div className="text-xs text-slate-500">{session.medium} · {session.campaign}</div>
                  </td>
                  <td className="py-4 pr-4">
                    <div className="font-medium text-slate-700">{session.city || session.region || '-'}</div>
                    <div className="text-xs text-slate-500">{session.country || '-'}</div>
                  </td>
                  <td className="py-4 pr-4">
                    <div className="font-medium text-slate-700">{session.ipAddress || '-'}</div>
                    <div className="text-xs text-slate-500">
                      {[session.deviceType, session.browser, session.os].filter(Boolean).join(' · ') || '-'}
                    </div>
                  </td>
                  <td className="py-4 pr-4">{session.pageViews}</td>
                  <td className="py-4 pr-4">{session.ctaClicks}</td>
                  <td className="py-4 pr-4">{session.formSubmits}</td>
                  <td className="py-4 pr-0 text-slate-500">
                    {new Date(session.lastSeen).toLocaleString('tr-TR', { day: '2-digit', month: 'short', hour: '2-digit', minute: '2-digit' })}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
};

export default MarketingAnalyticsDashboard;
