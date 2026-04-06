'use client';

import { useState, useEffect } from 'react';
import { ArrowUp } from 'lucide-react';
import styles from './ScrollToTopButton.module.css';

interface ScrollToTopButtonProps {
  containerId: string;
}

export const ScrollToTopButton = ({ containerId }: ScrollToTopButtonProps) => {
  const [showScrollTop, setShowScrollTop] = useState(false);

  useEffect(() => {
    const container = document.getElementById(containerId);
    if (!container) return;

    const handleScroll = () => {
      setShowScrollTop(container.scrollTop > window.innerHeight);
    };

    container.addEventListener('scroll', handleScroll, { passive: true });
    return () => container.removeEventListener('scroll', handleScroll);
  }, [containerId]);

  const scrollToTop = () => {
    const container = document.getElementById(containerId);
    if (container) {
      container.scrollTo({ top: 0, behavior: 'smooth' });
    }
  };

  return (
    <button
      className={`${styles.scrollTopButton} ${showScrollTop ? styles.scrollTopButtonVisible : ''}`}
      onClick={scrollToTop}
      aria-label="Scroll to top"
    >
      <ArrowUp size={24} />
    </button>
  );
};
