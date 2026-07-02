'use client';

import * as React from 'react';
import { useTranslations } from 'next-intl';
import { Control, useController } from 'react-hook-form';
import { CalendarClock } from 'lucide-react';
import { FormSectionTitle } from '@/components/ui/form-section-title';
import { cn } from '@/lib/utils';
import stylesCommon from '@/components/ui/common.module.css';

export interface BirthdaySectionProps {
  control: Control<any>;
  readOnly?: boolean;
  required?: boolean;
  onBirthdayChange?: (value: string) => void;
}

export function BirthdaySection({
  control,
  onBirthdayChange,
  readOnly = false,
  required = false,
}: BirthdaySectionProps) {
  const t = useTranslations('components.profile-sections.birthday');
  const inputRef = React.useRef<HTMLInputElement>(null);

  const {
    field,
    fieldState: { error },
  } = useController({
    name: 'birthday',
    control,
  });

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    field.onChange(e);
    onBirthdayChange?.(e.target.value);
  };

  const handleIconClick = () => {
    inputRef.current?.showPicker();
  };

  return (
    <div className={stylesCommon.sectionContainer}>
      <FormSectionTitle required={required}>{t('title')}</FormSectionTitle>
      <div className="relative flex items-center">
        <input
          {...field}
          ref={inputRef}
          type="date"
          value={field.value || new Date().toISOString().split('T')[0]}
          onChange={handleChange}
          readOnly={readOnly}
          max={new Date().toISOString().split('T')[0]}
          min="1900-01-01"
          className={cn(
            stylesCommon.inputDark,
            'w-full appearance-none pr-10 transition-colors [&::-webkit-calendar-picker-indicator]:hidden',
            !field.value && 'text-secondary',
          )}
        />
        <button
          type="button"
          onClick={handleIconClick}
          disabled={readOnly}
          className="absolute right-3 flex items-center justify-center opacity-50 transition-opacity hover:opacity-100"
        >
          <CalendarClock className="h-4 w-4" />
        </button>
      </div>
      {error && <p className={stylesCommon.errorMessage}>{error.message}</p>}
    </div>
  );
}
