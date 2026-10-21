'use client';

import { useEffect, useCallback } from 'react';
import { useParams } from 'next/navigation';
import { useTranslations } from 'next-intl';
import { Header } from '@/components/ui/header';
import { Icon } from '@/components/ui/icon';
import { Spinner } from '@/components/ui/spinner';
import { Button, ButtonVariant } from '@/components/ui/button';
import { UserX, CalendarClock } from 'lucide-react';
import { useRouter } from '@/i18n/routing';
import { useSafeBack } from '@/hooks/use-safe-back';
import { PAGE_ROUTES, API_ENDPOINTS } from '@/lib/routes';
import { toast } from 'react-toastify';
import apiClient from '@/lib/api-client';
import { calculateAge } from '@/lib/date';
import { useMeetupDetailStore, PendingUser } from '../use-meetup-detail-store';
import { ParticipantListItem } from '../../_components/participant-list-item';
import styles from './participants.module.css';

export default function ParticipantsPage() {
  const params = useParams();
  const t = useTranslations('screens.participants');
  const tDetail = useTranslations('screens.meetupDetail');
  const tHobbies = useTranslations('components.profile-sections.hobbies');
  const router = useRouter();
  const { safeBack } = useSafeBack();
  const eventId = params.id as string;

  const {
    event,
    approvedUsers,
    isLoading,
    fetchEvent,
    fetchApprovedUsers,
    removeApprovedUser,
    reset,
  } = useMeetupDetailStore();

  useEffect(() => {
    if (eventId) {
      fetchEvent(eventId);
    }
    return () => {
      reset();
    };
  }, [eventId]);

  useEffect(() => {
    if (event) {
      fetchApprovedUsers(eventId);
    }
  }, [event, eventId, fetchApprovedUsers]);

  const handleBack = () => {
    safeBack(PAGE_ROUTES.MEETUP_DETAIL(eventId));
  };

  const handleReject = useCallback(
    async (userId: string) => {
      try {
        await apiClient.patch<void>(
          API_ENDPOINTS.EVENTS.REJECT_PARTICIPANT(eventId, userId),
        );
        removeApprovedUser(userId);
        toast.success(t('rejected'));
      } catch {
        toast.error(t('actionFailed'));
      }
    },
    [eventId, removeApprovedUser, t],
  );

  if (isLoading) {
    return (
      <div className="flex min-h-screen items-center justify-center">
        <Spinner />
      </div>
    );
  }

  if (!event) {
    return (
      <div className="flex min-h-screen flex-col">
        <Header
          left={
            <Icon
              src="/assets/common/disclosure-left-icon.svg"
              alt="back"
              width={14}
              height={27}
              onClick={handleBack}
            />
          }
        />
        <div className="flex flex-1 items-center justify-center text-white/50">
          Event not found
        </div>
      </div>
    );
  }

  return (
    <div className="flex min-h-screen flex-col">
      <Header
        left={
          <Icon
            src="/assets/common/disclosure-left-icon.svg"
            alt="back"
            width={14}
            height={27}
            onClick={handleBack}
          />
        }
        title={t('title')}
      />

      <div className="flex flex-col gap-4 sm:px-4">
        {approvedUsers.length === 0 ? (
          <div className={styles.emptyState}>
            <div className={styles.emptyStateInner}>
              <CalendarClock size={20} className={styles.emptyStateIcon} />
              <span className={styles.emptyStateText}>
                {t('noParticipants')}
              </span>
            </div>
          </div>
        ) : (
          <div className={styles.card}>
            {approvedUsers.map((p: PendingUser, i: number) => (
              <div
                key={p.id}
                className={
                  i < approvedUsers.length - 1
                    ? styles.cardItem
                    : styles.cardItemLast
                }
              >
                <ParticipantListItem
                  avatarUrl={p.avatarImageUrl}
                  firstName={p.firstName}
                  lastName={p.lastName}
                  userId={p.id}
                  hobbies={p.hobbies}
                  translateHobby={(key) => tHobbies(key as 'gym')}
                  onClick={() => router.push(PAGE_ROUTES.USER_PROFILE(p.id))}
                  subtitle={
                    p.birthday && (
                      <span className="text-secondary text-xs">
                        {tDetail('age')}: {calculateAge(p.birthday)}
                      </span>
                    )
                  }
                  rightEl={
                    p.id !== event.creatorId ? (
                      <Button
                        variant={ButtonVariant.Destructive}
                        onClick={() => handleReject(p.id)}
                      >
                        <UserX size={16} />
                      </Button>
                    ) : undefined
                  }
                />
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}
