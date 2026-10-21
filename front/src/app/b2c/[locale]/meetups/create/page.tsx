'use client';

import { useState, useEffect } from 'react';
import { useSearchParams } from 'next/navigation';
import { useTranslations } from 'next-intl';

import { PAGE_ROUTES, API_ENDPOINTS } from '@/lib/routes';
import apiClient from '@/lib/api-client';
import { rethrowApiError } from '@/lib/api-error';
import { uploadImage } from '@/lib/image-utils';
import { useUserProfileStore } from '@/store/use-user-profile-store';
import { useAuthStore } from '@/lib/auth/use-auth-store';
import { MeetupForm } from '../_components/meetup-form';
import type { EventContactForm } from '@/components/ui/contact-section';
import { useSafeBack } from '@/hooks/use-safe-back';
import { useEventActions } from '@/hooks/use-event-actions';
import { OnboardingGuardDialog } from '@/components/common/onboarding-guard-dialog';

export default function CreateMeetupPage() {
  const searchParams = useSearchParams();
  const { safeBack } = useSafeBack();
  const t = useTranslations('components.forms.event');

  const venueId = searchParams.get('venueId');
  const firstName = useUserProfileStore((s) => s.firstName);
  const serverProfile = useUserProfileStore((s) => s.serverProfile);
  const fetchMyProfile = useUserProfileStore((s) => s.fetchMyProfile);
  const avatarUrl = serverProfile?.avatarImageUrl ?? null;
  const isAuthenticated = useAuthStore((s) => !!s.accessToken);

  useEffect(() => {
    if (isAuthenticated && !serverProfile) {
      fetchMyProfile();
    }
  }, [isAuthenticated, serverProfile, fetchMyProfile]);

  const [isSubmitting, setIsSubmitting] = useState(false);
  const {
    requireProfile,
    showOnboardingDialog,
    setShowOnboardingDialog,
    handleCreateProfile,
  } = useEventActions();

  const handleCancel = () => {
    safeBack(PAGE_ROUTES.MAP);
  };

  const handleSubmit = async (data: {
    title: string;
    description: string;
    startDateTime: string;
    coverBlob: Blob | null;
    contacts: EventContactForm[];
    hobbyIds: string[];
    maxParticipants: number | null;
    requiresApproval: boolean;
  }) => {
    requireProfile(async () => {
      setIsSubmitting(true);
      try {
        const response = await apiClient.post<{ id: string }>(
          API_ENDPOINTS.EVENTS.CREATE,
          {
            creatorAvatarUrl: avatarUrl,
            title: data.title,
            description: data.description,
            startDateTime: new Date(data.startDateTime).toISOString(),
            venueId,
            maxParticipants: data.maxParticipants,
            requiresApproval: data.requiresApproval,
            contacts: data.contacts.map((c) => ({
              type: c.type,
              value: c.value,
              isPublic: c.isPublic,
            })),
            hobbyIds: data.hobbyIds,
          },
        );

        const eventId = response.data.id;

        if (data.coverBlob) {
          const publicUrl = await uploadImage(
            data.coverBlob,
            API_ENDPOINTS.EVENTS.COVER_UPLOAD_URL(eventId),
          );
          await apiClient.patch(API_ENDPOINTS.EVENTS.UPDATE(eventId), {
            coverImageUrl: publicUrl,
          });
        }

        safeBack(PAGE_ROUTES.MAP);
      } catch (error) {
        rethrowApiError(error, 'Failed to create meetup');
      } finally {
        setIsSubmitting(false);
      }
    });
  };

  if (!venueId) {
    return (
      <div className="flex h-dvh items-center justify-center px-4 text-center text-sm text-white/50">
        No venue selected. Please go back and choose a venue.
      </div>
    );
  }

  return (
    <>
      <MeetupForm
        pageTitle={t('createTitle')}
        userName={firstName}
        onCancel={handleCancel}
        onSubmit={handleSubmit}
        isSubmitting={isSubmitting}
        submitLabel={t('save')}
      />

      <OnboardingGuardDialog
        isOpen={showOnboardingDialog}
        onClose={() => setShowOnboardingDialog(false)}
        onCreateProfile={handleCreateProfile}
      />
    </>
  );
}
