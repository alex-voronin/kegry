'use client';

import { useEffect } from 'react';
import { useTranslations } from 'next-intl';
import { toast } from 'react-toastify';
import { Header } from '@/components/ui/header';
import { Spinner } from '@/components/ui/spinner';
import { FormSectionTitle } from '@/components/ui/form-section-title';
import { Input } from '@/components/ui/input';
import { useSidebar, SidebarIcon, Sidebar } from '@/components/b2b/Sidebar';
import { useProfileStore } from './use-profile-store';
import commonStyles from '@/components/ui/common.module.css';

export default function B2BProfilePage() {
  const t = useTranslations('screens.profile');
  const { isOpen, open, close } = useSidebar();
  const { account, isLoading, fetchAccount } = useProfileStore();

  useEffect(() => {
    fetchAccount().catch(() => {
      toast.error(t('toast.loadFailed'));
    });
  }, [fetchAccount, t]);

  return (
    <div className="relative flex flex-1 flex-col overflow-hidden">
      <Header left={<SidebarIcon onClick={open} />} title={t('title')} />

      <Sidebar isOpen={isOpen} onClose={close} />

      <div className="flex flex-1 flex-col overflow-y-auto py-6 sm:px-4">
        {isLoading && !account ? (
          <div className="flex flex-1 items-center justify-center">
            <Spinner />
          </div>
        ) : (
          <div className="flex flex-col gap-6">
            <div className={commonStyles.sectionContainer}>
              <FormSectionTitle>{t('email')}</FormSectionTitle>
              <Input
                value={account?.email || ''}
                readOnly
                className={commonStyles.inputDark}
              />
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
