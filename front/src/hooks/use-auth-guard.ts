'use client';

import { useState, useCallback } from 'react';
import { useAuthStore } from '@/lib/auth/use-auth-store';

export function useAuthGuard() {
  const isAuthenticated = useAuthStore((s) => !!s.accessToken);
  const [isAuthRequiredDialogOpen, setIsAuthRequiredDialogOpen] =
    useState(false);

  const guardAuth = useCallback(
    (action: () => void) => {
      return () => {
        if (!isAuthenticated) {
          setIsAuthRequiredDialogOpen(true);
          return;
        }
        action();
      };
    },
    [isAuthenticated],
  );

  return {
    isAuthenticated,
    guardAuth,
    isAuthRequiredDialogOpen,
    setIsAuthRequiredDialogOpen,
  };
}
