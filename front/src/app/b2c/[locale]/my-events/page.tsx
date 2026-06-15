'use client';

import { useState, useEffect, useCallback, useRef, useMemo } from 'react';
import { useSearchParams } from 'next/navigation';
import { useTranslations } from 'next-intl';
import { useRouter } from '@/i18n/routing';
import {
  Trash2,
  LogOut,
  CalendarSync,
  Pencil,
} from 'lucide-react';
import { toast } from 'react-toastify';
import { Header } from '@/components/ui/header';
import { Icon } from '@/components/ui/icon';
import { Spinner } from '@/components/ui/spinner';
import { TabControl } from '@/components/ui/tab-control';
import { ModalMenu } from '@/components/ui/modal-menu';
import { ModalDialog } from '@/components/ui/modal-dialog';
import { useInView } from 'react-intersection-observer';
import { useViewportItemCount } from '@/hooks/use-viewport-item-count';
import {
  useMyEventsStore,
  MyEventItem,
} from '@/app/b2c/[locale]/my-events/use-my-events-store';
import { EventListItem } from '@/app/b2c/[locale]/my-events/_components/event-list-item';
import { PAGE_ROUTES, API_ENDPOINTS } from '@/lib/routes';
import apiClient from '@/lib/api-client';
import { useSafeBack } from '@/hooks/use-safe-back';
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
    'Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun',
    'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec',
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
          toast.error(err instanceof Error ? err.message : t('toast.loadFailed'));
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
          toast.error(err instanceof Error ? err.message : t('toast.loadFailed'));
        }
      };
      loadNext();
    }
  }, [inView, activeTab, tabState.hasMore, tabState.isLoading, tabState.hasError, tabState.events.length, limit]); // eslint-disable-line react-hooks/exhaustive-deps

  const [selectedEvent, setSelectedEvent] = useState<MyEventItem | null>(null);
  const [eventToDeleteId, setEventToDeleteId] = useState<string | null>(null);
  const [isDeleting, setIsDeleting] = useState(false);
  const lastMenuEventRef = useRef<MyEventItem | null>(null);

  const handleCardTap = useCallback(
    (event: MyEventItem) => {
      if (activeTab === 'owner') {
        router.push(PAGE_ROUTES.MEETUP_EDIT(event.id));
      } else {
        router.push(PAGE_ROUTES.MEETUP_DETAIL(event.id));
      }
    },
    [activeTab, router],
  );

  const handleMenuOpen = useCallback(
    (event: MyEventItem) => {
      lastMenuEventRef.current = event;
      setSelectedEvent(event);
    },
    [],
  );

  const handleDeleteEvent = useCallback(async () => {
    if (!eventToDeleteId) return;
    setIsDeleting(true);
    try {
      await apiClient.delete(API_ENDPOINTS.EVENTS.DELETE(eventToDeleteId));
      store.removeEvent('owner', eventToDeleteId);
      toast.success(t('toast.deleted'));
    } catch {
      toast.error(t('toast.deleteFailed'));
    } finally {
      setIsDeleting(false);
      setEventToDeleteId(null);
    }
  }, [eventToDeleteId, store, t]);

  const handleLeaveEvent = useCallback(async () => {
    const event = lastMenuEventRef.current;
    if (!event) return;
    try {
      await apiClient.post(API_ENDPOINTS.EVENTS.LEAVE(event.id));
      store.removeEvent('participant', event.id);
      toast.success(t('toast.left'));
    } catch {
      toast.error(t('toast.leaveFailed'));
    } finally {
      setSelectedEvent(null);
    }
  }, [store, t]);

  const isOwnerTab = activeTab === 'owner';

  const getMenuOptions = () => {
    if (!selectedEvent) return [];
    if (isOwnerTab) {
      return [
        {
          icon: <Pencil size={20} />,
          label: t('menu.edit'),
          onClick: () => {
            router.push(PAGE_ROUTES.MEETUP_EDIT(selectedEvent.id));
            setSelectedEvent(null);
          },
        },
        {
          icon: <Trash2 size={20} />,
          label: t('menu.delete'),
          onClick: () => {
            setEventToDeleteId(selectedEvent.id);
          },
          isDestructive: true,
        },
      ];
    }
    return [
      {
        icon: <LogOut size={20} />,
        label: t('menu.leave'),
        onClick: () => {
          handleLeaveEvent();
        },
        isDestructive: true,
      },
    ];
  };

  const emptyMessage = isOwnerTab
    ? t('emptyOwner')
    : t('emptyParticipant');

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
            setSelectedEvent(null);
            router.replace(`${PAGE_ROUTES.MY_EVENTS}?tab=${val}`, { scroll: false });
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
            <CalendarSync size={48} className="text-secondary/40" />
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

      <ModalMenu
        isOpen={!!selectedEvent}
        onClose={() => setSelectedEvent(null)}
        options={getMenuOptions()}
      />

      <ModalDialog
        isOpen={!!eventToDeleteId}
        onClose={() => setEventToDeleteId(null)}
        message={t('deleteConfirm')}
        primaryButtonText={t('cancel')}
        secondaryButtonText={isDeleting ? t('deleting') : t('delete')}
        onPrimaryAction={() => setEventToDeleteId(null)}
        onSecondaryAction={handleDeleteEvent}
      />
    </div>
  );
}
