'use client';

import { useState, useEffect, useCallback } from 'react';
import { useParams } from 'next/navigation';
import { useTranslations, useLocale } from 'next-intl';
import { useRouter } from '@/i18n/routing';
import { Header } from '@/components/ui/header';
import { Icon } from '@/components/ui/icon';
import { Spinner } from '@/components/ui/spinner';
import { Button, ButtonVariant } from '@/components/ui/button';
import { ModalMenu } from '@/components/ui/modal-menu';
import { PAGE_ROUTES } from '@/lib/routes';
import { toast } from 'react-toastify';
import {
  MapPin,
  Clock,
  Users,
  UserCheck,
  CheckCircle,
  CalendarClock,
  Phone,
  Mail,
  Globe,
  Pencil,
  UserPlus,
  UserMinus,
  UserX,
} from 'lucide-react';
import { useSafeBack } from '@/hooks/use-safe-back';
import { useEventActions } from '@/hooks/use-event-actions';
import { OnboardingGuardDialog } from '@/components/common/onboarding-guard-dialog';
import { useMeetupDetailStore, PendingUser } from './use-meetup-detail-store';
import { calculateAge } from '@/lib/date';
import { messengerIcons } from '@/components/ui/messenger-icons';
import apiClient from '@/lib/api-client';
import { API_ENDPOINTS } from '@/lib/routes';
import { ParticipantListItem } from '../_components/participant-list-item';
import styles from './meetup-detail.module.css';

function openContact(type: string, value: string) {
  switch (type) {
    case 'Telegram':
      window.open(`https://t.me/${value.replace(/^@/, '')}`, '_blank');
      break;
    case 'WhatsApp':
      window.open(`https://wa.me/${value.replace(/[^\d+]/g, '')}`, '_blank');
      break;
    case 'Viber':
      window.open(`viber://chat?number=${encodeURIComponent(value)}`, '_blank');
      break;
    case 'Phone':
      window.open(`tel:${value}`, '_blank');
      break;
    case 'Email':
      window.open(`mailto:${value}`, '_blank');
      break;
  }
}

