'use client';

import * as React from 'react';
import { useTranslations } from 'next-intl';
import { Control, useController } from 'react-hook-form';
import { FormSectionTitle } from '@/components/ui/form-section-title';
import { Input } from '@/components/ui/input';
import styles from './name-section.module.css';
import stylesCommon from '@/components/ui/common.module.css';

export interface NameSectionProps {
  control: Control<any>;
  readOnly?: boolean;
  required?: boolean;
  onFirstNameChange?: (value: string) => void;
  onLastNameChange?: (value: string) => void;
}

export function NameSection({
  control,
  onFirstNameChange,
  onLastNameChange,
  readOnly = false,
  required = false,
}: NameSectionProps) {
  const t = useTranslations('components.profile-sections.name');

  const { field: firstNameField, fieldState: firstNameState } = useController({
    name: 'firstName',
    control,
  });

  const { field: lastNameField, fieldState: lastNameState } = useController({
    name: 'lastName',
    control,
  });

  const errorMsg =
    firstNameState.error?.message || lastNameState.error?.message;

  return (
    <div className={stylesCommon.sectionContainer}>
      <FormSectionTitle required={required}>{t('title')}</FormSectionTitle>
      <div className={styles.nameFields}>
        <Input
          {...firstNameField}
          placeholder={t('firstName')}
          className={stylesCommon.inputDark}
          value={firstNameField.value || ''}
          onChange={(event: React.ChangeEvent<HTMLInputElement>) => {
            firstNameField.onChange(event);
            onFirstNameChange?.(event.target.value);
          }}
          readOnly={readOnly}
        />
        <Input
          {...lastNameField}
          placeholder={t('lastName')}
          className={stylesCommon.inputDark}
          value={lastNameField.value || ''}
          onChange={(event: React.ChangeEvent<HTMLInputElement>) => {
            lastNameField.onChange(event);
            onLastNameChange?.(event.target.value);
          }}
          readOnly={readOnly}
        />
      </div>
      {errorMsg && <p className={stylesCommon.errorMessage}>{errorMsg}</p>}
    </div>
  );
}
