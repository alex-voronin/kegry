'use client';

import { useEffect, useMemo, useState } from 'react';
import { useTranslations, useFormatter } from 'next-intl';
import { useRouter } from '@/i18n/routing';
import { ChevronDown, RefreshCw } from 'lucide-react';
import {
  BarChart,
  Bar,
  XAxis,
  YAxis,
  Tooltip,
  CartesianGrid,
  ResponsiveContainer,
} from 'recharts';
import { Header } from '@/components/ui/header';
import { ModalMenu } from '@/components/ui/modal-menu';
import { Spinner } from '@/components/ui/spinner';
import { SidebarIcon, Sidebar, useSidebar } from '@/components/b2b/Sidebar';
import { useAuthStore } from '@/lib/auth/use-auth-store';
import { useCompaniesStore } from '@/app/b2b/[locale]/companies/use-companies-store';
import { PAGE_ROUTES } from '@/lib/routes';
import { cn } from '@/lib/utils';
import { useDashboardStore, VisitsChartMode } from './use-dashboard-store';
import styles from './dashboard.module.css';

const MODES: { key: VisitsChartMode; labelKey: string }[] = [
  { key: 'Daily', labelKey: 'screens.dashboard.daily' },
  { key: 'Weekly', labelKey: 'screens.dashboard.weekly' },
  { key: 'Monthly', labelKey: 'screens.dashboard.monthly' },
];

