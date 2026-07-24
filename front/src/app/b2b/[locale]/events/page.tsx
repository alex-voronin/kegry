'use client';

import { useState, useEffect, useMemo } from 'react';
import { useTranslations } from 'next-intl';
import { useRouter } from '@/i18n/routing';
import { MoreHorizontal, SquarePen, Trash2, Users } from 'lucide-react';
import { toast } from 'react-toastify';
import { Header } from '@/components/ui/header';
import { Icon } from '@/components/ui/icon';
import { Spinner } from '@/components/ui/spinner';
import { useSidebar, SidebarIcon, Sidebar } from '@/components/b2b/Sidebar';
import { useEventsStore, B2BEventListItemDto } from './use-events-store';
import { useVenuesStore } from '../venues/use-venues-store';
import { ModalMenu } from '@/components/ui/modal-menu';
import { ModalDialog } from '@/components/ui/modal-dialog';
import { ButtonVariant } from '@/components/ui/button';
import { PAGE_ROUTES, API_ENDPOINTS } from '@/lib/routes';
import apiClient from '@/lib/api-client';
import { rethrowApiError } from '@/lib/api-error';
import styles from '../companies/companies.module.css';
import eventStyles from './events.module.css';

type DateGroup = {
  dateKey: string;
  label: string;
  events: B2BEventListItemDto[];
};

