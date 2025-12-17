"use client";

import * as React from "react";
import { useTranslations } from "next-intl";
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
    const t = useTranslations("components.profile-sections.bio");
    const MAX_BIO_LENGTH = 200;

    return (
        <div className={stylesCommon.sectionContainer}>
            <FormSectionTitle>{t("title")}</FormSectionTitle>
            <div className={styles.textareaWrapper}>
                <textarea
                    className={styles.textarea}
                    placeholder={t("placeholder")}
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


