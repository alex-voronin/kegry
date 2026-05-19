'use client';

import { useEffect, useState, useMemo, useRef, useCallback } from 'react';
import { useTranslations } from 'next-intl';
import { useRouter } from '@/i18n/routing';
import { useSafeBack } from '@/hooks/use-safe-back';
import { useParams } from 'next/navigation';
import { toast } from 'react-toastify';

import { Spinner } from '@/components/ui/spinner';
import { ModalDialog } from '@/components/ui/modal-dialog';
import { useVenueStore } from '../use-venue-store';
import { API_ENDPOINTS, PAGE_ROUTES } from '@/lib/routes';
import { uploadImage } from '@/lib/image-utils';
import {
  VenueForm,
  VenueFormValues,
  VenueFormHandle,
} from '@/components/b2b/venue-form';

export default function VenueEditPage() {
  const t = useTranslations('screens.editVenue');
  const tCommon = useTranslations('common');
  const router = useRouter();
  const { safeBack } = useSafeBack();
  const params = useParams();
  const venueId = params.id as string;

  const [isSubmitting, setIsSubmitting] = useState(false);
  const [showSaveDialog, setShowSaveDialog] = useState(false);
  const [isDirty, setIsDirty] = useState(false);
  const formRef = useRef<VenueFormHandle>(null);
  const { currentVenue, isLoading, fetchVenue, patchVenue } = useVenueStore();

  useEffect(() => {
    if (venueId) {
      fetchVenue(venueId);
    }
  }, [venueId, fetchVenue]);

  const goBack = useCallback(() => {
    safeBack(PAGE_ROUTES.B2B.VENUES_DETAIL(venueId));
  }, [safeBack, venueId]);

  const handleCancel = () => {
    if (isDirty) {
      setShowSaveDialog(true);
    } else {
      goBack();
    }
  };

  const handleSubmit = async (
    values: VenueFormValues,
    coverBlob: Blob | null,
  ) => {
    setIsSubmitting(true);
    try {
      let coverImageUrl: string | undefined;
      if (coverBlob) {
        coverImageUrl = await uploadImage(
          coverBlob,
          API_ENDPOINTS.B2B.VENUES.COVER_UPLOAD_URL(venueId),
        );
      }

      await patchVenue(venueId, {
        ...values,
        ...(coverImageUrl ? { coverImageUrl } : {}),
      });

      goBack();
    } catch (error: any) {
      toast.error(error.message || t('toast.updateFailed'));
    } finally {
      setIsSubmitting(false);
    }
  };

  const initialValues = useMemo(
    () => ({
      address: currentVenue?.address || '',
      googleMapsUrl: currentVenue?.googleMapsUrl || '',
      websiteUrl: currentVenue?.websiteUrl || '',
      instagramUrl: currentVenue?.instagramUrl || '',
      phoneNumber: currentVenue?.phoneNumber || '',
      workingHours: currentVenue?.workingHours || '',
      latitude: currentVenue?.latitude || 0,
      longitude: currentVenue?.longitude || 0,
      coverImageUrl: currentVenue?.coverImageUrl || null,
      tagIds: currentVenue?.tags?.map((t) => t.id) || [],
    }),
    [
      currentVenue?.address,
      currentVenue?.googleMapsUrl,
      currentVenue?.websiteUrl,
      currentVenue?.instagramUrl,
      currentVenue?.phoneNumber,
      currentVenue?.workingHours,
      currentVenue?.latitude,
      currentVenue?.longitude,
      currentVenue?.coverImageUrl,
      currentVenue?.tags,
    ],
  );

  if (isLoading || !currentVenue) {
    return (
      <div className="flex flex-1 items-center justify-center">
        <Spinner />
      </div>
    );
  }

  return (
    <>
      <VenueForm
        ref={formRef}
        title={t('title')}
        submitText={t('submit')}
        isSubmitting={isSubmitting}
        requireCover={false}
        initialValues={initialValues}
        onCancel={handleCancel}
        onSubmit={handleSubmit}
        onDirtyChange={setIsDirty}
      />
      <ModalDialog
        isOpen={showSaveDialog}
        onClose={() => setShowSaveDialog(false)}
        message={tCommon('saveChangesDialog.message')}
        primaryButtonText={tCommon('saveChangesDialog.no')}
        secondaryButtonText={tCommon('saveChangesDialog.yes')}
        onPrimaryAction={goBack}
        onSecondaryAction={() => {
          formRef.current?.submitForm();
        }}
      />
    </>
  );
}
