'use client';

import { useState, useEffect } from 'react';
import { useTranslations } from 'next-intl';
import { useRouter } from '@/i18n/routing';
import { Header } from '@/components/ui/header';
import { Icon } from '@/components/ui/icon';
import { ModalMenu } from '@/components/ui/modal-menu';
import { Spinner } from '@/components/ui/spinner';
import { ListItem } from '@/components/ui/list-item';
import { useCompaniesStore, CompanyDto } from './use-companies-store';
import { toast } from 'react-toastify';
import { PAGE_ROUTES } from '@/lib/routes';
import styles from './companies.module.css';
import { Sidebar, SidebarIcon, useSidebar } from '@/components/b2b/Sidebar';

export default function CompaniesPage() {
  const router = useRouter();
  const t = useTranslations('screens.companies');
  const [selectedCompany, setSelectedCompany] = useState<CompanyDto | null>(
    null,
  );
  const { isOpen, open, close } = useSidebar();

  const { companies, isLoading, fetchCompanies } = useCompaniesStore();

  useEffect(() => {
    fetchCompanies().catch(() => {
      toast.error(t('toast.loadFailed'));
    });
  }, [fetchCompanies, t]);

  const getMenuOptions = (company: CompanyDto) => [
    {
      icon: '/assets/common/venues-icon.svg',
      label: t('menu.viewCompany'),
      onClick: () => {
        setSelectedCompany(null);
      },
    },
  ];

  return (
    <div className="relative flex flex-1 flex-col overflow-hidden">
      <Header
        left={<SidebarIcon onClick={open} />}
        title={t('title')}
        right={
          <Icon
            src="/assets/friends-and-foes/plus-icon.svg"
            alt="add company"
            width={24}
            height={24}
            onClick={() => {
              /* Handle add company */
            }}
            priority
          />
        }
      />

      <Sidebar isOpen={isOpen} onClose={close} />

      <div className={styles.content}>
        {isLoading && companies.length === 0 ? (
          <div className={styles.emptyState}>
            <Spinner />
          </div>
        ) : companies.length === 0 ? (
          <div className={styles.emptyState}>
            <span className={styles.emptyStateText}>{t('noCompanies')}</span>
          </div>
        ) : (
          <div className={styles.companyList}>
            {companies.map((company) => (
              <ListItem
                key={company.id}
                className="cursor-pointer"
                onClick={() => setSelectedCompany(company)}
                avatarUrl={
                  company.avatarImageUrl || '/assets/common/venues-icon.svg'
                }
                avatarAlt={company.name}
                title={company.name}
                subtitle={company.email || ''}
                rightEl={
                  <Icon
                    src="/assets/common/more-icon.svg"
                    alt="more"
                    width={24}
                    height={24}
                    priority
                    stopPropagation
                    onClick={() => setSelectedCompany(company)}
                  />
                }
              />
            ))}
          </div>
        )}
      </div>

      <ModalMenu
        isOpen={!!selectedCompany}
        onClose={() => setSelectedCompany(null)}
        options={selectedCompany ? getMenuOptions(selectedCompany) : []}
      />
    </div>
  );
}
