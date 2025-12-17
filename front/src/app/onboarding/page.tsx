"use client";

import { useState } from "react";
import { Button, ButtonVariant } from "@/components/ui/button";
import { ProfileImageSection } from "@/components/ui/profile-sections/profile-image-section";
import { HobbiesSection } from "@/components/ui/profile-sections/hobbies-section";
import { FormSectionTitle } from "@/components/ui/form-section-title";
import { NameSection } from "@/components/ui/profile-sections/name-section";
import { AgeSection } from "@/components/ui/profile-sections/age-section";
import { GenderSection } from "@/components/ui/profile-sections/gender-section";
import { BioSection } from "@/components/ui/profile-sections/bio-section";
import styles from "./onboarding.module.css";

const GENDER_OPTIONS = [
    { value: "male", label: "Male", width: 75 },
    { value: "female", label: "Female", width: 75 },
];

export default function OnboardingPage() {
    const [gender, setGender] = useState<string | null>(null);
    const [hobbies, setHobbies] = useState<{ value: string; label: string }[]>([]);
    const [firstName, setFirstName] = useState("");
    const [lastName, setLastName] = useState("");
    const [age, setAge] = useState("");
    const [bio, setBio] = useState("");

    return (
        <div className={styles.page}>
            <div className={styles.container}>
                <h1 className={styles.header}>Tell us about yourself</h1>

                <ProfileImageSection src="/assets/camera-placeholder.png" />

                <form className={styles.form} onSubmit={(e) => e.preventDefault()}>
                    <NameSection
                        firstName={firstName}
                        lastName={lastName}
                        onFirstNameChange={setFirstName}
                        onLastNameChange={setLastName}
                    />

                    <AgeSection
                        age={age}
                        onAgeChange={setAge}
                    />

                    <GenderSection
                        options={GENDER_OPTIONS}
                        value={gender}
                        onChange={setGender}
                    />

                    <BioSection
                        bio={bio}
                        onBioChange={setBio}
                    />

                    {/* Hobbies Section */}
                    <HobbiesSection value={hobbies} onChange={setHobbies} />

                    {/* Footer */}
                    <div className={styles.footer}>
                        <Button
                            variant={ButtonVariant.Default}
                            className={styles.continueButton}
                        >
                            Continue
                        </Button>
                    </div>
                </form>
            </div>
        </div>
    );
}
