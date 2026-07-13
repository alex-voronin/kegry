'use client';

import { useState, useEffect, useCallback, useMemo } from 'react';
import { useSearchParams } from 'next/navigation';
import { useTranslations } from 'next-intl';
import { useRouter } from '@/i18n/routing';
import { CalendarClock } from 'lucide-react';
import { toast } from 'react-toastify';
import { Header } from '@/components/ui/header';
import { Icon } from '@/components/ui/icon';
import { Spinner } from '@/components/ui/spinner';
import { TabControl } from '@/components/ui/tab-control';
import { useInView } from 'react-intersection-observer';
import { useViewportItemCount } from '@/hooks/use-viewport-item-count';
import {
  useMyEventsStore,
  MyEventItem,
} from '@/app/b2c/[locale]/my-events/use-my-events-store';
import { EventListItem } from '@/app/b2c/[locale]/my-events/_components/event-list-item';
import { PAGE_ROUTES } from '@/lib/routes';
import { useSafeBack } from '@/hooks/use-safe-back';
import { VenueSelectorSheet } from '@/app/b2c/[locale]/map/_components/venue-selector-sheet';
import { EventActionSheet } from '@/components/common/event-action-sheet';
import { useAuthStore } from '@/lib/auth/use-auth-store';
import { useEventActions } from '@/hooks/use-event-actions';
import { OnboardingGuardDialog } from '@/components/common/onboarding-guard-dialog';
import styles from './my-events.module.css';

type TabType = 'owner' | 'participant';

type DateGroup = {
  dateKey: string;
  label: string;
  events: MyEventItem[];
};

function formatDateLabel(dateKey: string): string {
  const [, month, day] = dateKey.split('-');
  const monthNames = [
    'Jan',
    'Feb',
    'Mar',
    'Apr',
    'May',
    'Jun',
    'Jul',
    'Aug',
    'Sep',
    'Oct',
    'Nov',
    'Dec',
  ];
  return `${monthNames[parseInt(month, 10) - 1]} ${parseInt(day, 10)}`;
}

