import { Metadata } from 'next';
import { setRequestLocale } from 'next-intl/server';
import DesktopLayout from './_components/desktop-layout';
import MobileLayout from './_components/mobile-layout';
import styles from './layout.module.css';
import { BRAND_NAME } from '@/lib/constants';
import { getTranslations } from 'next-intl/server';
import { AutoSignInRedirect } from '@/components/common/auto-signin-redirect';

interface B2BLandingPageProps {
  params: Promise<{
    locale: string;
  }>;
}

export async function generateMetadata({
  params,
}: B2BLandingPageProps): Promise<Metadata> {
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

    metadataBase: new URL('https://business.kegry.rs'),

    alternates: {
      canonical: locale === 'en' ? '/en' : '/',
    },

    openGraph: {
      title: t('title'),
      description: t('description'),
      url: 'https://business.kegry.rs',
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

export default async function B2BLandingPage({ params }: B2BLandingPageProps) {
  const { locale } = await params;
  setRequestLocale(locale);

  return (
    <>
      <AutoSignInRedirect isB2B={true} />
      <div className={styles.desktopOnly}>
        <DesktopLayout />
      </div>
      <div className={styles.mobileOnly}>
        <MobileLayout />
      </div>
    </>
  );
}
