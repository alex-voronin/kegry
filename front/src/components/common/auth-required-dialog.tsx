'use client';

import { useCallback } from 'react';
import { useTranslations } from 'next-intl';
import { useRouter } from '@/i18n/routing';
import { ModalDialog } from '@/components/ui/modal-dialog';
import { PAGE_ROUTES } from '@/lib/routes';

interface AuthRequiredDialogProps {
  isOpen: boolean;
  onClose: () => void;
}

export function AuthRequiredDialog({
  isOpen,
  onClose,
}: AuthRequiredDialogProps) {
  const t = useTranslations('components.authRequired');
  const router = useRouter();

  const handleLogin = useCallback(() => {
    router.push(PAGE_ROUTES.LOGIN());
  }, [router]);

  return (
    <ModalDialog
      isOpen={isOpen}
      onClose={onClose}
      message={t('message')}
      primaryButtonText={t('cancel')}
      secondaryButtonText={t('login')}
      onPrimaryAction={onClose}
      onSecondaryAction={handleLogin}
    />
  );
}
