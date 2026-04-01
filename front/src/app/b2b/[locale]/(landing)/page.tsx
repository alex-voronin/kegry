import { Metadata } from 'next';
import { setRequestLocale } from 'next-intl/server';
import DesktopLayout from './_components/desktop-layout';
import MobileLayout from './_components/mobile-layout';
import styles from './layout.module.css';
import { BRAND_NAME } from '@/lib/constants';

export const metadata: Metadata = {
  title: `${BRAND_NAME} for Business`,
  description: `${BRAND_NAME} for Business is an all-in-one growth platform for bars. We help you attract more customers, build loyalty, and increase revenue.`,

  keywords: [
    'bar buddy',
    'bar meetups',
    'kegrycorn',
    'kegry',
    'manage venues',
    'venue management',
    'loyalty program',
    'manage discounts',
    'bar promotions',
  ],

  metadataBase: new URL('https://business.kegry.com'),

  alternates: {
    canonical: '/',
  },

  openGraph: {
    title: `${BRAND_NAME} for Business`,
    description: `${BRAND_NAME} for Business is an all-in-one growth platform for bars. We help you attract more customers, build loyalty, and increase revenue.`,
    url: 'https://business.kegry.com',
    siteName: BRAND_NAME,
    images: [
      {
        url: '/open-graph/default.webp',
        width: 1200,
        height: 675,
        alt: `${BRAND_NAME} for Business`,
        type: 'image/webp',
      },
    ],
    locale: 'en_US',
    type: 'website',
  },

  twitter: {
    card: 'summary_large_image',
    title: `${BRAND_NAME} for Business`,
    description: `${BRAND_NAME} for Business is an all-in-one growth platform for bars. We help you attract more customers, build loyalty, and increase revenue.`,
    images: ['/open-graph/default.webp'],
  },

  robots: {
    index: true,
    follow: true,
    googleBot: {
      index: true,
      follow: true,
      'max-image-preview': 'large',
    },
  },
};

interface B2BLandingPageProps {
  params: Promise<{
    locale: string;
  }>;
}

export default async function B2BLandingPage({ params }: B2BLandingPageProps) {
  const { locale } = await params;
  setRequestLocale(locale);

  return (
    <>
      <div className={styles.desktopOnly}>
        <DesktopLayout />
      </div>
      <div className={styles.mobileOnly}>
        <MobileLayout />
      </div>
    </>
  );
}
