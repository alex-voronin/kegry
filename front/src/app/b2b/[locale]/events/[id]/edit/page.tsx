'use client';

import { useEffect, useState } from 'react';
import { useParams } from 'next/navigation';
import { useTranslations } from 'next-intl';

import { Spinner } from '@/components/ui/spinner';
import { useSafeBack } from '@/hooks/use-safe-back';
import { PAGE_ROUTES, API_ENDPOINTS } from '@/lib/routes';
import apiClient from '@/lib/api-client';
import { rethrowApiError } from '@/lib/api-error';
import { uploadImage } from '@/lib/image-utils';
import { EventForm } from '../../_components/event-form';
import type { EventContactForm } from '@/components/ui/contact-section';

interface EventDetailDto {
  id: string;
  title: string;
  description: string;
  startDateTime: string;
  type: 'Event' | 'Meetup';
  coverImageUrl: string | null;
  isVisibleOnMap: boolean;
  maxParticipants: number | null;
  requiresApproval: boolean;
  contacts: { id: string; type: string; value: string; isPublic: boolean }[];
  hobbyIds: string[];
  [key: string]: unknown;
}

export default function EditEventPage() {
  const params = useParams();
  const t = useTranslations('components.forms.event');
  const { safeBack } = useSafeBack();

  const eventId = params.id as string;
  const [event, setEvent] = useState<EventDetailDto | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isSubmitting, setIsSubmitting] = useState(false);

  useEffect(() => {
    const loadEvent = async () => {
      try {
        const response = await apiClient.get(
          API_ENDPOINTS.B2B.EVENTS.DETAILS(eventId),
        );
        setEvent(response.data.result);
      } catch (error) {
        rethrowApiError(error, 'Failed to load event');
      } finally {
        setIsLoading(false);
      }
    };
    loadEvent();
  }, [eventId]);

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
      let coverImageUrl: string | undefined;
      if (data.coverBlob) {
        coverImageUrl = await uploadImage(
          data.coverBlob,
          API_ENDPOINTS.B2B.EVENTS.COVER_UPLOAD_URL(eventId),
        );
      }

      await apiClient.patch(API_ENDPOINTS.B2B.EVENTS.DETAILS(eventId), {
        title: data.title,
        description: data.description,
        startDateTime: new Date(data.startDateTime).toISOString(),
        ...(coverImageUrl ? { coverImageUrl } : {}),
        contacts: data.contacts.map((c) => ({
          type: c.type,
          value: c.value,
          isPublic: c.isPublic,
        })),
        hobbyIds: data.hobbyIds,
      });

      safeBack(PAGE_ROUTES.B2B.EVENTS);
    } catch (error) {
      rethrowApiError(error, 'Failed to update event');
    } finally {
      setIsSubmitting(false);
    }
  };

  if (isLoading) {
    return (
      <div className="flex flex-1 items-center justify-center">
        <Spinner />
      </div>
    );
  }

  if (!event) {
    return (
      <div className="flex flex-1 flex-col overflow-hidden sm:rounded-[16px]">
        <div className="text-secondary flex flex-1 items-center justify-center">
          Event not found
        </div>
      </div>
    );
  }

  return (
    <div className="overflow-hidden sm:rounded-[16px]">
      <EventForm
        title={t('editTitle')}
        onCancel={handleCancel}
        initialValues={{
          title: event.title,
          description: event.description,
          startDateTime: event.startDateTime
            ? (() => {
                const d = new Date(event.startDateTime);
                return new Date(d.getTime() - d.getTimezoneOffset() * 60000)
                  .toISOString()
                  .slice(0, 16);
              })()
            : '',
          coverImageUrl: event.coverImageUrl,
          contacts:
            (event.contacts ?? []).map(
              (c: {
                id: string;
                type: string;
                value: string;
                isPublic: boolean;
              }) => ({
                id: c.id,
                type: c.type,
                value: c.value,
                isPublic: c.isPublic,
              }),
            ) ?? [],
          hobbyIds: event.hobbyIds ?? [],
        }}
        onSubmit={handleSubmit}
        isSubmitting={isSubmitting}
        submitLabel={t('save')}
      />
    </div>
  );
}
