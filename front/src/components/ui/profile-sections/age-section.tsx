"use client";

import * as React from "react";
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
    return (
        <div className={stylesCommon.sectionContainer}>
            <FormSectionTitle>Age</FormSectionTitle>
            <Input
                placeholder="Your age"
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

