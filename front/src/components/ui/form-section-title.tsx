import React from "react";

interface FormSectionTitleProps {
    children: React.ReactNode;
    className?: string; // Allow optional className override/extension
}

export function FormSectionTitle({ children, className = "" }: FormSectionTitleProps) {
    // Using inline Tailwind classes here as it's a very simple component, 
    // but could be moved to a module if it grows.
    // Matching the "Name", "Age" text style from the screenshot (dark gray/white/50).
    return (
        <h3 className={`text-sm text-white/50 ${className}`}>
            {children}
        </h3>
    );
}
