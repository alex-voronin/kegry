'use client';

import { useTranslations } from 'next-intl';
import { useRouter } from '@/i18n/routing';
import { Button, ButtonVariant } from '@/components/ui/button';
import { AvatarSection } from '@/components/ui/profile-sections/avatar-section';
import { HobbiesSection } from '@/components/ui/profile-sections/hobbies-section';
import { NameSection } from '@/components/ui/profile-sections/name-section';
import { BirthdaySection } from '@/components/ui/profile-sections/birthday-section';
import { GenderSection } from '@/components/ui/profile-sections/gender-section';
import { BioSection } from '@/components/ui/profile-sections/bio-section';
import styles from './onboarding.module.css';
import { Header } from '@/components/ui/header';
import { useUserProfileStore, Gender } from '@/store/use-user-profile-store';
import { useHobbiesStore } from '@/store/use-hobbies-store';
import { HobbyOption } from '@/components/ui/profile-sections/hobbies-section';

export default function OnboardingPage() {
  const t = useTranslations('screens.onboarding');
  const router = useRouter();
  const tGender = useTranslations('components.profile-sections.gender');

  // User profile store
  const {
    firstName,
    lastName,
    birthday,
    bio,
    gender,
    avatarBlob,
    setFirstName,
    setLastName,
    setBirthday,
    setBio,
    setGender,
    setAvatarBlob,
  } = useUserProfileStore();
  const { hobbies, selectedHobbyIds, setSelectedHobbyIds } = useHobbiesStore();

  const GENDER_OPTIONS = [
    { value: 'male' as const, label: tGender('male'), width: 75 },
    { value: 'female' as const, label: tGender('female'), width: 75 },
  ];

  const selectedHobbies: HobbyOption[] = hobbies
    .filter((h) => selectedHobbyIds.includes(h.id))
    .map((h) => ({ value: h.id, label: h.name }));

  const handleHobbiesChange = (selected: HobbyOption[]) => {
    setSelectedHobbyIds(selected.map((h) => h.value));
  };

  const handleGenderChange = (value: string | null) => {
    setGender(value as Gender | null);
  };

  const handleContinue = () => {
    router.push('/onboarding/cover');
  };

  return (
    <div className={styles.wrapper}>
      <Header title={t('title')} />

      <AvatarSection avatarBlob={avatarBlob} onAvatarChange={setAvatarBlob} />

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
          value={selectedHobbies}
          onChange={handleHobbiesChange}
        />

        <div className={styles.footer}>
          <Button
            variant={ButtonVariant.Default}
            className={styles.continueButton}
            type="button"
            onClick={handleContinue}
          >
            {t('continue')}
          </Button>
        </div>
      </form>
    </div>
  );
}
