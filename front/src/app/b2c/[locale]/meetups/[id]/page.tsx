'use client';

import { useState, useEffect } from 'react';
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
import { MapPin, Clock, Users, CheckCircle, Globe, Phone, Mail } from 'lucide-react';
import { useSafeBack } from '@/hooks/use-safe-back';
import { useMeetupDetailStore } from './use-meetup-detail-store';
import { calculateAge } from '@/lib/date';
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

const contactIcons: Record<string, React.ReactNode> = {
  Telegram: (
    <svg width="18" height="18" viewBox="0 0 24 24" fill="currentColor" className={styles.fieldIcon}>
      <path d="M11.944 0A12 12 0 0 0 0 12a12 12 0 0 0 12 12 12 12 0 0 0 12-12A12 12 0 0 0 12 0a12 12 0 0 0-.056 0zm4.962 7.224c.1-.002.321.023.465.14a.506.506 0 0 1 .171.325c.016.093.036.306.02.472-.18 1.898-.962 6.502-1.36 8.627-.168.9-.499 1.201-.82 1.23-.696.065-1.225-.46-1.9-.902-1.056-.693-1.653-1.124-2.678-1.8-1.185-.78-.417-1.21.258-1.91.177-.184 3.247-2.977 3.307-3.23.007-.032.014-.15-.056-.212s-.174-.041-.249-.024c-.106.024-1.793 1.14-5.061 3.345-.48.33-.913.49-1.302.48-.428-.008-1.252-.241-1.865-.44-.752-.245-1.349-.374-1.297-.789.027-.216.325-.437.893-.663 3.498-1.524 5.83-2.529 6.998-3.014 3.332-1.386 4.025-1.627 4.476-1.635z" />
    </svg>
  ),
  WhatsApp: (
    <svg width="18" height="18" viewBox="0 0 24 24" fill="currentColor" className={styles.fieldIcon}>
      <path d="M17.472 14.382c-.297-.149-1.758-.867-2.03-.967-.273-.099-.471-.148-.67.15-.197.297-.767.966-.94 1.164-.173.199-.347.223-.644.075-.297-.15-1.255-.463-2.39-1.475-.883-.788-1.48-1.761-1.653-2.059-.173-.297-.018-.458.13-.606.134-.133.298-.347.446-.52.149-.174.198-.298.298-.497.099-.198.05-.371-.025-.52-.075-.149-.669-1.612-.916-2.207-.242-.579-.487-.5-.669-.51-.173-.008-.371-.01-.57-.01-.198 0-.52.074-.792.372-.272.297-1.04 1.016-1.04 2.479 0 1.462 1.065 2.875 1.213 3.074.149.198 2.096 3.2 5.077 4.487.709.306 1.262.489 1.694.625.712.227 1.36.195 1.871.118.571-.085 1.758-.719 2.006-1.413.248-.694.248-1.289.173-1.413-.074-.124-.272-.198-.57-.347m-5.421 7.403h-.004a9.87 9.87 0 01-5.031-1.378l-.361-.214-3.741.982.998-3.648-.235-.374a9.86 9.86 0 01-1.51-5.26c.001-5.45 4.436-9.884 9.888-9.884 2.64 0 5.122 1.03 6.988 2.898a9.825 9.825 0 012.893 6.994c-.003 5.45-4.437 9.884-9.885 9.884m8.413-18.297A11.815 11.815 0 0012.05 0C5.495 0 .16 5.335.157 11.892c0 2.096.547 4.142 1.588 5.945L.057 24l6.305-1.654a11.882 11.882 0 005.683 1.448h.005c6.554 0 11.89-5.335 11.893-11.893a11.821 11.821 0 00-3.48-8.413Z" />
    </svg>
  ),
  Viber: (
    <svg width="18" height="18" viewBox="0 0 24 24" fill="currentColor" className={styles.fieldIcon}>
      <path d="M11.4 0C9.473.028 5.333.344 3.02 2.467 1.302 4.187.696 6.7.633 9.817.57 12.933.488 18.776 6.12 20.36h.003l-.004 2.416s-.037.977.61 1.177c.777.242 1.234-.5 1.98-1.302.407-.44.972-1.084 1.397-1.58 3.85.326 6.812-.416 7.15-.525.776-.252 5.176-.816 5.892-6.657.74-6.02-.36-9.83-2.34-11.546-.596-.55-3.006-2.3-8.375-2.323 0 0-.395-.025-1.037-.017zm.058 1.693c.545-.004.88.017.88.017 4.542.02 6.717 1.388 7.222 1.846 1.675 1.435 2.53 4.868 1.906 9.897v.002c-.604 4.878-4.174 5.184-4.832 5.395-.28.09-2.882.737-6.153.524 0 0-2.436 2.94-3.197 3.704-.12.12-.26.167-.352.144-.13-.033-.166-.188-.165-.414l.02-4.018c-4.762-1.32-4.485-6.292-4.43-8.895.054-2.604.543-4.738 1.996-6.173 1.96-1.773 5.474-2.018 7.11-2.03zm.38 2.602c-.167 0-.303.135-.304.302 0 .167.133.303.3.305 1.624.01 2.946.537 4.028 1.592 1.073 1.046 1.62 2.468 1.633 4.334.002.167.14.3.307.3.166-.002.3-.138.3-.304-.014-1.984-.618-3.596-1.816-4.764-1.19-1.16-2.692-1.753-4.447-1.765zm-3.96.695c-.19-.032-.4.005-.616.117l-.01.002c-.43.247-.816.562-1.146.932-.002.004-.006.004-.008.008-.267.323-.42.638-.46.948-.008.046-.01.093-.007.14 0 .136.022.27.065.4l.013.01c.135.48.473 1.276 1.205 2.604.42.768.903 1.5 1.446 2.186.27.344.56.673.87.984l.132.132c.31.308.64.6.984.87.686.543 1.418 1.027 2.186 1.447 1.328.733 2.126 1.07 2.604 1.206l.01.014c.13.042.265.064.402.063.046.002.092 0 .138-.008.31-.036.627-.19.948-.46.004 0 .003-.002.008-.005.37-.33.683-.72.93-1.148l.003-.01c.225-.432.15-.842-.18-1.12-.004 0-.698-.58-1.037-.83-.36-.255-.73-.492-1.113-.71-.51-.285-1.032-.106-1.248.174l-.447.564c-.23.283-.657.246-.657.246-3.12-.796-3.955-3.955-3.955-3.955s-.037-.426.248-.656l.563-.448c.277-.215.456-.737.17-1.248-.217-.383-.454-.756-.71-1.115-.25-.34-.826-1.033-.83-1.035-.137-.165-.31-.265-.502-.297zm4.49.88c-.158.002-.29.124-.3.282-.01.167.115.312.282.324 1.16.085 2.017.466 2.645 1.15.63.688.93 1.524.906 2.57-.002.168.13.306.3.31.166.003.305-.13.31-.297.025-1.175-.334-2.193-1.067-2.994-.74-.81-1.777-1.253-3.05-1.346h-.024zm.463 1.63c-.16.002-.29.127-.3.287-.008.167.12.31.288.32.523.028.875.175 1.113.422.24.245.388.62.416 1.164.01.167.15.295.318.287.167-.008.295-.15.287-.317-.03-.644-.215-1.178-.58-1.557-.367-.378-.893-.574-1.52-.607h-.018z" />
    </svg>
  ),
  Phone: <Phone size={18} className={styles.fieldIcon} />,
  Email: <Mail size={18} className={styles.fieldIcon} />,
};

