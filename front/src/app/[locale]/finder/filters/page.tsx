'use client';

import { useState, useMemo, useEffect } from 'react';
import { useTranslations } from 'next-intl';
import { useRouter } from '@/i18n/routing';
import { Header } from '@/components/ui/header';
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
import styles from './filters.module.css';
import stylesCommon from '@/components/ui/common.module.css';

export default function FiltersScreen() {
  const t = useTranslations('screens.filters');
  const router = useRouter();

  const initialFilters = {
    when: 'today',
    distance: '10',
    ageFrom: '',
    ageTo: '',
    gender: 'any',
    hobbies: [] as HobbyOption[],
  };

  const [filters, setFilters] = useState(initialFilters);
  const [isModalOpen, setIsModalOpen] = useState(false);

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

  const handleApply = () => {
    console.log('Applying filters:', filters);
    router.back();
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
    <div className={styles.container}>
      <Header
        left={
          <Icon
            src="/assets/finder/filters/disclosure-left-icon.svg"
            alt="back"
            width={20}
            height={26}
            onClick={handleBack}
          />
        }
        title={t('title')}
      />

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
          <FormSectionTitle>{t('distance')}</FormSectionTitle>
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
          <Button onClick={handleApply} className={styles.applyButton}>
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
  );
}
