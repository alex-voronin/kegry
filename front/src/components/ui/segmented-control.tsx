'use client';

import * as React from 'react';
import { cn } from '@/lib/utils';
import styles from './segmented-control.module.css';

export interface SegmentedControlOption {
  value: string;
  label: string;
  width: string | number;
}

export interface SegmentedControlProps extends Omit<
  React.HTMLAttributes<HTMLDivElement>,
  'onChange'
> {
  options: SegmentedControlOption[];
  value: string | null;
  onChange: (value: string) => void;
  className?: string;
  highlightPrevious?: boolean;
  stretchItems?: boolean;
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
  highlightPrevious,
  stretchItems,
  ...props
}: SegmentedControlProps) {
  const selectedIndex = options.findIndex((o) => o.value === value);
  return (
    <div className={cn(styles.container, className)} {...props}>
      {options.map((option, index) => {
        const isActive = highlightPrevious
          ? index <= selectedIndex
          : value === option.value;
        return (
          <button
            key={option.value}
            type="button"
            className={cn(
              styles.option,
              isActive && styles.optionActive,
              stretchItems && 'flex-1',
            )}
            style={{ width: stretchItems ? undefined : getWidth(option.width) }}
            onClick={() => onChange(option.value)}
          >
            {option.label}
          </button>
        );
      })}
    </div>
  );
}
