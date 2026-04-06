import type { Metadata } from "next";
import { Suspense } from "react";
import { Inter } from "next/font/google";
import "./globals.css";
import Header from '@/components/navigation/Header';
import Footer from '@/components/navigation/Footer';
import ToastProvider from '@/components/ToastContainer';
import MarketingAnalyticsTracker from '@/components/analytics/MarketingAnalyticsTracker';
import ThirdPartyAnalytics from '@/components/analytics/ThirdPartyAnalytics';
import { SITE_URL, defaultOgImage } from '@/lib/marketing';

const inter = Inter({
  variable: "--font-inter",
  subsets: ["latin", "latin-ext"]
});

export const metadata: Metadata = {
  metadataBase: new URL(SITE_URL),
  title: {
    default: "YolPilot | Teslimat ve Rota Operasyon Platformu",
    template: "%s | YolPilot"
  },
  description: "Rota optimizasyonu, saha uygulaması, teslimat kanıtı ve müşteri bilgilendirmesini tek platformda yönetin.",
  keywords: ["rota optimizasyonu", "teslimat yönetimi", "araç takibi", "lojistik yazılımı", "Türkiye"],
  authors: [{ name: "YolPilot" }],
  alternates: {
    canonical: SITE_URL
  },
  openGraph: {
    title: "YolPilot | Teslimat ve Rota Operasyon Platformu",
    description: "Rota optimizasyonu, saha uygulaması, teslimat kanıtı ve müşteri bilgilendirmesini tek platformda yönetin.",
    url: SITE_URL,
    type: "website",
    locale: "tr_TR",
    siteName: "YolPilot",
    images: [
      {
        url: defaultOgImage,
        width: 1200,
        height: 630,
        alt: "YolPilot"
      }
    ]
  },
  twitter: {
    card: "summary_large_image",
    title: "YolPilot | Teslimat ve Rota Operasyon Platformu",
    description: "Rota optimizasyonu, saha uygulaması, teslimat kanıtı ve müşteri bilgilendirmesini tek platformda yönetin.",
    images: [defaultOgImage]
  }
};

export default function RootLayout({
  children
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="tr">
      <head>
        <link rel="icon" type="image/x-icon" href="/favicon.ico" />
        <link rel="shortcut icon" type="image/x-icon" href="/favicon.ico" />
      </head>
      <body className={`${inter.variable} font-sans antialiased`}>
        <ToastProvider>
          <ThirdPartyAnalytics />
          <Suspense fallback={null}>
            <MarketingAnalyticsTracker />
          </Suspense>
          <Header />
          <main>{children}</main>
          <Footer />
        </ToastProvider>
      </body>
    </html>
  );
}
