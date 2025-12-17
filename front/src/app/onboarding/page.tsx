"use client";

import React, { useState } from "react";
import Image from "next/image";
import { Button, ButtonVariant } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { FormSectionTitle } from "@/components/ui/form-section-title";
import { SegmentedControl } from "@/components/ui/segmented-control";
import styles from "./onboarding.module.css";

const GENDER_OPTIONS = [
    { value: "male", label: "Male", width: 75 },
    { value: "female", label: "Female", width: 75 },
];

export default function OnboardingPage() {
    const [gender, setGender] = useState<string | null>(null);

    return (
        <div className={styles.page}>
            <div className={styles.container}>
                <h1 className={styles.header}>Tell us about yourself</h1>

                {/* Profile Picture Placeholder */}
                <div className={styles.profileSection}>
                    <div className={styles.profilePlaceholder}>
                        <Image
                            src="/assets/camera-placeholder.png"
                            alt="Upload Profile Picture"
                            width={64}
                            height={64}
                            className={styles.profileImage}
                        />
                    </div>
                </div>

                <form className={styles.form} onSubmit={(e) => e.preventDefault()}>
                    {/* Name Section */}
                    <div className={styles.formGroup}>
                        <FormSectionTitle>Name</FormSectionTitle>
                        <div className={styles.nameFields}>
                            <Input placeholder="First Name" className={styles.inputDark} />
                            <Input placeholder="Last Name" className={styles.inputDark} />
                        </div>
                    </div>

                    {/* Age Section */}
                    <div className={styles.formGroup}>
                        <FormSectionTitle>Age</FormSectionTitle>
                        <Input placeholder="Your age" type="number" className={styles.inputDark} />
                    </div>

                    {/* Gender Section */}
                    <div className={styles.formGroup}>
                        <FormSectionTitle>Gender</FormSectionTitle>
                        <SegmentedControl
                            options={GENDER_OPTIONS}
                            value={gender}
                            onChange={setGender}
                        />
                    </div>

                    {/* Bio Section */}
                    <div className={styles.formGroup}>
                        <FormSectionTitle>Bio</FormSectionTitle>
                        <textarea
                            className={styles.textarea}
                            placeholder="Write about youself..."
                        />
                    </div>

                    {/* Hobbies Section */}
                    <div className={styles.formGroup}>
                        <FormSectionTitle>Hobbies</FormSectionTitle>
                        <Input placeholder="Start typing to add a new hobby..." className={styles.inputDark} />
                    </div>

                    {/* Footer */}
                    <div className={styles.footer}>
                        <Button
                            variant={ButtonVariant.Ghost}
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
