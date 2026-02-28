'use client';

import * as React from 'react';
import { useTranslations } from 'next-intl';
import { CalendarIcon } from 'lucide-react';
import { FormSectionTitle } from '@/components/ui/form-section-title';
import { cn } from '@/lib/utils';
import stylesCommon from '@/components/ui/common.module.css';

export interface BirthdaySectionProps {
  birthday: string;
  readOnly?: boolean;
  required?: boolean;
  onBirthdayChange: (value: string) => void;
}

export function BirthdaySection({
  birthday,
  onBirthdayChange,
  readOnly = false,
  required = false,
}: BirthdaySectionProps) {
  const t = useTranslations('components.profile-sections.birthday');
  const inputRef = React.useRef<HTMLInputElement>(null);

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    onBirthdayChange(e.target.value);
  };

  const handleIconClick = () => {
    inputRef.current?.showPicker();
  };

  return (
    <div className={stylesCommon.sectionContainer}>
      <FormSectionTitle required={required}>{t('title')}</FormSectionTitle>
      <div className="relative flex items-center">
        <input
          ref={inputRef}
          type="date"
          value={birthday || new Date().toISOString().split('T')[0]}
          onChange={handleChange}
          readOnly={readOnly}
          max={new Date().toISOString().split('T')[0]}
          min="1900-01-01"
          className={cn(
            stylesCommon.inputDark,
            'w-full appearance-none pr-10 transition-colors [&::-webkit-calendar-picker-indicator]:hidden',
            !birthday && 'text-secondary',
          )}
        />
        <button
          type="button"
          onClick={handleIconClick}
          disabled={readOnly}
          className="absolute right-3 flex items-center justify-center opacity-50 transition-opacity hover:opacity-100"
        >
          <CalendarIcon className="h-4 w-4" />
        </button>
      </div>
    </div>
  );
}