function groupByDate(events: B2BEventListItemDto[]): DateGroup[] {
  const groups = new Map<string, B2BEventListItemDto[]>();

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

function formatTime(dateStr: string): string {
  const d = new Date(dateStr);
  return d.toLocaleTimeString(undefined, {
    hour: '2-digit',
    minute: '2-digit',
  });
}

export default function EventsPage() {
  const router = useRouter();
  const t = useTranslations('screens.events');
  const { isOpen, open, close } = useSidebar();

  const { events, isLoading, fetchEvents } = useEventsStore();
  const { venues, fetchVenues } = useVenuesStore();

  const [isVenueMenuOpen, setIsVenueMenuOpen] = useState(false);
  const [selectedEventId, setSelectedEventId] = useState<string | null>(null);
  const [isEventMenuOpen, setIsEventMenuOpen] = useState(false);
  const [isDeleteModalOpen, setIsDeleteModalOpen] = useState(false);
  const [isDeleting, setIsDeleting] = useState(false);

  const dateGroups = useMemo(() => groupByDate(events), [events]);

  useEffect(() => {
    fetchEvents().catch(() => toast.error(t('toast.loadFailed')));
    fetchVenues().catch(() => {});
  }, [fetchEvents, fetchVenues, t]);

  const handleEventMenuOpen = (id: string, e: React.MouseEvent) => {
    e.stopPropagation();
    setSelectedEventId(id);
    setIsEventMenuOpen(true);
  };

  const handleEditEvent = () => {
    if (!selectedEventId) return;
    setIsEventMenuOpen(false);
    router.push(PAGE_ROUTES.B2B.EVENTS_EDIT(selectedEventId));
  };

  const handleDeleteEvent = () => {
    setIsEventMenuOpen(false);
    setIsDeleteModalOpen(true);
  };

  const handleDeleteConfirm = async () => {
    if (!selectedEventId) return;
    setIsDeleting(true);
    try {
      await apiClient.delete(API_ENDPOINTS.B2B.EVENTS.DETAILS(selectedEventId));
      fetchEvents();
    } catch (error) {
      rethrowApiError(error, t('toast.deleteFailed'));
    } finally {
      setIsDeleting(false);
      setIsDeleteModalOpen(false);
      setSelectedEventId(null);
    }
  };

  const handleAddEvent = async () => {
    let currentVenues = venues;
    if (currentVenues.length === 0) {
      await fetchVenues();
      currentVenues = useVenuesStore.getState().venues;
    }

    if (currentVenues.length === 0) {
      toast.error(t('toast.loadFailed'));
      return;
    }

    if (currentVenues.length === 1) {
      router.push(PAGE_ROUTES.B2B.EVENTS_CREATE(currentVenues[0].id));
    } else {
      setIsVenueMenuOpen(true);
    }
  };

  return (
    <div className="relative flex flex-1 flex-col overflow-hidden">
      <Header
        left={<SidebarIcon onClick={open} />}
        title={t('title')}
        right={
          venues.length > 0 ? (
            <Icon
              src="/assets/b2b/plus-icon.svg"
              alt="add event"
              width={22}
              height={22}
              onClick={handleAddEvent}
              priority
            />
          ) : undefined
        }
      />

      <Sidebar isOpen={isOpen} onClose={close} />

      <ModalMenu
        isOpen={isVenueMenuOpen}
        onClose={() => setIsVenueMenuOpen(false)}
        options={venues.map((venue) => ({
          label: venue.address,
          onClick: () => router.push(PAGE_ROUTES.B2B.EVENTS_CREATE(venue.id)),
        }))}
      />

      <ModalMenu
        isOpen={isEventMenuOpen}
        onClose={() => setIsEventMenuOpen(false)}
        options={[
          {
            icon: <SquarePen size={24} className="text-primary" />,
            label: 'Edit',
            onClick: handleEditEvent,
          },
          {
            icon: <Trash2 size={24} className="text-error" />,
            label: 'Delete',
            onClick: handleDeleteEvent,
            isDestructive: true,
          },
        ]}
      />

      <ModalDialog
        isOpen={isDeleteModalOpen}
        onClose={() => setIsDeleteModalOpen(false)}
        message="Are you sure you want to delete this event?"
        primaryButtonText="Cancel"
        secondaryButtonText={isDeleting ? 'Deleting...' : 'Delete'}
        secondaryButtonVariant={ButtonVariant.Destructive}
        onPrimaryAction={() => setIsDeleteModalOpen(false)}
        onSecondaryAction={handleDeleteConfirm}
      />

      <div className={styles.content}>
        {isLoading && events.length === 0 ? (
          <div className={styles.emptyState}>
            <Spinner />
          </div>
        ) : events.length === 0 ? (
          <div className={styles.emptyState}>
            <span className={styles.emptyStateText}>{t('noEvents')}</span>
          </div>
        ) : (
          <div className={eventStyles.eventList}>
            {dateGroups.map((group) => (
              <div key={group.dateKey}>
                <div className={eventStyles.dateSeparator}>{group.label}</div>
                <div className="flex flex-col gap-0.5">
                  {group.events.map((event) => (
                    <EventCard
                      key={event.id}
                      event={event}
                      onMenuOpen={(e) => handleEventMenuOpen(event.id, e)}
                    />
                  ))}
                </div>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}

function EventCard({
  event,
  onMenuOpen,
}: {
  event: B2BEventListItemDto;
  onMenuOpen: (e: React.MouseEvent) => void;
}) {
  const isExpired = new Date(event.startDateTime) < new Date();

  const handleClick = (e: React.MouseEvent) => {
    if ((e.target as HTMLElement).closest('button')) return;
    onMenuOpen(e);
  };

  return (
    <div
      onClick={handleClick}
      className={`${eventStyles.card} ${isExpired ? eventStyles.cardExpired : ''}`}
    >
      {event.coverImageUrl ? (
        <img
          src={event.coverImageUrl}
          alt={event.title}
          className={eventStyles.cardImage}
        />
      ) : null}
      <div className={eventStyles.cardOverlay} />

      <div className={eventStyles.cardContent}>
        <div className={eventStyles.cardInfo}>
          <div className={eventStyles.cardRow}>
            <span className={eventStyles.cardTime}>
              {formatTime(event.startDateTime)}
            </span>
            <span className={eventStyles.cardTitle}>{event.title}</span>
          </div>
          <div className={eventStyles.cardMeta}>
            <span className="truncate">{event.venueAddress}</span>
            <span className="flex shrink-0 items-center gap-1">
              <Users size={12} />
              {event.participantsCount}
            </span>
          </div>
        </div>

        <button
          onClick={(e) => {
            e.stopPropagation();
            onMenuOpen(e);
          }}
          className={eventStyles.cardMenuButton}
        >
          <MoreHorizontal size={16} className="text-primary" />
        </button>
      </div>
    </div>
  );
}
