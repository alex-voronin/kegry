'use client';

import * as React from 'react';
import { useTranslations } from 'next-intl';
import { Control, useController } from 'react-hook-form';
import { FormSectionTitle } from '@/components/ui/form-section-title';
import stylesCommon from '@/components/ui/common.module.css';
import styles from './bio-section.module.css';

export interface BioSectionProps {
  control: Control<any>;
  readOnly?: boolean;
  onBioChange?: (value: string) => void;
}

export function BioSection({
  control,
  onBioChange,
  readOnly = false,
}: BioSectionProps) {
  const t = useTranslations('components.profile-sections.bio');
  const MAX_BIO_LENGTH = 200;

  const {
    field,
    fieldState: { error },
  } = useController({
    name: 'bio',
    control,
  });

  return (
    <div className={stylesCommon.sectionContainer}>
      <FormSectionTitle>{t('title')}</FormSectionTitle>
      <div className={styles.textareaWrapper}>
        <textarea
          {...field}
          className={styles.textarea}
          placeholder={t('placeholder')}
          value={field.value || ''}
          maxLength={MAX_BIO_LENGTH}
          onChange={(event: React.ChangeEvent<HTMLTextAreaElement>) => {
            field.onChange(event);
            onBioChange?.(event.target.value);
          }}
          readOnly={readOnly}
        />
        <span className={styles.counter}>
          {(field.value || '').length}/{MAX_BIO_LENGTH}
        </span>
      </div>
      {error && <p className={stylesCommon.errorMessage}>{error.message}</p>}
    </div>
  );
}
