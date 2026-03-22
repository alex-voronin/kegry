import type { Metadata } from 'next';
import DesktopLayout from './_components/desktop-layout';
import MobileLayout from './_components/mobile-layout';
import styles from './layout.module.css';
import { AgeVerification } from './_components/AgeVerification';
import { ReferrerTracker } from './_components/ReferrerTracker';
import { QrRedirect } from './_components/qr-redirect';
import { Suspense } from 'react';
import { BRAND_NAME } from '@/lib/constants';

export const metadata: Metadata = {
  title: `${BRAND_NAME} — Find Your Bar Buddy`,
  description: `${BRAND_NAME} helps you find bar buddies. No dating, no endless texting - just spontaneous real-life meetups at the bar. The best stories start off-screen!`,

  keywords: [
    'bar buddy',
    'drinking buddy',
    'meet people IRL',
    'real life social',
    'bar meetups',
    'offline friends',
    'kegrycorn',
    'kegry',
    'goblins',
    'off-screen stories',
  ],

  metadataBase: new URL('https://kegry.com'),

  alternates: {
    canonical: '/',
  },

  openGraph: {
    title: `${BRAND_NAME} - Find Your Bar Buddy`,
    description:
      'Real bar buddies, real conversations, no dating. The best stories start off-screen!',
    url: 'https://kegry.com',
    siteName: BRAND_NAME,
    images: [
      {
        url: '/open-graph/default.webp',
        width: 1200,
        height: 675,
        alt: `${BRAND_NAME} - Find people for drinks & good times`,
        type: 'image/webp',
      },
    ],
    locale: 'en_US',
    type: 'website',
  },

  twitter: {
    card: 'summary_large_image',
    title: `${BRAND_NAME} - Find Your Bar Buddy`,
    description: 'The best stories start off-screen. Find your bar buddy now!',
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

export default function LandingPage() {
  return (
    <>
      <Suspense>
        <ReferrerTracker />
        <QrRedirect />
      </Suspense>
      <AgeVerification />
      <div className={styles.desktopOnly}>
        <DesktopLayout />
      </div>
      <div className={styles.mobileOnly}>
        <MobileLayout />
      </div>
    </>
  );
}
