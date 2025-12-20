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
    <header className={cn(styles.header, className)}>
      {left && <div className={styles.left}>{left}</div>}
      {title && <div className={styles.title}>{title}</div>}
      {right && <div className={styles.right}>{right}</div>}
    </header>
  );
}
