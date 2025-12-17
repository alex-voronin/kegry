"use client";

import * as React from "react";
import { FormSectionTitle } from "@/components/ui/form-section-title";
import stylesCommon from "@/components/ui/common.module.css";
import styles from "./bio-section.module.css";

export interface BioSectionProps {
    bio: string;
    readOnly?: boolean;
    onBioChange: (value: string) => void;
}

const MAX_BIO_LENGTH = 200;

export function BioSection({
    bio,
    onBioChange,
    readOnly = false,
}: BioSectionProps) {
    return (
        <div className={stylesCommon.sectionContainer}>
            <FormSectionTitle>Bio</FormSectionTitle>
            <div className={styles.textareaWrapper}>
                <textarea
                    className={styles.textarea}
                    placeholder="Write about youself..."
                    value={bio}
                    maxLength={MAX_BIO_LENGTH}
                    onChange={(event: React.ChangeEvent<HTMLTextAreaElement>) =>
                        onBioChange(event.target.value)
                    }
                    readOnly={readOnly}
                />
                <span className={styles.counter}>
                    {bio.length}/{MAX_BIO_LENGTH}
                </span>
            </div>
        </div>
    );
}


