import type { Metadata } from "next";
import { Inter } from "next/font/google";
import "./globals.css";
import Header from '@/components/navigation/Header';
import Footer from '@/components/navigation/Footer';
import ToastProvider from '@/components/ToastContainer';

const inter = Inter({
  variable: "--font-inter",
  subsets: ["latin"]
});

export const metadata: Metadata = {
  title: "YolPilot - Teslimat ve Rota Optimizasyon Platformu",
  description: "Rota optimizasyonu, teslimat takibi ve teslimat kaniti ile operasyonlarinizi tek yerden yonetin.",
  keywords: "rota optimizasyon, teslimat yonetimi, arac takibi, lojistik yazilim, Turkiye",
  authors: [{ name: "YolPilot" }],
  openGraph: {
    title: "YolPilot - Teslimat ve Rota Optimizasyon Platformu",
    description: "Rota optimizasyonu, teslimat takibi ve teslimat kaniti ile operasyonlarinizi tek yerden yonetin.",
    type: "website",
    locale: "tr_TR",
    siteName: "YolPilot"
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
          <Header />
          <main>{children}</main>
          <Footer />
        </ToastProvider>
      </body>
    </html>
  );
}
