'use client';

import { useState } from 'react';
import { useTranslations } from 'next-intl';
import { useRouter } from '@/i18n/routing';
import { useSafeBack } from '@/hooks/use-safe-back';
import { useParams } from 'next/navigation';
import { toast } from 'react-toastify';

import { useVenueStore } from '@/app/b2b/[locale]/venues/[id]/use-venue-store';
import { API_ENDPOINTS, PAGE_ROUTES } from '@/lib/routes';
import { uploadImage } from '@/lib/image-utils';
import { VenueForm, VenueFormValues } from '@/components/b2b/venue-form';

export default function VenueCreatePage() {
  const t = useTranslations('screens.createVenue');
  const router = useRouter();
  const { safeBack } = useSafeBack();
  const params = useParams();
  const companyId = params.id as string;

  const [isSubmitting, setIsSubmitting] = useState(false);
  const { createVenue, patchVenue } = useVenueStore();

  const handleCancel = () => {
    safeBack(PAGE_ROUTES.B2B.COMPANIES_DETAIL(companyId));
  };

  const handleSubmit = async (values: VenueFormValues, coverBlob: Blob | null) => {
    if (!coverBlob) return; // VenueForm ensures this is present if requireCover=true

    setIsSubmitting(true);
    try {
      // 1. Create venue
      const { id: venueId } = await createVenue(companyId, values);

      // 2. Upload cover
      const publicUrl = await uploadImage(
        coverBlob,
        API_ENDPOINTS.B2B.VENUES.COVER_UPLOAD_URL(venueId),
      );

      // 3. Patch venue with cover URL
      await patchVenue(venueId, { coverImageUrl: publicUrl });

      safeBack(PAGE_ROUTES.B2B.COMPANIES_DETAIL(companyId));
    } catch (error: any) {
      toast.error(error.message || t('toast.createFailed'));
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <VenueForm
      title={t('title')}
      submitText={t('submit')}
      isSubmitting={isSubmitting}
      requireCover={true}
      onCancel={handleCancel}
      onSubmit={handleSubmit}
    />
  );
}
