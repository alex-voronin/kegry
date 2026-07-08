'use client';

import { useState, useRef } from 'react';
import { useTranslations } from 'next-intl';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { toast } from 'react-toastify';
import { Camera, Users } from 'lucide-react';

import { Toggle } from '@/components/ui/toggle';
import { TabControl } from '@/components/ui/tab-control';

import { Button, ButtonVariant } from '@/components/ui/button';
import { Spinner } from '@/components/ui/spinner';
import { Input } from '@/components/ui/input';
import { Textarea } from '@/components/ui/textarea';
import { Header } from '@/components/ui/header';
import { Icon } from '@/components/ui/icon';
import { FormSectionTitle } from '@/components/ui/form-section-title';
import {
  ContactSection,
  type EventContactForm,
  type ContactSectionHandle,
} from '@/components/ui/contact-section';
import { ImageCropperModal } from '@/components/ui/image-cropper-modal';
import { compressImageToBlob } from '@/lib/image-utils';
import { VENUE_COVER_MAX_SIZE_BYTES } from '@/lib/constants';
import stylesCommon from '@/components/ui/common.module.css';

export type MeetupFormValues = {
  title: string;
  description: string | undefined;
  startDateTime: string;
  maxParticipants?: string;
  requiresApproval: boolean;
};

interface MeetupFormProps {
  pageTitle: string;
  userName: string;
  onCancel: () => void;
  initialIsSimple?: boolean;
  initialValues?: {
    title: string;
    description: string;
    startDateTime: string;
    coverImageUrl: string | null;
    contacts: EventContactForm[];
    hobbyIds: string[];
    maxParticipants: number | null;
    requiresApproval: boolean;
  };
  onSubmit: (data: {
    title: string;
    description: string;
    startDateTime: string;
    coverBlob: Blob | null;
    contacts: EventContactForm[];
    hobbyIds: string[];
    maxParticipants: number | null;
    requiresApproval: boolean;
  }) => Promise<void>;
  isSubmitting: boolean;
  submitLabel: string;
}

