'use client';

import { useEffect } from 'react';
import { useTranslations } from 'next-intl';
import { useRouter } from '@/i18n/routing';
import { ChevronRight } from 'lucide-react';
import { toast } from 'react-toastify';
import { Header } from '@/components/ui/header';
import { Spinner } from '@/components/ui/spinner';
import { VenueListItem } from '@/components/b2b/venue-list-item';
import { useSidebar, SidebarIcon, Sidebar } from '@/components/b2b/Sidebar';
import { PAGE_ROUTES } from '@/lib/routes';
import { useVenuesStore } from './use-venues-store';
import styles from '../companies/companies.module.css';

export default function VenuesPage() {
  const router = useRouter();
  const t = useTranslations('screens.b2bVenues');
  const tCommon = useTranslations('common');
  const { isOpen, open, close } = useSidebar();

  const { venues, isLoading, fetchVenues } = useVenuesStore();

  useEffect(() => {
    fetchVenues().catch(() => {
      toast.error(t('toast.loadFailed'));
    });
  }, [fetchVenues, t]);

  const handleVenueClick = (id: string) => {
    router.push(PAGE_ROUTES.B2B.VENUES_DETAIL(id));
  };

  return (
    <div className="relative flex flex-1 flex-col overflow-hidden">
      <Header left={<SidebarIcon onClick={open} />} title={t('title')} />

      <Sidebar isOpen={isOpen} onClose={close} />

      <div className={styles.content}>
        {isLoading && venues.length === 0 ? (
          <div className={styles.emptyState}>
            <Spinner />
          </div>
        ) : venues.length === 0 ? (
          <div
            className={`${styles.emptyState} cursor-pointer`}
            onClick={() => router.push(PAGE_ROUTES.B2B.COMPANIES)}
          >
            <span className={styles.emptyStateText}>{t('noVenues')}</span>
          </div>
        ) : (
          <div className={styles.companyList}>
            {venues.map((venue) => (
              <VenueListItem
                key={venue.id}
                className="cursor-pointer"
                onClick={() => handleVenueClick(venue.id)}
                imageUrl={venue.coverImageUrl || '/assets/b2b/venue-icon.svg'}
                imageAlt={venue.address}
                title={venue.address}
                badge={venue.pinCode}
                dimmed={!venue.isVerified}
                rightEl={<ChevronRight size={24} className="text-primary" />}
              />
            ))}
          </div>
        )}
      </div>
    </div>
  );
}
