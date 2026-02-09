'use client';

import { useState, useMemo, useEffect, useRef } from 'react';
import { useTranslations } from 'next-intl';
import { useRouter } from '@/i18n/routing';
import { Header } from '@/components/ui/header';
import { Spinner } from '@/components/ui/spinner';
import { Icon } from '@/components/ui/icon';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { SegmentedControl } from '@/components/ui/segmented-control';
import {
  HobbiesSection,
  HobbyOption,
} from '@/components/ui/profile-sections/hobbies-section';
import { ModalDialog } from '@/components/ui/modal-dialog';
import { FormSectionTitle } from '@/components/ui/form-section-title';
import { useFeedFiltersStore, FilterDto } from '@/store/use-feed-filters-store';
import { useSettingsStore, DistanceUnits } from '@/store/use-settings-store';
import {
  metersToKilometers,
  metersToMiles,
  kilometersToMeters,
  milesToMeters,
} from '@/lib/distance-utils';
import { toast } from 'react-toastify';
import styles from './filters.module.css';
import stylesCommon from '@/components/ui/common.module.css';

export default function FiltersScreen() {
  const t = useTranslations('screens.filters');
  const tCommon = useTranslations('common');
  const router = useRouter();
  const {
    filters: serverFilters,
    fetchFilters,
    updateFilters,
    isLoading,
    hasHydrated,
  } = useFeedFiltersStore();

  const distanceUnits = useSettingsStore((state) => state.distanceUnits);
  const isMiles = distanceUnits === DistanceUnits.Miles;

  const initialFilters = useMemo(
    () => ({
      when: 'today',
      distance: '10',
      ageFrom: '',
      ageTo: '',
      gender: 'any',
      hobbies: [] as HobbyOption[],
    }),
    [],
  );

  const [filters, setFilters] = useState(initialFilters);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const isLoaded = useRef(false);

  useEffect(() => {
    if (!hasHydrated) return;

    const loadFilters = async () => {
      try {
        await fetchFilters();
      } catch (err: any) {
        toast.error(err.message || t('loadFailed'));
      }
    };
    loadFilters();
  }, [hasHydrated, fetchFilters, t]);

  useEffect(() => {
    if (serverFilters && !isLoaded.current) {
      const distanceValue = isMiles
        ? metersToMiles(serverFilters.radiusMeters).toString()
        : metersToKilometers(serverFilters.radiusMeters).toString();
      setFilters({
        when: 'today',
        distance: distanceValue,
        ageFrom: serverFilters.ageFrom?.toString() || '',
        ageTo: serverFilters.ageTo?.toString() || '',
        gender: serverFilters.gender || 'any',
        hobbies: serverFilters.hobbyIds.map((id) => ({ value: id, label: '' })),
      });
      isLoaded.current = true;
    }
  }, [serverFilters, isMiles]);

  const hasChanges = useMemo(() => {
    return JSON.stringify(filters) !== JSON.stringify(initialFilters);
  }, [filters, initialFilters]);

  const handleBack = () => {
    if (hasChanges) {
      setIsModalOpen(true);
    } else {
      router.back();
    }
  };

  const handleApply = async () => {
    const radiusMeters = isMiles
      ? milesToMeters(parseFloat(filters.distance))
      : kilometersToMeters(parseFloat(filters.distance));
    const dto: FilterDto = {
      radiusMeters,
      ageFrom: filters.ageFrom ? parseInt(filters.ageFrom) : null,
      ageTo: filters.ageTo ? parseInt(filters.ageTo) : null,
      gender: filters.gender === 'any' ? null : filters.gender,
      hobbyIds: filters.hobbies.map((h) => h.value),
    };

    try {
      await updateFilters(dto);
      toast.success(t('applySuccess'));
      router.back();
    } catch (err: any) {
      toast.error(err.message || 'Failed to update filters');
    }
  };

  const handleDiscard = () => {
    router.back();
  };

  const timeOptions = [
    { value: 'now', label: t('now'), width: 100 },
    { value: 'today', label: t('today'), width: 100 },
    { value: 'tomorrow', label: t('tomorrow'), width: 140 },
  ];

  const genderOptions = [
    { value: 'any', label: t('genderAny'), width: 100 },
    { value: 'male', label: t('male'), width: 100 },
    { value: 'female', label: t('female'), width: 100 },
  ];

  return (
    <div>
      <Header
        left={
          <Icon
            src="/assets/common/disclosure-left-icon.svg"
            alt="back"
            width={20}
            height={26}
            onClick={handleBack}
          />
        }
        title={t('title')}
      />

      {isLoading && !isLoaded.current ? (
        <div className={styles.loaderContainer}>
          <Spinner />
        </div>
      ) : (
        <div className={styles.container}>
          <div className={styles.content}>
            <div className={styles.filterSection}>
              <FormSectionTitle>{t('when')}</FormSectionTitle>
              <SegmentedControl
                options={timeOptions}
                value={filters.when}
                onChange={(val) => setFilters({ ...filters, when: val })}
              />
            </div>

            <div className={styles.filterSection}>
              <FormSectionTitle>
                {t('distance')} (
                {isMiles ? tCommon('units.miles') : tCommon('units.kilometers')}
                )
              </FormSectionTitle>
              <Input
                type="number"
                value={filters.distance}
                onChange={(e) =>
                  setFilters({ ...filters, distance: e.target.value })
                }
                className={stylesCommon.inputDark}
              />
            </div>

            <div className={styles.filterSection}>
              <FormSectionTitle>{t('age')}</FormSectionTitle>
              <div className={styles.ageRangeWrapper}>
                <div className={styles.ageInputGroup}>
                  <span className={styles.ageLabel}>{t('from')}</span>
                  <Input
                    type="number"
                    value={filters.ageFrom}
                    placeholder={t('any')}
                    onChange={(e) =>
                      setFilters({ ...filters, ageFrom: e.target.value })
                    }
                    className={styles.ageInput}
                  />
                </div>
                <div className={styles.ageInputGroup}>
                  <span className={styles.ageLabel}>{t('to')}</span>
                  <Input
                    type="number"
                    value={filters.ageTo}
                    placeholder={t('any')}
                    onChange={(e) =>
                      setFilters({ ...filters, ageTo: e.target.value })
                    }
                    className={styles.ageInput}
                  />
                </div>
              </div>
            </div>

            <div className={styles.filterSection}>
              <FormSectionTitle>{t('gender')}</FormSectionTitle>
              <SegmentedControl
                options={genderOptions}
                value={filters.gender}
                onChange={(val) => setFilters({ ...filters, gender: val })}
              />
            </div>

            <div className={styles.filterSection}>
              <HobbiesSection
                value={filters.hobbies}
                onChange={(hobbies) => setFilters({ ...filters, hobbies })}
              />
            </div>

            <div className={styles.applyButtonWrapper}>
              <Button
                onClick={handleApply}
                className={styles.applyButton}
                loading={isLoading}
              >
                {t('apply')}
              </Button>
            </div>
          </div>

          <ModalDialog
            isOpen={isModalOpen}
            onClose={() => setIsModalOpen(false)}
            message={t('applyDialog.title')}
            primaryButtonText={t('applyDialog.buttonYes')}
            secondaryButtonText={t('applyDialog.buttonNo')}
            onPrimaryAction={handleApply}
            onSecondaryAction={handleDiscard}
          />
        </div>
      )}
    </div>
  );
}
