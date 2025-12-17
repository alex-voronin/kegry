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

export function BioSection({
    bio,
    onBioChange,
    readOnly = false,
}: BioSectionProps) {
    return (
        <div className={stylesCommon.sectionContainer}>
            <FormSectionTitle>Bio</FormSectionTitle>
            <textarea
                className={styles.textarea}
                placeholder="Write about youself..."
                value={bio}
                onChange={(event: React.ChangeEvent<HTMLTextAreaElement>) =>
                    onBioChange(event.target.value)
                }
                readOnly={readOnly}
            />
        </div>
    );
}


