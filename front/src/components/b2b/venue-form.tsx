import { useState, useRef, useEffect, useImperativeHandle, forwardRef } from 'react';
import { useTranslations } from 'next-intl';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { Camera, MapPin } from 'lucide-react';
import { toast } from 'react-toastify';

import { Header } from '@/components/ui/header';
import { Button, ButtonVariant } from '@/components/ui/button';
import { Spinner } from '@/components/ui/spinner';
import { Textarea } from '@/components/ui/textarea';
import { Icon } from '@/components/ui/icon';
import { FormSectionTitle } from '@/components/ui/form-section-title';
import { Input } from '@/components/ui/input';
import { LeafletMapPickerModal } from '@/components/ui/leaflet-mappicker-modal';
import { ImageCropperModal } from '@/components/ui/image-cropper-modal';
import { compressImageToBlob } from '@/lib/image-utils';
import { cn } from '@/lib/utils';
import {
  VENUE_COVER_MAX_LONG_SIDE,
  VENUE_COVER_MAX_SIZE_BYTES,
} from '@/lib/constants';

import stylesCommon from '@/components/ui/common.module.css';
import styles from './venue-form.module.css';

export interface VenueFormValues {
  address: string;
  googleMapsUrl?: string;
  websiteUrl?: string;
  instagramUrl?: string;
  phoneNumber?: string;
  workingHours?: string;
  latitude: number;
  longitude: number;
}

export interface VenueFormHandle {
  submitForm: () => void;
}

export interface VenueFormProps {
  title: string;
  submitText: string;
  isSubmitting: boolean;
  initialValues?: Partial<VenueFormValues> & { coverImageUrl?: string | null };
  requireCover: boolean;
  onCancel: () => void;
  onSubmit: (values: VenueFormValues, coverBlob: Blob | null) => Promise<void>;
  onDirtyChange?: (isDirty: boolean) => void;
}

