'use client';

import { useEffect } from 'react';
import { useTranslations } from 'next-intl';
import { Control, useController } from 'react-hook-form';
import Select from 'react-select';
import { FormSectionTitle } from '@/components/ui/form-section-title';
import stylesCommon from '@/components/ui/common.module.css';
import { useHobbiesStore } from '@/store/use-hobbies-store';
import { toast } from 'react-toastify';

export interface HobbyOption {
  value: string;
  label: string;
}

export interface HobbiesSectionProps {
  control: Control<any>;
  onChange?: (hobbies: HobbyOption[]) => void;
  readOnly?: boolean;
  required?: boolean;
}

const selectStyles = {
  control: (base: Record<string, unknown>) => ({
    ...base,
    backgroundColor: '#292929',
    borderColor: 'transparent',
    minHeight: '44px',
    borderRadius: '0',
    boxShadow: 'none',
    '@media (min-width: 500px)': {
      borderRadius: '12px',
    },
    '&:hover': {
      borderColor: 'none',
    },
  }),
  menu: (base: Record<string, unknown>) => ({
    ...base,
    backgroundColor: '#292929',
  }),
  option: (base: Record<string, unknown>, state: { isFocused: boolean }) => ({
    ...base,
    backgroundColor: state.isFocused ? 'rgba(255,255,255,0.1)' : 'transparent',
    color: '#ffffff',
  }),
  multiValue: (base: Record<string, unknown>) => ({
    ...base,
    backgroundColor: 'transparent',
    border: '1px solid #ffffff',
    borderRadius: '9999px',
  }),
  multiValueLabel: (base: Record<string, unknown>) => ({
    ...base,
    color: '#ffffff',
  }),
  multiValueRemove: (base: Record<string, unknown>) => ({
    ...base,
    color: '#A0A0A0',
    ':hover': {
      color: '#ffffff',
    },
  }),
  input: (base: Record<string, unknown>) => ({
    ...base,
    color: '#ffffff',
  }),
  placeholder: (base: Record<string, unknown>) => ({
    ...base,
    color: '#A0A0A0',
  }),
};

const readOnlySelectStyles = {
  ...selectStyles,
  multiValueRemove: (base: Record<string, unknown>) => ({
    ...base,
    display: 'none',
  }),
};

export function HobbiesSection({
  control,
  onChange,
  readOnly = false,
  required = false,
}: HobbiesSectionProps) {
  const t = useTranslations('components.profile-sections.hobbies');
  const { hobbies, fetchHobbies, isLoading, hasHydrated } = useHobbiesStore();

  const {
    field,
    fieldState: { error },
  } = useController({
    name: 'hobbies',
    control,
  });

  const selectedHobbiesOptions: HobbyOption[] = hobbies
    .filter((h) => (field.value || []).includes(h.id))
    .map((h) => ({
      value: h.id,
      label: t(h.name),
    }));

  useEffect(() => {
    if (!hasHydrated) return;

    const loadHobbies = async () => {
      try {
        await fetchHobbies();
      } catch (err: any) {
        toast.error(err.message || t('loadFailed'));
      }
    };
    loadHobbies();
  }, [hasHydrated, fetchHobbies, t]);

  // Map API hobbies to select options
  const hobbyOptions: HobbyOption[] = hobbies.map((h) => {
    return {
      value: h.id,
      label: t(h.name),
    };
  });

  return (
    <div className={stylesCommon.sectionContainer}>
      <FormSectionTitle required={required}>{t('title')}</FormSectionTitle>
      <Select
        instanceId="hobby-select"
        isMulti
        options={hobbyOptions}
        value={selectedHobbiesOptions}
        onChange={(selected) => {
          const selectedArray = selected as HobbyOption[];
          field.onChange(selectedArray.map((h) => h.value));
          onChange?.(selectedArray);
        }}
        placeholder={t('placeholder')}
        classNamePrefix="hobby-select"
        isDisabled={readOnly}
        isClearable={!readOnly}
        isLoading={isLoading}
        styles={readOnly ? readOnlySelectStyles : selectStyles}
        components={
          readOnly
            ? { DropdownIndicator: () => null, IndicatorSeparator: () => null }
            : undefined
        }
      />
      {error && <p className={stylesCommon.errorMessage}>{error.message}</p>}
    </div>
  );
}
