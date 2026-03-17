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

export default function CompaniesPage() {
  const router = useRouter();
  const t = useTranslations('screens.companies');
  const [selectedCompany, setSelectedCompany] = useState<CompanyDto | null>(
    null,
  );

  const { companies, isLoading, fetchCompanies, reset } = useCompaniesStore();

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
    <div className="flex flex-1 flex-col">
      <Header
        left={
          <Icon
            src="/assets/common/profile-icon.svg"
            alt="profile"
            width={30}
            height={32}
            onClick={() => router.push(PAGE_ROUTES.B2B.HOME)}
            priority
          />
        }
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

      <div className={styles.content}>
        {isLoading && companies.length === 0 ? (
          <div className={styles.emptyState}>
            <Spinner />
          </div>
        ) : companies.length === 0 ? (
          <div className={styles.emptyState}>
            <span className={styles.emptyStateText}>
              {t('noCompanies')}
            </span>
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
