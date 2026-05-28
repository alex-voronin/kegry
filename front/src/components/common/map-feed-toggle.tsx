'use client';

import { useRouter } from '@/i18n/routing';
import { useTranslations } from 'next-intl';
import { PAGE_ROUTES } from '@/lib/routes';
import styles from './map-feed-toggle.module.css';

type Props = {
  activeView: 'map' | 'feed';
  className?: string;
};

export function MapFeedToggle({ activeView, className }: Props) {
  const router = useRouter();
  const t = useTranslations('screens.map');

  return (
    <div className={className} onClick={(e) => e.stopPropagation()}>
      <div className={styles.pillToggle}>
        <button
          className={`${styles.pillOption} ${activeView === 'map' ? styles.pillOptionActive : ''}`}
          onClick={() => router.push(PAGE_ROUTES.MAP)}
        >
          {t('mapLabel')}
        </button>
        <button
          className={`${styles.pillOption} ${activeView === 'feed' ? styles.pillOptionActive : ''}`}
          onClick={() => router.push(PAGE_ROUTES.FEED)}
        >
          {t('feed')}
        </button>
      </div>
    </div>
  );
}
