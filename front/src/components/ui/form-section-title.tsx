import React from 'react';

interface FormSectionTitleProps {
  children: React.ReactNode;
  className?: string; // Allow optional className override/extension
  required?: boolean;
}

export function FormSectionTitle({
  children,
  className = '',
  required = false,
}: FormSectionTitleProps) {
  // Using inline Tailwind classes here as it's a very simple component,
  // but could be moved to a module if it grows.
  // Matching the "Name", "Age" text style from the screenshot (dark gray/white/50).
  return (
    <h3 className={`pl-2 text-sm text-white/50 sm:pl-0 ${className}`}>
      {children}
      {required && <span className="text-primary ml-0.5 text-base">*</span>}
    </h3>
  );
}