function groupByDate(events: MyEventItem[]): DateGroup[] {
  const groups = new Map<string, MyEventItem[]>();

  for (const ev of events) {
    const d = new Date(ev.startDateTime);
    const key = `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
    if (!groups.has(key)) groups.set(key, []);
    groups.get(key)!.push(ev);
  }

  const today = new Date();
  const todayKey = `${today.getFullYear()}-${String(today.getMonth() + 1).padStart(2, '0')}-${String(today.getDate()).padStart(2, '0')}`;
  const tomorrow = new Date(today);
  tomorrow.setDate(tomorrow.getDate() + 1);
  const tomorrowKey = `${tomorrow.getFullYear()}-${String(tomorrow.getMonth() + 1).padStart(2, '0')}-${String(tomorrow.getDate()).padStart(2, '0')}`;

  return Array.from(groups.entries())
    .sort(([a], [b]) => a.localeCompare(b))
    .map(([key, evs]) => ({
      dateKey: key,
      label:
        key === todayKey
          ? 'Today'
          : key === tomorrowKey
            ? 'Tomorrow'
            : formatDateLabel(key),
      events: evs,
    }));
}

export default function MyEventsScreen() {
  const router = useRouter();
  const t = useTranslations('screens.myEvents');
  const { safeBack } = useSafeBack();

  const searchParams = useSearchParams();
  const [activeTab, setActiveTab] = useState<TabType>(
    () => (searchParams.get('tab') as TabType) || 'owner',
  );

  const store = useMyEventsStore();
  const tabState = store[activeTab];
  const { venueCache } = store;

  const dateGroups = useMemo(
    () => groupByDate(tabState.events),
    [tabState.events],
  );

  const { ref: pageBottomRef, inView } = useInView({
    threshold: 0,
    rootMargin: '200px',
  });

  const limit = useViewportItemCount({
    itemHeight: 72,
    reservedHeight: 60,
    buffer: 4,
  });

  useEffect(() => {
    return () => {
      store.reset();
    };
  }, []); // eslint-disable-line react-hooks/exhaustive-deps

  useEffect(() => {
    if (
      tabState.events.length === 0 &&
      !tabState.isLoading &&
      tabState.hasMore &&
      !tabState.hasError
    ) {
      const load = async () => {
        try {
          await store.fetchEvents(activeTab, 0, limit);
        } catch (err: unknown) {
          toast.error(
            err instanceof Error ? err.message : t('toast.loadFailed'),
          );
        }
      };
      load();
    }
  }, [activeTab, limit]); // eslint-disable-line react-hooks/exhaustive-deps

  useEffect(() => {
    if (
      inView &&
      tabState.hasMore &&
      !tabState.isLoading &&
      !tabState.hasError &&
      tabState.events.length > 0
    ) {
      const loadNext = async () => {
        try {
          await store.fetchEvents(activeTab, tabState.events.length, limit);
        } catch (err: unknown) {
          toast.error(
            err instanceof Error ? err.message : t('toast.loadFailed'),
          );
        }
      };
      loadNext();
    }
  }, [
    inView,
    activeTab,
    tabState.hasMore,
    tabState.isLoading,
    tabState.hasError,
    tabState.events.length,
    limit,
  ]); // eslint-disable-line react-hooks/exhaustive-deps

  const [isVenueSelectorOpen, setIsVenueSelectorOpen] = useState(false);
  const [menuEvent, setMenuEvent] = useState<MyEventItem | null>(null);
  const userId = useAuthStore((s) => s.userId);
  const isOwnerTab = activeTab === 'owner';
  const {
    requireProfile,
    showOnboardingDialog,
    setShowOnboardingDialog,
    handleCreateProfile,
  } = useEventActions();

  const handleCardTap = useCallback(
    (event: MyEventItem) => {
      if (activeTab === 'owner') {
        setMenuEvent(event);
      } else {
        router.push(PAGE_ROUTES.MEETUP_DETAIL(event.id));
      }
    },
    [activeTab, router],
  );

  const handleMenuOpen = useCallback((event: MyEventItem) => {
    setMenuEvent(event);
  }, []);

  const handleAddMeetupClick = useCallback(() => {
    requireProfile(() => {
      setIsVenueSelectorOpen(true);
    });
  }, [requireProfile]);

  const handleSelectVenue = useCallback(
    (venueId: string, _venueName: string) => {
      router.push(PAGE_ROUTES.MEETUP_CREATE(venueId));
    },
    [router],
  );

  const emptyMessage = isOwnerTab ? t('emptyOwner') : t('emptyParticipant');

  return (
    <div className="flex flex-1 flex-col">
      <Header
        left={
          <Icon
            src="/assets/common/disclosure-left-icon.svg"
            alt="back"
            width={14}
            height={27}
            onClick={() => safeBack(PAGE_ROUTES.MAP)}
            priority
          />
        }
        title={t('title')}
        right={
          isOwnerTab ? (
            <Icon
              src="/assets/common/plus-icon.svg"
              alt="add meetup"
              width={22}
              height={22}
              onClick={handleAddMeetupClick}
              priority
            />
          ) : undefined
        }
      />

      <div className={styles.tabBar}>
        <TabControl
          options={[
            { value: 'owner', label: t('ownerTab') },
            { value: 'participant', label: t('participantTab') },
          ]}
          value={activeTab}
          onChange={(val) => {
            setActiveTab(val as TabType);
            setMenuEvent(null);
            router.replace(`${PAGE_ROUTES.MY_EVENTS}?tab=${val}`, {
              scroll: false,
            });
          }}
        />
      </div>

      <div className={styles.content}>
        {tabState.isLoading && tabState.events.length === 0 ? (
          <div className={styles.emptyState}>
            <Spinner />
          </div>
        ) : tabState.events.length === 0 ? (
          <div className={styles.emptyState}>
            <CalendarClock size={48} className="text-secondary/40" />
            <span>{emptyMessage}</span>
          </div>
        ) : (
          <div className={styles.eventList}>
            {dateGroups.map((group) => (
              <div key={group.dateKey}>
                <div className={styles.dateSeparator}>{group.label}</div>
                <div className="flex flex-col gap-0.5">
                  {group.events.map((event) => (
                    <EventListItem
                      key={event.id}
                      event={event}
                      venue={venueCache[event.venueId]}
                      onTap={handleCardTap}
                      onMenuOpen={handleMenuOpen}
                      showOwnerIndicator={isOwnerTab}
                    />
                  ))}
                </div>
              </div>
            ))}
            {tabState.hasMore && (
              <div ref={pageBottomRef} className="flex justify-center py-4">
                <Spinner />
              </div>
            )}
          </div>
        )}
      </div>

      <VenueSelectorSheet
        isOpen={isVenueSelectorOpen}
        onClose={() => setIsVenueSelectorOpen(false)}
        onSelectVenue={handleSelectVenue}
        initialVenues={[]}
      />

      <EventActionSheet
        event={menuEvent}
        currentUserId={userId}
        onClose={() => setMenuEvent(null)}
        onJoined={() => {}}
        onLeft={(id) => store.removeEvent(activeTab, id)}
        onDeleted={(id) => store.removeEvent('owner', id)}
        onEdit={(id) => router.push(PAGE_ROUTES.MEETUP_EDIT(id))}
      />

      <OnboardingGuardDialog
        isOpen={showOnboardingDialog}
        onClose={() => setShowOnboardingDialog(false)}
        onCreateProfile={handleCreateProfile}
      />
    </div>
  );
}
