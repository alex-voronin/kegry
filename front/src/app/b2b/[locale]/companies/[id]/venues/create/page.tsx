'use client';

import { useEffect, useRef, useState } from 'react';
import { useTranslations } from 'next-intl';
import { useRouter } from '@/i18n/routing';
import { useParams } from 'next/navigation';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { toast } from 'react-toastify';
import { Camera } from 'lucide-react';
import { Header } from '@/components/ui/header';
import { Button, ButtonVariant } from '@/components/ui/button';
import { Spinner } from '@/components/ui/spinner';
import { Textarea } from '@/components/ui/textarea';
import { Icon } from '@/components/ui/icon';
import stylesCommon from '@/components/ui/common.module.css';
import { FormSectionTitle } from '@/components/ui/form-section-title';
import { Input } from '@/components/ui/input';
import { useVenueStore } from '@/app/b2b/[locale]/venues/[id]/use-venue-store';
import { API_ENDPOINTS, PAGE_ROUTES } from '@/lib/routes';
import { uploadImage, compressImageToBlob } from '@/lib/image-utils';
import { parseGoogleMapsUrl } from '@/lib/geo-utils';
import styles from './create-venue.module.css';

export default function VenueCreatePage() {
  const t = useTranslations('screens.createVenue');
  const router = useRouter();
  const params = useParams();
  const companyId = params.id as string;
  const fileInputRef = useRef<HTMLInputElement>(null);

  const [isSubmitting, setIsSubmitting] = useState(false);
  const [coverPreview, setCoverPreview] = useState<string | null>(null);
  const [coverBlob, setCoverBlob] = useState<Blob | null>(null);

  const { createVenue, patchVenue } = useVenueStore();

  const schema = z.object({
    address: z.string().trim().min(1, t('validation.addressRequired')),
    googleMapsUrl: z
      .url({ error: t('validation.googleMapsUrlInvalid') })
      .optional()
      .or(z.literal('')),
    websiteUrl: z
      .url({ error: t('validation.websiteUrlInvalid') })
      .optional()
      .or(z.literal('')),
    instagramUrl: z
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

  type FormValues = z.infer<typeof schema>;

  const {
    register,
    handleSubmit,
    setValue,
    watch,
    formState: { errors, isSubmitted },
  } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: {
      address: '',
      googleMapsUrl: '',
      websiteUrl: '',
      instagramUrl: '',
      phoneNumber: '',
      workingHours: '',
    },
  });

  const googleMapsUrlValue = watch('googleMapsUrl');

  useEffect(() => {
    if (googleMapsUrlValue) {
      const coords = parseGoogleMapsUrl(googleMapsUrlValue);
      if (coords) {
        setValue('latitude', coords.latitude, { shouldValidate: true });
        setValue('longitude', coords.longitude, { shouldValidate: true });
      }
    }
  }, [googleMapsUrlValue, setValue]);

  const handleCoverClick = () => {
    fileInputRef.current?.click();
  };

  const handleFileChange = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;

    try {
      const reader = new FileReader();
      reader.onloadend = () => {
        setCoverPreview(reader.result as string);
      };
      reader.readAsDataURL(file);

      const compressedBlob = await compressImageToBlob(file, 1024 * 1024, 1200);
      setCoverBlob(compressedBlob);
    } catch (err) {
      toast.error('Failed to process image');
    }
  };

  const onSubmit = async (values: FormValues) => {
    if (!coverBlob) {
      toast.error(t('validation.coverRequired'));
      return;
    }

    setIsSubmitting(true);
    try {
      // 1. Create venue
      const { id: venueId } = await createVenue(companyId, {
        ...values,
        latitude: values.latitude as number,
        longitude: values.longitude as number,
      });

      // 2. Upload cover
      const publicUrl = await uploadImage(
        coverBlob,
        API_ENDPOINTS.B2B.VENUES.COVER_UPLOAD_URL(venueId),
      );

      // 3. Patch venue with cover URL
      await patchVenue(venueId, { coverImageUrl: publicUrl });

      router.push(PAGE_ROUTES.B2B.COMPANIES_DETAIL(companyId));
    } catch (err: any) {
      toast.error(t('toast.createFailed'));
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <>
      <div className={styles.header}>
        <Header
          title={t('title')}
          left={
            <Icon
              src="/assets/b2b/disclosure-left-icon.svg"
              alt="back"
              width={14}
              height={27}
              onClick={() => router.back()}
            />
          }
        />
      </div>

      <div className={styles.wrapper}>
        <form className={styles.form} onSubmit={handleSubmit(onSubmit)}>
          <div className={styles.coverSection}>
            <div className={styles.coverContainer} onClick={handleCoverClick}>
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
                    <span className={styles.requiredAsterisk}>*</span>
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
            {isSubmitted && !coverBlob && (
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

          <div className="flex gap-4">
            <div className={`${stylesCommon.sectionContainer} flex-1`}>
              <FormSectionTitle required>{t('latitude')}</FormSectionTitle>
              <Input
                {...register('latitude')}
                placeholder={t('latitude')}
                className={stylesCommon.inputDark}
              />
              {errors.latitude?.message && (
                <p className={stylesCommon.errorMessage}>
                  {errors.latitude.message}
                </p>
              )}
            </div>
            <div className={`${stylesCommon.sectionContainer} flex-1`}>
              <FormSectionTitle required>{t('longitude')}</FormSectionTitle>
              <Input
                {...register('longitude')}
                placeholder={t('longitude')}
                className={stylesCommon.inputDark}
              />
              {errors.longitude?.message && (
                <p className={stylesCommon.errorMessage}>
                  {errors.longitude.message}
                </p>
              )}
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
              disabled={isSubmitting}
            >
              {isSubmitting ? <Spinner /> : t('submit')}
            </Button>
          </div>
        </form>
      </div>
    </>
  );
}
