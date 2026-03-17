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
import { HobbiesSection } from '@/components/ui/profile-sections/hobbies-section';
import { ModalDialog } from '@/components/ui/modal-dialog';
import { FormSectionTitle } from '@/components/ui/form-section-title';
import { useFeedFiltersStore, FilterDto } from './use-feed-filters-store';
import { useSettingsStore, DistanceUnits } from '@/app/b2c/[locale]/settings/use-settings-store';
import { useHobbiesStore } from '@/store/use-hobbies-store';
import { useFeedStore } from '../use-feed-store';
import {
  metersToKilometers,
  metersToMiles,
  kilometersToMeters,
  milesToMeters,
} from '@/lib/distance-utils';
import { toast } from 'react-toastify';
import styles from './filters.module.css';
import stylesCommon from '@/components/ui/common.module.css';
import { useForm, Controller } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';

const MAX_DISTANCE_KM = 200;
const MAX_DISTANCE_MI = 124;
const MIN_AGE = 18;
const MAX_HOBBIES = 10;

export default function FiltersScreen() {
  const t = useTranslations('screens.filters');
  const tCommon = useTranslations('common');
  const tHobbies = useTranslations('components.profile-sections.hobbies');
  const router = useRouter();
  const {
    filters: serverFilters,
    fetchFilters,
    updateFilters,
    isLoading,
    hasHydrated,
  } = useFeedFiltersStore();

  const {
    hobbies: storeHobbies,
    fetchHobbies,
    isLoading: isHobbiesLoading,
  } = useHobbiesStore();

  const distanceUnits = useSettingsStore((state) => state.distanceUnits);
  const isMiles = distanceUnits === DistanceUnits.Miles;

  const schema = useMemo(() => {
    const maxDist = isMiles ? MAX_DISTANCE_MI : MAX_DISTANCE_KM;
    const units = isMiles
      ? tCommon('units.miles')
      : tCommon('units.kilometers');

    return z
      .object({
        distance: z
          .number(t('validation.distance', { max: maxDist, units }))
          .min(0, t('validation.distance', { max: maxDist, units }))
          .max(maxDist, t('validation.distance', { max: maxDist, units })),
        ageFrom: z.union([
          z.literal(''),
          z.number().min(MIN_AGE, t('validation.minAge', { min: MIN_AGE })),
        ]),
        ageTo: z.union([
          z.literal(''),
          z.number().min(MIN_AGE, t('validation.minAge', { min: MIN_AGE })),
        ]),
        gender: z.string(),
        hobbies: z
          .array(z.string())
          .max(MAX_HOBBIES, t('validation.maxHobbies', { max: MAX_HOBBIES })),
      })
      .refine(
        (data) => {
          if (data.ageFrom === '' || data.ageTo === '') return true;
          return data.ageFrom <= data.ageTo;
        },
        {
          message: t('validation.ageRange'),
          path: ['ageFrom'], // Show error on ageFrom
        },
      );
  }, [isMiles, t, tCommon]);

  type FormData = z.infer<typeof schema>;

  const defaultValues: FormData = {
    distance: 10,
    ageFrom: '',
    ageTo: '',
    gender: 'any',
    hobbies: [],
  };

  const {
    register,
    handleSubmit,
    control,
    reset,
    formState: { errors, isValid, isDirty },
  } = useForm<FormData>({
    resolver: zodResolver(schema),
    defaultValues,
    mode: 'onChange',
  });

  const [isModalOpen, setIsModalOpen] = useState(false);
  const isLoaded = useRef(false);

  useEffect(() => {
    if (!hasHydrated) return;

    const loadData = async () => {
      try {
        await Promise.all([fetchFilters(), fetchHobbies()]);
      } catch (err: any) {
        toast.error(err.message || t('loadFailed'));
      }
    };
    loadData();
  }, [hasHydrated, fetchFilters, fetchHobbies, t]);

  useEffect(() => {
    if (serverFilters && storeHobbies.length > 0 && !isLoaded.current) {
      const distanceValue = isMiles
        ? metersToMiles(serverFilters.radiusMeters)
        : metersToKilometers(serverFilters.radiusMeters);

      // Round to 1 decimal place to avoid long floats
      const roundedDistance = Math.round(distanceValue * 10) / 10;

      // Just map to the array of string IDs to match other forms
      const hobbyIds = serverFilters.hobbyIds;

      reset({
        distance: roundedDistance,
        ageFrom: serverFilters.ageFrom ?? '',
        ageTo: serverFilters.ageTo ?? '',
        gender: serverFilters.gender || 'any',
        hobbies: hobbyIds,
      });
      isLoaded.current = true;
    }
  }, [serverFilters, storeHobbies, isMiles, reset, tHobbies]);

  const handleBack = () => {
    if (isDirty) {
      setIsModalOpen(true);
    } else {
      router.back();
    }
  };

  const onSubmit = async (data: FormData) => {
    const radiusMeters = isMiles
      ? milesToMeters(Number(data.distance))
      : kilometersToMeters(Number(data.distance));

    const dto: FilterDto = {
      radiusMeters,
      ageFrom: data.ageFrom === '' ? null : Number(data.ageFrom),
      ageTo: data.ageTo === '' ? null : Number(data.ageTo),
      gender: data.gender === 'any' ? null : (data.gender as any),
      hobbyIds: data.hobbies,
    };

    try {
      await updateFilters(dto);
      useFeedStore.getState().reset();
      router.back();
    } catch (err: any) {
      toast.error(err.message || 'Failed to update filters');
    }
  };

  const handleDiscard = () => {
    router.back();
  };

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
          <form className={styles.content} onSubmit={handleSubmit(onSubmit)}>
            <div className={styles.filterSection}>
              <FormSectionTitle>
                {t('distance')} (
                {isMiles ? tCommon('units.miles') : tCommon('units.kilometers')}
                )
              </FormSectionTitle>
              <Input
                type="number"
                step="0.1"
                className={stylesCommon.inputDark}
                disabled={isLoading}
                {...register('distance', { valueAsNumber: true })}
              />
              {errors.distance && (
                <div className={styles.errorText}>
                  {errors.distance.message}
                </div>
              )}
            </div>

            <div className={styles.filterSection}>
              <FormSectionTitle>{t('age')}</FormSectionTitle>
              <div className={styles.ageRangeWrapper}>
                <div className={styles.ageInputGroup}>
                  <span className={styles.ageLabel}>{t('from')}</span>
                  <Input
                    type="number"
                    placeholder={t('any')}
                    className={styles.ageInput}
                    disabled={isLoading}
                    {...register('ageFrom', {
                      setValueAs: (v: string) => (v === '' ? '' : Number(v)),
                    })}
                  />
                </div>
                <div className={styles.ageInputGroup}>
                  <span className={styles.ageLabel}>{t('to')}</span>
                  <Input
                    type="number"
                    placeholder={t('any')}
                    className={styles.ageInput}
                    disabled={isLoading}
                    {...register('ageTo', {
                      setValueAs: (v: string) => (v === '' ? '' : Number(v)),
                    })}
                  />
                </div>
              </div>
              {errors.ageFrom && (
                <div className={styles.errorText}>{errors.ageFrom.message}</div>
              )}
              {errors.ageTo && (
                <div className={styles.errorText}>{errors.ageTo.message}</div>
              )}
            </div>

            <div className={styles.filterSection}>
              <FormSectionTitle>{t('gender')}</FormSectionTitle>
              <Controller
                control={control}
                name="gender"
                render={({ field }) => (
                  <SegmentedControl
                    options={genderOptions}
                    value={field.value}
                    onChange={field.onChange}
                  />
                )}
              />
            </div>

            <div className={styles.filterSection}>
              <HobbiesSection
                control={control}
                placeholder={t('hobbiesPlaceholder')}
              />
            </div>

            <div className={styles.applyButtonWrapper}>
              <Button
                type="submit"
                className={styles.applyButton}
                loading={isLoading}
                disabled={!isValid || isLoading}
              >
                {t('apply')}
              </Button>
            </div>
          </form>

          <ModalDialog
            isOpen={isModalOpen}
            onClose={() => setIsModalOpen(false)}
            message={t('applyDialog.title')}
            primaryButtonText={t('applyDialog.buttonNo')}
            secondaryButtonText={t('applyDialog.buttonYes')}
            onPrimaryAction={handleDiscard}
            onSecondaryAction={handleSubmit(onSubmit)}
          />
        </div>
      )}
    </div>
  );
}
