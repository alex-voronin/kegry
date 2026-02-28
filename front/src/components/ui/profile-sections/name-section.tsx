'use client';

import * as React from 'react';
import { useTranslations } from 'next-intl';
import { FormSectionTitle } from '@/components/ui/form-section-title';
import { Input } from '@/components/ui/input';
import styles from './name-section.module.css';
import stylesCommon from '@/components/ui/common.module.css';

export interface NameSectionProps {
  firstName: string;
  lastName: string;
  readOnly?: boolean;
  required?: boolean;
  onFirstNameChange: (value: string) => void;
  onLastNameChange: (value: string) => void;
}

export function NameSection({
  firstName,
  lastName,
  onFirstNameChange,
  onLastNameChange,
  readOnly = false,
  required = false,
}: NameSectionProps) {
  const t = useTranslations('components.profile-sections.name');

  return (
    <div className={stylesCommon.sectionContainer}>
      <FormSectionTitle required={required}>{t('title')}</FormSectionTitle>
      <div className={styles.nameFields}>
        <Input
          placeholder={t('firstName')}
          className={stylesCommon.inputDark}
          value={firstName}
          onChange={(event: React.ChangeEvent<HTMLInputElement>) =>
            onFirstNameChange(event.target.value)
          }
          readOnly={readOnly}
        />
        <Input
          placeholder={t('lastName')}
          className={stylesCommon.inputDark}
          value={lastName}
          onChange={(event: React.ChangeEvent<HTMLInputElement>) =>
            onLastNameChange(event.target.value)
          }
          readOnly={readOnly}
        />
      </div>
    </div>
  );
}
