'use client';

import { useEffect, useMemo, useState } from 'react';
import { useTranslations } from 'next-intl';
import { useParams } from 'next/navigation';
import { useRouter } from '@/i18n/routing';
import { z } from 'zod';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { toast } from 'react-toastify';
import { Header } from '@/components/ui/header';
import { Icon } from '@/components/ui/icon';
import { Button, ButtonVariant } from '@/components/ui/button';
import { Spinner } from '@/components/ui/spinner';
import { FormSectionTitle } from '@/components/ui/form-section-title';
import stylesCommon from '@/components/ui/common.module.css';
import { Input } from '@/components/ui/input';
import { Toggle } from '@/components/ui/toggle';
import apiClient from '@/lib/api-client';
import { API_ENDPOINTS, PAGE_ROUTES } from '@/lib/routes';
import { useCompanyProfileStore } from '../../use-company-profile-store';

type FormValues = {
  isActive: boolean;
  visitsUpperBound: number;
};

export default function LoyaltyLevelEditPage() {
  const t = useTranslations('screens.editLoyaltyLevel');
  const router = useRouter();
  const { id: companyId, levelId } = useParams() as {
    id: string;
    levelId: string;
  };

  const numericLevelId = Number(levelId);

  const { selectedCompany, isLoading, fetchCompany } = useCompanyProfileStore();
  const [isSubmitting, setIsSubmitting] = useState(false);

  useEffect(() => {
    if (companyId) fetchCompany(companyId);
  }, [companyId, fetchCompany]);

  const level = useMemo(() => {
    return (
      selectedCompany?.loyaltyLevels?.find((l) => l.id === numericLevelId) ||
      null
    );
  }, [selectedCompany, numericLevelId]);

  const schema = z.object({
    isActive: z.boolean(),
    visitsUpperBound: z
      .number()
      .int()
      .min(1, t('validation.visitsUpperBoundMin')),
  });

  const {
    setValue,
    register,
    handleSubmit,
    reset,
    watch,
    formState: { errors, isDirty },
  } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { isActive: true, visitsUpperBound: 1 },
  });

  useEffect(() => {
    if (!level) return;
    reset({
      isActive: level.isActive,
      visitsUpperBound: level.visitsUpperBound,
    });
  }, [level, reset]);

  const onSubmit = async (values: FormValues) => {
    setIsSubmitting(true);
    try {
      await apiClient.put(
        API_ENDPOINTS.LOYALTY_LEVELS.BY_COMPANY_ID(companyId),
        {
          loyaltyLevel: {
            id: numericLevelId,
            isActive: values.isActive,
            visitsUpperBound: values.visitsUpperBound,
          },
        },
      );

      router.replace(
        PAGE_ROUTES.B2B.COMPANIES_DETAIL(companyId) + '?expanded=loyalty',
      );
    } catch {
      toast.error(t('toast.updateFailed'));
    } finally {
      setIsSubmitting(false);
    }
  };

  if (isLoading || !selectedCompany || !level) {
    return (
      <div className="flex flex-1 items-center justify-center">
        <Spinner />
      </div>
    );
  }

  const isActive = watch('isActive');

  return (
    <div className="flex flex-1 flex-col">
      <Header
        title={t('title')}
        left={
          <Icon
            src="/assets/b2b/disclosure-left-icon.svg"
            alt="back"
            width={14}
            height={27}
            onClick={() =>
              router.replace(
                PAGE_ROUTES.B2B.COMPANIES_DETAIL(companyId) +
                  '?expanded=loyalty',
              )
            }
          />
        }
      />

      <form
        className="flex flex-1 flex-col gap-4 px-4 sm:p-4"
        onSubmit={handleSubmit(onSubmit)}
      >
        <div className={stylesCommon.sectionContainer}>
          <FormSectionTitle>{t('isActive')}</FormSectionTitle>
          <div className="bg-form-element flex h-[44px] items-center justify-between px-4 sm:rounded-xl">
            <span className="text-secondary text-sm">{`Level ${level.id}`}</span>
            <Toggle
              checked={isActive}
              onChange={(checked) => {
                setValue('isActive', checked, { shouldDirty: true });
              }}
              srTitle={t('isActive')}
            />
          </div>
        </div>

        <div className={stylesCommon.sectionContainer}>
          <FormSectionTitle>{t('visitsUpperBound')}</FormSectionTitle>
          <Input
            type="number"
            {...register('visitsUpperBound', { valueAsNumber: true })}
            className={stylesCommon.inputDark}
          />
          {errors.visitsUpperBound?.message && (
            <p className={stylesCommon.errorMessage}>
              {errors.visitsUpperBound.message}
            </p>
          )}
        </div>

        <div className="mt-auto mb-10 flex w-full justify-center">
          <Button
            variant={ButtonVariant.Default}
            className="h-[40px] w-[298px]"
            type="submit"
            disabled={isSubmitting || !isDirty}
          >
            {isSubmitting ? <Spinner /> : t('submit')}
          </Button>
        </div>
      </form>
    </div>
  );
}
