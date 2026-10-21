'use client';

import { useState } from 'react';
import { useSearchParams } from 'next/navigation';
import { useTranslations } from 'next-intl';

import { useSafeBack } from '@/hooks/use-safe-back';
import { PAGE_ROUTES, API_ENDPOINTS } from '@/lib/routes';
import apiClient from '@/lib/api-client';
import { rethrowApiError } from '@/lib/api-error';
import { uploadImage } from '@/lib/image-utils';
import { EventForm } from '../_components/event-form';
import type { EventContactForm } from '@/components/ui/contact-section';

export default function CreateEventPage() {
  const searchParams = useSearchParams();
  const t = useTranslations('components.forms.event');
  const { safeBack } = useSafeBack();

  const venueId = searchParams.get('venueId');
  const [isSubmitting, setIsSubmitting] = useState(false);

  const handleCancel = () => {
    safeBack(PAGE_ROUTES.B2B.EVENTS);
  };

  const handleSubmit = async (data: {
    title: string;
    description: string;
    startDateTime: string;
    coverBlob: Blob | null;
    contacts: EventContactForm[];
    hobbyIds: string[];
  }) => {
    setIsSubmitting(true);
    try {
      const response = await apiClient.post<{ id: string }>(
        API_ENDPOINTS.B2B.EVENTS.BASE,
        {
          title: data.title,
          description: data.description,
          startDateTime: new Date(data.startDateTime).toISOString(),
          venueId: venueId || undefined,
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
          API_ENDPOINTS.B2B.EVENTS.COVER_UPLOAD_URL(eventId),
        );
        await apiClient.patch(API_ENDPOINTS.B2B.EVENTS.DETAILS(eventId), {
          coverImageUrl: publicUrl,
        });
      }

      safeBack(PAGE_ROUTES.B2B.EVENTS);
    } catch (error) {
      rethrowApiError(error, 'Failed to create event');
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <EventForm
      title={t('createTitle')}
      onCancel={handleCancel}
      onSubmit={handleSubmit}
      isSubmitting={isSubmitting}
      submitLabel={t('save')}
    />
  );
}
