"use client";

import * as React from "react";
import { useTranslations } from "next-intl";
import Select from "react-select";
import { FormSectionTitle } from "@/components/ui/form-section-title";

export interface HobbyOption {
    value: string;
    label: string;
}

export interface HobbiesSectionProps {
    value: HobbyOption[];
    onChange: (hobbies: HobbyOption[]) => void;
    readOnly?: boolean;
}

const selectStyles = {
    control: (base: Record<string, unknown>) => ({
        ...base,
        backgroundColor: "#292929",
        borderColor: "transparent",
        minHeight: "44px",
        borderRadius: "0",
        "@media (min-width: 768px)": {
            borderRadius: "12px",
        },
    }),
    menu: (base: Record<string, unknown>) => ({
        ...base,
        backgroundColor: "#292929",
    }),
    option: (base: Record<string, unknown>, state: { isFocused: boolean }) => ({
        ...base,
        backgroundColor: state.isFocused ? "rgba(255,255,255,0.1)" : "transparent",
        color: "#ffffff",
    }),
    multiValue: (base: Record<string, unknown>) => ({
        ...base,
        backgroundColor: "transparent",
        border: "1px solid #ffffff",
        borderRadius: "9999px",
    }),
    multiValueLabel: (base: Record<string, unknown>) => ({
        ...base,
        color: "#ffffff",
    }),
    multiValueRemove: (base: Record<string, unknown>) => ({
        ...base,
        color: "#A0A0A0",
        ":hover": {
            backgroundColor: "rgba(255,255,255,0.2)",
            color: "#ffffff",
        },
    }),
    input: (base: Record<string, unknown>) => ({
        ...base,
        color: "#ffffff",
    }),
    placeholder: (base: Record<string, unknown>) => ({
        ...base,
        color: "#A0A0A0",
    }),
};

const readOnlySelectStyles = {
    ...selectStyles,
    multiValueRemove: (base: Record<string, unknown>) => ({
        ...base,
        display: "none",
    }),
};

export function HobbiesSection({ value, onChange, readOnly = false }: HobbiesSectionProps) {
    const t = useTranslations("components.profile-sections.hobbies");

    const HOBBY_OPTIONS: HobbyOption[] = [
        { value: "volleyball", label: t("volleyball") },
        { value: "boxing", label: t("boxing") },
        { value: "camping", label: t("camping") },
    ];

    return (
        <div style={{ display: "flex", flexDirection: "column", gap: "2px" }}>
            <FormSectionTitle>{t("title")}</FormSectionTitle>
            <Select
                instanceId="hobby-select"
                isMulti
                options={HOBBY_OPTIONS}
                value={value}
                onChange={(selected) => onChange(selected as HobbyOption[])}
                placeholder={t("placeholder")}
                classNamePrefix="hobby-select"
                isDisabled={readOnly}
                isClearable={!readOnly}
                styles={readOnly ? readOnlySelectStyles : selectStyles}
                components={readOnly ? { DropdownIndicator: () => null, IndicatorSeparator: () => null } : undefined}
            />
        </div>
    );
}