export default function DashboardPage() {
  const t = useTranslations();
  const format = useFormatter();
  const router = useRouter();
  const { isOpen, open, close } = useSidebar();

  const { hasHydrated } = useAuthStore();

  const {
    companies,
    isLoading: isLoadingCompanies,
    fetchCompanies,
  } = useCompaniesStore();
  const {
    mode,
    venues,
    isLoading: isLoadingVisits,
    hasError,
    setMode,
    fetchVisits,
  } = useDashboardStore();

  useEffect(() => {
    if (hasHydrated) {
      fetchCompanies();
    }
  }, [hasHydrated, fetchCompanies]);

  const companiesList = useMemo(() => companies ?? [], [companies]);

  const [selectedCompanyId, setSelectedCompanyId] = useState<string | null>(
    null,
  );

  useEffect(() => {
    if (companiesList.length > 0 && !selectedCompanyId) {
      setSelectedCompanyId(companiesList[0].id);
    }
  }, [companiesList, selectedCompanyId]);

  useEffect(() => {
    if (selectedCompanyId) {
      fetchVisits(selectedCompanyId, mode);
    }
  }, [selectedCompanyId, mode, fetchVisits]);

  const [isCompanyMenuOpen, setIsCompanyMenuOpen] = useState(false);

  const selectedCompany = useMemo(
    () => companiesList.find((c) => c.id === selectedCompanyId) ?? null,
    [companiesList, selectedCompanyId],
  );

  const handleModeChange = (newMode: VisitsChartMode) => {
    setMode(newMode);
  };

  const handleRetry = () => {
    if (selectedCompanyId) {
      fetchVisits(selectedCompanyId, mode);
    }
  };

  if (!hasHydrated || isLoadingCompanies) {
    return (
      <div className="relative flex flex-1 flex-col overflow-hidden">
        <Header
          left={<SidebarIcon onClick={open} />}
          title={t('screens.sidebar.dashboard')}
        />
        <Sidebar isOpen={isOpen} onClose={close} />
        <div className="flex flex-1 items-center justify-center">
          <Spinner />
        </div>
      </div>
    );
  }

  if (companiesList.length === 0) {
    return (
      <div className="relative flex flex-1 flex-col overflow-hidden">
        <Header
          left={<SidebarIcon onClick={open} />}
          title={t('screens.sidebar.dashboard')}
        />
        <Sidebar isOpen={isOpen} onClose={close} />
        <div className={styles.content}>
          <div
            className={cn(styles.emptyState, 'cursor-pointer')}
            onClick={() => router.push(PAGE_ROUTES.B2B.COMPANIES)}
          >
            <span className={styles.emptyStateText}>
              {t('screens.dashboard.noCompanies')}
            </span>
            <span className="text-secondary mt-2 text-sm">
              {t('screens.dashboard.noCompaniesHint')}
            </span>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="relative flex flex-1 flex-col overflow-hidden">
      <Header
        left={<SidebarIcon onClick={open} />}
        title={t('screens.sidebar.dashboard')}
      />
      <Sidebar isOpen={isOpen} onClose={close} />

      <div className={styles.content}>
        {selectedCompany && companiesList.length > 1 && (
          <div
            className="flex cursor-pointer items-center gap-2 sm:rounded-xl bg-[var(--color-form-element)] px-3 py-2 text-sm"
            onClick={() => setIsCompanyMenuOpen(true)}
          >
            <span className="flex-1">{selectedCompany.name}</span>
            <ChevronDown
              size={16}
              style={{ color: 'var(--color-secondary)' }}
            />
          </div>
        )}

        {selectedCompanyId && (
          <div className={styles.visitsCard}>
            <h2 className={styles.visitsCardTitle}>
              {t('screens.dashboard.title')}
            </h2>
            <div className={styles.modeTabs}>
              {MODES.map((m) => (
                <button
                  key={m.key}
                  type="button"
                  className={`${styles.modeTab} ${mode === m.key ? styles.modeTabActive : ''}`}
                  onClick={() => handleModeChange(m.key)}
                >
                  {t(m.labelKey)}
                </button>
              ))}
            </div>

            {isLoadingVisits && (
              <div className="flex flex-1 items-center justify-center py-16">
                <Spinner />
              </div>
            )}

            {hasError && !isLoadingVisits && (
              <div className={styles.errorState}>
                <p className={styles.errorText}>
                  {t('screens.dashboard.error')}
                </p>
                <button
                  type="button"
                  onClick={handleRetry}
                  className={styles.retryButton}
                >
                  <RefreshCw size={16} className="mr-1 inline" />
                  Retry
                </button>
              </div>
            )}

            {!isLoadingVisits && !hasError && venues.length === 0 && (
              <div className={styles.emptyState}>
                <p className={styles.emptyStateText}>
                  {t('screens.dashboard.noVenues')}
                </p>
              </div>
            )}

            {!isLoadingVisits &&
              !hasError &&
              venues.map((venue) => (
                <div key={venue.venueId} className={styles.venueCard}>
                  <p className={styles.venueAddress}>{venue.address}</p>
                  <ResponsiveContainer width="100%" height={200}>
                    <BarChart
                      data={venue.dataPoints}
                      margin={{ top: 0, right: 0, bottom: 0, left: -24 }}
                    >
                      <CartesianGrid
                        stroke="rgba(255,255,255,0.06)"
                        vertical={false}
                      />
                      <XAxis
                        dataKey="label"
                        tickFormatter={(v) =>
                          mode === 'Monthly'
                            ? format.dateTime(new Date(v), { month: 'short' })
                            : format.dateTime(new Date(v), {
                                month: 'numeric',
                                day: 'numeric',
                              })
                        }
                        tick={{ fontSize: 11, fill: '#a0a0a0' }}
                        axisLine={false}
                        tickLine={false}
                      />
                      <YAxis
                        tick={{ fontSize: 11, fill: '#a0a0a0' }}
                        axisLine={false}
                        tickLine={false}
                        allowDecimals={false}
                      />
                      <Tooltip
                        labelFormatter={(v) =>
                          mode === 'Monthly'
                            ? format.dateTime(new Date(v), { month: 'short' })
                            : format.dateTime(new Date(v), {
                                month: 'numeric',
                                day: 'numeric',
                              })
                        }
                        contentStyle={{
                          background: '#292929',
                          border: '1px solid rgba(255,255,255,0.1)',
                          borderRadius: 8,
                          fontSize: 12,
                        }}
                        labelStyle={{ color: '#f0f0f0', marginBottom: 4 }}
                      />
                      <Bar
                        dataKey="count"
                        fill="#ffab10"
                        radius={[4, 4, 0, 0]}
                      />
                    </BarChart>
                  </ResponsiveContainer>
                </div>
              ))}
          </div>
        )}
      </div>

      <ModalMenu
        isOpen={isCompanyMenuOpen}
        onClose={() => setIsCompanyMenuOpen(false)}
        options={companiesList.map((c) => ({
          label: c.name,
          onClick: () => setSelectedCompanyId(c.id),
        }))}
      />
    </div>
  );
}
