import React from 'react';
import { cn } from '@/lib/utils';
import styles from './header.module.css';

interface HeaderProps {
  left?: React.ReactNode;
  title?: React.ReactNode | string;
  right?: React.ReactNode;
  className?: string;
}

export const Header = React.forwardRef<HTMLElement, HeaderProps>(
  ({ left, title, right, className }, ref) => {
    return (
      <header
        ref={ref}
        className={cn(styles.header, 'min-h-[40px]', className)}
      >
        {/*We have to use min-h-[40px] here explicitly, as twMerge can't pick classes from styles.module.css */}
        <div className={styles.left}>{left}</div>
        <div className={styles.title}>{title}</div>
        <div className={styles.right}>{right}</div>
      </header>
    );
  },
);

Header.displayName = 'Header';
