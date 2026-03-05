'use client';

import { useState, useEffect } from 'react';

interface UseViewportItemCountOptions {
  /** Height of a single item in pixels */
  itemHeight: number;
  /** Height reserved for non-scrollable elements (e.g. header, footer) in pixels */
  reservedHeight?: number;
  /** Extra items to add beyond what fits in the viewport */
  buffer?: number;
  /** Fallback count when window is not available (SSR) */
  fallback?: number;
}

/**
 * Calculates how many items fit in the visible viewport area,
 * accounting for reserved UI space (headers, footers, etc.).
 * Automatically recalculates on window resize.
 */
export function useViewportItemCount({
  itemHeight,
  reservedHeight = 0,
  buffer = 4,
  fallback = 20,
}: UseViewportItemCountOptions): number {
  const [count, setCount] = useState(() => {
    if (typeof window !== 'undefined') {
      const availableHeight = window.innerHeight - reservedHeight;
      return Math.ceil(availableHeight / itemHeight) + buffer;
    }
    return fallback;
  });

  useEffect(() => {
    const calculate = () => {
      const availableHeight = window.innerHeight - reservedHeight;
      const itemsThatFit = Math.ceil(availableHeight / itemHeight);
      setCount(itemsThatFit + buffer);
    };

    // Recalculate just in case, though initializer should have it mostly right for the first render
    calculate();

    window.addEventListener('resize', calculate);
    return () => window.removeEventListener('resize', calculate);
  }, [itemHeight, reservedHeight, buffer]);

  return count;
}
