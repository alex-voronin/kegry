'use client';

import { useState, useEffect } from 'react';
import { useTranslations } from 'next-intl';
import { useRouter, useParams } from 'next/navigation';
import {
  ChevronDown,
  ChevronRight,
  Plus,
  Medal,
  SquarePen,
  Trash2,
} from 'lucide-react';
import { toast } from 'react-toastify';
import { Header } from '@/components/ui/header';
import { Icon } from '@/components/ui/icon';
import { Spinner } from '@/components/ui/spinner';
import { ListItem } from '@/components/ui/list-item';
import { VenueListItem } from '@/components/b2b/venue-list-item';
import { FormSectionTitle } from '@/components/ui/form-section-title';
import { ModalMenu } from '@/components/ui/modal-menu';
import { ModalDialog } from '@/components/ui/modal-dialog';
import { ButtonVariant } from '@/components/ui/button';
import { useSafeBack } from '@/hooks/use-safe-back';
import stylesCommon from '@/components/ui/common.module.css';
import { Input } from '@/components/ui/input';
import { PAGE_ROUTES } from '@/lib/routes';
import { useCompaniesStore, VenueListItemDto } from '../use-companies-store';
import styles from './profile.module.css';

export default function CompanyProfilePage() {
  const t = useTranslations('screens.companyProfile');
  const router = useRouter();
  const { id } = useParams() as { id: string };
  const EXPANDED_SESSION_STORAGE_KEY = `loyalty_expanded_${id}`;
  const [isLoyaltyExpanded, setIsLoyaltyExpanded] = useState(false);

  const [isMenuOpen, setIsMenuOpen] = useState(false);
  const [isDeleteModalOpen, setIsDeleteModalOpen] = useState(false);
  const [isDeleting, setIsDeleting] = useState(false);

  const { safeBack } = useSafeBack();

  const {
    selectedCompany,
    isProfileLoading: isLoading,
    fetchCompany,
    deleteCompany,
  } = useCompaniesStore();

  const handleBack = () => {
    safeBack(PAGE_ROUTES.B2B.COMPANIES);
  };

  const handleAddVenue = () => {
    router.push(PAGE_ROUTES.B2B.VENUES_CREATE(id));
  };

  const handleVenueClick = (venueId: string) => {
    router.push(PAGE_ROUTES.B2B.VENUES_DETAIL(venueId));
  };

  const handleLoyaltyLevelClick = (levelId: number) => {
    router.push(PAGE_ROUTES.B2B.COMPANY_LOYALTY_LEVEL(id, levelId));
  };

  const menuOptions = [
    {
      icon: <SquarePen size={24} className="text-primary" />,
      label: t('menu.edit'),
      onClick: () => {
        setIsMenuOpen(false);
        router.push(PAGE_ROUTES.B2B.COMPANIES_EDIT(id));
      },
    },
    {
      icon: <Trash2 size={24} className="text-error" />,
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

  useEffect(() => {
    const expanded = sessionStorage.getItem(EXPANDED_SESSION_STORAGE_KEY);
    if (expanded !== null) {
      setIsLoyaltyExpanded(expanded === 'true');
    }
  }, [EXPANDED_SESSION_STORAGE_KEY]);

  useEffect(() => {
    if (id) {
      fetchCompany(id);
    }
  }, [id, fetchCompany]);

  if (isLoading || !selectedCompany) {
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
        />

        <div className="flex flex-1 items-center justify-center">
          <Spinner />
        </div>
      </>
    );
  }

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
          <div className="flex items-center gap-4">
            <SquarePen
              size={24}
              className="cursor-pointer"
              onClick={() => router.push(PAGE_ROUTES.B2B.COMPANIES_EDIT(id))}
            />
            <Icon
              src="/assets/b2b/more-icon.svg"
              alt="more"
              width={24}
              height={24}
              onClick={() => setIsMenuOpen(true)}
            />
          </div>
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
            <FormSectionTitle>{t('name')}</FormSectionTitle>
            <Input
              value={selectedCompany.name}
              readOnly
              className={stylesCommon.inputDark}
            />
          </div>

          <div className={stylesCommon.sectionContainer}>
            <FormSectionTitle>{t('email')}</FormSectionTitle>
            <Input
              value={selectedCompany.email || ''}
              readOnly
              placeholder={t('emailPlaceholder')}
              className={stylesCommon.inputDark}
            />
          </div>

          <div className={styles.listSection}>
            <div className={styles.listHeader}>
              <button
                type="button"
                className="flex w-full items-center justify-between"
                onClick={() => {
                  const nextValue = !isLoyaltyExpanded;
                  setIsLoyaltyExpanded(nextValue);
                  sessionStorage.setItem(
                    EXPANDED_SESSION_STORAGE_KEY,
                    String(nextValue),
                  );
                }}
              >
                <h2 className={styles.listTitle}>{t('loyaltyLevelsTitle')}</h2>
                <ChevronDown
                  size={24}
                  className={`text-secondary transition-transform ${
                    isLoyaltyExpanded ? 'rotate-180' : ''
                  }`}
                />
              </button>
            </div>

            {isLoyaltyExpanded && (
              <div className={styles.list}>
                {selectedCompany.loyaltyLevels.map((level) => (
                  <ListItem
                    key={level.id}
                    className="cursor-pointer"
                    onClick={() => handleLoyaltyLevelClick(level.id)}
                    avatarAlt={t('loyaltyLevelItemTitle', { level: level.id })}
                    title={t('loyaltyLevelItemTitle', { level: level.id })}
                    subtitle={t('loyaltyLevelSubtitle', {
                      visitsUpperBound: level.visitsUpperBound,
                    })}
                    dimmed={!level.isActive}
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

          <div className={styles.listSection}>
            <div className={styles.listHeader}>
              <h2 className={styles.listTitle}>{t('venuesTitle')}</h2>
              <div onClick={handleAddVenue} className="cursor-pointer">
                <Plus size={24} className="text-primary" />
              </div>
            </div>

            {selectedCompany.venues.length === 0 ? (
              <div className={styles.emptyState}>
                <span>{t('noVenues')}</span>
              </div>
            ) : (
              <div className={styles.list}>
                {selectedCompany.venues.map((venue: VenueListItemDto) => (
                  <VenueListItem
                    key={venue.id}
                    className="cursor-pointer"
                    onClick={() => handleVenueClick(venue.id)}
                    imageUrl={
                      venue.coverImageUrl || '/assets/b2b/venue-icon.svg'
                    }
                    imageAlt={venue.address}
                    title={venue.address}
                    dimmed={!venue.isVerified}
                    rightEl={
                      <ChevronRight size={24} className="text-primary" />
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
