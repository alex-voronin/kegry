'use client';

import { useState, useEffect } from 'react';
import { useParams } from 'next/navigation';
import { useTranslations } from 'next-intl';

import { PAGE_ROUTES, API_ENDPOINTS } from '@/lib/routes';
import apiClient from '@/lib/api-client';
import { rethrowApiError } from '@/lib/api-error';
import { uploadImage } from '@/lib/image-utils';
import { useUserProfileStore } from '@/store/use-user-profile-store';
import { useAuthStore } from '@/lib/auth/use-auth-store';
import { Spinner } from '@/components/ui/spinner';
import { MeetupForm } from '../../_components/meetup-form';
import type { EventContactForm } from '@/components/ui/contact-section';
import { useSafeBack } from '@/hooks/use-safe-back';
import type { EventDto } from '@/lib/event-types';

export default function EditMeetupPage() {
  const params = useParams();
  const t = useTranslations('components.forms.event');
  const { safeBack } = useSafeBack();

  const eventId = params.id as string;
  const firstName = useUserProfileStore((s) => s.firstName);
  const isAuthenticated = useAuthStore((s) => !!s.accessToken);

  const [isLoading, setIsLoading] = useState(true);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [event, setEvent] = useState<EventDto | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!eventId || !isAuthenticated) return;
    const fetchEvent = async () => {
      try {
        const response = await apiClient.get<EventDto>(
          API_ENDPOINTS.EVENTS.UPDATE(eventId),
        );
        const ev = response.data;
        if (!ev.isCreator) {
          setError('You can only edit your own meetups');
          return;
        }
        setEvent(ev);
      } catch {
        setError('Failed to load event');
      } finally {
        setIsLoading(false);
      }
    };
    fetchEvent();
  }, [eventId, isAuthenticated]);

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
    if (!eventId) return;
    setIsSubmitting(true);
    try {
      let coverImageUrl = event?.coverImageUrl ?? null;

      if (data.coverBlob) {
        const publicUrl = await uploadImage(
          data.coverBlob,
          API_ENDPOINTS.EVENTS.COVER_UPLOAD_URL(eventId),
        );
        coverImageUrl = publicUrl;
      }

      await apiClient.patch(API_ENDPOINTS.EVENTS.UPDATE(eventId), {
        coverImageUrl,
        title: data.title,
        description: data.description,
        startDateTime: new Date(data.startDateTime).toISOString(),
        maxParticipants: data.maxParticipants,
        requiresApproval: data.requiresApproval,
        contacts: data.contacts.map((c) => ({
          type: c.type,
          value: c.value,
          isPublic: c.isPublic,
        })),
        hobbyIds: data.hobbyIds,
      });

      safeBack(PAGE_ROUTES.MAP);
    } catch (error) {
      rethrowApiError(error, 'Failed to update meetup');
    } finally {
      setIsSubmitting(false);
    }
  };

  if (!eventId) {
    return (
      <div className="flex h-dvh items-center justify-center px-4 text-center text-sm text-white/50">
        No event selected.
      </div>
    );
  }

  if (isLoading) {
    return (
      <div className="flex h-dvh items-center justify-center">
        <Spinner />
      </div>
    );
  }

  if (error || !event) {
    return (
      <div className="flex h-dvh items-center justify-center px-4 text-center text-sm text-white/50">
        {error || 'Failed to load event'}
      </div>
    );
  }

  const hasAdvancedFields = !!(event.description || event.coverImageUrl);
  const initialIsSimple = !hasAdvancedFields;

  return (
    <MeetupForm
      pageTitle={t('editTitle')}
      userName={firstName}
      onCancel={handleCancel}
      initialIsSimple={initialIsSimple}
      initialValues={{
        title: event.title,
        description: event.description,
        startDateTime: (() => {
          const d = new Date(event.startDateTime);
          return new Date(d.getTime() - d.getTimezoneOffset() * 60000)
            .toISOString()
            .slice(0, 16);
        })(),
        coverImageUrl: event.coverImageUrl,
        contacts: event.contacts.map((c) => ({
          id: c.id,
          type: c.type,
          value: c.value,
          isPublic: c.isPublic,
        })),
        hobbyIds: event.hobbyIds,
        maxParticipants: event.maxParticipants,
        requiresApproval: event.requiresApproval,
      }}
      onSubmit={handleSubmit}
      isSubmitting={isSubmitting}
      submitLabel={t('save')}
    />
  );
}
