"use client";

import * as React from "react";
import { useTranslations } from "next-intl";
import { FormSectionTitle } from "@/components/ui/form-section-title";
import { Input } from "@/components/ui/input";
import stylesCommon from "@/components/ui/common.module.css";

export interface AgeSectionProps {
    age: string;
    readOnly?: boolean;
    onAgeChange: (value: string) => void;
}

export function AgeSection({
    age,
    onAgeChange,
    readOnly = false,
}: AgeSectionProps) {
    const t = useTranslations("components.profile-sections.age");

    return (
        <div className={stylesCommon.sectionContainer}>
            <FormSectionTitle>{t("title")}</FormSectionTitle>
            <Input
                placeholder={t("placeholder")}
                type="number"
                className={stylesCommon.inputDark}
                value={age}
                onChange={(event: React.ChangeEvent<HTMLInputElement>) =>
                    onAgeChange(event.target.value)
                }
                readOnly={readOnly}
            />
        </div>
    );
}

