'use client';

import { useTranslations } from 'next-intl';
import { useRouter } from '@/i18n/routing';
import { ModalDialog } from '@/components/ui/modal-dialog';
import { openTawkChat } from '@/components/tawk-messenger';
import { B2B_SUBDOMAIN } from '@/lib/constants';

interface B2CUserLoginAttemptDialogProps {
  isOpen: boolean;
  onClose: () => void;
}

export function B2CUserLoginAttemptDialog({
  isOpen,
  onClose,
}: B2CUserLoginAttemptDialogProps) {
  const t = useTranslations('screens.login.b2cUserAttemptDialog');
  const router = useRouter();

  const handleOpenCustomerPortal = () => {
    const hostname = window.location.hostname;
    const protocol = window.location.protocol;
    const newHostname = hostname.startsWith(B2B_SUBDOMAIN)
      ? hostname.replace(B2B_SUBDOMAIN, '')
      : hostname;

    // Go to B2C root with current locale
    let pathname = window.location.pathname;
    const localeMatch = pathname.match(/^\/(en|sr)/);
    const localePrefix = localeMatch ? localeMatch[0] : '';

    window.location.href = `${protocol}//${newHostname}${localePrefix}/`;
  };

  return (
    <ModalDialog
      isOpen={isOpen}
      onClose={onClose}
      message={t('message')}
      primaryButtonText={t('contactSupport')}
      secondaryButtonText={t('openCustomerPortal')}
      onPrimaryAction={openTawkChat}
      onSecondaryAction={handleOpenCustomerPortal}
    />
  );
}