export function MeetupForm({
  pageTitle,
  userName,
  onCancel,
  initialIsSimple,
  initialValues,
  onSubmit,
  isSubmitting,
  submitLabel,
}: MeetupFormProps) {
  const t = useTranslations('components.forms.event');
  const fileInputRef = useRef<HTMLInputElement>(null);
  const [isSimple, setIsSimple] = useState(initialIsSimple ?? true);
  const [participantMode, setParticipantMode] = useState<'tete' | 'custom'>(
    'tete',
  );

  const meetupSchema = z.object({
    title: z
      .string()
      .min(1, t('validation.titleRequired'))
      .max(200, t('validation.titleMaxLength')),
    description: isSimple
      ? z.string().optional()
      : z
          .string()
          .min(1, t('validation.descriptionRequired'))
          .max(2000, t('validation.descriptionMaxLength')),
    startDateTime: z
      .string()
      .min(1, t('validation.startDateTimeRequired'))
      .refine(
        (val) => {
          const date = new Date(val);
          return !isNaN(date.getTime()) && date >= new Date();
        },
        { message: t('validation.startDateTimePast') },
      )
      .refine(
        (val) => {
          const date = new Date(val);
          const maxDate = new Date();
          maxDate.setMonth(maxDate.getMonth() + 2);
          return !isNaN(date.getTime()) && date <= maxDate;
        },
        { message: t('validation.startDateTimeTooFar') },
      ),
    maxParticipants: z.string().optional(),
    requiresApproval: z.boolean(),
  });

  const [coverPreview, setCoverPreview] = useState<string | null>(
    initialValues?.coverImageUrl ?? null,
  );
  const [coverBlob, setCoverBlob] = useState<Blob | null>(null);
  const [rawImageSrc, setRawImageSrc] = useState<string | null>(null);
  const [isCropperOpen, setIsCropperOpen] = useState(false);
  const [hobbyIds] = useState<string[]>(initialValues?.hobbyIds ?? []);
  const contactsRef = useRef<ContactSectionHandle>(null);

  const now = new Date();
  const tomorrow = new Date(now);
  tomorrow.setDate(tomorrow.getDate() + 1);
  const defaultDateTime = new Date(
    tomorrow.getTime() - tomorrow.getTimezoneOffset() * 60000,
  )
    .toISOString()
    .slice(0, 16);

  const {
    register,
    handleSubmit,
    watch,
    setValue,
    formState: { errors },
  } = useForm<MeetupFormValues>({
    resolver: zodResolver(meetupSchema),
    defaultValues: {
      title: initialValues?.title ?? t('meetupTitle', { name: userName }),
      description: initialValues?.description ?? '',
      startDateTime: initialValues?.startDateTime ?? defaultDateTime,
      maxParticipants: initialValues?.maxParticipants?.toString() ?? '',
      requiresApproval: initialValues?.requiresApproval ?? true,
    },
  });

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
      );
      setCoverBlob(compressedBlob);
      setIsCropperOpen(false);
    } catch {
      toast.error('Failed to process cropped image');
    }
  };

  const onFormSubmit = handleSubmit(async (values: MeetupFormValues) => {
    if (!isSimple && !coverBlob && !initialValues?.coverImageUrl) {
      toast.error(t('validation.coverRequired'));
      return;
    }

    if (!contactsRef.current?.validate()) {
      return;
    }

    const maxParticipants = isSimple
      ? participantMode === 'tete'
        ? 2
        : values.maxParticipants
          ? parseInt(values.maxParticipants, 10)
          : null
      : values.maxParticipants
        ? parseInt(values.maxParticipants, 10)
        : null;

    const requiresApproval = values.requiresApproval;

    await onSubmit({
      title: values.title,
      description: isSimple ? '' : (values.description ?? ''),
      startDateTime: values.startDateTime,
      coverBlob: isSimple ? null : coverBlob,
      contacts: contactsRef.current?.getContacts() ?? [],
      hobbyIds,
      maxParticipants,
      requiresApproval,
    });
  });

  return (
    <div className="overflow-hidden sm:rounded-[16px]">
      <form onSubmit={onFormSubmit} className="flex flex-col gap-5">
        {/* Cover image — advanced only */}
        {!isSimple && (
          <div className="w-full">
            <div
              className="relative h-[130px] w-full shrink-0 cursor-pointer"
              onClick={handleCoverClick}
            >
              <Header
                className="absolute! top-0! left-0! z-10! w-full! bg-transparent!"
                title={pageTitle}
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
                    alt="Cover"
                    className="h-full w-full object-cover"
                  />
                  <div className="absolute inset-0 bg-black/60" />
                </>
              ) : (
                <div className="bg-form-element flex h-full w-full items-center justify-center">
                  <span className="relative inline-flex">
                    <Camera size={40} className="text-secondary opacity-50" />
                    <span className="text-primary absolute -top-1 -right-2 font-sans text-xl">
                      *
                    </span>
                  </span>
                </div>
              )}
              <div className="absolute right-3 bottom-3 flex h-10 w-10 items-center justify-center rounded-full bg-black/60">
                <Camera size={20} className="text-white" />
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
        )}

        {/* Simple mode header */}
        {isSimple && (
          <Header
            title={pageTitle}
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
        )}

        {/* Mode toggle */}
        <TabControl
          options={[
            { value: 'simple', label: 'Simple' },
            { value: 'advanced', label: 'Advanced' },
          ]}
          value={isSimple ? 'simple' : 'advanced'}
          onChange={(v) => setIsSimple(v === 'simple')}
          className="sm:mx-4"
        />

        {/* Title */}
        <div className={`${stylesCommon.sectionContainer} px-0 sm:px-4`}>
          <FormSectionTitle required>{t('titleField')}</FormSectionTitle>
          <Input
            {...register('title')}
            placeholder={t('titleField')}
            className={stylesCommon.inputDark}
          />
          {errors.title && (
            <p className={stylesCommon.errorMessage}>{errors.title.message}</p>
          )}
        </div>

        {/* Description — advanced only */}
        {!isSimple && (
          <div className={`${stylesCommon.sectionContainer} px-0 sm:px-4`}>
            <FormSectionTitle required>{t('description')}</FormSectionTitle>
            <Textarea
              {...register('description')}
              placeholder={t('description')}
              rows={4}
              className={`${stylesCommon.inputDark} min-h-[120px] py-3`}
            />
            {errors.description && (
              <p className={stylesCommon.errorMessage}>
                {errors.description.message}
              </p>
            )}
          </div>
        )}

        {/* Date & Time */}
        <div className={`${stylesCommon.sectionContainer} px-0 sm:px-4`}>
          <FormSectionTitle required>{t('startDateTime')}</FormSectionTitle>
          <Input
            type="datetime-local"
            {...register('startDateTime')}
            className={`${stylesCommon.inputDark} [color-scheme:dark]`}
          />
          {errors.startDateTime && (
            <p className={stylesCommon.errorMessage}>
              {errors.startDateTime.message}
            </p>
          )}
        </div>

        {/* Participants mode — simple only */}
        {isSimple && (
          <div className={`${stylesCommon.sectionContainer} px-0 sm:px-4`}>
            <FormSectionTitle>{t('participants')}</FormSectionTitle>
            <TabControl
              size="md"
              options={[
                {
                  value: 'tete',
                  label: (
                    <>
                      <Users size={16} />
                      Tête-à-tête
                    </>
                  ),
                },
                {
                  value: 'custom',
                  label: (
                    <>
                      <span className="flex">
                        <Users size={16} />
                        <Users size={16} />
                      </span>
                      {t('group')}
                    </>
                  ),
                },
              ]}
              value={participantMode}
              onChange={(v) => setParticipantMode(v as 'tete' | 'custom')}
            />
          </div>
        )}

        {/* Max participants input — simple custom or advanced */}
        {(participantMode === 'custom' || !isSimple) && (
          <div className={`${stylesCommon.sectionContainer} px-0 sm:px-4`}>
            <FormSectionTitle>{t('maxParticipants')}</FormSectionTitle>
            <Input
              type="number"
              min={1}
              {...register('maxParticipants')}
              placeholder={t('maxParticipantsPlaceholder')}
              className={stylesCommon.inputDark}
            />
          </div>
        )}

        {/* Requires Approval */}
        <div className={`${stylesCommon.sectionContainer} px-0 sm:px-4`}>
          <div className="flex items-center justify-between px-2 sm:px-0">
            <span className="text-sm text-white/80">
              {t('requiresApproval')}
            </span>
            <Toggle
              checked={watch('requiresApproval')}
              onChange={(v) => setValue('requiresApproval', v)}
            />
          </div>
        </div>

        <ContactSection
          ref={contactsRef}
          initialContacts={initialValues?.contacts}
        />

        <div className="mb-10 flex justify-center px-0 sm:px-4">
          <Button
            type="submit"
            disabled={isSubmitting}
            className="w-[280px]"
            variant={ButtonVariant.Default}
          >
            {isSubmitting ? <Spinner /> : submitLabel}
          </Button>
        </div>

        <ImageCropperModal
          isOpen={isCropperOpen}
          onClose={() => setIsCropperOpen(false)}
          imageSrc={rawImageSrc}
          onCropComplete={handleCropComplete}
          outputSize={{ width: 784, height: 208 }}
        />
      </form>
    </div>
  );
}
