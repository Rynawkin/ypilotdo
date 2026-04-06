import type { Metadata } from 'next';
import { defaultOgImage, SITE_URL } from './marketing';

type PageMetadataInput = {
  title: string;
  description: string;
  path?: string;
  keywords?: string[];
};

export function createPageMetadata({ title, description, path = '/', keywords = [] }: PageMetadataInput): Metadata {
  const url = new URL(path, SITE_URL).toString();

  return {
    title,
    description,
    keywords,
    alternates: {
      canonical: url
    },
    openGraph: {
      title,
      description,
      url,
      siteName: 'YolPilot',
      locale: 'tr_TR',
      type: 'website',
      images: [
        {
          url: defaultOgImage,
          width: 1200,
          height: 630,
          alt: title
        }
      ]
    },
    twitter: {
      card: 'summary_large_image',
      title,
      description,
      images: [defaultOgImage]
    }
  };
}
