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
import { PAGE_ROUTES } from '@/lib/routes';
import {
  useUserProfileStore,
  Gender,
  PatchProfileRequest,
} from '@/store/use-user-profile-store';
import { useHobbiesStore } from '@/store/use-hobbies-store';
import { HobbyOption } from '@/components/ui/profile-sections/hobbies-section';
import styles from './edit.module.css';

export default function ProfileEditPage() {
  const t = useTranslations('screens.editProfile');
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

  const GENDER_OPTIONS = [
    { value: 'male' as const, label: tGender('male'), width: 75 },
    { value: 'female' as const, label: tGender('female'), width: 75 },
  ];

  const selectedHobbiesOptions: HobbyOption[] = hobbies
    .filter((h) => selectedHobbyIds.includes(h.id))
    .map((h) => ({
      value: h.id,
      label: tHobbies(h.name),
    }));

  const handleHobbiesChange = (selected: HobbyOption[]) => {
    setSelectedHobbyIds(selected.map((h) => h.value));
  };

  const handleGenderChange = (value: string | null) => {
    setGender(value as Gender | null);
  };

  const handleSave = async () => {
    setIsSaving(true);
    try {
      const patchData: Partial<PatchProfileRequest> = {
        firstName,
        lastName,
        bio,
        birthday,
        gender: gender ?? undefined,
        hobbyIds: selectedHobbyIds,
      };

      await patchProfile(patchData);
      toast.success(t('toast.saved'));
      router.push(PAGE_ROUTES.PROFILE);
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

  if (isLoading && !serverProfile) {
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

      <form className={styles.form} onSubmit={(e) => e.preventDefault()}>
        <NameSection
          firstName={firstName}
          lastName={lastName}
          onFirstNameChange={setFirstName}
          onLastNameChange={setLastName}
        />

        <BirthdaySection birthday={birthday} onBirthdayChange={setBirthday} />

        <GenderSection
          options={GENDER_OPTIONS}
          value={gender}
          onChange={handleGenderChange}
        />

        <BioSection bio={bio} onBioChange={setBio} />

        <HobbiesSection
          value={selectedHobbiesOptions}
          onChange={handleHobbiesChange}
        />

        <div className={styles.footer}>
          <Button
            variant={ButtonVariant.Default}
            className={styles.saveButton}
            type="button"
            onClick={handleSave}
            disabled={
              !hasChanges ||
              isSaving ||
              !firstName ||
              !lastName ||
              !birthday ||
              !gender
            }
          >
            {isSaving ? <Spinner /> : t('save')}
          </Button>
        </div>
      </form>
    </div>
  );
}
