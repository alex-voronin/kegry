'use client';

import * as React from 'react';
import { cn } from '@/lib/utils';
import styles from './segmented-control.module.css';

export interface SegmentedControlOption {
  value: string;
  label: string;
  width: string | number;
}

export interface SegmentedControlProps {
  options: SegmentedControlOption[];
  value: string | null;
  onChange: (value: string) => void;
  className?: string;
}

function getWidth(width: string | number | undefined): string | undefined {
  if (width === undefined) return undefined;
  return typeof width === 'number' ? `${width}px` : width;
}

export function SegmentedControl({
  options,
  value,
  onChange,
  className,
}: SegmentedControlProps) {
  return (
    <div className={cn(styles.container, className)}>
      {options.map((option) => (
        <button
          key={option.value}
          type="button"
          className={cn(
            styles.option,
            value === option.value && styles.optionActive,
          )}
          style={{ width: getWidth(option.width) }}
          onClick={() => onChange(option.value)}
        >
          {option.label}
        </button>
      ))}
    </div>
  );
}
