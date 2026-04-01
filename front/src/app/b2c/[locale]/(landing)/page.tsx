import type { Metadata } from 'next';
import DesktopLayout from './_components/desktop-layout';
import MobileLayout from './_components/mobile-layout';
import styles from './layout.module.css';
import { AgeVerification } from './_components/AgeVerification';
import { ReferrerTracker } from './_components/ReferrerTracker';
import { QrRedirect } from './_components/qr-redirect';
import { Suspense } from 'react';
import { BRAND_NAME } from '@/lib/constants';
import { getTranslations, setRequestLocale } from 'next-intl/server';

interface LandingPageProps {
  params: Promise<{
    locale: string;
  }>;
}

export async function generateMetadata({
  params,
}: LandingPageProps): Promise<Metadata> {
  const { locale } = await params;
  const t = await getTranslations({
    locale,
    namespace: 'screens.landing.metadata',
  });

  return {
    title: t('title'),
    description: t('description'),

    keywords: t('keywords')
      .split(',')
      .map((k) => k.trim()),

    metadataBase: new URL('https://kegry.com'),

    alternates: {
      canonical: locale === 'sr' ? '/sr' : '/',
    },

    openGraph: {
      title: t('title'),
      description: t('description'),
      url: 'https://kegry.com',
      siteName: BRAND_NAME,
      images: [
        {
          url: '/open-graph/default.webp',
          width: 1200,
          height: 675,
          alt: t('title'),
          type: 'image/webp',
        },
      ],
      locale: locale === 'sr' ? 'sr_RS' : 'en_US',
      type: 'website',
    },

    twitter: {
      card: 'summary_large_image',
      title: t('title'),
      description: t('description'),
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
}

export default async function LandingPage({ params }: LandingPageProps) {
  const { locale } = await params;
  setRequestLocale(locale);

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
