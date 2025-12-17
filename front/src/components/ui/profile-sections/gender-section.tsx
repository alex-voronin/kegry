"use client";

import * as React from "react";
import { FormSectionTitle } from "@/components/ui/form-section-title";
import { SegmentedControl, SegmentedControlOption } from "@/components/ui/segmented-control";
import stylesCommon from "@/components/ui/common.module.css";

export interface GenderSectionProps {
    options: SegmentedControlOption[];
    value: string | null;
    readOnly?: boolean;
    onChange: (value: string) => void;
}

export function GenderSection({
    options,
    value,
    onChange,
    readOnly = false,
}: GenderSectionProps) {
    return (
        <div className={stylesCommon.sectionContainer}>
            <FormSectionTitle>Gender</FormSectionTitle>
            <SegmentedControl
                options={options}
                value={value}
                onChange={onChange}
                className={readOnly ? stylesCommon.readOnlySegmented : undefined}
            />
        </div>
    );
}


