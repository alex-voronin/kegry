import { setRequestLocale } from 'next-intl/server';
import DesktopLayout from './_components/desktop-layout';
import MobileLayout from './_components/mobile-layout';
import styles from './layout.module.css';

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
