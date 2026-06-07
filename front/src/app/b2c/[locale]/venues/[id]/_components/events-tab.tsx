'use client';

import { useEffect, useMemo } from 'react';
import { useInView } from 'react-intersection-observer';
import { useRouter } from '@/i18n/routing';
import { Spinner } from '@/components/ui/spinner';
import { cn } from '@/lib/utils';
import { Users, MoreHorizontal, CheckCircle } from 'lucide-react';
import { PAGE_ROUTES } from '@/lib/routes';
import styles from '../venue-detail.module.css';
import type { EventListItem } from '../../../map/_components/types';

type DateGroup = {
  dateKey: string;
  label: string;
  events: EventListItem[];
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

function formatTime(dateStr: string): string {
  const d = new Date(dateStr);
  return d.toLocaleTimeString(undefined, {
    hour: '2-digit',
    minute: '2-digit',
  });
}

function groupByDate(events: EventListItem[]): DateGroup[] {
  const groups = new Map<string, EventListItem[]>();

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

interface EventsTabProps {
  venueEvents: EventListItem[];
  isEventsLoading: boolean;
  hasMoreEvents: boolean;
  onLoadMore: () => void;
  onEventMenuOpen: (eventId: string, e: React.MouseEvent) => void;
  onEmpty: () => string;
}

export function EventsTab({
  venueEvents,
  isEventsLoading,
  hasMoreEvents,
  onLoadMore,
  onEventMenuOpen,
  onEmpty,
}: EventsTabProps) {
  const router = useRouter();
  const dateGroups = useMemo(() => groupByDate(venueEvents), [venueEvents]);
  const { ref: eventsBottomRef, inView: eventsInView } = useInView({
    threshold: 0,
    rootMargin: '200px',
  });

  useEffect(() => {
    if (eventsInView && hasMoreEvents && !isEventsLoading) {
      onLoadMore();
    }
  }, [eventsInView, hasMoreEvents, isEventsLoading, onLoadMore]);

  if (isEventsLoading && venueEvents.length === 0) {
    return (
      <div className={styles.section}>
        <div className="flex justify-center py-8">
          <Spinner />
        </div>
      </div>
    );
  }

  if (venueEvents.length === 0) {
    return (
      <div className={styles.section}>
        <div className="text-secondary py-8 text-center text-sm">
          {onEmpty()}
        </div>
      </div>
    );
  }

  return (
    <div className={styles.section}>
      <div className={styles.eventList}>
        {dateGroups.map((group) => (
          <div key={group.dateKey}>
            <div className={styles.dateSeparator}>{group.label}</div>
            <div className="flex flex-col gap-1">
              {group.events.map((ev) => (
                <div
                  key={ev.id}
                  className={cn(
                    styles.eventCard,
                    ev.isJoined && styles.eventCardJoined,
                  )}
                  onClick={(e) => onEventMenuOpen(ev.id, e)}
                >
                  {ev.coverImageUrl ? (
                    <img
                      src={ev.coverImageUrl}
                      alt={ev.title}
                      className={styles.eventCardImage}
                    />
                  ) : (
                    <img
                      src="/assets/event/cover-default.webp"
                      alt={ev.title}
                      className={styles.eventCardImage}
                    />
                  )}
                  <div className={styles.eventCardOverlay} />
                  <div className={styles.eventCardContent}>
                    <div className={styles.eventCardInfo}>
                      <div className={styles.eventCardRow}>
                        <span className={styles.eventCardTime}>
                          {formatTime(ev.startDateTime)}
                        </span>
                        <span className={styles.eventCardTitle}>
                          {ev.title}
                        </span>
                      </div>
                    </div>
                    <div className="flex shrink-0 items-center gap-2">
                      {ev.type === 'Meetup' && ev.creatorAvatarUrl && (
                        <button
                          type="button"
                          onClick={(e) => {
                            e.stopPropagation();
                            router.push(PAGE_ROUTES.USER_PROFILE(ev.creatorId));
                          }}
                          className="h-7 w-7 shrink-0 overflow-hidden rounded-full border border-white/20"
                        >
                          <img
                            src={ev.creatorAvatarUrl}
                            alt=""
                            className="h-full w-full object-cover"
                          />
                        </button>
                      )}
                      <span className="flex items-center gap-1">
                        {ev.isJoined && (
                          <CheckCircle size={12} className="text-accent" />
                        )}
                        <Users size={12} />
                        {ev.participantsCount}
                      </span>
                      <div className="flex h-7 w-7 items-center justify-center">
                        <MoreHorizontal size={16} className="text-white/70" />
                      </div>
                    </div>
                  </div>
                </div>
              ))}
            </div>
          </div>
        ))}
        {hasMoreEvents && (
          <div ref={eventsBottomRef} className="flex justify-center py-4">
            <Spinner />
          </div>
        )}
      </div>
    </div>
  );
}