export default function MeetupDetailPage() {
  const params = useParams();
  const router = useRouter();
  const t = useTranslations('screens.meetupDetail');
  const tHobbies = useTranslations('components.profile-sections.hobbies');
  const locale = useLocale();
  const { safeBack } = useSafeBack();
  const eventId = params.id as string;
  const [isMenuOpen, setIsMenuOpen] = useState(false);

  const { event, venue, creatorName, creatorHobbies, creatorBirthday, isLoading, isJoining, fetchEvent, joinEvent, leaveEvent, reset } =
    useMeetupDetailStore();

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
    try {
      if (event.isJoined) {
        await leaveEvent(event.id);
      } else {
        await joinEvent(event.id);
      }
    } catch {
      toast.error(t('actionFailed'));
    }
  };

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
        <div className={styles.emptyStateContent}>
          Event not found
        </div>
      </div>
    );
  }

  const formattedDate = new Date(event.startDateTime).toLocaleDateString(locale, {
    weekday: 'long',
    year: 'numeric',
    month: 'long',
    day: 'numeric',
  });

  const formattedTime = new Date(event.startDateTime).toLocaleTimeString(locale, {
    hour: '2-digit',
    minute: '2-digit',
  });

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
          <span className={styles.coverTitleText}>
            {event.title}
          </span>
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
              <span>{formattedDate} {formattedTime}</span>
            </div>
            <div className={styles.coverInfoRight}>
              {event.type === 'Meetup' && event.creatorAvatarUrl && (
                <button
                  type="button"
                  onClick={() => router.push(PAGE_ROUTES.USER_PROFILE(event.creatorId))}
                  className={styles.avatarSmall}
                >
                  <img
                    src={event.creatorAvatarUrl}
                    alt=""
                    className={styles.avatarSmallImg}
                  />
                </button>
              )}
              <Users size={14} />
              <span>{event.maxParticipants ? `${event.participantsCount}/${event.maxParticipants}` : event.participantsCount}</span>
            </div>
          </div>
        </div>
      </div>

      {/* Content */}
      <div className={styles.contentWrapper}>
        <div className={styles.sectionList}>

          {/* Description */}
          {event.description && (
            <p className={`${styles.sectionCard} text-sm leading-relaxed text-primary`}>
              {event.description}
            </p>
          )}

          {/* Creator — meetups only */}
          {event.type === 'Meetup' && (
            <div className={`${styles.sectionCard} ${styles.clickable}`} onClick={() => router.push(PAGE_ROUTES.USER_PROFILE(event.creatorId))}>
              <div className={styles.creatorRow}>
                {event.creatorAvatarUrl ? (
                  <div className={styles.creatorAvatar}>
                    <img src={event.creatorAvatarUrl} alt="" className={styles.creatorAvatarImg} />
                  </div>
                ) : (
                  <div className={styles.creatorAvatarFallback}>
                    {(creatorName ?? event.creatorId).charAt(0).toUpperCase()}
                  </div>
                )}
                <div className="flex flex-col">
                  <span className={styles.creatorName}>
                    {creatorName ?? event.creatorId.slice(0, 8)}
                  </span>
                  {creatorBirthday && (
                    <span className={styles.creatorAge}>{t('age')}: {calculateAge(creatorBirthday)}</span>
                  )}
                </div>
              </div>
              {creatorHobbies.length > 0 && (
                <div className={styles.creatorHobbies}>
                  {creatorHobbies.map((h, i) => (
                    <span key={i} className={styles.creatorHobbyTag}>
                      {tHobbies(h as 'gym') || h}
                    </span>
                  ))}
                </div>
              )}
            </div>
          )}

          {/* Venue */}
          {venue && (
            <div
              className={`${styles.venueRow} ${styles.clickable}`}
              onClick={() => router.push(PAGE_ROUTES.VENUE_DETAIL(event.venueId))}
            >
              <MapPin size={20} className={styles.fieldIcon} />
              <div className="flex flex-col">
                <span className={styles.venueName}>{venue.name}</span>
                <span className={styles.venueAddress}>{venue.address}</span>
              </div>
            </div>
          )}

          {/* Requires Approval */}
          {event.requiresApproval && (
            <div className={styles.approvalRow}>
              <CheckCircle size={20} className="shrink-0 text-accent" />
              <span className={styles.fieldValue}>{t('requiresApproval')}</span>
            </div>
          )}

          {/* Contacts */}
          {event.contacts.length > 0 && (
            <div className={styles.sectionCard}>
              <span className={styles.sectionTitle}>{t('contacts')}</span>
              <div className={styles.contactList}>
                {event.contacts.map((c) => (
                  <div key={c.id} className={styles.contactItem} onClick={() => openContact(c.type, c.value)}>
                    {contactIcons[c.type] ?? <Globe size={18} className={styles.contactIcon} />}
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

        {!event.isCreator && (
          <div className={styles.footer}>
            <Button
              variant={event.isJoined ? ButtonVariant.Secondary : ButtonVariant.Default}
              className={styles.footerButton}
              onClick={handleJoinLeave}
              disabled={isJoining}
            >
              {isJoining ? (
                <Spinner />
              ) : event.isJoined ? (
                t('leave')
              ) : (
                t('join')
              )}
            </Button>
          </div>
        )}
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
                },
              ]
            : [
                {
                  label: event.isJoined ? t('leave') : t('join'),
                  onClick: () => {
                    setIsMenuOpen(false);
                    handleJoinLeave();
                  },
                },
              ]),
        ]}
      />
    </div>
  );
}
