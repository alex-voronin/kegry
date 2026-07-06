'use client';

import { useState, useCallback } from 'react';
import { useRouter } from '@/i18n/routing';
import { useAuthStore, RequiredAction } from '@/lib/auth/use-auth-store';
import { PAGE_ROUTES } from '@/lib/routes';

export function useEventActions() {
  const requiredAction = useAuthStore((s) => s.requiredAction);
  const [showOnboardingDialog, setShowOnboardingDialog] = useState(false);
  const router = useRouter();

  const requireProfile = useCallback(
    <T>(onAllowed: () => T): T | undefined => {
      if (requiredAction === RequiredAction.OnboardingCompletion) {
        setShowOnboardingDialog(true);
        return undefined;
      }
      return onAllowed();
    },
    [requiredAction],
  );

  const handleCreateProfile = useCallback(() => {
    setShowOnboardingDialog(false);
    router.push(PAGE_ROUTES.ONBOARDING);
  }, [router]);

  return {
    requireProfile,
    showOnboardingDialog,
    setShowOnboardingDialog,
    handleCreateProfile,
  };
}
