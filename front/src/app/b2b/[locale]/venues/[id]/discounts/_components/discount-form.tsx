'use client';

import { useEffect, useImperativeHandle, forwardRef } from 'react';

import { useTranslations } from 'next-intl';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { Button, ButtonVariant } from '@/components/ui/button';
import { Spinner } from '@/components/ui/spinner';
import { FormSectionTitle } from '@/components/ui/form-section-title';
import { Input } from '@/components/ui/input';
import { Toggle } from '@/components/ui/toggle';
import { Select } from '@/components/ui/select';
import stylesCommon from '@/components/ui/common.module.css';
import styles from './discount-form.module.css';

const daysOfWeek = [
  'Monday',
  'Tuesday',
  'Wednesday',
  'Thursday',
  'Friday',
  'Saturday',
  'Sunday',
];
const workDays = ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday'];
const weekendDays = ['Saturday', 'Sunday'];

interface LoyaltyLevelOption {
  value: number;
  label: string;
}

export interface DiscountFormHandle {
  submitForm: () => void;
}

interface DiscountFormProps {
  initialValues?: {
    percent: number | null;
    days: string[];
    loyaltyLevelId: number | null;
  };
  loyaltyLevels: { id: number; visitsUpperBound: number; isActive: boolean }[];
  onSubmit: (values: {
    percent: number | null;
    days: string[];
    loyaltyLevelId: number | null;
  }) => Promise<void>;
  isSubmitting: boolean;
  submitLabel: string;
  translationNamespace: 'createDiscount' | 'editDiscount';
  onDirtyChange?: (isDirty: boolean) => void;
}

