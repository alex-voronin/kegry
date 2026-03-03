'use client';

import { useState, useEffect, useRef } from 'react';
import { useTranslations } from 'next-intl';
import { useRouter } from '@/i18n/routing';
import { toast } from 'react-toastify';
import { Button, ButtonVariant } from '@/components/ui/button';
import { HobbiesSection } from '@/components/ui/profile-sections/hobbies-section';
import { NameSection } from '@/components/ui/profile-sections/name-section';
import { BirthdaySection } from '@/components/ui/profile-sections/birthday-section';
import { GenderSection } from '@/components/ui/profile-sections/gender-section';
import { BioSection } from '@/components/ui/profile-sections/bio-section';
import { Header } from '@/components/ui/header';
import { Icon } from '@/components/ui/icon';
import { Spinner } from '@/components/ui/spinner';
import {
  useUserProfileStore,
  Gender,
  PatchProfileRequest,
} from '@/store/use-user-profile-store';
import { useHobbiesStore } from '@/store/use-hobbies-store';
import { HobbyOption } from '@/components/ui/profile-sections/hobbies-section';
import styles from './edit.module.css';

import { z } from 'zod';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { calculateAge } from '@/lib/date';

export default function ProfileEditPage() {
  const t = useTranslations('screens.editProfile');
  const tValidation = useTranslations('screens.onboarding.validation');
  const router = useRouter();
  const tHobbies = useTranslations('components.profile-sections.hobbies');
  const tGender = useTranslations('components.profile-sections.gender');

  const [isSaving, setIsSaving] = useState(false);

  const {
    serverProfile,
    firstName,
    lastName,
    birthday,
    bio,
    gender,
    selectedHobbyIds,
    isLoading,
    fetchMyProfile,
    patchProfile,
    setFirstName,
    setLastName,
    setBirthday,
    setBio,
    setGender,
    setSelectedHobbyIds,
    discardLocalProfileChanges,
  } = useUserProfileStore();

  const { hobbies, fetchHobbies } = useHobbiesStore();

  const isInitialized = useRef(false);

  useEffect(() => {
    if (isInitialized.current) return;
    isInitialized.current = true;

    if (!serverProfile) {
      fetchMyProfile();
    }
    if (hobbies.length === 0) {
      fetchHobbies();
    }
  }, [serverProfile, hobbies.length, fetchMyProfile, fetchHobbies]);

  const localizedSchema = z.object({
    firstName: z.string().trim().min(1, tValidation('firstNameRequired')),
    lastName: z.string().trim().min(1, tValidation('lastNameRequired')),
    birthday: z
      .string()
      .min(1, tValidation('birthdayRequired'))
      .refine((dateString) => {
        return calculateAge(dateString) >= 18;
      }, tValidation('minAge')),
    gender: z.string().min(1, tValidation('genderRequired')),
    bio: z.string().optional(),
    hobbies: z
      .array(z.string())
      .min(1, tValidation('minHobbies'))
      .max(10, tValidation('maxHobbies')),
  });

  type EditProfileFormValues = z.infer<typeof localizedSchema>;

  const {
    reset,
    control,
    handleSubmit,
    formState: { errors },
  } = useForm<EditProfileFormValues>({
    resolver: zodResolver(localizedSchema),
    defaultValues: {
      firstName: firstName || '',
      lastName: lastName || '',
      birthday: birthday || '',
      gender: gender || '',
      bio: bio || '',
      hobbies: selectedHobbyIds || [],
    },
  });

  useEffect(() => {
    if (serverProfile) {
      reset({
        firstName: firstName,
        lastName: lastName,
        birthday: birthday,
        gender: gender || '',
        bio: bio,
        hobbies: selectedHobbyIds,
      });
    }
  }, [serverProfile, reset]); // eslint-disable-line react-hooks/exhaustive-deps

  const GENDER_OPTIONS = [
    { value: 'Male' as const, label: tGender('male'), width: 75 },
    { value: 'Female' as const, label: tGender('female'), width: 75 },
  ];

  const selectedHobbiesOptions: HobbyOption[] = hobbies
    .filter((h) => selectedHobbyIds.includes(h.id))
    .map((h) => ({
      value: h.id,
      label: tHobbies(h.name),
    }));

  const onSubmit = async (data: EditProfileFormValues) => {
    setIsSaving(true);
    try {
      const patchData: Partial<PatchProfileRequest> = {
        firstName: data.firstName,
        lastName: data.lastName,
        bio: data.bio,
        birthday: data.birthday,
        gender: data.gender as Gender,
        hobbyIds: data.hobbies,
      };

      await patchProfile(patchData);
      router.back();
    } catch (err: any) {
      toast.error(err.message || 'Failed to save profile');
    } finally {
      setIsSaving(false);
    }
  };

  const handleBack = () => {
    discardLocalProfileChanges(serverProfile);
    router.back();
  };

  if (serverProfile === null) {
    return (
      <div className="flex flex-1 items-center justify-center">
        <Spinner />
      </div>
    );
  }

  const hasChanges =
    firstName !== serverProfile?.firstName ||
    lastName !== serverProfile?.lastName ||
    bio !== serverProfile?.bio ||
    birthday !== serverProfile?.birthday ||
    gender !== serverProfile?.gender ||
    JSON.stringify(selectedHobbyIds.sort()) !==
      JSON.stringify(serverProfile?.hobbies.map((h) => h.id).sort() || []);

  return (
    <div className={styles.wrapper}>
      <Header
        title={t('title')}
        left={
          <Icon
            src="/assets/common/disclosure-left-icon.svg"
            alt="back"
            width={14}
            height={27}
            onClick={handleBack}
          />
        }
      />

      <form className={styles.form} onSubmit={handleSubmit(onSubmit)}>
        <NameSection
          control={control}
          onFirstNameChange={setFirstName}
          onLastNameChange={setLastName}
        />
        <BirthdaySection
          control={control}
          required
          onBirthdayChange={setBirthday}
        />

        <GenderSection
          control={control}
          options={GENDER_OPTIONS}
          required
          onChange={(val) => setGender(val as Gender | null)}
        />

        <BioSection control={control} onBioChange={setBio} />

        <HobbiesSection
          control={control}
          required
          onChange={(selected) => {
            const ids = selected.map((h) => h.value);
            setSelectedHobbyIds(ids);
          }}
        />

        <div className={styles.footer}>
          <Button
            variant={ButtonVariant.Default}
            className={styles.saveButton}
            type="submit"
            disabled={!hasChanges || isSaving}
          >
            {isSaving ? <Spinner /> : t('save')}
          </Button>
        </div>
      </form>
    </div>
  );
}
