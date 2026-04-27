'use client';

import { useState, useRef, useEffect, useLayoutEffect } from 'react';
import Image from 'next/image';
import { useTranslations } from 'next-intl';
import { useRouter } from '@/i18n/routing';
import { Button } from '@/components/ui/button';
import { PAGE_ROUTES } from '@/lib/routes';
import { useAuthStore, RequiredAction } from '@/lib/auth/use-auth-store';
import styles from './how-it-works.module.css';
import { getOS } from '@/lib/os-utils';
import { checkPermissionStatus } from '@/lib/permissions';
import { useCallback } from 'react';
import { ModalDialog } from '@/components/ui/modal-dialog';
import { ButtonVariant } from '@/components/ui/button';
import { SafariShareIcon } from '@/components/ui/safari-share-icon';

const TOTAL_SLIDES = 3;
const SWIPE_THRESHOLD = 30;
const DRAG_SENSITIVITY = 1.5;

export default function HowItWorksPage() {
  const t = useTranslations('screens.howItWorks');
  const { setRequiredAction } = useAuthStore();
  const router = useRouter();
  const [currentSlide, setCurrentSlide] = useState(0);
  const [dragOffset, setDragOffset] = useState(0);
  const [isDragging, setIsDragging] = useState(false);
  const touchStartX = useRef(0);
  const wasDragged = useRef(false);
  const [os, setOs] = useState<'ios' | 'android' | 'other' | null>(null);
  const buttonRef = useRef<HTMLDivElement>(null);
  const [isPermissionDialogOpen, setIsPermissionDialogOpen] = useState(false);

  useEffect(() => {
    setOs(getOS());
  }, []);

  const requestPrompts = async () => {
    // Trigger location prompt
    try {
      await new Promise((resolve, reject) => {
        navigator.geolocation.getCurrentPosition(resolve, reject, {
          timeout: 1000,
        });
      });
    } catch (e) {
      console.warn('Location request failed or denied', e);
    }

    // Trigger notification prompt
    if ('Notification' in window) {
      try {
        await window.Notification.requestPermission();
      } catch (e) {
        console.warn('Notification request failed or denied', e);
      }
    }
  };

  const finishOnboarding = useCallback(() => {
    setRequiredAction(RequiredAction.None);
    router.push(PAGE_ROUTES.FEED);
  }, [router, setRequiredAction]);

  useLayoutEffect(() => {
    const adjustScroll = () => {
      if (!buttonRef.current) return;
      const rect = buttonRef.current.getBoundingClientRect();

      if (rect.bottom > window.innerHeight) {
        window.scrollTo({
          top: document.body.scrollHeight,
          behavior: 'smooth',
        });
      }
    };

    // HACK: Delay scroll to ensure the page layout stabilizes after the navbar changes size on mobile clients.
    const timeoutId = setTimeout(adjustScroll, 100);
    return () => clearTimeout(timeoutId);
  }, []);

  const handleNext = async () => {
    if (currentSlide === 1) {
      await requestPrompts();

      // Proceed regardless of status
      setCurrentSlide(2);
      return;
    }

    if (currentSlide < TOTAL_SLIDES - 1) {
      setCurrentSlide(currentSlide + 1);
    } else {
      const { locationAllowed, notificationAllowed } =
        await checkPermissionStatus();
      if (!locationAllowed || !notificationAllowed) {
        setIsPermissionDialogOpen(true);
      } else {
        finishOnboarding();
      }
    }
  };

  const handleTouchStart = (e: React.TouchEvent) => {
    touchStartX.current = e.touches[0].clientX;
    setIsDragging(true);
  };

  const handleTouchMove = (e: React.TouchEvent) => {
    if (!isDragging) return;
    const currentX = e.touches[0].clientX;
    const diff = touchStartX.current - currentX;
    setDragOffset(diff);
  };

  const handleTouchEnd = () => {
    setIsDragging(false);

    if (Math.abs(dragOffset) > SWIPE_THRESHOLD) {
      if (dragOffset > 0 && currentSlide < TOTAL_SLIDES - 1) {
        setCurrentSlide(currentSlide + 1);
      } else if (dragOffset < 0 && currentSlide > 0) {
        setCurrentSlide(currentSlide - 1);
      }
    }

    setDragOffset(0);
  };

  const handleMouseDown = (e: React.MouseEvent) => {
    touchStartX.current = e.clientX;
    setIsDragging(true);
    wasDragged.current = false;
  };

  const handleMouseMove = (e: React.MouseEvent) => {
    if (!isDragging) return;
    const currentX = e.clientX;
    const diff = touchStartX.current - currentX;
    if (Math.abs(diff) > 5) {
      wasDragged.current = true;
    }
    setDragOffset(diff);
  };

  const handleMouseUp = () => {
    if (!isDragging) return;

    setIsDragging(false);

    if (Math.abs(dragOffset) > SWIPE_THRESHOLD) {
      if (dragOffset > 0 && currentSlide < TOTAL_SLIDES - 1) {
        setCurrentSlide(currentSlide + 1);
      } else if (dragOffset < 0 && currentSlide > 0) {
        setCurrentSlide(currentSlide - 1);
      }
    }

    setDragOffset(0);
  };

  const handleMouseLeave = () => {
    if (isDragging) {
      setIsDragging(false);
      setDragOffset(0);
    }
  };

  const handleClick = (e: React.MouseEvent<HTMLDivElement>) => {
    if (wasDragged.current) {
      e.preventDefault();
      e.stopPropagation();
      return;
    }

    const rect = e.currentTarget.getBoundingClientRect();
    const clickX = e.clientX - rect.left;
    const isLeftHalf = clickX < rect.width / 2;

    if (isLeftHalf && currentSlide > 0) {
      setCurrentSlide(currentSlide - 1);
    } else if (!isLeftHalf && currentSlide < TOTAL_SLIDES - 1) {
      setCurrentSlide(currentSlide + 1);
    }
  };

  const getSlideTransform = () => {
    const baseOffset = currentSlide * -100;
    const dragPercent = isDragging
      ? ((dragOffset * DRAG_SENSITIVITY) / window.innerWidth) * -100
      : 0;
    return `translateX(calc(${baseOffset}% + ${dragPercent}%))`;
  };

  return (
    <div className={styles.wrapper}>
      <div className={styles.content}>
        <Image
          src="/assets/how-it-works/sensei-placeholder.webp"
          alt="Sensei"
          width={120}
          height={194}
          className={styles.sensei}
          priority
        />

        <h1 className={styles.greeting}>{t('greeting')}</h1>

        <div
          className={styles.carouselViewport}
          onTouchStart={handleTouchStart}
          onTouchMove={handleTouchMove}
          onTouchEnd={handleTouchEnd}
          onMouseDown={handleMouseDown}
          onMouseMove={handleMouseMove}
          onMouseUp={handleMouseUp}
          onMouseLeave={handleMouseLeave}
          onClick={handleClick}
        >
          <div
            className={styles.carouselTrack}
            style={{
              transform: getSlideTransform(),
              transition: isDragging ? 'none' : 'transform 0.3s ease-out',
            }}
          >
            <div className={styles.slide}>
              <div className={styles.card}>
                <div className={styles.section}>
                  <p>{t('slideFeed.textDontGoAlone')}</p>
                </div>
                <div className={styles.section}>
                  <p>{t('slideFeed.textInvites')}</p>
                </div>
                <div className={styles.section}>
                  <p>{t('slideFeed.textNewChat')}</p>
                </div>
              </div>
            </div>

            <div className={styles.slide}>
              <div className={styles.card}>
                <div className={styles.permissionNote}>
                  <p>{t('slidePermissions.location')}</p>
                </div>
                <div className={styles.permissionNote}>
                  <p>{t('slidePermissions.notifications')}</p>
                </div>
              </div>
            </div>

            <div className={styles.slide}>
              <div className={styles.card}>
                {(os === 'ios' || os === 'other' || os === null) && (
                  <div className={styles.permissionNote}>
                    <p>{t('slidePWA.textIphone')}</p>
                    <p className="mt-2 font-semibold">
                      {t.rich('slidePWA.instructionIphone', {
                        icon: () => (
                          <SafariShareIcon className={styles.inlineIcon} />
                        ),
                      })}
                    </p>
                  </div>
                )}
                {(os === 'android' || os === 'other' || os === null) && (
                  <div className={styles.permissionNote}>
                    <p>{t('slidePWA.textAndroid')}</p>
                    <p className="mt-2 font-semibold">
                      {t('slidePWA.instructionAndroid')}
                    </p>
                  </div>
                )}
              </div>
            </div>
          </div>
        </div>

        <div className={styles.indicators}>
          {Array.from({ length: TOTAL_SLIDES }).map((_, index) => (
            <button
              key={index}
              type="button"
              className={`${styles.indicator} ${currentSlide === index ? styles.active : ''}`}
              onClick={() => setCurrentSlide(index)}
              aria-label={`Slide ${index + 1}`}
            />
          ))}
        </div>

        <div className={styles.footer} ref={buttonRef}>
          <Button onClick={handleNext} className={styles.continueButton}>
            {currentSlide === TOTAL_SLIDES - 1
              ? t('continue')
              : currentSlide === 1
                ? t('enablePermissions')
                : t('next')}
          </Button>
        </div>

        <ModalDialog
          isOpen={isPermissionDialogOpen}
          onClose={() => setIsPermissionDialogOpen(false)}
          message={t('permissionsWarningDialog.message')}
          primaryButtonText={t('permissionsWarningDialog.allow')}
          secondaryButtonText={t('permissionsWarningDialog.proceed')}
          onPrimaryAction={async () => {
            await requestPrompts();
            finishOnboarding();
          }}
          onSecondaryAction={finishOnboarding}
          primaryButtonVariant={ButtonVariant.Default}
          secondaryButtonVariant={ButtonVariant.Secondary}
        />
      </div>
    </div>
  );
}