export const DiscountForm = forwardRef<DiscountFormHandle, DiscountFormProps>(
  function DiscountForm(
    {
      initialValues,
      loyaltyLevels,
      onSubmit,
      isSubmitting,
      submitLabel,
      translationNamespace,
      onDirtyChange,
    },
    ref,
  ) {
    const t = useTranslations(`screens.${translationNamespace}`);
    const tCommon = useTranslations('common');
    const tDiscountForm = useTranslations('components.forms.discount');

    const schema = z
      .object({
        isCustomDiscount: z.boolean(),
        percent: z.number().nullable(),
        days: z
          .array(z.string())
          .min(1, tDiscountForm('validation.daysRequired')),
        loyaltyLevelId: z
          .number()
          .nullable()
          .refine((val) => val !== null, {
            message: tDiscountForm('validation.loyaltyLevelRequired'),
          }),
      })
      .superRefine((data, ctx) => {
        if (!data.isCustomDiscount) {
          if (data.percent === null || data.percent < 1 || data.percent > 100) {
            ctx.addIssue({
              code: z.ZodIssueCode.custom,
              message: tDiscountForm('validation.percentRange'),
              path: ['percent'],
            });
          }
        }
      });

    type FormValues = {
      isCustomDiscount: boolean;
      percent: number | null;
      days: string[];
      loyaltyLevelId: number | null;
    };

    const {
      register,
      handleSubmit,
      setValue,
      watch,
      formState: { errors, isDirty },
    } = useForm<FormValues>({
      resolver: zodResolver(schema) as any,
      defaultValues: {
        isCustomDiscount: initialValues?.percent === null,
        percent:
          initialValues?.percent !== undefined ? initialValues.percent : 0,
        days: initialValues?.days ?? [],
        loyaltyLevelId:
          initialValues?.loyaltyLevelId ??
          loyaltyLevels.find((l) => l.isActive)?.id ??
          null,
      },
    });

    useEffect(() => {
      onDirtyChange?.(isDirty);
    }, [isDirty, onDirtyChange]);

    useImperativeHandle(
      ref,
      () => ({
        submitForm: () => {
          handleSubmit(onSubmit)();
        },
      }),
      [handleSubmit, onSubmit],
    );

    const selectedDays = watch('days');

    const handleDayToggle = (day: string) => {
      if (selectedDays.includes(day)) {
        setValue(
          'days',
          selectedDays.filter((d) => d !== day),
          { shouldValidate: true, shouldDirty: true },
        );
      } else {
        setValue('days', [...selectedDays, day], {
          shouldValidate: true,
          shouldDirty: true,
        });
      }
    };

    const isWorkDaysSelected = workDays.every((day) =>
      selectedDays.includes(day),
    );
    const isWeekendSelected = weekendDays.every((day) =>
      selectedDays.includes(day),
    );

    const handleWorkDaysToggle = (checked: boolean) => {
      let newDays = [...selectedDays];
      if (checked) {
        newDays = Array.from(new Set([...newDays, ...workDays]));
      } else {
        newDays = newDays.filter((day) => !workDays.includes(day));
      }
      setValue('days', newDays, { shouldValidate: true, shouldDirty: true });
    };

    const handleWeekendToggle = (checked: boolean) => {
      let newDays = [...selectedDays];
      if (checked) {
        newDays = Array.from(new Set([...newDays, ...weekendDays]));
      } else {
        newDays = newDays.filter((day) => !weekendDays.includes(day));
      }
      setValue('days', newDays, { shouldValidate: true, shouldDirty: true });
    };

    return (
      <form className={styles.form} onSubmit={handleSubmit(onSubmit)}>
        <div className={stylesCommon.sectionContainer}>
          <div
            className={`${styles.daysGrid} ${watch('isCustomDiscount') ? '' : 'mb-2'}`}
          >
            <div className={styles.dayRow}>
              <span className={styles.dayLabel}>
                {tDiscountForm('customDiscountMode')}
              </span>
              <Toggle
                checked={watch('isCustomDiscount')}
                onChange={(checked) => {
                  setValue('isCustomDiscount', checked, { shouldDirty: true });
                  if (checked) {
                    setValue('percent', null, {
                      shouldValidate: true,
                      shouldDirty: true,
                    });
                  } else {
                    setValue('percent', 0, {
                      shouldValidate: true,
                      shouldDirty: true,
                    });
                  }
                }}
              />
            </div>
          </div>

          {!watch('isCustomDiscount') && (
            <>
              <FormSectionTitle required>
                {tDiscountForm('percent')}
              </FormSectionTitle>
              <Input
                {...register('percent', { valueAsNumber: true })}
                type="number"
                placeholder="10"
                className={stylesCommon.inputDark}
              />
              {errors.percent?.message && (
                <p className={stylesCommon.errorMessage}>
                  {errors.percent.message}
                </p>
              )}
            </>
          )}
        </div>

        <div className={stylesCommon.sectionContainer}>
          <FormSectionTitle required>
            {tDiscountForm('loyaltyLevel')}
          </FormSectionTitle>
          <Select
            options={loyaltyLevels
              .filter((l) => l.isActive)
              .map((l) => ({
                value: l.id,
                label: tDiscountForm('loyaltyLevelOption', {
                  level: l.id,
                  visits: l.visitsUpperBound,
                }),
              }))}
            value={
              watch('loyaltyLevelId') !== null
                ? {
                    value: watch('loyaltyLevelId')!,
                    label: tDiscountForm('loyaltyLevelOption', {
                      level: watch('loyaltyLevelId')!,
                      visits:
                        loyaltyLevels.find(
                          (l) => l.id === watch('loyaltyLevelId'),
                        )?.visitsUpperBound || 0,
                    }),
                  }
                : null
            }
            onChange={(option: any) =>
              setValue('loyaltyLevelId', option?.value ?? null, {
                shouldDirty: true,
              })
            }
            placeholder={tDiscountForm('loyaltyLevelPlaceholder')}
            isSearchable={false}
          />
          {errors.loyaltyLevelId?.message && (
            <p className={stylesCommon.errorMessage}>
              {errors.loyaltyLevelId.message}
            </p>
          )}
        </div>

        <div className={stylesCommon.sectionContainer}>
          <FormSectionTitle>{tDiscountForm('when')}</FormSectionTitle>

          <div className={styles.masterToggles}>
            <div className={styles.dayRow}>
              <span className={styles.dayLabel}>
                {tDiscountForm('workDays')}
              </span>
              <Toggle
                checked={isWorkDaysSelected}
                onChange={handleWorkDaysToggle}
              />
            </div>
            <div className={styles.dayRow}>
              <span className={styles.dayLabel}>
                {tDiscountForm('weekend')}
              </span>
              <Toggle
                checked={isWeekendSelected}
                onChange={handleWeekendToggle}
              />
            </div>
          </div>

          <div className={styles.daysGrid}>
            {daysOfWeek.map((day) => (
              <div key={day} className={styles.dayRow}>
                <span className={styles.dayLabel}>
                  {tCommon(`days.${day}`)}
                </span>
                <Toggle
                  checked={selectedDays.includes(day)}
                  onChange={() => handleDayToggle(day)}
                />
              </div>
            ))}
          </div>
          {errors.days?.message && (
            <p className={stylesCommon.errorMessage}>{errors.days.message}</p>
          )}
        </div>

        <div className={styles.footer}>
          <Button
            variant={ButtonVariant.Default}
            className={styles.submitButton}
            type="submit"
            disabled={isSubmitting}
          >
            {isSubmitting ? <Spinner /> : submitLabel}
          </Button>
        </div>
      </form>
    );
  },
);
