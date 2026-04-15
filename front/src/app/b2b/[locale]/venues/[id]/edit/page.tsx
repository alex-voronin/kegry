'use client';

import { useEffect, useState } from 'react';
import { useTranslations } from 'next-intl';
import { useRouter } from '@/i18n/routing';
import { useSafeBack } from '@/hooks/use-safe-back';
import { useParams } from 'next/navigation';
import { toast } from 'react-toastify';

import { Spinner } from '@/components/ui/spinner';
import { useVenueStore } from '../use-venue-store';
import { API_ENDPOINTS, PAGE_ROUTES } from '@/lib/routes';
import { uploadImage } from '@/lib/image-utils';
import { VenueForm, VenueFormValues } from '@/components/b2b/venue-form';

export default function VenueEditPage() {
  const t = useTranslations('screens.editVenue');
  const router = useRouter();
  const { safeBack } = useSafeBack();
  const params = useParams();
  const venueId = params.id as string;

  const [isSubmitting, setIsSubmitting] = useState(false);
  const { currentVenue, isLoading, fetchVenue, patchVenue } = useVenueStore();

  useEffect(() => {
    if (venueId) {
      fetchVenue(venueId);
    }
  }, [venueId, fetchVenue]);

  const handleCancel = () => {
    safeBack(PAGE_ROUTES.B2B.VENUES_DETAIL(venueId));
  };

  const handleSubmit = async (values: VenueFormValues, coverBlob: Blob | null) => {
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

      safeBack(PAGE_ROUTES.B2B.VENUES_DETAIL(venueId));
    } catch (error: any) {
      toast.error(error.message || t('toast.updateFailed'));
    } finally {
      setIsSubmitting(false);
    }
  };

  if (isLoading || !currentVenue) {
    return (
      <div className="flex flex-1 items-center justify-center">
        <Spinner />
      </div>
    );
  }

  return (
    <VenueForm
      title={t('title')}
      submitText={t('submit')}
      isSubmitting={isSubmitting}
      requireCover={false}
      initialValues={{
        address: currentVenue.address || '',
        googleMapsUrl: currentVenue.googleMapsUrl || '',
        websiteUrl: currentVenue.websiteUrl || '',
        instagramUrl: currentVenue.instagramUrl || '',
        phoneNumber: currentVenue.phoneNumber || '',
        workingHours: currentVenue.workingHours || '',
        latitude: currentVenue.latitude,
        longitude: currentVenue.longitude,
        coverImageUrl: currentVenue.coverImageUrl,
      }}
      onCancel={handleCancel}
      onSubmit={handleSubmit}
    />
  );
}
