'use client';

import { useState, useEffect } from 'react';
import { useTranslations } from 'next-intl';
import { useRouter } from '@/i18n/routing';
import { ChevronRight } from 'lucide-react';
import { toast } from 'react-toastify';
import { Header } from '@/components/ui/header';
import { Icon } from '@/components/ui/icon';
import { Spinner } from '@/components/ui/spinner';
import { ModalMenu } from '@/components/ui/modal-menu';
import { VenueListItem } from '@/components/b2b/venue-list-item';
import { useSidebar, SidebarIcon, Sidebar } from '@/components/b2b/Sidebar';
import { PAGE_ROUTES } from '@/lib/routes';
import { useVenuesStore } from './use-venues-store';
import { useCompaniesStore } from '../companies/use-companies-store';
import styles from '../companies/companies.module.css';

export default function VenuesPage() {
  const router = useRouter();
  const t = useTranslations('screens.b2bVenues');
  const { isOpen, open, close } = useSidebar();

  const { venues, isLoading, fetchVenues } = useVenuesStore();
  const { companies, fetchCompanies } = useCompaniesStore();

  const [isCompanyMenuOpen, setIsCompanyMenuOpen] = useState(false);

  useEffect(() => {
    fetchVenues().catch(() => {
      toast.error(t('toast.loadFailed'));
    });
  }, [fetchVenues, t]);

  const handleVenueClick = (id: string) => {
    router.push(PAGE_ROUTES.B2B.VENUES_DETAIL(id));
  };

  const handleAddVenue = async () => {
    let currentCompanies = companies;
    if (currentCompanies.length === 0) {
      await fetchCompanies();
      currentCompanies = useCompaniesStore.getState().companies;
    }

    if (currentCompanies.length === 0) {
      toast.error(t('toast.noCompanies'));
      return;
    }

    if (currentCompanies.length === 1) {
      router.push(PAGE_ROUTES.B2B.VENUES_CREATE(currentCompanies[0].id));
    } else {
      setIsCompanyMenuOpen(true);
    }
  };

  return (
    <div className="relative flex flex-1 flex-col overflow-hidden">
      <Header
        left={<SidebarIcon onClick={open} />}
        title={t('title')}
        right={
          companies.length > 0 ? (
            <Icon
              src="/assets/b2b/plus-icon.svg"
              alt="add venue"
              width={22}
              height={22}
              onClick={handleAddVenue}
              priority
            />
          ) : undefined
        }
      />

      <Sidebar isOpen={isOpen} onClose={close} />

      <ModalMenu
        isOpen={isCompanyMenuOpen}
        onClose={() => setIsCompanyMenuOpen(false)}
        options={companies.map((company) => ({
          label: company.name,
          icon: company.avatarImageUrl ? (
            <img
              src={company.avatarImageUrl}
              alt={company.name}
              className="h-6 w-6 rounded-full object-cover"
            />
          ) : undefined,
          onClick: () =>
            router.push(PAGE_ROUTES.B2B.VENUES_CREATE(company.id)),
        }))}
      />

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
                badge={venue.isPinCheckInAllowed ? venue.pinCode : undefined}
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
