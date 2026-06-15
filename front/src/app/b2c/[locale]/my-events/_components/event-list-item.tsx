'use client';

import { MoreHorizontal, CalendarCheck, Clock, Users } from 'lucide-react';
import { MyEventItem, VenueInfo } from '../use-my-events-store';
import styles from '../my-events.module.css';

interface EventListItemProps {
  event: MyEventItem;
  venue: VenueInfo | undefined;
  onTap: (event: MyEventItem) => void;
  onMenuOpen: (event: MyEventItem) => void;
  showOwnerIndicator?: boolean;
}

export function EventListItem({
  event,
  venue,
  onTap,
  onMenuOpen,
  showOwnerIndicator,
}: EventListItemProps) {
  const isExpired = new Date(event.startDateTime) < new Date();
  return (
    <div
      className={`${styles.eventCard} ${event.participationStatus === 'Approved' ? styles.eventCardJoined : ''} ${isExpired ? styles.eventCardExpired : ''}`}
      onClick={() => onTap(event)}
    >
      <img
        src={event.coverImageUrl || '/assets/event/cover-default.webp'}
        alt={event.title}
        className={styles.eventCardImage}
      />
      <div className={styles.eventCardOverlay} />
      <div className={styles.eventCardContent}>
        <div className={styles.eventCardInfo}>
          <div className={styles.eventCardTime}>
            {formatTime(event.startDateTime)}
          </div>
          <div className={styles.eventCardTitle}>
            {(event.participationStatus === 'Approved' || (showOwnerIndicator && !event.participationStatus)) && (
              <CalendarCheck size={14} className="text-accent shrink-0" />
            )}
            {event.participationStatus === 'Pending' && (
              <Clock size={14} className="text-accent shrink-0" />
            )}
            <span className="min-w-0 truncate">{event.title}</span>
          </div>
          <div className={styles.eventCardVenueRow}>
            <img
              src={venue?.avatarUrl || '/assets/common/venues-icon.svg'}
              alt=""
              className={styles.eventCardVenueAvatar}
            />
            <span className="min-w-0 truncate">{venue?.name || 'Unknown Venue'}</span>
            <span className={styles.eventCardMetaDot}>·</span>
            <span className="flex shrink-0 items-center gap-1">
              <Users size={12} />
              {event.participantsCount}
            </span>
          </div>
        </div>
        <button
          onClick={(e) => {
            e.stopPropagation();
            onMenuOpen(event);
          }}
          className="flex h-7 w-7 shrink-0 items-center justify-center rounded-full hover:bg-white/10"
        >
          <MoreHorizontal size={16} className="text-white/70" />
        </button>
      </div>
    </div>
  );
}

function formatTime(dateStr: string): string {
  const d = new Date(dateStr);
  return d.toLocaleTimeString(undefined, {
    hour: '2-digit',
    minute: '2-digit',
  });
}
