'use client';

import * as React from 'react';
import { useTranslations } from 'next-intl';
import { FormSectionTitle } from '@/components/ui/form-section-title';
import {
  SegmentedControl,
  SegmentedControlOption,
} from '@/components/ui/segmented-control';
import stylesCommon from '@/components/ui/common.module.css';

export interface GenderSectionProps {
  options: SegmentedControlOption[];
  value: string | null;
  readOnly?: boolean;
  required?: boolean;
  onChange: (value: string) => void;
}

export function GenderSection({
  options,
  value,
  onChange,
  readOnly = false,
  required = false,
}: GenderSectionProps) {
  const t = useTranslations('components.profile-sections.gender');

  return (
    <div className={stylesCommon.sectionContainer}>
      <FormSectionTitle required={required}>{t('title')}</FormSectionTitle>
      <SegmentedControl
        options={options}
        value={value}
        onChange={onChange}
        className={readOnly ? stylesCommon.readOnlySegmented : undefined}
      />
    </div>
  );
}
