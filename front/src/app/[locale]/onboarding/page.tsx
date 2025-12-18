'use client';

import { useState } from 'react';
import { useTranslations } from 'next-intl';
import { Button, ButtonVariant } from '@/components/ui/button';
import { ProfileImageSection } from '@/components/ui/profile-sections/profile-image-section';
import { HobbiesSection } from '@/components/ui/profile-sections/hobbies-section';
import { NameSection } from '@/components/ui/profile-sections/name-section';
import { AgeSection } from '@/components/ui/profile-sections/age-section';
import { GenderSection } from '@/components/ui/profile-sections/gender-section';
import { BioSection } from '@/components/ui/profile-sections/bio-section';
import styles from './onboarding.module.css';

export default function OnboardingPage() {
  const t = useTranslations('screens.onboarding');
  const tGender = useTranslations('components.profile-sections.gender');
  const [gender, setGender] = useState<string | null>(null);
  const [hobbies, setHobbies] = useState<{ value: string; label: string }[]>(
    [],
  );
  const [firstName, setFirstName] = useState('');
  const [lastName, setLastName] = useState('');
  const [age, setAge] = useState('');
  const [bio, setBio] = useState('');

  const GENDER_OPTIONS = [
    { value: 'male', label: tGender('male'), width: 75 },
    { value: 'female', label: tGender('female'), width: 75 },
  ];

  return (
    <div className={styles.page}>
      <div className={styles.container}>
        <h1 className={styles.header}>{t('title')}</h1>

        <ProfileImageSection src="/assets/camera-placeholder.png" />

        <form className={styles.form} onSubmit={(e) => e.preventDefault()}>
          <NameSection
            firstName={firstName}
            lastName={lastName}
            onFirstNameChange={setFirstName}
            onLastNameChange={setLastName}
          />

          <AgeSection age={age} onAgeChange={setAge} />

          <GenderSection
            options={GENDER_OPTIONS}
            value={gender}
            onChange={setGender}
          />

          <BioSection bio={bio} onBioChange={setBio} />

          <HobbiesSection value={hobbies} onChange={setHobbies} />

          <div className={styles.footer}>
            <Button
              variant={ButtonVariant.Default}
              className={styles.continueButton}
            >
              {t('continue')}
            </Button>
          </div>
        </form>
      </div>
    </div>
  );
}
