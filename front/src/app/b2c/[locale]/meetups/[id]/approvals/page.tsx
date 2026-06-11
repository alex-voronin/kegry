'use client';

import { useEffect, useCallback } from 'react';
import { useParams } from 'next/navigation';
import { useTranslations } from 'next-intl';
import { Header } from '@/components/ui/header';
import { Icon } from '@/components/ui/icon';
import { Spinner } from '@/components/ui/spinner';
import { Button, ButtonVariant } from '@/components/ui/button';
import { UserCheck, UserX, CalendarCheck } from 'lucide-react';
import { useSafeBack } from '@/hooks/use-safe-back';
import { PAGE_ROUTES, API_ENDPOINTS } from '@/lib/routes';
import { toast } from 'react-toastify';
import apiClient from '@/lib/api-client';
import { calculateAge } from '@/lib/date';
import { useMeetupDetailStore, PendingUser } from '../use-meetup-detail-store';
import { ParticipantListItem } from '../../_components/participant-list-item';
import styles from './approvals.module.css';

export default function ApprovalsPage() {
  const params = useParams();
  const t = useTranslations('screens.approvals');
  const tDetail = useTranslations('screens.meetupDetail');
  const tHobbies = useTranslations('components.profile-sections.hobbies');
  const { safeBack } = useSafeBack();
  const eventId = params.id as string;

  const {
    event,
    pendingUsers,
    isLoading,
    fetchEvent,
    fetchPendingUsers,
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
      fetchPendingUsers(eventId);
    }
  }, [event, eventId, fetchPendingUsers]);

  const handleBack = () => {
    safeBack(PAGE_ROUTES.MEETUP_DETAIL(eventId));
  };

  const handleApprove = useCallback(
    async (userId: string) => {
      try {
        await apiClient.patch(
          API_ENDPOINTS.EVENTS.APPROVE_PARTICIPANT(eventId, userId),
        );
        toast.success(t('approved'));
        fetchPendingUsers(eventId);
      } catch {
        toast.error(t('actionFailed'));
      }
    },
    [eventId, fetchPendingUsers, t],
  );

  const handleReject = useCallback(
    async (userId: string) => {
      try {
        await apiClient.patch(
          API_ENDPOINTS.EVENTS.REJECT_PARTICIPANT(eventId, userId),
        );
        toast.success(t('rejected'));
        fetchPendingUsers(eventId);
      } catch {
        toast.error(t('actionFailed'));
      }
    },
    [eventId, fetchPendingUsers, t],
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
        title={t('pendingApprovalsTitle')}
      />

      <div className="flex flex-col gap-4 sm:px-4">
        <div>
          {pendingUsers.length === 0 ? (
            <p className={styles.empty}>{t('noPending')}</p>
          ) : (
            <div className={styles.card}>
              {pendingUsers.map((p: PendingUser, i: number) => (
                <div
                  key={p.id}
                  className={i < pendingUsers.length - 1 ? styles.cardItem : styles.cardItemLast}
                >
                  <ParticipantListItem
                    avatarUrl={p.avatarImageUrl}
                    firstName={p.firstName}
                    lastName={p.lastName}
                    userId={p.id}
                    hobbies={p.hobbies}
                    translateHobby={(key) => tHobbies(key as 'gym')}
                    subtitle={
                      p.birthday && (
                        <span className="text-secondary text-xs">
                          {tDetail('age')}: {calculateAge(p.birthday)}
                        </span>
                      )
                    }
                    rightEl={
                      <>
                        <Button
                          variant={ButtonVariant.Solid}
                          onClick={() => handleApprove(p.id)}
                        >
                          <UserCheck size={16} />
                        </Button>
                        <Button
                          variant={ButtonVariant.Destructive}
                          onClick={() => handleReject(p.id)}
                        >
                          <UserX size={16} />
                        </Button>
                      </>
                    }
                  />
                </div>
              ))}
            </div>
          )}
        </div>

        {event.pendingApprovalsCount === 0 && (
          <div className={styles.emptyState}>
            <div className={styles.emptyStateInner}>
              <CalendarCheck size={20} className={styles.emptyStateIcon} />
              <span className={styles.emptyStateText}>{t('allApproved')}</span>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
