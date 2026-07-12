'use client';

import { useState, useEffect } from 'react';
import { useTranslations } from 'next-intl';
import { useRouter } from '@/i18n/routing';
import { Header } from '@/components/ui/header';
import { Icon } from '@/components/ui/icon';
import { Spinner } from '@/components/ui/spinner';
import { ListItem } from '@/components/ui/list-item';
import { useCompaniesStore } from './use-companies-store';
import { toast } from 'react-toastify';
import { PAGE_ROUTES } from '@/lib/routes';
import { useAuthStore } from '@/lib/auth/use-auth-store';
import styles from './companies.module.css';
import { useSidebar, SidebarIcon, Sidebar } from '@/components/b2b/Sidebar';
import { cn } from '@/lib/utils';
import { ChevronRight } from 'lucide-react';

export default function CompaniesPage() {
  const router = useRouter();
  const t = useTranslations('screens.companies');
  const { isOpen, open, close } = useSidebar();

  const { hasHydrated } = useAuthStore();

  const { companies, isLoading, fetchCompanies } = useCompaniesStore();

  useEffect(() => {
    if (hasHydrated) {
      fetchCompanies().catch(() => {
        toast.error(t('toast.loadFailed'));
      });
    }
  }, [hasHydrated, fetchCompanies, t]);

  const handleCompanyClick = (id: string) => {
    router.push(PAGE_ROUTES.B2B.COMPANIES_DETAIL(id));
  };

  return (
    <div className="relative flex flex-1 flex-col overflow-hidden">
      <Header
        left={<SidebarIcon onClick={open} />}
        title={t('title')}
        right={
          <Icon
            src="/assets/b2b/plus-icon.svg"
            alt="add company"
            width={22}
            height={22}
            onClick={() => router.push(PAGE_ROUTES.B2B.COMPANIES_CREATE)}
            priority
          />
        }
      />

      <Sidebar isOpen={isOpen} onClose={close} />

      <div className={styles.content}>
        {!hasHydrated || (isLoading && companies.length === 0) ? (
          <div className={styles.emptyState}>
            <Spinner />
          </div>
        ) : companies.length === 0 ? (
          <div
            className={cn(styles.emptyState, 'cursor-pointer')}
            onClick={() => router.push(PAGE_ROUTES.B2B.COMPANIES_CREATE)}
          >
            <span className={styles.emptyStateText}>{t('noCompanies')}</span>
          </div>
        ) : (
          <div className={styles.companyList}>
            {companies.map((company) => (
              <ListItem
                key={company.id}
                className="cursor-pointer"
                onClick={() => handleCompanyClick(company.id)}
                avatarUrl={
                  company.avatarImageUrl ||
                  '/assets/b2b/avatar-placeholder.webp'
                }
                avatarAlt={company.name}
                title={company.name}
                subtitle={company.email || ''}
                rightEl={<ChevronRight size={24} className="text-secondary" />}
              />
            ))}
          </div>
        )}
      </div>
    </div>
  );
}
