'use client';

import { useRouter } from '@/i18n/routing';
import { PAGE_ROUTES } from '@/lib/routes';
import { MapPinned, Sticker } from 'lucide-react';
import styles from './map-feed-toggle.module.css';

type Props = {
  activeView: 'map' | 'feed';
  className?: string;
};

export function MapFeedToggle({ activeView, className }: Props) {
  const router = useRouter();

  return (
    <div className={className} onClick={(e) => e.stopPropagation()}>
      <div className={styles.pillToggle}>
        <button
          className={`${styles.pillOption} ${activeView === 'map' ? styles.pillOptionActive : ''}`}
          onClick={() => router.push(PAGE_ROUTES.MAP)}
        >
          <MapPinned size={20} />
        </button>
        <button
          className={`${styles.pillOption} ${activeView === 'feed' ? styles.pillOptionActive : ''}`}
          onClick={() =>
            router.push(`${PAGE_ROUTES.FEED}?auto-chat-redirect=false`)
          }
        >
          <Sticker size={20} />
        </button>
      </div>
    </div>
  );
}
