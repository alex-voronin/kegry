'use client';

import { useEffect, useMemo, useState, useCallback, useRef } from 'react';
import { useTranslations } from 'next-intl';
import { useParams } from 'next/navigation';
import { toast } from 'react-toastify';
import { Header } from '@/components/ui/header';
import { Icon } from '@/components/ui/icon';
import { Button, ButtonVariant } from '@/components/ui/button';
import { Spinner } from '@/components/ui/spinner';
import { FormSectionTitle } from '@/components/ui/form-section-title';
import { Select } from '@/components/ui/select';
import { ModalDialog } from '@/components/ui/modal-dialog';
import stylesCommon from '@/components/ui/common.module.css';
import { Input } from '@/components/ui/input';
import apiClient from '@/lib/api-client';
import { API_ENDPOINTS, PAGE_ROUTES } from '@/lib/routes';
import { useSafeBack } from '@/hooks/use-safe-back';
import { extractApiError } from '@/lib/api-error';
import { LoyaltyLevelsTimeline } from '@/components/b2b/loyalty-levels-timeline';
import { useCompaniesStore } from '../../use-companies-store';

const LEVEL_COUNT_OPTIONS = [
  { value: 0, label: '0' },
  { value: 1, label: '1' },
  { value: 2, label: '2' },
  { value: 3, label: '3' },
  { value: 4, label: '4' },
];

