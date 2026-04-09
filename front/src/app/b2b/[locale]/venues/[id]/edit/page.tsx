'use client';

import { useEffect, useRef, useState } from 'react';
import { useTranslations } from 'next-intl';
import { useRouter } from '@/i18n/routing';
import { useSafeBack } from '@/hooks/use-safe-back';
import { useParams } from 'next/navigation';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { toast } from 'react-toastify';
import { Camera, MapPin } from 'lucide-react';
import { Header } from '@/components/ui/header';
import { Button, ButtonVariant } from '@/components/ui/button';
import { Spinner } from '@/components/ui/spinner';
import { Textarea } from '@/components/ui/textarea';
import { Icon } from '@/components/ui/icon';
import stylesCommon from '@/components/ui/common.module.css';
import { FormSectionTitle } from '@/components/ui/form-section-title';
import { Input } from '@/components/ui/input';
import { useVenueStore } from '../use-venue-store';
import { API_ENDPOINTS, PAGE_ROUTES } from '@/lib/routes';
import { uploadImage, compressImageToBlob } from '@/lib/image-utils';
import { LeafletMapPickerModal } from '@/components/ui/leaflet-mappicker-modal';
import styles from './edit-venue.module.css';
import { cn } from '@/lib/utils';

export default function VenueEditPage() {
  const t = useTranslations('screens.editVenue');
  const tDetail = useTranslations('screens.venueDetail');
  const router = useRouter();
  const { safeBack } = useSafeBack();
  const params = useParams();
  const venueId = params.id as string;
  const fileInputRef = useRef<HTMLInputElement>(null);

  const [isSubmitting, setIsSubmitting] = useState(false);
  const [coverPreview, setCoverPreview] = useState<string | null>(null);
  const [coverBlob, setCoverBlob] = useState<Blob | null>(null);
  const [isMapPickerOpen, setIsMapPickerOpen] = useState(false);

  const { currentVenue, isLoading, fetchVenue, patchVenue } = useVenueStore();

  useEffect(() => {
    if (venueId) {
      fetchVenue(venueId);
    }
  }, [venueId, fetchVenue]);

  const schema = z.object({
    address: z.string().trim().min(1, t('addressRequired')),
    googleMapsUrl: z
      .url({ error: t('googleMapsUrlInvalid') })
      .optional()
      .or(z.literal('')),
    websiteUrl: z
      .url({ error: t('websiteUrlInvalid') })
      .optional()
      .or(z.literal('')),
    instagramUrl: z
      .url({ error: t('instagramUrlInvalid') })
      .optional()
      .or(z.literal('')),
    phoneNumber: z.string().trim().optional().or(z.literal('')),
    workingHours: z.string().trim().optional().or(z.literal('')),
    latitude: z.preprocess(
      (val) => (val === '' || isNaN(Number(val)) ? undefined : Number(val)),
      z
        .number({ error: t('latitudeRequired') })
        .min(-90, t('latitudeRange'))
        .max(90, t('latitudeRange')),
    ) as unknown as z.ZodNumber,
    longitude: z.preprocess(
      (val) => (val === '' || isNaN(Number(val)) ? undefined : Number(val)),
      z
        .number({ error: t('longitudeRequired') })
        .min(-180, t('longitudeRange'))
        .max(180, t('longitudeRange')),
    ) as unknown as z.ZodNumber,
  });

  type FormValues = z.infer<typeof schema>;

  const {
    register,
    handleSubmit,
    reset,
    setValue,
    watch,
    formState: { errors, isDirty },
  } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: {
      address: '',
      googleMapsUrl: '',
      websiteUrl: '',
      instagramUrl: '',
      phoneNumber: '',
      workingHours: '',
      latitude: 0,
      longitude: 0,
    },
  });

  useEffect(() => {
    if (!currentVenue) return;
    reset({
      address: currentVenue.address || '',
      googleMapsUrl: currentVenue.googleMapsUrl || '',
      websiteUrl: currentVenue.websiteUrl || '',
      instagramUrl: currentVenue.instagramUrl || '',
      phoneNumber: currentVenue.phoneNumber || '',
      workingHours: currentVenue.workingHours || '',
      latitude: currentVenue.latitude || 0,
      longitude: currentVenue.longitude || 0,
    });
  }, [currentVenue, reset]);

  const handleCoverClick = () => {
    fileInputRef.current?.click();
  };

  const handleFileChange = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;

    try {
      const reader = new FileReader();
      reader.onloadend = () => setCoverPreview(reader.result as string);
      reader.readAsDataURL(file);

      const compressedBlob = await compressImageToBlob(file, 512 * 1024, 1280);
      setCoverBlob(compressedBlob);
    } catch {
      toast.error('Failed to process image');
    }
  };

  const onSubmit = async (values: FormValues) => {
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

  const displayCover = coverPreview || currentVenue.coverImageUrl;
  const canSubmit = isDirty || !!coverBlob;

  return (
    <div className={styles.wrapper}>
      <form className={styles.form} onSubmit={handleSubmit(onSubmit)}>
        <div className={styles.coverSection}>
          <div className={styles.coverContainer} onClick={handleCoverClick}>
            <Header
              className={styles.header}
              title={t('title')}
              left={
                <Icon
                  src="/assets/b2b/disclosure-left-icon.svg"
                  alt="back"
                  width={14}
                  height={27}
                  stopPropagation
                  onClick={() =>
                    safeBack(PAGE_ROUTES.B2B.VENUES_DETAIL(venueId))
                  }
                />
              }
            />
            {displayCover ? (
              <>
                <img
                  src={displayCover}
                  alt="cover"
                  className={styles.coverImage}
                />
                <div className={styles.coverOverlay} />
              </>
            ) : (
              <div className={styles.coverPlaceholder}>
                <div className={styles.coverPlaceholderIcon}>
                  <Camera size={40} className="text-secondary opacity-50" />
                </div>
              </div>
            )}
            <div className={styles.cameraOverlay}>
              <Camera size={24} className="text-white" />
            </div>
            <input
              type="file"
              ref={fileInputRef}
              className="hidden"
              accept="image/*"
              onChange={handleFileChange}
            />
          </div>
        </div>

        <div className={stylesCommon.sectionContainer}>
          <FormSectionTitle required>{t('address')}</FormSectionTitle>
          <Input
            {...register('address')}
            placeholder={t('address')}
            className={stylesCommon.inputDark}
          />
          {errors.address?.message && (
            <p className={stylesCommon.errorMessage}>
              {errors.address.message}
            </p>
          )}
        </div>

        <div className={stylesCommon.sectionContainer}>
          <FormSectionTitle>{t('googleMapsUrl')}</FormSectionTitle>
          <Input
            {...register('googleMapsUrl')}
            placeholder={t('googleMapsUrl')}
            className={stylesCommon.inputDark}
          />
          {errors.googleMapsUrl?.message && (
            <p className={stylesCommon.errorMessage}>
              {errors.googleMapsUrl.message}
            </p>
          )}
        </div>

        <div className={styles.coordinatesRow}>
          <div
            className={cn(stylesCommon.sectionContainer, styles.coordinateCol)}
          >
            <FormSectionTitle required>{t('latitude')}</FormSectionTitle>
            <Input
              {...register('latitude', { valueAsNumber: true })}
              type="number"
              step="any"
              placeholder={t('latitude')}
              className={cn(
                stylesCommon.inputDark,
                'w-full rounded-lg! sm:rounded-xl!',
              )}
            />
            {errors.latitude?.message && (
              <p className={stylesCommon.errorMessage}>
                {errors.latitude.message}
              </p>
            )}
          </div>
          <div
            className={cn(stylesCommon.sectionContainer, styles.coordinateCol)}
          >
            <FormSectionTitle required>{t('longitude')}</FormSectionTitle>
            <Input
              {...register('longitude', { valueAsNumber: true })}
              type="number"
              step="any"
              placeholder={t('longitude')}
              className={cn(
                stylesCommon.inputDark,
                'w-full rounded-lg! sm:rounded-xl!',
              )}
            />
            {errors.longitude?.message && (
              <p className={stylesCommon.errorMessage}>
                {errors.longitude.message}
              </p>
            )}
          </div>
          <div className={stylesCommon.sectionContainer}>
            <FormSectionTitle required className="invisible">
              Map
            </FormSectionTitle>
            <button
              type="button"
              onClick={() => setIsMapPickerOpen(true)}
              className={styles.mapPickerButton}
            >
              <MapPin size={21} />
            </button>
          </div>
        </div>

        <div className={stylesCommon.sectionContainer}>
          <FormSectionTitle>{t('phoneNumber')}</FormSectionTitle>
          <Input
            {...register('phoneNumber')}
            placeholder={t('phoneNumber')}
            className={stylesCommon.inputDark}
          />
          {errors.phoneNumber?.message && (
            <p className={stylesCommon.errorMessage}>
              {errors.phoneNumber.message}
            </p>
          )}
        </div>

        <div className={stylesCommon.sectionContainer}>
          <FormSectionTitle>{t('websiteUrl')}</FormSectionTitle>
          <Input
            {...register('websiteUrl')}
            placeholder={t('websiteUrl')}
            className={stylesCommon.inputDark}
          />
          {errors.websiteUrl?.message && (
            <p className={stylesCommon.errorMessage}>
              {errors.websiteUrl.message}
            </p>
          )}
        </div>

        <div className={stylesCommon.sectionContainer}>
          <FormSectionTitle>{t('instagramUrl')}</FormSectionTitle>
          <Input
            {...register('instagramUrl')}
            placeholder={t('instagramUrl')}
            className={stylesCommon.inputDark}
          />
          {errors.instagramUrl?.message && (
            <p className={stylesCommon.errorMessage}>
              {errors.instagramUrl.message}
            </p>
          )}
        </div>

        <div className={stylesCommon.sectionContainer}>
          <FormSectionTitle>{t('workingHours')}</FormSectionTitle>
          <Textarea
            {...register('workingHours')}
            placeholder={t('workingHours')}
            className={stylesCommon.inputDark}
            rows={4}
          />
          {errors.workingHours?.message && (
            <p className={stylesCommon.errorMessage}>
              {errors.workingHours.message}
            </p>
          )}
        </div>

        <div className={styles.footer}>
          <Button
            variant={ButtonVariant.Default}
            className={styles.submitButton}
            type="submit"
            disabled={isSubmitting || !canSubmit}
          >
            {isSubmitting ? <Spinner /> : t('submit')}
          </Button>
        </div>
      </form>

      <LeafletMapPickerModal
        isOpen={isMapPickerOpen}
        onClose={() => setIsMapPickerOpen(false)}
        initialLat={watch('latitude')}
        initialLng={watch('longitude')}
        onSave={(lat, lng) => {
          setValue('latitude', lat, {
            shouldValidate: true,
            shouldDirty: true,
          });
          setValue('longitude', lng, {
            shouldValidate: true,
            shouldDirty: true,
          });
        }}
      />
    </div>
  );
}
