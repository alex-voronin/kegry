import { cn } from '@/lib/utils';
import styles from './tab-control.module.css';

export interface TabOption {
  value: string;
  label: React.ReactNode;
}

interface TabControlProps {
  options: TabOption[];
  value: string;
  onChange: (value: string) => void;
  size?: 'sm' | 'md';
  className?: string;
}

export function TabControl({
  options,
  value,
  onChange,
  size = 'sm',
  className,
}: TabControlProps) {
  const hasSeparators = options.length > 2;
  return (
    <div className={cn(styles.container, className)}>
      {options.map((option, index) => {
        const isActive = value === option.value;
        const isFirst = index === 0;
        const isLast = index === options.length - 1;
        return (
          <button
            key={option.value}
            type="button"
            onClick={() => onChange(option.value)}
            className={cn(
              styles.option,
              size === 'sm' ? styles.optionSm : styles.optionMd,
              isActive ? styles.optionActive : styles.optionInactive,
              isActive && !isFirst && styles.borderLeft,
              isActive && !isLast && styles.borderRight,
              hasSeparators && !isLast && styles.separator,
            )}
          >
            {option.label}
          </button>
        );
      })}
    </div>
  );
}