export default function LoyaltyLevelsManagePage() {
  const tCompany = useTranslations('screens.companyProfile');
  const tCommon = useTranslations('common');
  const { id: companyId } = useParams() as { id: string };
  const { safeBack } = useSafeBack();
  const [showSaveDialog, setShowSaveDialog] = useState(false);
  const selectRef = useRef<HTMLDivElement>(null);

  const {
    selectedCompany,
    isProfileLoading: isLoading,
    fetchCompany,
  } = useCompaniesStore();

  const [activeCount, setActiveCount] = useState(0);
  const [bounds, setBounds] = useState<Record<number, string>>({});
  const [isSaving, setIsSaving] = useState(false);

  useEffect(() => {
    if (companyId) fetchCompany(companyId);
  }, [companyId, fetchCompany]);

  useEffect(() => {
    if (!selectedCompany) return;
    const count = selectedCompany.loyaltyLevels.filter(
      (l) => l.isActive,
    ).length;
    setActiveCount(count);
    const init: Record<number, string> = {};
    for (const level of selectedCompany.loyaltyLevels) {
      init[level.id] = String(level.visitsUpperBound);
    }
    setBounds(init);
  }, [selectedCompany]);

  const handleActiveCountChange = (value: string | number) => {
    const count = Number(value);
    setActiveCount(count);
    setBounds((prev) => {
      const next = { ...prev };
      for (let i = 0; i < 4; i++) {
        if (i >= count) continue;
        const existing = Number(next[i]);
        if (!next[i] || existing === 0) {
          const prevVal = i > 0 ? Number(next[i - 1]) || 0 : 0;
          next[i] = String(prevVal + 5);
        }
      }
      return next;
    });
  };

  const handleBoundChange = (levelId: number, value: string) => {
    if (value === '' || /^\d+$/.test(value)) {
      setBounds((prev) => ({ ...prev, [levelId]: value }));
    }
  };

  const getLevelBound = (levelId: number): number => {
    if (activeCount === 0) return 0;
    if (levelId < activeCount - 1) {
      return Number(bounds[levelId]) || 0;
    }
    const prev = levelId > 0 ? getLevelBound(levelId - 1) : 0;
    return prev + 5;
  };

  const previewLevels = useMemo(
    () =>
      Array.from({ length: activeCount }, (_, i) => ({
        id: i,
        visitsUpperBound: getLevelBound(i),
      })),
    [activeCount, bounds],
  );

  const boundsValid = useMemo(() => {
    let prev = 0;
    for (let i = 0; i < activeCount - 1; i++) {
      const val = Number(bounds[i]);
      if (!val || val <= prev) return false;
      prev = val;
    }
    return true;
  }, [activeCount, bounds]);

  const hasChanges = useMemo(() => {
    if (!selectedCompany) return false;
    const activeChanged =
      activeCount !==
      selectedCompany.loyaltyLevels.filter((l) => l.isActive).length;
    if (activeChanged) return true;
    return selectedCompany.loyaltyLevels.some(
      (l) => l.visitsUpperBound !== getLevelBound(l.id),
    );
  }, [activeCount, bounds, selectedCompany]);

  const goBack = useCallback(() => {
    safeBack(PAGE_ROUTES.B2B.COMPANIES_DETAIL(companyId));
  }, [safeBack, companyId]);

  const handleTimelineClick = () => {
    selectRef.current?.scrollIntoView({ behavior: 'smooth', block: 'center' });
    const control =
      selectRef.current?.querySelector<HTMLElement>('[role="combobox"]');
    if (control) {
      control.dispatchEvent(new MouseEvent('mousedown', { bubbles: true }));
    }
  };

  const handleCancel = () => {
    if (hasChanges) {
      setShowSaveDialog(true);
    } else {
      goBack();
    }
  };

  const handleSave = async () => {
    if (!selectedCompany || !boundsValid) {
      if (!boundsValid) toast.error(tCompany('boundsMustIncrease'));
      return;
    }
    setIsSaving(true);

    try {
      const levels = Array.from({ length: 4 }, (_, i) => ({
        id: i,
        isActive: i < activeCount,
        visitsUpperBound: getLevelBound(i),
      }));

      await apiClient.put(
        API_ENDPOINTS.B2B.LOYALTY_LEVELS.BY_COMPANY_ID(companyId),
        { loyaltyLevels: levels },
      );

      await fetchCompany(companyId);
      toast.success(tCompany('toast.saveSuccess'));
      safeBack(PAGE_ROUTES.B2B.COMPANIES_DETAIL(companyId));
    } catch (err: any) {
      const errorMessage = extractApiError(err)[0];
      toast.error(errorMessage || tCompany('toast.saveFailed'));
    } finally {
      setIsSaving(false);
    }
  };

  if (isLoading || !selectedCompany) {
    return (
      <div className="flex flex-1 items-center justify-center">
        <Spinner />
      </div>
    );
  }

  return (
    <div className="flex h-dvh flex-col overflow-hidden">
      <Header
        title={tCompany('loyaltyLevelsTitle')}
        left={
          <Icon
            src="/assets/b2b/disclosure-left-icon.svg"
            alt="back"
            width={14}
            height={27}
            onClick={handleCancel}
          />
        }
      />

      <form
        className="flex flex-1 flex-col overflow-hidden"
        onSubmit={(e) => {
          e.preventDefault();
          handleSave();
        }}
      >
        <div className="flex-1 overflow-y-auto py-4 sm:px-4">
          <div className="flex flex-col gap-4">
            <div>
              <LoyaltyLevelsTimeline
                levels={previewLevels}
                onClick={handleTimelineClick}
                showTapHint={false}
              />
            </div>

            <div className={stylesCommon.sectionContainer} ref={selectRef}>
              <FormSectionTitle className="px-4 sm:px-0">
                {tCompany('activeLevels')}
              </FormSectionTitle>
              <Select
                options={LEVEL_COUNT_OPTIONS}
                value={LEVEL_COUNT_OPTIONS.find((o) => o.value === activeCount)}
                onChange={(option) => {
                  if (option && 'value' in option) {
                    handleActiveCountChange(option.value);
                  }
                }}
              />
            </div>

            {Array.from({ length: Math.max(0, activeCount - 1) }, (_, i) => (
              <div key={i} className={stylesCommon.sectionContainer}>
                <FormSectionTitle className="px-4 sm:px-0">
                  {tCompany('levelNumber', { level: i })}
                </FormSectionTitle>
                <Input
                  type="text"
                  inputMode="numeric"
                  value={bounds[i] ?? ''}
                  onChange={(e) => handleBoundChange(i, e.target.value)}
                  className={stylesCommon.inputDark}
                />
              </div>
            ))}

            {activeCount > 0 && !boundsValid && (
              <p className={`${stylesCommon.errorMessage} px-4 sm:px-0`}>
                {tCompany('boundsMustIncrease')}
              </p>
            )}
          </div>
        </div>

        <div className="flex w-full justify-center pt-4 pb-10 sm:pb-6">
          <Button
            variant={ButtonVariant.Default}
            className="h-[40px] w-[298px]"
            type="submit"
            disabled={isSaving || !hasChanges || !boundsValid}
          >
            {isSaving ? <Spinner /> : tCompany('save')}
          </Button>
        </div>
      </form>

      <ModalDialog
        isOpen={showSaveDialog}
        onClose={() => setShowSaveDialog(false)}
        message={tCommon('saveChangesDialog.message')}
        primaryButtonText={tCommon('saveChangesDialog.no')}
        secondaryButtonText={tCommon('saveChangesDialog.yes')}
        onPrimaryAction={goBack}
        onSecondaryAction={() => {
          setShowSaveDialog(false);
          handleSave();
        }}
      />
    </div>
  );
}
