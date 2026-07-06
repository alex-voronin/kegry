'use client';

import { useTranslations } from 'next-intl';
import { ModalDialog } from '@/components/ui/modal-dialog';

interface OnboardingGuardDialogProps {
  isOpen: boolean;
  onClose: () => void;
  onCreateProfile: () => void;
}

export function OnboardingGuardDialog({
  isOpen,
  onClose,
  onCreateProfile,
}: OnboardingGuardDialogProps) {
  const t = useTranslations('components.onboardingGuard');

  return (
    <ModalDialog
      isOpen={isOpen}
      onClose={onClose}
      message={t('message')}
      primaryButtonText={t('cancel')}
      secondaryButtonText={t('create')}
      onPrimaryAction={onClose}
      onSecondaryAction={onCreateProfile}
    />
  );
}
