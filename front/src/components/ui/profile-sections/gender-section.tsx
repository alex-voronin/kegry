'use client';

import * as React from 'react';
import { useTranslations } from 'next-intl';
import { Control, useController } from 'react-hook-form';
import { FormSectionTitle } from '@/components/ui/form-section-title';
import {
  SegmentedControl,
  SegmentedControlOption,
} from '@/components/ui/segmented-control';
import stylesCommon from '@/components/ui/common.module.css';

export interface GenderSectionProps {
  options: SegmentedControlOption[];
  control: Control<any>;
  readOnly?: boolean;
  required?: boolean;
  onChange?: (value: string) => void;
}

export function GenderSection({
  options,
  control,
  onChange,
  readOnly = false,
  required = false,
}: GenderSectionProps) {
  const t = useTranslations('components.profile-sections.gender');

  const {
    field,
    fieldState: { error },
  } = useController({
    name: 'gender',
    control,
  });

  return (
    <div className={stylesCommon.sectionContainer}>
      <FormSectionTitle required={required}>{t('title')}</FormSectionTitle>
      <SegmentedControl
        options={options}
        value={field.value as string}
        onChange={(val) => {
          field.onChange(val);
          onChange?.(val);
        }}
        className={readOnly ? stylesCommon.readOnlySegmented : undefined}
      />
      {error && <p className={stylesCommon.errorMessage}>{error.message}</p>}
    </div>
  );
}