export default function MeetupDetailPage() {
  const params = useParams();
  const router = useRouter();
  const t = useTranslations('screens.meetupDetail');
  const tHobbies = useTranslations('components.profile-sections.hobbies');
  const [isInlineApprovedLoading, setIsInlineApprovedLoading] = useState(false);
  const locale = useLocale();
  const { safeBack } = useSafeBack();
  const eventId = params.id as string;
  const [isMenuOpen, setIsMenuOpen] = useState(false);
  const {
    requireProfile,
    showOnboardingDialog,
    setShowOnboardingDialog,
    handleCreateProfile,
  } = useEventActions();

  const {
    event,
    venue,
    isLoading,
    isJoining,
    fetchEvent,
    joinEvent,
    leaveEvent,
    approvedUsers,
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
  }, [eventId, fetchEvent, reset]);

  const handleBack = () => {
    safeBack(PAGE_ROUTES.MAP);
  };

  const handleJoinLeave = async () => {
    if (!event) return;
    requireProfile(async () => {
      try {
        if (
          event.participationStatus === 'Approved' ||
          event.participationStatus
        ) {
          await leaveEvent(event.id);
        } else {
          await joinEvent(event.id);
        }
      } catch {
        toast.error(t('actionFailed'));
      }
    });
  };

  const showInlineParticipants = !event
    ? false
    : event.maxParticipants !== null;

  useEffect(() => {
    if (showInlineParticipants && eventId) {
      setIsInlineApprovedLoading(true);
      fetchApprovedUsers(eventId, [event!.creatorId]).finally(() =>
        setIsInlineApprovedLoading(false),
      );
    }
  }, [showInlineParticipants, eventId, fetchApprovedUsers]);

  const handleInlineReject = useCallback(
    async (userId: string) => {
      try {
        await apiClient.patch(
          API_ENDPOINTS.EVENTS.REJECT_PARTICIPANT(eventId, userId),
        );
        removeApprovedUser(userId);
      } catch {
        toast.error(t('actionFailed'));
      }
    },
    [eventId, removeApprovedUser, t],
  );

  const handleEdit = () => {
    router.push(PAGE_ROUTES.MEETUP_EDIT(eventId));
  };

  if (isLoading) {
    return (
      <div className={styles.loadingContainer}>
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
        <div className={styles.loadingSpinner}>
          <Spinner />
        </div>
      </div>
    );
  }

  if (!event) {
    return (
      <div className={styles.emptyState}>
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
        <div className={styles.emptyStateContent}>Event not found</div>
      </div>
    );
  }

  const formattedDate = new Date(event.startDateTime).toLocaleDateString(
    locale,
    {
      weekday: 'long',
      year: 'numeric',
      month: 'long',
      day: 'numeric',
    },
  );

  const formattedTime = new Date(event.startDateTime).toLocaleTimeString(
    locale,
    {
      hour: '2-digit',
      minute: '2-digit',
    },
  );

  return (
    <div className={styles.wrapper}>
      {/* Cover Image with Title overlay */}
      <div className={styles.coverContainer}>
        <Header
          className="absolute! top-0! left-0! z-20! w-full! bg-transparent!"
          left={
            <Icon
              src="/assets/common/disclosure-left-icon.svg"
              alt="back"
              width={14}
              height={27}
              onClick={handleBack}
            />
          }
          right={
            <Icon
              src="/assets/common/more-icon.svg"
              alt="more"
              width={24}
              height={24}
              onClick={() => setIsMenuOpen(true)}
            />
          }
        />
        <div className={styles.coverTitle}>
          <span className={styles.coverTitleText}>{event.title}</span>
        </div>
        {event.coverImageUrl ? (
          <>
            <img
              src={event.coverImageUrl}
              alt={event.title}
              className={styles.coverImage}
            />
            <div className={styles.coverOverlay} />
          </>
        ) : (
          <img
            src="/assets/event/cover-default.webp"
            alt=""
            className={`${styles.coverImage} opacity-30`}
          />
        )}

        {/* Date & Creator over the cover */}
        <div className={styles.coverBottomOverlay}>
          <div className={styles.coverInfoRow}>
            <div className={styles.coverInfoLeft}>
              <Clock size={14} />
              <span>
                {formattedDate} {formattedTime}
              </span>
            </div>
            <div className={styles.coverInfoRight}>
              {(event.participationStatus === 'Approved' ||
                event.isCreator) && (
                <CalendarClock size={14} className="text-accent" />
              )}
              <Users size={14} />
              <span>
                {event.maxParticipants
                  ? `${event.participantsCount}/${event.maxParticipants}`
                  : event.participantsCount}
              </span>
            </div>
          </div>
        </div>
      </div>

      {/* Content */}
      <div className={styles.contentWrapper}>
        <div className={styles.sectionList}>
          {/* Description */}
          {event.description && (
            <p
              className={`${styles.sectionCard} text-primary text-sm leading-relaxed`}
            >
              {event.description}
            </p>
          )}

          {/* Creator + Participants */}
          {(() => {
            const creatorProfile =
              event.type === 'Meetup'
                ? approvedUsers.find(
                    (p: PendingUser) => p.id === event.creatorId,
                  )
                : null;

            if (showInlineParticipants) {
              const nonCreatorParticipants = approvedUsers.filter(
                (p: PendingUser) => p.id !== event.creatorId,
              );
              const hasItems =
                (creatorProfile ? 1 : 0) + nonCreatorParticipants.length > 0;

              if (!hasItems) return null;

              return (
                <div className="flex flex-col gap-1 overflow-hidden sm:rounded-xl">
                  {creatorProfile && (
                    <ParticipantListItem
                      onClick={() =>
                        router.push(PAGE_ROUTES.USER_PROFILE(event.creatorId))
                      }
                      avatarUrl={creatorProfile.avatarImageUrl}
                      firstName={creatorProfile.firstName}
                      lastName={creatorProfile.lastName}
                      userId={creatorProfile.id}
                      hobbies={creatorProfile.hobbies}
                      translateHobby={(key) => tHobbies(key as 'gym')}
                      subtitle={
                        creatorProfile.birthday && (
                          <span className="text-secondary text-xs">
                            {t('age')}: {calculateAge(creatorProfile.birthday)}
                          </span>
                        )
                      }
                    />
                  )}
                  {nonCreatorParticipants.map((p: PendingUser) => (
                    <ParticipantListItem
                      key={p.id}
                      avatarUrl={p.avatarImageUrl}
                      firstName={p.firstName}
                      lastName={p.lastName}
                      userId={p.id}
                      hobbies={p.hobbies}
                      translateHobby={(key) => tHobbies(key as 'gym')}
                      onClick={() =>
                        router.push(PAGE_ROUTES.USER_PROFILE(p.id))
                      }
                      subtitle={
                        p.birthday && (
                          <span className="text-secondary text-xs">
                            {t('age')}: {calculateAge(p.birthday)}
                          </span>
                        )
                      }
                      rightEl={
                        event.isCreator ? (
                          <Button
                            variant={ButtonVariant.Destructive}
                            className="shrink-0"
                            onClick={() => handleInlineReject(p.id)}
                          >
                            <UserX size={16} />
                          </Button>
                        ) : undefined
                      }
                    />
                  ))}
                  {isInlineApprovedLoading && approvedUsers.length === 0 && (
                    <div className="flex items-center justify-center px-4 py-3">
                      <Spinner />
                    </div>
                  )}
                </div>
              );
            }

            // Creator card (non-inline, e.g. large meetup or standalone)
            if (creatorProfile) {
              return (
                <div className="bg-form-element overflow-hidden sm:rounded-xl">
                  <ParticipantListItem
                    onClick={() =>
                      router.push(PAGE_ROUTES.USER_PROFILE(event.creatorId))
                    }
                    avatarUrl={creatorProfile.avatarImageUrl}
                    firstName={creatorProfile.firstName}
                    lastName={creatorProfile.lastName}
                    userId={creatorProfile.id}
                    hobbies={creatorProfile.hobbies}
                    translateHobby={(key) => tHobbies(key as 'gym')}
                    subtitle={
                      creatorProfile.birthday && (
                        <span className="text-secondary text-xs">
                          {t('age')}: {calculateAge(creatorProfile.birthday)}
                        </span>
                      )
                    }
                  />
                </div>
              );
            }

            // Participants nav link (creator only, large meetup)
            if (event.isCreator && event.participantsCount > 0) {
              return (
                <div
                  className={`${styles.sectionCard} ${styles.clickable}`}
                  onClick={() =>
                    router.push(PAGE_ROUTES.PARTICIPANTS(event.id))
                  }
                >
                  <div className="flex items-center gap-2">
                    <CalendarClock
                      size={20}
                      className="text-secondary shrink-0"
                    />
                    <span className="text-primary text-sm">
                      {t('participants')}
                    </span>
                    {event.participantsCount > 0 && (
                      <span className="bg-accent text-background ml-auto flex h-5 min-w-5 items-center justify-center rounded-full px-1.5 text-xs font-bold">
                        {event.participantsCount}
                      </span>
                    )}
                  </div>
                </div>
              );
            }

            return null;
          })()}

          {/* Venue */}
          {venue && (
            <div
              className={`${styles.venueRow} ${styles.clickable}`}
              onClick={() =>
                router.push(PAGE_ROUTES.VENUE_DETAIL(event.venueId))
              }
            >
              <MapPin size={20} className={styles.fieldIcon} />
              <div className="flex flex-col">
                <span className={styles.venueName}>{venue.name}</span>
                <span className={styles.venueAddress}>{venue.address}</span>
              </div>
            </div>
          )}

          {/* Participation Status */}
          <div className={styles.approvalRow}>
            {event.participationStatus === 'Approved' ? (
              <>
                <CalendarClock size={20} className="text-accent shrink-0" />
                <span className={styles.fieldValue}>{t('joined')}</span>
              </>
            ) : event.participationStatus ? (
              <>
                <Clock size={20} className="text-accent shrink-0" />
                <span className={styles.fieldValue}>
                  {t('awaitingApproval')}
                </span>
              </>
            ) : event.requiresApproval ? (
              <>
                <CalendarClock size={20} className="text-secondary shrink-0" />
                <span className={styles.fieldValue}>
                  {t('requiresApproval')}
                </span>
              </>
            ) : (
              <>
                <CalendarClock size={20} className="text-accent shrink-0" />
                <span className={styles.fieldValue}>
                  {t('noApprovalRequired')}
                </span>
              </>
            )}
            {!event.isCreator && (
              <Button
                variant={ButtonVariant.Solid}
                className="ml-auto shrink-0"
                onClick={handleJoinLeave}
                disabled={isJoining}
              >
                {isJoining ? (
                  <Spinner />
                ) : event.participationStatus === 'Approved' ? (
                  t('leave')
                ) : event.participationStatus ? (
                  t('cancelRequest')
                ) : (
                  t('join')
                )}
              </Button>
            )}
          </div>

          {/* Pending Approvals — creator only */}
          {event.isCreator && event.requiresApproval && (
            <div
              className={`${styles.sectionCard} ${styles.clickable}`}
              onClick={() => router.push(PAGE_ROUTES.APPROVALS(event.id))}
            >
              <div className="flex items-center gap-2">
                <UserCheck size={20} className="text-secondary shrink-0" />
                <span className="text-primary text-sm">
                  {t('pendingApprovals')}
                </span>
                {event.pendingApprovalsCount > 0 && (
                  <span className="bg-accent text-background ml-auto flex h-5 min-w-5 items-center justify-center rounded-full px-1.5 text-xs font-bold">
                    {event.pendingApprovalsCount}
                  </span>
                )}
              </div>
            </div>
          )}

          {/* Contacts */}
          {event.contacts.length > 0 && (
            <div className={styles.sectionCard}>
              <span className={styles.sectionTitle}>{t('contacts')}</span>
              <div className={styles.contactList}>
                {event.contacts.map((c) => (
                  <div
                    key={c.id}
                    className={styles.contactItem}
                    onClick={() => openContact(c.type, c.value)}
                  >
                    <span
                      className={styles.fieldIcon}
                      style={{
                        width: 18,
                        height: 18,
                        display: 'inline-flex',
                        alignItems: 'center',
                        justifyContent: 'center',
                      }}
                    >
                      {messengerIcons[c.type] ??
                        (c.type === 'Phone' ? (
                          <Phone size={18} />
                        ) : c.type === 'Email' ? (
                          <Mail size={18} />
                        ) : (
                          <Globe size={18} />
                        ))}
                    </span>
                    <span className={styles.contactValue}>{c.value}</span>
                  </div>
                ))}
              </div>
            </div>
          )}

          {/* Hobbies */}
          {event.hobbyIds.length > 0 && (
            <div className={styles.sectionCard}>
              <span className={styles.sectionTitle}>{t('hobbies')}</span>
              <div className={styles.hobbyTags}>
                {event.hobbyIds.map((id) => (
                  <span key={id} className={styles.hobbyTag}>
                    {id.slice(0, 8)}
                  </span>
                ))}
              </div>
            </div>
          )}
        </div>
      </div>

      <ModalMenu
        isOpen={isMenuOpen}
        onClose={() => setIsMenuOpen(false)}
        options={[
          ...(event.isCreator
            ? [
                {
                  label: t('edit'),
                  onClick: () => {
                    setIsMenuOpen(false);
                    handleEdit();
                  },
                  icon: <Pencil size={24} />,
                },
              ]
            : [
                {
                  label:
                    event.participationStatus === 'Approved'
                      ? t('leave')
                      : event.participationStatus
                        ? t('cancelRequest')
                        : t('join'),
                  onClick: () => {
                    setIsMenuOpen(false);
                    handleJoinLeave();
                  },
                  icon: event.participationStatus ? (
                    <UserMinus size={24} />
                  ) : (
                    <UserPlus size={24} />
                  ),
                },
              ]),
        ]}
      />

      <OnboardingGuardDialog
        isOpen={showOnboardingDialog}
        onClose={() => setShowOnboardingDialog(false)}
        onCreateProfile={handleCreateProfile}
      />
    </div>
  );
}
