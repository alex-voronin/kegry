'use client';

import { cn } from '@/lib/utils';

interface BadgeProps {
  count: number;
  className?: string;
}

export function Badge({ count, className = '' }: BadgeProps) {
  if (count <= 0) return null;
  return (
    <span
      className={cn(
        'flex h-4 min-w-4 items-center justify-center rounded-full bg-[var(--color-alert-vibrant)] px-1 font-sans text-xs leading-none font-semibold text-white',
        className,
      )}
    >
      {count > 99 ? '99+' : count}
    </span>
  );
}