export const VenueForm = forwardRef<VenueFormHandle, VenueFormProps>(function VenueForm({
  title,
  submitText,
  isSubmitting,
  initialValues,
  requireCover,
  onCancel,
  onSubmit,
  onDirtyChange,
}, ref) {
  const t = useTranslations('components.forms.venue');
  const fileInputRef = useRef<HTMLInputElement>(null);

  const [coverPreview, setCoverPreview] = useState<string | null>(
    initialValues?.coverImageUrl || null,
  );
  const [coverBlob, setCoverBlob] = useState<Blob | null>(null);
  const [isMapPickerOpen, setIsMapPickerOpen] = useState(false);
  const [isCropperOpen, setIsCropperOpen] = useState(false);
  const [rawImageSrc, setRawImageSrc] = useState<string | null>(null);

  const schema = z.object({
    address: z.string().trim().min(1, t('validation.addressRequired')),
    googleMapsUrl: z
      .string()
      .trim()
      .url({ error: t('validation.googleMapsUrlInvalid') })
      .optional()
      .or(z.literal('')),
    websiteUrl: z
      .string()
      .trim()
      .url({ error: t('validation.websiteUrlInvalid') })
      .optional()
      .or(z.literal('')),
    instagramUrl: z
      .string()
      .trim()
      .url({ error: t('validation.instagramUrlInvalid') })
      .optional()
      .or(z.literal('')),
    phoneNumber: z.string().trim().optional().or(z.literal('')),
    workingHours: z.string().trim().optional().or(z.literal('')),
    latitude: z.preprocess(
      (val) => (val === '' || isNaN(Number(val)) ? undefined : Number(val)),
      z
        .number({ error: t('validation.latitudeRequired') })
        .min(-90, t('validation.latitudeRange'))
        .max(90, t('validation.latitudeRange')),
    ) as unknown as z.ZodNumber,
    longitude: z.preprocess(
      (val) => (val === '' || isNaN(Number(val)) ? undefined : Number(val)),
      z
        .number({ error: t('validation.longitudeRequired') })
        .min(-180, t('validation.longitudeRange'))
        .max(180, t('validation.longitudeRange')),
    ) as unknown as z.ZodNumber,
  });

  const {
    register,
    handleSubmit,
    setValue,
    watch,
    reset,
    formState: { errors, isSubmitted, isDirty },
  } = useForm<VenueFormValues>({
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
    if (initialValues) {
      reset({
        address: initialValues.address || '',
        googleMapsUrl: initialValues.googleMapsUrl || '',
        websiteUrl: initialValues.websiteUrl || '',
        instagramUrl: initialValues.instagramUrl || '',
        phoneNumber: initialValues.phoneNumber || '',
        workingHours: initialValues.workingHours || '',
        latitude: initialValues.latitude || 0,
        longitude: initialValues.longitude || 0,
      });
      if (initialValues.coverImageUrl) {
        setCoverPreview(initialValues.coverImageUrl);
      }
    }
  }, [initialValues, reset]);

  const handleCoverClick = () => {
    fileInputRef.current?.click();
  };

  const handleFileChange = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;

    try {
      const reader = new FileReader();
      reader.onloadend = () => {
        setRawImageSrc(reader.result as string);
        setIsCropperOpen(true);
      };
      reader.readAsDataURL(file);

      // Reset input value to allow selecting the same file again
      if (fileInputRef.current) {
        fileInputRef.current.value = '';
      }
    } catch {
      toast.error('Failed to process image');
    }
  };

  const handleCropComplete = async (croppedBlob: Blob) => {
    try {
      setCoverPreview(URL.createObjectURL(croppedBlob));

      const compressedBlob = await compressImageToBlob(
        croppedBlob,
        VENUE_COVER_MAX_SIZE_BYTES,
        VENUE_COVER_MAX_LONG_SIDE,
      );
      setCoverBlob(compressedBlob);
      setIsCropperOpen(false);
    } catch {
      toast.error('Failed to process cropped image');
    }
  };

  const handleFormSubmit = (values: VenueFormValues) => {
    if (requireCover && !coverBlob && !initialValues?.coverImageUrl) {
      toast.error(t('validation.coverRequired'));
      return;
    }
    onSubmit(values, coverBlob);
  };

  const canSubmit = isDirty || !!coverBlob;

  const hasDirtyChanges = isDirty || !!coverBlob;

  useEffect(() => {
    onDirtyChange?.(hasDirtyChanges);
  }, [hasDirtyChanges, onDirtyChange]);

  useImperativeHandle(ref, () => ({
    submitForm: () => {
      handleSubmit(handleFormSubmit)();
    },
  }), [handleSubmit, handleFormSubmit]);

  return (
    <div className={styles.wrapper}>
      <form className={styles.form} onSubmit={handleSubmit(handleFormSubmit)}>
        <div className={styles.coverSection}>
          <div className={styles.coverContainer} onClick={handleCoverClick}>
            <Header
              className={styles.header}
              title={title}
              left={
                <Icon
                  src="/assets/b2b/disclosure-left-icon.svg"
                  alt="back"
                  width={14}
                  height={27}
                  stopPropagation
                  onClick={onCancel}
                />
              }
            />
            {coverPreview ? (
              <>
                <img
                  src={coverPreview}
                  alt="cover"
                  className={styles.coverImage}
                />
                <div className={styles.coverOverlay} />
              </>
            ) : (
              <div className={styles.coverPlaceholder}>
                <div className={styles.coverPlaceholderIcon}>
                  <Camera size={40} className="text-secondary opacity-50" />
                  {requireCover && !initialValues?.coverImageUrl && (
                    <span className={styles.requiredAsterisk}>*</span>
                  )}
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
          {isSubmitted &&
            requireCover &&
            !coverBlob &&
            !initialValues?.coverImageUrl && (
              <p className={`${stylesCommon.errorMessage} px-4`}>
                {t('validation.coverRequired')}
              </p>
            )}
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
              {...register('latitude')}
              type="text"
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
              {...register('longitude')}
              type="text"
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
            className={`${stylesCommon.inputDark} min-h-[120px] py-3`}
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
            {isSubmitting ? <Spinner /> : submitText}
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

      <ImageCropperModal
        isOpen={isCropperOpen}
        onClose={() => setIsCropperOpen(false)}
        imageSrc={rawImageSrc}
        onCropComplete={handleCropComplete}
        outputSize={{ width: 784, height: 208 }}
      />
    </div>
  );
});
