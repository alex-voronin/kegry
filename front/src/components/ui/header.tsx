import React from 'react';
import { cn } from '@/lib/utils';
import styles from './header.module.css';

interface HeaderProps {
  left?: React.ReactNode;
  title?: React.ReactNode | string;
  right?: React.ReactNode;
  className?: string;
}

export function Header({ left, title, right, className }: HeaderProps) {
  return (
    <header className={cn(styles.header, 'min-h-[40px]', className)}>
      {/*We have to use min-h-[40px] here explicitly, as twMerge can't pick classes from styles.module.css */}
      <div className={styles.left}>{left}</div>
      <div className={styles.title}>{title}</div>
      <div className={styles.right}>{right}</div>
    </header>
  );
}
