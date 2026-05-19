'use client';

import { useEffect, useState } from 'react';
import styles from './check-in-animation.module.css';

interface CheckInAnimationProps {
  visible: boolean;
  onDone: () => void;
}

export function CheckInAnimation({ visible, onDone }: CheckInAnimationProps) {
  const [phase, setPhase] = useState<'enter' | 'exit' | null>(null);

  useEffect(() => {
    if (!visible) {
      setPhase(null);
      return;
    }

    setPhase('enter');

    const exitTimer = setTimeout(() => setPhase('exit'), 1600);
    const doneTimer = setTimeout(() => onDone(), 2200);

    return () => {
      clearTimeout(exitTimer);
      clearTimeout(doneTimer);
    };
  }, [visible, onDone]);

  if (!phase) return null;

  return (
    <div
      className={`${styles.overlay} ${phase === 'exit' ? styles.fadeOut : ''}`}
    >
      <div className={styles.content}>
        <span className={styles.emoji}>🎉</span>
        <span className={styles.plusOne}>+1</span>
      </div>
    </div>
  );
}
