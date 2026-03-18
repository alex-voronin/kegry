'use client';

import { useState, useEffect } from 'react';
import { useTranslations } from 'next-intl';
import { useRouter, useParams, useSearchParams } from 'next/navigation';
import { ChevronDown, ChevronRight, Plus, Medal } from 'lucide-react';
import { toast } from 'react-toastify';
import { Header } from '@/components/ui/header';
import { Icon } from '@/components/ui/icon';
import { Spinner } from '@/components/ui/spinner';
import { ListItem } from '@/components/ui/list-item';
import { FormSectionTitle } from '@/components/ui/form-section-title';
import { ModalMenu } from '@/components/ui/modal-menu';
import { ModalDialog } from '@/components/ui/modal-dialog';
import { ButtonVariant } from '@/components/ui/button';
import stylesCommon from '@/components/ui/common.module.css';
import { Input } from '@/components/ui/input';
import { PAGE_ROUTES } from '@/lib/routes';
import { useCompanyProfileStore } from './use-company-profile-store';
import styles from './profile.module.css';

export default function CompanyProfilePage() {
  const t = useTranslations('screens.companyProfile');
  const tCreate = useTranslations('screens.createCompany');
  const router = useRouter();
  const { id } = useParams() as { id: string };
  const searchParams = useSearchParams();

  const [isMenuOpen, setIsMenuOpen] = useState(false);
  const [isDeleteModalOpen, setIsDeleteModalOpen] = useState(false);
  const [isDeleting, setIsDeleting] = useState(false);
  const [isLoyaltyExpanded, setIsLoyaltyExpanded] = useState(
    searchParams.get('expanded') === 'loyalty',
  );

  const { selectedCompany, isLoading, fetchCompany, deleteCompany } =
    useCompanyProfileStore();

  useEffect(() => {
    if (searchParams.get('expanded')) {
      const url = new URL(window.location.href);
      url.searchParams.delete('expanded');
      window.history.replaceState({}, '', url.toString());
    }
  }, [searchParams]);

  useEffect(() => {
    if (id) {
      fetchCompany(id);
    }
  }, [id, fetchCompany]);

  if (isLoading || !selectedCompany) {
    return (
      <div className="flex flex-1 items-center justify-center">
        <Spinner />
      </div>
    );
  }

  const handleBack = () => {
    router.back();
  };

  const handleAddVenue = () => {
    // Placeholder for adding venue
    console.log('Add venue for company:', id);
  };

  const handleVenueClick = (venueId: string) => {
    // Placeholder for venue detail
    console.log('Venue click:', venueId);
  };

  const handleLoyaltyLevelClick = (levelId: number) => {
    router.push(PAGE_ROUTES.B2B.COMPANY_LOYALTY_LEVEL(id, levelId));
  };

  const menuOptions = [
    {
      icon: '/assets/b2b/edit-icon.svg',
      label: t('menu.edit'),
      onClick: () => {
        setIsMenuOpen(false);
        router.push(PAGE_ROUTES.B2B.COMPANIES_EDIT(id));
      },
    },
    {
      icon: '/assets/b2b/delete-icon.svg',
      label: t('menu.delete'),
      onClick: () => {
        setIsMenuOpen(false);
        setIsDeleteModalOpen(true);
      },
      isDestructive: true,
    },
  ];

  const handleDeleteConfirm = async () => {
    setIsDeleting(true);
    try {
      await deleteCompany(id);
      router.push(PAGE_ROUTES.B2B.COMPANIES);
    } catch (err: any) {
      toast.error('Failed to delete company');
    } finally {
      setIsDeleting(false);
      setIsDeleteModalOpen(false);
    }
  };

  return (
    <>
      <Header
        title={t('title')}
        left={
          <Icon
            src="/assets/b2b/disclosure-left-icon.svg"
            alt="back"
            width={14}
            height={27}
            onClick={handleBack}
          />
        }
        right={
          <Icon
            src="/assets/b2b/more-icon.svg"
            alt="more"
            width={24}
            height={24}
            onClick={() => setIsMenuOpen(true)}
          />
        }
      />

      <div className={styles.wrapper}>
        <div className={styles.form}>
          <div className={stylesCommon.sectionContainer}>
            <div className={styles.avatarSection}>
              <div className={styles.avatarContainer}>
                <img
                  src={
                    selectedCompany.avatarImageUrl ||
                    '/assets/b2b/avatar-placeholder.webp'
                  }
                  alt="avatar"
                  className={styles.avatarImage}
                />
              </div>
            </div>
          </div>

          <div className={stylesCommon.sectionContainer}>
            <FormSectionTitle>{tCreate('name')}</FormSectionTitle>
            <Input
              value={selectedCompany.name}
              readOnly
              className={stylesCommon.inputDark}
            />
          </div>

          <div className={stylesCommon.sectionContainer}>
            <FormSectionTitle>{tCreate('email')}</FormSectionTitle>
            <Input
              value={selectedCompany.email || ''}
              readOnly
              placeholder={tCreate('emailPlaceholder')}
              className={stylesCommon.inputDark}
            />
          </div>

          <div className={styles.venuesSection}>
            <div className={styles.venuesHeader}>
              <button
                type="button"
                className="flex w-full items-center justify-between"
                onClick={() => setIsLoyaltyExpanded((v) => !v)}
              >
                <h2 className={styles.venuesTitle}>
                  {t('loyaltyLevelsTitle')}
                </h2>
                <ChevronDown
                  size={24}
                  className={`text-secondary transition-transform ${
                    isLoyaltyExpanded ? 'rotate-180' : ''
                  }`}
                />
              </button>
            </div>

            {isLoyaltyExpanded && (
              <div className={styles.venueList}>
                {selectedCompany.loyaltyLevels.map((level) => (
                  <ListItem
                    key={level.id}
                    className="cursor-pointer"
                    onClick={() => handleLoyaltyLevelClick(level.id)}
                    avatarAlt={`level ${level.id}`}
                    title={`Level ${level.id}`}
                    subtitle={t('loyaltyLevelSubtitle', {
                      visitsUpperBound: level.visitsUpperBound,
                    })}
                    expired={!level.isActive}
                    leadingIcon={
                      <Medal
                        size={24}
                        className={
                          level.isActive ? 'text-primary' : 'text-secondary'
                        }
                      />
                    }
                    rightEl={
                      <ChevronRight size={24} className="text-secondary" />
                    }
                  />
                ))}
              </div>
            )}
          </div>

          <div className={styles.venuesSection}>
            <div className={styles.venuesHeader}>
              <h2 className={styles.venuesTitle}>{t('venuesTitle')}</h2>
              <div onClick={handleAddVenue} className="cursor-pointer">
                <Plus size={24} className="text-primary" />
              </div>
            </div>

            {selectedCompany.venues.length === 0 ? (
              <div className={styles.emptyState}>
                <span>{t('noVenues')}</span>
              </div>
            ) : (
              <div className={styles.venueList}>
                {selectedCompany.venues.map((venue: any) => (
                  <ListItem
                    key={venue.id}
                    className="cursor-pointer"
                    onClick={() => handleVenueClick(venue.id)}
                    avatarUrl={
                      venue.coverImageUrl || '/assets/b2b/venue-icon.svg'
                    }
                    avatarAlt={venue.address}
                    title={venue.address}
                    subtitle=""
                    rightEl={
                      <ChevronRight size={24} className="text-secondary" />
                    }
                  />
                ))}
              </div>
            )}
          </div>
        </div>
      </div>

      <ModalMenu
        isOpen={isMenuOpen}
        onClose={() => setIsMenuOpen(false)}
        options={menuOptions}
      />

      <ModalDialog
        isOpen={isDeleteModalOpen}
        onClose={() => setIsDeleteModalOpen(false)}
        message={t('deleteDialog.title')}
        primaryButtonText={t('deleteDialog.buttonNo')}
        secondaryButtonText={t('deleteDialog.buttonYes')}
        secondaryButtonVariant={ButtonVariant.Destructive}
        onPrimaryAction={() => setIsDeleteModalOpen(false)}
        onSecondaryAction={handleDeleteConfirm}
      />
    </>
  );
}
